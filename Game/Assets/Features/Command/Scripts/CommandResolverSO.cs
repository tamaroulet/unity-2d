// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using UnityEngine;

namespace Game.Features.Command
{
    /// <summary>
    /// GameState にコマンドの効果を適用する純粋関数としての ScriptableObject。
    /// static フィールド、Time、Random、DateTime、Debug は参照しない。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Command/CommandResolver", fileName = "CommandResolver")]
    public class CommandResolverSO : ScriptableObject
    {
        /// <summary>
        /// コマンドの identity（CommandDataSO 自体）を受け取るオーバーロード。
        /// 現時点では effect ベースの Resolve に委譲するのみだが、将来的に
        /// 「特定コマンド使用時のみ発動するレリック」等、コマンド識別子に基づく判定を
        /// 追加する際の拡張点として用意している。
        /// </summary>
        public CommandResult Resolve(GameState state, CommandDataSO command, GameRulesSO rules)
        {
            return Resolve(state, command.Effect, rules);
        }

        public CommandResult Resolve(GameState state, CommandEffect effect, GameRulesSO rules)
        {
            if (state.Stamina < effect.StaminaCost)
            {
                return new CommandResult
                {
                    State = state,
                    IsExecutable = false,
                    Termination = TerminationKind.Continue
                };
            }

            CommandEffect determinedEffect = DetermineEffect(effect);
            GameState appliedState = ApplyEffect(state, determinedEffect, rules);

            TerminationKind afterApply = TurnRules.EvaluateTermination(appliedState, rules.MaxTurn);
            if (afterApply == TerminationKind.GameOver)
            {
                return new CommandResult
                {
                    State = appliedState,
                    IsExecutable = true,
                    Termination = TerminationKind.GameOver
                };
            }

            GameState advancedState = TurnRules.AdvanceTurn(appliedState);
            TerminationKind afterAdvance = TurnRules.EvaluateTermination(advancedState, rules.MaxTurn);

            return new CommandResult
            {
                State = advancedState,
                IsExecutable = true,
                Termination = afterAdvance
            };
        }

        /// <summary>
        /// 効果値を決定する。型フェーズでは恒等関数。将来の乱数挿入用にこの関数を分離している。
        /// </summary>
        private CommandEffect DetermineEffect(CommandEffect baseEffect)
        {
            return baseEffect;
        }

        /// <summary>
        /// 効果値を状態に適用し、パラメータをクランプする。ターンは加算しない。
        /// </summary>
        private GameState ApplyEffect(GameState state, CommandEffect effect, GameRulesSO rules)
        {
            int stamina = TurnRules.Clamp(state.Stamina + effect.StaminaDelta, rules.ParamMin, rules.ParamMax);
            int skill = TurnRules.Clamp(state.Skill + effect.SkillDelta, rules.ParamMin, rules.ParamMax);
            int mental = TurnRules.Clamp(state.Mental + effect.MentalDelta, rules.ParamMin, rules.ParamMax);

            return state with { Stamina = stamina, Skill = skill, Mental = mental };
        }
    }
}
