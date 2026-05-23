using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Fast melee rusher. No shooting — closes distance fast, high contact damage.
    /// Homes on player x-axis while charging forward in -z so dodge timing matters.
    /// </summary>
    public class ChargerEnemy : EnemyBase
    {
        [SerializeField] private float lateralHomingSpeed = 4f;
        private Transform _player;

        public void InitCharger(Transform player) { _player = player; }

        void Update()
        {
            float dx = 0f;
            if (_player != null)
            {
                float diff = _player.position.x - transform.position.x;
                dx = Mathf.Clamp(diff, -1f, 1f) * lateralHomingSpeed * Time.deltaTime;
            }
            transform.Translate(new Vector3(dx, 0f, -Config.moveSpeed * Time.deltaTime));
            if (transform.position.z < -8f) EscapeOffScreen();
        }
    }
}
