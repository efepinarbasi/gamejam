using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lacuna
{
    /// <summary>Put this on any object in the MainMenu scene and wire the buttons' OnClick to it.</summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string labSceneName = "Lab";

        public void Play()
        {
            SceneManager.LoadScene(labSceneName);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
