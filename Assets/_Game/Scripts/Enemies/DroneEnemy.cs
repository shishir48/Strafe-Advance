using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Small fast fragile enemy that flocks with other drones via simple boids:
    ///   • <b>Cohesion</b>  — steer toward neighbor centroid
    ///   • <b>Separation</b> — push away from too-close neighbors
    ///   • <b>Advance</b>   — bias toward player (-z + lateral)
    ///
    /// Maintains a static registry so each drone can find neighbors in O(n) per drone.
    /// Good for small swarms (≤ 30 drones); for larger flocks switch to a uniform grid.
    /// </summary>
    public class DroneEnemy : EnemyBase
    {
        private static readonly List<DroneEnemy> All = new List<DroneEnemy>(32);

        [Header("Flocking")]
        [SerializeField] private float neighborRadius = 2.5f;
        [SerializeField] private float separationRadius = 1.0f;
        [SerializeField] private float cohesionWeight   = 0.6f;
        [SerializeField] private float separationWeight = 1.4f;
        [SerializeField] private float advanceWeight    = 1.2f;
        [SerializeField] private float playerHomingWeight = 0.5f;
        [SerializeField] private float maxSteerPerSecond = 4f;

        private Transform _player;
        private Vector3 _velocity;

        public void InitDrone(Transform player)
        {
            _player = player;
            _velocity = Vector3.back * Config.moveSpeed; // initial forward bias
        }

        void OnEnable()  => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            Vector3 desired = Vector3.zero;

            // Advance (constant forward bias toward player z)
            desired += Vector3.back * advanceWeight;

            // Player lateral homing
            if (_player != null)
            {
                float dx = _player.position.x - transform.position.x;
                desired += new Vector3(Mathf.Clamp(dx, -1f, 1f), 0f, 0f) * playerHomingWeight;
            }

            // Cohesion + Separation
            Vector3 centroid = Vector3.zero;
            Vector3 separation = Vector3.zero;
            int neighbors = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var other = All[i];
                if (other == null || other == this) continue;
                Vector3 toOther = other.transform.position - transform.position;
                float d = toOther.magnitude;
                if (d > neighborRadius || d <= 0.001f) continue;
                centroid += other.transform.position;
                neighbors++;
                if (d < separationRadius) separation -= toOther / d; // push away normalised
            }
            if (neighbors > 0)
            {
                centroid /= neighbors;
                desired += (centroid - transform.position).normalized * cohesionWeight;
            }
            desired += separation * separationWeight;

            // Steer toward desired direction
            Vector3 targetVel = desired.normalized * Config.moveSpeed;
            _velocity = Vector3.MoveTowards(_velocity, targetVel, maxSteerPerSecond * Time.deltaTime);
            transform.position += _velocity * Time.deltaTime;

            if (transform.position.z < -8f) EscapeOffScreen();
        }
    }
}
