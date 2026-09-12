// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class MetaPointResolverTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private MetaPointResolverSO _resolver;
        private Game.Features.Command.GameRulesSO _rules;

        [SetUp]
        public void SetUp()
        {
            _resolver = ScriptableObject.CreateInstance<MetaPointResolverSO>();
            _createdObjects.Add(_resolver);

            _rules = GameRulesSOFactory.Create(
                paramMin: 0,
                paramMax: 100,
                initialStamina: 50,
                initialSkill: 10,
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

        // ---------------------------------------------------------------
        // CalculateEarnedPoints
        // ---------------------------------------------------------------

        [Test]
        public void CalculateEarnedPoints_CombinesAllBonusesUsingDefaultFormula()
        {
            GameState finalState = new GameState { CurrentTurn = 12, Stamina = 40, Skill = 20, Mental = 30 };

            int earned = _resolver.CalculateEarnedPoints(finalState, isGameClear: true, bossDefeatedCount: 1);

            // 12*5 + 20*2 + 1*50 + 100 = 60 + 40 + 50 + 100 = 250
            Assert.AreEqual(250, earned);
        }

        [Test]
        public void CalculateEarnedPoints_WithoutGameClear_ExcludesClearBonus()
        {
            GameState finalState = new GameState { CurrentTurn = 12, Stamina = 40, Skill = 20, Mental = 30 };

            int earned = _resolver.CalculateEarnedPoints(finalState, isGameClear: false, bossDefeatedCount: 1);

            // 12*5 + 20*2 + 1*50 + 0 = 150
            Assert.AreEqual(150, earned);
        }

        [Test]
        public void CalculateEarnedPoints_WithZeroBossDefeatedCount_ExcludesBossBonus()
        {
            GameState finalState = new GameState { CurrentTurn = 5, Stamina = 40, Skill = 0, Mental = 30 };

            int earned = _resolver.CalculateEarnedPoints(finalState, isGameClear: false, bossDefeatedCount: 0);

            // 5*5 + 0*2 + 0*50 + 0 = 25
            Assert.AreEqual(25, earned);
        }

        [Test]
        public void CalculateEarnedPoints_WithNullFinalState_ReturnsZero()
        {
            int earned = _resolver.CalculateEarnedPoints(null, isGameClear: true, bossDefeatedCount: 5);

            Assert.AreEqual(0, earned);
        }

        [Test]
        public void CalculateEarnedPoints_WithNegativeTurnAndSkill_ClampsIndividualTermsToZero()
        {
            // CurrentTurn / Skill が負値になることは通常想定しないが、防御的にクランプされることを確認する。
            GameState finalState = new GameState { CurrentTurn = -3, Stamina = 40, Skill = -10, Mental = 30 };

            int earned = _resolver.CalculateEarnedPoints(finalState, isGameClear: false, bossDefeatedCount: 2);

            // max(0,-3)*5 + max(0,-10)*2 + 2*50 + 0 = 0 + 0 + 100 = 100
            Assert.AreEqual(100, earned);
        }

        [Test]
        public void CalculateEarnedPoints_WithNegativeBossDefeatedCount_ClampsToZero()
        {
            GameState finalState = new GameState { CurrentTurn = 10, Stamina = 40, Skill = 0, Mental = 30 };

            int earned = _resolver.CalculateEarnedPoints(finalState, isGameClear: false, bossDefeatedCount: -3);

            // 10*5 + 0 + max(0,-3)*50 + 0 = 50
            Assert.AreEqual(50, earned);
        }

        // ---------------------------------------------------------------
        // ApplyRunResult
        // ---------------------------------------------------------------

        [Test]
        public void ApplyRunResult_AddsEarnedPointsAndIncrementsRunCount()
        {
            MetaProfileState profile = new MetaProfileState
            {
                AvailableMetaPoints = 10,
                TotalEarnedMetaPoints = 100,
                TotalRunsCompleted = 3
            };

            MetaProfileState result = _resolver.ApplyRunResult(profile, 50);

            Assert.AreEqual(60, result.AvailableMetaPoints);
            Assert.AreEqual(150, result.TotalEarnedMetaPoints);
            Assert.AreEqual(4, result.TotalRunsCompleted);
        }

        [Test]
        public void ApplyRunResult_WithNegativeEarnedPoints_DoesNotReducePoints()
        {
            MetaProfileState profile = new MetaProfileState
            {
                AvailableMetaPoints = 10,
                TotalEarnedMetaPoints = 100,
                TotalRunsCompleted = 3
            };

            MetaProfileState result = _resolver.ApplyRunResult(profile, -50);

            Assert.AreEqual(10, result.AvailableMetaPoints);
            Assert.AreEqual(100, result.TotalEarnedMetaPoints);
            Assert.AreEqual(4, result.TotalRunsCompleted); // 周回自体は完了しているためカウントは進む
        }

        [Test]
        public void ApplyRunResult_WithNullProfile_ReturnsNull()
        {
            MetaProfileState result = _resolver.ApplyRunResult(null, 50);

            Assert.IsNull(result);
        }

        [Test]
        public void ApplyRunResult_DoesNotMutateOriginalProfile()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 10 };

            _resolver.ApplyRunResult(profile, 50);

            Assert.AreEqual(10, profile.AvailableMetaPoints);
        }

        // ---------------------------------------------------------------
        // ApplyUnlock
        // ---------------------------------------------------------------

        [Test]
        public void ApplyUnlock_WithSufficientPoints_DeductsCostAndAddsId()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 100, UnlockedIds = new[] { 5 } };
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Stat_Stamina_01", cost: 30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(profile, unlock);

            Assert.IsTrue(success);
            Assert.AreEqual(70, newProfile.AvailableMetaPoints);
            CollectionAssert.AreEquivalent(new[] { 5, 1 }, newProfile.UnlockedIds);
        }

        [Test]
        public void ApplyUnlock_WithExactCost_ReducesAvailablePointsToZero()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 30 };
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Stat_Stamina_01", cost: 30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(profile, unlock);

            Assert.IsTrue(success);
            Assert.AreEqual(0, newProfile.AvailableMetaPoints);
        }

        [Test]
        public void ApplyUnlock_WithInsufficientPoints_FailsAndLeavesProfileUnchanged()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 10 };
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Stat_Stamina_01", cost: 30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(profile, unlock);

            Assert.IsFalse(success);
            Assert.AreEqual(10, newProfile.AvailableMetaPoints);
            Assert.AreEqual(0, newProfile.UnlockedIds.Count);
        }

        [Test]
        public void ApplyUnlock_WhenAlreadyUnlocked_FailsAndDoesNotDoubleCharge()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 100, UnlockedIds = new[] { 1 } };
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Stat_Stamina_01", cost: 30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(profile, unlock);

            Assert.IsFalse(success);
            Assert.AreEqual(100, newProfile.AvailableMetaPoints);
            Assert.AreEqual(1, newProfile.UnlockedIds.Count);
        }

        [Test]
        public void ApplyUnlock_WithNegativeCostData_IsRejected()
        {
            // Cost が負値のデータは、購入のたびに MetaPoints を増殖させる経路になるため拒否する。
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 100 };
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Broken", cost: -30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(profile, unlock);

            Assert.IsFalse(success);
            Assert.AreEqual(100, newProfile.AvailableMetaPoints);
            Assert.AreEqual(0, newProfile.UnlockedIds.Count);
        }

        [Test]
        public void ApplyUnlock_WithNullUnlock_FailsAndReturnsOriginalProfile()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 100 };

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(profile, null);

            Assert.IsFalse(success);
            Assert.AreSame(profile, newProfile);
        }

        [Test]
        public void ApplyUnlock_WithNullProfile_FailsGracefully()
        {
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Stat_Stamina_01", cost: 30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            (MetaProfileState newProfile, bool success) = _resolver.ApplyUnlock(null, unlock);

            Assert.IsFalse(success);
            Assert.IsNull(newProfile);
        }

        [Test]
        public void ApplyUnlock_DoesNotMutateOriginalProfileOnSuccess()
        {
            MetaProfileState profile = new MetaProfileState { AvailableMetaPoints = 100 };
            MetaUnlockSO unlock = CreateUnlock(1, "Unlock_Stat_Stamina_01", cost: 30, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 0);

            _resolver.ApplyUnlock(profile, unlock);

            Assert.AreEqual(100, profile.AvailableMetaPoints);
            Assert.AreEqual(0, profile.UnlockedIds.Count);
        }

        // ---------------------------------------------------------------
        // MetaUnlockCatalogSO.FindById
        // ---------------------------------------------------------------

        [Test]
        public void MetaUnlockCatalogSO_FindById_ReturnsMatchingUnlock()
        {
            MetaUnlockSO unlockA = CreateUnlock(10, "Unlock_A", 10, MetaUnlockKind.InitialStaminaBonus, 5, 0);
            MetaUnlockSO unlockB = CreateUnlock(20, "Unlock_B", 20, MetaUnlockKind.InitialSkillBonus, 5, 0);
            MetaUnlockCatalogSO catalog = CreateCatalog(unlockA, unlockB);

            MetaUnlockSO found = catalog.FindById(20);

            Assert.IsNotNull(found);
            Assert.AreEqual(20, found.UnlockId);
        }

        [Test]
        public void MetaUnlockCatalogSO_FindById_ReturnsNullWhenNotFound()
        {
            MetaUnlockCatalogSO catalog = CreateCatalog();

            MetaUnlockSO found = catalog.FindById(999);

            Assert.IsNull(found);
        }

        // ---------------------------------------------------------------
        // ApplyUnlockedStatBonuses
        // ---------------------------------------------------------------

        [Test]
        public void ApplyUnlockedStatBonuses_AggregatesAllThreeStatKindsAndClamps()
        {
            MetaUnlockSO staminaUnlock = CreateUnlock(1, "Unlock_Stamina", 10, MetaUnlockKind.InitialStaminaBonus, bonusValue: 60, targetRelicId: 0);
            MetaUnlockSO skillUnlock = CreateUnlock(2, "Unlock_Skill", 10, MetaUnlockKind.InitialSkillBonus, bonusValue: 15, targetRelicId: 0);
            MetaUnlockSO mentalUnlock = CreateUnlock(3, "Unlock_Mental", 10, MetaUnlockKind.InitialMentalBonus, bonusValue: 5, targetRelicId: 0);
            MetaUnlockCatalogSO catalog = CreateCatalog(staminaUnlock, skillUnlock, mentalUnlock);

            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1, 2, 3 } };
            GameState baseState = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 10, Mental = 50 };

            GameState result = _resolver.ApplyUnlockedStatBonuses(baseState, profile, catalog, _rules);

            Assert.AreEqual(100, result.Stamina); // 50 + 60 = 110 -> clamped to 100
            Assert.AreEqual(25, result.Skill);   // 10 + 15
            Assert.AreEqual(55, result.Mental);  // 50 + 5
        }

        [Test]
        public void ApplyUnlockedStatBonuses_IgnoresUnlockRelicKind()
        {
            MetaUnlockSO relicUnlock = CreateUnlock(1, "Unlock_Relic", 10, MetaUnlockKind.UnlockRelic, bonusValue: 999, targetRelicId: 7);
            MetaUnlockCatalogSO catalog = CreateCatalog(relicUnlock);
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1 } };
            GameState baseState = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 10, Mental = 50 };

            GameState result = _resolver.ApplyUnlockedStatBonuses(baseState, profile, catalog, _rules);

            Assert.AreEqual(50, result.Stamina);
            Assert.AreEqual(10, result.Skill);
            Assert.AreEqual(50, result.Mental);
        }

        [Test]
        public void ApplyUnlockedStatBonuses_WithDuplicateUnlockedIds_DoesNotDoubleCount()
        {
            // 保存データの手動改変等で UnlockedIds に重複が紛れ込んでも、二重加算しないことを保証する。
            MetaUnlockSO staminaUnlock = CreateUnlock(1, "Unlock_Stamina", 10, MetaUnlockKind.InitialStaminaBonus, bonusValue: 20, targetRelicId: 0);
            MetaUnlockCatalogSO catalog = CreateCatalog(staminaUnlock);
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1, 1, 1 } };
            GameState baseState = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 10, Mental = 50 };

            GameState result = _resolver.ApplyUnlockedStatBonuses(baseState, profile, catalog, _rules);

            Assert.AreEqual(70, result.Stamina); // 50 + 20（1回分のみ）
        }

        [Test]
        public void ApplyUnlockedStatBonuses_WithUnlockIdMissingFromCatalog_IsSkipped()
        {
            MetaUnlockCatalogSO catalog = CreateCatalog();
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 999 } };
            GameState baseState = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 10, Mental = 50 };

            GameState result = _resolver.ApplyUnlockedStatBonuses(baseState, profile, catalog, _rules);

            Assert.AreEqual(50, result.Stamina);
            Assert.AreEqual(10, result.Skill);
            Assert.AreEqual(50, result.Mental);
        }

        [Test]
        public void ApplyUnlockedStatBonuses_WithNullArguments_ReturnsBaseStateUnchanged()
        {
            GameState baseState = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 10, Mental = 50 };

            GameState result = _resolver.ApplyUnlockedStatBonuses(baseState, null, null, null);

            Assert.AreSame(baseState, result);
        }

        // ---------------------------------------------------------------
        // ResolveUnlockedRelics
        // ---------------------------------------------------------------

        [Test]
        public void ResolveUnlockedRelics_ReturnsRelicsMatchingTargetRelicId()
        {
            MetaUnlockSO relicUnlock = CreateUnlock(1, "Unlock_Relic", 10, MetaUnlockKind.UnlockRelic, bonusValue: 0, targetRelicId: 42);
            MetaUnlockCatalogSO catalog = CreateCatalog(relicUnlock);
            RelicSO relic = CreateRelic(42);
            RelicCatalogSO relicCatalog = CreateRelicCatalog(relic);
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1 } };

            IReadOnlyList<RelicSO> result = _resolver.ResolveUnlockedRelics(profile, catalog, relicCatalog);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(42, result[0].RelicId);
        }

        [Test]
        public void ResolveUnlockedRelics_IgnoresNonRelicKindUnlocks()
        {
            MetaUnlockSO statUnlock = CreateUnlock(1, "Unlock_Stat", 10, MetaUnlockKind.InitialStaminaBonus, bonusValue: 10, targetRelicId: 42);
            MetaUnlockCatalogSO catalog = CreateCatalog(statUnlock);
            RelicSO relic = CreateRelic(42);
            RelicCatalogSO relicCatalog = CreateRelicCatalog(relic);
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1 } };

            IReadOnlyList<RelicSO> result = _resolver.ResolveUnlockedRelics(profile, catalog, relicCatalog);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ResolveUnlockedRelics_WithDuplicateTargetRelicId_ReturnsRelicOnlyOnce()
        {
            MetaUnlockSO unlockA = CreateUnlock(1, "Unlock_A", 10, MetaUnlockKind.UnlockRelic, bonusValue: 0, targetRelicId: 42);
            MetaUnlockSO unlockB = CreateUnlock(2, "Unlock_B", 10, MetaUnlockKind.UnlockRelic, bonusValue: 0, targetRelicId: 42);
            MetaUnlockCatalogSO catalog = CreateCatalog(unlockA, unlockB);
            RelicSO relic = CreateRelic(42);
            RelicCatalogSO relicCatalog = CreateRelicCatalog(relic);
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1, 2 } };

            IReadOnlyList<RelicSO> result = _resolver.ResolveUnlockedRelics(profile, catalog, relicCatalog);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void ResolveUnlockedRelics_WithMissingRelicInRelicCatalog_IsSkipped()
        {
            MetaUnlockSO relicUnlock = CreateUnlock(1, "Unlock_Relic", 10, MetaUnlockKind.UnlockRelic, bonusValue: 0, targetRelicId: 999);
            MetaUnlockCatalogSO catalog = CreateCatalog(relicUnlock);
            RelicCatalogSO relicCatalog = CreateRelicCatalog();
            MetaProfileState profile = new MetaProfileState { UnlockedIds = new[] { 1 } };

            IReadOnlyList<RelicSO> result = _resolver.ResolveUnlockedRelics(profile, catalog, relicCatalog);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ResolveUnlockedRelics_WithNullArguments_ReturnsEmptyList()
        {
            IReadOnlyList<RelicSO> result = _resolver.ResolveUnlockedRelics(null, null, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private MetaUnlockSO CreateUnlock(
            int unlockId,
            string unlockName,
            int cost,
            MetaUnlockKind kind,
            int bonusValue,
            int targetRelicId)
        {
            MetaUnlockSO unlock = ScriptableObject.CreateInstance<MetaUnlockSO>();
            SetField(unlock, "_unlockId", unlockId);
            SetField(unlock, "_unlockName", unlockName);
            SetField(unlock, "_cost", cost);
            SetField(unlock, "_kind", kind);
            SetField(unlock, "_bonusValue", bonusValue);
            SetField(unlock, "_targetRelicId", targetRelicId);
            _createdObjects.Add(unlock);
            return unlock;
        }

        private MetaUnlockCatalogSO CreateCatalog(params MetaUnlockSO[] unlocks)
        {
            MetaUnlockCatalogSO catalog = ScriptableObject.CreateInstance<MetaUnlockCatalogSO>();
            SetField(catalog, "_unlocks", new List<MetaUnlockSO>(unlocks));
            _createdObjects.Add(catalog);
            return catalog;
        }

        private RelicSO CreateRelic(int relicId)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            SetField(relic, "_relicId", relicId);
            SetField(relic, "_displayName", $"Relic_{relicId}");
            SetField(relic, "_description", $"Description of {relicId}");
            SetField(relic, "_triggerKind", RelicTriggerKind.OnTurnStart);
            SetField(relic, "_staminaDeltaBonus", 0);
            SetField(relic, "_skillDeltaBonus", 0);
            SetField(relic, "_mentalDeltaBonus", 0);
            SetField(relic, "_staminaCostMultiplier", 1f);
            _createdObjects.Add(relic);
            return relic;
        }

        private RelicCatalogSO CreateRelicCatalog(params RelicSO[] relics)
        {
            RelicCatalogSO catalog = ScriptableObject.CreateInstance<RelicCatalogSO>();
            SetField(catalog, "_relics", new List<RelicSO>(relics));
            _createdObjects.Add(catalog);
            return catalog;
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
