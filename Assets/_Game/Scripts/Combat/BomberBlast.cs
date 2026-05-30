using UnityEngine;

namespace StrafAdvance
{
    /// <summary>Death-blast AoE damage falloff for <see cref="BomberEnemy"/>.</summary>
    public static class BomberBlast
    {
        /// <summary>Linear falloff: full at center, zero at/after radius.</summary>
        public static int DamageAt(float distance, float radius, int maxDamage)
        {
            if (distance >= radius) return 0;
            if (distance <= 0f) return maxDamage;
            float t = 1f - distance / radius;
            return Mathf.Max(0, Mathf.RoundToInt(maxDamage * t));
        }
    }
}
