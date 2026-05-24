using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Weapon stats. Currently authored as pure data via <see cref="WeaponCatalog"/>;
    /// move to ScriptableObject + Addressables once the player can equip more than 5.
    /// </summary>
    [System.Serializable]
    public class WeaponConfig
    {
        public string id;
        public string displayName;
        public string description;

        public int    damage          = 10;
        public float  fireRate        = 0.18f; // seconds between shots
        public float  bulletSpeed     = 22f;
        public float  homingStrength  = 0f;
        public int    multishotCount  = 1;       // total bullets per fire (1 = single shot)
        public float  multishotSpread = 8f;      // degrees between bullets in fan

        public Color  bulletColor     = new Color(0.31f, 0.76f, 0.97f);

        // Shop price in soft currency. 0 = starter (free, always unlocked).
        public int    price           = 0;
    }

    /// <summary>Hard-coded weapon catalogue. 5 starter weapons.</summary>
    public static class WeaponCatalog
    {
        public static readonly WeaponConfig[] All =
        {
            new WeaponConfig { id = "blaster_default", displayName = "Standard Blaster",  description = "Balanced single-shot.",         damage = 10, fireRate = 0.18f, bulletSpeed = 22f, price = 0 },
            new WeaponConfig { id = "rapid_smg",       displayName = "Rapid SMG",         description = "Fast, low damage.",             damage = 6,  fireRate = 0.07f, bulletSpeed = 26f, bulletColor = new Color(1f, 0.9f, 0.3f),  price = 500 },
            new WeaponConfig { id = "heavy_cannon",    displayName = "Heavy Cannon",      description = "Slow, heavy hits.",             damage = 35, fireRate = 0.55f, bulletSpeed = 18f, bulletColor = new Color(1f, 0.4f, 0.1f),  price = 1200 },
            new WeaponConfig { id = "shotgun_spread",  displayName = "Scatter Gun",       description = "5-bullet spread.",              damage = 5,  fireRate = 0.4f,  bulletSpeed = 20f, multishotCount = 5, multishotSpread = 10f, bulletColor = new Color(1f, 0.6f, 0.2f), price = 1800 },
            new WeaponConfig { id = "homing_pistol",   displayName = "Tracker Pistol",    description = "Bullets curve toward enemies.", damage = 12, fireRate = 0.30f, bulletSpeed = 16f, homingStrength = 220f, bulletColor = new Color(0.4f, 1f, 0.6f), price = 2500 },
        };

        public static WeaponConfig Default => All[0];

        public static WeaponConfig Find(string id)
        {
            foreach (var w in All) if (w.id == id) return w;
            return Default;
        }
    }
}
