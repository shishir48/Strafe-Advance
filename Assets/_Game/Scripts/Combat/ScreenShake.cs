using UnityEngine;

namespace StrafAdvance
{
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private float traumaDecay    = 1.8f;
        [SerializeField] private float maxPosOffset   = 0.35f;
        [SerializeField] private float maxRotDegrees  = 1.5f;
        [SerializeField] private float noiseFrequency = 22f;

        private float _trauma;
        private float _noiseSeed;

        public Vector3    PositionShake { get; private set; }
        public Quaternion RotationShake { get; private set; }

        void Awake()
        {
            _noiseSeed = Random.value * 1000f;
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

        public void Add(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                PositionShake = Vector3.zero;
                RotationShake = Quaternion.identity;
                return;
            }

            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * noiseFrequency + _noiseSeed;

            float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(t, t)  - 0.5f) * 2f;

            PositionShake = new Vector3(x, y, 0f) * maxPosOffset * shake;
            RotationShake = Quaternion.Euler(0f, 0f, r * maxRotDegrees * shake);

            _trauma = Mathf.Max(0f, _trauma - traumaDecay * Time.unscaledDeltaTime);
        }
    }

    /// <summary>Custom shake trigger (e.g., explosions). Amount 0..1.</summary>
    public readonly struct ShakeRequest
    {
        public readonly float Amount;
        public ShakeRequest(float amount) { Amount = amount; }
    }
}
