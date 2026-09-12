// SPDX-AI-Disclosure: ai-generated
using Game.Features.Command;
using Game.Features.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// コマンド名・コストを表示し、押下時に GameFlowController.ExecuteCommand を
    /// 呼び出す薄いビュー。自身はコマンドの実行可否や効果を判定しない。
    /// </summary>
    public class CommandButtonView : MonoBehaviour
    {
        private CommandDataSO _command;
        private GameFlowController _gameFlowController;
        private Button _button;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _costText;

        /// <summary>
        /// 表示中のコマンド名。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public string DisplayedName => _command != null ? _command.CommandName : string.Empty;

        /// <summary>
        /// 表示中のコマンドコスト。
        /// </summary>
        public int DisplayedCost => _command != null ? _command.Effect.StaminaCost : 0;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            RefreshLabel();

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnCommandClick);
                _button.onClick.AddListener(OnCommandClick);
            }
        }

        private void EnsureReferences()
        {
            if (_button == null) _button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnCommandClick);
            }
        }

        /// <summary>
        /// ランタイムブートストラップ時に各参照を直接代入・結線する。
        /// </summary>
        public void Bind(
            CommandDataSO command,
            GameFlowController gameFlowController,
            Button button = null,
            TextMeshProUGUI nameText = null,
            TextMeshProUGUI costText = null)
        {
            _command = command;
            _gameFlowController = gameFlowController;
            if (button != null) _button = button;
            if (nameText != null) _nameText = nameText;
            if (costText != null) _costText = costText;

            EnsureReferences();
            RefreshLabel();

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnCommandClick);
                _button.onClick.AddListener(OnCommandClick);
            }
        }

        /// <summary>
        /// _command のフィールドからコマンド名とコストの表示を更新する。
        /// </summary>
        private void RefreshLabel()
        {
            if (_command == null)
            {
                return;
            }

            if (_nameText != null)
            {
                string cmdName = _command.name switch
                {
                    "Study" => "STUDY\nSkill+5",
                    "Train" => "TRAIN\nSkill+10",
                    "Rest" => "REST\nStamina+30",
                    _ => _command.CommandName
                };
                _nameText.text = cmdName;
            }

            if (_costText != null)
            {
                _costText.text = $"Cost: {_command.Effect.StaminaCost}";
            }
        }

        /// <summary>
        /// ボタン押下時に呼ばれ、GameFlowController へコマンドの実行を委譲する。
        /// </summary>
        private void OnCommandClick()
        {
            EnsureReferences();
            Debug.Log($"[CommandButtonView] Clicked button for command: {(_command != null ? _command.name : "null")}");

            if (_command == null || _gameFlowController == null)
            {
                Debug.LogError($"[CommandButtonView] Error: _command is {(_command == null ? "null" : "valid")}, _gameFlowController is {(_gameFlowController == null ? "null" : "valid")}");
                return;
            }

            _gameFlowController.ExecuteCommand(_command);
        }

        /// <summary>
        /// EditMode では Button.onClick.Invoke() が動的リスナーを発火しない場合があるため、
        /// テスト等から OnCommandClick を直接実行するための公開メソッド。
        /// </summary>
        public void Execute()
        {
            OnCommandClick();
        }
    }
}
