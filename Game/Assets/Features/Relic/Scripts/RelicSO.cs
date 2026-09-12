// SPDX-AI-Disclosure: ai-generated
using UnityEngine;

namespace Game.Features.Relic
{
    /// <summary>
    /// レリック1件の定義データ（ID、表示名、説明文、発動タイミング、効果値）。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Relic/Relic", fileName = "Relic")]
    public class RelicSO : ScriptableObject
    {
        [SerializeField] private int _relicId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private RelicTriggerKind _triggerKind;
        [SerializeField] private int _staminaDeltaBonus;
        [SerializeField] private int _skillDeltaBonus;
        [SerializeField] private int _mentalDeltaBonus;
        [SerializeField] private float _staminaCostMultiplier = 1f;

        public int RelicId => _relicId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public RelicTriggerKind TriggerKind => _triggerKind;

        public RelicEffect Effect =>
            new RelicEffect(_staminaDeltaBonus, _skillDeltaBonus, _mentalDeltaBonus, _staminaCostMultiplier);
    }
}
