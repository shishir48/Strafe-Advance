using System;
using System.Collections.Generic;

namespace StrafAdvance
{
    /// <summary>
    /// Generic FSM with validated transitions, enter/exit callbacks, and a state-change event.
    /// Lighter than Stateless/UniRx — zero deps, ~100 LOC, fast enough for any game loop.
    ///
    /// Usage:
    ///   var fsm = new StateMachine&lt;GameState&gt;(GameState.Menu);
    ///   fsm.Allow(GameState.Menu, GameState.Playing);
    ///   fsm.OnEntered(GameState.Playing, () =&gt; Time.timeScale = 1f);
    ///   fsm.OnExited (GameState.Playing, () =&gt; SaveSystem.Save());
    ///   fsm.OnChanged += (prev, cur) =&gt; Debug.Log($"{prev} → {cur}");
    ///   fsm.TryTransition(GameState.Playing); // returns true if allowed
    /// </summary>
    public class StateMachine<TState> where TState : struct, Enum
    {
        private readonly HashSet<(TState from, TState to)> _allowed = new HashSet<(TState, TState)>();
        private readonly Dictionary<TState, Action>        _enter   = new Dictionary<TState, Action>();
        private readonly Dictionary<TState, Action>        _exit    = new Dictionary<TState, Action>();
        private readonly bool _strict;

        public TState Current { get; private set; }
        public event Action<TState, TState> OnChanged;

        /// <param name="initial">Starting state. No Enter callback fires for initial state.</param>
        /// <param name="strict">If true, only Allow'd transitions succeed. If false, all transitions succeed.</param>
        public StateMachine(TState initial, bool strict = true)
        {
            Current = initial;
            _strict = strict;
        }

        public void Allow(TState from, TState to) => _allowed.Add((from, to));

        public void OnEntered(TState state, Action callback)
        {
            if (callback == null) return;
            _enter.TryGetValue(state, out var cur);
            _enter[state] = cur + callback;
        }

        public void OnExited(TState state, Action callback)
        {
            if (callback == null) return;
            _exit.TryGetValue(state, out var cur);
            _exit[state] = cur + callback;
        }

        public bool CanTransition(TState to) =>
            !_strict || _allowed.Contains((Current, to));

        public bool TryTransition(TState to)
        {
            if (EqualityComparer<TState>.Default.Equals(Current, to)) return false;
            if (!CanTransition(to)) return false;
            var prev = Current;
            if (_exit.TryGetValue(prev, out var exitCb)) exitCb?.Invoke();
            Current = to;
            if (_enter.TryGetValue(to, out var enterCb)) enterCb?.Invoke();
            OnChanged?.Invoke(prev, to);
            return true;
        }

        /// <summary>Force a state without firing callbacks. Use for restore-from-save only.</summary>
        public void ForceSet(TState state) => Current = state;
    }
}
