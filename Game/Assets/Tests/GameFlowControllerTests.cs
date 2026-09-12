// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [Explicit("PlayMode 曳光弾で置換予定")]
    public class GameFlowControllerTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

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
        public void StartGame_TransitionsToWaitingInputAndRaisesInitialStateWhenNoEventFires()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);

            controller.StartGame();

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
            Assert.AreEqual(1, raisedStates.Count);
            Assert.AreEqual(100, controller.CurrentState.Stamina);
            Assert.AreEqual(0, controller.CurrentState.Skill);
            Assert.AreEqual(50, controller.CurrentState.Mental);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
        }

        [Test]
        public void StartGame_FiresMatchingEventAtTurnStartAndTransitionsToShowingEvent()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventSO gameEvent = CreateEvent(
                eventId: 5, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1, skillDelta: 5);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            List<GameState> raisedStates = new List<GameState>();
            List<int> raisedEventIds = new List<int>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameEventFiredChannelSO eventFiredChannel = CreateChannel<GameEventFiredChannelSO, int>(raisedEventIds.Add);
            GameFlowController controller = CreateController(
                rules, catalog, gameStateChannel: gameStateChannel, eventFiredChannel: eventFiredChannel);

            controller.StartGame();

            Assert.AreEqual(GamePhase.ShowingEvent, controller.CurrentPhase);
            Assert.AreEqual(2, raisedStates.Count);
            Assert.AreEqual(1, raisedEventIds.Count);
            Assert.AreEqual(5, raisedEventIds[0]);
            Assert.AreEqual(5, controller.CurrentState.Skill);
        }

        [Test]
        public void ExecuteCommand_IsIgnoredWhilePhaseIsShowingEvent()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventSO gameEvent = CreateEvent(
                eventId: 5, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1, skillDelta: 5);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            controller.ExecuteCommand(train);

            Assert.AreEqual(GamePhase.ShowingEvent, controller.CurrentPhase);
            Assert.AreEqual(5, controller.CurrentState.Skill);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(2, raisedStates.Count);
        }

        [Test]
        public void AdvanceTurn_OnGameOverOrClear_AccumulatesMetaPointsAndAppliesUnlockBonusOnNextRun()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 20, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            MetaPointResolverSO metaResolver = ScriptableObject.CreateInstance<MetaPointResolverSO>();
            _createdObjects.Add(metaResolver);

            MetaUnlockSO unlock = ScriptableObject.CreateInstance<MetaUnlockSO>();
            _createdObjects.Add(unlock);
            SetField(unlock, "_unlockId", 1);
            SetField(unlock, "_unlockName", "Unlock_Stat_Stamina_01");
            SetField(unlock, "_cost", 30);
            SetField(unlock, "_kind", MetaUnlockKind.InitialStaminaBonus);
            SetField(unlock, "_bonusValue", 15);

            MetaUnlockCatalogSO metaCatalog = ScriptableObject.CreateInstance<MetaUnlockCatalogSO>();
            _createdObjects.Add(metaCatalog);
            SetField(metaCatalog, "_unlocks", new List<MetaUnlockSO> { unlock });

            GameFlowController controller = CreateController(rules, catalog);
            SetField(controller, "_metaPointResolver", metaResolver);
            SetField(controller, "_metaUnlockCatalog", metaCatalog);

            controller.StartGame();
            Assert.AreEqual(100, controller.CurrentState.Stamina);

            // 1回目のランで休養コマンドを実行してターン進行
            CommandDataSO rest = CreateCommand("Rest", 0, 10, 0, 0);
            controller.ExecuteCommand(rest);

            // メンタルを0にしてGameOverにする
            CommandDataSO breakdown = CreateCommand("Breakdown", 0, 0, -50, 0);
            controller.ExecuteCommand(breakdown);

            Assert.AreEqual(GamePhase.GameOver, controller.CurrentPhase);
            Assert.IsTrue(controller.MetaProfile.AvailableMetaPoints > 0, "GameOver時にMetaPointsが獲得されていること");
            Assert.AreEqual(1, controller.MetaProfile.TotalRunsCompleted);

            // メタショップでアンロックを購入
            bool purchased = controller.TryPurchaseMetaUnlock(unlock);
            Assert.IsTrue(purchased, "アンロックが正常に購入できること");
            Assert.AreEqual(1, controller.MetaProfile.UnlockedIds.Count);

            // 2回目のランを開始：初期ステータスボーナス（Stamina 100 + 15 = 100クランプ、または基本値からの加算）が反映されること
            GameRulesSO rulesWithLowerInitial = CreateRules(0, 100, 50, 20, 50, 1, 24);
            SetField(controller, "_gameRules", rulesWithLowerInitial);
            controller.StartGame();

            Assert.AreEqual(65, controller.CurrentState.Stamina, "初期スタミナ50にアンロックボーナス+15が加算されて65になること");
        }

        [Test]
        public void OnEventDismissed_TransitionsFromShowingEventToWaitingInput()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventSO gameEvent = CreateEvent(
                eventId: 5, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1, skillDelta: 5);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameFlowController controller = CreateController(rules, catalog);
            controller.StartGame();
            Assert.AreEqual(GamePhase.ShowingEvent, controller.CurrentPhase);

            controller.OnEventDismissed();

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
        }

        [Test]
        public void ExecuteCommand_AppliesEffectAndReturnsToWaitingInputWhenGameContinues()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            controller.ExecuteCommand(train);

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
            Assert.AreEqual(80, controller.CurrentState.Stamina);
            Assert.AreEqual(8, controller.CurrentState.Skill);
            Assert.AreEqual(45, controller.CurrentState.Mental);
            Assert.AreEqual(2, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(2, raisedStates.Count);
        }

        [Test]
        public void ExecuteCommand_IsIgnoredAndStateUnchangedWhenStaminaInsufficient()
        {
            GameRulesSO rules = CreateRules(0, 100, 10, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            controller.ExecuteCommand(train);

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
            Assert.AreEqual(10, controller.CurrentState.Stamina);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(1, raisedStates.Count);
        }

        [Test]
        public void ExecuteCommand_TransitionsToGameOverWithoutRaisingEndingWhenMentalReachesZero()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<EndingKind> raisedEndings = new List<EndingKind>();
            EndingDecidedChannelSO endingDecidedChannel = CreateChannel<EndingDecidedChannelSO, EndingKind>(raisedEndings.Add);
            GameFlowController controller = CreateController(rules, catalog, endingDecidedChannel: endingDecidedChannel);
            controller.StartGame();
            CommandDataSO breakdown = CreateCommand("Breakdown", 0, 0, -50, 0);

            controller.ExecuteCommand(breakdown);

            Assert.AreEqual(GamePhase.GameOver, controller.CurrentPhase);
            Assert.AreEqual(0, controller.CurrentState.Mental);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(0, raisedEndings.Count);
        }

        [Test]
        public void StartGame_TriggersBossBattleAtBossTurn_AndTransitionsToShowingRelicDraftOnVictory()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 30, 80, 12, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);

            BossSO boss = ScriptableObject.CreateInstance<BossSO>();
            _createdObjects.Add(boss);
            SetField(boss, "_bossId", 1);
            SetField(boss, "_maxHp", 50);
            SetField(boss, "_attackPower", 10);
            SetField(boss, "_mentalPressurePower", 5);
            SetField(boss, "_guardShieldAmount", 0);
            SetField(boss, "_specialAttackMultiplier", 1.5f);
            SetField(boss, "_actionPattern", new List<BossActionKind> { BossActionKind.Attack });

            BossCatalogSO bossCatalog = ScriptableObject.CreateInstance<BossCatalogSO>();
            _createdObjects.Add(bossCatalog);
            SetField(bossCatalog, "_bosses", new List<BossSO> { boss });

            AutoBattleResolverSO autoBattleResolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
            _createdObjects.Add(autoBattleResolver);

            GameFlowController controller = CreateController(rules, catalog);
            SetField(controller, "_bossCatalog", bossCatalog);
            SetField(controller, "_autoBattleResolver", autoBattleResolver);
            SetField(controller, "_bossBattleTurns", new List<int> { 12 });

            controller.StartGame();

            Assert.AreEqual(GamePhase.ShowingRelicDraft, controller.CurrentPhase);
            Assert.IsTrue(controller.CurrentState.Stamina > 0);
        }

        [Test]
        public void StartGame_TriggersBossBattleAtBossTurn_AndTransitionsToGameOverOnDefeat()
        {
            GameRulesSO rules = CreateRules(0, 100, 20, 1, 10, 12, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);

            BossSO boss = ScriptableObject.CreateInstance<BossSO>();
            _createdObjects.Add(boss);
            SetField(boss, "_bossId", 1);
            SetField(boss, "_maxHp", 300);
            SetField(boss, "_attackPower", 30);
            SetField(boss, "_mentalPressurePower", 10);
            SetField(boss, "_guardShieldAmount", 0);
            SetField(boss, "_specialAttackMultiplier", 1.5f);
            SetField(boss, "_actionPattern", new List<BossActionKind> { BossActionKind.Attack });

            BossCatalogSO bossCatalog = ScriptableObject.CreateInstance<BossCatalogSO>();
            _createdObjects.Add(bossCatalog);
            SetField(bossCatalog, "_bosses", new List<BossSO> { boss });

            AutoBattleResolverSO autoBattleResolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
            _createdObjects.Add(autoBattleResolver);

            GameFlowController controller = CreateController(rules, catalog);
            SetField(controller, "_bossCatalog", bossCatalog);
            SetField(controller, "_autoBattleResolver", autoBattleResolver);
            SetField(controller, "_bossBattleTurns", new List<int> { 12 });

            controller.StartGame();

            Assert.AreEqual(GamePhase.GameOver, controller.CurrentPhase);
            Assert.AreEqual(0, controller.CurrentState.Stamina);
        }

        [Test]
        public void StartGame_ProgressesThroughAllFourActs_DefeatsAllBossesAndReachesGameClear()
        {
            // Act 1〜4（Turn 6/12/18/24）のボス戦が順番に発生し、
            // 4体すべてを撃破して周回がクリアに到達することを検証する。
            GameRulesSO rules = CreateRules(0, 999, 500, 100, 500, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 999);

            BossCatalogSO bossCatalog = CreateFourActBossCatalog();

            AutoBattleResolverSO autoBattleResolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
            _createdObjects.Add(autoBattleResolver);

            GameFlowController controller = CreateController(rules, catalog);
            SetField(controller, "_bossCatalog", bossCatalog);
            SetField(controller, "_autoBattleResolver", autoBattleResolver);
            SetField(controller, "_bossBattleTurns", new List<int> { 6, 12, 18, 24 });

            controller.StartGame();
            CommandDataSO rest = CreateCommand("Rest", staminaDelta: 50, skillDelta: 0, mentalDelta: 0, staminaCost: 0);

            for (int i = 0;
                i < 30 && controller.CurrentPhase != GamePhase.GameClear && controller.CurrentPhase != GamePhase.GameOver;
                i++)
            {
                if (controller.CurrentPhase == GamePhase.ShowingRelicDraft)
                {
                    // このテストではドラフト内容自体は無関係なため、適当な ID でドラフトを閉じる。
                    controller.OnRelicAcquired(i + 100);
                }

                if (controller.CurrentPhase == GamePhase.WaitingInput)
                {
                    controller.ExecuteCommand(rest);
                }
            }

            Assert.AreEqual(GamePhase.GameClear, controller.CurrentPhase);
            Assert.AreEqual(4, controller.BossDefeatedCount, "Act 1〜4 の全ボスを撃破していること");
        }

        [Test]
        public void ExecuteCommand_TransitionsToGameClearAndRaisesEndingDecidedChannelOnFinalTurn()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 1);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            EndingRulesSO endingRules = EndingRulesSOFactory.Create(2, 1, 1, 1000);
            _createdObjects.Add(endingRules);
            EndingResolverSO endingResolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(endingResolver);
            List<EndingKind> raisedEndings = new List<EndingKind>();
            EndingDecidedChannelSO endingDecidedChannel = CreateChannel<EndingDecidedChannelSO, EndingKind>(raisedEndings.Add);
            GameFlowController controller = CreateController(
                rules, catalog, endingRules: endingRules, endingResolver: endingResolver,
                endingDecidedChannel: endingDecidedChannel);
            controller.StartGame();
            CommandDataSO rest = CreateCommand("Rest", 0, 0, 0, 0);

            controller.ExecuteCommand(rest);

            Assert.AreEqual(GamePhase.GameClear, controller.CurrentPhase);
            Assert.AreEqual(1, raisedEndings.Count);
            Assert.AreEqual(EndingKind.Stamina, raisedEndings[0]);
        }

        [Test]
        public void ExecuteCommand_TransitionsToGameOverAndRaisesEndingDecidedChannelWithDefeatWhenMentalDepleted()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 10, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<EndingKind> raisedEndings = new List<EndingKind>();
            EndingDecidedChannelSO endingDecidedChannel = CreateChannel<EndingDecidedChannelSO, EndingKind>(raisedEndings.Add);
            GameFlowController controller = CreateController(
                rules, catalog, endingDecidedChannel: endingDecidedChannel);
            controller.StartGame();
            CommandDataSO stressCommand = CreateCommand("Stress", 0, 0, -10, 0);

            controller.ExecuteCommand(stressCommand);

            Assert.AreEqual(GamePhase.GameOver, controller.CurrentPhase);
            Assert.AreEqual(1, raisedEndings.Count);
            Assert.AreEqual(EndingKind.Defeat, raisedEndings[0]);
        }

        private GameFlowController CreateController(
            GameRulesSO rules,
            GameEventCatalogSO catalog,
            EndingRulesSO endingRules = null,
            EndingResolverSO endingResolver = null,
            GameStateEventChannelSO gameStateChannel = null,
            GameEventFiredChannelSO eventFiredChannel = null,
            EndingDecidedChannelSO endingDecidedChannel = null)
        {
            GameObject controllerObject = new GameObject(nameof(GameFlowController));
            _createdObjects.Add(controllerObject);
            GameFlowController controller = controllerObject.AddComponent<GameFlowController>();

            CommandResolverSO commandResolver = ScriptableObject.CreateInstance<CommandResolverSO>();
            _createdObjects.Add(commandResolver);
            EventResolverSO eventResolver = ScriptableObject.CreateInstance<EventResolverSO>();
            _createdObjects.Add(eventResolver);

            SetField(controller, "_gameRules", rules);
            SetField(controller, "_commandResolver", commandResolver);
            SetField(controller, "_eventCatalog", catalog);
            SetField(controller, "_eventResolver", eventResolver);
            SetField(controller, "_endingRules", endingRules ?? EmptyEndingRules());
            SetField(controller, "_endingResolver", endingResolver ?? EmptyEndingResolver());
            SetField(controller, "_gameStateChannel", gameStateChannel ?? CreateChannel<GameStateEventChannelSO, GameState>(null));
            SetField(controller, "_eventFiredChannel", eventFiredChannel ?? CreateChannel<GameEventFiredChannelSO, int>(null));
            SetField(controller, "_endingDecidedChannel", endingDecidedChannel ?? CreateChannel<EndingDecidedChannelSO, EndingKind>(null));

            return controller;
        }

        private EndingRulesSO EmptyEndingRules()
        {
            EndingRulesSO rules = EndingRulesSOFactory.Create(2, 1, 1, 250);
            _createdObjects.Add(rules);
            return rules;
        }

        private EndingResolverSO EmptyEndingResolver()
        {
            EndingResolverSO resolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(resolver);
            return resolver;
        }

        private GameRulesSO CreateRules(
            int paramMin, int paramMax,
            int initialStamina, int initialSkill, int initialMental,
            int startTurn, int maxTurn)
        {
            GameRulesSO rules = GameRulesSOFactory.Create(
                paramMin, paramMax, initialStamina, initialSkill, initialMental, startTurn, maxTurn);
            _createdObjects.Add(rules);
            return rules;
        }

        private GameEventCatalogSO CreateCatalog(int paramMin, int paramMax, params GameEventSO[] events)
        {
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(paramMin, paramMax, events);
            _createdObjects.Add(catalog);
            return catalog;
        }

        private GameEventSO CreateEvent(
            int eventId, EventTriggerKind triggerKind, int triggerTurn,
            TrackedParameter targetParameter, int threshold, int priority,
            int staminaDelta = 0, int skillDelta = 0, int mentalDelta = 0)
        {
            GameEventSO gameEvent = GameEventSOFactory.CreateEvent(
                eventId, triggerKind, triggerTurn, targetParameter, threshold, priority,
                staminaDelta, skillDelta, mentalDelta);
            _createdObjects.Add(gameEvent);
            return gameEvent;
        }

        private CommandDataSO CreateCommand(
            string commandName, int staminaDelta, int skillDelta, int mentalDelta, int staminaCost)
        {
            CommandDataSO command = ScriptableObject.CreateInstance<CommandDataSO>();
            SetField(command, "_commandName", commandName);
            SetField(command, "_staminaDelta", staminaDelta);
            SetField(command, "_skillDelta", skillDelta);
            SetField(command, "_mentalDelta", mentalDelta);
            SetField(command, "_staminaCost", staminaCost);
            _createdObjects.Add(command);
            return command;
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

            List<BossSO> bosses = new List<BossSO>();
            foreach ((int id, string name, int hp, int atk, int mentalPressure, int shield, float mul) in specs)
            {
                BossSO boss = ScriptableObject.CreateInstance<BossSO>();
                _createdObjects.Add(boss);
                SetField(boss, "_bossId", id);
                SetField(boss, "_bossName", name);
                SetField(boss, "_maxHp", hp);
                SetField(boss, "_attackPower", atk);
                SetField(boss, "_mentalPressurePower", mentalPressure);
                SetField(boss, "_guardShieldAmount", shield);
                SetField(boss, "_specialAttackMultiplier", mul);
                SetField(boss, "_actionPattern", new List<BossActionKind>
                {
                    BossActionKind.Attack, BossActionKind.MentalPressure, BossActionKind.Guard, BossActionKind.SpecialAttack
                });
                bosses.Add(boss);
            }

            BossCatalogSO catalog = ScriptableObject.CreateInstance<BossCatalogSO>();
            _createdObjects.Add(catalog);
            SetField(catalog, "_bosses", bosses);
            return catalog;
        }

        private TChannel CreateChannel<TChannel, TValue>(Action<TValue> onRaised)
            where TChannel : EventChannelSO<TValue>
        {
            TChannel channel = ScriptableObject.CreateInstance<TChannel>();
            _createdObjects.Add(channel);

            if (onRaised != null)
            {
                channel.OnEventRaised += onRaised;
            }

            return channel;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。" +
                    "フィールド名がリネームされていないか、テストヘルパーを確認してください。");
            }

            field.SetValue(target, value);
        }
    }
}
