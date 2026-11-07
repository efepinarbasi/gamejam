using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Eski statik 2D OnGUI yerine sahneyi 3D animasyonlu, 
    /// havada süzülen ve kavis çizerek yuvalara kilitlenen interaktif yapboz moduna geçirir.
    /// </summary>
    public class DNAPuzzleTester : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnPlay()
        {
            // Lab sahnesi gibi kendi kamera/istasyon kontrolcüsü olan sahnelerde veya
            // içinde DNAPuzzle bileşeni bulunmayan sahnelerde otomatik kurulum yapıp kamerayı değiştirme!
            if (FindAnyObjectByType<CameraStationController>() != null) return;
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
            // Eğer bu obje sahnede varsa, doğrudan yeni 3D animasyonlu kontrolcüye devret
            if (GetComponent<DNAAnimatedPuzzleController>() == null)
            {
                gameObject.AddComponent<DNAAnimatedPuzzleController>();
            }
            enabled = false; // Eski OnGUI arayüzünü devre dışı bırak
        }
    }
}
