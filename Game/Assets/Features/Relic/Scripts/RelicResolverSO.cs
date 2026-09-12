// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Command;
using UnityEngine;

namespace Game.Features.Relic
{
    /// <summary>
    /// 所持レリックの効果を GameState や CommandEffect に適用する純粋関数 Resolver。
    /// 内部状態を持たず、引数のみから計算を行う。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Relic/RelicResolver", fileName = "RelicResolver")]
    public class RelicResolverSO : ScriptableObject
    {
        /// <summary>
        /// ターン開始時に発動するレリック（OnTurnStart）の効果を適用した新しい GameState を返す。
        /// </summary>
        public GameState ApplyTurnStartRelics(
            GameState state,
            IReadOnlyList<RelicSO> activeRelics,
            GameRulesSO rules)
        {
            if (state == null || activeRelics == null || activeRelics.Count == 0 || rules == null)
            {
                return state;
            }

            int staminaBonus = 0;
            int skillBonus = 0;
            int mentalBonus = 0;

            for (int i = 0; i < activeRelics.Count; i++)
            {
                RelicSO relic = activeRelics[i];
                if (relic != null && relic.TriggerKind == RelicTriggerKind.OnTurnStart)
                {
                    staminaBonus += relic.Effect.StaminaDeltaBonus;
                    skillBonus += relic.Effect.SkillDeltaBonus;
                    mentalBonus += relic.Effect.MentalDeltaBonus;
                }
            }

            int clampedStamina = TurnRules.Clamp(state.Stamina + staminaBonus, rules.ParamMin, rules.ParamMax);
            int clampedSkill = TurnRules.Clamp(state.Skill + skillBonus, rules.ParamMin, rules.ParamMax);
            int clampedMental = TurnRules.Clamp(state.Mental + mentalBonus, rules.ParamMin, rules.ParamMax);

            return state with
            {
                Stamina = clampedStamina,
                Skill = clampedSkill,
                Mental = clampedMental
            };
        }

        /// <summary>
        /// コマンド実行時に発動するレリック（OnCommandExecuted）の効果を基本効果に合成して返す。
        /// </summary>
        public CommandEffect ModifyCommandEffect(
            CommandEffect baseEffect,
            IReadOnlyList<RelicSO> activeRelics)
        {
            if (activeRelics == null || activeRelics.Count == 0)
            {
                return baseEffect;
            }

            int staminaBonus = 0;
            int skillBonus = 0;
            int mentalBonus = 0;
            float costMultiplier = 1f;

            for (int i = 0; i < activeRelics.Count; i++)
            {
                RelicSO relic = activeRelics[i];
                if (relic != null && relic.TriggerKind == RelicTriggerKind.OnCommandExecuted)
                {
                    staminaBonus += relic.Effect.StaminaDeltaBonus;
                    skillBonus += relic.Effect.SkillDeltaBonus;
                    mentalBonus += relic.Effect.MentalDeltaBonus;
                    costMultiplier *= relic.Effect.StaminaCostMultiplier;
                }
            }

            int newStaminaDelta = baseEffect.StaminaDelta + staminaBonus;
            int newSkillDelta = baseEffect.SkillDelta + skillBonus;
            int newMentalDelta = baseEffect.MentalDelta + mentalBonus;
            int newCost = Mathf.RoundToInt(baseEffect.StaminaCost * costMultiplier);

            return new CommandEffect(newStaminaDelta, newSkillDelta, newMentalDelta, Mathf.Max(0, newCost));
        }

        /// <summary>
        /// ターン終了時に発動するレリック（OnTurnEnd）の効果を適用した新しい GameState を返す。
        /// </summary>
        public GameState ApplyTurnEndRelics(
            GameState state,
            IReadOnlyList<RelicSO> activeRelics,
            GameRulesSO rules)
        {
            if (state == null || activeRelics == null || activeRelics.Count == 0 || rules == null)
            {
                return state;
            }

            int staminaBonus = 0;
            int skillBonus = 0;
            int mentalBonus = 0;

            for (int i = 0; i < activeRelics.Count; i++)
            {
                RelicSO relic = activeRelics[i];
                if (relic != null && relic.TriggerKind == RelicTriggerKind.OnTurnEnd)
                {
                    staminaBonus += relic.Effect.StaminaDeltaBonus;
                    skillBonus += relic.Effect.SkillDeltaBonus;
                    mentalBonus += relic.Effect.MentalDeltaBonus;
                }
            }

            int clampedStamina = TurnRules.Clamp(state.Stamina + staminaBonus, rules.ParamMin, rules.ParamMax);
            int clampedSkill = TurnRules.Clamp(state.Skill + skillBonus, rules.ParamMin, rules.ParamMax);
            int clampedMental = TurnRules.Clamp(state.Mental + mentalBonus, rules.ParamMin, rules.ParamMax);

            return state with
            {
                Stamina = clampedStamina,
                Skill = clampedSkill,
                Mental = clampedMental
            };
        }
    }
}
