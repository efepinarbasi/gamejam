using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lacuna
{
    /// <summary>
    /// The main menu's buttons. Put this on any object in the MainMenu scene and wire the
    /// buttons' OnClick to Play and Quit.
    /// Play sends the camera back to its starting position first and only then loads the
    /// Lab scene, so the player sees the camera pull away from the menu screen.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string labSceneName = "Lab";
        [Tooltip("The camera script of this scene. Empty = found automatically.")]
        [SerializeField] private CameraStationController cameraController;
        [Tooltip("Pause after the camera is back at its start position, before the Lab loads.")]
        [SerializeField, Min(0f)] private float loadDelay = 0.25f;

        private bool loading;

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        public void Play()
        {
            if (loading) return; // a second click while the camera is moving does nothing
            loading = true;
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            CameraStationController cam = cameraController != null
                ? cameraController
                : CameraStationController.Instance;

            if (cam != null)
            {
                // If the camera is still flying in, let it arrive first.
                while (cam.CurrentState == CameraStationController.State.Moving)
                    yield return null;

                if (cam.CurrentState == CameraStationController.State.AtStation)
                    cam.ReturnToBase();

                while (cam.CurrentState != CameraStationController.State.AtBase)
                    yield return null;
            }

            if (loadDelay > 0f) yield return new WaitForSeconds(loadDelay);
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