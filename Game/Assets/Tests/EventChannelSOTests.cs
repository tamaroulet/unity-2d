// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EventChannelSOTests
    {
        private GameEventFiredChannelSO _channel;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<GameEventFiredChannelSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_channel);
        }

        [Test]
        public void CallsSubscriberOnceWhenRaised()
        {
            int callCount = 0;
            _channel.OnEventRaised += _ => callCount++;

            _channel.Raise(1);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void PassesRaisedValueToSubscriber()
        {
            int receivedValue = -1;
            _channel.OnEventRaised += value => receivedValue = value;

            _channel.Raise(42);

            Assert.AreEqual(42, receivedValue);
        }

        [Test]
        public void DoesNotThrowWhenRaisedWithNoSubscribers()
        {
            Assert.DoesNotThrow(() => _channel.Raise(1));
        }

        [Test]
        public void DoesNotCallHandlerAfterUnsubscribe()
        {
            int callCount = 0;
            System.Action<int> handler = _ => callCount++;
            _channel.OnEventRaised += handler;
            _channel.OnEventRaised -= handler;

            _channel.Raise(1);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void NotifiesAllSubscribersWhenRaised()
        {
            int firstCallCount = 0;
            int secondCallCount = 0;
            _channel.OnEventRaised += _ => firstCallCount++;
            _channel.OnEventRaised += _ => secondCallCount++;

            _channel.Raise(1);

            Assert.AreEqual(1, firstCallCount);
            Assert.AreEqual(1, secondCallCount);
        }
    }
}
