// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Relic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class AutoBattleResolverTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private AutoBattleResolverSO _resolver;
        private GameRulesSO _rules;

        [SetUp]
        public void SetUp()
        {
            _resolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
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

        // --- Attack ---------------------------------------------------

        [Test]
        public void ResolveTurn_Attack_DealsSkillDamageAndCostsStaminaThenBossCounterAttacks()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 1, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 1, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(20, result.DamageDealtToBoss);
            Assert.AreEqual(80, result.BossState.CurrentHp);
            Assert.AreEqual(BossActionKind.Attack, result.BossActionTaken);
            Assert.AreEqual(10, result.StaminaDamageTaken); // AttackPower 20 mitigated 50% by Mental 50/100
            Assert.AreEqual(0, result.MentalDamageTaken);
            Assert.AreEqual(75, result.PlayerState.Stamina); // 100 - 15(cost) - 10(boss attack)
            Assert.AreEqual(50, result.PlayerState.Mental);
            Assert.AreEqual(2, result.BossState.BattleTurn);
            Assert.AreEqual(BattleOutcomeKind.InProgress, result.Outcome);
        }

        [Test]
        public void ResolveTurn_Attack_DamageIsAbsorbedByShieldBeforeHp()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 2, currentHp: 100, maxHp: 100, shield: 15, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 2, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.MentalPressure });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(20, result.DamageDealtToBoss); // raw damage, before shield absorption
            Assert.AreEqual(0, result.BossState.Shield);   // 15 shield fully absorbed
            Assert.AreEqual(95, result.BossState.CurrentHp); // remaining 5 damage after shield
            Assert.AreEqual(15, result.MentalDamageTaken);
            Assert.AreEqual(35, result.PlayerState.Mental); // 50 - 15
        }

        [Test]
        public void ResolveTurn_Attack_SkillIsBoostedByRelicSkillDeltaBonus()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 3, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 3, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Guard });
            RelicSO relic = CreateRelic(
                relicId: 1, triggerKind: RelicTriggerKind.OnCommandExecuted,
                staminaBonus: 0, skillBonus: 10, mentalBonus: 0, costMultiplier: 0.6f);

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, new[] { relic }, _rules);

            Assert.AreEqual(30, result.DamageDealtToBoss); // (20 + 10) skill
            Assert.AreEqual(70, result.BossState.CurrentHp);
            Assert.AreEqual(91, result.PlayerState.Stamina); // 100 - round(15 * 0.6) = 100 - 9
        }

        [Test]
        public void ResolveTurn_Attack_StacksMultipleRelicBonusesAndCostMultipliers()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 10, Mental = 50 };
            BossState boss = CreateBossState(bossId: 4, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 4, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Guard });
            RelicSO relicA = CreateRelic(1, RelicTriggerKind.OnCommandExecuted, 0, 5, 0, 0.8f);
            RelicSO relicB = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, 0, 5, 0, 1.0f);

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, new[] { relicA, relicB }, _rules);

            Assert.AreEqual(20, result.DamageDealtToBoss); // 10 + 5 + 5
            Assert.AreEqual(88, result.PlayerState.Stamina); // 100 - round(15 * 0.8 * 1.0) = 100 - 12
        }

        [Test]
        public void ResolveTurn_Attack_IgnoresRelicsWithNonCommandExecutedTrigger()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 5, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 5, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Guard });
            RelicSO relic = CreateRelic(
                relicId: 9, triggerKind: RelicTriggerKind.OnTurnStart,
                staminaBonus: 0, skillBonus: 100, mentalBonus: 0, costMultiplier: 1f);

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, new[] { relic }, _rules);

            Assert.AreEqual(20, result.DamageDealtToBoss); // OnTurnStart relic must not apply
            Assert.AreEqual(85, result.PlayerState.Stamina); // 100 - 15, no cost multiplier change
        }

        // --- Defend / MentalFocus --------------------------------------

        [Test]
        public void ResolveTurn_Defend_RecoversStaminaClampedAtMaxAndBossGuardsSimultaneously()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 95, Skill = 0, Mental = 50 };
            BossState boss = CreateBossState(bossId: 6, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 6, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Guard });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Defend, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(100, result.PlayerState.Stamina); // 95 + 20 = 115 -> clamped to 100
            Assert.AreEqual(50, result.PlayerState.Mental);
            Assert.AreEqual(10, result.BossState.Shield); // Guard adds GuardShieldAmount
            Assert.AreEqual(0, result.DamageDealtToBoss);
        }

        [Test]
        public void ResolveTurn_MentalFocus_RecoversMentalClampedAtMaxAndNegatesBossAttackAtFullShield()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 90 };
            BossState boss = CreateBossState(bossId: 7, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 7, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.MentalFocus, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(100, result.PlayerState.Mental); // 90 + 20 = 110 -> clamped to 100
            Assert.AreEqual(100, result.PlayerState.Stamina); // Mental at MentalShieldFullValue -> 0 damage
            Assert.AreEqual(0, result.StaminaDamageTaken);
        }

        // --- Boss actions -----------------------------------------------

        [Test]
        public void ResolveTurn_BossMentalPressure_DamagesMentalDirectlyAndClamps()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 5 };
            BossState boss = CreateBossState(bossId: 8, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 8, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.MentalPressure });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Defend, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(0, result.PlayerState.Mental); // 5 - 15 -> clamped to 0
            Assert.AreEqual(15, result.MentalDamageTaken);
        }

        [Test]
        public void ResolveTurn_BossSpecialAttack_DealsMultipliedDamageMitigatedByMental()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 50, Skill = 0, Mental = 50 };
            BossState boss = CreateBossState(bossId: 9, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 9, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.SpecialAttack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Defend, bossData, System.Array.Empty<RelicSO>(), _rules);

            // Defend: 50 + 20 = 70. SpecialAttack raw = 20 * 2 = 40, mitigated 50% by Mental 50/100 -> 20.
            Assert.AreEqual(20, result.StaminaDamageTaken);
            Assert.AreEqual(50, result.PlayerState.Stamina); // 70 - 20
        }

        [Test]
        public void ResolveTurn_BossActionCyclesThroughPatternByBattleTurn()
        {
            BossSO bossData = CreateBoss(
                bossId: 10, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f,
                actionPattern: new[]
                {
                    BossActionKind.Attack, BossActionKind.MentalPressure, BossActionKind.Guard, BossActionKind.SpecialAttack
                });

            Assert.AreEqual(BossActionKind.Attack, bossData.GetActionForTurn(1));
            Assert.AreEqual(BossActionKind.MentalPressure, bossData.GetActionForTurn(2));
            Assert.AreEqual(BossActionKind.Guard, bossData.GetActionForTurn(3));
            Assert.AreEqual(BossActionKind.SpecialAttack, bossData.GetActionForTurn(4));
            Assert.AreEqual(BossActionKind.Attack, bossData.GetActionForTurn(5)); // wraps around
        }

        [Test]
        public void ResolveTurn_BossActionFallsBackToAttackWhenPatternIsEmpty()
        {
            BossSO bossData = CreateBoss(
                bossId: 11, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: System.Array.Empty<BossActionKind>());

            Assert.AreEqual(BossActionKind.Attack, bossData.GetActionForTurn(1));
        }

        // --- Win / loss determination -----------------------------------

        [Test]
        public void ResolveTurn_PlayerAttackDefeatsBoss_ReturnsVictoryWithoutBossCounterAttack()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 12, currentHp: 15, maxHp: 100, shield: 0, battleTurn: 3);
            BossSO bossData = CreateBoss(
                bossId: 12, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(BattleOutcomeKind.Victory, result.Outcome);
            Assert.AreEqual(0, result.BossState.CurrentHp);
            Assert.IsNull(result.BossActionTaken);
            Assert.AreEqual(0, result.StaminaDamageTaken); // boss never got to act
            Assert.AreEqual(85, result.PlayerState.Stamina); // only the attack's own AP cost applied
            Assert.AreEqual(3, result.BossState.BattleTurn); // not advanced; boss did not act
        }

        [Test]
        public void ResolveTurn_BossAttackDefeatsPlayer_ReturnsDefeat()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 15, Skill = 0, Mental = 0 };
            BossState boss = CreateBossState(bossId: 13, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 13, maxHp: 100, attackPower: 40, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Defend, bossData, System.Array.Empty<RelicSO>(), _rules);

            // Defend: 15 + 20 = 35. Boss Attack: 40 raw, 0% mitigation (Mental 0) -> 35 - 40 = -5 -> clamped to 0.
            Assert.AreEqual(BattleOutcomeKind.Defeat, result.Outcome);
            Assert.AreEqual(0, result.PlayerState.Stamina);
            Assert.AreEqual(BossActionKind.Attack, result.BossActionTaken); // boss did act this turn
        }

        [Test]
        public void ResolveTurn_PlayerOverspendsStaminaOnAttack_ReturnsDefeatWithoutBossAction()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 10, Skill = 5, Mental = 50 };
            BossState boss = CreateBossState(bossId: 14, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 14, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(BattleOutcomeKind.Defeat, result.Outcome);
            Assert.AreEqual(0, result.PlayerState.Stamina); // 10 - 15 -> clamped to 0
            Assert.IsNull(result.BossActionTaken); // player collapsed before the boss could act
            Assert.AreEqual(5, result.DamageDealtToBoss); // the attack still lands
            Assert.AreEqual(95, result.BossState.CurrentHp);
            Assert.AreEqual(1, result.BossState.BattleTurn); // not advanced
        }

        [Test]
        public void ResolveTurn_AlreadyDefeatedBoss_ReturnsUnchangedStateAsVictory()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 15, currentHp: 0, maxHp: 100, shield: 0, battleTurn: 5);
            BossSO bossData = CreateBoss(
                bossId: 15, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(BattleOutcomeKind.Victory, result.Outcome);
            Assert.IsNull(result.BossActionTaken);
            Assert.AreSame(player, result.PlayerState);
            Assert.AreSame(boss, result.BossState);
        }

        [Test]
        public void ResolveTurn_AlreadyDefeatedPlayer_ReturnsUnchangedStateAsDefeat()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 0, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 16, currentHp: 50, maxHp: 100, shield: 0, battleTurn: 5);
            BossSO bossData = CreateBoss(
                bossId: 16, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(BattleOutcomeKind.Defeat, result.Outcome);
            Assert.IsNull(result.BossActionTaken);
            Assert.AreSame(player, result.PlayerState);
            Assert.AreSame(boss, result.BossState);
        }

        // --- Immutability -------------------------------------------------

        [Test]
        public void ResolveTurn_DoesNotMutateOriginalPlayerOrBossStateInstances()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(bossId: 17, currentHp: 100, maxHp: 100, shield: 0, battleTurn: 1);
            BossSO bossData = CreateBoss(
                bossId: 17, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BattleTurnResult result = _resolver.ResolveTurn(
                player, boss, BattlePlayerActionKind.Attack, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(100, player.Stamina);
            Assert.AreEqual(100, boss.CurrentHp);
            Assert.AreEqual(1, boss.BattleTurn);
            Assert.IsFalse(ReferenceEquals(player, result.PlayerState));
            Assert.IsFalse(ReferenceEquals(boss, result.BossState));
        }

        // --- BossSO / BossCatalogSO ----------------------------------------

        [Test]
        public void BossSO_CreateInitialState_BuildsFullHpStateAtBattleTurnOne()
        {
            BossSO bossData = CreateBoss(
                bossId: 20, maxHp: 250, attackPower: 30, mentalPressurePower: 20, guardShieldAmount: 15,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            BossState initial = bossData.CreateInitialState();

            Assert.AreEqual(20, initial.BossId);
            Assert.AreEqual(250, initial.CurrentHp);
            Assert.AreEqual(250, initial.MaxHp);
            Assert.AreEqual(0, initial.Shield);
            Assert.AreEqual(1, initial.BattleTurn);
            Assert.IsFalse(initial.IsDefeated);
        }

        [Test]
        public void BossCatalogSO_FindById_ReturnsMatchingBoss()
        {
            BossSO bossA = CreateBoss(
                bossId: 30, maxHp: 100, attackPower: 20, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });
            BossSO bossB = CreateBoss(
                bossId: 31, maxHp: 250, attackPower: 30, mentalPressurePower: 20, guardShieldAmount: 15,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });
            BossCatalogSO catalog = ScriptableObject.CreateInstance<BossCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "_bosses", new List<BossSO> { bossA, bossB });

            BossSO found = catalog.FindById(31);

            Assert.IsNotNull(found);
            Assert.AreEqual(31, found.BossId);
        }

        [Test]
        public void BossCatalogSO_FindById_ReturnsNullWhenNotFound()
        {
            BossCatalogSO catalog = ScriptableObject.CreateInstance<BossCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "_bosses", new List<BossSO>());

            BossSO found = catalog.FindById(999);

            Assert.IsNull(found);
        }

        // --- DecidePlayerAction & ResolveFullBattle -----------------------

        [Test]
        public void DecidePlayerAction_LowStamina_ChoosesDefend()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 10, Skill = 20, Mental = 50 };
            BossState boss = CreateBossState(1, 100, 100, 0, 1);
            BossSO bossData = CreateBoss(1, 100, 20, 15, 10, 2f, new[] { BossActionKind.Attack });

            BattlePlayerActionKind action = _resolver.DecidePlayerAction(player, boss, bossData);

            Assert.AreEqual(BattlePlayerActionKind.Defend, action);
        }

        [Test]
        public void DecidePlayerAction_SufficientStaminaAndMental_ChoosesAttack()
        {
            GameState player = new GameState { CurrentTurn = 1, Stamina = 80, Skill = 20, Mental = 60 };
            BossState boss = CreateBossState(1, 100, 100, 0, 1);
            BossSO bossData = CreateBoss(1, 100, 20, 15, 10, 2f, new[] { BossActionKind.Attack });

            BattlePlayerActionKind action = _resolver.DecidePlayerAction(player, boss, bossData);

            Assert.AreEqual(BattlePlayerActionKind.Attack, action);
        }

        [Test]
        public void ResolveFullBattle_HighSkillBuild_AchievesVictoryWithinFewTurns()
        {
            GameState player = new GameState { CurrentTurn = 12, Stamina = 100, Skill = 35, Mental = 80 };
            BossSO bossData = CreateBoss(
                bossId: 1, maxHp: 100, attackPower: 15, mentalPressurePower: 10, guardShieldAmount: 0,
                specialAttackMultiplier: 1.5f, actionPattern: new[] { BossActionKind.Attack });

            FullBattleResult result = _resolver.ResolveFullBattle(player, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(BattleOutcomeKind.Victory, result.Outcome);
            Assert.IsTrue(result.FinalBossState.IsDefeated);
            Assert.AreEqual(0, result.FinalBossState.CurrentHp);
            Assert.IsTrue(result.TurnHistory.Count <= 4); // 35 damage per turn kills 100 HP in 3 turns
            Assert.IsTrue(result.FinalPlayerState.Stamina > 0);
        }

        [Test]
        public void ResolveFullBattle_LowSkillBuild_SuffersDefeatDueToExhaustion()
        {
            GameState player = new GameState { CurrentTurn = 12, Stamina = 30, Skill = 2, Mental = 10 };
            BossSO bossData = CreateBoss(
                bossId: 1, maxHp: 200, attackPower: 25, mentalPressurePower: 15, guardShieldAmount: 10,
                specialAttackMultiplier: 2f, actionPattern: new[] { BossActionKind.Attack });

            FullBattleResult result = _resolver.ResolveFullBattle(player, bossData, System.Array.Empty<RelicSO>(), _rules);

            Assert.AreEqual(BattleOutcomeKind.Defeat, result.Outcome);
            Assert.IsFalse(result.FinalBossState.IsDefeated);
            Assert.AreEqual(0, result.FinalPlayerState.Stamina);
        }

        // --- Test helpers -------------------------------------------------

        private static BossState CreateBossState(int bossId, int currentHp, int maxHp, int shield, int battleTurn)
        {
            return new BossState
            {
                BossId = bossId,
                CurrentHp = currentHp,
                MaxHp = maxHp,
                Shield = shield,
                BattleTurn = battleTurn
            };
        }

        private BossSO CreateBoss(
            int bossId,
            int maxHp,
            int attackPower,
            int mentalPressurePower,
            int guardShieldAmount,
            float specialAttackMultiplier,
            IReadOnlyList<BossActionKind> actionPattern)
        {
            BossSO boss = ScriptableObject.CreateInstance<BossSO>();
            SetField(boss, "_bossId", bossId);
            SetField(boss, "_bossName", $"Boss_{bossId}");
            SetField(boss, "_maxHp", maxHp);
            SetField(boss, "_attackPower", attackPower);
            SetField(boss, "_mentalPressurePower", mentalPressurePower);
            SetField(boss, "_guardShieldAmount", guardShieldAmount);
            SetField(boss, "_specialAttackMultiplier", specialAttackMultiplier);
            SetField(boss, "_actionPattern", new List<BossActionKind>(actionPattern));
            _createdObjects.Add(boss);
            return boss;
        }

        private RelicSO CreateRelic(
            int relicId,
            RelicTriggerKind triggerKind,
            int staminaBonus,
            int skillBonus,
            int mentalBonus,
            float costMultiplier)
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
