using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Eski statik 2D OnGUI yerine sahneyi 3D animasyonlu, 
    /// havada süzülen ve kavis çizerek yuvalara kilitlenen interaktif yapboz moduna geçirir.
    /// Lab sahnesinde ise masadaki monitör istasyonuyla (ScreenStation) tam entegre çalışır.
    /// </summary>
    public class DNAPuzzleTester : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnPlay()
        {
            var stationCtrl = Object.FindAnyObjectByType<CameraStationController>();
            if (stationCtrl != null)
            {
                if (Object.FindAnyObjectByType<DNAAnimatedPuzzleController>() == null)
                {
                    var go = new GameObject("--- DNA Puzzle System (Lab Terminal) ---");
                    go.AddComponent<DNAPuzzleManager>();
                    var puzzleCtrl = go.AddComponent<DNAAnimatedPuzzleController>();
                    puzzleCtrl.InitializeForLabStation(stationCtrl);
                }
                return;
            }

            if (FindAnyObjectByType<DNAPuzzleTester>() == null && FindAnyObjectByType<DNAPuzzleManager>() == null) return;

            if (FindAnyObjectByType<DNAAnimatedPuzzleController>() == null)
            {
                var go = new GameObject("--- DNA Puzzle System (Animated 3D) ---");
                go.AddComponent<DNAPuzzleManager>();
                go.AddComponent<DNAAnimatedPuzzleController>();
            }
        }

        private void Awake()
        {
            var stationCtrl = Object.FindAnyObjectByType<CameraStationController>();
            if (GetComponent<DNAAnimatedPuzzleController>() == null)
            {
                var ctrl = gameObject.AddComponent<DNAAnimatedPuzzleController>();
                if (stationCtrl != null)
                {
                    ctrl.InitializeForLabStation(stationCtrl);
                }
            }
            enabled = false; // Eski OnGUI arayüzünü devre dışı bırak
        }
    }
}
