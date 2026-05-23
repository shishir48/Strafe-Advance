using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "StrafAdvance/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public int   maxHp = 30;
        public int   contactDamage = 10;
        public float moveSpeed = 3f;
        public float fireRate  = 2f;
        public int   bulletDamage = 8;

        [Header("AI")]
        [Tooltip("0 = no leading, 1 = perfect prediction. Hits where player WILL be after the bullet travel time.")]
        [Range(0f, 1f)] public float aimLeadFactor = 0f;
        [Tooltip("Random spread in degrees applied to each shot. 0 = pixel-perfect.")]
        [Range(0f, 45f)] public float accuracyJitterDeg = 0f;

        /// <summary>Scale this config's stats by a global difficulty multiplier (level 1 = 1.0×).</summary>
        public EnemyConfig WithDifficulty(float mul)
        {
            var copy = ScriptableObject.CreateInstance<EnemyConfig>();
            copy.maxHp           = Mathf.Max(1, Mathf.RoundToInt(maxHp * mul));
            copy.contactDamage   = Mathf.Max(1, Mathf.RoundToInt(contactDamage * mul));
            copy.moveSpeed       = moveSpeed * Mathf.Lerp(1f, 1.3f, (mul - 1f) / 2f); // soft cap on speed
            copy.fireRate        = fireRate  / Mathf.Lerp(1f, 1.5f, (mul - 1f) / 2f); // faster cap
            copy.bulletDamage    = Mathf.Max(1, Mathf.RoundToInt(bulletDamage * mul));
            copy.aimLeadFactor   = aimLeadFactor;
            copy.accuracyJitterDeg = accuracyJitterDeg;
            return copy;
        }
    }
}
