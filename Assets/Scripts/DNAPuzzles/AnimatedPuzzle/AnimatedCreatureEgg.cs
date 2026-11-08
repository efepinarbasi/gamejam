using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Gelişmiş Bilim Kurgu Biyo-Kuluçka & Canlı Holo-Organizma Ünitesi (Sci-Fi Bio-Incubator Pod).
    /// - Yüksek teknolojili silindirik cam hazne (Stasis Glass Cylinder)
    /// - Amniyotik biyo-sıvı ve organik yukarı süzülen kabarcıklar (Fluid & Rising Bubbles)
    /// - Yukarı-aşağı tarama yapan holografik biyo-lazer halkası (Holo-Scan Laser)
    /// - Ağır siber taban ve tavan kapağı, 3 adet dikey pylon destek sütunu ve durum LED'leri
    /// - İçeride sarmala takılan genlere göre GÖZÜMÜZÜN ÖNÜNDE EVRİMLEŞEN çok katmanlı biyo-organizma:
    ///   * Parlayan iç çekirdek (Nucleus) & nefes alan yarı saydam dış zar (Cell Membrane)
    ///   * Zırh plakaları, flutter yapan solungaç kanatları, omurga segmentleri, boynuzlar, kozmik dönen DNA halkaları
    /// - Taban altında yüzen şık siber hologram bilgi ve telemetri paneli (Holo-HUD)
    /// </summary>
    public class AnimatedCreatureEgg : MonoBehaviour
    {
        // 1. Kuluçka Kapsül Donanımı (Incubator Chamber Hardware)
        private GameObject podRoot;
        private Transform glassCylinder;
        private Material glassMat;
        private Transform fluidVolume;
        private Material fluidMat;
        private Color targetFluidColor = new Color(0.15f, 0.65f, 0.95f, 0.12f);

        // Sıvı İçi Canlı Kabarcıklar (Amniotic Bio-Bubbles)
        private class BubbleData
        {
            public Transform transform;
            public float speed;
            public float wobbleFreq;
            public float wobbleAmp;
            public float seed;
            public float baseRadius;
            public float currentAngle;
        }
        private List<BubbleData> bioBubbles = new List<BubbleData>();

        // Holografik Biyo-Tarama Halkası (Scanning Laser Ring)
        private Transform scanRingTrans;
        private Material scanRingMat;
        private Transform scanLaserDisc;
        private Material scanLaserMat;
        private float scanTimer = 0f;
        private float scanPulseBoost = 0f;

        // Pylon LED & Taban Işıkları
        private List<MeshRenderer> statusLEDs = new List<MeshRenderer>();
        private Material ledMat;
        private Light bioLight;

        // 2. Çok Katmanlı Biyo-Organizma (Multi-Layer Morphing Specimen)
        private Transform organismRoot;
        private Transform coreSphere;
        private Material coreMaterial;
        private Transform outerMembrane;
        private Material membraneMaterial;

        // Morfolojik Organ Adaptasyonları
        private List<Transform> armorPlates = new List<Transform>();
        private Material armorPlateMat;
        private float targetArmorScale = 0f;
        private float currentArmorScale = 0f;

        private List<Transform> gillFins = new List<Transform>();
        private Material gillFinMat;
        private float targetGillScale = 0f;
        private float currentGillScale = 0f;

        private List<Transform> spineNodes = new List<Transform>();
        private Material spineMat;
        private float targetSpineScale = 0f;
        private float currentSpineScale = 0f;

        private List<Transform> hornSpikes = new List<Transform>();
        private Material hornMat;
        private float targetHornScale = 0f;
        private float currentHornScale = 0f;

        private GameObject cosmicHelixObj;
        private List<Transform> helixNodes = new List<Transform>();
        private Material helixMat;
        private float targetHelixScale = 0f;
        private float currentHelixScale = 0f;

        // 3. Alt Siber Holo-HUD Bilgi Paneli (Native Vektör Çözünürlüklü OnGUI)
        private string displayTitle = "◆ PROTO-SPESİMEN #01 ◆";
        private string displayTelemetry = "STASİS: AKTİF  |  AMNİYOTİK BASINÇ: 1.0 ATM";
        private bool isResonant = false;
        private bool isReady = false;
        private GUIStyle titleStyle;
        private GUIStyle subStyle;
        private GUIStyle boxStyle;
        private Camera cachedCam;

        // Animasyon ve Durum Değişkenleri
        private float breatheTimer = 0f;
        private float morphPulse = 0f;
        private bool isAberrant = false;
        private float targetCreatureScale = 1.0f;
        private Color targetOrganismColor = new Color(0.2f, 0.85f, 1f);
        private Color targetGlowColor = new Color(0.3f, 0.95f, 1f);

        private void Awake()
        {
            BuildStunningBioIncubator();
        }

        private void BuildStunningBioIncubator()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            podRoot = new GameObject("IncubatorPodStructure");
            podRoot.transform.SetParent(transform, false);

            var darkMetalMat = new Material(litShader);
            darkMetalMat.color = new Color(0.06f, 0.09f, 0.14f);
            if (darkMetalMat.HasProperty("_Smoothness")) darkMetalMat.SetFloat("_Smoothness", 0.90f);
            if (darkMetalMat.HasProperty("_Metallic")) darkMetalMat.SetFloat("_Metallic", 0.55f);

            var neonTrimMat = new Material(unlitShader);
            neonTrimMat.color = new Color(0.2f, 0.85f, 1f, 0.90f);

            ledMat = new Material(unlitShader);
            ledMat.color = new Color(0.2f, 1f, 0.5f);

            // ==========================================
            // 1. AĞIR SİBER TABAN ÜNİTESİ (Pedestal Base)
            // ==========================================
            // Ana Taban Podyumu
            GameObject mainBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mainBase.name = "PodBase_Tier1";
            mainBase.transform.SetParent(podRoot.transform, false);
            mainBase.transform.localPosition = new Vector3(0f, -1.52f, 0f);
            mainBase.transform.localScale = new Vector3(2.70f, 0.16f, 2.70f);
            Destroy(mainBase.GetComponent<Collider>());
            mainBase.GetComponent<MeshRenderer>().material = darkMetalMat;

            // Taban İkincil Bevel Katmanı
            GameObject subBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            subBase.name = "PodBase_Tier2";
            subBase.transform.SetParent(podRoot.transform, false);
            subBase.transform.localPosition = new Vector3(0f, -1.38f, 0f);
            subBase.transform.localScale = new Vector3(2.46f, 0.12f, 2.46f);
            Destroy(subBase.GetComponent<Collider>());
            subBase.GetComponent<MeshRenderer>().material = darkMetalMat;

            // Taban Neon Halka
            GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "PodBase_NeonRing";
            baseRing.transform.SetParent(podRoot.transform, false);
            baseRing.transform.localPosition = new Vector3(0f, -1.31f, 0f);
            baseRing.transform.localScale = new Vector3(2.50f, 0.025f, 2.50f);
            Destroy(baseRing.GetComponent<Collider>());
            baseRing.GetComponent<MeshRenderer>().material = neonTrimMat;

            // Taban İçi Sıvı Emiş / Işık Yayıcı Tabla
            GameObject baseDish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDish.name = "PodBase_EmitterDish";
            baseDish.transform.SetParent(podRoot.transform, false);
            baseDish.transform.localPosition = new Vector3(0f, -1.27f, 0f);
            baseDish.transform.localScale = new Vector3(2.05f, 0.05f, 2.05f);
            Destroy(baseDish.GetComponent<Collider>());
            baseDish.GetComponent<MeshRenderer>().material = darkMetalMat;

            // 3 Adet Ön Durum LED'i
            for (int l = 0; l < 3; l++)
            {
                float angle = (-35f + l * 35f) * Mathf.Deg2Rad;
                float lx = Mathf.Sin(angle) * 1.28f;
                float lz = -Mathf.Cos(angle) * 1.28f;

                GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                led.name = $"StatusLED_{l}";
                led.transform.SetParent(podRoot.transform, false);
                led.transform.localPosition = new Vector3(lx, -1.37f, lz);
                led.transform.localScale = Vector3.one * 0.09f;
                Destroy(led.GetComponent<Collider>());
                var mr = led.GetComponent<MeshRenderer>();
                mr.material = ledMat;
                statusLEDs.Add(mr);
            }

            // ==========================================
            // 2. TAVAN MANYETİK EMİTÖR KAPAĞI (Top Ceiling Cap)
            // ==========================================
            GameObject topCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topCap.name = "PodTop_Tier1";
            topCap.transform.SetParent(podRoot.transform, false);
            topCap.transform.localPosition = new Vector3(0f, 1.50f, 0f);
            topCap.transform.localScale = new Vector3(2.70f, 0.16f, 2.70f);
            Destroy(topCap.GetComponent<Collider>());
            topCap.GetComponent<MeshRenderer>().material = darkMetalMat;

            GameObject subTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            subTop.name = "PodTop_Tier2";
            subTop.transform.SetParent(podRoot.transform, false);
            subTop.transform.localPosition = new Vector3(0f, 1.38f, 0f);
            subTop.transform.localScale = new Vector3(2.46f, 0.10f, 2.46f);
            Destroy(subTop.GetComponent<Collider>());
            subTop.GetComponent<MeshRenderer>().material = darkMetalMat;

            GameObject topRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topRing.name = "PodTop_NeonRing";
            topRing.transform.SetParent(podRoot.transform, false);
            topRing.transform.localPosition = new Vector3(0f, 1.32f, 0f);
            topRing.transform.localScale = new Vector3(2.50f, 0.025f, 2.50f);
            Destroy(topRing.GetComponent<Collider>());
            topRing.GetComponent<MeshRenderer>().material = neonTrimMat;

            // Aşağıya Odaklanan Manyetik Stasis Lens Kubbesi
            GameObject lensCone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lensCone.name = "PodTop_StasisLens";
            lensCone.transform.SetParent(podRoot.transform, false);
            lensCone.transform.localPosition = new Vector3(0f, 1.28f, 0f);
            lensCone.transform.localScale = new Vector3(0.9f, 0.28f, 0.9f);
            Destroy(lensCone.GetComponent<Collider>());
            lensCone.GetComponent<MeshRenderer>().material = neonTrimMat;

            // ==========================================
            // 3. 2 ADET ARKA SİBER PYLON SÜTUNU (Görüşü asla kapatmaz, sadece arkada)
            // ==========================================
            Vector3[] rearPylonPositions = new Vector3[]
            {
                new Vector3(-1.05f, 0f, 0.65f), // Arka Sol
                new Vector3(1.05f, 0f, 0.65f)   // Arka Sağ
            };

            for (int p = 0; p < rearPylonPositions.Length; p++)
            {
                GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pylon.name = $"PodRearPylon_{p}";
                pylon.transform.SetParent(podRoot.transform, false);
                pylon.transform.localPosition = rearPylonPositions[p];
                pylon.transform.localScale = new Vector3(0.11f, 1.36f, 0.11f);
                Destroy(pylon.GetComponent<Collider>());
                pylon.GetComponent<MeshRenderer>().material = darkMetalMat;

                // İç Enerji Çizgisi
                GameObject neonStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                neonStrip.name = $"PylonGlow_{p}";
                neonStrip.transform.SetParent(pylon.transform, false);
                neonStrip.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                neonStrip.transform.localScale = new Vector3(0.35f, 0.95f, 0.35f);
                Destroy(neonStrip.GetComponent<Collider>());
                neonStrip.GetComponent<MeshRenderer>().material = neonTrimMat;
            }

            // ==========================================
            // 4. KRİSTAL ŞEFFAF SİLİNDİRİK BİYO-CAM HAZNE (Stasis Glass)
            // ==========================================
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glass.name = "StasisGlassCylinder";
            glass.transform.SetParent(podRoot.transform, false);
            glass.transform.localPosition = Vector3.zero;
            glass.transform.localScale = new Vector3(2.32f, 1.34f, 2.32f);
            Destroy(glass.GetComponent<Collider>());

            glassMat = new Material(unlitShader);
            MakeTransparent(glassMat);
            glassMat.color = new Color(0.18f, 0.65f, 0.95f, 0.12f);
            glassMat.renderQueue = 3150; // En son çizilir, içeriyi asla engellemez
            glass.GetComponent<MeshRenderer>().material = glassMat;
            glassCylinder = glass.transform;

            // ==========================================
            // 5. AMNİYOTİK BİYO-SIVI VE YÜZEN KABARCIKLAR (Fluid & Bubbles)
            // ==========================================
            GameObject fluid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fluid.name = "AmnioticFluidVolume";
            fluid.transform.SetParent(podRoot.transform, false);
            fluid.transform.localPosition = Vector3.zero;
            fluid.transform.localScale = new Vector3(2.24f, 1.32f, 2.24f);
            Destroy(fluid.GetComponent<Collider>());

            fluidMat = new Material(unlitShader);
            MakeTransparent(fluidMat);
            fluidMat.color = new Color(targetFluidColor.r, targetFluidColor.g, targetFluidColor.b, 0.08f);
            fluidMat.renderQueue = 3000;
            fluid.GetComponent<MeshRenderer>().material = fluidMat;
            fluidVolume = fluid.transform;

            // 8 Adet Yukarı Süzülen Canlı Biyo-Kabarcık
            Material bubbleMat = new Material(unlitShader);
            MakeTransparent(bubbleMat);
            bubbleMat.color = new Color(0.70f, 0.95f, 1f, 0.60f);
            bubbleMat.renderQueue = 3050;

            bioBubbles.Clear();
            for (int b = 0; b < 8; b++)
            {
                GameObject bub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bub.name = $"BioBubble_{b}";
                bub.transform.SetParent(podRoot.transform, false);
                Destroy(bub.GetComponent<Collider>());
                bub.GetComponent<MeshRenderer>().material = bubbleMat;

                float startY = Random.Range(-1.20f, 1.20f);
                float rad = Random.Range(0.20f, 0.85f);
                float ang = Random.Range(0f, Mathf.PI * 2f);
                bub.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, startY, Mathf.Sin(ang) * rad);

                float scale = Random.Range(0.045f, 0.085f);
                bub.transform.localScale = Vector3.one * scale;

                var data = new BubbleData
                {
                    transform = bub.transform,
                    speed = Random.Range(0.35f, 0.75f),
                    wobbleFreq = Random.Range(2.5f, 4.5f),
                    wobbleAmp = Random.Range(0.015f, 0.035f),
                    seed = Random.Range(0f, 100f),
                    baseRadius = rad,
                    currentAngle = ang
                };
                bioBubbles.Add(data);
            }

            // ==========================================
            // 6. HOLOGRAFİK BİYO-TARAMA LAZER HALKASI (Scan Laser)
            // ==========================================
            GameObject scanRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scanRing.name = "BioScanLaserRing";
            scanRing.transform.SetParent(podRoot.transform, false);
            scanRing.transform.localPosition = new Vector3(0f, 0f, 0f);
            scanRing.transform.localScale = new Vector3(2.28f, 0.03f, 2.28f);
            Destroy(scanRing.GetComponent<Collider>());

            scanRingMat = new Material(unlitShader);
            scanRingMat.color = new Color(0.2f, 0.95f, 1f, 0.85f);
            scanRingMat.renderQueue = 3080;
            scanRing.GetComponent<MeshRenderer>().material = scanRingMat;
            scanRingTrans = scanRing.transform;

            // Yatay Tarama Lazer Diski (İç Işık Hüzmesi)
            GameObject scanDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scanDisc.name = "BioScanLaserPlane";
            scanDisc.transform.SetParent(scanRing.transform, false);
            scanDisc.transform.localPosition = Vector3.zero;
            scanDisc.transform.localScale = new Vector3(0.98f, 0.1f, 0.98f);
            Destroy(scanDisc.GetComponent<Collider>());

            scanLaserMat = new Material(unlitShader);
            MakeTransparent(scanLaserMat);
            scanLaserMat.color = new Color(0.2f, 0.95f, 1f, 0.18f);
            scanLaserMat.renderQueue = 3070;
            scanDisc.GetComponent<MeshRenderer>().material = scanLaserMat;
            scanLaserDisc = scanDisc.transform;

            // ==========================================
            // 7. ÇOK KATMANLI BİYO-ORGANİZMA (Creature Inside)
            // ==========================================
            organismRoot = new GameObject("SpecimenOrganismRoot").transform;
            organismRoot.SetParent(transform, false);
            organismRoot.localPosition = Vector3.zero;

            // İç Çekirdek (Nucleus)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "CreatureNucleus";
            core.transform.SetParent(organismRoot, false);
            core.transform.localScale = Vector3.one * 1.40f;
            coreSphere = core.transform;
            Destroy(core.GetComponent<Collider>());

            coreMaterial = new Material(litShader);
            coreMaterial.color = targetOrganismColor;
            if (coreMaterial.HasProperty("_Smoothness")) coreMaterial.SetFloat("_Smoothness", 0.92f);
            if (coreMaterial.HasProperty("_Metallic")) coreMaterial.SetFloat("_Metallic", 0.40f);
            core.GetComponent<MeshRenderer>().material = coreMaterial;

            // Dış Biyo-Zar (Translucent Outer Membrane)
            GameObject membrane = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            membrane.name = "CellularMembrane";
            membrane.transform.SetParent(organismRoot, false);
            membrane.transform.localScale = Vector3.one * 1.62f;
            outerMembrane = membrane.transform;
            Destroy(membrane.GetComponent<Collider>());

            membraneMaterial = new Material(unlitShader);
            MakeTransparent(membraneMaterial);
            membraneMaterial.color = new Color(targetGlowColor.r, targetGlowColor.g, targetGlowColor.b, 0.35f);
            membrane.GetComponent<MeshRenderer>().material = membraneMaterial;

            // Zırh Plakaları (6 Pul Segmenti)
            armorPlateMat = new Material(litShader);
            armorPlateMat.color = new Color(1f, 0.5f, 0.15f);
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;
                float px = Mathf.Cos(angle) * 0.95f;
                float py = Mathf.Sin(angle) * 0.95f;

                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = $"ArmorPlate_{i}";
                plate.transform.SetParent(core.transform, false);
                plate.transform.localPosition = new Vector3(px, py, Random.Range(-0.2f, 0.2f));
                plate.transform.localRotation = Quaternion.Euler(0f, 0f, (i / 6f) * 360f + 45f);
                plate.transform.localScale = Vector3.zero;
                Destroy(plate.GetComponent<Collider>());
                plate.GetComponent<MeshRenderer>().material = armorPlateMat;
                armorPlates.Add(plate.transform);
            }

            // Solungaç Kanatçıkları (4 Hydrodynamic Flutter Fin)
            gillFinMat = new Material(unlitShader);
            MakeTransparent(gillFinMat);
            gillFinMat.color = new Color(0.2f, 1f, 0.6f, 0.85f);
            for (int g = 0; g < 4; g++)
            {
                bool isLeft = (g % 2 == 0);
                float yOffset = (g < 2) ? 0.35f : -0.35f;

                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fin.name = $"GillFin_{g}";
                fin.transform.SetParent(core.transform, false);
                fin.transform.localPosition = new Vector3(isLeft ? -1.05f : 1.05f, yOffset, 0f);
                fin.transform.localScale = Vector3.zero;
                Destroy(fin.GetComponent<Collider>());
                fin.GetComponent<MeshRenderer>().material = gillFinMat;
                gillFins.Add(fin.transform);
            }

            // Omurga Segmentleri (5 Vertebrae Nodes)
            spineMat = new Material(litShader);
            spineMat.color = new Color(0.3f, 0.95f, 1f);
            for (int s = 0; s < 5; s++)
            {
                GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spine.name = $"SpineNode_{s}";
                spine.transform.SetParent(core.transform, false);
                float y = Mathf.Lerp(0.85f, -0.85f, s / 4f);
                spine.transform.localPosition = new Vector3(0f, y, 0.95f);
                spine.transform.localScale = Vector3.zero;
                Destroy(spine.GetComponent<Collider>());
                spine.GetComponent<MeshRenderer>().material = spineMat;
                spineNodes.Add(spine.transform);
            }

            // Boynuzlar (2 Curved Horns)
            hornMat = new Material(litShader);
            hornMat.color = new Color(0.95f, 0.45f, 0.12f);
            for (int h = 0; h < 2; h++)
            {
                GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                horn.name = $"Horn_{h}";
                horn.transform.SetParent(core.transform, false);
                float hx = (h == 0) ? -0.42f : 0.42f;
                horn.transform.localPosition = new Vector3(hx, 0.82f, -0.15f);
                horn.transform.localRotation = Quaternion.Euler(-25f, (h == 0 ? -20f : 20f), (h == 0 ? -18f : 18f));
                horn.transform.localScale = Vector3.zero;
                Destroy(horn.GetComponent<Collider>());
                horn.GetComponent<MeshRenderer>().material = hornMat;
                hornSpikes.Add(horn.transform);
            }

            // Kozmik Çift Sarmallı Biyo-Halkalar (Orbiting Nodes)
            cosmicHelixObj = new GameObject("CosmicDoubleHelix");
            cosmicHelixObj.transform.SetParent(transform, false);

            helixMat = new Material(unlitShader);
            helixMat.color = new Color(1f, 0.88f, 0.25f, 0.95f);

            int helixPoints = 16;
            for (int h = 0; h < helixPoints; h++)
            {
                float a = (h / (float)helixPoints) * Mathf.PI * 2f;
                float r = 1.65f;
                float hx = Mathf.Cos(a) * r;
                float hy = Mathf.Sin(a) * r;
                float hz = Mathf.Sin(a * 2f) * 0.4f;

                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = $"HelixDot_{h}";
                dot.transform.SetParent(cosmicHelixObj.transform, false);
                dot.transform.localPosition = new Vector3(hx, hy, hz);
                dot.transform.localScale = Vector3.zero;
                Destroy(dot.GetComponent<Collider>());
                dot.GetComponent<MeshRenderer>().material = helixMat;
                helixNodes.Add(dot.transform);
            }

            // Biyo-Işık Kaynağı (Internal Ambient Pod Light)
            GameObject lightObj = new GameObject("BioGlowLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            bioLight = lightObj.AddComponent<Light>();
            bioLight.type = LightType.Point;
            bioLight.range = 8.5f;
            bioLight.intensity = 2.4f;
            bioLight.color = targetOrganismColor;
        }

        private static void MakeTransparent(Material mat)
        {
            if (mat == null) return;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private void Update()
        {
            breatheTimer += Time.deltaTime * 2.4f;

            // 1. Organik Sıvı Kabarcıklarının Yukarı Süzülüşü (Buoyant Bubble Drift)
            for (int b = 0; b < bioBubbles.Count; b++)
            {
                var bub = bioBubbles[b];
                if (bub == null || bub.transform == null) continue;

                Vector3 lp = bub.transform.localPosition;
                lp.y += bub.speed * Time.deltaTime;

                // Hafif dalgalanma (sinusoidal wobble)
                float wob = Mathf.Sin(Time.time * bub.wobbleFreq + bub.seed) * bub.wobbleAmp;
                float currentRad = bub.baseRadius + wob;
                lp.x = Mathf.Cos(bub.currentAngle) * currentRad;
                lp.z = Mathf.Sin(bub.currentAngle) * currentRad;

                // Tavana ulaşınca tabandan tekrar doğ
                if (lp.y > 1.25f)
                {
                    lp.y = -1.25f;
                    bub.baseRadius = Random.Range(0.20f, 0.85f);
                    bub.currentAngle = Random.Range(0f, Mathf.PI * 2f);
                }
                bub.transform.localPosition = lp;
            }

            // 2. Holografik Biyo-Lazer Tarama Halkası (Vertical Ping-Pong Sweep)
            scanTimer += Time.deltaTime * (0.85f + scanPulseBoost * 1.5f);
            if (scanPulseBoost > 0f)
            {
                scanPulseBoost = Mathf.Max(0f, scanPulseBoost - Time.deltaTime * 2f);
            }

            if (scanRingTrans != null)
            {
                float sweepY = Mathf.PingPong(scanTimer, 2.30f) - 1.15f;
                scanRingTrans.localPosition = new Vector3(0f, sweepY, 0f);
                scanRingTrans.Rotate(Vector3.up * (25f + scanPulseBoost * 140f) * Time.deltaTime, Space.Self);
            }

            // 3. Çekirdek & Biyo-Zar Nabız Animasyonu (Canlı Nefes Alır + Tepki Verir)
            if (morphPulse > 0f)
            {
                morphPulse = Mathf.Max(0f, morphPulse - Time.deltaTime * 3.5f);
            }
            float pulseBounce = Mathf.Sin(morphPulse * Mathf.PI) * 0.22f;
            float breathe = 1f + Mathf.Sin(breatheTimer) * 0.05f + pulseBounce;
            float membraneBreathe = 1f + Mathf.Sin(breatheTimer + 1.2f) * 0.08f + pulseBounce * 1.3f;

            if (coreSphere != null)
            {
                float baseS = 1.40f * Mathf.Clamp(targetCreatureScale, 0.8f, 1.8f);
                coreSphere.localScale = Vector3.one * (baseS * breathe);
                coreSphere.Rotate(Vector3.up * (8f + morphPulse * 120f) * Time.deltaTime, Space.Self);

                if (isAberrant)
                {
                    coreSphere.localPosition = Random.insideUnitSphere * 0.045f;
                }
                else
                {
                    coreSphere.localPosition = Vector3.zero;
                }
            }

            if (outerMembrane != null)
            {
                float baseM = 1.62f * Mathf.Clamp(targetCreatureScale, 0.8f, 1.8f);
                outerMembrane.localScale = Vector3.one * (baseM * membraneBreathe);
                outerMembrane.Rotate(Vector3.up * (-5f - morphPulse * 60f) * Time.deltaTime, Space.Self);
            }

            // 4. Zırh Plakaları Yumuşak Büyüme/Küçülme
            currentArmorScale = Mathf.Lerp(currentArmorScale, targetArmorScale, Time.deltaTime * 6f);
            for (int i = 0; i < armorPlates.Count; i++)
            {
                if (armorPlates[i] != null)
                {
                    armorPlates[i].localScale = new Vector3(0.48f, 0.48f, 0.15f) * currentArmorScale;
                }
            }

            // 5. Solungaç Kanatları Flutter Animasyonu
            currentGillScale = Mathf.Lerp(currentGillScale, targetGillScale, Time.deltaTime * 6f);
            for (int g = 0; g < gillFins.Count; g++)
            {
                if (gillFins[g] != null)
                {
                    bool isLeft = (g % 2 == 0);
                    float flutter = Mathf.Sin(breatheTimer * 3.5f + g) * 0.18f;
                    gillFins[g].localScale = new Vector3(0.55f + flutter, 0.32f, 0.12f) * currentGillScale;
                    gillFins[g].localRotation = Quaternion.Euler(0f, isLeft ? -25f + flutter * 20f : 25f - flutter * 20f, 0f);
                }
            }

            // 6. Omurga Segmentleri
            currentSpineScale = Mathf.Lerp(currentSpineScale, targetSpineScale, Time.deltaTime * 6f);
            for (int s = 0; s < spineNodes.Count; s++)
            {
                if (spineNodes[s] != null)
                {
                    float spineMod = 1f + Mathf.Sin(breatheTimer * 2f + s * 0.5f) * 0.10f;
                    spineNodes[s].localScale = Vector3.one * (0.24f * spineMod * currentSpineScale);
                }
            }

            // 7. Boynuzlar
            currentHornScale = Mathf.Lerp(currentHornScale, targetHornScale, Time.deltaTime * 6f);
            for (int h = 0; h < hornSpikes.Count; h++)
            {
                if (hornSpikes[h] != null)
                {
                    hornSpikes[h].localScale = new Vector3(0.20f, 0.48f, 0.20f) * currentHornScale;
                }
            }

            // 8. Kozmik Heliks Dönüşü
            currentHelixScale = Mathf.Lerp(currentHelixScale, targetHelixScale, Time.deltaTime * 6f);
            if (cosmicHelixObj != null)
            {
                cosmicHelixObj.transform.Rotate(Vector3.forward * 40f * Time.deltaTime, Space.Self);
                for (int h = 0; h < helixNodes.Count; h++)
                {
                    if (helixNodes[h] != null)
                    {
                        helixNodes[h].localScale = Vector3.one * (0.12f * currentHelixScale);
                    }
                }
            }

            // 9. Renk, Sıvı ve Işık Geçişleri
            if (coreMaterial != null)
            {
                coreMaterial.color = Color.Lerp(coreMaterial.color, targetOrganismColor, Time.deltaTime * 5f);
            }
            if (membraneMaterial != null)
            {
                Color memCol = new Color(targetGlowColor.r, targetGlowColor.g, targetGlowColor.b, 0.35f);
                membraneMaterial.color = Color.Lerp(membraneMaterial.color, memCol, Time.deltaTime * 5f);
            }
            if (fluidMat != null)
            {
                fluidMat.color = Color.Lerp(fluidMat.color, targetFluidColor, Time.deltaTime * 4f);
            }
            if (scanRingMat != null)
            {
                Color scCol = isAberrant ? Color.red : targetGlowColor;
                scanRingMat.color = Color.Lerp(scanRingMat.color, scCol, Time.deltaTime * 5f);
            }
            if (scanLaserMat != null)
            {
                Color slCol = new Color(targetGlowColor.r, targetGlowColor.g, targetGlowColor.b, 0.22f);
                if (isAberrant) slCol = new Color(1f, 0.15f, 0.15f, 0.35f);
                scanLaserMat.color = Color.Lerp(scanLaserMat.color, slCol, Time.deltaTime * 5f);
            }
            if (bioLight != null)
            {
                bioLight.color = Color.Lerp(bioLight.color, targetOrganismColor, Time.deltaTime * 5f);
            }
        }

        private void OnGUI()
        {
            if (!gameObject.activeInHierarchy) return;

            if (cachedCam == null)
            {
                cachedCam = Camera.main;
                if (cachedCam == null) cachedCam = FindAnyObjectByType<Camera>();
            }
            if (cachedCam == null) return;

            // Kuluçkanın hemen altındaki 3D taban noktasını ekran koordinatına çevir
            Vector3 worldP = transform.position + new Vector3(0f, -1.95f, 0f);
            Vector3 screenP = cachedCam.WorldToScreenPoint(worldP);
            if (screenP.z <= 0f) return;

            float guiX = screenP.x;
            float guiY = Screen.height - screenP.y;

            float cardW = 320f;
            float cardH = 58f;
            Rect cardRect = new Rect(guiX - cardW * 0.5f, guiY - cardH * 0.5f, cardW, cardH);

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.fontSize = 13;
                titleStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (subStyle == null)
            {
                subStyle = new GUIStyle(GUI.skin.label);
                subStyle.fontStyle = FontStyle.Normal;
                subStyle.fontSize = 11;
                subStyle.alignment = TextAnchor.MiddleCenter;
            }

            Color oldBg = GUI.backgroundColor;
            Color oldCol = GUI.color;

            // 1. Şık Yarı Saydam Koyu Siber Cam Panel
            GUI.backgroundColor = isAberrant ? new Color(0.38f, 0.08f, 0.12f, 0.94f) : new Color(0.015f, 0.045f, 0.09f, 0.94f);
            GUI.Box(cardRect, GUIContent.none);

            // 2. Başlık Metni (Native Vektör, Subpixel Anti-Aliased, Asla Pikselleşmez)
            Color tCol = isAberrant ? new Color(1f, 0.35f, 0.35f) : (isReady ? new Color(1f, 0.90f, 0.35f) : new Color(0.35f, 0.95f, 1f));
            GUI.color = tCol;
            GUI.Label(new Rect(cardRect.x, cardRect.y + 6f, cardW, 22f), displayTitle, titleStyle);

            // 3. Telemetri Alt Metni (Ferah, Net, Okunaklı)
            Color sCol = isAberrant ? new Color(1f, 0.65f, 0.65f) : (isResonant ? new Color(0.35f, 1f, 0.60f) : new Color(0.85f, 0.95f, 1f));
            GUI.color = sCol;
            GUI.Label(new Rect(cardRect.x, cardRect.y + 28f, cardW, 22f), displayTelemetry, subStyle);

            // 4. Küçük Canlı Vital Işığı (Sol Kenarda)
            float vitalPulse = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f;
            Color dotCol = isAberrant ? Color.red : (isResonant ? Color.green : new Color(0.2f, 1f, 0.5f));
            GUI.color = Color.Lerp(dotCol * 0.4f, dotCol * 1.3f, vitalPulse);
            GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 6f, 18f, 20f), "●", titleStyle);

            GUI.backgroundColor = oldBg;
            GUI.color = oldCol;
        }

        public void UpdateFromStrand(GeneData[] strand, Dictionary<StatType, int> stats, int resonanceCount, int instability, PlanetData planet = null, List<GeneSynergy> synergies = null)
        {
            if (strand == null) return;

            morphPulse = 1.0f;
            scanPulseBoost = 1.0f;

            var synthData = CreatureSynthesisBridge.PreviewFromPuzzle(strand, stats, synergies, planet);
            var p = synthData.phenotype;

            targetCreatureScale = p.scaleMultiplier;
            targetHornScale = p.hasHorns ? (0.6f + p.hornScale * 0.4f) : 0f;
            targetArmorScale = p.hasArmorPlates ? (0.6f + p.armorIntensity * 0.5f) : 0f;
            targetGillScale = p.hasGillsOrFins ? p.gillScale : 0f;
            targetSpineScale = p.hasSpinalRidge ? 1.0f : 0f;
            targetHelixScale = p.hasCosmicRings ? 1.0f : 0f;

            targetOrganismColor = p.primarySkinColor;
            targetGlowColor = p.glowColor;
            isAberrant = (p.bodyType == CreatureBodyType.AberrantMutant);

            // Kuluçka Sıvı Rengini Gezegene ve Canlıya Göre Uyarla
            if (isAberrant)
            {
                targetFluidColor = new Color(0.85f, 0.15f, 0.15f, 0.22f);
            }
            else if (planet != null && planet.planetName.Contains("Pyros"))
            {
                targetFluidColor = new Color(1f, 0.50f, 0.12f, 0.14f);
            }
            else if (planet != null && planet.planetName.Contains("Toxis"))
            {
                targetFluidColor = new Color(0.20f, 0.95f, 0.35f, 0.15f);
            }
            else
            {
                targetFluidColor = new Color(0.12f, 0.65f, 0.95f, 0.14f);
            }

            if (armorPlateMat != null) armorPlateMat.color = p.secondaryColor;
            if (gillFinMat != null) gillFinMat.color = p.glowColor;
            if (spineMat != null) spineMat.color = p.secondaryColor;
            if (hornMat != null) hornMat.color = p.secondaryColor;

            int genCount = synthData.equippedGenes.Count;
            isReady = (genCount > 0);
            isResonant = (resonanceCount >= 2 && instability == 0);

            if (genCount == 0)
            {
                displayTitle = "◆ PROTO-SPESİMEN #01 ◆";
                displayTelemetry = "STASİS KORUMA: AKTİF  |  AMNİYOTİK BASINÇ: 1.0 ATM";
            }
            else if (isAberrant)
            {
                displayTitle = $"☣️ {synthData.speciesName.ToUpper()}";
                displayTelemetry = "⚠️ KRİTİK MUTASYON! HİLKAT GARİBESİ";
            }
            else
            {
                displayTitle = $"★ {synthData.speciesName.ToUpper()}";
                string resText = (resonanceCount >= 2 && instability == 0) ? "REZONANS KUSURSUZ ✨" : (instability > 1 ? "KARARSIZLIK MEVCUT ⚠️" : "STABİL %100");
                displayTelemetry = $"[{p.heightMeters:F1}m • {p.weightKg:F0}kg]  |  {resText}";
            }

            // Durum LED'lerini güncelle
            Color ledColor = isAberrant ? Color.red : (genCount == 0 ? new Color(0.2f, 0.85f, 1f) : new Color(0.2f, 1f, 0.5f));
            for (int i = 0; i < statusLEDs.Count; i++)
            {
                if (statusLEDs[i] != null && statusLEDs[i].material != null)
                {
                    statusLEDs[i].material.color = ledColor;
                }
            }
        }

        public void OnPhaseSimulate(SimulationPhase phase, bool ok)
        {
            if (!ok)
            {
                targetOrganismColor = Color.red;
                targetFluidColor = new Color(0.85f, 0.15f, 0.15f, 0.30f);
                displayTitle = "⚠️ SPESİMEN PROTOKOLÜ";
                displayTelemetry = "❌ SİMÜLASYON ADAPTASYONU ÇÖKTÜ!";
                return;
            }

            switch (phase)
            {
                case SimulationPhase.AtmosphericEntry:
                    targetOrganismColor = new Color(1f, 0.45f, 0.1f);
                    targetFluidColor = new Color(1f, 0.45f, 0.1f, 0.20f);
                    break;
                case SimulationPhase.ChemicalAtmosphere:
                    targetOrganismColor = new Color(0.2f, 1f, 0.5f);
                    targetFluidColor = new Color(0.2f, 1f, 0.5f, 0.20f);
                    break;
                case SimulationPhase.GravityPressure:
                    targetOrganismColor = new Color(0.3f, 0.7f, 1f);
                    targetFluidColor = new Color(0.3f, 0.7f, 1f, 0.20f);
                    break;
                case SimulationPhase.GeneticIntegrity:
                    targetOrganismColor = new Color(0.85f, 0.35f, 1f);
                    targetFluidColor = new Color(0.85f, 0.35f, 1f, 0.20f);
                    break;
                case SimulationPhase.Completed:
                    targetOrganismColor = new Color(1f, 0.88f, 0.18f);
                    targetFluidColor = new Color(1f, 0.88f, 0.18f, 0.22f);
                    displayTitle = "👑 EVRİM BAŞARILI!";
                    displayTelemetry = "GEZEGENE UYUM KANITLANDI • YAŞAM AKTİF";
                    break;
            }
        }
    }
}
