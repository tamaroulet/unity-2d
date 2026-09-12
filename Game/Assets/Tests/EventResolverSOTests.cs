// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Event;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EventResolverSOTests
    {
        private EventResolverSO _resolver;
        private readonly List<Object> _createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _resolver = ScriptableObject.CreateInstance<EventResolverSO>();
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

        private GameEventSO CreateEvent(
            int eventId,
            EventTriggerKind triggerKind,
            int triggerTurn,
            TrackedParameter targetParameter,
            int threshold,
            int priority,
            int staminaDelta = 0,
            int skillDelta = 0,
            int mentalDelta = 0)
        {
            GameEventSO gameEvent = GameEventSOFactory.CreateEvent(
                eventId, triggerKind, triggerTurn, targetParameter, threshold, priority,
                staminaDelta, skillDelta, mentalDelta);
            _createdObjects.Add(gameEvent);
            return gameEvent;
        }

        private GameEventCatalogSO CreateCatalog(int paramMin, int paramMax, params GameEventSO[] events)
        {
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(paramMin, paramMax, events);
            _createdObjects.Add(catalog);
            return catalog;
        }

        [Test]
        public void FiresEventWhenSingleEventConditionIsMet()
        {
            GameEventSO gameEvent = CreateEvent(0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState state = new GameState { CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.IsTrue(result.HasFired);
            Assert.AreEqual(gameEvent, result.FiredEvent);
        }

        [Test]
        public void DoesNotChangeStateWhenNoEventConditionIsMet()
        {
            GameEventSO gameEvent = CreateEvent(0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState state = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.IsFalse(result.HasFired);
            Assert.AreEqual(state, result.State);
        }

        [Test]
        public void DoesNotRefireAlreadyFiredEvent()
        {
            GameEventSO gameEvent = CreateEvent(0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState state = new GameState
            {
                CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 50, FiredEventMask = 1 << 0
            };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.IsFalse(result.HasFired);
            Assert.AreEqual(state, result.State);
        }

        [Test]
        public void FiresOnlyHigherPriorityEventWhenTwoEventsWithDifferentPriorityMatch()
        {
            GameEventSO lowPriority = CreateEvent(0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventSO highPriority = CreateEvent(1, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 20);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, lowPriority, highPriority);
            GameState state = new GameState { CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.AreEqual(highPriority, result.FiredEvent);
        }

        [Test]
        public void FiresSmallerIdEventWhenTwoEventsWithSamePriorityMatch()
        {
            GameEventSO largerId = CreateEvent(1, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventSO smallerId = CreateEvent(0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, largerId, smallerId);
            GameState state = new GameState { CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.AreEqual(smallerId, result.FiredEvent);
        }

        [Test]
        public void SetsCorrespondingBitInFiredEventMaskAfterFiring()
        {
            GameEventSO gameEvent = CreateEvent(3, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState state = new GameState { CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.AreEqual(1 << 3, result.State.FiredEventMask);
        }

        [Test]
        public void ClampsParameterAtUpperBoundAfterApplyingEffect()
        {
            GameEventSO gameEvent = CreateEvent(
                0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10,
                staminaDelta: 20);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState state = new GameState { CurrentTurn = 5, Stamina = 95, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.AreEqual(100, result.State.Stamina);
        }

        [Test]
        public void ClampsParameterAtLowerBoundAfterApplyingEffect()
        {
            GameEventSO gameEvent = CreateEvent(
                0, EventTriggerKind.ParameterBelow, 0, TrackedParameter.Mental, 30, 10,
                mentalDelta: -20);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState state = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 10 };

            EventResult result = _resolver.Resolve(state, catalog);

            Assert.AreEqual(0, result.State.Mental);
        }

        [Test]
        public void DoesNotMutateOriginalGameStateInstance()
        {
            GameEventSO gameEvent = CreateEvent(
                0, EventTriggerKind.TurnReached, 5, TrackedParameter.Stamina, 0, 10,
                staminaDelta: -10);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameState originalState = new GameState { CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 50 };

            EventResult result = _resolver.Resolve(originalState, catalog);

            Assert.AreEqual(100, originalState.Stamina);
            Assert.AreEqual(0, originalState.FiredEventMask);
            Assert.IsFalse(ReferenceEquals(originalState, result.State));
        }
    }
}
