using UnityEngine;

namespace StrafAdvance
{
    public enum CosmeticSlot { Player, Bullet, Trail }

    /// <summary>Cosmetic skin entry — pure data + a color hint used by <see cref="SkinApplyService"/>.</summary>
    public class CosmeticSkin
    {
        public string       Id;
        public string       DisplayName;
        public CosmeticSlot Slot;
        public int          Price;        // soft currency; 0 = starter / free
        public Color        TintColor;
    }

    /// <summary>
    /// Hard-coded cosmetic catalogue. Tints are placeholders until real art lands;
    /// they still give the player a visible signal that their unlock did something.
    /// </summary>
    public static class CosmeticCatalog
    {
        public static readonly CosmeticSkin[] All =
        {
            // Player skins
            new() { Id = "player_default", DisplayName = "Standard Hull",  Slot = CosmeticSlot.Player, Price = 0,    TintColor = new Color(0.31f, 0.76f, 0.97f) },
            new() { Id = "player_crimson", DisplayName = "Crimson Hull",   Slot = CosmeticSlot.Player, Price = 600,  TintColor = new Color(0.95f, 0.25f, 0.30f) },
            new() { Id = "player_void",    DisplayName = "Void Hull",     Slot = CosmeticSlot.Player, Price = 1200, TintColor = new Color(0.20f, 0.10f, 0.40f) },
            // Bullet skins
            new() { Id = "bullet_default", DisplayName = "Standard Bolt",  Slot = CosmeticSlot.Bullet, Price = 0,    TintColor = new Color(0.31f, 0.76f, 0.97f) },
            new() { Id = "bullet_amber",   DisplayName = "Amber Bolt",     Slot = CosmeticSlot.Bullet, Price = 400,  TintColor = new Color(1f, 0.75f, 0.20f) },
            new() { Id = "bullet_emerald", DisplayName = "Emerald Bolt",   Slot = CosmeticSlot.Bullet, Price = 800,  TintColor = new Color(0.20f, 0.95f, 0.45f) },
            // Trail skins (cosmetic only — applied to bullet trail / VFX)
            new() { Id = "trail_default", DisplayName = "Standard Trail", Slot = CosmeticSlot.Trail, Price = 0,    TintColor = new Color(0.4f, 0.8f, 1f) },
            new() { Id = "trail_neon",    DisplayName = "Neon Trail",    Slot = CosmeticSlot.Trail, Price = 500,  TintColor = new Color(1f, 0.2f, 0.85f) },
        };

        public static CosmeticSkin Find(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }

        public static System.Collections.Generic.IEnumerable<CosmeticSkin> WhereSlot(CosmeticSlot slot)
        {
            foreach (var s in All) if (s.Slot == slot) yield return s;
        }
    }
}
