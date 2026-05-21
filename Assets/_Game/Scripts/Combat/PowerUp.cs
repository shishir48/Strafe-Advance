using UnityEngine;

namespace StrafAdvance
{
    public enum PowerUpType { RapidFire, Shield, Multishot }

    public class PowerUp : MonoBehaviour
    {
        [SerializeField] private PowerUpType type;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float moveSpeed = 4f;

        void Update() => transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (other.TryGetComponent<PlayerBuffs>(out var buffs))
                buffs.ApplyBuff(type, duration);
            Destroy(gameObject);
        }
    }
}
