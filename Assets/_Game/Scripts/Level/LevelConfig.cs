using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "StrafAdvance/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public string levelName = "Level 1";
        public WaveConfig[] waves;
        public GameObject bossPrefab;
        public float worldScrollSpeed = 4f;
        public string iapProductId = "free";
        public float parTimeSeconds = 120f;
    }
}
