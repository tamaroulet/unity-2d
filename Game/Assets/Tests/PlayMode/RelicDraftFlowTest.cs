// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Core;
using Game.Features.GameFlow;
using Game.Features.Relic;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    public class RelicDraftFlowTest
    {
        [UnityTest]
        public IEnumerator RelicDraftFlow_BossDefeat_OffersUnownedRelics_SelectRaisesChannel()
        {
            var op = SceneManager.LoadSceneAsync("MainGame", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;

            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var bossDialog = UnityEngine.Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            var relicDialog = UnityEngine.Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            var studyBtn = UnityEngine.Object.FindObjectsByType<CommandButtonView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(b => b.gameObject.name == "StudyButton").GetComponent<Button>();

            for (int t = 1; t <= 5; t++)
            {
                int turn = t;
                yield return Wait(() => flow.CurrentPhase == GamePhase.WaitingInput && flow.CurrentState.CurrentTurn == turn);
                Click(studyBtn.gameObject);
                yield return null;
            }

            yield return Wait(() => flow.CurrentState != null && flow.CurrentState.CurrentTurn == 6 && bossDialog.IsVisible);

            var dismiss = GetField<Button>(bossDialog, "_dismissButton");
            Click(dismiss.gameObject);

            // 1. ボス撃破後に RelicDraft パネルが表示される
            yield return Wait(() => relicDialog.IsVisible);
            Assert.IsTrue(relicDialog.IsVisible, "RelicDraft panel not shown after boss defeat.");

            // 2. 未所持レリックのみが候補に出る（重複なし）
            var cards = GetField<List<RelicCardView>>(relicDialog, "_cardViews");
            Assert.IsTrue(cards != null && cards.Count > 0, "No card views.");
            var owned = new HashSet<int>(flow.CurrentState.AcquiredRelicIds);
            var offered = cards.Select(c => c.BoundRelic).ToList();
            Assert.IsTrue(offered.All(r => r != null && !owned.Contains(r.RelicId)), "Offered relic is already owned.");
            Assert.AreEqual(offered.Count, offered.Select(r => r.RelicId).Distinct().Count(), "Duplicate relics offered.");

            // 3. カード選択で RelicAcquiredChannelSO が発火する
            var channel = GetField<RelicAcquiredChannelSO>(relicDialog, "_relicAcquiredChannel");
            int acquired = -1;
            Action<int> handler = id => acquired = id;
            channel.OnEventRaised += handler;

            int targetId = cards[0].BoundRelic.RelicId;
            Click(GetField<Button>(cards[0], "_selectButton").gameObject);
            channel.OnEventRaised -= handler;

            Assert.AreEqual(targetId, acquired, "RelicAcquiredChannelSO did not fire with expected id.");
            yield return Wait(() => !relicDialog.IsVisible && flow.CurrentPhase == GamePhase.WaitingInput);
            Assert.IsFalse(relicDialog.IsVisible, "RelicDraft panel not dismissed.");
        }

        private static void Click(GameObject go) =>
            ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        private static T GetField<T>(object target, string name) where T : class =>
            (target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
             ?? target.GetType().BaseType?.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance))?.GetValue(target) as T;

        private static IEnumerator Wait(Func<bool> cond)
        {
            float t = 0f;
            while (!cond() && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "Timeout waiting for condition.");
        }
    }
}
