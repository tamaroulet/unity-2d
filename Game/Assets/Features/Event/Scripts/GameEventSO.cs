// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using UnityEngine;

namespace Game.Features.Event
{
    /// <summary>
    /// イベント1件の発火条件と効果を保持する。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Event/GameEvent", fileName = "GameEvent")]
    public class GameEventSO : ScriptableObject
    {
        [SerializeField] private int _eventId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _body;
        [SerializeField] private EventTriggerKind _triggerKind;
        [SerializeField] private int _triggerTurn;
        [SerializeField] private TrackedParameter _targetParameter;
        [SerializeField] private int _threshold;
        [SerializeField] private int _priority;
        [SerializeField] private int _staminaDelta;
        [SerializeField] private int _skillDelta;
        [SerializeField] private int _mentalDelta;

        public int EventId => _eventId;

        public string DisplayName => _displayName;

        public string Body => _body;

        public EventTriggerKind TriggerKind => _triggerKind;

        public int TriggerTurn => _triggerTurn;

        public TrackedParameter TargetParameter => _targetParameter;

        public int Threshold => _threshold;

        public int Priority => _priority;

        /// <summary>
        /// 自身のフィールドから組み立てた CommandEffect を返す。StaminaCost は常に 0。
        /// </summary>
        public CommandEffect Effect => new CommandEffect(_staminaDelta, _skillDelta, _mentalDelta, 0);
    }
}
