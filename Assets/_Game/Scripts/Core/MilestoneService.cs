using UnityEngine;

namespace StrafAdvance
{
    public enum MilestoneKind { Kill, Combo, Level }

    /// <summary>A reward milestone was hit — for decoupled juice (banner/audio/screen punch).</summary>
    public readonly struct MilestoneReached
    {
        public readonly MilestoneKind Kind;
        public readonly string Label;
        public MilestoneReached(MilestoneKind kind, string label) { Kind = kind; Label = label; }
    }

    /// <summary>
    /// Reward-juice layer. Watches kills / combo tiers / level-ups and celebrates milestones with a
    /// banner (<see cref="ToastNotifier"/>), a screen punch (<see cref="ShakeRequest"/>) and a chime
    /// (<see cref="AudioManager"/>), and republishes <see cref="MilestoneReached"/> for other systems.
    /// Resets per run.
    /// </summary>
    public class MilestoneService : MonoBehaviour
    {
        const int KillStep = 25;

        static readonly Color KillColor  = new Color(1f,   0.8f, 0.2f);
        static readonly Color ComboColor = new Color(0.3f, 0.9f, 1f);
        static readonly Color LevelColor = new Color(0.6f, 1f,   0.4f);

        readonly MilestoneTracker _killMilestones = new MilestoneTracker(KillStep);
        int _kills;
        int _lastComboMult = 1;

        void OnEnable()
        {
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<ComboChanged>.Subscribe(OnCombo);
            EventBus<PlayerLeveledUp>.Subscribe(OnLevelUp);
            EventBus<GameStateChanged>.Subscribe(OnStateChanged);
        }

        void OnDisable()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<ComboChanged>.Unsubscribe(OnCombo);
            EventBus<PlayerLeveledUp>.Unsubscribe(OnLevelUp);
            EventBus<GameStateChanged>.Unsubscribe(OnStateChanged);
        }

        void OnStateChanged(GameStateChanged s)
        {
            if (s.Current == GameState.Playing) ResetRun();
        }

        void ResetRun()
        {
            _kills = 0;
            _lastComboMult = 1;
            _killMilestones.Reset();
        }

        void OnKill(EnemyKilled k)
        {
            _kills++;
            int m = _killMilestones.Check(_kills);
            if (m > 0) Celebrate(MilestoneKind.Kill, m + " KILLS!", KillColor, 0.4f, SoundID.ComboTier);
        }

        void OnCombo(ComboChanged c)
        {
            if (c.Multiplier > _lastComboMult && c.Multiplier >= 2)
                Celebrate(MilestoneKind.Combo, "COMBO x" + c.Multiplier + "!", ComboColor, 0.3f, SoundID.ComboTier);
            _lastComboMult = c.Multiplier;
        }

        void OnLevelUp(PlayerLeveledUp l)
        {
            Celebrate(MilestoneKind.Level, "LEVEL " + l.NewLevel + "!", LevelColor, 0.5f, SoundID.PerkUnlock);
        }

        void Celebrate(MilestoneKind kind, string label, Color accent, float shake, SoundID sfx)
        {
            EventBus<MilestoneReached>.Publish(new MilestoneReached(kind, label));
            if (ToastNotifier.Instance != null) ToastNotifier.Instance.Enqueue(label, "", accent);
            EventBus<ShakeRequest>.Publish(new ShakeRequest(shake));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfx);
        }
    }
}
