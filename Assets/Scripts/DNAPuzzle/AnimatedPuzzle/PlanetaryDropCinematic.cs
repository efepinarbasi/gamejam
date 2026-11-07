using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Biyo-Kapsülün (Drop Pod) seçili gezegenin atmosferine dalışını,
    /// ısı/soğuk kalkanını, kimyasal biyo-filtresini ve yüzeye iniş zaferini
    /// sinematik kamera açıları ve görsel efektlerle canlandıran sinematik motoru.
    /// </summary>
    public class PlanetaryDropCinematic : MonoBehaviour
    {
        public static PlanetaryDropCinematic Instance { get; private set; }

        private Camera cinematicCam;
        private Vector3 originalCamPos;
        private Quaternion originalCamRot;
        private float originalCamFov;

        private GameObject podObj;
        private Transform podCoreGlow;
        private Light podThrusterLight;
        private GameObject heatShieldObj;
        private Material heatShieldMat;
        private GameObject bioDomeObj;
        private Material bioDomeMat;

        private List<Transform> podExhaustParticles = new List<Transform>();
        private List<Transform> debrisPieces = new List<Transform>();
        private List<Transform> victoryParticles = new List<Transform>();

        private bool isCinematicActive = false;
        private float cinematicTimer = 0f;
        private string activeTelemetryStatus = "";
        private float currentAltitude = 450f;
        private float currentSpeed = 24.5f;
        private float hullTemperature = 250f;

        private AnimatedLaboratoryScreen labScreen;
        private DNAPuzzleAudio audioManager;
        private DNAPuzzleManager puzzleManager;

        public bool IsActive => isCinematicActive;
        public event Action OnCinematicFinished;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Initialize(Camera cam, AnimatedLaboratoryScreen lab, DNAPuzzleAudio audio, DNAPuzzleManager manager = null)
        {
            cinematicCam = cam;
            labScreen = lab;
            audioManager = audio;
            puzzleManager = manager;

            if (cinematicCam != null)
            {
                originalCamPos = cinematicCam.transform.position;
                originalCamRot = cinematicCam.transform.rotation;
                originalCamFov = cinematicCam.fieldOfView;
            }

            BuildDropPodChassis();
        }

        private void BuildDropPodChassis()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            // 1. Ana Biyo-Kapsül Gövdesi
            podObj = new GameObject("BioDropPod");
            podObj.transform.SetParent(transform, false);
            podObj.SetActive(false);

            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hull.name = "PodHull";
            hull.transform.SetParent(podObj.transform, false);
            hull.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            hull.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(hull.GetComponent<Collider>());

            var hullMat = new Material(litShader);
            hullMat.color = new Color(0.12f, 0.16f, 0.24f);
            if (hullMat.HasProperty("_Smoothness")) hullMat.SetFloat("_Smoothness", 0.90f);
            if (hullMat.HasProperty("_Metallic")) hullMat.SetFloat("_Metallic", 0.85f);
            hull.GetComponent<MeshRenderer>().material = hullMat;

            // 2. Kapsül Ön Gözlem Kubbesi (Cam Kokpit & İçindeki Canlı)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "CockpitCanopy";
            canopy.transform.SetParent(podObj.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            canopy.transform.localScale = Vector3.one * 0.48f;
            Destroy(canopy.GetComponent<Collider>());

            var canopyMat = new Material(unlitShader);
            canopyMat.color = new Color(0.2f, 0.9f, 1f, 0.85f);
            canopy.GetComponent<MeshRenderer>().material = canopyMat;
            podCoreGlow = canopy.transform;

            // 3. Arka Plazma İtiş Motoru Işığı
            GameObject lightGo = new GameObject("ThrusterLight");
            lightGo.transform.SetParent(podObj.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, -0.9f);
            podThrusterLight = lightGo.AddComponent<Light>();
            podThrusterLight.type = LightType.Point;
            podThrusterLight.range = 7f;
            podThrusterLight.intensity = 2.5f;
            podThrusterLight.color = new Color(0.2f, 0.8f, 1f);

            // 4. Atmosferik Reentry Isı Kalkanı
            heatShieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heatShieldObj.name = "HeatShieldCone";
            heatShieldObj.transform.SetParent(podObj.transform, false);
            heatShieldObj.transform.localPosition = new Vector3(0f, 0f, 0.72f);
            heatShieldObj.transform.localScale = new Vector3(1.2f, 1.2f, 0.4f);
            Destroy(heatShieldObj.GetComponent<Collider>());

            heatShieldMat = new Material(unlitShader);
            heatShieldMat.color = new Color(1f, 0.45f, 0.1f, 0.85f);
            heatShieldObj.GetComponent<MeshRenderer>().material = heatShieldMat;
            heatShieldObj.SetActive(false);

            // 5. Yüzey Koloni Biyo-Kubbesi (Zaferde açılan holografik üs)
            bioDomeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bioDomeObj.name = "ColonyBioDome";
            bioDomeObj.transform.SetParent(podObj.transform, false);
            bioDomeObj.transform.localPosition = Vector3.zero;
            bioDomeObj.transform.localScale = Vector3.one * 0.1f;
            Destroy(bioDomeObj.GetComponent<Collider>());

            bioDomeMat = new Material(unlitShader);
            bioDomeMat.color = new Color(0.2f, 1f, 0.6f, 0.65f);
            bioDomeObj.GetComponent<MeshRenderer>().material = bioDomeMat;
            bioDomeObj.SetActive(false);

            // 6. İtiş Parçacıkları (Exhaust Jet Particles)
            GameObject exhaustParent = new GameObject("ExhaustSystem");
            exhaustParent.transform.SetParent(podObj.transform, false);

            for (int i = 0; i < 14; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.name = $"Jet_{i}";
                p.transform.SetParent(exhaustParent.transform, false);
                p.transform.localPosition = new Vector3(0f, 0f, -0.8f - (i * 0.12f));
                float s = UnityEngine.Random.Range(0.08f, 0.22f);
                p.transform.localScale = Vector3.one * s;
                Destroy(p.GetComponent<Collider>());

                var pMat = new Material(unlitShader);
                pMat.color = new Color(0.3f, 0.9f, 1f, 0.9f);
                p.GetComponent<MeshRenderer>().material = pMat;
                podExhaustParticles.Add(p.transform);
            }

            // 7. Enkaz & Kutlama Parçacıkları
            BuildDebrisAndCelebration(unlitShader);
        }

        private void BuildDebrisAndCelebration(Shader unlitShader)
        {
            GameObject debrisParent = new GameObject("DebrisPool");
            debrisParent.transform.SetParent(transform, false);

            for (int i = 0; i < 20; i++)
            {
                GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cube);
                d.name = $"Debris_{i}";
                d.transform.SetParent(debrisParent.transform, false);
                d.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.08f, 0.18f);
                Destroy(d.GetComponent<Collider>());

                var dMat = new Material(unlitShader);
                dMat.color = new Color(1f, 0.35f, 0.1f);
                d.GetComponent<MeshRenderer>().material = dMat;
                d.SetActive(false);
                debrisPieces.Add(d.transform);
            }

            for (int i = 0; i < 25; i++)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = $"VictoryStar_{i}";
                s.transform.SetParent(debrisParent.transform, false);
                s.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.09f, 0.20f);
                Destroy(s.GetComponent<Collider>());

                var sMat = new Material(unlitShader);
                sMat.color = new Color(1f, 0.88f, 0.2f);
                s.GetComponent<MeshRenderer>().material = sMat;
                s.SetActive(false);
                victoryParticles.Add(s.transform);
            }
        }

        public void PlayDropSequence(PlanetData planet, int planetIdx, SurvivalSimulator simulator, Vector3 incubatorPos, DNAPuzzleManager manager = null)
        {
            if (manager != null) puzzleManager = manager;
            StopAllCoroutines();
            StartCoroutine(ExecuteCinematicRoutine(planet, planetIdx, simulator, incubatorPos));
        }

        public void SkipCinematic()
        {
            isCinematicActive = false;
            StopAllCoroutines();
            ResetCameraAndEffects();
        }

        private IEnumerator ExecuteCinematicRoutine(PlanetData planet, int planetIdx, SurvivalSimulator simulator, Vector3 incubatorPos)
        {
            isCinematicActive = true;
            cinematicTimer = 0f;

            Vector3 planetTargetPos = labScreen != null ? labScreen.GetSimulationChamberPosition(planetIdx) : new Vector3((planetIdx - 1) * 4.85f, 5.35f, 4.3f);

            // Kapsülü Başlangıç Noktasına (Kuluçka Yuvasına) Taşı
            podObj.transform.position = incubatorPos;
            podObj.transform.rotation = Quaternion.LookRotation(planetTargetPos - incubatorPos);
            podObj.SetActive(true);
            heatShieldObj.SetActive(false);
            bioDomeObj.SetActive(false);

            // Renk ve Işık Temasını Gezegene Uyarla
            ConfigureCinematicTheme(planetIdx);

            activeTelemetryStatus = "🔬 SİMÜLASYON TESTİ: Biyo-Kapsül çevre test odasına aktarılıyor...";
            currentAltitude = 480f;
            currentSpeed = 28.5f;
            hullTemperature = 180f;

            audioManager?.PlayLaunch();

            // 1. FAZ: Fırlatma ve Gökyüzüne Tırmanış (Kuluçkadan Gezegene Doğru İlerleme)
            float t = 0f;
            Vector3 startPos = incubatorPos;
            Vector3 midFlightPos = Vector3.Lerp(startPos, planetTargetPos, 0.55f) + new Vector3(0f, 0.8f, -1.8f);

            while (t < 1.0f)
            {
                t += Time.deltaTime * 1.1f;
                float curve = Mathf.SmoothStep(0f, 1f, t);
                podObj.transform.position = Vector3.Lerp(startPos, midFlightPos, curve);
                podObj.transform.rotation = Quaternion.Slerp(
                    podObj.transform.rotation, 
                    Quaternion.LookRotation(planetTargetPos - podObj.transform.position), 
                    Time.deltaTime * 8f
                );

                // Kameranın Kapsülü ve Gezegeni Takip Etmesi
                if (cinematicCam != null)
                {
                    Vector3 camGoal = Vector3.Lerp(originalCamPos, planetTargetPos + new Vector3(0f, -0.6f, -7.5f), curve * 0.7f);
                    cinematicCam.transform.position = Vector3.Lerp(cinematicCam.transform.position, camGoal, Time.deltaTime * 5f);
                    cinematicCam.transform.LookAt(podObj.transform.position + Vector3.forward * 1.5f);
                }

                currentAltitude = Mathf.Lerp(480f, 180f, curve);
                currentSpeed = Mathf.Lerp(28.5f, 42.0f, curve);
                yield return null;
            }

            // 2. FAZ: Atmosfere Giriş (Atmospheric Entry - Alev / Soğuk Şok Dalgası)
            heatShieldObj.SetActive(true);
            audioManager?.PlayAtmosphericRoar();

            activeTelemetryStatus = $"🔥 ATMOSFERE GİRİŞ: {planet.planetName} termal kalkan testi...";
            float entryTimer = 0f;
            Vector3 entryStart = podObj.transform.position;
            Vector3 entryEnd = planetTargetPos + new Vector3(0f, -0.15f, -1.2f);

            while (entryTimer < 1.1f)
            {
                entryTimer += Time.deltaTime;
                float progress = entryTimer / 1.1f;
                podObj.transform.position = Vector3.Lerp(entryStart, entryEnd, progress);

                // Atmosferik Türbülans ve Kamera Sarsıntısı
                Vector3 shake = UnityEngine.Random.insideUnitSphere * 0.08f;
                podObj.transform.position += shake;

                hullTemperature = Mathf.Lerp(350f, 1840f, progress);
                currentAltitude = Mathf.Lerp(180f, 45f, progress);
                currentSpeed = Mathf.Lerp(42.0f, 16.5f, progress);

                // Isı kalkanı nabzı
                float pulse = 1.1f + Mathf.Sin(entryTimer * 25f) * 0.15f;
                heatShieldObj.transform.localScale = new Vector3(pulse, pulse, 0.45f);

                yield return null;
            }

            // Simülatör durumunu kontrol et
            EvaluationResult eval = (puzzleManager != null) ? puzzleManager.EvaluateCreatureSurvival() :
                                   (DNAPuzzleManager.Instance != null ? DNAPuzzleManager.Instance.EvaluateCreatureSurvival() : new EvaluationResult());
            Dictionary<StatType, int> curStats = (puzzleManager != null) ? puzzleManager.CalculateTotalStats() :
                                                 (DNAPuzzleManager.Instance != null ? DNAPuzzleManager.Instance.CalculateTotalStats() : new Dictionary<StatType, int>());
            bool heatColdPass = CheckHeatColdPass(planet, curStats);

            if (!heatColdPass)
            {
                yield return TriggerFailureSequence("💥 KRİTİK HASAR: Kapsül aşırı sıcaklık/soğuk şokuna dayanamadı!");
                yield break;
            }

            // 3. FAZ: Kimyasal Biyosfer ve Radyasyon Dalgaları
            heatShieldObj.SetActive(false);
            activeTelemetryStatus = "☣️ BİYOSFER TARAMASI: Asit gölleri ve radyasyon kalkanı testi...";
            audioManager?.PlayShieldDeflection();

            float chemTimer = 0f;
            while (chemTimer < 0.95f)
            {
                chemTimer += Time.deltaTime;
                hullTemperature = Mathf.Lerp(1840f, 210f, chemTimer / 0.95f);
                currentAltitude = Mathf.Lerp(45f, 12f, chemTimer / 0.95f);
                currentSpeed = Mathf.Lerp(16.5f, 5.2f, chemTimer / 0.95f);

                // Biyo-kalkan parlaması
                if (podCoreGlow != null)
                {
                    float glow = 0.5f + Mathf.Sin(chemTimer * 20f) * 0.15f;
                    podCoreGlow.localScale = Vector3.one * glow;
                }
                yield return null;
            }

            bool chemPass = CheckChemPass(planet, curStats);
            if (!chemPass)
            {
                yield return TriggerFailureSequence("☣️ KRİTİK HASAR: Asit ve radyasyon biyo-kalkanı deldi!");
                yield break;
            }

            // 4. FAZ: Yüzey İnişi & Retroroketler (Touchdown)
            activeTelemetryStatus = "🌍 İNİŞ: Retroroketler ateşleniyor, yüzey teması gerçekleşiyor...";
            audioManager?.PlayTouchdown();

            float landTimer = 0f;
            Vector3 finalTouchdown = planetTargetPos + new Vector3(0f, -0.65f, -0.85f);
            Vector3 landStart = podObj.transform.position;

            while (landTimer < 0.85f)
            {
                landTimer += Time.deltaTime;
                float lp = Mathf.SmoothStep(0f, 1f, landTimer / 0.85f);
                podObj.transform.position = Vector3.Lerp(landStart, finalTouchdown, lp);
                currentAltitude = Mathf.Lerp(12f, 0f, lp);
                currentSpeed = Mathf.Lerp(5.2f, 0f, lp);
                yield return null;
            }

            bool fullSuccess = eval.isSuccess;
            if (!fullSuccess)
            {
                yield return TriggerFailureSequence("⚠️ HASAR: Yüksek yerçekimi ve genetik metabolizma canlıyı eledi.");
                yield break;
            }

            // 5. FAZ: Zafer & Biyo-Kubbe Koloni Açılışı
            yield return TriggerVictorySequence(planetIdx);
        }

        private bool CheckHeatColdPass(PlanetData planet, Dictionary<StatType, int> stats)
        {
            int reqH = planet.GetRequiredStatValue(StatType.HeatResistance);
            int curH = stats.ContainsKey(StatType.HeatResistance) ? stats[StatType.HeatResistance] : 0;
            int reqC = planet.GetRequiredStatValue(StatType.ColdResistance);
            int curC = stats.ContainsKey(StatType.ColdResistance) ? stats[StatType.ColdResistance] : 0;
            return curH >= reqH && curC >= reqC;
        }

        private bool CheckChemPass(PlanetData planet, Dictionary<StatType, int> stats)
        {
            int reqT = planet.GetRequiredStatValue(StatType.ToxicityFilter);
            int curT = stats.ContainsKey(StatType.ToxicityFilter) ? stats[StatType.ToxicityFilter] : 0;
            int reqR = planet.GetRequiredStatValue(StatType.RadiationShield);
            int curR = stats.ContainsKey(StatType.RadiationShield) ? stats[StatType.RadiationShield] : 0;
            return curT >= reqT && curR >= reqR;
        }

        private IEnumerator TriggerFailureSequence(string reason)
        {
            activeTelemetryStatus = reason;
            audioManager?.PlayExplosion();

            // Kapsülü Gizle, Enkazı Patlat
            podObj.SetActive(false);
            Vector3 expPos = podObj.transform.position;

            for (int i = 0; i < debrisPieces.Count; i++)
            {
                var d = debrisPieces[i];
                d.position = expPos;
                d.gameObject.SetActive(true);
            }

            float debTimer = 0f;
            while (debTimer < 1.4f)
            {
                debTimer += Time.deltaTime;
                for (int i = 0; i < debrisPieces.Count; i++)
                {
                    float angle = (i / (float)debrisPieces.Count) * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), UnityEngine.Random.Range(-0.5f, 0.5f));
                    debrisPieces[i].position += dir * (3.5f * Time.deltaTime);
                    debrisPieces[i].Rotate(Vector3.one * 180f * Time.deltaTime);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.8f);
            ResetCameraAndEffects();
        }

        private IEnumerator TriggerVictorySequence(int planetIdx)
        {
            activeTelemetryStatus = "🏆 ZAFER: Canlı başarıyla adapte oldu! Yeni Koloni kuruldu!";
            audioManager?.PlayVictory();

            // Holografik Biyo-Kubbeyi Büyüt
            bioDomeObj.SetActive(true);
            float domeTimer = 0f;
            while (domeTimer < 1.0f)
            {
                domeTimer += Time.deltaTime * 1.5f;
                float dScale = Mathf.Lerp(0.1f, 1.85f, Mathf.SmoothStep(0f, 1f, domeTimer));
                bioDomeObj.transform.localScale = Vector3.one * dScale;
                yield return null;
            }

            // Yıldız Kutlama Parçacıklarını Havai Fişek Gibi Fırlat
            Vector3 center = podObj.transform.position;
            for (int i = 0; i < victoryParticles.Count; i++)
            {
                var s = victoryParticles[i];
                s.position = center;
                s.gameObject.SetActive(true);
            }

            // Simülasyon Odasına Test Başarısını Bildir
            labScreen?.TriggerClickJuice(planetIdx);

            float starTimer = 0f;
            while (starTimer < 1.6f)
            {
                starTimer += Time.deltaTime;
                for (int i = 0; i < victoryParticles.Count; i++)
                {
                    float a = (i / (float)victoryParticles.Count) * Mathf.PI * 2f;
                    Vector3 upDir = new Vector3(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.8f + 1.2f, UnityEngine.Random.Range(-0.3f, 0.3f));
                    victoryParticles[i].position += upDir * (2.8f * Time.deltaTime);
                }
                yield return null;
            }

            yield return new WaitForSeconds(1.2f);
            ResetCameraAndEffects();
        }

        private void ResetCameraAndEffects()
        {
            isCinematicActive = false;
            if (podObj != null) podObj.SetActive(false);
            foreach (var d in debrisPieces) if (d != null) d.gameObject.SetActive(false);
            foreach (var v in victoryParticles) if (v != null) v.gameObject.SetActive(false);

            // Kamerayı Orijinal Yapboz Konumuna Geri Döndür
            if (cinematicCam != null)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothReturnCameraRoutine());
            }

            OnCinematicFinished?.Invoke();
        }

        private IEnumerator SmoothReturnCameraRoutine()
        {
            float t = 0f;
            Vector3 startP = cinematicCam.transform.position;
            Quaternion startR = cinematicCam.transform.rotation;

            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float p = t / 0.6f;
                cinematicCam.transform.position = Vector3.Lerp(startP, originalCamPos, p);
                cinematicCam.transform.rotation = Quaternion.Slerp(startR, originalCamRot, p);
                yield return null;
            }

            cinematicCam.transform.position = originalCamPos;
            cinematicCam.transform.rotation = originalCamRot;
            cinematicCam.fieldOfView = originalCamFov;
        }

        private void ConfigureCinematicTheme(int planetIdx)
        {
            if (planetIdx == 0) // 🔥 Pyros-9 (Akkor Lav)
            {
                heatShieldMat.color = new Color(1f, 0.42f, 0.08f, 0.9f);
                podThrusterLight.color = new Color(1f, 0.5f, 0.15f);
                if (podCoreGlow != null) podCoreGlow.GetComponent<MeshRenderer>().material.color = new Color(1f, 0.7f, 0.2f);
            }
            else if (planetIdx == 1) // ❄️ Cryo-Kore (Buzul)
            {
                heatShieldMat.color = new Color(0.2f, 0.85f, 1f, 0.85f);
                podThrusterLight.color = new Color(0.3f, 0.9f, 1f);
                if (podCoreGlow != null) podCoreGlow.GetComponent<MeshRenderer>().material.color = new Color(0.4f, 0.95f, 1f);
            }
            else // ☣️ Toxis-Prime (Asit & Radyasyon)
            {
                heatShieldMat.color = new Color(0.45f, 1f, 0.35f, 0.9f);
                podThrusterLight.color = new Color(0.7f, 0.3f, 1f);
                if (podCoreGlow != null) podCoreGlow.GetComponent<MeshRenderer>().material.color = new Color(0.4f, 1f, 0.4f);
            }
        }

        private void Update()
        {
            if (!isCinematicActive) return;

            cinematicTimer += Time.deltaTime;

            // Arka İtiş Plazma Alevi Animasyonu
            for (int i = 0; i < podExhaustParticles.Count; i++)
            {
                if (podExhaustParticles[i] != null)
                {
                    float jitter = Mathf.Sin(cinematicTimer * 45f + i) * 0.06f;
                    podExhaustParticles[i].localPosition = new Vector3(jitter, jitter, -0.8f - (i * 0.11f));
                }
            }

            if (podThrusterLight != null)
            {
                podThrusterLight.intensity = 2.2f + Mathf.Sin(cinematicTimer * 30f) * 0.6f;
            }
        }

        private void OnGUI()
        {
            if (!isCinematicActive) return;

            float screenW = Screen.width;
            float screenH = Screen.height;

            // 1. Üst Sinematik Telemetri Çubuğu (Letterbox)
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, screenW, 58), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(20, 8, screenW - 40, 48));
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.2f, 1f, 0.85f);
            GUILayout.Label("🛰️ BİYO-KAPSÜL TELEMETRİ TAKİBİ", EditorBold(13));
            GUI.color = Color.white;

            GUILayout.Space(25);
            GUILayout.Label($"İRTİFA: {Mathf.Max(0, (int)currentAltitude)} km", EditorBold(12));
            GUILayout.Label($"HIZ: {currentSpeed:F1} km/s", EditorBold(12));
            GUI.color = hullTemperature > 1200f ? Color.red : new Color(1f, 0.75f, 0.2f);
            GUILayout.Label($"GÖVDE ISISI: {(int)hullTemperature}°C", EditorBold(12));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
            if (GUILayout.Button("⏩ HIZLI GEÇ", GUILayout.Height(28), GUILayout.Width(100)))
            {
                SkipCinematic();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 2. Alt Sinematik Durum & Altyazı Çubuğu
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, screenH - 52, screenW, 52), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(30, screenH - 42, screenW - 60, 36));
            GUI.color = activeTelemetryStatus.Contains("KRİTİK") || activeTelemetryStatus.Contains("HASAR") ? 
                        new Color(1f, 0.35f, 0.35f) : 
                        (activeTelemetryStatus.Contains("ZAFER") ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.92f, 0.35f));
            GUILayout.Label(activeTelemetryStatus, EditorCenter(14));
            GUI.color = Color.white;
            GUILayout.EndArea();
        }

        private GUIStyle EditorBold(int size)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontStyle = FontStyle.Bold;
            s.fontSize = size;
            return s;
        }

        private GUIStyle EditorCenter(int size)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontStyle = FontStyle.Bold;
            s.fontSize = size;
            s.alignment = TextAnchor.MiddleCenter;
            return s;
        }
    }
}
