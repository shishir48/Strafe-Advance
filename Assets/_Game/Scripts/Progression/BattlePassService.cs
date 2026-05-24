using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Tracks battle pass XP, tier-up cadence, and per-lane claim state. Subscribes to gameplay
    /// events for XP grants; tier transitions emit <see cref="BattlePassTierReached"/> so HUD/toast
    /// systems can announce them. Claim is split per lane (free vs premium) so buying the premium
    /// pass retroactively unlocks every tier the player has already passed.
    /// </summary>
    public class BattlePassService : MonoBehaviour
    {
        public static BattlePassService Instance { get; private set; }

        // XP awards per kill — keeps the BP curve independent from player progression.
        const int XpPerKill     = 25;
        const int XpPerElite    = 75;
        const int XpPerMiniBoss = 250;

        public int Tier              => SaveSystem.Current.progress.battlePassTier;
        public int Xp                => SaveSystem.Current.progress.battlePassXp;
        public bool PremiumOwned     => SaveSystem.Current.progress.premiumPassOwned;
        public int MaxTier           => BattlePassCatalog.MaxTier;

        public int XpIntoCurrentTier => CurrentTierStart == 0 ? Xp : Xp - CurrentTierStart;
        public int XpSpanCurrentTier => NextTierStart - CurrentTierStart;
        public float ProgressToNext  => XpSpanCurrentTier <= 0 ? 1f : Mathf.Clamp01((float)XpIntoCurrentTier / XpSpanCurrentTier);

        int CurrentTierStart => Tier <= 0 ? 0 : BattlePassCatalog.ByIndex(Tier).XpRequired;
        int NextTierStart    => Tier >= MaxTier ? CurrentTierStart : BattlePassCatalog.ByIndex(Tier + 1).XpRequired;

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

        void OnKill(EnemyKilled k)
        {
            int gain = XpForKill(k.Type);
            if (gain > 0) AddXp(gain);
        }

        public static int XpForKill(EnemyType type) => type switch
        {
            EnemyType.Elite    => XpPerElite,
            EnemyType.MiniBoss => XpPerMiniBoss,
            _                  => XpPerKill,
        };

        /// <summary>Grant XP and check for tier-ups. Multiple tier-ups in one grant emit one event per tier.</summary>
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            var p = SaveSystem.Current.progress;
            p.battlePassXp += amount;

            int newTier = BattlePassCatalog.TierFromXp(p.battlePassXp);
            while (p.battlePassTier < newTier)
            {
                p.battlePassTier++;
                EventBus<BattlePassTierReached>.Publish(new BattlePassTierReached(p.battlePassTier, BattlePassCatalog.MaxTier));
            }
            SaveSystem.Save();
        }

        /// <summary>Claim a tier's reward on the specified lane. Returns false if not eligible / already claimed / premium-locked.</summary>
        public bool Claim(int tier, bool premiumLane)
        {
            if (tier <= 0 || tier > MaxTier) return false;
            if (tier > Tier) return false; // not yet reached
            if (premiumLane && !PremiumOwned) return false;

            var p = SaveSystem.Current.progress;
            var claimedList = premiumLane ? p.claimedPremiumTiers : p.claimedFreeTiers;
            if (claimedList.Contains(tier)) return false;

            var def = BattlePassCatalog.ByIndex(tier);
            var reward = premiumLane ? def.Premium : def.Free;
            GrantReward(reward);
            claimedList.Add(tier);
            SaveSystem.Save();
            return true;
        }

        /// <summary>One-shot purchase of the premium pass; grants no rewards directly — the player still has to claim each tier.</summary>
        public void UnlockPremium()
        {
            if (PremiumOwned) return;
            SaveSystem.Current.progress.premiumPassOwned = true;
            SaveSystem.Save();
        }

        public bool IsClaimed(int tier, bool premiumLane) =>
            (premiumLane ? SaveSystem.Current.progress.claimedPremiumTiers
                         : SaveSystem.Current.progress.claimedFreeTiers).Contains(tier);

        static void GrantReward(BattlePassReward r)
        {
            switch (r.Type)
            {
                case BattlePassRewardType.Currency:
                    if (CurrencyService.Instance != null) CurrencyService.Instance.Grant(r.Amount);
                    else SaveSystem.Current.progress.softCurrency += r.Amount;
                    break;
                case BattlePassRewardType.WeaponUnlock:
                    var owned = SaveSystem.Current.progress.unlockedWeaponIds;
                    if (!owned.Contains(r.PayloadId)) owned.Add(r.PayloadId);
                    break;
            }
        }
    }

    public readonly struct BattlePassTierReached
    {
        public readonly int Tier;
        public readonly int MaxTier;
        public BattlePassTierReached(int tier, int max) { Tier = tier; MaxTier = max; }
    }
}
