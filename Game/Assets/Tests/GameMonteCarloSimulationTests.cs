// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [Explicit("PlayMode 曳光弾で置換予定")]
    public class GameMonteCarloSimulationTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _createdGameObjects)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _createdGameObjects.Clear();

            foreach (UnityEngine.Object obj in _createdObjects)
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void MonteCarlo_1000Runs_RandomPolicy_RunsWithoutExceptionsAndProducesValidOutcomes()
        {
            GameRulesSO rules = CreateRules();
            CommandDataSO rest = CreateCommand("Rest", staminaDelta: 20, skillDelta: 0, mentalDelta: 10, staminaCost: 0);
            CommandDataSO train = CreateCommand("Train", staminaDelta: -20, skillDelta: 8, mentalDelta: -5, staminaCost: 20);
            CommandDataSO special = CreateCommand("Special", staminaDelta: -35, skillDelta: 16, mentalDelta: -10, staminaCost: 35);
            CommandDataSO[] commands = { rest, train, special };

            RelicSO r1 = CreateRelic(1, RelicTriggerKind.OnTurnStart, 2, 0, 0);
            RelicSO r2 = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, 0, 3, 0);
            RelicCatalogSO catalog = CreateCatalog(r1, r2);
            RelicResolverSO relicResolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(relicResolver);

            int clearCount = 0;
            int gameOverCount = 0;
            int totalFinalSkill = 0;

            System.Random rand = new System.Random(42); // 固定シードで決定論的検証

            for (int run = 0; run < 1000; run++)
            {
                GameFlowController controller = CreateController(rules, catalog, relicResolver);
                controller.StartGame();

                // 24ターン分確実に完走するまで実行
                int guard = 0;
                while (controller.CurrentPhase != GamePhase.GameClear && controller.CurrentPhase != GamePhase.GameOver && guard++ < 300)
                {
                    if (controller.CurrentPhase == GamePhase.ShowingEvent)
                    {
                        controller.OnEventDismissed();
                    }

                    if (controller.CurrentPhase == GamePhase.ShowingRelicDraft)
                    {
                        controller.OnRelicAcquired(rand.Next(1, 3));
                    }

                    if (controller.CurrentPhase == GamePhase.WaitingInput)
                    {
                        CommandDataSO chosen = commands[rand.Next(commands.Length)];
                        int turnBefore = controller.CurrentState.CurrentTurn;
                        controller.ExecuteCommand(chosen);

                        // 実行不可（ターンが進まなかった）だった場合は休養
                        if (controller.CurrentPhase == GamePhase.WaitingInput && controller.CurrentState.CurrentTurn == turnBefore)
                        {
                            controller.ExecuteCommand(rest);
                        }
                    }
                }

                if (controller.CurrentPhase == GamePhase.GameClear)
                {
                    clearCount++;
                    totalFinalSkill += controller.CurrentState.Skill;
                }
                else if (controller.CurrentPhase == GamePhase.GameOver)
                {
                    gameOverCount++;
                }
                else
                {
                    if (run < 3)
                    {
                        Debug.LogError($"[MonteCarlo Diagnostic] Run {run} stopped at Phase: {controller.CurrentPhase}, Turn: {controller.CurrentState.CurrentTurn}, Stamina: {controller.CurrentState.Stamina}, Mental: {controller.CurrentState.Mental}, Guard: {guard}");
                    }
                }
            }

            Assert.AreEqual(1000, clearCount + gameOverCount, "1000 回のシミュレーションがすべて正常にクリアまたはゲームオーバーで終了すること");
            Debug.Log($"[MonteCarlo] 1,000 Runs Completed: Clear={clearCount}, GameOver={gameOverCount}, AvgSkillWhenCleared={(clearCount > 0 ? (float)totalFinalSkill / clearCount : 0):F1}");
        }

        [Test]
        public void MonteCarlo_1000Runs_WithBossBattle_RandomAndGreedyPolicies_VerifyStability()
        {
            GameRulesSO rules = CreateRules();
            CommandDataSO rest = CreateCommand("Rest", staminaDelta: 20, skillDelta: 0, mentalDelta: 10, staminaCost: 0);
            CommandDataSO train = CreateCommand("Train", staminaDelta: -20, skillDelta: 8, mentalDelta: -5, staminaCost: 20);
            CommandDataSO special = CreateCommand("Special", staminaDelta: -35, skillDelta: 16, mentalDelta: -10, staminaCost: 35);
            CommandDataSO[] commands = { rest, train, special };

            RelicSO r1 = CreateRelic(1, RelicTriggerKind.OnTurnStart, 2, 0, 0);
            RelicSO r2 = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, 0, 3, 0);
            RelicCatalogSO catalog = CreateCatalog(r1, r2);
            RelicResolverSO relicResolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(relicResolver);

            // Act 1〜4（Turn 6/12/18/24）に対応する BossId 1〜4 のカタログを組み立てる。
            BossCatalogSO bossCatalog = CreateFourActBossCatalog();

            AutoBattleResolverSO autoBattleResolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
            _createdObjects.Add(autoBattleResolver);

            int clearCount = 0;
            int gameOverCount = 0;
            System.Random rand = new System.Random(12345);

            for (int run = 0; run < 1000; run++)
            {
                GameFlowController controller = CreateController(rules, catalog, relicResolver);
                SetField(controller, "_bossCatalog", bossCatalog);
                SetField(controller, "_autoBattleResolver", autoBattleResolver);
                SetField(controller, "_bossBattleTurns", new List<int> { 6, 12, 18, 24 });

                controller.StartGame();

                // 24ターン分確実に完走するまで実行
                int guard = 0;
                while (controller.CurrentPhase != GamePhase.GameClear && controller.CurrentPhase != GamePhase.GameOver && guard++ < 300)
                {
                    if (controller.CurrentPhase == GamePhase.ShowingEvent)
                    {
                        controller.OnEventDismissed();
                    }

                    if (controller.CurrentPhase == GamePhase.ShowingRelicDraft)
                    {
                        // ボス撃破後のドラフトでレリック獲得（Act 1〜4 の撃破ごとに最大4回発生する）
                        controller.OnRelicAcquired(rand.Next(1, 3));
                    }

                    if (controller.CurrentPhase == GamePhase.WaitingInput)
                    {
                        CommandDataSO chosen = commands[rand.Next(commands.Length)];
                        int turnBefore = controller.CurrentState.CurrentTurn;
                        controller.ExecuteCommand(chosen);

                        if (controller.CurrentPhase == GamePhase.WaitingInput && controller.CurrentState.CurrentTurn == turnBefore)
                        {
                            controller.ExecuteCommand(rest);
                        }
                    }
                }

                if (controller.CurrentPhase == GamePhase.GameClear)
                {
                    clearCount++;
                }
                else if (controller.CurrentPhase == GamePhase.GameOver)
                {
                    gameOverCount++;
                }
            }

            Assert.AreEqual(1000, clearCount + gameOverCount, "Act 1〜4 のボス戦を含む1000回シミュレーションが全て正常終了すること");
            Debug.Log($"[MonteCarlo Boss] 1,000 Runs With Boss Battle (Act 1-4): Clear={clearCount}, GameOver={gameOverCount}");
        }

        [Test]
        public void MonteCarlo_1000Runs_WithMetaProgression_VerifiesPointAccumulationAndUnlockProgression()
        {
            GameRulesSO rules = CreateRules();
            CommandDataSO rest = CreateCommand("Rest", staminaDelta: 20, skillDelta: 0, mentalDelta: 10, staminaCost: 0);
            CommandDataSO train = CreateCommand("Train", staminaDelta: -20, skillDelta: 8, mentalDelta: -5, staminaCost: 20);
            CommandDataSO special = CreateCommand("Special", staminaDelta: -35, skillDelta: 16, mentalDelta: -10, staminaCost: 35);
            CommandDataSO[] commands = { rest, train, special };

            RelicSO r1 = CreateRelic(1, RelicTriggerKind.OnTurnStart, 2, 0, 0);
            RelicSO r2 = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, 0, 3, 0);
            RelicCatalogSO catalog = CreateCatalog(r1, r2);
            RelicResolverSO relicResolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(relicResolver);

            // Act 1〜4（Turn 6/12/18/24）に対応する BossId 1〜4 のカタログを組み立てる。
            BossCatalogSO bossCatalog = CreateFourActBossCatalog();

            AutoBattleResolverSO autoBattleResolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
            _createdObjects.Add(autoBattleResolver);

            MetaPointResolverSO metaResolver = ScriptableObject.CreateInstance<MetaPointResolverSO>();
            _createdObjects.Add(metaResolver);

            MetaUnlockSO uStamina = ScriptableObject.CreateInstance<MetaUnlockSO>();
            _createdObjects.Add(uStamina);
            SetField(uStamina, "_unlockId", 1);
            SetField(uStamina, "_cost", 50);
            SetField(uStamina, "_kind", MetaUnlockKind.InitialStaminaBonus);
            SetField(uStamina, "_bonusValue", 15);

            MetaUnlockSO uSkill = ScriptableObject.CreateInstance<MetaUnlockSO>();
            _createdObjects.Add(uSkill);
            SetField(uSkill, "_unlockId", 2);
            SetField(uSkill, "_cost", 100);
            SetField(uSkill, "_kind", MetaUnlockKind.InitialSkillBonus);
            SetField(uSkill, "_bonusValue", 10);

            MetaUnlockCatalogSO metaCatalog = ScriptableObject.CreateInstance<MetaUnlockCatalogSO>();
            _createdObjects.Add(metaCatalog);
            SetField(metaCatalog, "_unlocks", new List<MetaUnlockSO> { uStamina, uSkill });

            GameFlowController controller = CreateController(rules, catalog, relicResolver);
            SetField(controller, "_bossCatalog", bossCatalog);
            SetField(controller, "_autoBattleResolver", autoBattleResolver);
            SetField(controller, "_bossBattleTurns", new List<int> { 6, 12, 18, 24 });
            SetField(controller, "_metaPointResolver", metaResolver);
            SetField(controller, "_metaUnlockCatalog", metaCatalog);

            int clearCount = 0;
            int gameOverCount = 0;
            System.Random rand = new System.Random(999);

            for (int run = 0; run < 1000; run++)
            {
                // 所持ポイントがあればアンロックを自動購入
                if (controller.MetaProfile.AvailableMetaPoints >= 100 && !controller.MetaProfile.UnlockedIds.Contains(2))
                {
                    controller.TryPurchaseMetaUnlock(uSkill);
                }
                else if (controller.MetaProfile.AvailableMetaPoints >= 50 && !controller.MetaProfile.UnlockedIds.Contains(1))
                {
                    controller.TryPurchaseMetaUnlock(uStamina);
                }

                controller.StartGame();

                // 24ターン分確実に完走するまで実行
                int guard = 0;
                while (controller.CurrentPhase != GamePhase.GameClear && controller.CurrentPhase != GamePhase.GameOver && guard++ < 300)
                {
                    if (controller.CurrentPhase == GamePhase.ShowingEvent)
                    {
                        controller.OnEventDismissed();
                    }

                    if (controller.CurrentPhase == GamePhase.ShowingRelicDraft)
                    {
                        controller.OnRelicAcquired(rand.Next(1, 3));
                    }

                    if (controller.CurrentPhase == GamePhase.WaitingInput)
                    {
                        CommandDataSO chosen = commands[rand.Next(commands.Length)];
                        int turnBefore = controller.CurrentState.CurrentTurn;
                        controller.ExecuteCommand(chosen);

                        if (controller.CurrentPhase == GamePhase.WaitingInput && controller.CurrentState.CurrentTurn == turnBefore)
                        {
                            controller.ExecuteCommand(rest);
                        }
                    }
                }

                if (controller.CurrentPhase == GamePhase.GameClear)
                {
                    clearCount++;
                }
                else if (controller.CurrentPhase == GamePhase.GameOver)
                {
                    gameOverCount++;
                }
            }

            Assert.AreEqual(1000, clearCount + gameOverCount, "メタプログレッション1000回シミュレーションが全て正常終了すること");
            Assert.AreEqual(1000, controller.MetaProfile.TotalRunsCompleted);
            Assert.IsTrue(controller.MetaProfile.TotalEarnedMetaPoints > 50000, "1000周で十分なMetaPointsが蓄積されていること");
            Assert.IsTrue(controller.MetaProfile.UnlockedIds.Contains(1), "初期スタミナアンロックが購入されていること");
            Assert.IsTrue(controller.MetaProfile.UnlockedIds.Contains(2), "初期スキルアンロックが購入されていること");

            Debug.Log($"[MonteCarlo MetaProgression] 1,000 Runs: Clear={clearCount}, GameOver={gameOverCount}, TotalEarnedPoints={controller.MetaProfile.TotalEarnedMetaPoints}");
        }

        private GameRulesSO CreateRules()
        {
            GameRulesSO rules = GameRulesSOFactory.Create(
                paramMin: 0, paramMax: 100, initialStamina: 100, initialSkill: 0, initialMental: 50, startTurn: 1, maxTurn: 24);
            _createdObjects.Add(rules);
            return rules;
        }

        private CommandDataSO CreateCommand(string name, int staminaDelta, int skillDelta, int mentalDelta, int staminaCost)
        {
            CommandDataSO cmd = ScriptableObject.CreateInstance<CommandDataSO>();
            SetField(cmd, "_commandName", name);
            SetField(cmd, "_staminaDelta", staminaDelta);
            SetField(cmd, "_skillDelta", skillDelta);
            SetField(cmd, "_mentalDelta", mentalDelta);
            SetField(cmd, "_staminaCost", staminaCost);
            _createdObjects.Add(cmd);
            return cmd;
        }

        private RelicSO CreateRelic(int id, RelicTriggerKind trigger, int stam, int skill, int men)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            SetField(relic, "_relicId", id);
            SetField(relic, "_displayName", $"Relic_{id}");
            SetField(relic, "_description", $"Desc_{id}");
            SetField(relic, "_triggerKind", trigger);
            SetField(relic, "_staminaDeltaBonus", stam);
            SetField(relic, "_skillDeltaBonus", skill);
            SetField(relic, "_mentalDeltaBonus", men);
            SetField(relic, "_staminaCostMultiplier", 1f);
            _createdObjects.Add(relic);
            return relic;
        }

        private RelicCatalogSO CreateCatalog(params RelicSO[] relics)
        {
            RelicCatalogSO cat = ScriptableObject.CreateInstance<RelicCatalogSO>();
            SetField(cat, "_relics", new List<RelicSO>(relics));
            _createdObjects.Add(cat);
            return cat;
        }

        /// <summary>
        /// Boss_Act1_01〜Boss_Act4_01（BossId 1〜4）を保持する BossCatalogSO を組み立てる。
        /// 数値は BossAssetGenerator が生成する実アセットの値と揃えている。
        /// </summary>
        private BossCatalogSO CreateFourActBossCatalog()
        {
            (int id, string name, int hp, int atk, int mentalPressure, int shield, float mul)[] specs =
            {
                (1, "Boss_Act1_01", 80, 15, 10, 5, 1.5f),
                (2, "Boss_Act2_01", 140, 22, 15, 10, 1.8f),
                (3, "Boss_Act3_01", 220, 30, 20, 15, 2.0f),
                (4, "Boss_Act4_01", 320, 40, 25, 20, 2.2f),
            };

            List<BossSO> bosses = specs.Select(spec =>
            {
                BossSO boss = ScriptableObject.CreateInstance<BossSO>();
                _createdObjects.Add(boss);
                SetField(boss, "_bossId", spec.id);
                SetField(boss, "_bossName", spec.name);
                SetField(boss, "_maxHp", spec.hp);
                SetField(boss, "_attackPower", spec.atk);
                SetField(boss, "_mentalPressurePower", spec.mentalPressure);
                SetField(boss, "_guardShieldAmount", spec.shield);
                SetField(boss, "_specialAttackMultiplier", spec.mul);
                SetField(boss, "_actionPattern", new List<BossActionKind>
                {
                    BossActionKind.Attack, BossActionKind.MentalPressure, BossActionKind.Guard, BossActionKind.SpecialAttack
                });
                return boss;
            }).ToList();

            BossCatalogSO catalog = ScriptableObject.CreateInstance<BossCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "_bosses", bosses);
            return catalog;
        }

        private GameFlowController CreateController(GameRulesSO rules, RelicCatalogSO catalog, RelicResolverSO relicResolver)
        {
            GameObject go = new GameObject("GameFlowController");
            _createdGameObjects.Add(go);
            GameFlowController controller = go.AddComponent<GameFlowController>();

            CommandResolverSO commandResolver = ScriptableObject.CreateInstance<CommandResolverSO>();
            _createdObjects.Add(commandResolver);

            EventResolverSO eventResolver = ScriptableObject.CreateInstance<EventResolverSO>();
            _createdObjects.Add(eventResolver);

            GameEventCatalogSO eventCatalog = ScriptableObject.CreateInstance<GameEventCatalogSO>();
            SetField(eventCatalog, "_events", new List<GameEventSO>());
            _createdObjects.Add(eventCatalog);

            EndingResolverSO endingResolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(endingResolver);

            EndingRulesSO endingRules = ScriptableObject.CreateInstance<EndingRulesSO>();
            _createdObjects.Add(endingRules);

            GameStateEventChannelSO gameStateChannel = ScriptableObject.CreateInstance<GameStateEventChannelSO>();
            _createdObjects.Add(gameStateChannel);

            GameEventFiredChannelSO eventFiredChannel = ScriptableObject.CreateInstance<GameEventFiredChannelSO>();
            _createdObjects.Add(eventFiredChannel);

            EndingDecidedChannelSO endingDecidedChannel = ScriptableObject.CreateInstance<EndingDecidedChannelSO>();
            _createdObjects.Add(endingDecidedChannel);

            BossCatalogSO bossCatalog = CreateFourActBossCatalog();
            AutoBattleResolverSO autoBattleResolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
            _createdObjects.Add(autoBattleResolver);

            SetField(controller, "_gameRules", rules);
            SetField(controller, "_commandResolver", commandResolver);
            SetField(controller, "_eventCatalog", eventCatalog);
            SetField(controller, "_eventResolver", eventResolver);
            SetField(controller, "_endingRules", endingRules);
            SetField(controller, "_endingResolver", endingResolver);
            SetField(controller, "_relicCatalog", catalog);
            SetField(controller, "_relicResolver", relicResolver);
            SetField(controller, "_bossCatalog", bossCatalog);
            SetField(controller, "_autoBattleResolver", autoBattleResolver);
            SetField(controller, "_bossBattleTurns", new List<int> { 6, 12, 18, 24 });
            SetField(controller, "_gameStateChannel", gameStateChannel);
            SetField(controller, "_eventFiredChannel", eventFiredChannel);
            SetField(controller, "_endingDecidedChannel", endingDecidedChannel);

            return controller;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            }
            field.SetValue(target, value);
        }
    }
}
