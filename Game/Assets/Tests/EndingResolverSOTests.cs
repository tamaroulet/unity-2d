// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Ending;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EndingResolverSOTests
    {
        private EndingResolverSO _resolver;
        private readonly List<Object> _createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _resolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(_resolver);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        private EndingRulesSO CreateRules(
            int skillWeight,
            int mentalWeight,
            int staminaWeight,
            int threshold,
            TrackedParameter[] priorityOrder = null)
        {
            EndingRulesSO rules = EndingRulesSOFactory.Create(
                skillWeight, mentalWeight, staminaWeight, threshold, priorityOrder);
            _createdObjects.Add(rules);
            return rules;
        }

        [Test]
        public void ReturnsTrueEndingWhenScoreEqualsThreshold()
        {
            EndingRulesSO rules = CreateRules(2, 1, 1, 250);
            GameState state = new GameState { CurrentTurn = 0, Stamina = 20, Skill = 100, Mental = 30 };

            EndingKind result = _resolver.Resolve(state, rules);

            Assert.AreEqual(EndingKind.True, result);
        }

        [Test]
        public void DoesNotReturnTrueEndingWhenScoreIsOneBelowThreshold()
        {
            EndingRulesSO rules = CreateRules(2, 1, 1, 250);
            GameState state = new GameState { CurrentTurn = 0, Stamina = 19, Skill = 100, Mental = 30 };

            EndingKind result = _resolver.Resolve(state, rules);

            Assert.AreNotEqual(EndingKind.True, result);
        }

        [Test]
        public void ReturnsSkillEndingWhenSkillIsMax()
        {
            EndingRulesSO rules = CreateRules(2, 1, 1, 250);
            GameState state = new GameState { CurrentTurn = 0, Stamina = 10, Skill = 50, Mental = 10 };

            EndingKind result = _resolver.Resolve(state, rules);

            Assert.AreEqual(EndingKind.Skill, result);
        }

        [Test]
        public void ReturnsMentalEndingWhenMentalIsMax()
        {
            EndingRulesSO rules = CreateRules(2, 1, 1, 250);
            GameState state = new GameState { CurrentTurn = 0, Stamina = 10, Skill = 10, Mental = 50 };

            EndingKind result = _resolver.Resolve(state, rules);

            Assert.AreEqual(EndingKind.Mental, result);
        }

        [Test]
        public void ReturnsStaminaEndingWhenStaminaIsMax()
        {
            EndingRulesSO rules = CreateRules(2, 1, 1, 250);
            GameState state = new GameState { CurrentTurn = 0, Stamina = 50, Skill = 10, Mental = 10 };

            EndingKind result = _resolver.Resolve(state, rules);

            Assert.AreEqual(EndingKind.Stamina, result);
        }

        [Test]
        public void ReturnsSkillEndingWhenSkillAndMentalTieForMaxAccordingToPriorityOrder()
        {
            EndingRulesSO rules = CreateRules(2, 1, 1, 250);
            GameState state = new GameState { CurrentTurn = 0, Stamina = 10, Skill = 50, Mental = 50 };

            EndingKind result = _resolver.Resolve(state, rules);

            Assert.AreEqual(EndingKind.Skill, result);
        }

        [Test]
        public void ChangingWeightsChangesResolvedEnding()
        {
            GameState state = new GameState { CurrentTurn = 0, Stamina = 20, Skill = 100, Mental = 30 };
            EndingRulesSO defaultWeightRules = CreateRules(2, 1, 1, 250);
            EndingRulesSO reducedSkillWeightRules = CreateRules(1, 1, 1, 250);

            EndingKind resultWithDefaultWeights = _resolver.Resolve(state, defaultWeightRules);
            EndingKind resultWithReducedSkillWeight = _resolver.Resolve(state, reducedSkillWeightRules);

            Assert.AreEqual(EndingKind.True, resultWithDefaultWeights);
            Assert.AreEqual(EndingKind.Skill, resultWithReducedSkillWeight);
        }
    }
}
