using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Scrolls discrete set-dressing props (pillars, beams, crates) toward the player in
    /// lockstep with <see cref="CorridorScroller"/>'s world speed, recycling each prop back to
    /// the far end once it passes behind the camera (masked by fog/DoF on re-entry).
    ///
    /// Continuous surfaces (floor, walls, rails, guide-strips) stay static — they're uniform,
    /// so no parallax cue is lost — and the lights + the player's overhead fixture stay anchored.
    /// Put this on a group whose direct children are the scrolling props.
    /// </summary>
    public class EnvironmentScroller : MonoBehaviour
    {
        [Tooltip("Local z behind the player where a prop is off-screen and recycles to the far end.")]
        [SerializeField] private float recycleZ = -12f;
        [Tooltip("Distance added on recycle — the repeating span of the prop layout.")]
        [SerializeField] private float loopSpan = 64f;

        private CorridorScroller _source;
        private Transform[] _props;

        void Start()
        {
            _source = FindAnyObjectByType<CorridorScroller>();
            int n = transform.childCount;
            _props = new Transform[n];
            for (int i = 0; i < n; i++) _props[i] = transform.GetChild(i);
        }

        void Update()
        {
            if (_source == null || _props == null) return;
            float speed = _source.Speed;
            if (speed <= 0f) return;

            float move = speed * Time.deltaTime;
            for (int i = 0; i < _props.Length; i++)
            {
                Transform t = _props[i];
                Vector3 p = t.localPosition;
                p.z -= move;
                if (p.z < recycleZ) p.z += loopSpan;
                t.localPosition = p;
            }
        }
    }
}
