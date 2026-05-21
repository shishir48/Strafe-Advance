using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "StrafAdvance/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public int maxHp = 30;
        public int contactDamage = 10;
        public float moveSpeed = 3f;
        public float fireRate = 2f;
        public int bulletDamage = 8;
    }
}
