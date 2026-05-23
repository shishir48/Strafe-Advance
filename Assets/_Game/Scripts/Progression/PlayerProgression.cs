using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// XP / level / perk unlock service. Persists via <see cref="SaveSystem"/>.
    /// Subscribes to <c>EnemyKilled</c> to grant XP and rolls perk unlocks on level-up.
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        public static PlayerProgression Instance { get; private set; }

        [SerializeField] private int xpPerGrunt   = 10;
        [SerializeField] private int xpPerFlanker = 25;
        [SerializeField] private int xpPerElite   = 75;
        [SerializeField] private int xpPerCharger = 20;

        public int Level => SaveSystem.Current.progress.playerLevel;
        public int Xp    => SaveSystem.Current.progress.playerXp;
        public int XpToNextLevel => XpRequiredFor(Level + 1) - Xp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<EnemyKilled>.Subscribe(OnKill);
        }

        void OnDestroy()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            if (Instance == this) Instance = null;
        }

        void OnKill(EnemyKilled k) => AddXp(XpFor(k.Type));

        public int XpFor(EnemyType t) => t switch
        {
            EnemyType.Grunt   => xpPerGrunt,
            EnemyType.Flanker => xpPerFlanker,
            EnemyType.Elite   => xpPerElite,
            EnemyType.Charger => xpPerCharger,
            _                 => 0,
        };

        /// <summary>Quadratic curve: level N requires 100 * N * N XP total.</summary>
        public static int XpRequiredFor(int level) => 100 * level * level;

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            var p = SaveSystem.Current.progress;
            p.playerXp += amount;
            while (p.playerXp >= XpRequiredFor(p.playerLevel + 1))
            {
                p.playerLevel++;
                UnlockNextPerk();
                EventBus<PlayerLeveledUp>.Publish(new PlayerLeveledUp(p.playerLevel));
            }
            SaveSystem.Save();
            EventBus<XpGained>.Publish(new XpGained(amount, p.playerXp, p.playerLevel));
        }

        void UnlockNextPerk()
        {
            var unlocked = SaveSystem.Current.progress.unlockedPerkIds;
            foreach (var perk in PerkCatalog.All)
                if (!unlocked.Contains(perk.id))
                {
                    unlocked.Add(perk.id);
                    EventBus<PerkUnlocked>.Publish(new PerkUnlocked(perk.id));
                    return;
                }
        }

        /// <summary>Returns combined stat multipliers of all equipped perks.</summary>
        public Perk GetEquippedStats()
        {
            var p = new Perk();
            foreach (var id in SaveSystem.Current.progress.equippedPerkIds)
            {
                var perk = PerkCatalog.Find(id);
                if (perk == null) continue;
                p.damageMultiplier      *= perk.damageMultiplier;
                p.fireRateMultiplier    *= perk.fireRateMultiplier;
                p.maxHpMultiplier       *= perk.maxHpMultiplier;
                p.strafeSpeedMultiplier *= perk.strafeSpeedMultiplier;
            }
            return p;
        }
    }

    public readonly struct XpGained        { public readonly int Amount, TotalXp, Level; public XpGained(int a, int t, int l) { Amount = a; TotalXp = t; Level = l; } }
    public readonly struct PlayerLeveledUp { public readonly int NewLevel; public PlayerLeveledUp(int l) { NewLevel = l; } }
    public readonly struct PerkUnlocked    { public readonly string PerkId; public PerkUnlocked(string id) { PerkId = id; } }
}
