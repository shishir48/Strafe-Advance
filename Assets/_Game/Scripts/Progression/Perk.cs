using System;
using System.Collections.Generic;

namespace StrafAdvance
{
    /// <summary>Stat modifiers a perk applies at run start. Pure data — no Unity types.</summary>
    [Serializable]
    public class Perk
    {
        public string id;
        public string displayName;
        public string description;
        public float  damageMultiplier   = 1f;
        public float  fireRateMultiplier = 1f;
        public float  maxHpMultiplier    = 1f;
        public float  strafeSpeedMultiplier = 1f;
        // Shop price in soft currency. Perks unlock through leveling for free; shop is the alternate path.
        public int    price              = 0;
    }

    /// <summary>Hard-coded perk catalogue. Will move to ScriptableObject + Addressables once perk count > 10.</summary>
    public static class PerkCatalog
    {
        public static readonly Perk[] All =
        {
            new Perk { id = "dmg_up_1",       displayName = "Heavy Rounds",    description = "+25% damage",          damageMultiplier   = 1.25f },
            new Perk { id = "firerate_up_1",  displayName = "Trigger Discipline", description = "+20% fire rate",   fireRateMultiplier = 0.83f }, // lower interval = faster
            new Perk { id = "hp_up_1",        displayName = "Reinforced Plating", description = "+30% max HP",      maxHpMultiplier    = 1.30f },
            new Perk { id = "speed_up_1",     displayName = "Light Loadout",   description = "+25% strafe speed",    strafeSpeedMultiplier = 1.25f },
            new Perk { id = "crit_pack",      displayName = "Hot Pack",         description = "+15% damage, -10% HP", damageMultiplier = 1.15f, maxHpMultiplier = 0.90f },
        };

        public static Perk Find(string id)
        {
            foreach (var p in All) if (p.id == id) return p;
            return null;
        }
    }
}
