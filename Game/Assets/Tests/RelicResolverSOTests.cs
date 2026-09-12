// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Command;
using Game.Features.Relic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class RelicResolverSOTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private RelicResolverSO _resolver;
        private GameRulesSO _rules;

        [SetUp]
        public void SetUp()
        {
            _resolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(_resolver);

            _rules = GameRulesSOFactory.Create(
                paramMin: 0,
                paramMax: 100,
                initialStamina: 100,
                initialSkill: 0,
                initialMental: 50,
                startTurn: 1,
                maxTurn: 24);
            _createdObjects.Add(_rules);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object createdObject in _createdObjects)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ApplyTurnStartRelics_AppliesStaminaAndSkillBonusesAndClamps()
        {
            GameState state = new GameState { CurrentTurn = 1, Stamina = 95, Skill = 10, Mental = 50 };
            RelicSO relic = CreateRelic(
                relicId: 1,
                triggerKind: RelicTriggerKind.OnTurnStart,
                staminaBonus: 10,
                skillBonus: 5,
                mentalBonus: 0);

            GameState result = _resolver.ApplyTurnStartRelics(state, new[] { relic }, _rules);

            Assert.AreEqual(100, result.Stamina); // 95 + 10 = 105 -> clamped to 100
            Assert.AreEqual(15, result.Skill);   // 10 + 5 = 15
            Assert.AreEqual(50, result.Mental);
        }

        [Test]
        public void ApplyTurnStartRelics_WithNoRelics_ReturnsOriginalState()
        {
            GameState state = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 10, Mental = 50 };

            GameState result = _resolver.ApplyTurnStartRelics(state, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(50, result.Stamina);
            Assert.AreEqual(10, result.Skill);
        }

        [Test]
        public void ModifyCommandEffect_AppliesDeltaBonusesAndCostMultiplier()
        {
            CommandEffect baseEffect = new CommandEffect(staminaDelta: -20, skillDelta: 10, mentalDelta: -5, staminaCost: 20);
            RelicSO relic = CreateRelic(
                relicId: 2,
                triggerKind: RelicTriggerKind.OnCommandExecuted,
                staminaBonus: 5,
                skillBonus: 5,
                mentalBonus: 0,
                costMultiplier: 0.5f);

            CommandEffect modified = _resolver.ModifyCommandEffect(baseEffect, new[] { relic });

            Assert.AreEqual(-15, modified.StaminaDelta); // -20 + 5
            Assert.AreEqual(15, modified.SkillDelta);   // 10 + 5
            Assert.AreEqual(-5, modified.MentalDelta);
            Assert.AreEqual(10, modified.StaminaCost);  // 20 * 0.5 = 10
        }

        [Test]
        public void ModifyCommandEffect_StacksMultipleRelics()
        {
            CommandEffect baseEffect = new CommandEffect(staminaDelta: -20, skillDelta: 10, mentalDelta: 0, staminaCost: 20);
            RelicSO relicA = CreateRelic(1, RelicTriggerKind.OnCommandExecuted, 0, 5, 0, 0.8f);
            RelicSO relicB = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, 0, 5, 2, 1.0f);

            CommandEffect modified = _resolver.ModifyCommandEffect(baseEffect, new[] { relicA, relicB });

            Assert.AreEqual(20, modified.SkillDelta);  // 10 + 5 + 5
            Assert.AreEqual(2, modified.MentalDelta);  // 0 + 2
            Assert.AreEqual(16, modified.StaminaCost); // 20 * 0.8 = 16
        }

        [Test]
        public void ApplyTurnEndRelics_AppliesMentalBonusAndClamps()
        {
            GameState state = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 20, Mental = 40 };
            RelicSO relic = CreateRelic(
                relicId: 3,
                triggerKind: RelicTriggerKind.OnTurnEnd,
                staminaBonus: 0,
                skillBonus: 0,
                mentalBonus: 10);

            GameState result = _resolver.ApplyTurnEndRelics(state, new[] { relic }, _rules);

            Assert.AreEqual(50, result.Mental); // 40 + 10 = 50
        }

        [Test]
        public void RelicCatalogSO_FindById_ReturnsMatchingRelic()
        {
            RelicSO relicA = CreateRelic(10, RelicTriggerKind.OnTurnStart, 5, 0, 0);
            RelicSO relicB = CreateRelic(20, RelicTriggerKind.OnCommandExecuted, 0, 5, 0);
            RelicCatalogSO catalog = ScriptableObject.CreateInstance<RelicCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "_relics", new List<RelicSO> { relicA, relicB });

            RelicSO found = catalog.FindById(20);

            Assert.IsNotNull(found);
            Assert.AreEqual(20, found.RelicId);
        }

        [Test]
        public void RelicCatalogSO_FindById_ReturnsNullWhenNotFound()
        {
            RelicCatalogSO catalog = ScriptableObject.CreateInstance<RelicCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "_relics", new List<RelicSO>());

            RelicSO found = catalog.FindById(999);

            Assert.IsNull(found);
        }

        private RelicSO CreateRelic(
            int relicId,
            RelicTriggerKind triggerKind,
            int staminaBonus,
            int skillBonus,
            int mentalBonus,
            float costMultiplier = 1f)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            SetField(relic, "_relicId", relicId);
            SetField(relic, "_displayName", $"Relic_{relicId}");
            SetField(relic, "_description", $"Description of {relicId}");
            SetField(relic, "_triggerKind", triggerKind);
            SetField(relic, "_staminaDeltaBonus", staminaBonus);
            SetField(relic, "_skillDeltaBonus", skillBonus);
            SetField(relic, "_mentalDeltaBonus", mentalBonus);
            SetField(relic, "_staminaCostMultiplier", costMultiplier);
            _createdObjects.Add(relic);
            return relic;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new System.InvalidOperationException(
                    $"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            }

            field.SetValue(target, value);
        }
    }
}
