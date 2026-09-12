// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.GameFlow;
using Game.Features.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// GameEventFiredChannelSO を購読し、発火したイベントの内容をポップアップとして
    /// 表示する薄いビュー。自身はイベントの発火判定を行わない。
    /// </summary>
    public class EventDialogView : MonoBehaviour
    {
        private GameEventFiredChannelSO _eventFiredChannel;
        private GameEventCatalogSO _eventCatalog;
        private GameFlowController _gameFlowController;

        private GameObject _panelRoot;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _bodyText;
        private Button _okButton;

        /// <summary>
        /// ランタイムブートストラップ時に各参照を直接代入・結線する。
        /// </summary>
        public void Bind(
            GameObject panelRoot,
            TextMeshProUGUI titleText = null,
            TextMeshProUGUI bodyText = null,
            Button okButton = null,
            GameEventFiredChannelSO channel = null,
            GameEventCatalogSO catalog = null,
            GameFlowController gameFlowController = null)
        {
            _panelRoot = panelRoot;
            if (titleText != null) _titleText = titleText;
            if (bodyText != null) _bodyText = bodyText;
            if (channel != null)
            {
                if (_eventFiredChannel != null)
                {
                    _eventFiredChannel.OnEventRaised -= OnEventFired;
                }
                _eventFiredChannel = channel;
                _eventFiredChannel.OnEventRaised += OnEventFired;
            }
            if (catalog != null) _eventCatalog = catalog;
            if (gameFlowController != null) _gameFlowController = gameFlowController;
            if (okButton != null)
            {
                if (_okButton != null)
                {
                    _okButton.onClick.RemoveListener(OnEventDismissed);
                }
                _okButton = okButton;
                _okButton.onClick.AddListener(OnEventDismissed);
            }
        }

        /// <summary>
        /// パネルの表示状態。
        /// </summary>
        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;

        /// <summary>
        /// 表示中のタイトル。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public string DisplayedTitle => _titleText != null ? _titleText.text : string.Empty;

        private void OnEnable()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_eventFiredChannel != null)
            {
                _eventFiredChannel.OnEventRaised += OnEventFired;
            }

            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(OnEventDismissed);
                _okButton.onClick.AddListener(OnEventDismissed);
            }
        }

        private void OnDisable()
        {
            if (_eventFiredChannel != null)
            {
                _eventFiredChannel.OnEventRaised -= OnEventFired;
            }

            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(OnEventDismissed);
            }
        }

        /// <summary>
        /// Unity EditMode では PlayerLoop が常時動作せず SetActive(true) による
        /// OnEnable() の自動発火が不安定になる場合があるため、テスト等から
        /// チャンネル購読を明示的に行うための公開メソッド。
        /// </summary>
        public void Bind(GameEventFiredChannelSO channel, GameEventCatalogSO catalog)
        {
            _eventFiredChannel = channel;
            _eventCatalog = catalog;
            if (channel != null)
            {
                channel.OnEventRaised += OnEventFired;
            }
        }

        /// <summary>
        /// GameEventFiredChannelSO からの通知を受け、該当イベントの内容をカタログから
        /// 取得してダイアログを表示する。カタログに該当イベントが無ければ何もしない。
        /// </summary>
        public void OnEventFired(int eventId)
        {
            GameEventSO firedEvent = FindEvent(eventId);
            if (firedEvent == null)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = firedEvent.DisplayName;
            }

            if (_bodyText != null)
            {
                _bodyText.text = firedEvent.Body;
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// OK ボタン押下時に呼ばれ、ダイアログを閉じる。
        /// </summary>
        private void OnEventDismissed()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_gameFlowController != null && _gameFlowController.CurrentPhase == GamePhase.ShowingEvent)
            {
                _gameFlowController.OnEventDismissed();
            }
        }

        /// <summary>
        /// EditMode では Button.onClick.Invoke() が動的リスナーを発火しない場合があるため、
        /// テスト等から OnEventDismissed を直接実行するための公開メソッド。
        /// </summary>
        public void Dismiss()
        {
            OnEventDismissed();
        }

        private GameEventSO FindEvent(int eventId)
        {
            if (_eventCatalog == null)
            {
                return null;
            }

            IReadOnlyList<GameEventSO> events = _eventCatalog.Events;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].EventId == eventId)
                {
                    return events[i];
                }
            }

            return null;
        }
    }
}
