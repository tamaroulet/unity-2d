// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.GameFlow;
using Game.Features.Relic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// レリック3択ドラフト獲得ダイアログの表示と選択を担う View。
    /// 自身は計算を行わず、RelicAcquiredChannelSO 経由で獲得通知を発行する。
    /// </summary>
    public class RelicDraftDialogView : MonoBehaviour
    {
        private RelicAcquiredChannelSO _relicAcquiredChannel;
        private GameFlowController _gameFlowController;
        private GameObject _panelRoot;
        private List<RelicCardView> _cardViews = new List<RelicCardView>();

        /// <summary>
        /// ランタイムブートストラップ時に各参照を直接代入・結線する。
        /// </summary>
        public void Bind(
            GameObject panelRoot,
            List<RelicCardView> cardViews,
            GameFlowController controller = null,
            RelicAcquiredChannelSO channel = null)
        {
            _panelRoot = panelRoot;
            if (cardViews != null)
            {
                _cardViews = cardViews;
            }
            if (controller != null || channel != null)
            {
                Bind(controller, channel);
            }
        }

        public int LastSelectedRelicId { get; private set; } = -1;

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        private void OnEnable()
        {
            HookController();
        }

        private void OnDisable()
        {
            UnhookController();
        }

        private void HookController()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.OnRelicDraftRequested -= Show;
                _gameFlowController.OnRelicDraftRequested += Show;
            }
        }

        private void UnhookController()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.OnRelicDraftRequested -= Show;
            }
        }

        /// <summary>
        /// テスト等から明示的に依存を注入・購読するためのバインドメソッド。
        /// </summary>
        public void Bind(GameFlowController controller, RelicAcquiredChannelSO channel = null)
        {
            UnhookController();
            _gameFlowController = controller;
            if (channel != null)
            {
                _relicAcquiredChannel = channel;
            }
            HookController();
        }

        /// <summary>
        /// 提示されたレリック一覧（通常3件）を各カードにバインドし、パネルを表示する。
        /// </summary>
        public void Show(IReadOnlyList<RelicSO> options)
        {
            if (options == null || options.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (i < options.Count && _cardViews[i] != null)
                {
                    _cardViews[i].gameObject.SetActive(true);
                    _cardViews[i].Bind(options[i], OnCardSelected);
                }
                else if (_cardViews[i] != null)
                {
                    _cardViews[i].gameObject.SetActive(false);
                }
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// レリック選択時のハンドラ。チャンネル通知を発行してダイアログを閉じる。
        /// </summary>
        public void OnCardSelected(int relicId)
        {
            LastSelectedRelicId = relicId;

            if (_relicAcquiredChannel != null)
            {
                _relicAcquiredChannel.Raise(relicId);
            }

            Dismiss();
        }

        /// <summary>
        /// パネルを非表示にする。
        /// </summary>
        public void Dismiss()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }
    }
}
