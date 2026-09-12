// SPDX-AI-Disclosure: ai-generated
using System;
using System.Linq;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// メタショップ（周回アンロック購入）ダイアログの表示とポイント管理を行う View コンポーネント。
    /// </summary>
    public class MetaShopDialogView : MonoBehaviour
    {
        private GameObject _panelRoot;
        private TextMeshProUGUI _availablePointsText;
        private TextMeshProUGUI _totalRunsText;
        private Button _closeButton;

        private GameFlowController _gameFlowController;
        private MetaUnlockCatalogSO _unlockCatalog;
        private Button[] _itemButtons = new Button[3];
        private TextMeshProUGUI[] _itemNameTexts = new TextMeshProUGUI[3];
        private TextMeshProUGUI[] _itemDescTexts = new TextMeshProUGUI[3];

        private Action _onCloseCallback;

        /// <summary>
        /// ランタイムブートストラップ時に各参照を直接代入・結線する。
        /// </summary>
        public void Bind(
            GameObject panelRoot,
            TextMeshProUGUI availablePointsText = null,
            TextMeshProUGUI totalRunsText = null,
            Button closeButton = null,
            Button[] itemButtons = null,
            TextMeshProUGUI[] itemNameTexts = null,
            TextMeshProUGUI[] itemDescTexts = null,
            GameFlowController controller = null,
            MetaUnlockCatalogSO catalog = null)
        {
            _panelRoot = panelRoot;
            if (availablePointsText != null) _availablePointsText = availablePointsText;
            if (totalRunsText != null) _totalRunsText = totalRunsText;

            if (closeButton != null)
            {
                if (_closeButton != null)
                {
                    _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
                }
                _closeButton = closeButton;
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (itemButtons != null) _itemButtons = itemButtons;
            if (itemNameTexts != null) _itemNameTexts = itemNameTexts;
            if (itemDescTexts != null) _itemDescTexts = itemDescTexts;

            if (controller != null || catalog != null)
            {
                Bind(controller, catalog);
            }
        }

        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            HookController();
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
            UnhookController();
        }

        private void HookController()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.OnMetaShopRequested -= Show;
                _gameFlowController.OnMetaShopRequested += Show;
            }
        }

        private void UnhookController()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.OnMetaShopRequested -= Show;
            }
        }

        public void Bind(GameFlowController controller, MetaUnlockCatalogSO catalog = null)
        {
            UnhookController();
            _gameFlowController = controller;
            if (catalog != null)
            {
                _unlockCatalog = catalog;
            }
            HookController();
        }

        public void Show(MetaProfileState profile, Action onClose = null)
        {
            _onCloseCallback = onClose;
            UpdateProfile(profile);

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        public void UpdateProfile(MetaProfileState profile)
        {
            if (profile == null)
            {
                return;
            }

            if (_availablePointsText != null)
            {
                _availablePointsText.text = $"POINTS: {profile.AvailableMetaPoints}";
            }

            if (_totalRunsText != null)
            {
                _totalRunsText.text = $"RUNS: {profile.TotalRunsCompleted}";
            }

            RenderCards(profile);
        }

        private void RenderCards(MetaProfileState profile)
        {
            if (_unlockCatalog == null || _unlockCatalog.Unlocks == null)
            {
                return;
            }

            var unlocks = _unlockCatalog.Unlocks;
            int cardCount = _itemButtons != null ? _itemButtons.Length : 0;

            for (int i = 0; i < cardCount; i++)
            {
                Button btn = _itemButtons[i];
                if (btn == null) continue;

                Transform cardTr = btn.transform.parent != null ? btn.transform.parent : btn.transform;

                if (i < unlocks.Count && unlocks[i] != null)
                {
                    cardTr.gameObject.SetActive(true);
                    MetaUnlockSO unlock = unlocks[i];

                    if (_itemNameTexts != null && i < _itemNameTexts.Length && _itemNameTexts[i] != null)
                    {
                        _itemNameTexts[i].text = unlock.UnlockName;
                    }

                    if (_itemDescTexts != null && i < _itemDescTexts.Length && _itemDescTexts[i] != null)
                    {
                        _itemDescTexts[i].text = $"+{unlock.BonusValue}\nCost: {unlock.Cost} Pts";
                    }

                    bool isPurchased = profile.UnlockedIds != null && profile.UnlockedIds.Contains(unlock.UnlockId);
                    btn.interactable = !isPurchased && profile.AvailableMetaPoints >= unlock.Cost;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (_gameFlowController != null && _gameFlowController.TryPurchaseMetaUnlock(unlock))
                        {
                            UpdateProfile(_gameFlowController.MetaProfile);
                        }
                    });
                }
                else
                {
                    cardTr.gameObject.SetActive(false);
                }
            }
        }

        public void Dismiss()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            _onCloseCallback?.Invoke();
            _onCloseCallback = null;
        }

        private void OnCloseButtonClicked()
        {
            Dismiss();
        }
    }
}
