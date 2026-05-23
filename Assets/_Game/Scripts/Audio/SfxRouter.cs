using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Single bridge between gameplay EventBus and <see cref="AudioManager"/>.
    /// Subscribes to typed messages and calls <c>AudioManager.PlaySFX</c>. Keeps
    /// gameplay code audio-blind and audio code event-blind — Strategy + Mediator pattern.
    ///
    /// Wire one instance per scene (handled by GameSetup). Idempotent singleton with
    /// editor-safe subscribe/unsubscribe.
    /// </summary>
    public class SfxRouter : MonoBehaviour
    {
        public static SfxRouter Instance { get; private set; }

        private int _lastComboTier;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            Subscribe();
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        void Subscribe()
        {
            EventBus<EnemyKilled>.Subscribe(OnKilled);
            EventBus<EnemyDamaged>.Subscribe(OnDamaged);
            EventBus<PlayerDamaged>.Subscribe(OnPlayerHit);
            EventBus<DodgePerformed>.Subscribe(OnDodge);
            EventBus<ShieldHit>.Subscribe(OnShieldHit);
            EventBus<ComboChanged>.Subscribe(OnComboChanged);
            EventBus<PerkUnlocked>.Subscribe(OnPerkUnlocked);
            EventBus<BossPhaseChanged>.Subscribe(OnBossPhase);
        }

        void Unsubscribe()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKilled);
            EventBus<EnemyDamaged>.Unsubscribe(OnDamaged);
            EventBus<PlayerDamaged>.Unsubscribe(OnPlayerHit);
            EventBus<DodgePerformed>.Unsubscribe(OnDodge);
            EventBus<ShieldHit>.Unsubscribe(OnShieldHit);
            EventBus<ComboChanged>.Unsubscribe(OnComboChanged);
            EventBus<PerkUnlocked>.Unsubscribe(OnPerkUnlocked);
            EventBus<BossPhaseChanged>.Unsubscribe(OnBossPhase);
        }

        void Play(SoundID id)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(id);
        }

        void OnKilled(EnemyKilled k)
        {
            Play(k.Type == EnemyType.Elite || k.Type == EnemyType.MiniBoss
                 ? SoundID.EliteDeath
                 : SoundID.EnemyDeath);
        }

        void OnDamaged(EnemyDamaged _) => Play(SoundID.EnemyHit);
        void OnPlayerHit(PlayerDamaged _) => Play(SoundID.PlayerHit);
        void OnDodge(DodgePerformed _)    => Play(SoundID.Dodge);
        void OnShieldHit(ShieldHit _)     => Play(SoundID.ShieldHit);
        void OnPerkUnlocked(PerkUnlocked _) => Play(SoundID.PerkUnlock);
        void OnBossPhase(BossPhaseChanged b) => Play(b.Phase >= 1 ? SoundID.BossPhase2 : SoundID.BossRoar);

        void OnComboChanged(ComboChanged c)
        {
            // Fire only when the multiplier tier increases.
            if (c.Multiplier > _lastComboTier && c.Multiplier > 1) Play(SoundID.ComboTier);
            _lastComboTier = c.Multiplier;
        }
    }
}
