using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Yüksek Teknolojili, Düzenli Siber Biyo-Mühendislik Laboratuvar Ekranı (Clean Sci-Fi Lab Screen).
    /// Karmaşık ve pikselli kutular yerine:
    /// - Derinlik hissi veren zarif siber-cam arka plan paneli
    /// - Üstte yuvalarla çakışmayan ince, kristal netliğinde Ortam Simülasyon Teşhis Bandı
    /// - Sol tarafta canlı için sade ve şık kuluçka halkası (Specimen Containment Ring)
    /// - Kenarlarda zarif, yumuşak biyo-telemetri ekolayzır barları
    /// - Yüksek çözünürlüklü (High-Res Anti-Aliased) 3D tipografi
    /// </summary>
    public class AnimatedLaboratoryScreen : MonoBehaviour
    {
        private TextMesh activeEnvStatusText;
        private Transform envBannerTrans;
        private Material envBannerBorderMat;
        private Light labCeilingLight;
        private Light labAmbientGlow;
        private List<Transform> equalizerBars = new List<Transform>();

        private int activeEnvironmentIdx = 0;
        private float animTimer = 0f;
        private Coroutine activeClickJuice;

        public event Action<int> OnEnvironmentSelected;

        public void Initialize()
        {
            BuildCleanLaboratoryTerminal();
            BuildDiagnosticEqualizerBars();
            SetEnvironment(0);
        }

        private void BuildCleanLaboratoryTerminal()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            // 1. Ana Laboratuvar Terminal Ekran Camı (Dark Sleek Glass Display)
            GameObject screenQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenQuad.name = "LabMonitorScreen";
            screenQuad.transform.SetParent(transform, false);
            screenQuad.transform.localPosition = new Vector3(0f, 0.20f, 5.2f);
            screenQuad.transform.localScale = new Vector3(34f, 17f, 1f);
            Destroy(screenQuad.GetComponent<Collider>());

            var screenMat = new Material(unlitShader);
            screenMat.color = new Color(0.012f, 0.018f, 0.032f, 0.98f);
            screenQuad.GetComponent<MeshRenderer>().material = screenMat;

            // 2. Çok İnce, Yumuşak Siber Koordinat Izgaraları (Gözü yormayan düşük parlaklık)
            Color softGridCol = new Color(0.15f, 0.45f, 0.75f, 0.07f);
            for (int i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                CreateGridLine(new Vector3(i * 4.6f, 0.20f, 5.15f), new Vector3(0.02f, 16f, 0.01f), softGridCol);
            }
            for (int j = -2; j <= 2; j++)
            {
                CreateGridLine(new Vector3(0f, 0.20f + j * 3.4f, 5.15f), new Vector3(32f, 0.02f, 0.01f), softGridCol);
            }

            // 3. Zarif, İnce Çevresel Simülasyon Teşhis Bandı (Slotların tam üstünde, boşluklu ve yüksekte)
            GameObject bannerObj = new GameObject("EnvSimulationBanner");
            bannerObj.transform.SetParent(transform, false);
            bannerObj.transform.localPosition = new Vector3(1.35f, 4.15f, 3.2f);
            envBannerTrans = bannerObj.transform;

            // Arka Cam Panel
            GameObject bPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bPlate.name = "BannerPlate";
            bPlate.transform.SetParent(bannerObj.transform, false);
            bPlate.transform.localScale = new Vector3(14.8f, 0.68f, 0.05f);
            Destroy(bPlate.GetComponent<Collider>());
            var bPlateMat = new Material(unlitShader);
            bPlateMat.color = new Color(0.02f, 0.04f, 0.08f, 0.90f);
            bPlate.GetComponent<MeshRenderer>().material = bPlateMat;

            // İnce Neon Çerçeve
            GameObject bBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bBorder.name = "BannerBorder";
            bBorder.transform.SetParent(bannerObj.transform, false);
            bBorder.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            bBorder.transform.localScale = new Vector3(14.92f, 0.74f, 0.03f);
            Destroy(bBorder.GetComponent<Collider>());
            envBannerBorderMat = new Material(unlitShader);
            envBannerBorderMat.color = new Color(1f, 0.45f, 0.12f, 0.85f);
            bBorder.GetComponent<MeshRenderer>().material = envBannerBorderMat;

            // Yüksek Çözünürlüklü Vektör Kalitesinde Keskin Metin (Font size 84, küçük char size = Kristal Netlik)
            GameObject envTextObj = new GameObject("ActiveEnvStatusText");
            envTextObj.transform.SetParent(bannerObj.transform, false);
            envTextObj.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            activeEnvStatusText = envTextObj.AddComponent<TextMesh>();
            activeEnvStatusText.text = "🧪 HEDEF ORTAM: 🔥 PYROS-9 (Magma Dünyası)  |  480°C  |  3.2 ATM  |  TERMAL STABİLİTE AKTİF";
            activeEnvStatusText.characterSize = 0.023f;
            activeEnvStatusText.fontSize = 84;
            activeEnvStatusText.alignment = TextAlignment.Center;
            activeEnvStatusText.anchor = TextAnchor.MiddleCenter;
            activeEnvStatusText.color = new Color(1f, 0.88f, 0.35f);
            activeEnvStatusText.fontStyle = FontStyle.Bold;

            // 4. Laboratuvar Işıkları
            GameObject ceilingLightObj = new GameObject("LabCeilingLight");
            ceilingLightObj.transform.SetParent(transform, false);
            ceilingLightObj.transform.localPosition = new Vector3(0f, 5.5f, 1.0f);
            labCeilingLight = ceilingLightObj.AddComponent<Light>();
            labCeilingLight.type = LightType.Point;
            labCeilingLight.color = new Color(0.92f, 0.97f, 1f);
            labCeilingLight.intensity = 1.3f;
            labCeilingLight.range = 16f;

            GameObject ambientLightObj = new GameObject("LabAmbientGlow");
            ambientLightObj.transform.SetParent(transform, false);
            ambientLightObj.transform.localPosition = new Vector3(0f, 0.5f, 3.2f);
            labAmbientGlow = ambientLightObj.AddComponent<Light>();
            labAmbientGlow.type = LightType.Point;
            labAmbientGlow.color = new Color(1f, 0.45f, 0.12f);
            labAmbientGlow.intensity = 1.1f;
            labAmbientGlow.range = 14f;
        }

        private void CreateGridLine(Vector3 localPos, Vector3 localScale, Color col)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GridLine";
            line.transform.SetParent(transform, false);
            line.transform.localPosition = localPos;
            line.transform.localScale = localScale;
            Destroy(line.GetComponent<Collider>());

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            var mat = new Material(unlit);
            mat.color = col;
            line.GetComponent<MeshRenderer>().material = mat;
        }

        private void BuildDiagnosticEqualizerBars()
        {
            equalizerBars.Clear();

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            var barMat = new Material(unlit);
            barMat.color = new Color(0.2f, 0.85f, 1f, 0.65f);

            // Ekranın sağ kenarında taşmayan 6 adet ince telemetri çubuğu
            for (int b = 0; b < 6; b++)
            {
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"EqBar_{b}";
                bar.transform.SetParent(transform, false);
                bar.transform.localPosition = new Vector3(14.6f + b * 0.18f, 1.45f, 4.6f);
                bar.transform.localScale = new Vector3(0.06f, 0.35f, 0.01f);
                Destroy(bar.GetComponent<Collider>());
                bar.GetComponent<MeshRenderer>().material = barMat;
                equalizerBars.Add(bar.transform);
            }
        }

        public void SetEnvironment(int idx)
        {
            activeEnvironmentIdx = Mathf.Clamp(idx, 0, 2);

            Color themeCol;
            string envInfo;

            switch (activeEnvironmentIdx)
            {
                case 1:
                    themeCol = new Color(0.2f, 0.85f, 1f);
                    envInfo = "🧪 HEDEF ORTAM: ❄️ CRYO-KORE (Buzul Dünyası)  |  -85°C  |  0.8 ATM  |  KİYO-STABİLİTE AKTİF";
                    break;
                case 2:
                    themeCol = new Color(0.25f, 1f, 0.5f);
                    envInfo = "🧪 HEDEF ORTAM: ☣️ TOXIS-PRIME (Asit Dünyası)  |  TOKSİK  |  5.4 ATM  |  BİYO-KORUMA AKTİF";
                    break;
                default:
                    themeCol = new Color(1f, 0.45f, 0.12f);
                    envInfo = "🧪 HEDEF ORTAM: 🔥 PYROS-9 (Magma Dünyası)  |  480°C  |  3.2 ATM  |  TERMAL STABİLİTE AKTİF";
                    break;
            }

            if (envBannerBorderMat != null)
            {
                envBannerBorderMat.color = themeCol;
            }
            if (activeEnvStatusText != null)
            {
                activeEnvStatusText.text = envInfo;
                activeEnvStatusText.color = themeCol;
            }
            if (labAmbientGlow != null)
            {
                labAmbientGlow.color = themeCol;
            }

            OnEnvironmentSelected?.Invoke(activeEnvironmentIdx);
        }

        public void TriggerClickJuice(int idx)
        {
            if (!gameObject.activeInHierarchy) return;
            if (activeClickJuice != null) StopCoroutine(activeClickJuice);
            activeClickJuice = StartCoroutine(ClickJuiceRoutine());
        }

        private IEnumerator ClickJuiceRoutine()
        {
            if (!gameObject.activeInHierarchy || envBannerTrans == null) yield break;
            float elapsed = 0f;
            float dur = 0.28f;
            Vector3 baseS = Vector3.one;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                envBannerTrans.localScale = baseS * punch;
                yield return null;
            }

            envBannerTrans.localScale = baseS;
        }

        public Vector3 GetSimulationChamberPosition(int idx)
        {
            float startX = -3.2f;
            float spacingX = 3.2f;
            return new Vector3(startX + idx * spacingX, 3.35f, 4.3f);
        }

        public int GetChamberIndexFromCollider(Collider col)
        {
            // Kullanıcı üst bardaki butonlarla ortamı seçtiğinde tetiklenir
            return -1;
        }

        private void Update()
        {
            animTimer += Time.deltaTime;

            // Telemetri ekolayzır barlarının hafif sinüs dalgalanması
            for (int b = 0; b < equalizerBars.Count; b++)
            {
                var bar = equalizerBars[b];
                if (bar != null)
                {
                    float wave = 0.2f + Mathf.Abs(Mathf.Sin(animTimer * 4f + b * 0.7f)) * 0.8f;
                    bar.localScale = new Vector3(0.06f, wave, 0.01f);
                }
            }
        }
    }
}
