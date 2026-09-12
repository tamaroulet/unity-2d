// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using Game.Features.Boss;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// GameStateEventChannelSO を購読し、Stamina, Skill, Mental, Turn の
    /// テキスト・ゲージを更新する薄いビュー。自身はパラメータの計算を行わない。
    /// </summary>
    public class StatusView : MonoBehaviour
    {
        private GameStateEventChannelSO _gameStateChannel;
        private TextMeshProUGUI _turnText;
        private TextMeshProUGUI _staminaText;
        private TextMeshProUGUI _skillText;
        private TextMeshProUGUI _mentalText;
        private Slider _staminaGauge;
        private Slider _skillGauge;
        private Slider _mentalGauge;
        private RectTransform _staminaBarFill;
        private RectTransform _skillBarFill;
        private RectTransform _mentalBarFill;
        private TextMeshProUGUI _metaPointsText;
        private GameFlowController _gameFlowController;
        private BossBattleDialogView _bossBattleDialog;

        /// <summary>
        /// 直近に受信した GameState。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public GameState LastDisplayedState { get; private set; }

        /// <summary>
        /// 直近に受信した MetaProfileState。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public MetaProfileState LastDisplayedProfile { get; private set; }

        private void Awake()
        {
        }

        private void Start()
        {
            HookFlowControllerEvents();
        }

        private void HookFlowControllerEvents()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.OnBossBattleOccurred -= HandleBossBattle;
                _gameFlowController.OnBossBattleOccurred += HandleBossBattle;

                _gameFlowController.OnMetaProfileChanged -= HandleMetaProfileChanged;
                _gameFlowController.OnMetaProfileChanged += HandleMetaProfileChanged;
            }
        }

        private void HandleBossBattle(BossSO boss, FullBattleResult result, System.Action callback)
        {
            if (_bossBattleDialog != null)
            {
                _bossBattleDialog.Show(boss, result, callback);
            }
            else
            {
                callback?.Invoke();
            }
        }

        private void HandleMetaProfileChanged(MetaProfileState profile)
        {
            if (profile == null)
            {
                return;
            }

            LastDisplayedProfile = profile;

            if (_metaPointsText != null)
            {
                _metaPointsText.text = $"POINTS: {profile.AvailableMetaPoints}";
            }
        }

        private void OnEnable()
        {
            HookFlowControllerEvents();
            if (_gameStateChannel != null)
            {
                _gameStateChannel.OnEventRaised += OnGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.OnBossBattleOccurred -= HandleBossBattle;
                _gameFlowController.OnMetaProfileChanged -= HandleMetaProfileChanged;
            }

            if (_gameStateChannel != null)
            {
                _gameStateChannel.OnEventRaised -= OnGameStateChanged;
            }
        }

        /// <summary>
        /// Unity EditMode では PlayerLoop が常時動作せず SetActive(true) による
        /// OnEnable() の自動発火が不安定になる場合があるため、テスト等から
        /// チャンネル購読を明示的に行うための公開メソッド。
        /// ランタイムブートストラップ時には各 UI 参照も渡す。
        /// </summary>
        public void Bind(
            GameStateEventChannelSO channel,
            GameFlowController gameFlowController = null,
            BossBattleDialogView bossBattleDialog = null,
            TextMeshProUGUI turnText = null,
            TextMeshProUGUI staminaText = null,
            TextMeshProUGUI skillText = null,
            TextMeshProUGUI mentalText = null,
            TextMeshProUGUI metaPointsText = null,
            Slider staminaGauge = null,
            Slider skillGauge = null,
            Slider mentalGauge = null,
            RectTransform staminaBarFill = null,
            RectTransform skillBarFill = null,
            RectTransform mentalBarFill = null)
        {
            _gameStateChannel = channel;
            if (channel != null)
            {
                channel.OnEventRaised -= OnGameStateChanged;
                channel.OnEventRaised += OnGameStateChanged;
            }

            if (gameFlowController != null)
            {
                _gameFlowController = gameFlowController;
                HookFlowControllerEvents();
            }

            if (bossBattleDialog != null)
            {
                _bossBattleDialog = bossBattleDialog;
            }

            if (turnText != null) _turnText = turnText;
            if (staminaText != null) _staminaText = staminaText;
            if (skillText != null) _skillText = skillText;
            if (mentalText != null) _mentalText = mentalText;
            if (metaPointsText != null) _metaPointsText = metaPointsText;

            if (staminaGauge != null) _staminaGauge = staminaGauge;
            if (skillGauge != null) _skillGauge = skillGauge;
            if (mentalGauge != null) _mentalGauge = mentalGauge;

            if (staminaBarFill != null) _staminaBarFill = staminaBarFill;
            if (skillBarFill != null) _skillBarFill = skillBarFill;
            if (mentalBarFill != null) _mentalBarFill = mentalBarFill;

            if (_gameFlowController != null)
            {
                if (_gameFlowController.CurrentState != null)
                {
                    OnGameStateChanged(_gameFlowController.CurrentState);
                }
                if (_gameFlowController.MetaProfile != null)
                {
                    HandleMetaProfileChanged(_gameFlowController.MetaProfile);
                }
            }
        }

        /// <summary>
        /// GameStateEventChannelSO からの通知を受け、テキストとゲージを最新の状態に更新する。
        /// </summary>
        public void OnGameStateChanged(GameState state)
        {
            if (state == null)
            {
                return;
            }

            LastDisplayedState = state;

            if (_turnText != null) _turnText.text = $"TURN {state.CurrentTurn} / 24";
            if (_staminaText != null) _staminaText.text = $"Stamina: {state.Stamina} / 100";
            if (_skillText != null) _skillText.text = $"Skill: {state.Skill}";
            if (_mentalText != null) _mentalText.text = $"Mental: {state.Mental} / 100";

            SetGauge(_staminaGauge, state.Stamina);
            SetGauge(_skillGauge, state.Skill);
            SetGauge(_mentalGauge, state.Mental);
        }

        private static void SetGauge(Slider gauge, int value)
        {
            if (gauge != null)
            {
                gauge.value = value;
            }
        }
    }
}
