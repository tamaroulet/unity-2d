// SPDX-AI-Disclosure: ai-generated
using System;
using UnityEngine;

namespace Game.Features.Relic
{
    /// <summary>
    /// レリックが与えるパッシブ効果値（加算ボーナスおよび消費倍率）。
    /// </summary>
    [Serializable]
    public readonly struct RelicEffect
    {
        [SerializeField] private readonly int _staminaDeltaBonus;
        [SerializeField] private readonly int _skillDeltaBonus;
        [SerializeField] private readonly int _mentalDeltaBonus;
        [SerializeField] private readonly float _staminaCostMultiplier;

        public RelicEffect(
            int staminaDeltaBonus,
            int skillDeltaBonus,
            int mentalDeltaBonus,
            float staminaCostMultiplier = 1f)
        {
            _staminaDeltaBonus = staminaDeltaBonus;
            _skillDeltaBonus = skillDeltaBonus;
            _mentalDeltaBonus = mentalDeltaBonus;
            _staminaCostMultiplier = staminaCostMultiplier <= 0f ? 1f : staminaCostMultiplier;
        }

        public int StaminaDeltaBonus => _staminaDeltaBonus;

        public int SkillDeltaBonus => _skillDeltaBonus;

        public int MentalDeltaBonus => _mentalDeltaBonus;

        public float StaminaCostMultiplier => _staminaCostMultiplier <= 0f ? 1f : _staminaCostMultiplier;
    }
}
