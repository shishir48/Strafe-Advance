using UnityEngine;

namespace StrafAdvance
{
    public enum EnemyType { Grunt, Flanker, Elite }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "StrafAdvance/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public EnemyType enemyType;
        public int count = 5;
        public float spawnInterval = 1.5f;
    }
}
