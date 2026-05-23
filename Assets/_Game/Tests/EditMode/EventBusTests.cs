using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class EventBusTests
    {
        // Local message type to avoid coupling tests to live game messages.
        public readonly struct TestMsg { public readonly int V; public TestMsg(int v) { V = v; } }

        [SetUp]
        public void SetUp() => EventBus<TestMsg>.Clear();

        [TearDown]
        public void TearDown() => EventBus<TestMsg>.Clear();

        [Test]
        public void Subscribe_ReceivesPublishedMessage()
        {
            int captured = -1;
            EventBus<TestMsg>.Subscribe(m => captured = m.V);
            EventBus<TestMsg>.Publish(new TestMsg(42));
            Assert.AreEqual(42, captured);
        }

        [Test]
        public void Unsubscribe_StopsReceivingMessages()
        {
            int count = 0;
            System.Action<TestMsg> h = _ => count++;
            EventBus<TestMsg>.Subscribe(h);
            EventBus<TestMsg>.Publish(new TestMsg(1));
            EventBus<TestMsg>.Unsubscribe(h);
            EventBus<TestMsg>.Publish(new TestMsg(2));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void DoubleSubscribe_DeduplicatesHandlers()
        {
            int count = 0;
            System.Action<TestMsg> h = _ => count++;
            EventBus<TestMsg>.Subscribe(h);
            EventBus<TestMsg>.Subscribe(h);
            EventBus<TestMsg>.Publish(new TestMsg(0));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void HandlerException_DoesNotStopOtherHandlers()
        {
            int reached = 0;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            EventBus<TestMsg>.Subscribe(_ => throw new System.Exception("boom"));
            EventBus<TestMsg>.Subscribe(_ => reached++);
            EventBus<TestMsg>.Publish(new TestMsg(0));
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.AreEqual(1, reached);
        }
    }
}
