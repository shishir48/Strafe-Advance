using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Global enemy stat multiplier. Driven by player level (so meta-progression scales challenge).
    /// Multiplier formula: 1 + (playerLevel-1) × <see cref="perLevelMul"/>, capped at <see cref="maxMul"/>.
    /// </summary>
    public static class DifficultyService
    {
        public const float perLevelMul = 0.08f; // +8% per player level
        public const float maxMul      = 3.0f;  // ×3 cap

        public static float Current
        {
            get
            {
                int level = SaveSystem.Current.progress.playerLevel;
                return Mathf.Clamp(1f + (level - 1) * perLevelMul, 1f, maxMul);
            }
        }
    }

    /// <summary>Math helpers for predictive aim and accuracy spread.</summary>
    public static class AimingMath
    {
        /// <summary>
        /// Compute a leading position for an enemy firing at a moving player.
        /// Falls back to current player position when <paramref name="leadFactor"/> is 0 or
        /// the prediction would diverge (player faster than bullet).
        /// </summary>
        public static Vector3 PredictPlayerPosition(
            Vector3 shooterPos, Vector3 playerPos, Vector3 playerVelocity,
            float bulletSpeed, float leadFactor)
        {
            if (leadFactor <= 0f || bulletSpeed <= 0.01f) return playerPos;
            float dist = Vector3.Distance(shooterPos, playerPos);
            float t    = dist / bulletSpeed;
            Vector3 predicted = playerPos + playerVelocity * t * leadFactor;
            return predicted;
        }

        /// <summary>Rotate a direction by random degrees in the XZ plane to simulate inaccuracy.</summary>
        public static Quaternion Jitter(Quaternion baseRot, float maxDegrees)
        {
            if (maxDegrees <= 0f) return baseRot;
            float d = Random.Range(-maxDegrees, maxDegrees);
            return Quaternion.Euler(0f, d, 0f) * baseRot;
        }
    }
}
