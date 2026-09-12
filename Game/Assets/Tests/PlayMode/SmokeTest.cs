// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.GameFlow;
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
    /// Gate 4 の曳光弾（tracer bullet）。MainGame シーンを実際にロードし、実際の uGUI
    /// クリック経路で STUDY コマンドを 1 回実行して、TURN 1 → 2 の進行・ゲージ
    /// (BarFill) の変動・例外 0 件を 1 本で証明する。
    ///
    /// このテストが赤になっても、緑にするためにプロダクションコードへ分岐・自己修復・
    /// Find 系の再導入を行ってはならない（development-rules.md「テスト」節）。出力をそのまま
    /// 貼って停止し、人間に報告すること。
    ///
    /// ゲージは Slider ではなく RectTransform.anchorMax.x で表現されている
    /// （StatusView._staminaGauge 等はシーン上で未アサインであり、Slider は存在しない）。
    /// </summary>
    public class SmokeTest
    {
        private const string SceneName = "MainGame";
        private const string StudyButtonName = "StudyButton";
        private const float TimeoutSeconds = 10f;
        private const float FillTolerance = 0.001f;

        private readonly List<string> _capturedFailures = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _capturedFailures.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _capturedFailures.Add($"[{type}] {condition}\n{stackTrace}");
            }
        }

        [UnityTest]
        public IEnumerator MainGame_StudyButtonClick_AdvancesTurnFrom1To2_WithZeroExceptions()
        {
            LogAssert.Expect(LogType.Log, "[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50");
            LogAssert.Expect(LogType.Log, "[CommandButtonView] Clicked button for command: Study");
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"^\[SmokeTest\] BEFORE.*"));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"^\[SmokeTest\] AFTER.*"));

            // ---------- Arrange: MainGame をロードし、入力待ちまで進める ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(
                load != null,
                $"シーン '{SceneName}' のロードを開始できなかった。EditorBuildSettings の Scene List を確認せよ。");

            while (!load.isDone)
            {
                yield return null;
            }

            // Awake / OnEnable / Start を確実に 1 巡させる
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            StatusView status = UnityEngine.Object.FindFirstObjectByType<StatusView>();
            Assert.IsTrue(status != null, "StatusView が MainGame シーンに存在しない。");

            yield return WaitForCondition(
                () => flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"起動から {TimeoutSeconds} 秒以内に GamePhase.WaitingInput へ到達しなかった。"
                      + $" 現在の Phase = {flow.CurrentPhase}");

            GameState before = flow.CurrentState;
            Assert.IsTrue(before != null, "StartGame 後に GameFlowController.CurrentState が null。");
            Assert.AreEqual(1, before.CurrentTurn, "初期ターンが 1 ではない。");
            Assert.IsTrue(
                status.LastDisplayedState != null,
                "StatusView が初期 GameState を受信していない。GameStateChannel の購読経路が切れている。");

            RectTransform staminaBar = GetSerializedField<RectTransform>(status, "_staminaBarFill");
            RectTransform skillBar = GetSerializedField<RectTransform>(status, "_skillBarFill");
            RectTransform mentalBar = GetSerializedField<RectTransform>(status, "_mentalBarFill");
            TextMeshProUGUI turnText = GetSerializedField<TextMeshProUGUI>(status, "_turnText");

            float staminaFillBefore = staminaBar.anchorMax.x;
            float skillFillBefore = skillBar.anchorMax.x;
            float mentalFillBefore = mentalBar.anchorMax.x;

            CommandButtonView studyView = FindCommandButtonByName(StudyButtonName);
            Button studyButton = studyView.GetComponent<Button>();
            Assert.IsTrue(studyButton != null, $"'{StudyButtonName}' に Button コンポーネントが無い。");
            Assert.IsTrue(studyButton.IsInteractable(), $"'{StudyButtonName}' が interactable でない。");

            Debug.Log(
                $"[SmokeTest] BEFORE Turn={before.CurrentTurn} Stamina={before.Stamina}"
                + $" Skill={before.Skill} Mental={before.Mental}"
                + $" Fill(Sta/Skl/Mnt)={staminaFillBefore:F3}/{skillFillBefore:F3}/{mentalFillBefore:F3}"
                + $" TurnText=\"{turnText.text}\" Phase={flow.CurrentPhase}");

            // ---------- Act: 実際の uGUI クリック経路をエミュレートする ----------
            bool accepted = ExecuteEvents.Execute(
                studyButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(accepted, $"'{StudyButtonName}' が pointerClick を受理しなかった。");

            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn >= 2,
                () => $"クリックから {TimeoutSeconds} 秒以内に TURN が 2 へ進まなかった。"
                      + $" 現在の Turn = {(flow.CurrentState != null ? flow.CurrentState.CurrentTurn : -1)},"
                      + $" Phase = {flow.CurrentPhase}");

            // UI 反映を確定させるため 1 フレーム待つ
            yield return null;

            // ---------- Assert ----------
            GameState after = flow.CurrentState;
            GameState displayed = status.LastDisplayedState;

            float staminaFillAfter = staminaBar.anchorMax.x;
            float skillFillAfter = skillBar.anchorMax.x;
            float mentalFillAfter = mentalBar.anchorMax.x;

            Debug.Log(
                $"[SmokeTest] AFTER  Turn={after.CurrentTurn} Stamina={after.Stamina}"
                + $" Skill={after.Skill} Mental={after.Mental}"
                + $" Fill(Sta/Skl/Mnt)={staminaFillAfter:F3}/{skillFillAfter:F3}/{mentalFillAfter:F3}"
                + $" TurnText=\"{turnText.text}\" Phase={flow.CurrentPhase}");

            // 1. ターン進行
            Assert.AreEqual(2, after.CurrentTurn, "TURN が 2 になっていない。");
            Assert.AreEqual(
                GamePhase.WaitingInput, flow.CurrentPhase, "TURN 2 開始後に入力待ちへ戻っていない。");

            // 2. STUDY の効果（Stamina 100→90 / Skill 0→5 / Mental 50→55）
            Assert.AreEqual(90, after.Stamina, "Stamina が 90 でない。");
            Assert.AreEqual(5, after.Skill, "Skill が 5 でない。");
            Assert.AreEqual(55, after.Mental, "Mental が 55 でない。");

            // 3. GameStateChannel 経由で View まで届いていること
            Assert.IsTrue(displayed != null, "StatusView が GameState を受信していない。");
            Assert.AreEqual(2, displayed.CurrentTurn, "StatusView が受信した Turn が 2 でない。");
            Assert.AreEqual(90, displayed.Stamina, "StatusView が受信した Stamina が 90 でない。");
            Assert.AreEqual(5, displayed.Skill, "StatusView が受信した Skill が 5 でない。");
            Assert.AreEqual(55, displayed.Mental, "StatusView が受信した Mental が 55 でない。");

            // 4. ゲージ（BarFill の anchorMax.x）が state に追随して変動したこと
            Assert.AreEqual(0.90f, staminaFillAfter, FillTolerance, "Stamina ゲージが 0.90 でない。");
            Assert.AreEqual(0.05f, skillFillAfter, FillTolerance, "Skill ゲージが 0.05 でない。");
            Assert.AreEqual(0.55f, mentalFillAfter, FillTolerance, "Mental ゲージが 0.55 でない。");
            Assert.Less(staminaFillAfter, staminaFillBefore, "Stamina ゲージが減っていない。");
            Assert.Greater(skillFillAfter, skillFillBefore, "Skill ゲージが増えていない。");
            Assert.Greater(mentalFillAfter, mentalFillBefore, "Mental ゲージが増えていない。");

            // 5. 画面ラベル
            Assert.AreEqual("TURN 2 / 24", turnText.text, "TurnText の表示が更新されていない。");

            // 6. 例外 0 件
            Assert.IsEmpty(
                _capturedFailures,
                "実行中に Error / Exception / Assert ログが発生した:\n"
                + string.Join("\n", _capturedFailures));

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MainGame_AdvanceToTurn6_BossBattleDismiss_AdvancesToTurn7_WithZeroExceptions()
        {
            // ---------- Arrange: MainGame をロード ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"シーン '{SceneName}' のロードを開始できなかった。");
            while (!load.isDone) yield return null;
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            BossBattleDialogView bossDialog = UnityEngine.Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(bossDialog != null, "BossBattleDialogView が MainGame シーンに存在しない。");

            CommandButtonView studyView = FindCommandButtonByName(StudyButtonName);
            Button studyButton = studyView.GetComponent<Button>();

            // ターン 1 から 5 まで Study コマンドを連続実行してターン 6（ボス戦）へ進める
            for (int t = 1; t <= 5; t++)
            {
                int currentTurn = t;
                yield return WaitForCondition(
                    () => flow.CurrentPhase == GamePhase.WaitingInput && flow.CurrentState.CurrentTurn == currentTurn,
                    () => $"ターン {currentTurn} の WaitingInput に到達しなかった。Phase={flow.CurrentPhase}");

                bool clicked = ExecuteEvents.Execute(
                    studyButton.gameObject,
                    new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
                Assert.IsTrue(clicked, $"ターン {currentTurn} で '{StudyButtonName}' のクリックが受理されなかった。");

                yield return null;
            }

            // ---------- Act: ターン 6（ボス戦）への突入とダイアログ確認 ----------
            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn == 6,
                () => $"ターン 6 に進まなかった。CurrentTurn={flow.CurrentState?.CurrentTurn}, Phase={flow.CurrentPhase}");

            yield return WaitForCondition(
                () => bossDialog.IsVisible,
                () => $"ターン 6 到達後に BossBattleDialogView が表示されなかった。Phase={flow.CurrentPhase}");

            Button dismissButton = GetSerializedField<Button>(bossDialog, "_dismissButton");
            Assert.IsTrue(dismissButton != null, "BossBattleDialogView._dismissButton が null。");
            Assert.IsTrue(dismissButton.IsInteractable(), "BossBattleDialogView._dismissButton が interactable でない。");

            // ボス戦ダイアログの決定（Dismiss）ボタンをクリック
            AssertRaycastReachesButton(dismissButton, "dismissButton");
            bool dismissed = ExecuteEvents.Execute(
                dismissButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(dismissed, "BossBattleDialogView の dismissButton クリックが受理されなかった。");

            // ボス勝利後、レリックドラフト画面が表示されることを確認
            RelicDraftDialogView relicDraftDialog = UnityEngine.Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(relicDraftDialog != null, "RelicDraftDialogView がシーンに見つからない。");
            yield return WaitForCondition(
                () => relicDraftDialog.IsVisible,
                () => $"ボス戦ダイアログ決定後に RelicDraftDialogView が表示されなかった。Phase={flow.CurrentPhase}");

            // カード1を選択してドラフトを完了する
            List<RelicCardView> cards = GetField<List<RelicCardView>>(relicDraftDialog, "_cardViews");
            Assert.IsTrue(cards != null && cards.Count > 0, "RelicDraftDialogView._cardViews が空。");
            Button selectButton = GetSerializedField<Button>(cards[0], "_selectButton");
            Assert.IsTrue(selectButton != null, "RelicCardView._selectButton が null。");

            AssertRaycastReachesButton(selectButton, "relicCard[0]._selectButton");
            bool cardClicked = ExecuteEvents.Execute(
                selectButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(cardClicked, "RelicCardView の SelectButton クリックが受理されなかった。");

            // ドラフトが閉じ、WaitingInput へ復帰することを確認
            yield return WaitForCondition(
                () => !relicDraftDialog.IsVisible && flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"レリックドラフト選択後に入力待ちへ復帰しなかった。IsVisible={relicDraftDialog.IsVisible}, Phase={flow.CurrentPhase}");

            // ---------- ターン 6 のコマンドを実行してターン 7 へ進める ----------
            bool turn6Clicked = ExecuteEvents.Execute(
                studyButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(turn6Clicked, "ターン 6 でのコマンドクリックが受理されなかった。");

            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn == 7,
                () => $"ターン 7 へ進まなかった。CurrentTurn={flow.CurrentState?.CurrentTurn}, Phase={flow.CurrentPhase}");

            yield return WaitForCondition(
                () => flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"ターン 7 開始後に入力待ちへ戻らなかった。Phase={flow.CurrentPhase}");

            // ---------- Assert: 例外 0 件とターン 7 到達 ----------
            Assert.AreEqual(7, flow.CurrentState.CurrentTurn, "ターンが 7 になっていない。");
            Assert.IsEmpty(
                _capturedFailures,
                "ターン 1〜7 進行中に Error / Exception / Assert ログが発生した:\n"
                + string.Join("\n", _capturedFailures));
        }

        [UnityTest]
        public IEnumerator MainGame_AdvanceToTurn24_AllBossesDefeated_ShowsEndingPanel_WithZeroExceptions()
        {
            // ---------- Arrange: MainGame をロード ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"シーン '{SceneName}' のロードを開始できなかった。");
            while (!load.isDone) yield return null;
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            BossBattleDialogView bossDialog = UnityEngine.Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(bossDialog != null, "BossBattleDialogView が MainGame シーンに存在しない。");

            RelicDraftDialogView relicDraftDialog = UnityEngine.Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(relicDraftDialog != null, "RelicDraftDialogView が MainGame シーンに存在しない。");

            EndingView endingView = UnityEngine.Object.FindFirstObjectByType<EndingView>(FindObjectsInactive.Include);
            Assert.IsTrue(endingView != null, "EndingView が MainGame シーンに存在しない。");

            MetaShopDialogView metaShopDialog = UnityEngine.Object.FindFirstObjectByType<MetaShopDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(metaShopDialog != null, "MetaShopDialogView が MainGame シーンに存在しない。");

            StatusView statusView = UnityEngine.Object.FindFirstObjectByType<StatusView>(FindObjectsInactive.Include);
            Assert.IsTrue(statusView != null, "StatusView が MainGame シーンに存在しない。");

            Button studyButton = FindCommandButtonByName(StudyButtonName).GetComponent<Button>();


            // 初期状態では EndingPanel と RelicDraftDialogPanel, MetaShopDialogPanel は非アクティブ
            Assert.IsFalse(endingView.IsPanelActive, "初期状態で EndingView がアクティブになっている。");
            Assert.IsFalse(relicDraftDialog.IsVisible, "初期状態で RelicDraftDialogView がアクティブになっている。");
            Assert.IsFalse(metaShopDialog.IsPanelActive, "初期状態で MetaShopDialogView がアクティブになっている。");

            // ---------- Act: ターン 1 から 24 まで進行 ----------
            // クリックの運転は RunDriver に一本化している。ダイアログが増えたら
            // RunDriver へ 1 か所足す。以前は各テストが独自のクリックループを持ち、
            // 互いに相手の知らないダイアログを 1 つずつ持っていた。
            RunDriver driver = RunDriver.FromScene();
            float maxTime = Time.realtimeSinceStartup + 30f;

            while (!endingView.IsPanelActive && flow.CurrentPhase != GamePhase.GameOver)
            {
                if (Time.realtimeSinceStartup > maxTime)
                {
                    Assert.Fail($"30 秒以内にエンディング画面へ到達しなかった。{driver.Describe(flow)}");
                }

                if (driver.Peek(flow) == RunDriver.Action.DismissBoss)
                {
                    AssertRaycastReachesButton(driver.BossDismissButton, "bossDismissButton");
                }

                RunDriver.Action taken = driver.Step(flow);
                if (taken != RunDriver.Action.None)
                {
                    Assert.IsTrue(driver.LastClickAccepted,
                        $"クリックが受理されなかった（{taken}）。{driver.Describe(flow)}");
                }

                yield return null;
            }

            int bossCount = driver.BossCount;
            int draftCount = driver.DraftCount;

            // UI 反映を確定させるため 1 フレーム待つ
            yield return null;

            // ---------- Assert: ターン 24 最終ボス撃破後のエンディング画面表示と例外ゼロ ----------
            Assert.AreEqual(GamePhase.GameClear, flow.CurrentPhase, "GamePhase が GameClear に到達していない。");
            Assert.AreEqual(4, bossCount, "4 回のボス戦ダイアログが表示されていない。");
            Assert.AreEqual(3, draftCount, "Act 1〜3 の 3 回のレリックドラフトが表示されていない。");

            Assert.IsTrue(endingView.IsPanelActive, "ゲームクリア後に EndingView.IsPanelActive が true になっていない。");
            Assert.IsFalse(string.IsNullOrEmpty(endingView.DisplayedResult), "EndingView.DisplayedResult が空文字列。");

            // ---------- Act: エンディング画面の RestartButton をクリックしてショップ提示と周回ループを検証 ----------
            int pointsBeforeRestart = flow.MetaProfile.AvailableMetaPoints;
            Assert.IsTrue(pointsBeforeRestart > 0, $"クリア時点の AvailableMetaPoints が 0 以下: {pointsBeforeRestart}");

            Button restartButton = GetSerializedField<Button>(endingView, "_restartButton");
            Assert.IsTrue(restartButton != null, "EndingView._restartButton が null。");
            AssertRaycastReachesButton(restartButton, "EndingView.RestartButton");

            bool restartClicked = ExecuteEvents.Execute(
                restartButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(restartClicked, "EndingView の RestartButton クリックが受理されなかった。");

            yield return WaitForCondition(
                () => metaShopDialog.IsPanelActive,
                () => $"RESTART クリック後に MetaShopDialogView.IsPanelActive が true にならなかった。");

            Button shopCloseButton = GetSerializedField<Button>(metaShopDialog, "_closeButton");
            Assert.IsTrue(shopCloseButton != null, "MetaShopDialogView._closeButton が null。");
            AssertRaycastReachesButton(shopCloseButton, "MetaShopDialogView.CloseShopButton");

            bool shopClosed = ExecuteEvents.Execute(
                shopCloseButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(shopClosed, "MetaShopDialogView の CloseShopButton クリックが受理されなかった。");

            yield return WaitForCondition(
                () => !endingView.IsPanelActive && !metaShopDialog.IsPanelActive && flow.CurrentPhase == GamePhase.WaitingInput && flow.CurrentState.CurrentTurn == 1,
                () => $"ショップ終了後にエンディング画面が閉じてターン1のWaitingInputへ復帰しなかった。EndingActive={endingView.IsPanelActive}, ShopActive={metaShopDialog.IsPanelActive}, Phase={flow.CurrentPhase}, Turn={flow.CurrentState?.CurrentTurn}");

            Assert.IsFalse(endingView.IsPanelActive, "リスタート後に EndingView.IsPanelActive が false になっていない。");
            Assert.IsFalse(metaShopDialog.IsPanelActive, "リスタート後に MetaShopDialogView.IsPanelActive が false になっていない。");
            Assert.AreEqual(1, flow.CurrentState.CurrentTurn, "リスタート後に Turn が 1 に戻っていない。");
            Assert.AreEqual(GamePhase.WaitingInput, flow.CurrentPhase, "リスタート後に入力待ちへ戻っていない。");

            Assert.AreEqual(pointsBeforeRestart, flow.MetaProfile.AvailableMetaPoints, "リスタート後に AvailableMetaPoints が持ち越されていない。");
            Assert.AreEqual(1, flow.MetaProfile.TotalRunsCompleted, "リスタート後に TotalRunsCompleted が 1 になっていない。");

            Assert.IsTrue(statusView.LastDisplayedProfile != null, "リスタート後に StatusView.LastDisplayedProfile が null。");
            Assert.AreEqual(pointsBeforeRestart, statusView.LastDisplayedProfile.AvailableMetaPoints, "HUD の LastDisplayedProfile.AvailableMetaPoints が一致しない。");

            Assert.IsEmpty(
                _capturedFailures,
                "24 ターン進行およびリスタート中に Error / Exception / Assert ログが発生した:\n"
                + string.Join("\n", _capturedFailures));
        }


        /// <summary>
        /// predicate が真になるまで毎フレーム待つ。TimeoutSeconds を超えたら失敗する。
        /// </summary>
        private static IEnumerator WaitForCondition(Func<bool> predicate, Func<string> timeoutMessage)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;

            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(timeoutMessage());
                }

                yield return null;
            }
        }

        /// <summary>
        /// GameObject 名で CommandButtonView を引く。テストコードでのシーン検索は
        /// development-rules.md の禁止対象（ランタイムコード）ではない。
        /// </summary>
        private static CommandButtonView FindCommandButtonByName(string gameObjectName)
        {
            CommandButtonView[] views = UnityEngine.Object.FindObjectsByType<CommandButtonView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (CommandButtonView view in views)
            {
                if (view.gameObject.name == gameObjectName)
                {
                    return view;
                }
            }

            string found = views.Length == 0
                ? "(なし)"
                : string.Join(", ", Array.ConvertAll(views, v => v.gameObject.name));
            Assert.Fail(
                $"CommandButtonView を持つ GameObject '{gameObjectName}' がシーンに見つからない。検出できたのは: {found}");
            return null;
        }

        /// <summary>
        /// [SerializeField] private フィールドの実体を読む。人間の Inspector アサインが
        /// 落ちている場合に、原因を名指しで失敗させるために使う。
        /// </summary>
        private static T GetSerializedField<T>(Component target, string fieldName) where T : UnityEngine.Object
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(
                field != null, $"{target.GetType().Name}.{fieldName} というフィールドが存在しない。");

            T value = field.GetValue(target) as T;
            UnityEngine.Object asObject = value;
            Assert.IsTrue(
                asObject != null,
                $"{target.GetType().Name}.{fieldName} が Inspector で未アサイン、または型が {typeof(T).Name} でない。"
                + " 人間のアサインを確認せよ。");

            return value;
        }

        /// <summary>
        /// [SerializeField] private フィールド（非 UnityEngine.Object 型）の実体を読む。
        /// </summary>
        private static T GetField<T>(Component target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(
                field != null, $"{target.GetType().Name}.{fieldName} というフィールドが存在しない。");

            object value = field.GetValue(target);
            Assert.IsTrue(
                value != null,
                $"{target.GetType().Name}.{fieldName} が null。");

            return (T)value;
        }

        /// <summary>
        /// 対照群（u-0028）: 対象ボタンの上に一時的な遮蔽物を置くと AssertRaycastReachesButton / RunDriver が
        /// 確実に失敗（Blocked）し、遮蔽物を外すと正常に通ることを実証する。
        /// 置いた遮蔽物は必ず同じテスト内で破棄して後片付けする。
        /// </summary>
        [UnityTest]
        public IEnumerator MainGame_CommandButtonClick_BlockedByOverlay_AssertFailsAndRecovers()
        {
            Debug.Log("[ContrastTest] Starting test");

            // ---------- Arrange: MainGame をロードし、入力待ちまで進める ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"シーン '{SceneName}' のロードを開始できなかった。");

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            yield return WaitForCondition(
                () => flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"起動から {TimeoutSeconds} 秒以内に GamePhase.WaitingInput へ到達しなかった。");

            Debug.Log("[ContrastTest] Scene loaded, WaitingInput reached");

            Button studyButton = GameObject.Find(StudyButtonName)?.GetComponent<Button>();
            Assert.IsTrue(studyButton != null, $"{StudyButtonName} が見つからない。");

            RunDriver driver = RunDriver.FromScene();
            driver.CommandPolicy = _ => studyButton;

            // 1. 遮蔽物がない通常状態では届くことを確認
            AssertRaycastReachesButton(studyButton, StudyButtonName);
            Debug.Log("[ContrastTest] Step 1 passed (reachable without obstruction)");

            // 2. button.interactable = false のときに落ちることを実証
            studyButton.interactable = false;
            bool interactableThrew = false;
            try
            {
                AssertRaycastReachesButton(studyButton, StudyButtonName);
            }
            catch (AssertionException ex)
            {
                interactableThrew = true;
                Assert.IsTrue(ex.Message.Contains("not interactable"), $"想定と異なるエラーメッセージ: {ex.Message}");
            }
            finally
            {
                studyButton.interactable = true;
            }
            Assert.IsTrue(interactableThrew, "interactable = false なのに AssertRaycastReachesButton が失敗しなかった。");
            Debug.Log("[ContrastTest] Step 2 passed (interactable=false throws)");

            // 3. 必ず落ちる側の実証: ボタンの前面に一時的な遮蔽物を配置
            Canvas canvas = studyButton.GetComponentInParent<Canvas>();
            Assert.IsTrue(canvas != null, "Canvas not found for studyButton.");

            GameObject obstruction = new GameObject("TemporaryObstruction", typeof(RectTransform));
            try
            {
                obstruction.transform.SetParent(studyButton.transform.parent, false);
                RectTransform rt = obstruction.GetComponent<RectTransform>();
                rt.position = studyButton.transform.position;
                rt.sizeDelta = new Vector2(500f, 500f);
                rt.SetAsLastSibling();

                Image img = obstruction.AddComponent<Image>();
                img.color = Color.red;
                img.raycastTarget = true;

                Canvas.ForceUpdateCanvases();
                yield return null;

                // 直接呼び出しで Blocked になることを検証
                bool directAssertThrew = false;
                try
                {
                    AssertRaycastReachesButton(studyButton, StudyButtonName);
                }
                catch (AssertionException ex)
                {
                    directAssertThrew = true;
                    Assert.IsTrue(
                        ex.Message.Contains("blocked by 'TemporaryObstruction'"),
                        $"想定と異なるエラーメッセージ: {ex.Message}");
                }
                Assert.IsTrue(directAssertThrew, "遮蔽物を配置したにもかかわらず AssertRaycastReachesButton が失敗しなかった。");
                Debug.Log("[ContrastTest] Step 3a passed (direct assert blocked)");

                // RunDriver.Step でも Blocked になることを検証
                bool driverStepThrew = false;
                try
                {
                    driver.Step(flow);
                }
                catch (AssertionException ex)
                {
                    driverStepThrew = true;
                    Assert.IsTrue(
                        ex.Message.Contains("blocked by 'TemporaryObstruction'"),
                        $"想定と異なるエラーメッセージ: {ex.Message}");
                }
                Assert.IsTrue(driverStepThrew, "遮蔽物を配置したにもかかわらず driver.Step が失敗しなかった。");
                Debug.Log("[ContrastTest] Step 3b passed (driver.Step blocked)");
            }
            finally
            {
                // 遮蔽物を必ず破棄する（シーンに残さない）
                if (obstruction != null)
                {
                    UnityEngine.Object.DestroyImmediate(obstruction);
                }
            }

            Canvas.ForceUpdateCanvases();
            yield return null;

            // 4. 遮蔽物がシーンに残っていないことを確認
            Assert.IsNull(GameObject.Find("TemporaryObstruction"), "遮蔽物がシーンに残っている。");
            Debug.Log("[ContrastTest] Step 4 passed (obstruction destroyed)");

            // 5. 遮蔽物除去後は正常に driver.Step でクリックが届き、ターン進行できることを確認
            RunDriver.Action taken = driver.Step(flow);
            Assert.AreEqual(RunDriver.Action.ClickCommand, taken);
            Assert.IsTrue(driver.LastClickAccepted, "遮蔽物除去後の driver.Step クリックが受理されなかった。");
            Debug.Log("[ContrastTest] Step 5 passed (driver.Step succeeded)");

            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn == 2 && flow.CurrentPhase == GamePhase.WaitingInput,
                () => "クリック後に Turn 2 / WaitingInput へ遷移しなかった。");

            Assert.AreEqual(2, flow.CurrentState.CurrentTurn);
            Debug.Log("[ContrastTest] Step 6 passed (Turn 2 reached)");
        }

        /// <summary>
        /// GraphicRaycaster を通して指定ボタン（またはその子要素）がクリック可能位置の最前面にあるかを検証する。
        /// 実体は RunDriver.AssertRaycastReachesButton に集約。
        /// </summary>
        private static void AssertRaycastReachesButton(Button button, string buttonName)
        {
            RunDriver.AssertRaycastReachesButton(button, buttonName);
        }
    }
}
