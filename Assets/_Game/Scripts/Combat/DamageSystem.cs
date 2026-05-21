using UnityEngine;

namespace StrafAdvance
{
    public static class DamageSystem
    {
        public static int Calculate(int baseDamage, float multiplier = 1f)
            => Mathf.RoundToInt(baseDamage * multiplier);
    }
}
