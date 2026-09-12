// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Features.Ending
{
    /// <summary>
    /// エンディング判定に用いる重み・閾値・同値時の優先順を保持する。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Ending/EndingRules", fileName = "EndingRules")]
    public class EndingRulesSO : ScriptableObject
    {
        [SerializeField] private int _skillWeight = 2;
        [SerializeField] private int _mentalWeight = 1;
        [SerializeField] private int _staminaWeight = 1;
        [SerializeField] private int _threshold = 250;

        [SerializeField]
        private TrackedParameter[] _priorityOrder =
        {
            TrackedParameter.Skill,
            TrackedParameter.Mental,
            TrackedParameter.Stamina
        };

        public int SkillWeight => _skillWeight;

        public int MentalWeight => _mentalWeight;

        public int StaminaWeight => _staminaWeight;

        public int Threshold => _threshold;

        public IReadOnlyList<TrackedParameter> PriorityOrder => _priorityOrder;
    }
}
