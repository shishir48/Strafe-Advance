using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerBuffs : MonoBehaviour
    {
        [SerializeField] private AutoShooter autoShooter;
        [SerializeField] private PlayerHealth health;

        private readonly Dictionary<PowerUpType, Coroutine> _active = new Dictionary<PowerUpType, Coroutine>();

        public void ApplyBuff(PowerUpType type, float duration)
        {
            if (_active.TryGetValue(type, out Coroutine existing))
                StopCoroutine(existing);
            _active[type] = StartCoroutine(BuffRoutine(type, duration));
        }

        IEnumerator BuffRoutine(PowerUpType type, float duration)
        {
            SetEffect(type, true);
            yield return new WaitForSeconds(duration);
            SetEffect(type, false);
            _active.Remove(type);
        }

        void SetEffect(PowerUpType type, bool on)
        {
            switch (type)
            {
                case PowerUpType.RapidFire:
                    if (autoShooter != null) autoShooter.SetFireRateMultiplier(on ? 0.4f : 1f);
                    break;
                case PowerUpType.Shield:
                    if (health != null) health.SetInvincible(on);
                    break;
                case PowerUpType.Multishot:
                    if (autoShooter != null) autoShooter.SetMultishot(on);
                    break;
            }
        }
    }
}
