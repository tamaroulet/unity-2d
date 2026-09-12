// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI パネルを実行時にコードから動的生成し、View に依存を注入するランタイムブートストラッパー。
    /// ADR 0002 (Code-first Bootstrap) に基づき、シーンへのキャッシュをやめて実行時に構築する。
    /// </summary>
    public class UiBootstrapper : MonoBehaviour
    {
        private static readonly Color ColorBgDialog = new Color(0.14f, 0.17f, 0.24f, 0.98f);
        private static readonly Color ColorOverlay = new Color(0.0f, 0.0f, 0.0f, 0.80f);
        private static readonly Color ColorModalButtonBg = new Color(0.25f, 0.45f, 0.85f, 1.0f);
        private static readonly Color ColorBgHeader = new Color(0.12f, 0.15f, 0.20f, 0.98f);
        private static readonly Color ColorBgFooter = new Color(0.10f, 0.12f, 0.16f, 0.98f);
        private static readonly Color ColorButtonBg = new Color(0.18f, 0.22f, 0.30f, 1.0f);
        private static readonly Color ColorBarBg = new Color(0.18f, 0.20f, 0.26f, 1.0f);
        private static readonly Color ColorStamina = new Color(0.22f, 0.85f, 0.45f, 1.0f);
        private static readonly Color ColorSkill = new Color(0.25f, 0.65f, 0.98f, 1.0f);
        private static readonly Color ColorMental = new Color(0.92f, 0.35f, 0.65f, 1.0f);
        private static readonly Color ColorBgBossDialog = new Color(0.20f, 0.10f, 0.12f, 0.98f);
        private static readonly Color ColorBossHp = new Color(0.92f, 0.25f, 0.25f, 1.0f);
        private static readonly Color ColorShield = new Color(0.30f, 0.75f, 0.95f, 1.0f);

        private Canvas _canvas;
        private EndingView _endingView;
        private GameFlowController _gameFlowController;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }
            if (_gameFlowController == null)
            {
                _gameFlowController = Object.FindFirstObjectByType<GameFlowController>();
            }

            BootstrapBackground();
            BootstrapUIViews();
            BootstrapBossBattleDialogPanel();
            BootstrapEventDialogPanel();
            BootstrapRelicDraftDialogPanel();
            BootstrapMetaShopDialogPanel();
            BootstrapStatusPanel();
            BootstrapCommandPanel();
            BootstrapEndingPanel();
        }

        private void BootstrapBackground()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_canvas.transform, false);
            bgGo.transform.SetAsFirstSibling();

            RectTransform rect = bgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = bgGo.GetComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.12f, 1.0f);
            img.raycastTarget = false;
        }

        private void BootstrapUIViews()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            GameObject viewsGo = new GameObject("UIViews", typeof(RectTransform));
            viewsGo.transform.SetParent(_canvas.transform, false);
            viewsGo.SetActive(true);

            _endingView = viewsGo.AddComponent<EndingView>();
            viewsGo.AddComponent<BossBattleDialogView>();
            viewsGo.AddComponent<EventDialogView>();
            viewsGo.AddComponent<RelicDraftDialogView>();
            viewsGo.AddComponent<MetaShopDialogView>();
        }

        private void BootstrapStatusPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            GameStateEventChannelSO gameStateChannel = Resources.Load<GameStateEventChannelSO>("GameStateChannel");
            if (gameStateChannel == null)
            {
                Debug.LogError("[UiBootstrapper] Failed to load GameStateEventChannelSO from Resources.");
            }

            // StatusPanel (ヘッダー枠)
            GameObject panelGo = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image), typeof(StatusView));
            panelGo.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.82f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(20f, -10f);
            panelRect.offsetMax = new Vector2(-20f, -10f);

            Image panelImg = panelGo.GetComponent<Image>();
            panelImg.color = ColorBgHeader;

            StatusView statusView = panelGo.GetComponent<StatusView>();

            // ゲージ3本
            CreateGaugeGroup(
                panelRect, "StaminaGroup", ColorStamina, new Vector2(-400, 0), "Stamina: 50 / 100",
                out Slider staminaGauge, out RectTransform staminaBarFill, out TextMeshProUGUI staminaText);

            CreateGaugeGroup(
                panelRect, "SkillGroup", ColorSkill, new Vector2(0, 0), "Skill: 10",
                out Slider skillGauge, out RectTransform skillBarFill, out TextMeshProUGUI skillText);

            CreateGaugeGroup(
                panelRect, "MentalGroup", ColorMental, new Vector2(400, 0), "Mental: 80 / 100",
                out Slider mentalGauge, out RectTransform mentalBarFill, out TextMeshProUGUI mentalText);

            // テキストラベル
            TextMeshProUGUI turnText = CreateLabel(
                panelRect, "TurnText", "TURN 1 / 24", new Vector2(-750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Left);

            TextMeshProUGUI pointsText = CreateLabel(
                panelRect, "PointsText", "POINTS: 0", new Vector2(750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Right);

            BossBattleDialogView bossBattleDialog = _canvas.GetComponentInChildren<BossBattleDialogView>(true);

            // StatusView への直接代入（Bind）
            statusView.Bind(
                gameStateChannel,
                _gameFlowController,
                bossBattleDialog,
                turnText,
                staminaText,
                skillText,
                mentalText,
                pointsText,
                staminaGauge,
                skillGauge,
                mentalGauge,
                staminaBarFill,
                skillBarFill,
                mentalBarFill);
        }

        private static void CreateGaugeGroup(
            RectTransform parent,
            string name,
            Color barColor,
            Vector2 pos,
            string label,
            out Slider slider,
            out RectTransform barFillRect,
            out TextMeshProUGUI labelTmp)
        {
            GameObject groupGo = new GameObject(name, typeof(RectTransform));
            groupGo.transform.SetParent(parent, false);

            RectTransform groupRect = groupGo.GetComponent<RectTransform>();
            groupRect.anchoredPosition = pos;
            groupRect.sizeDelta = new Vector2(320, 60);

            // BarBg (Slider コンポーネントを担う)
            GameObject barBgGo = new GameObject("BarBg", typeof(RectTransform), typeof(Image), typeof(Slider));
            barBgGo.transform.SetParent(groupRect, false);

            RectTransform barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(20, -5);
            barBgRect.sizeDelta = new Vector2(220, 24);

            Image barBgImg = barBgGo.GetComponent<Image>();
            barBgImg.color = ColorBarBg;

            // BarFill
            GameObject barFillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFillGo.transform.SetParent(barBgRect, false);

            barFillRect = barFillGo.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0f, 1f);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            barFillRect.sizeDelta = Vector2.zero;

            Image barFillImg = barFillGo.GetComponent<Image>();
            barFillImg.color = barColor;

            // Slider 設定（表示専用・入力不可）
            slider = barBgGo.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.handleRect = null;
            slider.targetGraphic = null;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = false;
            slider.fillRect = barFillRect;

            // Label
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(groupRect, false);

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(20, 15);
            labelRect.sizeDelta = new Vector2(220, 24);

            labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = Color.white;
            labelTmp.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateLabel(
            RectTransform parent,
            string name,
            string text,
            Vector2 pos,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject labelGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            RectTransform rect = labelGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void BootstrapCommandPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            // CommandPanel (フッター枠)
            GameObject panelGo = new GameObject("CommandPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);
            panelGo.transform.SetSiblingIndex(1); // Background の直後、モーダルダイアログ群より手前（奥側）

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0.22f);
            panelRect.offsetMin = new Vector2(20f, 15f);
            panelRect.offsetMax = new Vector2(-20f, 15f);

            Image panelImg = panelGo.GetComponent<Image>();
            panelImg.color = ColorBgFooter;

            if (_gameFlowController == null)
            {
                _gameFlowController = Object.FindFirstObjectByType<GameFlowController>();
            }

            CommandDataSO studyCmd = Resources.Load<CommandDataSO>("Study");
            CommandDataSO trainCmd = Resources.Load<CommandDataSO>("Train");
            CommandDataSO restCmd = Resources.Load<CommandDataSO>("Rest");

            if (studyCmd == null) Debug.LogError("[UiBootstrapper] Failed to load Study CommandDataSO from Resources.");
            if (trainCmd == null) Debug.LogError("[UiBootstrapper] Failed to load Train CommandDataSO from Resources.");
            if (restCmd == null) Debug.LogError("[UiBootstrapper] Failed to load Rest CommandDataSO from Resources.");

            CreateCommandButton(panelRect, "StudyButton", studyCmd, new Vector2(-400, 0), _gameFlowController);
            CreateCommandButton(panelRect, "TrainButton", trainCmd, new Vector2(0, 0), _gameFlowController);
            CreateCommandButton(panelRect, "RestButton", restCmd, new Vector2(400, 0), _gameFlowController);
        }

        private static void CreateCommandButton(
            RectTransform parent,
            string name,
            CommandDataSO command,
            Vector2 pos,
            GameFlowController controller)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CommandButtonView));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 120);

            Image img = btnGo.GetComponent<Image>();
            img.color = ColorButtonBg;
            img.raycastTarget = true;

            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;

            // アイコン枠
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(-80, 0);
            iconRect.sizeDelta = new Vector2(56, 56);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            // テキストラベル（コマンド名）
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(rect, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(40, 18);
            textRect.sizeDelta = new Vector2(180, 50);

            TextMeshProUGUI nameTmp = textGo.GetComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 20;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;

            // コスト表示ラベル
            GameObject costGo = new GameObject("CostText", typeof(RectTransform), typeof(TextMeshProUGUI));
            costGo.transform.SetParent(rect, false);
            RectTransform costRect = costGo.GetComponent<RectTransform>();
            costRect.anchoredPosition = new Vector2(40, -25);
            costRect.sizeDelta = new Vector2(180, 30);

            TextMeshProUGUI costTmp = costGo.GetComponent<TextMeshProUGUI>();
            costTmp.fontSize = 16;
            costTmp.alignment = TextAlignmentOptions.Center;
            costTmp.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            costTmp.raycastTarget = false;

            CommandButtonView btnView = btnGo.GetComponent<CommandButtonView>();
            btnView.Bind(command, controller, btn, nameTmp, costTmp);
        }

        private void BootstrapEndingPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            EndingDecidedChannelSO endingChannel = Resources.Load<EndingDecidedChannelSO>("EndingDecidedChannel");
            if (endingChannel == null)
            {
                Debug.LogError("[UiBootstrapper] Failed to load EndingDecidedChannelSO from Resources.");
            }

            // EndingPanel (全画面モーダルオーバーレイ)
            GameObject panelGo = new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);
            panelGo.transform.SetAsLastSibling();

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image overlayImg = panelGo.GetComponent<Image>();
            overlayImg.color = ColorOverlay;

            // PanelRoot (ダイアログ本体枠)
            GameObject rootGo = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelGo.transform, false);

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(950, 650);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>();
            rootImg.color = ColorBgDialog;

            // EndingTitleText
            GameObject titleGo = new GameObject("EndingTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(rootGo.transform, false);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 220);
            titleRect.sizeDelta = new Vector2(700, 60);
            TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "GAME CLEAR!";
            titleTmp.fontSize = 38;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;

            // EndingDescriptionText
            GameObject descGo = new GameObject("EndingDescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(rootGo.transform, false);
            RectTransform descRect = descGo.GetComponent<RectTransform>();
            descRect.anchoredPosition = new Vector2(0, 60);
            descRect.sizeDelta = new Vector2(700, 150);
            TextMeshProUGUI descTmp = descGo.GetComponent<TextMeshProUGUI>();
            descTmp.text = "You survived all 24 turns and defeated all 4 Act Bosses!\nEarned MetaPoints: +150 Pts";
            descTmp.fontSize = 24;
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.color = Color.white;
            descTmp.raycastTarget = false;

            // RestartButton
            GameObject btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rootGo.transform, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -180);
            btnRect.sizeDelta = new Vector2(450, 70);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorModalButtonBg;

            Button restartButton = btnGo.GetComponent<Button>();

            // RestartButton Text
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(450, 70);
            TextMeshProUGUI btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "RESTART / SHOP";
            btnTmp.fontSize = 22;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            panelGo.SetActive(false);

            // EndingView への依存注入
            if (_endingView != null)
            {
                _endingView.Bind(endingChannel, _gameFlowController, restartButton, panelGo, titleTmp);
            }
            else
            {
                Debug.LogError("[UiBootstrapper] _endingView is null, cannot bind EndingPanel references.");
            }
        }

        private void BootstrapBossBattleDialogPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            // BossBattleDialogPanel (全画面モーダルオーバーレイ)
            GameObject panelGo = new GameObject("BossBattleDialogPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image overlayImg = panelGo.GetComponent<Image>();
            overlayImg.color = ColorOverlay;
            overlayImg.raycastTarget = true;

            // PanelRoot (ダイアログ本体枠)
            GameObject rootGo = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelGo.transform, false);

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1250, 750);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>();
            rootImg.color = ColorBgBossDialog;

            // BossTitleText
            TextMeshProUGUI titleTmp = CreateLabel(
                rootRect, "BossTitleText", "BOSS BATTLE", new Vector2(0, 290), new Vector2(800, 50), 34, TextAlignmentOptions.Center);

            // BossEmblem
            GameObject emblemGo = new GameObject("BossEmblem", typeof(RectTransform), typeof(Image));
            emblemGo.transform.SetParent(rootRect, false);
            RectTransform emblemRect = emblemGo.GetComponent<RectTransform>();
            emblemRect.anchoredPosition = new Vector2(0, 110);
            emblemRect.sizeDelta = new Vector2(200, 200);
            Image emblemImg = emblemGo.GetComponent<Image>();
            emblemImg.color = Color.white;
            emblemImg.raycastTarget = false;

            // BossHpGroup
            CreateGaugeGroup(
                rootRect, "BossHpGroup", ColorBossHp, new Vector2(0, -60), "Boss HP: 80 / 80",
                out Slider bossHpSlider, out RectTransform _, out TextMeshProUGUI bossHpText);

            // BossShieldGroup
            CreateGaugeGroup(
                rootRect, "BossShieldGroup", ColorShield, new Vector2(0, -130), "Shield: 10",
                out Slider _, out RectTransform _, out TextMeshProUGUI shieldText);

            // BattleLogText
            TextMeshProUGUI logTmp = CreateLabel(
                rootRect, "BattleLogText", "", new Vector2(0, -195), new Vector2(800, 50), 16, TextAlignmentOptions.Center);

            // AutoBattleNextButton
            GameObject btnGo = new GameObject("AutoBattleNextButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rootRect, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -260);
            btnRect.sizeDelta = new Vector2(400, 70);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorModalButtonBg;
            btnImg.raycastTarget = true;

            Button dismissButton = btnGo.GetComponent<Button>();
            dismissButton.targetGraphic = btnImg;

            // AutoBattleNextButton / Text
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(400, 70);
            TextMeshProUGUI btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "AUTO BATTLE / NEXT";
            btnTmp.fontSize = 22;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            // 初期状態は非アクティブ
            panelGo.SetActive(false);

            // BossBattleDialogView への依存注入
            BossBattleDialogView bossDialog = _canvas.GetComponentInChildren<BossBattleDialogView>(true)
                ?? Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);

            if (bossDialog != null)
            {
                bossDialog.Bind(panelGo, titleTmp, bossHpText, bossHpSlider, shieldText, logTmp, dismissButton, btnTmp);
            }
            else
            {
                Debug.LogError("[UiBootstrapper] BossBattleDialogView not found, cannot bind references.");
            }
        }

        private void BootstrapEventDialogPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            // EventDialogPanel (全画面モーダルオーバーレイ)
            GameObject panelGo = new GameObject("EventDialogPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image overlayImg = panelGo.GetComponent<Image>();
            overlayImg.color = ColorOverlay;
            overlayImg.raycastTarget = true;

            // PanelRoot (ダイアログ本体枠)
            GameObject rootGo = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelGo.transform, false);

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(850, 520);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>();
            rootImg.color = ColorBgDialog;

            // EventTitleText
            TextMeshProUGUI titleTmp = CreateLabel(
                rootRect, "EventTitleText", "EVENT OCCURRED", new Vector2(0, 160), new Vector2(700, 50), 32, TextAlignmentOptions.Center);

            // EventDescriptionText
            TextMeshProUGUI descTmp = CreateLabel(
                rootRect, "EventDescriptionText", "A training event has occurred.\nChoose your option carefully.", new Vector2(0, 40), new Vector2(700, 140), 22, TextAlignmentOptions.Center);

            // OkButton
            GameObject btnGo = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rootRect, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -120);
            btnRect.sizeDelta = new Vector2(300, 60);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorModalButtonBg;
            btnImg.raycastTarget = true;

            Button okButton = btnGo.GetComponent<Button>();
            okButton.targetGraphic = btnImg;

            // OkButton / Text
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(300, 60);
            TextMeshProUGUI btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "OK";
            btnTmp.fontSize = 22;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            // 初期状態は非アクティブ
            panelGo.SetActive(false);

            // EventDialogView への依存注入
            EventDialogView eventDialog = _canvas.GetComponentInChildren<EventDialogView>(true)
                ?? Object.FindFirstObjectByType<EventDialogView>(FindObjectsInactive.Include);

            if (eventDialog != null)
            {
                GameEventFiredChannelSO eventChannel = Resources.Load<GameEventFiredChannelSO>("EventFiredChannel");
                GameEventCatalogSO eventCatalog = Resources.Load<GameEventCatalogSO>("GameEventCatalog");
                eventDialog.Bind(panelGo, titleTmp, descTmp, okButton, eventChannel, eventCatalog, _gameFlowController);
            }
            else
            {
                Debug.LogError("[UiBootstrapper] EventDialogView not found, cannot bind references.");
            }
        }

        private void BootstrapRelicDraftDialogPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            // RelicDraftDialogPanel (全画面モーダルオーバーレイ)
            GameObject panelGo = new GameObject("RelicDraftDialogPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image overlayImg = panelGo.GetComponent<Image>();
            overlayImg.color = ColorOverlay;
            overlayImg.raycastTarget = true;

            // PanelRoot (ダイアログ本体枠)
            GameObject rootGo = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelGo.transform, false);

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1150, 650);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>();
            rootImg.color = ColorBgDialog;

            // DraftTitleText
            CreateLabel(
                rootRect, "DraftTitleText", "RELIC DRAFT (SELECT PASSIVE)", new Vector2(0, 240), new Vector2(800, 50), 32, TextAlignmentOptions.Center);

            // Card1, Card2, Card3
            List<RelicCardView> cardViews = new List<RelicCardView>
            {
                CreateRelicCard(rootRect, "Card1", "Iron Dumbbell", "+5 Stamina/Turn", new Vector2(-340, -20)),
                CreateRelicCard(rootRect, "Card2", "Book of Wisdom", "+20% Skill Gain", new Vector2(0, -20)),
                CreateRelicCard(rootRect, "Card3", "Healing Amulet", "+30% Mental Guard", new Vector2(340, -20))
            };

            // 初期状態は非アクティブ
            panelGo.SetActive(false);

            // RelicDraftDialogView への依存注入
            RelicDraftDialogView relicDialog = _canvas.GetComponentInChildren<RelicDraftDialogView>(true)
                ?? Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);

            if (relicDialog != null)
            {
                RelicAcquiredChannelSO relicChannel = Resources.Load<RelicAcquiredChannelSO>("RelicAcquiredChannel");
                relicDialog.Bind(panelGo, cardViews, _gameFlowController, relicChannel);
            }
            else
            {
                Debug.LogError("[UiBootstrapper] RelicDraftDialogView not found, cannot bind references.");
            }
        }

        private static RelicCardView CreateRelicCard(
            RectTransform parent,
            string name,
            string relicName,
            string desc,
            Vector2 pos)
        {
            GameObject cardGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RelicCardView));
            cardGo.transform.SetParent(parent, false);

            RectTransform rect = cardGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280, 380);

            Image img = cardGo.GetComponent<Image>();
            img.color = ColorButtonBg;
            img.raycastTarget = true;

            // Icon
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 80);
            iconRect.sizeDelta = new Vector2(80, 80);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            // NameText
            TextMeshProUGUI nameLbl = CreateLabel(rect, "NameText", relicName, new Vector2(0, 0), new Vector2(240, 40), 22, TextAlignmentOptions.Center);

            // DescText
            TextMeshProUGUI descLbl = CreateLabel(rect, "DescText", desc, new Vector2(0, -60), new Vector2(240, 80), 18, TextAlignmentOptions.Center);

            // SelectButton
            GameObject btnGo = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rect, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -130);
            btnRect.sizeDelta = new Vector2(200, 45);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorModalButtonBg;
            btnImg.raycastTarget = true;

            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = btnImg;

            // SelectButton / Text
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(200, 45);
            TextMeshProUGUI btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "SELECT";
            btnTmp.fontSize = 22;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            RelicCardView cardView = cardGo.GetComponent<RelicCardView>();
            cardView.BindElements(nameLbl, descLbl, btn);
            return cardView;
        }

        private void BootstrapMetaShopDialogPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            // MetaShopDialogPanel (全画面モーダルオーバーレイ)
            GameObject panelGo = new GameObject("MetaShopDialogPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image overlayImg = panelGo.GetComponent<Image>();
            overlayImg.color = ColorOverlay;
            overlayImg.raycastTarget = true;

            // PanelRoot (ダイアログ本体枠)
            GameObject rootGo = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelGo.transform, false);

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1150, 700);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>();
            rootImg.color = ColorBgDialog;

            // ShopTitleText
            CreateLabel(
                rootRect, "ShopTitleText", "META PROGRESSION SHOP", new Vector2(0, 270), new Vector2(800, 50), 32, TextAlignmentOptions.Center);

            // PointsText
            TextMeshProUGUI pointsTmp = CreateLabel(
                rootRect, "PointsText", "POINTS: 0", new Vector2(-250, 220), new Vector2(400, 40), 24, TextAlignmentOptions.Left);

            // RunsText
            TextMeshProUGUI runsTmp = CreateLabel(
                rootRect, "RunsText", "RUNS: 0", new Vector2(250, 220), new Vector2(400, 40), 24, TextAlignmentOptions.Right);

            // Item1, Item2, Item3
            Button[] itemButtons = new Button[3];
            TextMeshProUGUI[] itemNameTexts = new TextMeshProUGUI[3];
            TextMeshProUGUI[] itemDescTexts = new TextMeshProUGUI[3];

            CreateShopItemCard(rootRect, "Item1", "Initial Stamina +10\nCost: 50 Pts", new Vector2(-340, 10), out itemButtons[0], out itemNameTexts[0], out itemDescTexts[0]);
            CreateShopItemCard(rootRect, "Item2", "Initial Skill +5\nCost: 100 Pts", new Vector2(0, 10), out itemButtons[1], out itemNameTexts[1], out itemDescTexts[1]);
            CreateShopItemCard(rootRect, "Item3", "Initial Mental +15\nCost: 150 Pts", new Vector2(340, 10), out itemButtons[2], out itemNameTexts[2], out itemDescTexts[2]);

            // CloseShopButton
            GameObject closeBtnGo = new GameObject("CloseShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(rootRect, false);
            RectTransform closeBtnRect = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRect.anchoredPosition = new Vector2(0, -250);
            closeBtnRect.sizeDelta = new Vector2(450, 60);

            Image closeBtnImg = closeBtnGo.GetComponent<Image>();
            closeBtnImg.color = ColorModalButtonBg;
            closeBtnImg.raycastTarget = true;

            Button closeButton = closeBtnGo.GetComponent<Button>();
            closeButton.targetGraphic = closeBtnImg;

            // CloseShopButton / Text
            GameObject closeBtnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeBtnTextGo.transform.SetParent(closeBtnGo.transform, false);
            RectTransform closeBtnTextRect = closeBtnTextGo.GetComponent<RectTransform>();
            closeBtnTextRect.anchoredPosition = Vector2.zero;
            closeBtnTextRect.sizeDelta = new Vector2(450, 60);
            TextMeshProUGUI closeBtnTmp = closeBtnTextGo.GetComponent<TextMeshProUGUI>();
            closeBtnTmp.text = "CLOSE / NEXT RUN";
            closeBtnTmp.fontSize = 22;
            closeBtnTmp.alignment = TextAlignmentOptions.Center;
            closeBtnTmp.color = Color.white;
            closeBtnTmp.raycastTarget = false;

            // 初期状態は非アクティブ
            panelGo.SetActive(false);

            // MetaShopDialogView への依存注入
            MetaShopDialogView metaShopDialog = _canvas.GetComponentInChildren<MetaShopDialogView>(true)
                ?? Object.FindFirstObjectByType<MetaShopDialogView>(FindObjectsInactive.Include);

            if (metaShopDialog != null)
            {
                MetaUnlockCatalogSO unlockCatalog = Resources.Load<MetaUnlockCatalogSO>("MetaUnlockCatalog");
                metaShopDialog.Bind(panelGo, pointsTmp, runsTmp, closeButton, itemButtons, itemNameTexts, itemDescTexts, _gameFlowController, unlockCatalog);
            }
            else
            {
                Debug.LogError("[UiBootstrapper] MetaShopDialogView not found, cannot bind references.");
            }
        }

        private static void CreateShopItemCard(
            RectTransform parent,
            string cardName,
            string desc,
            Vector2 pos,
            out Button itemButton,
            out TextMeshProUGUI nameText,
            out TextMeshProUGUI descText)
        {
            GameObject cardGo = new GameObject(cardName, typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);

            RectTransform rect = cardGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280, 420);

            Image img = cardGo.GetComponent<Image>();
            img.color = ColorButtonBg;
            img.raycastTarget = true;

            // Icon
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 80);
            iconRect.sizeDelta = new Vector2(80, 80);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            // NameText
            nameText = CreateLabel(rect, "NameText", cardName, new Vector2(0, 0), new Vector2(240, 40), 22, TextAlignmentOptions.Center);

            // DescText
            descText = CreateLabel(rect, "DescText", desc, new Vector2(0, -60), new Vector2(240, 80), 18, TextAlignmentOptions.Center);

            // SelectButton
            GameObject btnGo = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rect, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -130);
            btnRect.sizeDelta = new Vector2(200, 45);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorModalButtonBg;
            btnImg.raycastTarget = true;

            itemButton = btnGo.GetComponent<Button>();
            itemButton.targetGraphic = btnImg;

            // SelectButton / Text
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(200, 45);
            TextMeshProUGUI btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "SELECT";
            btnTmp.fontSize = 22;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;
        }
    }
}
