using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "StrafAdvance/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        public int maxHp = 100;
        public float strafeSpeed = 8f;
        public float strafeLimit = 3f;
        public float fireRate = 0.3f;
        public int bulletDamage = 10;
        public float homingStrength = 5f;
    }
}
