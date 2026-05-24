namespace StrafAdvance
{
    /// <summary>Reward kinds the battle pass can grant. Extend as needed (cosmetic IDs, perk IDs, etc).</summary>
    public enum BattlePassRewardType { Currency, WeaponUnlock }

    /// <summary>Single reward payload. <see cref="Value"/> is amount for currency, weapon ID for weapon unlock.</summary>
    public readonly struct BattlePassReward
    {
        public readonly BattlePassRewardType Type;
        public readonly int Amount;       // for Currency
        public readonly string PayloadId; // for WeaponUnlock
        BattlePassReward(BattlePassRewardType type, int amount, string payload)
        {
            Type = type; Amount = amount; PayloadId = payload;
        }
        public static BattlePassReward Currency(int amount)   => new BattlePassReward(BattlePassRewardType.Currency, amount, null);
        public static BattlePassReward Weapon(string weaponId) => new BattlePassReward(BattlePassRewardType.WeaponUnlock, 0, weaponId);

        public string Label => Type switch
        {
            BattlePassRewardType.Currency     => $"◆ {Amount:N0}",
            BattlePassRewardType.WeaponUnlock => $"⚔ {WeaponCatalog.Find(PayloadId).displayName}",
            _ => "?"
        };
    }

    /// <summary>One tier slot in the battle pass.</summary>
    public class BattlePassTier
    {
        public int TierIndex;            // 1-based for display, matches save data
        public int XpRequired;           // cumulative XP at which tier unlocks
        public BattlePassReward Free;
        public BattlePassReward Premium;
    }

    /// <summary>
    /// Hard-coded Season 1 catalog: 10 tiers, linear XP curve. Will move to ScriptableObject when
    /// designers want to author seasons without code changes.
    /// </summary>
    public static class BattlePassCatalog
    {
        public const int SeasonId   = 1;
        public const int MaxTier    = 10;
        public const int XpPerTier  = 500;

        public static readonly BattlePassTier[] Tiers =
        {
            new BattlePassTier { TierIndex = 1,  XpRequired = 500,  Free = BattlePassReward.Currency(100),  Premium = BattlePassReward.Currency(250) },
            new BattlePassTier { TierIndex = 2,  XpRequired = 1000, Free = BattlePassReward.Currency(150),  Premium = BattlePassReward.Currency(300) },
            new BattlePassTier { TierIndex = 3,  XpRequired = 1500, Free = BattlePassReward.Currency(200),  Premium = BattlePassReward.Weapon("rapid_smg") },
            new BattlePassTier { TierIndex = 4,  XpRequired = 2000, Free = BattlePassReward.Currency(250),  Premium = BattlePassReward.Currency(500) },
            new BattlePassTier { TierIndex = 5,  XpRequired = 2500, Free = BattlePassReward.Currency(300),  Premium = BattlePassReward.Weapon("heavy_cannon") },
            new BattlePassTier { TierIndex = 6,  XpRequired = 3000, Free = BattlePassReward.Currency(400),  Premium = BattlePassReward.Currency(750) },
            new BattlePassTier { TierIndex = 7,  XpRequired = 3500, Free = BattlePassReward.Currency(450),  Premium = BattlePassReward.Weapon("shotgun_spread") },
            new BattlePassTier { TierIndex = 8,  XpRequired = 4000, Free = BattlePassReward.Currency(500),  Premium = BattlePassReward.Currency(1000) },
            new BattlePassTier { TierIndex = 9,  XpRequired = 4500, Free = BattlePassReward.Currency(600),  Premium = BattlePassReward.Weapon("homing_pistol") },
            new BattlePassTier { TierIndex = 10, XpRequired = 5000, Free = BattlePassReward.Currency(1000), Premium = BattlePassReward.Currency(2500) },
        };

        /// <summary>Resolve the tier number a given cumulative XP value qualifies for (0 if below tier 1).</summary>
        public static int TierFromXp(int xp)
        {
            int tier = 0;
            for (int i = 0; i < Tiers.Length; i++)
                if (xp >= Tiers[i].XpRequired) tier = Tiers[i].TierIndex;
            return tier;
        }

        public static BattlePassTier ByIndex(int index)
        {
            foreach (var t in Tiers) if (t.TierIndex == index) return t;
            return null;
        }
    }
}
