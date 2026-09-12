// SPDX-AI-Disclosure: ai-generated
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.Relic;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// 指示書 26: 過去の再発バグを法則で塞ぐ 6 つの不変条件テスト。
    /// UI 操作（ExecuteEvents）経由で実行し、INV-1〜5 の緑および INV-6 の実測値を検証する。
    /// </summary>
    public class GameInvariantsTest
    {
        private const string SceneName = "MainGame";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("Game.MetaProfile");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("Game.MetaProfile");
            PlayerPrefs.Save();
        }

        private static T GetField<T>(Component target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(field != null, $"{target.GetType().Name}.{fieldName} field not found.");
            return (T)field.GetValue(target);
        }

        private static IEnumerator LoadSceneAndInit(System.Action<GameFlowController, Canvas> onReady)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"Failed to load scene '{SceneName}'.");
            while (!load.isDone) yield return null;
            yield return null;

            GameFlowController flow = Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController not found.");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsTrue(canvas != null, "Canvas not found.");

            onReady(flow, canvas);
        }

        [UnityTest]
        public IEnumerator Invariants_FullRun_Clear_INV1_to_INV5_Hold()
        {
            GameFlowController flow = null;
            Canvas canvas = null;
            yield return LoadSceneAndInit((f, c) => { flow = f; canvas = c; });

            RunDriver driver = RunDriver.FromScene();

            int endingCount = 0;
            EndingDecidedChannelSO endingChannel = GetField<EndingDecidedChannelSO>(flow, "_endingDecidedChannel");
            if (endingChannel != null) endingChannel.OnEventRaised += _ => endingCount++;

            GamePhase lastPhase = flow.CurrentPhase;
            float phaseEnterTime = Time.realtimeSinceStartup;

            float deadline = Time.realtimeSinceStartup + 40f;
            while (!driver.EndingVisible && Time.realtimeSinceStartup < deadline)
            {
                // INV-1: フェーズ滞留チェック
                GameInvariants.AssertPhaseProgress(flow, ref lastPhase, ref phaseEnterTime, driver.LastAction, timeoutSeconds: 10f);

                if (driver.Peek(flow) == RunDriver.Action.ClickCommand)
                {
                    // INV-3: WaitingInput 中にブロッカーが残っていないこと
                    GameInvariants.AssertNoFullscreenBlockerInWaitingInput(canvas);

                    // INV-4: グリフが揃っていること
                    GameInvariants.AssertAllTextGlyphsPresent();

                    // INV-5: 押せるボタンは押せること
                    GameInvariants.AssertInteractableButtonsReachable();
                }

                driver.Step(flow);

                yield return null;
            }

            yield return null;

            // INV-2: 終端到達時にエンディングが通知されたこと
            Assert.AreEqual(GamePhase.GameClear, flow.CurrentPhase, "Game did not end in GameClear.");
            GameInvariants.AssertEndingDecided(endingCount, flow.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator Invariants_DefeatRun_INV1_and_INV2_Hold()
        {
            GameFlowController flow = null;
            Canvas canvas = null;
            yield return LoadSceneAndInit((f, c) => { flow = f; canvas = c; });

            RunDriver driver = RunDriver.FromScene();
            // 敗北させたいので Train を連打してスタミナを枯渇させる。
            // ダイアログの捌きは共有し、コマンドの選び方だけを差し替える。
            driver.CommandPolicy = _ => driver.TrainButton;

            int endingCount = 0;
            EndingDecidedChannelSO endingChannel = GetField<EndingDecidedChannelSO>(flow, "_endingDecidedChannel");
            if (endingChannel != null) endingChannel.OnEventRaised += _ => endingCount++;

            GamePhase lastPhase = flow.CurrentPhase;
            float phaseEnterTime = Time.realtimeSinceStartup;

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!driver.EndingVisible && Time.realtimeSinceStartup < deadline)
            {
                GameInvariants.AssertPhaseProgress(flow, ref lastPhase, ref phaseEnterTime, driver.LastAction, timeoutSeconds: 10f);
                driver.Step(flow);
                yield return null;
            }

            yield return null;

            Assert.AreEqual(GamePhase.GameOver, flow.CurrentPhase, "Game did not end in GameOver.");
            GameInvariants.AssertEndingDecided(endingCount, flow.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator Invariants_INV6_SubsystemsFiredAtLeastOnce()
        {
            GameFlowController flow = null;
            Canvas canvas = null;
            yield return LoadSceneAndInit((f, c) => { flow = f; canvas = c; });

            RunDriver driver = RunDriver.FromScene();

            int eventCount = 0;
            int bossCount = 0;
            int draftCount = 0;

            GameEventFiredChannelSO eventChannel = GetField<GameEventFiredChannelSO>(flow, "_eventFiredChannel");
            if (eventChannel != null) eventChannel.OnEventRaised += _ => eventCount++;
            flow.OnBossBattleOccurred += (_, _, _) => bossCount++;
            flow.OnRelicDraftRequested += _ => draftCount++;

            float deadline = Time.realtimeSinceStartup + 40f;
            while (!driver.EndingVisible
                   && flow.CurrentPhase != GamePhase.GameClear
                   && flow.CurrentPhase != GamePhase.GameOver
                   && Time.realtimeSinceStartup < deadline)
            {
                driver.Step(flow);
                yield return null;
            }

            yield return null;

            Debug.Log($"[INV-6 実測値] EventCount={eventCount}, BossCount={bossCount}, DraftCount={draftCount}");

            // INV-6 のアサーション（指示書 26: イベント未発火により現時点では赤になることが期待される）
            GameInvariants.AssertSubsystemsFired(eventCount, bossCount, draftCount);
        }
    }
}
