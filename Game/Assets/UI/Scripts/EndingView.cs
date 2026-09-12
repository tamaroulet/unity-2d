// SPDX-AI-Disclosure: ai-generated
using System;
using Game.Core;
using Game.Features.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// EndingDecidedChannelSO を購読し、確定したエンディングに応じたリザルト画面を
    /// 表示する薄いビュー。自身はエンディングの判定を行わない。
    /// </summary>
    public class EndingView : MonoBehaviour
    {
        [Serializable]
        private struct EndingLabel
        {
            public EndingKind Kind;
            public string DisplayName;
        }

        private EndingDecidedChannelSO _endingDecidedChannel;
        private GameObject _panelRoot;
        private TextMeshProUGUI _resultText;
        private EndingLabel[] _endingLabels;
        private Button _restartButton;
        private GameFlowController _gameFlowController;

        /// <summary>
        /// パネルの表示状態。
        /// </summary>
        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;

        /// <summary>
        /// 表示中のリザルトテキスト。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public string DisplayedResult => _resultText != null ? _resultText.text : string.Empty;

        private void OnEnable()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_endingDecidedChannel != null)
            {
                _endingDecidedChannel.OnEventRaised += OnEndingDecided;
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void OnDisable()
        {
            if (_endingDecidedChannel != null)
            {
                _endingDecidedChannel.OnEventRaised -= OnEndingDecided;
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }
        }

        /// <summary>
        /// リスタートボタンがクリックされたときに呼ばれる。
        /// パネルを非表示にし、GameFlowController.RequestRestart() で周回の流れを進める。
        /// </summary>
        public void OnRestartClicked()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_gameFlowController != null)
            {
                _gameFlowController.RequestRestart();
            }
        }

        /// <summary>
        /// Unity EditMode では PlayerLoop が常時動作せず SetActive(true) による
        /// OnEnable() の自動発火が不安定になる場合があるため、テスト等から
        /// チャンネル購読を明示的に行うための公開メソッド。
        /// ランタイムブートストラップ時には各 UI 参照も渡す。
        /// </summary>
        public void Bind(
            EndingDecidedChannelSO channel,
            GameFlowController gameFlowController = null,
            Button restartButton = null,
            GameObject panelRoot = null,
            TextMeshProUGUI resultText = null)
        {
            _endingDecidedChannel = channel;
            if (channel != null)
            {
                channel.OnEventRaised -= OnEndingDecided;
                channel.OnEventRaised += OnEndingDecided;
            }

            if (gameFlowController != null)
            {
                _gameFlowController = gameFlowController;
            }

            if (panelRoot != null)
            {
                _panelRoot = panelRoot;
            }

            if (resultText != null)
            {
                _resultText = resultText;
            }

            if (restartButton != null)
            {
                if (_restartButton != null)
                {
                    _restartButton.onClick.RemoveListener(OnRestartClicked);
                }
                _restartButton = restartButton;
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
            else if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        /// <summary>
        /// EndingDecidedChannelSO からの通知を受け、リザルトテキストを設定して画面を表示する。
        /// </summary>
        public void OnEndingDecided(EndingKind ending)
        {
            if (_resultText != null)
            {
                _resultText.text = ResolveDisplayName(ending);
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// _endingLabels からエンディングに対応する表示名を探す。見つからなければ
        /// enum に応じたフォールバック名を返す。
        /// </summary>
        private string ResolveDisplayName(EndingKind ending)
        {
            if (_endingLabels != null)
            {
                foreach (EndingLabel label in _endingLabels)
                {
                    if (label.Kind == ending)
                    {
                        return label.DisplayName;
                    }
                }
            }

            return ending switch
            {
                EndingKind.True => "TRUE ENDING",
                EndingKind.Skill => "SKILL MASTER ENDING",
                EndingKind.Mental => "MENTAL FORTITUDE ENDING",
                EndingKind.Stamina => "IRON VITALITY ENDING",
                EndingKind.Defeat => "GAME OVER",
                _ => "GAME CLEAR"
            };
        }
    }
}
