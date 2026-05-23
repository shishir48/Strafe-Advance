using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class StateMachineTests
    {
        enum TestState { A, B, C }

        [Test]
        public void TryTransition_FailsWhenNotAllowed_InStrictMode()
        {
            var fsm = new StateMachine<TestState>(TestState.A, strict: true);
            // No Allow calls — strict mode rejects everything.
            Assert.IsFalse(fsm.TryTransition(TestState.B));
            Assert.AreEqual(TestState.A, fsm.Current);
        }

        [Test]
        public void TryTransition_SucceedsWhenAllowed()
        {
            var fsm = new StateMachine<TestState>(TestState.A, strict: true);
            fsm.Allow(TestState.A, TestState.B);
            Assert.IsTrue(fsm.TryTransition(TestState.B));
            Assert.AreEqual(TestState.B, fsm.Current);
        }

        [Test]
        public void TryTransition_SkipsSameState()
        {
            var fsm = new StateMachine<TestState>(TestState.A);
            fsm.Allow(TestState.A, TestState.A);
            Assert.IsFalse(fsm.TryTransition(TestState.A));
        }

        [Test]
        public void Callbacks_FireOnTransition_ExitThenEnter()
        {
            var fsm = new StateMachine<TestState>(TestState.A);
            fsm.Allow(TestState.A, TestState.B);
            var order = new System.Collections.Generic.List<string>();
            fsm.OnExited (TestState.A, () => order.Add("exitA"));
            fsm.OnEntered(TestState.B, () => order.Add("enterB"));
            fsm.OnChanged += (prev, cur) => order.Add($"changed:{prev}-{cur}");
            fsm.TryTransition(TestState.B);
            CollectionAssert.AreEqual(new[] { "exitA", "enterB", "changed:A-B" }, order);
        }

        [Test]
        public void NonStrict_AllowsAnyTransition()
        {
            var fsm = new StateMachine<TestState>(TestState.A, strict: false);
            Assert.IsTrue(fsm.TryTransition(TestState.C));
            Assert.AreEqual(TestState.C, fsm.Current);
        }
    }
}
