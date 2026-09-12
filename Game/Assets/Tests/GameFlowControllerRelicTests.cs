// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.Relic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [Explicit("PlayMode 曳光弾で置換予定")]
    public class GameFlowControllerRelicTests
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
        public void StartGame_WithNoRelics_InitializesEmptyActiveRelics()
        {
            GameRulesSO rules = CreateRules();
            GameFlowController controller = CreateController(rules);

            controller.StartGame();

            Assert.AreEqual(0, controller.ActiveRelics.Count);
            Assert.AreEqual(0, controller.CurrentState.AcquiredRelicIds.Count);
        }

        [Test]
        public void OnRelicAcquired_AddsRelicToActiveListAndUpdatesGameState()
        {
            GameRulesSO rules = CreateRules();
            RelicSO relic = CreateRelic(1, RelicTriggerKind.OnTurnStart, 10, 0, 0);
            RelicCatalogSO catalog = CreateCatalog(relic);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO stateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog: catalog, gameStateChannel: stateChannel);
            controller.StartGame();

            controller.OnRelicAcquired(1);

            Assert.AreEqual(1, controller.ActiveRelics.Count);
            Assert.AreEqual(1, controller.CurrentState.AcquiredRelicIds.Count);
            Assert.AreEqual(1, controller.CurrentState.AcquiredRelicIds[0]);
            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
        }

        [Test]
        public void BeginTurn_WithTurnStartRelic_AppliesPassiveBonus()
        {
            GameRulesSO rules = CreateRules(initialStamina: 50);
            RelicSO relic = CreateRelic(1, RelicTriggerKind.OnTurnStart, staminaBonus: 10, skillBonus: 5, mentalBonus: 0);
            RelicCatalogSO catalog = CreateCatalog(relic);
            RelicResolverSO resolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(resolver);

            GameFlowController controller = CreateController(rules, catalog: catalog, relicResolver: resolver);
            controller.StartGame();
            controller.OnRelicAcquired(1);

            // ターンを進めて次のターンの開始時効果を検証
            CommandDataSO rest = CreateCommand("Rest", 0, 0, 0, 0);
            controller.ExecuteCommand(rest);

            // ターン2開始時に +10 スタミナ, +5 スキルが加算される
            Assert.AreEqual(60, controller.CurrentState.Stamina);
            Assert.AreEqual(5, controller.CurrentState.Skill);
        }

        [Test]
        public void ExecuteCommand_WithCommandRelic_AppliesModifiedEffect()
        {
            GameRulesSO rules = CreateRules(initialStamina: 100);
            RelicSO relic = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, staminaBonus: 10, skillBonus: 10, mentalBonus: 0, costMultiplier: 0.5f);
            RelicCatalogSO catalog = CreateCatalog(relic);
            RelicResolverSO resolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(resolver);

            GameFlowController controller = CreateController(rules, catalog: catalog, relicResolver: resolver);
            controller.StartGame();
            controller.OnRelicAcquired(2);

            CommandDataSO train = CreateCommand("Train", staminaDelta: -20, skillDelta: 8, mentalDelta: -5, staminaCost: 20);
            controller.ExecuteCommand(train);

            // コスト半減 (20 * 0.5 = 10) かつ スタミナ軽減 (+10) -> Stamina 100 + (-20 + 10) = 90
            // スキル加算 (8 + 10 = 18) -> Skill = 18
            Assert.AreEqual(90, controller.CurrentState.Stamina);
            Assert.AreEqual(18, controller.CurrentState.Skill);
        }

        private GameRulesSO CreateRules(
            int paramMin = 0, int paramMax = 100, int initialStamina = 100,
            int initialSkill = 0, int initialMental = 50, int startTurn = 1, int maxTurn = 24)
        {
            GameRulesSO rules = GameRulesSOFactory.Create(
                paramMin, paramMax, initialStamina, initialSkill, initialMental, startTurn, maxTurn);
            _createdObjects.Add(rules);
            return rules;
        }

        private RelicSO CreateRelic(
            int relicId, RelicTriggerKind triggerKind,
            int staminaBonus, int skillBonus, int mentalBonus, float costMultiplier = 1f)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            SetField(relic, "_relicId", relicId);
            SetField(relic, "_displayName", $"Relic_{relicId}");
            SetField(relic, "_description", $"Desc_{relicId}");
            SetField(relic, "_triggerKind", triggerKind);
            SetField(relic, "_staminaDeltaBonus", staminaBonus);
            SetField(relic, "_skillDeltaBonus", skillBonus);
            SetField(relic, "_mentalDeltaBonus", mentalBonus);
            SetField(relic, "_staminaCostMultiplier", costMultiplier);
            _createdObjects.Add(relic);
            return relic;
        }

        private RelicCatalogSO CreateCatalog(params RelicSO[] relics)
        {
            RelicCatalogSO catalog = ScriptableObject.CreateInstance<RelicCatalogSO>();
            SetField(catalog, "_relics", new List<RelicSO>(relics));
            _createdObjects.Add(catalog);
            return catalog;
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

        private TChannel CreateChannel<TChannel, TData>(System.Action<TData> listener)
            where TChannel : EventChannelSO<TData>
        {
            TChannel channel = ScriptableObject.CreateInstance<TChannel>();
            channel.OnEventRaised += listener;
            _createdObjects.Add(channel);
            return channel;
        }

        private GameFlowController CreateController(
            GameRulesSO rules,
            RelicCatalogSO catalog = null,
            RelicResolverSO relicResolver = null,
            GameStateEventChannelSO gameStateChannel = null)
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

            if (gameStateChannel == null)
            {
                gameStateChannel = ScriptableObject.CreateInstance<GameStateEventChannelSO>();
                _createdObjects.Add(gameStateChannel);
            }

            GameEventFiredChannelSO eventFiredChannel = ScriptableObject.CreateInstance<GameEventFiredChannelSO>();
            _createdObjects.Add(eventFiredChannel);

            EndingDecidedChannelSO endingDecidedChannel = ScriptableObject.CreateInstance<EndingDecidedChannelSO>();
            _createdObjects.Add(endingDecidedChannel);

            SetField(controller, "_gameRules", rules);
            SetField(controller, "_commandResolver", commandResolver);
            SetField(controller, "_eventCatalog", eventCatalog);
            SetField(controller, "_eventResolver", eventResolver);
            SetField(controller, "_endingRules", endingRules);
            SetField(controller, "_endingResolver", endingResolver);
            SetField(controller, "_relicCatalog", catalog);
            SetField(controller, "_relicResolver", relicResolver);
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
                throw new System.InvalidOperationException(
                    $"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            }
            field.SetValue(target, value);
        }
    }
}
