#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNAPuzzle.Editor
{
    public static class DNAPuzzleEditorMenu
    {
        [MenuItem("DNA Puzzle/1. Örnek Varlıkları Oluştur (Gezegenler ve Genler)", false, 1)]
        public static void GenerateScriptableObjectAssets()
        {
            string rootDir = "Assets/DNA_Data";
            string planetsDir = $"{rootDir}/Planets";
            string genesDir = $"{rootDir}/Genes";

            if (!Directory.Exists(planetsDir)) Directory.CreateDirectory(planetsDir);
            if (!Directory.Exists(genesDir)) Directory.CreateDirectory(genesDir);

            // Gezegenleri oluştur ve kaydet
            var planets = DNAPuzzleSampleData.CreateSamplePlanets();
            foreach (var p in planets)
            {
                string assetPath = $"{planetsDir}/{p.planetId}.asset";
                AssetDatabase.CreateAsset(p, assetPath);
            }

            // Genleri oluştur ve kaydet
            var genes = DNAPuzzleSampleData.CreateSampleGenes();
            foreach (var g in genes)
            {
                string assetPath = $"{genesDir}/{g.geneId}.asset";
                AssetDatabase.CreateAsset(g, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DNA Puzzle Kurulumu Başarılı!",
                $"3 Gezegen ve 21+ Gelişmiş Biyolojik Gen başarıyla '{rootDir}' klasörüne oluşturuldu!\n\nArtık Unity Inspector'dan istediğiniz gibi düzenleyebilir veya yenilerini ekleyebilirsiniz.",
                "Harika!"
            );
        }

        [MenuItem("DNA Puzzle/2. Oynanabilir Test Sahnesini Kur", false, 2)]
        public static void SetupDemoInCurrentScene()
        {
            var existingManager = Object.FindAnyObjectByType<DNAPuzzleManager>();
            if (existingManager != null)
            {
                Selection.activeGameObject = existingManager.gameObject;
                EditorUtility.DisplayDialog("Zaten Mevcut", "Sahnede 'DNAPuzzleManager' zaten bulunuyor! Seçildi.", "Tamam");
                return;
            }

            var go = new GameObject("--- DNA Puzzle System ---");
            go.AddComponent<DNAPuzzleManager>();
            go.AddComponent<DNAPuzzleTester>();

            // Kameranın olduğundan emin ol
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camGo.tag = "MainCamera";
            }
            else
            {
                Camera.main.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
            }

            Selection.activeGameObject = go;
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog(
                "Sahne Hazır!",
                "DNA Puzzle Sistemi sahneye eklendi!\n\nŞimdi Unity'de doğrudan ▶ PLAY butonuna basarak oyunu hemen test edebilir ve oynayabilirsiniz!",
                "Hemen Dene!"
            );
        }
    }
}
#endif
