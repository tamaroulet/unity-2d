// SPDX-AI-Disclosure: ai-generated
using System;
using System.Text;
using Game.Features.Boss;
using Game.Features.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// ボスバトルダイアログの表示・ステータスゲージ更新・ログ表示を制御する View コンポーネント。
    /// GameFlowController またはイベントチャンネルからの通知を受けて表示を行う。
    /// </summary>
    public class BossBattleDialogView : MonoBehaviour
    {
        private GameObject _panelRoot;
        private TextMeshProUGUI _bossNameText;
        private TextMeshProUGUI _bossHpText;
        private Slider _bossHpSlider;
        private TextMeshProUGUI _shieldText;
        private TextMeshProUGUI _battleLogText;
        private Button _dismissButton;
        private TextMeshProUGUI _dismissButtonText;

        private Action _onDismissedCallback;

        /// <summary>
        /// ランタイムブートストラップ時に各参照を直接代入・結線する。
        /// </summary>
        public void Bind(
            GameObject panelRoot,
            TextMeshProUGUI bossNameText = null,
            TextMeshProUGUI bossHpText = null,
            Slider bossHpSlider = null,
            TextMeshProUGUI shieldText = null,
            TextMeshProUGUI battleLogText = null,
            Button dismissButton = null,
            TextMeshProUGUI dismissButtonText = null)
        {
            _panelRoot = panelRoot;
            if (bossNameText != null) _bossNameText = bossNameText;
            if (bossHpText != null) _bossHpText = bossHpText;
            if (bossHpSlider != null) _bossHpSlider = bossHpSlider;
            if (shieldText != null) _shieldText = shieldText;
            if (battleLogText != null) _battleLogText = battleLogText;
            if (dismissButton != null)
            {
                if (_dismissButton != null)
                {
                    _dismissButton.onClick.RemoveListener(OnDismissButtonClicked);
                }
                _dismissButton = dismissButton;
                _dismissButton.onClick.AddListener(OnDismissButtonClicked);
            }
            if (dismissButtonText != null) _dismissButtonText = dismissButtonText;

            EnsureReferences();
        }

        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;
        public bool IsVisible => IsPanelActive;

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (_dismissButtonText == null)
            {
                _dismissButtonText = ResolveDismissButtonText();
            }

            if (_dismissButtonText != null)
            {
                _dismissButtonText.text = "AUTO BATTLE / NEXT";
            }
        }

        /// <summary>
        /// 決定ボタンのラベルを解決する。ボス名・HP・シールド・戦闘ログとして
        /// 既にバインドされているテキストは、誤ってボタンラベルとして上書きしないよう除外する。
        /// </summary>
        private TextMeshProUGUI ResolveDismissButtonText()
        {
            if (_dismissButton == null)
            {
                return null;
            }

            foreach (TextMeshProUGUI candidate in _dismissButton.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (candidate == _bossNameText || candidate == _bossHpText ||
                    candidate == _shieldText || candidate == _battleLogText)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private void OnEnable()
        {
            EnsureReferences();
            if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveListener(OnDismissButtonClicked);
                _dismissButton.onClick.AddListener(OnDismissButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveListener(OnDismissButtonClicked);
            }
        }

        /// <summary>
        /// ボス戦の結果とターン履歴を受け取ってダイアログを表示する。
        /// </summary>
        public void Show(BossSO boss, FullBattleResult battleResult, Action onDismissed = null)
        {
            EnsureReferences();
            if (_panelRoot == null)
            {
                Debug.LogError("[BossBattleDialogView] _panelRoot is null, cannot display boss battle modal.");
                return;
            }
            _onDismissedCallback = onDismissed;

            if (_bossNameText != null)
            {
                _bossNameText.text = boss != null ? boss.BossName : "BOSS BATTLE";
            }

            if (_dismissButtonText == null)
            {
                _dismissButtonText = ResolveDismissButtonText();
            }

            if (_dismissButtonText != null)
            {
                _dismissButtonText.text = battleResult != null && battleResult.Outcome == BattleOutcomeKind.Victory
                    ? "VICTORY / NEXT ACT"
                    : "DEFEAT / RESTART";
            }

            if (battleResult != null)
            {
                UpdateBossStatus(battleResult.FinalBossState);
                UpdateBattleLog(battleResult);
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// ボスのHPおよびシールド表示を更新する。
        /// </summary>
        public void UpdateBossStatus(BossState bossState)
        {
            if (bossState == null)
            {
                return;
            }

            if (_bossHpText != null)
            {
                _bossHpText.text = $"HP: {bossState.CurrentHp} / {bossState.MaxHp}";
            }

            if (_bossHpSlider != null)
            {
                _bossHpSlider.maxValue = bossState.MaxHp;
                _bossHpSlider.value = bossState.CurrentHp;
            }

            if (_shieldText != null)
            {
                _shieldText.text = bossState.Shield > 0 ? $"Shield: {bossState.Shield}" : string.Empty;
            }
        }

        /// <summary>
        /// 戦闘ターン履歴から要約ログを構築して表示する。
        /// </summary>
        private void UpdateBattleLog(FullBattleResult battleResult)
        {
            if (_battleLogText == null || battleResult == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(battleResult.Outcome == BattleOutcomeKind.Victory ? "[VICTORY] Boss Defeated!" : "[DEFEAT] Player Defeated...");

            if (battleResult.TurnHistory != null)
            {
                for (int i = 0; i < battleResult.TurnHistory.Count; i++)
                {
                    var turn = battleResult.TurnHistory[i];
                    sb.AppendLine($"Turn {i + 1}: Player -{turn.DamageDealtToBoss} DMG | Boss Action: {turn.BossActionTaken}");
                }
            }

            _battleLogText.text = sb.ToString();
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        public void Dismiss()
        {
            Hide();
            _onDismissedCallback?.Invoke();
            _onDismissedCallback = null;
        }

        private void OnDismissButtonClicked()
        {
            Dismiss();
        }
    }
}
