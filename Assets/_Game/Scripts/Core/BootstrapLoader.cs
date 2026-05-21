using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrafAdvance
{
    public class BootstrapLoader : MonoBehaviour
    {
        void Start() => SceneManager.LoadScene("GameScene");
    }
}
