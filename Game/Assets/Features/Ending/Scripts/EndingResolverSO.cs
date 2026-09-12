// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using UnityEngine;

namespace Game.Features.Ending
{
    /// <summary>
    /// GameState とエンディングルールからエンディング種別を判定する純粋関数としての
    /// ScriptableObject。static フィールド、Time、Random、DateTime、Debug は参照しない。
    /// `TerminationKind.Normal` に到達した場合にのみ呼ばれることを前提とする。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Ending/EndingResolver", fileName = "EndingResolver")]
    public class EndingResolverSO : ScriptableObject
    {
        public EndingKind Resolve(GameState state, EndingRulesSO rules)
        {
            int score = (state.Skill * rules.SkillWeight)
                + (state.Mental * rules.MentalWeight)
                + (state.Stamina * rules.StaminaWeight);

            if (score >= rules.Threshold)
            {
                return EndingKind.True;
            }

            return SelectByMaxParameter(state, rules);
        }

        /// <summary>
        /// 3つのパラメータのうち最大値のものに対応するエンドを返す。
        /// 最大値が複数ある場合、rules の優先順が先のものを返す。
        /// </summary>
        private EndingKind SelectByMaxParameter(GameState state, EndingRulesSO rules)
        {
            int maxValue = state.Stamina;

            if (state.Skill > maxValue)
            {
                maxValue = state.Skill;
            }

            if (state.Mental > maxValue)
            {
                maxValue = state.Mental;
            }

            foreach (TrackedParameter parameter in rules.PriorityOrder)
            {
                if (GetParameterValue(state, parameter) == maxValue)
                {
                    return ToEndingKind(parameter);
                }
            }

            return EndingKind.True;
        }

        private int GetParameterValue(GameState state, TrackedParameter parameter)
        {
            switch (parameter)
            {
                case TrackedParameter.Stamina:
                    return state.Stamina;
                case TrackedParameter.Skill:
                    return state.Skill;
                case TrackedParameter.Mental:
                    return state.Mental;
                default:
                    return 0;
            }
        }

        private EndingKind ToEndingKind(TrackedParameter parameter)
        {
            switch (parameter)
            {
                case TrackedParameter.Stamina:
                    return EndingKind.Stamina;
                case TrackedParameter.Skill:
                    return EndingKind.Skill;
                case TrackedParameter.Mental:
                    return EndingKind.Mental;
                default:
                    return EndingKind.True;
            }
        }
    }
}
