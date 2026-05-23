using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Fast melee rusher with a telegraphed lunge. Movement phases:
    ///   1. <c>Approach</c> — slow advance with lateral homing on player.
    ///   2. <c>WindUp</c>   — when within <see cref="lungeTriggerZ"/>, pause + scale up + flash red.
    ///                       Window for player to dodge.
    ///   3. <c>Lunge</c>    — burst forward at <see cref="lungeSpeedMul"/>x speed for <see cref="lungeSeconds"/>.
    ///   4. Repeat or escape.
    /// </summary>
    public class ChargerEnemy : EnemyBase
    {
        enum State { Approach, WindUp, Lunge }

        [SerializeField] private float lateralHomingSpeed = 4f;
        [SerializeField] private float lungeTriggerZ      = 8f;   // start wind-up when player z-distance < this
        [SerializeField] private float windUpSeconds      = 0.5f;
        [SerializeField] private float lungeSeconds       = 0.7f;
        [SerializeField] private float lungeSpeedMul      = 2.6f;
        [SerializeField] private float lungeCooldown      = 1.5f;
        [SerializeField] private Color telegraphColor     = new Color(3f, 0.4f, 0.4f, 1f);

        private Transform _player;
        private State _state = State.Approach;
        private float _stateTimer;
        private float _lungeReadyAt;
        private Vector3 _restScale;
        private Renderer[] _renderers;
        private Color[] _baseEmission;

        public void InitCharger(Transform player)
        {
            _player    = player;
            _restScale = transform.localScale;
            _renderers = GetComponentsInChildren<Renderer>();
            _baseEmission = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] is ParticleSystemRenderer || _renderers[i] is LineRenderer) continue;
                var m = _renderers[i].material;
                if (m.HasProperty("_EmissionColor")) _baseEmission[i] = m.GetColor("_EmissionColor");
            }
        }

        void Update()
        {
            _stateTimer += Time.deltaTime;

            switch (_state)
            {
                case State.Approach:
                    ApproachTick();
                    if (_player != null
                        && Mathf.Abs(transform.position.z - _player.position.z) < lungeTriggerZ
                        && Time.time >= _lungeReadyAt)
                        EnterWindUp();
                    break;

                case State.WindUp:
                    // Hold position, pulse scale + emission.
                    float u = _stateTimer / windUpSeconds;
                    transform.localScale = _restScale * Mathf.Lerp(1f, 1.25f, u);
                    SetEmission(Color.Lerp(Color.black, telegraphColor, u));
                    if (_stateTimer >= windUpSeconds) EnterLunge();
                    break;

                case State.Lunge:
                    transform.Translate(Vector3.back * Config.moveSpeed * lungeSpeedMul * Time.deltaTime);
                    if (_stateTimer >= lungeSeconds) EnterApproach();
                    break;
            }

            if (transform.position.z < -8f) EscapeOffScreen();
        }

        void ApproachTick()
        {
            float dx = 0f;
            if (_player != null)
            {
                float diff = _player.position.x - transform.position.x;
                dx = Mathf.Clamp(diff, -1f, 1f) * lateralHomingSpeed * Time.deltaTime;
            }
            transform.Translate(new Vector3(dx, 0f, -Config.moveSpeed * Time.deltaTime));
        }

        void EnterApproach()
        {
            _state = State.Approach;
            _stateTimer = 0f;
            transform.localScale = _restScale;
            SetEmission(Color.black);
            _lungeReadyAt = Time.time + lungeCooldown;
        }

        void EnterWindUp()
        {
            _state = State.WindUp;
            _stateTimer = 0f;
        }

        void EnterLunge()
        {
            _state = State.Lunge;
            _stateTimer = 0f;
            transform.localScale = _restScale;
            SetEmission(telegraphColor); // hold red during dash
        }

        void SetEmission(Color c)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null || r is ParticleSystemRenderer || r is LineRenderer) continue;
                if (r.material != null && r.material.HasProperty("_EmissionColor"))
                    r.material.SetColor("_EmissionColor", _baseEmission[i] + c);
            }
        }
    }
}
