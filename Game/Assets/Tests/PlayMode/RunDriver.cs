// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Features.GameFlow;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// ゲームを 1 ラン進めるための、唯一のクリック運転手。
    ///
    /// なぜ 1 つにまとめるか:
    ///   以前は SmokeTest と GameInvariantsTest がそれぞれ独自のクリックループを持ち、
    ///   互いに相手が知らないダイアログを 1 つずつ知っている状態になっていた。
    ///     SmokeTest          … メタショップを知る / イベントダイアログを知らない
    ///     GameInvariantsTest … イベントダイアログを知る / メタショップを知らない
    ///   その結果、イベントの発火ターンを 11 に移しただけで SmokeTest が
    ///   押す先を失って 30 秒待って落ちた。ゲームは壊れていない。運転手が古かった。
    ///
    ///   ダイアログを増やしたら、ここへ 1 か所足す。片方だけが知っている状態を作らない。
    /// </summary>
    internal sealed class RunDriver
    {
        public enum Action
        {
            None,
            DismissBoss,
            SelectRelic,
            DismissEvent,
            CloseMetaShop,
            ClickCommand,
        }

        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly BossBattleDialogView _bossDialog;
        private readonly RelicDraftDialogView _relicDraft;
        private readonly EventDialogView _eventDialog;
        private readonly MetaShopDialogView _metaShop;
        private readonly EndingView _ending;

        private readonly Button _bossDismiss;
        private readonly Button _eventOk;
        private readonly Button _shopClose;
        private readonly List<RelicCardView> _relicCards;

        private readonly Button _study;
        private readonly Button _train;
        private readonly Button _rest;

        public int BossCount { get; private set; }
        public int DraftCount { get; private set; }
        public int EventCount { get; private set; }
        public int ShopCount { get; private set; }
        public int CommandCount { get; private set; }

        public string LastAction { get; private set; } = "SceneLoaded";

        /// <summary>直前の Step が押したボタンが、実際にクリックを受理したか。</summary>
        public bool LastClickAccepted { get; private set; } = true;

        public bool EndingVisible => _ending != null && _ending.IsPanelActive;

        public EndingView EndingView => _ending;
        public Button BossDismissButton => _bossDismiss;
        public Button StudyButton => _study;
        public Button TrainButton => _train;
        public Button RestButton => _rest;

        /// <summary>
        /// コマンドの選び方を上書きする。null なら既定の方針を使う。
        /// ダイアログの捌きは共有したいが、どのコマンドを押すかはテストごとに違うため
        /// （例: 敗北させたいテストは Train を連打する）。
        /// </summary>
        public Func<GameFlowController, Button> CommandPolicy { get; set; }

        private RunDriver()
        {
            _bossDialog = UnityEngine.Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            _relicDraft = UnityEngine.Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            _eventDialog = UnityEngine.Object.FindFirstObjectByType<EventDialogView>(FindObjectsInactive.Include);
            _metaShop = UnityEngine.Object.FindFirstObjectByType<MetaShopDialogView>(FindObjectsInactive.Include);
            _ending = UnityEngine.Object.FindFirstObjectByType<EndingView>(FindObjectsInactive.Include);

            _bossDismiss = Field<Button>(_bossDialog, "_dismissButton");
            _eventOk = Field<Button>(_eventDialog, "_okButton");
            _shopClose = Field<Button>(_metaShop, "_closeButton");
            _relicCards = Field<List<RelicCardView>>(_relicDraft, "_cardViews");

            _study = CommandButton("StudyButton");
            _train = CommandButton("TrainButton");
            _rest = CommandButton("RestButton");
        }

        public static RunDriver FromScene()
        {
            return new RunDriver();
        }

        /// <summary>
        /// 次に何をするかだけを返す。何も押さない。
        /// 呼び出し側が「コマンド選択の直前に不変条件を検査する」等を挟めるようにするため。
        /// </summary>
        public Action Peek(GameFlowController flow)
        {
            if (IsOpen(_bossDialog != null ? _bossDialog.gameObject : null)
                && _bossDialog != null && _bossDialog.IsVisible)
            {
                return Action.DismissBoss;
            }

            if (_relicDraft != null && _relicDraft.IsVisible)
            {
                return Action.SelectRelic;
            }

            if (_eventDialog != null && _eventDialog.IsPanelActive)
            {
                return Action.DismissEvent;
            }

            if (_metaShop != null && _metaShop.IsPanelActive)
            {
                return Action.CloseMetaShop;
            }

            if (flow != null && flow.CurrentPhase == GamePhase.WaitingInput)
            {
                return Action.ClickCommand;
            }

            return Action.None;
        }

        /// <summary>Peek が返した 1 手を実際に行う。行った内容を返す。</summary>
        public Action Step(GameFlowController flow)
        {
            Action action = Peek(flow);
            LastClickAccepted = true;

            switch (action)
            {
                case Action.DismissBoss:
                    BossCount++;
                    LastAction = "DismissBossDialog";
                    LastClickAccepted = Click(_bossDismiss);
                    break;

                case Action.SelectRelic:
                    DraftCount++;
                    LastAction = "SelectRelicCard";
                    LastClickAccepted = Click(FirstRelicSelectButton());
                    break;

                case Action.DismissEvent:
                    EventCount++;
                    LastAction = "DismissEventDialog";
                    LastClickAccepted = Click(_eventOk);
                    break;

                case Action.CloseMetaShop:
                    ShopCount++;
                    LastAction = "CloseMetaShop";
                    LastClickAccepted = Click(_shopClose);
                    break;

                case Action.ClickCommand:
                    Button chosen = CommandPolicy != null ? CommandPolicy(flow) : ChooseCommand(flow);
                    CommandCount++;
                    LastAction = $"ClickCommand_{(chosen != null ? chosen.gameObject.name : "null")}";
                    LastClickAccepted = Click(chosen);
                    break;
            }

            return action;
        }

        /// <summary>
        /// 何が起きているか分からないまま落ちるのを避けるための、状況の 1 行。
        /// タイムアウト時のメッセージに使う。
        /// </summary>
        public string Describe(GameFlowController flow)
        {
            string phase = flow != null ? flow.CurrentPhase.ToString() : "?";
            string turn = flow != null && flow.CurrentState != null
                ? flow.CurrentState.CurrentTurn.ToString()
                : "?";

            return $"Phase={phase}, Turn={turn}, 次の手={Peek(flow)}, 直前={LastAction}, " +
                   $"ボス={BossCount}, ドラフト={DraftCount}, イベント={EventCount}, " +
                   $"ショップ={ShopCount}, コマンド={CommandCount}";
        }

        public Button ChooseCommand(GameFlowController flow)
        {
            if (flow == null || flow.CurrentState == null)
            {
                return _rest;
            }

            int turn = flow.CurrentState.CurrentTurn;
            bool beforeBoss = turn == 5 || turn == 11 || turn == 17 || turn == 23;

            if (beforeBoss && flow.CurrentState.Stamina < 70)
            {
                return _rest;
            }

            if (flow.CurrentState.Stamina <= 50)
            {
                return _rest;
            }

            if (flow.CurrentState.Mental <= 35)
            {
                return _study;
            }

            return _train;
        }

        private Button FirstRelicSelectButton()
        {
            if (_relicCards == null || _relicCards.Count == 0)
            {
                return null;
            }

            return Field<Button>(_relicCards[0], "_selectButton");
        }

        private static bool IsOpen(GameObject go)
        {
            return go != null && go.activeInHierarchy;
        }

        /// <summary>
        /// GraphicRaycaster を通して指定ボタン（またはその子要素）がクリック可能位置の最前面にあり、
        /// かつ interactable であるかを検証する。
        /// 他のモーダルパネル（透明背景など）が上に被さっている場合、検知してテストを失敗させる。
        /// </summary>
        public static void AssertRaycastReachesButton(Button button, string buttonName = null)
        {
            string name = buttonName ?? (button != null ? button.gameObject.name : "null");
            Assert.IsTrue(button != null, $"{name} is null.");
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"{name} is not active in hierarchy.");
            Assert.IsTrue(button.interactable, $"{name} is not interactable.");

            Canvas canvas = button.GetComponentInParent<Canvas>();
            Assert.IsTrue(canvas != null, $"Canvas not found for {name}.");
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            Assert.IsTrue(raycaster != null, $"GraphicRaycaster not found on Canvas for {name}.");

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, button.transform.position);

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPoint
            };

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            Assert.IsTrue(results.Count > 0, $"Raycast hit nothing at {name} screen position {screenPoint}.");

            GameObject topHit = results[0].gameObject;
            bool isTargetOrChild = topHit == button.gameObject || topHit.transform.IsChildOf(button.transform);
            Assert.IsTrue(
                isTargetOrChild,
                $"Raycast to '{name}' was blocked by '{topHit.name}' (Parent: {topHit.transform.parent?.name}). Target button: {button.gameObject.name}");
        }

        private static bool Click(Button button)
        {
            if (button == null)
            {
                return false;
            }

            AssertRaycastReachesButton(button);

            return ExecuteEvents.Execute(
                button.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
        }

        private static Button CommandButton(string gameObjectName)
        {
            foreach (CommandButtonView view in
                     UnityEngine.Object.FindObjectsByType<CommandButtonView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view != null && view.gameObject.name == gameObjectName)
                {
                    return view.GetComponent<Button>();
                }
            }

            GameObject go = GameObject.Find(gameObjectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static T Field<T>(Component target, string fieldName) where T : class
        {
            if (target == null)
            {
                return null;
            }

            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(fieldName, Flags);
                if (f != null)
                {
                    return f.GetValue(target) as T;
                }
            }

            return null;
        }
    }
}
