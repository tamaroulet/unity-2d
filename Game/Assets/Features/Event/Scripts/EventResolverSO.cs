// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using UnityEngine;

namespace Game.Features.Event
{
    /// <summary>
    /// GameState とイベントカタログから発火すべきイベントを判定し、効果を適用する
    /// 純粋関数としての ScriptableObject。static フィールド、Time、Random、DateTime、
    /// Debug は参照しない。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Event/EventResolver", fileName = "EventResolver")]
    public class EventResolverSO : ScriptableObject
    {
        public EventResult Resolve(GameState state, GameEventCatalogSO catalog)
        {
            GameEventSO selected = SelectEvent(state, catalog);

            if (selected == null)
            {
                return new EventResult
                {
                    State = state,
                    FiredEvent = null,
                    HasFired = false
                };
            }

            GameState appliedState = ApplyEvent(state, selected, catalog);

            return new EventResult
            {
                State = appliedState,
                FiredEvent = selected,
                HasFired = true
            };
        }

        /// <summary>
        /// 未発火かつ条件を満たすイベントのうち、優先度最大（同値なら ID 最小）の1件を選ぶ。
        /// </summary>
        private GameEventSO SelectEvent(GameState state, GameEventCatalogSO catalog)
        {
            GameEventSO selected = null;

            foreach (GameEventSO candidate in catalog.Events)
            {
                if (IsAlreadyFired(state, candidate))
                {
                    continue;
                }

                if (!IsConditionMet(state, candidate))
                {
                    continue;
                }

                if (selected == null
                    || candidate.Priority > selected.Priority
                    || (candidate.Priority == selected.Priority && candidate.EventId < selected.EventId))
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        private bool IsAlreadyFired(GameState state, GameEventSO gameEvent)
        {
            return (state.FiredEventMask & (1UL << gameEvent.EventId)) != 0;
        }

        private bool IsConditionMet(GameState state, GameEventSO gameEvent)
        {
            switch (gameEvent.TriggerKind)
            {
                case EventTriggerKind.TurnReached:
                    return state.CurrentTurn == gameEvent.TriggerTurn;
                case EventTriggerKind.ParameterBelow:
                    return GetParameterValue(state, gameEvent.TargetParameter) < gameEvent.Threshold;
                default:
                    return false;
            }
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

        /// <summary>
        /// 選ばれたイベントの効果を適用し、パラメータをクランプし、発火済みビットを立てる。
        /// </summary>
        private GameState ApplyEvent(GameState state, GameEventSO gameEvent, GameEventCatalogSO catalog)
        {
            CommandEffect effect = gameEvent.Effect;

            int stamina = TurnRules.Clamp(state.Stamina + effect.StaminaDelta, catalog.ParamMin, catalog.ParamMax);
            int skill = TurnRules.Clamp(state.Skill + effect.SkillDelta, catalog.ParamMin, catalog.ParamMax);
            int mental = TurnRules.Clamp(state.Mental + effect.MentalDelta, catalog.ParamMin, catalog.ParamMax);
            ulong firedEventMask = state.FiredEventMask | (1UL << gameEvent.EventId);

            return state with
            {
                Stamina = stamina,
                Skill = skill,
                Mental = mental,
                FiredEventMask = firedEventMask
            };
        }
    }
}
