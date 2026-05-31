using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class CorridorScroller : MonoBehaviour
    {
        [SerializeField] private CorridorTile tilePrefab;
        [SerializeField] private int visibleTileCount = 5;
        [SerializeField] private float tileLength = 12f;

        private ObjectPool<CorridorTile> _pool;
        private readonly Queue<CorridorTile> _active = new Queue<CorridorTile>();
        private float _speed;
        private float _nextTileZ;

        public void Initialize(float scrollSpeed)
        {
            _speed = scrollSpeed;
            _pool = new ObjectPool<CorridorTile>(tilePrefab, visibleTileCount + 2);
            for (int i = 0; i < visibleTileCount; i++)
                SpawnTile();
        }

        void Update()
        {
            if (_speed <= 0f) return;
            float move = _speed * Time.deltaTime;
            foreach (CorridorTile tile in _active)
                tile.transform.Translate(Vector3.back * move);

            if (_active.Count > 0 && _active.Peek().transform.position.z < -tileLength)
            {
                _pool.Return(_active.Dequeue());
                SpawnTile();
            }
        }

        void SpawnTile()
        {
            CorridorTile tile = _pool.Get();
            tile.transform.position = new Vector3(0f, 0f, _nextTileZ);
            _active.Enqueue(tile);
            _nextTileZ += tileLength;
        }

        public void SetSpeed(float speed) => _speed = speed;
        public void Stop() => _speed = 0f;

        /// <summary>Current world-scroll speed. Read by EnvironmentScroller so the
        /// static set-dressing scrolls in lockstep with the corridor (incl. pause/stop).</summary>
        public float Speed => _speed;
    }
}
