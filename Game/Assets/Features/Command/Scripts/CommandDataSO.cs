// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using UnityEngine;

namespace Game.Features.Command
{
    /// <summary>
    /// コマンド名と、そのコマンドが GameState に与える効果値を保持する。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Command/CommandData", fileName = "CommandData")]
    public class CommandDataSO : ScriptableObject
    {
        [SerializeField] private string _commandName;
        [SerializeField] private int _staminaDelta;
        [SerializeField] private int _skillDelta;
        [SerializeField] private int _mentalDelta;
        [SerializeField] private int _staminaCost;

        public string CommandName => _commandName;

        /// <summary>
        /// 自身のフィールドから組み立てた CommandEffect を返す。
        /// </summary>
        public CommandEffect Effect =>
            new CommandEffect(_staminaDelta, _skillDelta, _mentalDelta, _staminaCost);
    }
}
