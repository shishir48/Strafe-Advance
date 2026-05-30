using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Static typed pub/sub. Subscribers register by message type; publishers fire by type.
    /// Drop-in replacement for the scattered <c>Action&lt;T&gt;</c> events on singletons.
    ///
    /// Usage:
    ///   EventBus&lt;EnemyKilled&gt;.Subscribe(OnKill);
    ///   EventBus&lt;EnemyKilled&gt;.Publish(new EnemyKilled(50));
    ///   EventBus&lt;EnemyKilled&gt;.Unsubscribe(OnKill);
    ///
    /// Zero-alloc dispatch (no LINQ, no closures). Safe to subscribe/unsubscribe during publish
    /// (copies the handler list each fire).
    /// </summary>
    public static class EventBus<T>
    {
        private static readonly List<Action<T>> _handlers = new List<Action<T>>(8);

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null) return;
            if (!_handlers.Contains(handler)) _handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            _handlers.Remove(handler);
        }

        public static void Publish(T msg)
        {
            if (_handlers.Count == 0) return;
            // Snapshot for safe mutation during dispatch.
            var copy = _handlers.ToArray();
            for (int i = 0; i < copy.Length; i++)
            {
                try { copy[i](msg); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
        }

        /// <summary>Wipe all subscribers. Call between play sessions to defend against domain-reload skip.</summary>
        public static void Clear() => _handlers.Clear();

        /// <summary>Live subscriber count. Used by leak-detection tests across scene reloads.</summary>
        public static int HandlerCount => _handlers.Count;
    }

    // ─── Standard game messages ─────────────────────────────────────────────────

    public readonly struct GameStateChanged
    {
        public readonly GameState Previous;
        public readonly GameState Current;
        public GameStateChanged(GameState previous, GameState current) { Previous = previous; Current = current; }
    }

    public readonly struct EnemyKilled
    {
        public readonly EnemyType Type;
        public readonly int       ScoreReward;
        public readonly Vector3   WorldPos;
        public EnemyKilled(EnemyType type, int scoreReward) : this(type, scoreReward, Vector3.zero) { }
        public EnemyKilled(EnemyType type, int scoreReward, Vector3 worldPos) { Type = type; ScoreReward = scoreReward; WorldPos = worldPos; }
    }

    public readonly struct WaveStarted
    {
        public readonly int Index;
        public readonly int Total;
        public WaveStarted(int index, int total) { Index = index; Total = total; }
    }

    public readonly struct PlayerDamaged
    {
        public readonly int Amount;
        public readonly int RemainingHp;
        public PlayerDamaged(int amount, int remainingHp) { Amount = amount; RemainingHp = remainingHp; }
    }

    public readonly struct DodgePerformed
    {
        public readonly float DirectionX;
        public DodgePerformed(float dx) { DirectionX = dx; }
    }
}
