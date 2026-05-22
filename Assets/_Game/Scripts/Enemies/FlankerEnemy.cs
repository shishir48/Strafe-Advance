using UnityEngine;

namespace StrafAdvance
{
    public class FlankerEnemy : EnemyBase
    {
        private Transform _player;
        private const float CurveStrength = 2.5f;

        public void InitFlanker(Transform player) => _player = player;

        void Update()
        {
            // Forward movement at full moveSpeed (independent of curve)
            transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);

            // Lateral curve toward player X
            if (_player != null)
            {
                float xDiff = _player.position.x - transform.position.x;
                float lateralMove = Mathf.Sign(xDiff) * CurveStrength * Time.deltaTime;
                transform.Translate(Vector3.right * lateralMove, Space.World);
            }

            if (transform.position.z < -8f)
                EscapeOffScreen();
        }
    }
}
