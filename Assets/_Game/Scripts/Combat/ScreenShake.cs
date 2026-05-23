using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Camera shake driven by the EventBus. Attach to the Main Camera (or any camera you want
    /// to shake). Listens for <c>ShakeRequest</c> and <c>EnemyKilled</c> / <c>PlayerDamaged</c>
    /// for built-in feedback. Uses Perlin noise to keep direction natural.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private float trauma         = 0f;   // 0..1
        [SerializeField] private float traumaDecay    = 1.8f; // per second
        [SerializeField] private float maxPosOffset   = 0.35f;
        [SerializeField] private float maxRotDegrees  = 1.5f;
        [SerializeField] private float noiseFrequency = 22f;

        private Vector3 _restLocalPos;
        private Quaternion _restLocalRot;
        private float _noiseSeed;

        void Awake()
        {
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;
            _noiseSeed    = Random.value * 1000f;
        }

        void OnEnable()
        {
            EventBus<ShakeRequest>.Subscribe(OnShake);
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<PlayerDamaged>.Subscribe(OnHit);
        }

        void OnDisable()
        {
            EventBus<ShakeRequest>.Unsubscribe(OnShake);
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<PlayerDamaged>.Unsubscribe(OnHit);
        }

        void OnShake(ShakeRequest r) => Add(r.Amount);
        void OnKill(EnemyKilled k)   => Add(k.Type == EnemyType.Elite ? 0.45f : 0.18f);
        void OnHit(PlayerDamaged d)  => Add(0.55f);

        public void Add(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        void LateUpdate()
        {
            if (trauma <= 0f)
            {
                transform.localPosition = _restLocalPos;
                transform.localRotation = _restLocalRot;
                return;
            }

            // Squaring trauma makes the shake feel less linear and more impactful at high values.
            float shake = trauma * trauma;
            float t = Time.unscaledTime * noiseFrequency + _noiseSeed;

            float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(t, t)  - 0.5f) * 2f;

            transform.localPosition = _restLocalPos + new Vector3(x, y, 0f) * maxPosOffset * shake;
            transform.localRotation = _restLocalRot * Quaternion.Euler(0f, 0f, r * maxRotDegrees * shake);

            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
        }
    }

    /// <summary>Custom shake trigger (e.g., explosions). Amount 0..1.</summary>
    public readonly struct ShakeRequest
    {
        public readonly float Amount;
        public ShakeRequest(float amount) { Amount = amount; }
    }
}
