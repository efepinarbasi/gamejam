using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// 3D Hareketli, İnteraktif DNA Sarmal Yapboz Kontrolcüsü.
    /// Üst gökyüzünde yan yana sergilenen 3 ayrı 3D gezegen (Pyros-9, Cryo-Kore, Toxis-Prime),
    /// taşmasız genişletilmiş genom parçaları, elektrikli nükleotid bağları ve 
    /// holo-organizma mutasyon kuluçkası içerir.
    /// </summary>
    public partial class DNAAnimatedPuzzleController : MonoBehaviour
    {
        public static DNAAnimatedPuzzleController Instance { get; private set; }

        private DNAPuzzleManager manager;
        private SurvivalSimulator simulator;
        private DNAPuzzleAudio audioManager;

        private List<AnimatedSlot> activeSlots = new List<AnimatedSlot>();
        private List<AnimatedGenePiece> poolPieces = new List<AnimatedGenePiece>();
        private List<AnimatedRepairCandidate> repairCandidates = new List<AnimatedRepairCandidate>();
        private AnimatedRepairCandidate hoveredCandidate;
        private AnimatedGenePiece selectedRepairGenome;
        private string repairFeedbackMessage = "🛠️ 1. ADIM: Bozuk bir genom seç ve alttaki doğru baz parçasıyla onar!";
        private bool isRepairZoomMode = false;
        private AnimatedGenePiece zoomedGenome = null;
        private GameObject focusBackdropQuad = null;

        // In-Game Dokunmatik Tablet Gerçek Jigsaw Puzzle Durumu (GDD Uyumlu)
        private Dictionary<LacunaSlotType, EditableGeneStrand> jigsawStrands;
        private int currentJigsawCreature = 0; // 0..9 (WIBBLE..GROMBLE)
        private static readonly LacunaSlotType[] tabletSlots = { LacunaSlotType.Z, LacunaSlotType.V, LacunaSlotType.K, LacunaSlotType.P };
        private static readonly LacunaShape[] tabletBases = { LacunaShape.Circle, LacunaShape.Square, LacunaShape.Triangle, LacunaShape.Diamond };
        private enum TabletPieceMotion { Idle, Held, Snapping, Returning }
        private TabletPieceMotion pieceMotion;
        private float helixPhase;
        private Texture2D helixRibbons;
        private const float HelixPeriod = 360f;
        private const float HelixMiddle = 253f;

        private int movingBaseIndex;
        private int dragControl;
        private Vector2 grabOffset;
        private Vector2 pieceCenter;
        private Vector2 releaseCenter;
        private float motionStarted;
        private int totalRepairs;
        private string jigsawMessage;
        private bool jigsawVictoryOpen = false;
        private string jigsawVictoryText = "";

        // Dokunmatik Tablet Çalışma Modu ve Ekran Boyutları
        private enum TabletScreenMode { Home, Puzzle, Sequencing }
        private TabletScreenMode tabletScreenMode = TabletScreenMode.Sequencing;

        // Odadan (Base) masadaki fiziksel tablet ekranının Game View (Screen.width x Screen.height) içindeki tam oranları:
        // Canlı ekran görüntüsü kalibrasyonu: TopY = 385px (0.7184 * H), BottomY = 456px (Height = 0.1830 * H),
        // BottomWidth = 124px (0.3070 * H), TopWidth = 90px (TopRatio = 0.7258), CenterX = 494.5px (0.4865 * W)
        private const float BaseCenterXRatio = 0.4865f;
        private const float BaseTopYRatio = 0.7184f;
        private const float BaseHeightRatio = 0.1830f;
        private const float BaseBottomWidthRatio = 0.3070f;
        private const float BaseTopRatio = 0.7258f;

        private RenderTexture tabletFrame;
        private Transform tabletGlass;
        private Material tabletScreenMaterial;
        // Inner glass edges measured in the docked Lab camera, in units of viewport height.
        private const float GlassTopWidth = 0.650f;
        private const float GlassBottomWidth = 0.940f;
        // Dokunmatik Tablet & Bulmaca Animasyon Değişkenleri
        private float tabletOpenAnim = 0f;
        private bool isTabletClosing = false;
        private float lastSnapFeedbackTimer = 0f;
        private Vector2 lastSnapPos = Vector2.zero;
        private float helixSpeedSurge = 0f;

        // Genom ve Ok Geçiş Animasyon Değişkenleri (Kayan Geçiş ve Otomatik İlerleme)
        private float transitionProgress = 1f; // 0..1, 1: bitti/sabit
        private int transitionDirection = 1; // 1: sola kay (ileri), -1: sağa kay (geri)
        private System.Action transitionMidpointAction = null;
        private bool hasFiredTransitionMidpoint = false;
        private float slotCelebrationTimer = 0f;
        private LacunaSlotType celebratingSlot = LacunaSlotType.Z;
        private static Texture2D _ringTex;
        private List<AnimatedResonanceBeam> resonanceBeams = new List<AnimatedResonanceBeam>();
        private DNAHelixStrandRibbon helixRibbon;
        private AnimatedCreatureEgg creatureEgg;
        private GameObject backgroundChamber;
        private AnimatedLaboratoryScreen labScreen;
        private AnimatedGenePiece hoveredGenePiece;
        private PlanetaryDropCinematic dropCinematic;
        private int lastSynergyCount = 0;
        private int lastResonanceCount = 0;
        private int lastInstabilityCount = 0;

        private int selectedSlot = 0;
        private Camera puzzleCamera;
        private EvaluationResult lastEvaluation;
        private string simulationLog = "";
        private bool isSimulating = false;
        [System.NonSerialized]
        private SynthesizedCreatureData lastSynthesizedCreature;
        private bool showSynthesisModal = false;
        private bool showRulebookModal = false;
        private Vector2 rulebookScrollPos = Vector2.zero;
        private int currentCreatureIdx = 0;
        private string teamDispatchMessage = "";

        private int currentPlanetIdx = 0;
        private List<PlanetData> planets;

        // Sürükle-Bırak Geçmeli Yapboz Mekaniği (Drag-and-Drop Interlocking System)
        private AnimatedGenePiece dragCandidatePiece = null;
        private AnimatedGenePiece draggedPiece = null;
        private Vector2 dragPieceStartMousePos;
        private Vector3 dragPieceOffset;
        private bool isDraggingPiece = false;
        private int snapTargetSlotIndex = -1;
        private bool isSnapComplementary = false;
        private string dragInterlockMessage = "";

        private AnimatedRepairCandidate dragCandidateCand = null;
        private AnimatedRepairCandidate draggedCandidate = null;
        private Vector2 dragCandStartMousePos;
        private bool isDraggingCandidate = false;
        private AnimatedGenePiece candidateDropTargetGenome = null;

        private LineRenderer snapGuideLine;

        #region Laboratuvar İstasyon & Monitör Entegrasyonu
        [Header("Laboratuvar İstasyon & Monitör Entegrasyonu")]
        public bool isLabMode { get; private set; } = false;
        public bool isTerminalActive { get; private set; } = true;
        private CameraStationController labStationController;
        private Camera dedicatedTerminalCamera;
        private GameObject puzzleRoot;
        private ScreenStation monitorStation;
        private ScreenStation tabletStation;
        private ScreenStation chamberStation;
        private GameObject labChamberCreature;
        private LacunaTerminalUI lacunaUI;
        #endregion

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        private void Start()
        {
            EnsurePuzzleInitialized();
        }

        #region Laboratuvar İstasyon & Monitör Entegrasyon Metotları
        public void InitializeForLabStation(CameraStationController stationCtrl)
        {
            isLabMode = true;
            labStationController = stationCtrl;
            isTerminalActive = false;

            // Terminal açıkken sağ tık veya ESC'nin odaya kontrolsüz dönmesini engelle
            CameraStationController.IsInputIntercepted = () => (isLabMode && isTerminalActive);

            transform.position = Vector3.zero;

            // 3D masadaki tablet modelinin üzerindeki dekoratif blender objelerini gizle
            CleanTablet3DModel();

            // Lab odasındaki ekran istasyonlarını tespit et
            foreach (var st in Object.FindObjectsByType<ScreenStation>())
            {
                if (st == null) continue;
                if (st.gameObject.name == "FirstScreen") monitorStation = st;
                else if (st.gameObject.name == "SecondScreen") tabletStation = st;
                else if (st.gameObject.name == "ThirdScreen") chamberStation = st;
            }

            if (stationCtrl != null && stationCtrl.loopStations != null)
            {
                if (monitorStation == null && stationCtrl.loopStations.Length > 0) monitorStation = stationCtrl.loopStations[0];
                if (tabletStation == null && stationCtrl.loopStations.Length > 1) tabletStation = stationCtrl.loopStations[1];
                if (chamberStation == null && stationCtrl.loopStations.Length > 2) chamberStation = stationCtrl.loopStations[2];
            }

            // DNA Puzzle artık soldaki klavyeli uzun monitörde (FirstScreen) çalışır.
            // Ortadaki konsol/tablet (SecondScreen) puzzle'dan ayrılır ve orijinal haline döner.
            if (tabletStation != null)
            {
                tabletStation.onEnter.RemoveListener(OnTerminalEnter);
                tabletStation.onExit.RemoveListener(OnTerminalExit);
            }
            if (tabletStation != null) tabletStation.displaySurface = null;
            tabletStation = monitorStation;

            // Puzzle istasyonu (sol monitör) DNA Puzzle ekranını açar
            if (tabletStation != null)
            {
                BuildTabletGlass();
                tabletStation.onEnter.RemoveListener(OnTerminalEnter);
                tabletStation.onEnter.AddListener(OnTerminalEnter);
                tabletStation.onExit.RemoveListener(OnTerminalExit);
                tabletStation.onExit.AddListener(OnTerminalExit);
            }

            EnsurePuzzleInitialized();

            // Başlangıçta tüm puzzle geometrisini ve kamerasını kapalı tut (oda tamamen orijinal ve temiz kalsın!)
            if (puzzleRoot != null) puzzleRoot.SetActive(false);
            if (dedicatedTerminalCamera != null)
            {
                dedicatedTerminalCamera.gameObject.SetActive(false);
                dedicatedTerminalCamera.enabled = false;
            }
        }

        private void CleanTablet3DModel()
        {
            var tabletObj = GameObject.Find("Gen rehberi tableti");
            if (tabletObj != null)
            {
                var s1 = tabletObj.transform.Find("Sphere.001");
                if (s1 != null && s1.gameObject.activeSelf)
                {
                    s1.gameObject.SetActive(false);
                }
            }
        }

        private Material CreateHoloMaterial(Color col, bool transparent = true)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            var mat = new Material(s);
            mat.color = col;
            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent in URP
                mat.SetFloat("_Blend", 0); // Alpha
                mat.renderQueue = 3000;
            }
            return mat;
        }

        private TextMesh CreateTextMeshOn(Transform parent, string name, string text, Vector3 localPos, float charSize, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = charSize;
            tm.fontSize = fontSize;
            tm.fontStyle = FontStyle.Bold;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            return tm;
        }

        private void UpdateLabHoloPreview()
        {
            if (!isLabMode) return;

            // Kuluçka odasındaki canlıyı yavaşça yüzdür
            if (labChamberCreature != null)
            {
                labChamberCreature.transform.Rotate(Vector3.up, 24f * Time.deltaTime, Space.World);
                var p = labChamberCreature.transform.position;
                p.y = 4.8f + Mathf.Sin(Time.time * 2f) * 0.15f;
                labChamberCreature.transform.position = p;

                // Tabela (plaque) daima kameraya baksın (aynalı/ters dönmesin)
                var plaque = labChamberCreature.transform.Find("SpecimenPlaque");
                if (plaque != null)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        plaque.rotation = cam.transform.rotation;
                    }
                }
            }
        }

        public void OnTerminalEnter()
        {
            if (!isLabMode) return;
            isTerminalActive = true;
            tabletScreenMode = TabletScreenMode.Sequencing;
            tabletOpenAnim = 0f;
            isTabletClosing = false;
            CameraStationController.IsInputIntercepted = () => true;
            if (puzzleRoot != null) puzzleRoot.SetActive(false);
            if (dedicatedTerminalCamera != null)
            {
                dedicatedTerminalCamera.gameObject.SetActive(false);
                dedicatedTerminalCamera.enabled = false;
            }
            audioManager?.PlayResonance();
            audioManager?.PlaySnap();
        }

        public void OnTerminalExit()
        {
            if (!isLabMode) return;
            if (isRepairZoomMode)
            {
                ExitRepairZoomMode();
            }
            CancelTabletDrag();
            tabletScreenMode = TabletScreenMode.Sequencing;
            isTerminalActive = false;
            tabletOpenAnim = 0f;
            isTabletClosing = false;
            CameraStationController.IsInputIntercepted = null;
            if (puzzleRoot != null) puzzleRoot.SetActive(false);
            if (dedicatedTerminalCamera != null)
            {
                dedicatedTerminalCamera.gameObject.SetActive(false);
                dedicatedTerminalCamera.enabled = false;
            }
        }

        public void StartTabletExit()
        {
            if (!isLabMode || !isTerminalActive) return;
            ReturnToLabRoom();
        }

        public void ReturnToLabRoom()
        {
            if (!isLabMode || !isTerminalActive) return;
            isTabletClosing = false;
            OnTerminalExit();
            if (labStationController != null)
            {
                labStationController.ReturnToBase();
            }
        }

        public void SpawnCreatureInLabChamber(SynthesizedCreatureData creature)
        {
            if (!isLabMode || creature == null || !creature.isValid) return;

            if (labChamberCreature != null) Destroy(labChamberCreature);

            labChamberCreature = new GameObject("LabChamberSynthesizedCreature");
            // Glass arkasında kuluçka odasının merkezi: (6.5, 4.8, -4.3)
            labChamberCreature.transform.position = new Vector3(6.5f, 4.8f, -4.3f);

            // 1. Canlı Biyo-Çekirdeği (Glowing Specimen Core)
            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreGo.name = "BioCore";
            coreGo.transform.SetParent(labChamberCreature.transform, false);
            coreGo.transform.localScale = Vector3.one * 1.5f;
            if (coreGo.GetComponent<Collider>()) Destroy(coreGo.GetComponent<Collider>());
            var ren = coreGo.GetComponent<Renderer>();

            Color coreColor = new Color(0.2f, 0.9f, 1f);
            if (creature.targetPlanet != null)
            {
                coreColor = creature.targetPlanet.themeColor;
            }
            else if (creature.phenotype != null && creature.phenotype.glowColor.a > 0.05f)
            {
                coreColor = creature.phenotype.glowColor;
            }
            ren.material = CreateHoloMaterial(coreColor, false);

            // 2. Etrafında Dönen Enerji Halkası
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringGo.name = "OrbitalShield";
            ringGo.transform.SetParent(labChamberCreature.transform, false);
            ringGo.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            ringGo.transform.localRotation = Quaternion.Euler(25f, 0f, 35f);
            if (ringGo.GetComponent<Collider>()) Destroy(ringGo.GetComponent<Collider>());
            ringGo.GetComponent<Renderer>().material = CreateHoloMaterial(new Color(coreColor.r, coreColor.g, coreColor.b, 0.6f), true);

            // 3. İç Işık (Point Light)
            var lightGo = new GameObject("SpecimenLight");
            lightGo.transform.SetParent(labChamberCreature.transform, false);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = coreColor;
            pl.range = 8f;
            pl.intensity = 3.5f;

            // 4. Numune Bilgi Tabelası (Plaque)
            string specName = !string.IsNullOrEmpty(creature.speciesName) ? creature.speciesName : "Biyo-Organizması";
            int stars = (lastEvaluation != null) ? Mathf.Max(1, lastEvaluation.starRating) : 3;
            CreateTextMeshOn(labChamberCreature.transform, "SpecimenPlaque",
                $"🔬 DENEYSEL CANLI: {specName}\n[ Derece: {stars} Yıldız • Yaşam: AKTİF ]",
                new Vector3(0f, 1.6f, 0f), 0.035f, 96, Color.white);
        }

        /// <summary>
        /// Lacuna Genetics GDD (Sayfa 4-5) Hidden Designer Tables'a göre
        /// yaratığın 4 final geninden (Z: Boy, V: Göz, K: Deri Rengi, P: Uzuvlar)
        /// 3D modelini ve kapsülünü kuluçka odasında (Glass arkasında) inşa eder.
        /// </summary>
        public void SpawnLacunaCreatureInLabChamber(string creatureName, string planetName, string geneZ, string geneV, string geneK, string geneP)
        {
            if (labChamberCreature != null) Destroy(labChamberCreature);

            labChamberCreature = new GameObject($"LacunaCreature_{creatureName}");
            labChamberCreature.transform.position = new Vector3(6.5f, 4.5f, -4.3f);

            // 1. Deri Rengi (Slot K: KRELL=Turuncu, KUPPA=Sarı, KOSH=Beyaz, KIMBO=Turkuaz, KLOOT=Mor)
            Color skinColor = new Color(1.0f, 0.45f, 0.12f);
            switch (geneK?.ToUpperInvariant())
            {
                case "KRELL": skinColor = new Color(1.0f, 0.45f, 0.12f); break; // Orange
                case "KUPPA": skinColor = new Color(1.0f, 0.88f, 0.15f); break; // Yellow
                case "KOSH":  skinColor = new Color(0.92f, 0.96f, 1.0f); break; // White
                case "KIMBO": skinColor = new Color(0.10f, 0.88f, 0.78f); break; // Teal
                case "KLOOT": skinColor = new Color(0.68f, 0.25f, 0.98f); break; // Violet
            }

            // 2. Boy Ölçeği (Slot Z: ZUUN=Tiny, ZIMMOK=Short, ZEMBO=Medium, ZORP=Tall, ZAKKO=Giant)
            float heightScale = 1.0f;
            switch (geneZ?.ToUpperInvariant())
            {
                case "ZUUN":   heightScale = 0.52f; break;
                case "ZIMMOK": heightScale = 0.75f; break;
                case "ZEMBO":  heightScale = 1.00f; break;
                case "ZORP":   heightScale = 1.30f; break;
                case "ZAKKO":  heightScale = 1.62f; break;
            }

            Material skinMat = CreateHoloMaterial(skinColor, false);
            Material eyeWhiteMat = CreateHoloMaterial(new Color(1f, 1f, 0.92f), false);
            Material pupilMat = CreateHoloMaterial(new Color(0.05f, 0.05f, 0.08f), false);
            Material capsuleGlassMat = CreateHoloMaterial(new Color(0.15f, 0.85f, 1f, 0.22f), true);
            Material capsuleRingMat = CreateHoloMaterial(new Color(0.2f, 0.9f, 0.65f, 0.85f), false);

            // Biyo-Kapsül Dış Camı ve Halkaları
            var capsuleTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capsuleTube.name = "BioCapsuleGlass";
            capsuleTube.transform.SetParent(labChamberCreature.transform, false);
            capsuleTube.transform.localScale = new Vector3(2.2f, 1.6f, 2.2f);
            if (capsuleTube.GetComponent<Collider>()) Destroy(capsuleTube.GetComponent<Collider>());
            capsuleTube.GetComponent<Renderer>().material = capsuleGlassMat;

            var topCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topCap.name = "CapsuleTopRing";
            topCap.transform.SetParent(labChamberCreature.transform, false);
            topCap.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            topCap.transform.localScale = new Vector3(2.35f, 0.08f, 2.35f);
            if (topCap.GetComponent<Collider>()) Destroy(topCap.GetComponent<Collider>());
            topCap.GetComponent<Renderer>().material = capsuleRingMat;

            var botCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            botCap.name = "CapsuleBottomRing";
            botCap.transform.SetParent(labChamberCreature.transform, false);
            botCap.transform.localPosition = new Vector3(0f, -1.62f, 0f);
            botCap.transform.localScale = new Vector3(2.35f, 0.08f, 2.35f);
            if (botCap.GetComponent<Collider>()) Destroy(botCap.GetComponent<Collider>());
            botCap.GetComponent<Renderer>().material = capsuleRingMat;

            // Yaratık Gövde Kökü
            var bodyRoot = new GameObject("CreatureBodyRoot");
            bodyRoot.transform.SetParent(labChamberCreature.transform, false);
            bodyRoot.transform.localScale = new Vector3(0.85f, heightScale * 0.85f, 0.85f);

            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(bodyRoot.transform, false);
            torso.transform.localScale = new Vector3(0.95f, 0.85f, 0.85f);
            if (torso.GetComponent<Collider>()) Destroy(torso.GetComponent<Collider>());
            torso.GetComponent<Renderer>().material = skinMat;

            // 3. Gözler (Slot V: VIXX=Eyeless, VOLM=One eye, VESHI=Two eyes, VARQ=Three eyes, VUNDO=Stalk eyes)
            string vUpper = geneV != null ? geneV.ToUpperInvariant() : "VESHI";
            if (vUpper == "VOLM")
            {
                CreateCreatureEye(bodyRoot.transform, new Vector3(0f, 0.35f, -0.45f), 0.28f, eyeWhiteMat, pupilMat);
            }
            else if (vUpper == "VESHI")
            {
                CreateCreatureEye(bodyRoot.transform, new Vector3(-0.20f, 0.35f, -0.42f), 0.20f, eyeWhiteMat, pupilMat);
                CreateCreatureEye(bodyRoot.transform, new Vector3(0.20f, 0.35f, -0.42f), 0.20f, eyeWhiteMat, pupilMat);
            }
            else if (vUpper == "VARQ")
            {
                CreateCreatureEye(bodyRoot.transform, new Vector3(-0.22f, 0.28f, -0.42f), 0.18f, eyeWhiteMat, pupilMat);
                CreateCreatureEye(bodyRoot.transform, new Vector3(0.22f, 0.28f, -0.42f), 0.18f, eyeWhiteMat, pupilMat);
                CreateCreatureEye(bodyRoot.transform, new Vector3(0f, 0.52f, -0.40f), 0.19f, eyeWhiteMat, pupilMat);
            }
            else if (vUpper == "VUNDO")
            {
                // Saplı Gözler (Stalk eyes)
                for (int s = -1; s <= 1; s += 2)
                {
                    var stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stalk.name = "EyeStalk";
                    stalk.transform.SetParent(bodyRoot.transform, false);
                    stalk.transform.localPosition = new Vector3(s * 0.24f, 0.88f, -0.15f);
                    stalk.transform.localRotation = Quaternion.Euler(-18f, 0f, -s * 16f);
                    stalk.transform.localScale = new Vector3(0.08f, 0.32f, 0.08f);
                    if (stalk.GetComponent<Collider>()) Destroy(stalk.GetComponent<Collider>());
                    stalk.GetComponent<Renderer>().material = skinMat;

                    CreateCreatureEye(bodyRoot.transform, new Vector3(s * 0.32f, 1.18f, -0.26f), 0.22f, eyeWhiteMat, pupilMat);
                }
            }

            // 4. Uzuvlar (Slot P: PHELM=Armless, PONDO=Stubby limbs, PLORT=Two arms, PUNKO=Tentacles, PRIZZ=Four arms)
            string pUpper = geneP != null ? geneP.ToUpperInvariant() : "PLORT";
            if (pUpper == "PONDO")
            {
                CreateCreatureLimb(bodyRoot.transform, new Vector3(-0.55f, 0.05f, 0f), new Vector3(0.18f, 0.22f, 0.18f), Quaternion.Euler(0f, 0f, 45f), skinMat);
                CreateCreatureLimb(bodyRoot.transform, new Vector3(0.55f, 0.05f, 0f), new Vector3(0.18f, 0.22f, 0.18f), Quaternion.Euler(0f, 0f, -45f), skinMat);
            }
            else if (pUpper == "PLORT")
            {
                CreateCreatureLimb(bodyRoot.transform, new Vector3(-0.72f, 0.05f, 0f), new Vector3(0.16f, 0.52f, 0.16f), Quaternion.Euler(0f, 0f, 55f), skinMat);
                CreateCreatureLimb(bodyRoot.transform, new Vector3(0.72f, 0.05f, 0f), new Vector3(0.16f, 0.52f, 0.16f), Quaternion.Euler(0f, 0f, -55f), skinMat);
            }
            else if (pUpper == "PRIZZ")
            {
                CreateCreatureLimb(bodyRoot.transform, new Vector3(-0.72f, 0.25f, 0f), new Vector3(0.15f, 0.50f, 0.15f), Quaternion.Euler(0f, 0f, 48f), skinMat);
                CreateCreatureLimb(bodyRoot.transform, new Vector3(0.72f, 0.25f, 0f), new Vector3(0.15f, 0.50f, 0.15f), Quaternion.Euler(0f, 0f, -48f), skinMat);
                CreateCreatureLimb(bodyRoot.transform, new Vector3(-0.70f, -0.22f, 0f), new Vector3(0.15f, 0.48f, 0.15f), Quaternion.Euler(0f, 0f, 68f), skinMat);
                CreateCreatureLimb(bodyRoot.transform, new Vector3(0.70f, -0.22f, 0f), new Vector3(0.15f, 0.48f, 0.15f), Quaternion.Euler(0f, 0f, -68f), skinMat);
            }
            else if (pUpper == "PUNKO")
            {
                // 4 Dokunaç (Tentacles)
                for (int t = 0; t < 4; t++)
                {
                    float side = (t % 2 == 0) ? -1f : 1f;
                    float yOff = (t < 2) ? 0.18f : -0.22f;
                    for (int seg = 0; seg < 3; seg++)
                    {
                        var segGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        segGo.name = "TentacleSeg";
                        segGo.transform.SetParent(bodyRoot.transform, false);
                        float dist = 0.52f + seg * 0.22f;
                        float waveY = yOff + Mathf.Sin(seg * 1.2f) * 0.12f;
                        segGo.transform.localPosition = new Vector3(side * dist, waveY, -0.1f * seg);
                        segGo.transform.localScale = Vector3.one * (0.24f - seg * 0.05f);
                        if (segGo.GetComponent<Collider>()) Destroy(segGo.GetComponent<Collider>());
                        segGo.GetComponent<Renderer>().material = skinMat;
                    }
                }
            }

            // İç Işık (Point Light)
            var lightGo = new GameObject("SpecimenLight");
            lightGo.transform.SetParent(labChamberCreature.transform, false);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = skinColor;
            pl.range = 8f;
            pl.intensity = 3.2f;

            // Numune Bilgi Tabelası
            CreateTextMeshOn(labChamberCreature.transform, "SpecimenPlaque",
                $"🔬 KAPSÜLLENEN NUMUNE: {creatureName} [{planetName}]\n[ Z:{geneZ} • V:{geneV} • K:{geneK} • P:{geneP} ]",
                new Vector3(0f, 1.95f, 0f), 0.032f, 96, Color.white);
        }

        private void CreateCreatureEye(Transform parent, Vector3 localPos, float size, Material whiteMat, Material pupilMat)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * size;
            if (eye.GetComponent<Collider>()) Destroy(eye.GetComponent<Collider>());
            eye.GetComponent<Renderer>().material = whiteMat;

            var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = "Pupil";
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0f, 0f, -0.42f);
            pupil.transform.localScale = Vector3.one * 0.48f;
            if (pupil.GetComponent<Collider>()) Destroy(pupil.GetComponent<Collider>());
            pupil.GetComponent<Renderer>().material = pupilMat;
        }

        private void CreateCreatureLimb(Transform parent, Vector3 localPos, Vector3 scale, Quaternion rot, Material mat)
        {
            var limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            limb.name = "Limb";
            limb.transform.SetParent(parent, false);
            limb.transform.localPosition = localPos;
            limb.transform.localRotation = rot;
            limb.transform.localScale = scale;
            if (limb.GetComponent<Collider>()) Destroy(limb.GetComponent<Collider>());
            limb.GetComponent<Renderer>().material = mat;
        }

        public void ViewInLabChamber()
        {
            if (!isLabMode) return;
            showSynthesisModal = false;
            isTerminalActive = false;
            if (puzzleRoot != null)
            {
                puzzleRoot.SetActive(false);
            }
            if (dedicatedTerminalCamera != null)
            {
                dedicatedTerminalCamera.gameObject.SetActive(false);
            }
            if (chamberStation != null && labStationController != null)
            {
                labStationController.GoToStation(chamberStation);
            }
            else if (labStationController != null)
            {
                labStationController.ReturnToBase();
            }
        }
        #endregion

        private void EnsurePuzzleInitialized()
        {
            if (!Application.isPlaying) return;
            if (Instance == null) Instance = this;

            if (!isLabMode)
            {
                var sc = Object.FindAnyObjectByType<CameraStationController>();
                if (sc != null)
                {
                    InitializeForLabStation(sc);
                    return;
                }
            }

            CameraStationController.IsInputIntercepted = () =>
            {
                if (isLabMode && isTerminalActive && showRulebookModal)
                {
                    showRulebookModal = false;
                    return true;
                }
                if (isLabMode && isTerminalActive && isRepairZoomMode)
                {
                    ExitRepairZoomMode();
                    return true;
                }
                return false;
            };

            if (activeSlots != null && activeSlots.Count > 0 && activeSlots[0] != null && manager != null && labScreen != null)
            {
                return;
            }

            // Domain Reload (Hot-Reload) sonrası sahnede kalan eski alt objeleri temizle ve sıfırdan kur
            activeSlots.Clear();
            poolPieces.Clear();
            foreach (var c in repairCandidates) if (c != null) Destroy(c.gameObject);
            repairCandidates.Clear();
            hoveredCandidate = null;
            selectedRepairGenome = null;
            zoomedGenome = null;
            focusBackdropQuad = null;
            isRepairZoomMode = false;
            draggedPiece = null;
            dragCandidatePiece = null;
            draggedCandidate = null;
            dragCandidateCand = null;
            isDraggingPiece = false;
            isDraggingCandidate = false;
            snapTargetSlotIndex = -1;
            snapGuideLine = null;
            resonanceBeams.Clear();
            helixRibbon = null;
            backgroundChamber = null;
            labScreen = null;
            creatureEgg = null;
            dropCinematic = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == tabletGlass) continue;
                Destroy(child.gameObject);
            }

            puzzleRoot = new GameObject("PuzzleRoot");
            puzzleRoot.transform.SetParent(transform, false);
            puzzleRoot.transform.localPosition = Vector3.zero;

            planets = DNAPuzzleSampleData.CreateSamplePlanets();

            manager = GetComponent<DNAPuzzleManager>();
            if (manager == null) manager = gameObject.AddComponent<DNAPuzzleManager>();

            simulator = GetComponent<SurvivalSimulator>();
            if (simulator == null) simulator = gameObject.AddComponent<SurvivalSimulator>();

            audioManager = GetComponent<DNAPuzzleAudio>();
            if (audioManager == null) audioManager = gameObject.AddComponent<DNAPuzzleAudio>();

            simulator.OnPhaseUpdate -= OnSimulatorPhaseUpdate;
            simulator.OnPhaseUpdate += OnSimulatorPhaseUpdate;

            simulator.OnSimulationFinished -= OnSimulatorFinished;
            simulator.OnSimulationFinished += OnSimulatorFinished;

            SetupCameraAndLighting();
            BuildStunningSciFiChamber();
            SpawnHolographicCreature();

            var dropObj = new GameObject("PlanetaryDropCinematic");
            dropObj.transform.SetParent(puzzleRoot.transform, false);
            dropCinematic = dropObj.AddComponent<PlanetaryDropCinematic>();
            dropCinematic.Initialize(puzzleCamera, labScreen, audioManager);
            dropCinematic.OnCinematicFinished += () =>
            {
                if (lastSynthesizedCreature != null && lastSynthesizedCreature.isValid)
                {
                    showSynthesisModal = true;
                    teamDispatchMessage = "";
                }
            };

            manager.SetGenePool(DNAPuzzleSampleData.CreateSampleGenes());
            LoadPlanet(Mathf.Clamp(currentPlanetIdx, 0, planets.Count - 1));

            // Lab sahnesindeyken başlangıçta tüm puzzle geometrisini gizli tut (oda tertemiz kalsın)
            if (isLabMode && !isTerminalActive)
            {
                if (puzzleRoot != null) puzzleRoot.SetActive(false);
                if (dedicatedTerminalCamera != null) dedicatedTerminalCamera.gameObject.SetActive(false);
            }
        }

        private void OnSimulatorPhaseUpdate(SimulationPhase phase, string msg, bool ok)
        {
            simulationLog = msg;
            isSimulating = (phase != SimulationPhase.Completed && phase != SimulationPhase.Idle);
            creatureEgg?.OnPhaseSimulate(phase, ok);
            if (ok) audioManager?.PlayPhaseStep();
            else audioManager?.PlayMismatch();
        }

        private void OnSimulatorFinished(EvaluationResult res)
        {
            lastEvaluation = res;
            isSimulating = false;
            creatureEgg?.OnPhaseSimulate(SimulationPhase.Completed, res.isSuccess);
            if (res.isSuccess) audioManager?.PlayVictory();
            else audioManager?.PlayMismatch();

            lastSynthesizedCreature = CreatureSynthesisBridge.BuildFromPuzzle(
                manager.StrandGenes,
                manager.CalculateTotalStats(),
                manager.GetActiveSynergies(),
                manager.CurrentPlanet,
                res
            );

            if (isLabMode)
            {
                var creatures = LacunaGeneticsDatabase.GetCreatures();
                var curC = creatures[Mathf.Clamp(currentCreatureIdx, 0, creatures.Count - 1)];
                string zG = poolPieces.Count > 0 && poolPieces[0]?.Data != null ? poolPieces[0].Data.geneName : curC.startGeneZ;
                string vG = poolPieces.Count > 1 && poolPieces[1]?.Data != null ? poolPieces[1].Data.geneName : curC.startGeneV;
                string kG = poolPieces.Count > 2 && poolPieces[2]?.Data != null ? poolPieces[2].Data.geneName : curC.startGeneK;
                string pG = poolPieces.Count > 3 && poolPieces[3]?.Data != null ? poolPieces[3].Data.geneName : curC.startGeneP;
                SpawnLacunaCreatureInLabChamber(curC.creatureName, curC.targetPlanet, zG, vG, kG, pG);
            }

            RefreshAllStates();
        }

        private void SetupCameraAndLighting()
        {
            if (isLabMode)
            {
                if (dedicatedTerminalCamera == null)
                {
                    var camGo = new GameObject("TerminalPuzzleCamera");
                    camGo.transform.SetParent(transform, false);
                    camGo.transform.localPosition = new Vector3(0f, 0.2f, -13.2f);
                    camGo.transform.localRotation = Quaternion.identity;
                    dedicatedTerminalCamera = camGo.AddComponent<Camera>();
                    dedicatedTerminalCamera.clearFlags = CameraClearFlags.SolidColor;
                    dedicatedTerminalCamera.backgroundColor = new Color(0.012f, 0.015f, 0.035f);
                    dedicatedTerminalCamera.fieldOfView = 56f;
                    dedicatedTerminalCamera.depth = 12; // MainCamera'nın (depth 0) üstünde çizilir
                }
                puzzleCamera = dedicatedTerminalCamera;
                if (!isTerminalActive)
                {
                    dedicatedTerminalCamera.gameObject.SetActive(false);
                }

                Transform pRoot = puzzleRoot != null ? puzzleRoot.transform : transform;

                // Lab sahnesinde odanın genel ışığını bozmamak için puzzle'a özel yerel point ışıklar oluştur
                var localKeyLightGo = new GameObject("TerminalLocalKeyLight");
                localKeyLightGo.transform.SetParent(pRoot, false);
                var localKey = localKeyLightGo.AddComponent<Light>();
                localKey.type = LightType.Point;
                localKey.transform.localPosition = new Vector3(0f, 5f, -8f);
                localKey.range = 35f;
                localKey.intensity = 2.5f;
                localKey.color = new Color(0.92f, 0.96f, 1f);

                var rimLightGo = new GameObject("CyanRimLight");
                rimLightGo.transform.SetParent(pRoot, false);
                var rimLight = rimLightGo.AddComponent<Light>();
                rimLight.type = LightType.Point;
                rimLight.transform.localPosition = new Vector3(-8f, 4f, -4f);
                rimLight.color = new Color(0.1f, 0.8f, 1f);
                rimLight.range = 25f;
                rimLight.intensity = 2.2f;

                var warmLightGo = new GameObject("PlasmaRimLight");
                warmLightGo.transform.SetParent(pRoot, false);
                var warmLight = warmLightGo.AddComponent<Light>();
                warmLight.type = LightType.Point;
                warmLight.range = 25f;
                warmLight.transform.localPosition = new Vector3(8f, -2f, -3f);
                warmLight.color = new Color(0.75f, 0.2f, 1f);
                warmLight.intensity = 1.8f;
                return;
            }

            puzzleCamera = Camera.main;
            if (puzzleCamera == null)
            {
                var camGo = new GameObject("MainCamera");
                puzzleCamera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            // Normal Standalone mod
            puzzleCamera.transform.position = new Vector3(0f, 0.2f, -13.2f);
            puzzleCamera.transform.rotation = Quaternion.identity;
            puzzleCamera.backgroundColor = new Color(0.012f, 0.015f, 0.035f);
            puzzleCamera.clearFlags = CameraClearFlags.SolidColor;
            puzzleCamera.fieldOfView = 56f;
            RenderSettings.ambientLight = new Color(0.2f, 0.28f, 0.42f);

            Light dirLight = null;
            foreach (var l in FindObjectsByType<Light>())
            {
                if (l != null && l.type == LightType.Directional)
                {
                    dirLight = l;
                    break;
                }
            }
            if (dirLight == null)
            {
                var lightGo = new GameObject("SciFiDirectionalLight");
                lightGo.transform.SetParent(transform, false);
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.transform.rotation = Quaternion.Euler(38f, -25f, 0f);
                dirLight.color = new Color(0.92f, 0.96f, 1f);
                dirLight.intensity = 1.3f;
            }

            var rimLightGo2 = new GameObject("CyanRimLight");
            rimLightGo2.transform.SetParent(transform, false);
            var rimLight2 = rimLightGo2.AddComponent<Light>();
            rimLight2.type = LightType.Point;
            rimLight2.transform.position = new Vector3(-8f, 4f, -4f);
            rimLight2.color = new Color(0.1f, 0.8f, 1f);
            rimLight2.range = 14f;
            rimLight2.intensity = 2.2f;

            var warmLightGo2 = new GameObject("PlasmaRimLight");
            warmLightGo2.transform.SetParent(transform, false);
            var warmLight2 = warmLightGo2.AddComponent<Light>();
            warmLight2.type = LightType.Point;
            warmLight2.range = 14f;
            warmLight2.transform.position = new Vector3(8f, -2f, -3f);
            warmLight2.color = new Color(0.75f, 0.2f, 1f);
            warmLight2.intensity = 1.8f;
        }

        private void BuildStunningSciFiChamber()
        {
            if (backgroundChamber != null) return;

            Transform pRoot = puzzleRoot != null ? puzzleRoot.transform : transform;
            backgroundChamber = new GameObject("SciFiBioChamberBackdrop");
            backgroundChamber.transform.SetParent(pRoot, false);
            backgroundChamber.transform.localPosition = Vector3.zero;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            // 1. Siber Biyo-Laboratuvar Zemin Platformu (Clean Cyber Lab Floor)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CyberLabFloor";
            floor.transform.SetParent(backgroundChamber.transform, false);
            floor.transform.localPosition = new Vector3(0f, -5.5f, 0f);
            floor.transform.localScale = new Vector3(36f, 0.2f, 12f);
            Destroy(floor.GetComponent<Collider>());
            var floorMat = new Material(litShader);
            floorMat.color = new Color(0.04f, 0.07f, 0.12f);
            if (floorMat.HasProperty("_Smoothness")) floorMat.SetFloat("_Smoothness", 0.92f);
            floor.GetComponent<MeshRenderer>().material = floorMat;

            // 2. Yüksek Teknolojili Biyo-Laboratuvar Terminal Ekranı (AnimatedLaboratoryScreen)
            GameObject labScreenObj = new GameObject("AnimatedLaboratoryScreen");
            labScreenObj.transform.SetParent(backgroundChamber.transform, false);
            labScreen = labScreenObj.AddComponent<AnimatedLaboratoryScreen>();
            labScreen.Initialize();

            // Ekrandaki 3D Simülasyon Odası Modüllerine tıklandığında ortamı değiştir
            labScreen.OnEnvironmentSelected += (idx) => {
                audioManager?.PlayResonance();
                if (idx != currentPlanetIdx)
                {
                    LoadPlanet(idx);
                }
                else
                {
                    labScreen.TriggerClickJuice(idx);
                }
            };
        }

        private void SpawnHolographicCreature()
        {
            Transform pRoot = puzzleRoot != null ? puzzleRoot.transform : transform;
            GameObject eggObj = new GameObject("AnimatedCreatureEgg");
            eggObj.transform.SetParent(pRoot, false);
            eggObj.transform.localPosition = new Vector3(-7.85f, 1.45f, 0f);
            eggObj.transform.localScale = Vector3.one * 0.95f;
            creatureEgg = eggObj.AddComponent<AnimatedCreatureEgg>();
        }

        public void LoadCreature(int creatureIdx)
        {
            var creatures = LacunaGeneticsDatabase.GetCreatures();
            currentCreatureIdx = Mathf.Clamp(creatureIdx, 0, creatures.Count - 1);
            DNAPuzzleSampleData.ActiveCreatureIndex = currentCreatureIdx;

            var c = creatures[currentCreatureIdx];
            int pIdx = 0;
            if (c.targetPlanet == "SNIBBLEX") pIdx = 1;
            else if (c.targetPlanet == "OOMGARTH") pIdx = 2;

            LoadPlanet(pIdx, keepActiveCreature: true);
        }

        public void LoadPlanet(int idx, bool keepActiveCreature = false)
        {
            if (isRepairZoomMode)
            {
                ExitRepairZoomMode(false);
            }

            currentPlanetIdx = idx;
            lastSynergyCount = 0;
            lastResonanceCount = 0;
            lastInstabilityCount = 0;
            selectedSlot = 0;

            if (!keepActiveCreature)
            {
                var creatures = LacunaGeneticsDatabase.GetCreatures();
                string targetP = idx == 1 ? "SNIBBLEX" : (idx == 2 ? "OOMGARTH" : "FABAUP");
                if (creatures[currentCreatureIdx].targetPlanet != targetP)
                {
                    for (int i = 0; i < creatures.Count; i++)
                    {
                        if (creatures[i].targetPlanet == targetP)
                        {
                            currentCreatureIdx = i;
                            break;
                        }
                    }
                }
                DNAPuzzleSampleData.ActiveCreatureIndex = currentCreatureIdx;
            }

            if (planets == null || planets.Count == 0)
                planets = DNAPuzzleSampleData.CreateSamplePlanets();

            PlanetData planet = planets[idx];
            manager.InitializePlanet(planet);

            // Laboratuvar ekranını ve aktif simülasyon odası seçimini anında güncelle
            if (labScreen != null)
            {
                labScreen.SetEnvironment(idx);
            }

            BuildSlotsAndStrand(planet.strandSlotCount);
            Build4BrokenGenomes(planet);
            BuildRepairCandidateTray(planet);
            RefreshAllStates();

            var curC = LacunaGeneticsDatabase.GetCreatures()[currentCreatureIdx];
            repairFeedbackMessage = $"🧬 DENEY #{curC.creatureNumber}: {curC.creatureName} ({curC.targetPlanet}) | Bozuk genomlara tıklayıp ●↔■ ve ▲↔◆ kurallarına göre onarın ve yanlış geni düzeltin!";
        }

        private void BuildSlotsAndStrand(int slotCount)
        {
            foreach (var s in activeSlots) if (s != null) Destroy(s.gameObject);
            activeSlots.Clear();

            foreach (var b in resonanceBeams) if (b != null) Destroy(b.gameObject);
            resonanceBeams.Clear();

            Transform pRoot = puzzleRoot != null ? puzzleRoot.transform : transform;
            float spacingX = (slotCount >= 5) ? 2.85f : 2.95f;
            float totalWidth = (slotCount - 1) * spacingX;
            float startX = 1.35f - (totalWidth * 0.5f);
            float slotY = 1.45f; // Ferah ve temiz aralık

            if (helixRibbon != null) Destroy(helixRibbon.gameObject);
            GameObject ribbonObj = new GameObject("DNAHelixStrandRibbon");
            ribbonObj.transform.SetParent(pRoot, false);
            helixRibbon = ribbonObj.AddComponent<DNAHelixStrandRibbon>();
            helixRibbon.Initialize(slotCount, startX, spacingX, slotY);

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObj = new GameObject($"GenomeSlot_{i}");
                slotObj.transform.SetParent(pRoot, false);
                slotObj.transform.localPosition = new Vector3(startX + i * spacingX, slotY, 0f);
                slotObj.transform.localScale = Vector3.one * 0.95f;

                var slot = slotObj.AddComponent<AnimatedSlot>();
                slot.Initialize(i);
                slot.OnSlotClicked += HandleSlotClicked;

                activeSlots.Add(slot);

                if (i > 0)
                {
                    GameObject beamObj = new GameObject($"ResonanceBeam_{i - 1}_{i}");
                    beamObj.transform.SetParent(pRoot, false);
                    var beam = beamObj.AddComponent<AnimatedResonanceBeam>();
                    beam.SetTargets(activeSlots[i - 1].transform, slot.transform);
                    resonanceBeams.Add(beam);
                }
            }
        }

        private void Build4BrokenGenomes(PlanetData planet)
        {
            foreach (var p in poolPieces) if (p != null) Destroy(p.gameObject);
            poolPieces.Clear();
            selectedRepairGenome = null;

            var brokenGenes = DNAPuzzleSampleData.CreateBrokenGenomesForPlanet(planet);
            manager.SetGenePool(brokenGenes);

            Transform pRoot = puzzleRoot != null ? puzzleRoot.transform : transform;
            int count = brokenGenes.Count;
            float spacingX = 2.95f;
            float startX = 1.35f - ((count - 1) * spacingX * 0.5f);
            float benchY = -0.58f; // Yukarıdaki yuvalarla ferah aralık

            for (int i = 0; i < count; i++)
            {
                GameObject pieceObj = new GameObject($"GenomePiece_{i}_{brokenGenes[i].geneName}");
                pieceObj.transform.SetParent(pRoot, false);
                Vector3 targetLocalPos = new Vector3(startX + i * spacingX, benchY, 0f);
                pieceObj.transform.localPosition = targetLocalPos + Vector3.down * 2.5f;
                pieceObj.transform.localScale = Vector3.one * 0.82f;

                var piece = pieceObj.AddComponent<AnimatedGenePiece>();
                piece.Initialize(brokenGenes[i]);
                piece.SetTargetScale(Vector3.one * 0.82f);
                Vector3 worldTarget = pieceObj.transform.position + Vector3.up * 2.5f;
                piece.MoveTo(worldTarget, Quaternion.identity, 0.35f + i * 0.05f);
                piece.OnPieceClicked += HandlePieceClicked;

                poolPieces.Add(piece);
            }

            // İlk bozuk genomu otomatik onarım hedefi olarak seç
            if (poolPieces.Count > 0)
            {
                SelectGenomeForRepair(poolPieces[0]);
            }
        }

        private void BuildRepairCandidateTray(PlanetData planet)
        {
            foreach (var c in repairCandidates) if (c != null) Destroy(c.gameObject);
            repairCandidates.Clear();
            hoveredCandidate = null;

            var brokenGenes = manager.GenePool;
            var candidates = DNAPuzzleSampleData.CreateRepairCandidatesForPlanet(planet, new List<GeneData>(brokenGenes));

            Transform pRoot = puzzleRoot != null ? puzzleRoot.transform : transform;
            int count = candidates.Count;
            float spacingX = 1.95f;
            float startX = 1.35f - ((count - 1) * spacingX * 0.5f);
            float trayY = -2.60f;

            for (int i = 0; i < count; i++)
            {
                GameObject candObj = new GameObject($"RepairCandidate_{i}_{candidates[i]}");
                candObj.transform.SetParent(pRoot, false);
                Vector3 localPos = new Vector3(startX + i * spacingX, trayY, 0f);
                candObj.transform.localPosition = localPos;

                var cand = candObj.AddComponent<AnimatedRepairCandidate>();
                cand.Initialize(i, candidates[i], candObj.transform.position);
                cand.OnCandidateClicked += HandleCandidateClicked;

                repairCandidates.Add(cand);
            }
        }

        public void SelectGenomeForRepair(AnimatedGenePiece piece)
        {
            if (selectedRepairGenome != null)
            {
                selectedRepairGenome.SetSelectedForRepair(false);
            }
            selectedRepairGenome = piece;
            if (selectedRepairGenome != null)
            {
                selectedRepairGenome.SetSelectedForRepair(true);
                if (selectedRepairGenome.Data != null && selectedRepairGenome.Data.isBroken && !selectedRepairGenome.Data.isRepaired)
                {
                    repairFeedbackMessage = $"🛠️ SEÇİLDİ: {selectedRepairGenome.Data.geneName} | {selectedRepairGenome.Data.defectDescription} -> Alttaki parçalardan doğru olanı seçin!";
                }
                else
                {
                    repairFeedbackMessage = $"✅ {selectedRepairGenome.Data?.geneName} ONARILDI! Tıklayarak yukarıdaki ana sarmala yerleştirebilirsin.";
                }
            }
        }

        private void EnsureFocusBackdrop()
        {
            if (focusBackdropQuad != null) return;
            focusBackdropQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            focusBackdropQuad.name = "RepairFocusBackdrop";
            focusBackdropQuad.transform.SetParent(transform, false);
            focusBackdropQuad.transform.localPosition = new Vector3(0f, 0.2f, 2.0f);
            focusBackdropQuad.transform.localScale = new Vector3(45f, 28f, 1f);
            Destroy(focusBackdropQuad.GetComponent<Collider>());

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            var mat = new Material(unlitShader);
            mat.color = new Color(0.010f, 0.016f, 0.032f);
            focusBackdropQuad.GetComponent<MeshRenderer>().material = mat;
            focusBackdropQuad.SetActive(false);
        }

        public Vector3 PuzzleToWorld(Vector3 localPos)
        {
            Transform root = puzzleRoot != null ? puzzleRoot.transform : transform;
            return root.TransformPoint(localPos);
        }

        public Vector3 WorldToPuzzle(Vector3 worldPos)
        {
            Transform root = puzzleRoot != null ? puzzleRoot.transform : transform;
            return root.InverseTransformPoint(worldPos);
        }

        public void EnterRepairZoomMode(AnimatedGenePiece piece)
        {
            if (piece == null) return;
            EnsureFocusBackdrop();

            // Eğer genom halihazırda ana sarmala takılıysa, düzenlemek için sarmaldan çıkar
            if (piece.IsEquipped)
            {
                int slotIdx = piece.CurrentSlotIndex;
                if (slotIdx >= 0 && slotIdx < activeSlots.Count && activeSlots[slotIdx] != null)
                {
                    activeSlots[slotIdx].RemovePiece();
                    manager.RemoveGene(slotIdx);
                }
                piece.CurrentSlotIndex = -1;
            }

            if (zoomedGenome != null && zoomedGenome != piece)
            {
                zoomedGenome.SetZoomedInRepairLab(false);
                zoomedGenome.SetSelectedForRepair(false);
                zoomedGenome.SetTargetScale(Vector3.one * 0.82f);
                if (!zoomedGenome.IsEquipped)
                {
                    int prevIdx = poolPieces.IndexOf(zoomedGenome);
                    if (prevIdx >= 0)
                    {
                        float spX = 2.95f;
                        float stX = 1.35f - ((poolPieces.Count - 1) * spX * 0.5f);
                        zoomedGenome.transform.position = PuzzleToWorld(new Vector3(stX + prevIdx * spX, -0.58f, 0f));
                    }
                }
                zoomedGenome.gameObject.SetActive(false);
            }

            isRepairZoomMode = true;
            zoomedGenome = piece;
            selectedRepairGenome = piece;

            // ÖNCE seçilen genomu aktif et ki hiçbir Coroutine hatası oluşmasın!
            if (!piece.gameObject.activeSelf)
            {
                piece.gameObject.SetActive(true);
            }
            piece.SetSelectedForRepair(true);
            piece.SetZoomedInRepairLab(true);

            if (focusBackdropQuad != null) focusBackdropQuad.SetActive(true);

            // Arka plandaki diğer slot ve parçaları gizle (arka plan tertemiz olsun)
            foreach (var slot in activeSlots) if (slot != null) slot.gameObject.SetActive(false);
            foreach (var p in poolPieces) if (p != null && p != piece) p.gameObject.SetActive(false);
            if (creatureEgg != null) creatureEgg.gameObject.SetActive(false);
            if (helixRibbon != null) helixRibbon.gameObject.SetActive(false);
            if (labScreen != null) labScreen.gameObject.SetActive(false);
            foreach (var b in resonanceBeams) if (b != null) b.gameObject.SetActive(false);

            // Seçilen genomu tam merkeze dengeli bir boyutta getir (Önce hedef ölçeği ata, sonra hareketi başlat)
            isDraggingPiece = false;
            draggedPiece = null;
            dragCandidatePiece = null;
            snapTargetSlotIndex = -1;

            Vector3 centerPos = PuzzleToWorld(new Vector3(0f, 0.72f, -1.8f));
            zoomedGenome.SetTargetScale(Vector3.one * 1.65f);
            zoomedGenome.MoveTo(centerPos, Quaternion.identity, 0.40f);

            // Alttaki onarım parçalarını odak moduna taşı
            int count = repairCandidates.Count;
            float spacingX = 2.05f;
            float startX = -((count - 1) * spacingX * 0.5f);
            float trayY = -2.35f;

            for (int i = 0; i < count; i++)
            {
                if (repairCandidates[i] != null)
                {
                    repairCandidates[i].gameObject.SetActive(true);
                    Vector3 pos = PuzzleToWorld(new Vector3(startX + i * spacingX, trayY, -1.8f));
                    repairCandidates[i].SetBasePosition(pos, Vector3.one * 1.12f);
                }
            }

            audioManager?.PlaySnap();
            repairFeedbackMessage = $"🔬 {piece.Data.geneName} ONARIM MODU | {piece.Data.defectDescription} -> Alttan doğru nükleotidi seçin!";
        }

        public void ExitRepairZoomMode(bool equipToMainStrand = false)
        {
            if (!isRepairZoomMode) return;

            if (focusBackdropQuad != null) focusBackdropQuad.SetActive(false);

            var genomeToHandle = zoomedGenome;
            isRepairZoomMode = false;
            zoomedGenome = null;

            // Gizlenen tüm sahne objelerini geri aç
            foreach (var slot in activeSlots) if (slot != null) slot.gameObject.SetActive(true);
            if (creatureEgg != null) creatureEgg.gameObject.SetActive(true);
            if (helixRibbon != null) helixRibbon.gameObject.SetActive(true);
            if (labScreen != null) labScreen.gameObject.SetActive(true);
            foreach (var b in resonanceBeams) if (b != null) b.gameObject.SetActive(true);

            // Alttaki onarım parçalarını normal tepsiye geri gönder
            int count = repairCandidates.Count;
            float spacingX = 1.95f;
            float startX = 1.35f - ((count - 1) * spacingX * 0.5f);
            float trayY = -2.65f;

            for (int i = 0; i < count; i++)
            {
                if (repairCandidates[i] != null)
                {
                    repairCandidates[i].gameObject.SetActive(true);
                    Vector3 pos = PuzzleToWorld(new Vector3(startX + i * spacingX, trayY, 0f));
                    repairCandidates[i].SetBasePosition(pos, Vector3.one);
                }
            }

            if (genomeToHandle != null && equipToMainStrand && (genomeToHandle.Data.isRepaired || !genomeToHandle.Data.isBroken || genomeToHandle.Data.isDeformed))
            {
                EquipRepairedGenomeToMainStrand(genomeToHandle);
            }

            // KESİN ÇÖZÜM: Sarmala takılı olmayan TÜM pool parçalarını güvenle aktif et, onarım zoom bayraklarını temizle ve kendi tezgah yuvalarına geri süzdür!
            for (int i = 0; i < poolPieces.Count; i++)
            {
                var p = poolPieces[i];
                if (p == null) continue;

                p.gameObject.SetActive(true);
                p.SetZoomedInRepairLab(false);
                p.SetSelectedForRepair(false);

                if (!p.IsEquipped)
                {
                    ReturnPieceToPool(p);
                }
            }

            var nextBroken = poolPieces.Find(p => p != null && !p.IsEquipped && p.Data != null && p.Data.isBroken && !p.Data.isRepaired && !p.Data.isDeformed);
            if (nextBroken != null)
            {
                SelectGenomeForRepair(nextBroken);
            }

            RefreshAllStates();
        }

        private void EquipRepairedGenomeToMainStrand(AnimatedGenePiece piece)
        {
            if (piece == null || activeSlots == null || activeSlots.Count == 0) return;

            // Eğer parça önceden başka bir yuvaya takılıysa önce oradan temizce ayır
            if (piece.IsEquipped && piece.CurrentSlotIndex >= 0 && piece.CurrentSlotIndex < activeSlots.Count)
            {
                activeSlots[piece.CurrentSlotIndex].RemovePiece();
                manager.RemoveGene(piece.CurrentSlotIndex);
            }

            int targetSlot = -1;
            if (selectedSlot >= 0 && selectedSlot < activeSlots.Count && !activeSlots[selectedSlot].IsOccupied)
            {
                targetSlot = selectedSlot;
            }
            else
            {
                int emptyIdx = activeSlots.FindIndex(s => s != null && !s.IsOccupied);
                if (emptyIdx != -1) targetSlot = emptyIdx;
                else
                {
                    targetSlot = Mathf.Clamp(selectedSlot, 0, activeSlots.Count - 1);
                    var oldPiece = activeSlots[targetSlot].RemovePiece();
                    if (oldPiece != null)
                    {
                        ReturnPieceToPool(oldPiece);
                        manager.RemoveGene(targetSlot);
                    }
                }
            }

            if (targetSlot >= 0 && targetSlot < activeSlots.Count)
            {
                activeSlots[targetSlot].PlacePiece(piece);
                manager.EquipGene(targetSlot, piece.Data);
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"🔗 {piece.Data.geneName} ana sarmal yuvasına ({targetSlot + 1}) takıldı!";

                int nextEmpty = activeSlots.FindIndex(s => s != null && !s.IsOccupied);
                selectedSlot = nextEmpty != -1 ? nextEmpty : targetSlot;
            }
        }

        private int lastClickFrame = -1;

        private void HandleSlotClicked(int slotIdx)
        {
            if (isSimulating || showSynthesisModal || isRepairZoomMode) return;
            if (Time.frameCount == lastClickFrame) return;
            lastClickFrame = Time.frameCount;

            if (slotIdx < 0 || slotIdx >= activeSlots.Count) return;
            var slot = activeSlots[slotIdx];
            selectedSlot = slotIdx;
            if (slot != null)
            {
                slot.TriggerDockPulse();
                if (slot.IsOccupied)
                {
                    var removed = slot.RemovePiece();
                    ReturnPieceToPool(removed);
                    manager.RemoveGene(slotIdx);
                    audioManager?.PlaySnap();
                    repairFeedbackMessage = $"↩️ {removed?.Data?.geneName} sarmaldan tezgaha geri indirildi.";
                    RefreshAllStates();
                }
                else
                {
                    // Boş yuvaya tıklandı: Seçili veya hazırda onarılmış bir genom varsa doğrudan bu yuvaya yerleştir!
                    AnimatedGenePiece readyPiece = null;
                    if (selectedRepairGenome != null && !selectedRepairGenome.IsEquipped &&
                        selectedRepairGenome.Data != null && (selectedRepairGenome.Data.isRepaired || !selectedRepairGenome.Data.isBroken))
                    {
                        readyPiece = selectedRepairGenome;
                    }
                    else
                    {
                        readyPiece = poolPieces.Find(p => p != null && !p.IsEquipped && p.Data != null && (p.Data.isRepaired || !p.Data.isBroken));
                    }

                    if (readyPiece != null)
                    {
                        EquipRepairedGenomeToMainStrand(readyPiece);
                        RefreshAllStates();
                    }
                    else
                    {
                        audioManager?.PlaySnap();
                        repairFeedbackMessage = $"🎯 Yuva #{slotIdx + 1} seçildi! Önce tezgahtaki bozuk bir genoma tıklayıp onarın.";
                    }
                }
            }
        }

        private void HandlePieceClicked(AnimatedGenePiece piece)
        {
            if (isSimulating || showSynthesisModal || piece == null) return;
            if (Time.frameCount == lastClickFrame) return;
            lastClickFrame = Time.frameCount;

            if (piece.IsEquipped)
            {
                // Sarmaldan çıkar ve tezgaha geri indir
                int slotIdx = piece.CurrentSlotIndex;
                if (slotIdx >= 0 && slotIdx < activeSlots.Count)
                {
                    activeSlots[slotIdx].RemovePiece();
                    manager.RemoveGene(slotIdx);
                }
                ReturnPieceToPool(piece);
                SelectGenomeForRepair(piece);
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"↩️ {piece.Data.geneName} sarmaldan tezgaha alındı. Tekrar düzenlemek için üzerine tıklayabilirsin!";
            }
            else
            {
                // Genom tezgahta:
                if (isRepairZoomMode)
                {
                    // Odak modundayken 3D genomun üzerine tıklanırsa mevcut bazı söküp tekrar düzenlemeye aç
                    if (piece.Data.isRepaired || piece.Data.hasPlacedNucleotide)
                    {
                        piece.ResetRepairState();
                        audioManager?.PlaySnap();
                        repairFeedbackMessage = $"🔄 {piece.Data.geneName} bazı söküldü! Alttaki kapsüllerden yeni bazı seçerek tekrar düzenleyebilirsin.";
                    }
                    else
                    {
                        piece.TriggerGlitchShake();
                        audioManager?.PlayMismatch();
                    }
                }
                else
                {
                    // Tezgahtaki genoma (ister bozuk ister daha önce onarılmış olsun) tıklanınca
                    // tam ekran Biyo-Onarım & Düzenleme Laboratuvarına getir!
                    EnterRepairZoomMode(piece);
                }
            }

            RefreshAllStates();
        }

        private void HandleCandidateClicked(AnimatedRepairCandidate candidate)
        {
            if (isSimulating || showSynthesisModal || showRulebookModal || candidate == null) return;
            if (Time.frameCount == lastClickFrame) return;
            lastClickFrame = Time.frameCount;

            string sym = DNAHelper.GetNucleotideSymbol(candidate.Nucleotide);

            if (isRepairZoomMode)
            {
                if (zoomedGenome == null || zoomedGenome.Data == null) return;
                var gData = zoomedGenome.Data;

                if (gData.isDeformed)
                {
                    audioManager?.PlayMismatch();
                    repairFeedbackMessage = "☣️ GENOM YANDI! Yeni baz denemek için önce 'Genomu Arındır & Sıfırla' butonuna tıklayın.";
                    return;
                }

                // Eğer genom zaten onarıldıysa, seçili basamağı (selectedEditRungIndex) bu bazla mutasyona uğrat!
                if (gData.isRepaired || !gData.isBroken)
                {
                    int editIdx = gData.selectedEditRungIndex >= 0 ? gData.selectedEditRungIndex : 0;
                    string matchedGene = zoomedGenome.MutateRungWithNucleotide(editIdx, candidate.Nucleotide);
                    candidate.TriggerClickPulse();
                    audioManager?.PlayResonance();
                    audioManager?.PlaySnap();
                    if (!string.IsNullOrEmpty(matchedGene))
                    {
                        repairFeedbackMessage = $"🧬 MUTASYON BAŞARILI! Dizilim {matchedGene} ({gData.lacunaTraitTR}) genomuna dönüştü!";
                    }
                    else
                    {
                        repairFeedbackMessage = $"🔬 Çift #{editIdx + 1} [{sym} ↔ {DNAHelper.GetNucleotideSymbol(DNAHelper.ShapeToNucleotide(LacunaGeneticsDatabase.GetPartner(DNAHelper.NucleotideToShape(candidate.Nucleotide))))}] olarak güncellendi!";
                    }
                    RefreshAllStates();
                    return;
                }

                // Odak modunda oyuncu eksik (?) bazı onarıyor
                bool zoomSuccess = zoomedGenome.TryRepairWith(candidate.Nucleotide);
                if (zoomSuccess)
                {
                    candidate.TriggerClickPulse();
                    audioManager?.PlayResonance();
                    audioManager?.PlaySnap();
                    repairFeedbackMessage = $"✨ HARİKA! {gData.geneName} ({sym}) ile onarıldı! Şimdi yanlış gense dönüştürebilir veya sarmala yerleştirebilirsin.";
                }
                else
                {
                    candidate.TriggerErrorShake();
                    if (gData.isDeformed)
                    {
                        audioManager?.PlayBurnout();
                        repairFeedbackMessage = $"💥 AŞIRI YÜKLENME! 3 hakkın tükendi ve {gData.geneName} yandı! Kararsız bir hilkat garibesine dönüştü.";
                    }
                    else
                    {
                        audioManager?.PlayMismatch();
                        repairFeedbackMessage = $"⚠️ UYUMSUZ BAZ ({sym})! Kalan Onarım Hakkı: {gData.remainingRepairAttempts}/{gData.maxRepairAttempts}. {gData.repairHint}";
                    }
                }

                RefreshAllStates();
                return;
            }

            // Ana ekrandayken (Odak modu kapalıyken) alttaki kapsüle tıklandıysa:
            var targetGenome = selectedRepairGenome;
            if (targetGenome == null || targetGenome.IsEquipped || (targetGenome.Data != null && (targetGenome.Data.isRepaired || !targetGenome.Data.isBroken)))
            {
                var firstBroken = poolPieces.Find(p => p != null && !p.IsEquipped && p.Data != null && p.Data.isBroken && !p.Data.isRepaired);
                if (firstBroken != null)
                {
                    SelectGenomeForRepair(firstBroken);
                    targetGenome = firstBroken;
                }
                else
                {
                    repairFeedbackMessage = "🎉 Tüm 4 genom zaten onarıldı! Şimdi onarılan genomlara tıklayarak yukarıdaki ana puzzle'a yerleştirin.";
                    audioManager?.PlayResonance();
                    return;
                }
            }

            if (targetGenome != null && targetGenome.Data != null && targetGenome.Data.isDeformed)
            {
                audioManager?.PlayMismatch();
                repairFeedbackMessage = "☣️ GENOM YANDI! Üzerine tıklayıp inceleme modunda arındırabilir veya sarmala takabilirsin.";
                return;
            }

            // Onarımı dene
            bool success = targetGenome.TryRepairWith(candidate.Nucleotide);
            if (success)
            {
                candidate.TriggerClickPulse();
                audioManager?.PlayResonance();
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"✨ HARİKA! {targetGenome.Data.geneName} ({sym}) ile onarıldı! Üzerine tıklayarak ana DNA sarmalına yerleştirin.";

                var nextBroken = poolPieces.Find(p => p != null && !p.IsEquipped && p.Data != null && p.Data.isBroken && !p.Data.isRepaired && !p.Data.isDeformed);
                if (nextBroken != null)
                {
                    SelectGenomeForRepair(nextBroken);
                }
            }
            else
            {
                candidate.TriggerErrorShake();
                if (targetGenome.Data != null && targetGenome.Data.isDeformed)
                {
                    audioManager?.PlayBurnout();
                    repairFeedbackMessage = $"💥 AŞIRI YÜKLENME! {targetGenome.Data.geneName} yandı ve deforme oldu!";
                }
                else
                {
                    audioManager?.PlayMismatch();
                    int rem = targetGenome.Data != null ? targetGenome.Data.remainingRepairAttempts : 0;
                    repairFeedbackMessage = $"❌ EŞLEŞME HATALI ({sym})! Kalan Hak: {rem}/3. {targetGenome.Data.repairHint}";
                }
            }

            RefreshAllStates();
        }

        private void ReturnPieceToPool(AnimatedGenePiece piece)
        {
            if (piece == null) return;
            piece.CurrentSlotIndex = -1;
            int idx = poolPieces.IndexOf(piece);
            if (idx < 0) return;
            int count = poolPieces.Count;
            float spacingX = 2.95f;
            float startX = 1.35f - ((count - 1) * spacingX * 0.5f);
            Vector3 homePos = PuzzleToWorld(new Vector3(startX + idx * spacingX, -0.58f, 0f));
            piece.SetTargetScale(Vector3.one * 0.82f);
            piece.MoveTo(homePos, Quaternion.identity, 0.32f);
        }

        public void ClearStrand()
        {
            if (isSimulating) return;

            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (activeSlots[i].CurrentPiece != null)
                {
                    var piece = activeSlots[i].CurrentPiece;
                    activeSlots[i].RemovePiece();
                    manager.RemoveGene(i);
                    ReturnPieceToPool(piece);
                }
            }

            selectedSlot = 0;
            lastSynergyCount = 0;
            lastResonanceCount = 0;
            lastInstabilityCount = 0;
            if (audioManager != null) audioManager.PlaySnap();
            RefreshAllStates();
        }

        private void RefreshAllStates()
        {
            var bonds = manager.GetBondStatuses();
            int resCount = manager.CalculateResonanceCount();
            int instCount = manager.CalculateInstability();
            var stats = manager.CalculateTotalStats();

            for (int i = 0; i < resonanceBeams.Count; i++)
            {
                if (i < bonds.Count)
                {
                    var b = bonds[i];
                    resonanceBeams[i].SetState(b.isResonant, b.isMismatch);
                }
                else
                {
                    resonanceBeams[i].SetState(false, false);
                }
            }

            if (resCount > lastResonanceCount)
            {
                audioManager?.PlayResonance();
            }
            else if (instCount > lastInstabilityCount)
            {
                audioManager?.PlayMismatch();
            }
            lastResonanceCount = resCount;
            lastInstabilityCount = instCount;

            var synergies = manager.GetActiveSynergies();
            if (creatureEgg != null)
            {
                creatureEgg.UpdateFromStrand(manager.StrandGenes, stats, resCount, instCount, manager.CurrentPlanet, synergies);
            }

            if (synergies.Count > lastSynergyCount)
            {
                audioManager?.PlayShieldDeflection();
            }
            lastSynergyCount = synergies.Count;
        }

        public bool IsPointerOverHUD()
        {
            if (isDraggingPiece || isDraggingCandidate) return false;
            if (showSynthesisModal || showRulebookModal) return true;
            Vector3 mp = DNAInput.MousePosition;
            float guiY = Screen.height - mp.y;
            if (isRepairZoomMode)
            {
                if (guiY < 160f) return true; // Üst laboratuvar başlık, teşhis ve genom dönüştürme kartı alanı
                if (guiY > Screen.height - 62f) return true; // Alt aksiyon ve yerleştir butonu
                return false;
            }
            if (guiY < 96f) return true; // Üst gezegen & durum barı
            if (mp.x > Screen.width - 295f && guiY < 260f) return true; // Sağ üst test & kontrol butonları

            // Ana ekrandaki yüzen hızlı aksiyon butonları (🚀 Tak / 🔧 Düzenle / ↩️ Çıkar) kontrolü
            Camera cam = puzzleCamera != null ? puzzleCamera : Camera.main;
            if (cam != null && poolPieces != null)
            {
                for (int i = 0; i < poolPieces.Count; i++)
                {
                    var p = poolPieces[i];
                    if (p == null || p.Data == null || !p.gameObject.activeInHierarchy) continue;
                    if (p.IsEquipped)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(p.transform.position + new Vector3(0f, -0.88f, 0f));
                        if (sp.z > 0f)
                        {
                            Rect r = new Rect(sp.x - 68f, (Screen.height - sp.y), 138f, 24f);
                            if (r.Contains(new Vector2(mp.x, guiY))) return true;
                        }
                    }
                    else if (p.Data.isRepaired || !p.Data.isBroken)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(p.transform.position + new Vector3(0f, -0.78f, 0f));
                        if (sp.z > 0f)
                        {
                            Rect r = new Rect(sp.x - 74f, (Screen.height - sp.y), 152f, 24f);
                            if (r.Contains(new Vector2(mp.x, guiY))) return true;
                        }
                    }
                }
            }
            return false;
        }

        #region Sürükle-Bırak Geçmeli Yapboz Mekaniği & Yardımcıları
        private LineRenderer GetOrCreateSnapGuideLine()
        {
            if (snapGuideLine == null)
            {
                var go = new GameObject("SnapGuideLine");
                go.transform.SetParent(transform, false);
                snapGuideLine = go.AddComponent<LineRenderer>();
                snapGuideLine.positionCount = 2;
                snapGuideLine.startWidth = 0.08f;
                snapGuideLine.endWidth = 0.08f;
                Shader s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Sprites/Default");
                snapGuideLine.material = new Material(s);
                snapGuideLine.enabled = false;
            }
            return snapGuideLine;
        }

        private void ShowSnapGuide(Vector3 start, Vector3 end, bool complementary)
        {
            var lr = GetOrCreateSnapGuideLine();
            lr.enabled = true;
            Color col = complementary ? new Color(0.2f, 1f, 0.45f, 0.85f) : new Color(1f, 0.75f, 0.2f, 0.85f);
            lr.material.color = col;
            lr.SetPosition(0, start + new Vector3(0f, 0f, -0.2f));
            lr.SetPosition(1, end + new Vector3(0f, 0f, -0.2f));
        }

        private void HideSnapGuide()
        {
            if (snapGuideLine != null) snapGuideLine.enabled = false;
        }

        private Vector3 GetMouseWorldPosition(float targetZ)
        {
            Camera cam = puzzleCamera != null ? puzzleCamera : Camera.main;
            if (cam == null) return new Vector3(0f, 0f, targetZ);
            Vector3 mp = DNAInput.MousePosition;
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, targetZ));
            Ray ray = cam.ScreenPointToRay(mp);
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            return new Vector3(0f, 0f, targetZ);
        }

        private void ResetAllConnectorGlows()
        {
            if (activeSlots != null)
            {
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    var p = activeSlots[i]?.CurrentPiece;
                    if (p != null)
                    {
                        p.SetConnectorGlow(true, false, false);
                        p.SetConnectorGlow(false, false, false);
                    }
                }
            }
            if (draggedPiece != null)
            {
                draggedPiece.SetConnectorGlow(true, false, false);
                draggedPiece.SetConnectorGlow(false, false, false);
            }
        }

        private void DropPieceOnSlot(AnimatedGenePiece piece, int targetSlot)
        {
            if (piece == null || piece.Data == null || targetSlot < 0 || targetSlot >= activeSlots.Count) return;

            // Onarılmamış bozuk genom kontrolü
            if (!piece.Data.isRepaired && piece.Data.isBroken && !piece.Data.isDeformed)
            {
                piece.TriggerGlitchShake();
                audioManager?.PlayMismatch();
                repairFeedbackMessage = $"⚠️ {piece.Data.geneName} henüz onarılmadı! Önce eksik nükleotidi sürükleyip eşleştirin.";
                if (piece.CurrentSlotIndex >= 0)
                {
                    activeSlots[piece.CurrentSlotIndex].PlacePiece(piece);
                }
                else
                {
                    ReturnPieceToPool(piece);
                }
                return;
            }

            int prevSlot = piece.CurrentSlotIndex;
            var targetSlotObj = activeSlots[targetSlot];
            var existingPiece = targetSlotObj.CurrentPiece;

            if (existingPiece != null && existingPiece != piece)
            {
                if (prevSlot >= 0 && prevSlot < activeSlots.Count)
                {
                    // İki yuva arasında kusursuz yapboz yer değiştirmesi (SWAP)
                    activeSlots[prevSlot].RemovePiece();
                    targetSlotObj.RemovePiece();

                    activeSlots[prevSlot].PlacePiece(existingPiece);
                    manager.EquipGene(prevSlot, existingPiece.Data);

                    targetSlotObj.PlacePiece(piece);
                    manager.EquipGene(targetSlot, piece.Data);

                    audioManager?.PlaySnap();
                    repairFeedbackMessage = $"🔀 {piece.Data.geneName} ile {existingPiece.Data.geneName} yer değiştirdi!";
                }
                else
                {
                    // Tezgahtan dolu yuvaya sürüklendi: Eski parça tezgaha, yeni parça yuvaya kenetlenir
                    targetSlotObj.RemovePiece();
                    manager.RemoveGene(targetSlot);
                    ReturnPieceToPool(existingPiece);

                    targetSlotObj.PlacePiece(piece);
                    manager.EquipGene(targetSlot, piece.Data);

                    audioManager?.PlaySnap();
                    repairFeedbackMessage = $"🔗 {piece.Data.geneName} yuva #{targetSlot + 1}'e kenetlendi! Önceki genom tezgaha alındı.";
                }
            }
            else if (existingPiece == piece)
            {
                // Aynı yuvaya geri bırakıldı
                targetSlotObj.PlacePiece(piece);
            }
            else
            {
                // Boş yuvaya doğrudan kenetlenme
                if (prevSlot >= 0 && prevSlot < activeSlots.Count)
                {
                    activeSlots[prevSlot].RemovePiece();
                    manager.RemoveGene(prevSlot);
                }

                targetSlotObj.PlacePiece(piece);
                manager.EquipGene(targetSlot, piece.Data);

                audioManager?.PlaySnap();
                repairFeedbackMessage = $"🔗 {piece.Data.geneName} yuva #{targetSlot + 1}'e başarıyla kenetlendi!";
            }

            selectedSlot = targetSlot;
            RefreshAllStates();
        }

        private void HandleCandidateDroppedOnGenome(AnimatedRepairCandidate candidate, AnimatedGenePiece targetGenome)
        {
            if (candidate == null || targetGenome == null || targetGenome.Data == null) return;

            if (targetGenome.Data.isDeformed)
            {
                audioManager?.PlayMismatch();
                repairFeedbackMessage = "☣️ GENOM YANDI! Yeni baz denemek için önce 'Genomu Arındır & Sıfırla' butonuna tıklayın.";
                return;
            }

            if (targetGenome.Data.isRepaired || !targetGenome.Data.isBroken)
            {
                int editIdx = targetGenome.Data.selectedEditRungIndex >= 0 ? targetGenome.Data.selectedEditRungIndex : 0;
                string matchedGene = targetGenome.MutateRungWithNucleotide(editIdx, candidate.Nucleotide);
                candidate.TriggerClickPulse();
                audioManager?.PlayResonance();
                audioManager?.PlaySnap();
                string sym = DNAHelper.GetNucleotideSymbol(candidate.Nucleotide);
                if (!string.IsNullOrEmpty(matchedGene))
                {
                    repairFeedbackMessage = $"🧬 MUTASYON BAŞARILI! {matchedGene} ({targetGenome.Data.lacunaTraitTR}) oluşturuldu!";
                }
                else
                {
                    repairFeedbackMessage = $"🔬 Çift #{editIdx + 1} [{sym}] olarak güncellendi!";
                }
                RefreshAllStates();
                return;
            }

            bool success = targetGenome.TryRepairWith(candidate.Nucleotide);
            if (success)
            {
                candidate.TriggerClickPulse();
                audioManager?.PlayResonance();
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"✨ HARİKA GEÇME! {targetGenome.Data.geneName} ({candidate.Nucleotide}) ile başarıyla onarıldı! Şimdi sarmala sürükleyebilirsin.";

                var nextBroken = poolPieces.Find(p => p != null && !p.IsEquipped && p.Data != null && p.Data.isBroken && !p.Data.isRepaired && !p.Data.isDeformed);
                if (nextBroken != null)
                {
                    SelectGenomeForRepair(nextBroken);
                }
            }
            else
            {
                candidate.TriggerErrorShake();
                if (targetGenome.Data.isDeformed)
                {
                    audioManager?.PlayBurnout();
                    repairFeedbackMessage = $"💥 AŞIRI YÜKLENME! 3 hak tükendi ve {targetGenome.Data.geneName} yandı!";
                }
                else
                {
                    audioManager?.PlayMismatch();
                    repairFeedbackMessage = $"❌ UYUMSUZ BAZ! Kalan Onarım Hakkı: {targetGenome.Data.remainingRepairAttempts}/3. {targetGenome.Data.repairHint}";
                }
            }

            RefreshAllStates();
        }
        #endregion

        private void Update()
        {
            if (isLabMode)
            {
                CleanTablet3DModel();
            }

            if (isTerminalActive)
            {
                // Geçiş animasyonu ilerlemesi (Oklar ve otomatik genom geçişi)
                if (transitionProgress < 1f)
                {
                    transitionProgress = Mathf.MoveTowards(transitionProgress, 1f, Time.unscaledDeltaTime / 0.38f);
                    if (!hasFiredTransitionMidpoint && transitionProgress >= 0.5f)
                    {
                        hasFiredTransitionMidpoint = true;
                        transitionMidpointAction?.Invoke();
                        transitionMidpointAction = null;
                    }
                }

                // Genom tamamlanma kutlaması ve otomatik geçiş tetikleme
                if (slotCelebrationTimer > 0f)
                {
                    slotCelebrationTimer = Mathf.MoveTowards(slotCelebrationTimer, 0f, Time.unscaledDeltaTime);
                    if (slotCelebrationTimer <= 0f)
                    {
                        // Kutlama bitti -> Otomatik akıcı geçiş animasyonuyla sıradaki genoma geç
                        StartTransition(1, null);
                    }
                }

                // DNA heliks dönüşü ve eşleşme hız dalgası
                helixSpeedSurge = Mathf.MoveTowards(helixSpeedSurge, 0f, Time.unscaledDeltaTime * 1.8f);
                float currentHelixSpeed = pieceMotion == TabletPieceMotion.Idle ? 0.82f + helixSpeedSurge * 2.4f : 0f;
                helixPhase = Mathf.Repeat(helixPhase + Time.unscaledDeltaTime * currentHelixSpeed, Mathf.PI * 2);

                // Tablet açılış / yumuşak kapanış animasyonu
                if (!isTabletClosing)
                {
                    tabletOpenAnim = Mathf.MoveTowards(tabletOpenAnim, 1f, Time.unscaledDeltaTime * 3.4f);
                }
                else
                {
                    tabletOpenAnim = Mathf.MoveTowards(tabletOpenAnim, 0f, Time.unscaledDeltaTime * 5.0f);
                    if (tabletOpenAnim <= 0.001f)
                    {
                        isTabletClosing = false;
                        OnTerminalExit();
                        if (labStationController != null) labStationController.ReturnToBase();
                        return;
                    }
                }
            }

            EnsurePuzzleInitialized();

            if (isLabMode && !isTerminalActive)
            {
                UpdateLabHoloPreview();
                // Oyuncu Base görünümündeyken 'T' veya 'Space' ile tablete geçebilsin
                if (labStationController != null && labStationController.CurrentState == CameraStationController.State.AtBase)
                {
                    if (DNAInput.GetKeyDown(KeyCode.T) || DNAInput.GetKeyDown(KeyCode.Space))
                    {
                        if (tabletStation != null) labStationController.GoToStation(tabletStation);
                        else OnTerminalEnter();
                    }
                }
                return;
            }

            if (isLabMode && isTerminalActive)
            {
                // Yalnızca Escape tuşu pencereyi kapatır, sağ tık bulmaca içi taş çıkarmada kullanılır!
                if (DNAInput.GetKeyDown(KeyCode.Escape))
                {
                    if (showRulebookModal)
                    {
                        showRulebookModal = false;
                        return;
                    }
                    if (jigsawVictoryOpen)
                    {
                        jigsawVictoryOpen = false;
                        return;
                    }
                    if (tabletScreenMode == TabletScreenMode.Puzzle)
                    {
                        CancelTabletDrag();
                        StartTransition(-1, () => { tabletScreenMode = TabletScreenMode.Home; });
                        return;
                    }
                    StartTabletExit();
                    return;
                }
                return; // Tablet açıkken alttaki 3D fizik ışın testlerini çalıştırma!
            }

            if (isSimulating || showSynthesisModal || showRulebookModal) return;

            Camera cam = puzzleCamera != null ? puzzleCamera : Camera.main;
            if (cam == null) return;

            bool overHud = IsPointerOverHUD();
            Ray ray = cam.ScreenPointToRay(DNAInput.MousePosition);
            RaycastHit[] hits = overHud ? new RaycastHit[0] : Physics.RaycastAll(ray, 100f);

            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            AnimatedGenePiece hitPiece = null;
            AnimatedSlot hitSlot = null;
            AnimatedRepairCandidate hitCandidate = null;
            int hitChamberIdx = -1;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                if (hitChamberIdx < 0 && labScreen != null)
                {
                    hitChamberIdx = labScreen.GetChamberIndexFromCollider(hit.collider);
                }

                var cand = hit.collider.GetComponentInParent<AnimatedRepairCandidate>();
                if (cand != null && hitCandidate == null) hitCandidate = cand;

                var piece = hit.collider.GetComponentInParent<AnimatedGenePiece>();
                if (piece != null && hitPiece == null) hitPiece = piece;

                var slot = hit.collider.GetComponentInParent<AnimatedSlot>();
                if (slot != null && hitSlot == null) hitSlot = slot;
            }

            // Fare Hover Durumu
            if (!isDraggingPiece && !isDraggingCandidate)
            {
                if (hitCandidate != hoveredCandidate)
                {
                    if (hoveredCandidate != null) hoveredCandidate.SetHovered(false);
                    hoveredCandidate = hitCandidate;
                    if (hoveredCandidate != null) hoveredCandidate.SetHovered(true);
                }

                if (hitPiece != hoveredGenePiece)
                {
                    if (hoveredGenePiece != null) hoveredGenePiece.SetHovered(false);
                    hoveredGenePiece = hitPiece;
                    if (hoveredGenePiece != null) hoveredGenePiece.SetHovered(true);
                }
            }

            // 1. FARE BASILDIĞINDA (GetMouseButtonDown)
            if (!overHud && DNAInput.GetMouseButtonDown(0))
            {
                if (hitChamberIdx >= 0)
                {
                    audioManager?.PlayResonance();
                    if (hitChamberIdx != currentPlanetIdx)
                    {
                        LoadPlanet(hitChamberIdx);
                    }
                    else
                    {
                        labScreen?.TriggerClickJuice(hitChamberIdx);
                    }
                }
                else if (hitCandidate != null)
                {
                    dragCandidateCand = hitCandidate;
                    dragCandStartMousePos = DNAInput.MousePosition;
                    isDraggingCandidate = false;
                }
                else if (hitPiece != null)
                {
                    if (isRepairZoomMode)
                    {
                        // Odak / Laboratuvar modunda genom sürüklenip tezgaha düşürülmez veya küçültülmez!
                        hitPiece.TriggerTouchPulse();
                        HandlePieceClicked(hitPiece);
                    }
                    else
                    {
                        dragCandidatePiece = hitPiece;
                        dragPieceStartMousePos = DNAInput.MousePosition;
                        isDraggingPiece = false;
                        snapTargetSlotIndex = -1;
                    }
                }
                else if (hitSlot != null)
                {
                    HandleSlotClicked(hitSlot.SlotIndex);
                }
            }

            // 2. FARE BASILI TUTULDUĞUNDA (GetMouseButton - Sürükleme Mantığı)
            if (DNAInput.GetMouseButton(0))
            {
                // A) GENOM PARÇASI SÜRÜKLEME (Odak modunda devre dışı)
                if (dragCandidatePiece != null && !isRepairZoomMode)
                {
                    float mouseMovedDist = Vector2.Distance(DNAInput.MousePosition, dragPieceStartMousePos);
                    if (!isDraggingPiece && mouseMovedDist > 6f)
                    {
                        isDraggingPiece = true;
                        draggedPiece = dragCandidatePiece;
                        draggedPiece.IsBeingDragged = true;
                        draggedPiece.TriggerTouchPulse();
                        audioManager?.PlaySnap();

                        Vector3 mWorld = GetMouseWorldPosition(draggedPiece.transform.position.z);
                        dragPieceOffset = draggedPiece.transform.position - mWorld;
                    }

                    if (isDraggingPiece && draggedPiece != null)
                    {
                        Vector3 mWorld = GetMouseWorldPosition(0f);
                        Vector3 targetWorld = mWorld + dragPieceOffset;
                        targetWorld.z = -0.72f;

                        draggedPiece.transform.position = Vector3.Lerp(draggedPiece.transform.position, targetWorld, Time.deltaTime * 32f);

                        // Dinamik fiziksel yapboz eğilme açısı
                        float deltaX = DNAInput.MousePosition.x - dragPieceStartMousePos.x;
                        float tiltZ = Mathf.Clamp(-deltaX * 0.04f, -12f, 12f);
                        draggedPiece.transform.rotation = Quaternion.Lerp(draggedPiece.transform.rotation, Quaternion.Euler(0f, 0f, tiltZ), Time.deltaTime * 14f);
                        draggedPiece.transform.localScale = Vector3.Lerp(draggedPiece.transform.localScale, Vector3.one * 1.05f, Time.deltaTime * 16f);

                        // En yakın yuvaya manyetik kenetlenme kontrolü
                        int closestSlot = -1;
                        float minDist = 2.15f;
                        for (int i = 0; i < activeSlots.Count; i++)
                        {
                            if (activeSlots[i] == null) continue;
                            float d = Vector2.Distance(new Vector2(targetWorld.x, targetWorld.y), new Vector2(activeSlots[i].transform.position.x, activeSlots[i].transform.position.y));
                            if (d < minDist)
                            {
                                minDist = d;
                                closestSlot = i;
                            }
                        }

                        if (closestSlot != snapTargetSlotIndex)
                        {
                            if (snapTargetSlotIndex >= 0 && snapTargetSlotIndex < activeSlots.Count && activeSlots[snapTargetSlotIndex] != null)
                            {
                                activeSlots[snapTargetSlotIndex].SetSnapHighlight(false, false);
                            }
                            snapTargetSlotIndex = closestSlot;
                        }

                        if (snapTargetSlotIndex >= 0 && snapTargetSlotIndex < activeSlots.Count)
                        {
                            var targetSlot = activeSlots[snapTargetSlotIndex];
                            bool comp = false;
                            string matchDetails = "";

                            ResetAllConnectorGlows();

                            // Sol komşu ile kenetlenme kontrolü
                            if (snapTargetSlotIndex > 0)
                            {
                                var leftPiece = activeSlots[snapTargetSlotIndex - 1].CurrentPiece;
                                if (leftPiece != null && leftPiece != draggedPiece && leftPiece.Data != null && draggedPiece.Data != null)
                                {
                                    bool isMatch = DNAHelper.IsComplementary(leftPiece.Data.rightConnector, draggedPiece.Data.leftConnector);
                                    if (isMatch) comp = true;
                                    string lSym = DNAHelper.GetNucleotideSymbol(leftPiece.Data.rightConnector);
                                    string rSym = DNAHelper.GetNucleotideSymbol(draggedPiece.Data.leftConnector);
                                    matchDetails += $"Sol: [{lSym} ⟷ {rSym} {(isMatch ? "✓ EŞLENİK" : "✕")}] ";
                                    leftPiece.SetConnectorGlow(false, true, isMatch);
                                    draggedPiece.SetConnectorGlow(true, true, isMatch);
                                }
                            }
                            // Sağ komşu ile kenetlenme kontrolü
                            if (snapTargetSlotIndex < activeSlots.Count - 1)
                            {
                                var rightPiece = activeSlots[snapTargetSlotIndex + 1].CurrentPiece;
                                if (rightPiece != null && rightPiece != draggedPiece && rightPiece.Data != null && draggedPiece.Data != null)
                                {
                                    bool isMatch = DNAHelper.IsComplementary(draggedPiece.Data.rightConnector, rightPiece.Data.leftConnector);
                                    if (isMatch) comp = true;
                                    string lSym = DNAHelper.GetNucleotideSymbol(draggedPiece.Data.rightConnector);
                                    string rSym = DNAHelper.GetNucleotideSymbol(rightPiece.Data.leftConnector);
                                    matchDetails += $"Sağ: [{lSym} ⟷ {rSym} {(isMatch ? "✓ EŞLENİK" : "✕")}] ";
                                    rightPiece.SetConnectorGlow(true, true, isMatch);
                                    draggedPiece.SetConnectorGlow(false, true, isMatch);
                                }
                            }

                            isSnapComplementary = comp;
                            targetSlot.SetSnapHighlight(true, isSnapComplementary);
                            ShowSnapGuide(draggedPiece.transform.position, targetSlot.transform.position, isSnapComplementary);

                            var existingPiece = targetSlot.CurrentPiece;
                            if (existingPiece != null && existingPiece != draggedPiece)
                            {
                                dragInterlockMessage = $"🔀 Yuva #{snapTargetSlotIndex + 1} ({existingPiece.Data.geneName}) ile Değiş-Tokuş Yap! {matchDetails}";
                            }
                            else
                            {
                                dragInterlockMessage = $"🧩 Yuva #{snapTargetSlotIndex + 1}'e Kenetle! {matchDetails}";
                            }
                        }
                        else
                        {
                            ResetAllConnectorGlows();
                            HideSnapGuide();
                            dragInterlockMessage = "";
                        }
                    }
                }

                // B) NÜKLEOTİD ONARIM KAPSÜLÜ SÜRÜKLEME
                if (dragCandidateCand != null)
                {
                    float mouseMovedDist = Vector2.Distance(DNAInput.MousePosition, dragCandStartMousePos);
                    if (!isDraggingCandidate && mouseMovedDist > 6f)
                    {
                        isDraggingCandidate = true;
                        draggedCandidate = dragCandidateCand;
                        draggedCandidate.IsBeingDragged = true;
                        audioManager?.PlaySnap();
                    }

                    if (isDraggingCandidate && draggedCandidate != null)
                    {
                        Vector3 mWorld = GetMouseWorldPosition(0f);
                        draggedCandidate.transform.position = Vector3.Lerp(draggedCandidate.transform.position, mWorld + new Vector3(0f, 0f, -0.65f), Time.deltaTime * 32f);
                        draggedCandidate.transform.localScale = Vector3.Lerp(draggedCandidate.transform.localScale, Vector3.one * 1.30f, Time.deltaTime * 16f);

                        // Bozuk genom üzerine gelindi mi?
                        AnimatedGenePiece targetGene = null;
                        float minGeneDist = 2.4f;
                        if (isRepairZoomMode && zoomedGenome != null)
                        {
                            float d = Vector2.Distance(new Vector2(mWorld.x, mWorld.y), new Vector2(zoomedGenome.transform.position.x, zoomedGenome.transform.position.y));
                            if (d < 3.8f) targetGene = zoomedGenome;
                        }
                        else
                        {
                            foreach (var p in poolPieces)
                            {
                                if (p == null || !p.gameObject.activeInHierarchy || p.Data == null) continue;
                                float d = Vector2.Distance(new Vector2(mWorld.x, mWorld.y), new Vector2(p.transform.position.x, p.transform.position.y));
                                if (d < minGeneDist)
                                {
                                    minGeneDist = d;
                                    targetGene = p;
                                }
                            }
                        }

                        if (targetGene != null && targetGene.Data != null)
                        {
                            candidateDropTargetGenome = targetGene;
                            string sym = DNAHelper.GetNucleotideSymbol(draggedCandidate.Nucleotide);
                            dragInterlockMessage = $"🧬 [{sym}] Bazını {targetGene.Data.geneName} Genomuna Yerleştirmek İçin Bırakın!";
                            ShowSnapGuide(draggedCandidate.transform.position, targetGene.transform.position, true);
                        }
                        else
                        {
                            candidateDropTargetGenome = null;
                            HideSnapGuide();
                            dragInterlockMessage = "";
                        }
                    }
                }
            }

            // 3. FARE BIRAKILDIĞINDA (GetMouseButtonUp)
            if (DNAInput.GetMouseButtonUp(0))
            {
                // A) GENOM PARÇASI BIRAKILDI
                if (isDraggingPiece && draggedPiece != null)
                {
                    isDraggingPiece = false;
                    draggedPiece.IsBeingDragged = false;
                    draggedPiece.transform.rotation = Quaternion.identity;
                    ResetAllConnectorGlows();
                    HideSnapGuide();

                    if (snapTargetSlotIndex >= 0 && snapTargetSlotIndex < activeSlots.Count)
                    {
                        activeSlots[snapTargetSlotIndex].SetSnapHighlight(false, false);
                        DropPieceOnSlot(draggedPiece, snapTargetSlotIndex);
                    }
                    else
                    {
                        // Boş alana bırakıldı
                        if (draggedPiece.CurrentSlotIndex >= 0)
                        {
                            // Eğer sarmaldan aşağı tezgaha sürüklendiyse sarmaldan çıkar
                            float benchThreshold = PuzzleToWorld(new Vector3(0f, 0.2f, 0f)).y;
                            if (draggedPiece.transform.position.y < benchThreshold)
                            {
                                int sIdx = draggedPiece.CurrentSlotIndex;
                                activeSlots[sIdx].RemovePiece();
                                manager.RemoveGene(sIdx);
                                ReturnPieceToPool(draggedPiece);
                                audioManager?.PlaySnap();
                                repairFeedbackMessage = $"↩️ {draggedPiece.Data.geneName} sarmaldan çıkarıldı ve tezgaha alındı.";
                                RefreshAllStates();
                            }
                            else
                            {
                                activeSlots[draggedPiece.CurrentSlotIndex].PlacePiece(draggedPiece);
                            }
                        }
                        else
                        {
                            ReturnPieceToPool(draggedPiece);
                        }
                    }

                    draggedPiece = null;
                    dragCandidatePiece = null;
                    snapTargetSlotIndex = -1;
                    dragInterlockMessage = "";
                }
                else if (!isDraggingPiece && dragCandidatePiece != null)
                {
                    if (!isRepairZoomMode)
                    {
                        dragCandidatePiece.TriggerTouchPulse();
                        HandlePieceClicked(dragCandidatePiece);
                    }
                    dragCandidatePiece = null;
                }

                // B) NÜKLEOTİD ONARIM KAPSÜLÜ BIRAKILDI
                if (isDraggingCandidate && draggedCandidate != null)
                {
                    isDraggingCandidate = false;
                    draggedCandidate.IsBeingDragged = false;
                    HideSnapGuide();

                    if (candidateDropTargetGenome != null)
                    {
                        HandleCandidateDroppedOnGenome(draggedCandidate, candidateDropTargetGenome);
                    }

                    draggedCandidate = null;
                    dragCandidateCand = null;
                    candidateDropTargetGenome = null;
                    dragInterlockMessage = "";
                }
                else if (!isDraggingCandidate && dragCandidateCand != null)
                {
                    // Kısa temiz tıklama
                    HandleCandidateClicked(dragCandidateCand);
                    dragCandidateCand = null;
                }
            }
        }

        #region Taşmasız Kompakt Modern HUD Arayüzü
        private void OnGUI()
        {
            if (isLabMode)
            {
                if (tabletGlass != null)
                {
                    bool interactive = isTerminalActive && labStationController != null
                        && labStationController.CurrentState == CameraStationController.State.AtStation
                        && labStationController.ActiveStation == tabletStation;
                    RenderTabletScreen(interactive);
                }
                return;
            }

            float screenW = Screen.width;
            float screenH = Screen.height;

            // Tam Ekran Genom Biyo-Onarım & İnceleme Modu
            if (isRepairZoomMode && zoomedGenome != null)
            {
                RenderRepairZoomHUD(screenW, screenH);
                if (showRulebookModal)
                {
                    DrawLacunaRulebookModal(screenW, screenH);
                }
                return;
            }

            if (planets == null || planets.Count == 0)
                planets = DNAPuzzleSampleData.CreateSamplePlanets();

            PlanetData planet = manager != null ? manager.CurrentPlanet : null;
            var creatures = LacunaGeneticsDatabase.GetCreatures();
            currentCreatureIdx = Mathf.Clamp(currentCreatureIdx, 0, creatures.Count - 1);
            var curCreature = creatures[currentCreatureIdx];
            string curPlanetName = currentPlanetIdx == 1 ? "SNIBBLEX" : (currentPlanetIdx == 2 ? "OOMGARTH" : "FABAUP");
            var lacunaPlanetDef = LacunaGeneticsDatabase.GetPlanet(curPlanetName);

            int targetPlanetIdx = -1;
            int targetCreatureIdx = -1;
            bool triggerTestSim = false;
            bool triggerClearStrand = false;

            // 1. Üst Bar: Başlık, Canlı Seçici, Kural Kitabı & Gezegen Butonları
            GUILayout.BeginArea(new Rect(14, 8, screenW - 28, 38));
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.35f, 0.95f, 1f);
            GUILayout.Label("🧬 LACUNA GENETICS // 3D SARMAL", EditorBold(13));
            GUI.color = Color.white;

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.18f, 0.28f, 0.45f);
            if (GUILayout.Button("◀", EditorBold(11), GUILayout.Width(28), GUILayout.Height(26)))
            {
                targetCreatureIdx = (currentCreatureIdx - 1 + creatures.Count) % creatures.Count;
            }
            GUI.backgroundColor = new Color(0.12f, 0.22f, 0.36f);
            GUILayout.Box($"#{curCreature.creatureNumber}/10 {curCreature.creatureName} ({curCreature.targetPlanet})", EditorBold(11), GUILayout.Height(26), GUILayout.Width(175));
            GUI.backgroundColor = new Color(0.18f, 0.28f, 0.45f);
            if (GUILayout.Button("▶", EditorBold(11), GUILayout.Width(28), GUILayout.Height(26)))
            {
                targetCreatureIdx = (currentCreatureIdx + 1) % creatures.Count;
            }

            GUILayout.Space(6);
            GUI.backgroundColor = showRulebookModal ? new Color(0.2f, 0.9f, 0.55f) : new Color(0.95f, 0.68f, 0.15f);
            if (GUILayout.Button("📖 KURAL KİTABI (20 GENOM)", EditorBold(11), GUILayout.Height(26), GUILayout.Width(182)))
            {
                showRulebookModal = !showRulebookModal;
                audioManager?.PlaySnap();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            for (int i = 0; i < planets.Count; i++)
            {
                bool isSel = (i == currentPlanetIdx);
                GUI.backgroundColor = isSel ? new Color(0.15f, 0.78f, 0.98f) : new Color(0.22f, 0.28f, 0.38f);
                string shortPName = i == 0 ? "🔥 FABAUP" : (i == 1 ? "❄️ SNIBBLEX" : "☣️ OOMGARTH");

                if (GUILayout.Button(shortPName, EditorBold(11), GUILayout.Height(26), GUILayout.Width(112)))
                {
                    targetPlanetIdx = i;
                }
            }
            GUI.backgroundColor = Color.white;

            if (isLabMode && isTerminalActive)
            {
                GUILayout.Space(6);
                GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 0.95f);
                if (GUILayout.Button("↩️ ODAYA DÖN", EditorBold(11), GUILayout.Height(26), GUILayout.Width(115)))
                {
                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    ReturnToLabRoom();
                    return;
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 1.5. Orta Rehber, Gezegen Kuralları & Onarım Bildirim Alanı
            int repairedCount = 0;
            int deformedCount = 0;
            foreach (var p in poolPieces)
            {
                if (p != null && p.Data != null)
                {
                    if (p.Data.isDeformed) deformedCount++;
                    else if (p.Data.isRepaired || !p.Data.isBroken) repairedCount++;
                }
            }

            float bannerW = Mathf.Min(760f, screenW - 310f);
            float bannerX = 18f;
            GUILayout.BeginArea(new Rect(bannerX, 44, bannerW, 56));
            GUILayout.BeginVertical();

            string stepTitle = (repairedCount < 4)
                ? (deformedCount > 0 
                    ? $"🛠️ 1. ADIM: GENOMLARI ONAR ({repairedCount}/4 Hazır, ☣️ {deformedCount} Genom Yandı!) — Eşlenik: ●↔■ ve ▲↔◆"
                    : $"🛠️ 1. ADIM: BOZUK GENOMLARI ONAR ({repairedCount}/4 Hazır) — Eşlenik Kuralı: ●↔■  ve  ▲↔◆")
                : "🎉 2. ADIM: GENOMLARI GEZEGEN KURALINA GÖRE DÜZENLE VE SARMALA YERLEŞTİR!";
            Color stepCol = (deformedCount > 0) ? new Color(1f, 0.35f, 0.25f) :
                            (repairedCount < 4) ? new Color(1f, 0.78f, 0.25f) : new Color(0.3f, 1f, 0.55f);

            GUI.color = stepCol;
            GUILayout.Label(stepTitle, EditorBold(11));

            if (lacunaPlanetDef != null)
            {
                GUI.color = new Color(0.45f, 0.92f, 1f);
                GUILayout.Label($"🌍 {lacunaPlanetDef.planetName} KURALI: Deri={lacunaPlanetDef.skinRuleTR} • Boy={lacunaPlanetDef.heightRuleTR} • Göz={lacunaPlanetDef.eyesRuleTR} • Uzuv={lacunaPlanetDef.limbsRuleTR}", EditorBold(10));
            }

            GUI.color = new Color(0.88f, 0.95f, 1f);
            GUILayout.Label(repairFeedbackMessage, EditorLabel(11));
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 2. Sağ Üst Panel: Test & Gönder ve Sarmalı Boşalt Butonları
            if (planet != null && manager != null)
            {
                float rightW = 270;
                GUILayout.BeginArea(new Rect(screenW - rightW - 18, 50, rightW, 260));
                
                // Test Butonu
                GUI.backgroundColor = isSimulating ? Color.yellow : new Color(0.15f, 0.85f, 0.45f);
                string btnTxt = isSimulating ? "⏳ İNİŞ TESTİ SÜRÜYOR..." : "🚀 CANLIYI TEST ET & GÖNDER";

                if (GUILayout.Button(btnTxt, GUILayout.Height(36)) && !isSimulating)
                {
                    triggerTestSim = true;
                }
                GUI.backgroundColor = Color.white;

                // Sarmalı Boşalt Butonu
                GUILayout.Space(6);
                GUI.backgroundColor = new Color(0.32f, 0.44f, 0.60f);
                if (GUILayout.Button("🔄 SARMALI BOŞALT", GUILayout.Height(26)) && !isSimulating)
                {
                    triggerClearStrand = true;
                }
                GUI.backgroundColor = Color.white;

                // Canlı Biyo-Model Rapor Kartı Butonu
                bool hasValidSynth = lastSynthesizedCreature != null && lastSynthesizedCreature.isValid && lastSynthesizedCreature.phenotype != null;
                if (hasValidSynth && !isSimulating)
                {
                    GUILayout.Space(6);
                    GUI.backgroundColor = new Color(0.2f, 0.75f, 1f);
                    if (GUILayout.Button("📋 CANLI BİYO-MODEL RAPORU", GUILayout.Height(26)))
                    {
                        showSynthesisModal = true;
                        teamDispatchMessage = "";
                    }
                    GUI.backgroundColor = Color.white;
                }

                // 4 Slotun Gezegen Uygunluk Özeti
                if (lacunaPlanetDef != null)
                {
                    GUILayout.Space(6);
                    GUI.color = new Color(0.35f, 0.95f, 1f);
                    GUILayout.Label($"🧬 {lacunaPlanetDef.planetName} SLOT KONTROLÜ:", EditorBold(11));
                    for (int s = 0; s < poolPieces.Count; s++)
                    {
                        var gp = poolPieces[s]?.Data;
                        if (gp == null) continue;
                        bool ok = lacunaPlanetDef.IsGeneAcceptable(gp.lacunaSlot, gp.geneName);
                        bool rep = gp.isRepaired || !gp.isBroken;
                        GUI.color = !rep ? new Color(1f, 0.75f, 0.25f) : (ok ? new Color(0.25f, 1f, 0.5f) : new Color(1f, 0.35f, 0.35f));
                        string stateStr = !rep ? "🛠️ Onarılmalı" : (ok ? "✔ Uygun" : "❌ Değiştirilmeli!");
                        GUILayout.Label($"• Slot {gp.lacunaSlot}: {gp.geneName} ({stateStr})", EditorBold(10));
                    }
                    GUI.color = Color.white;
                }

                if (isSimulating)
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label(simulationLog);
                    GUI.color = Color.white;
                }

                if (lastEvaluation != null && !isSimulating)
                {
                    GUI.color = lastEvaluation.isSuccess ? Color.green : Color.red;
                    string stars = lastEvaluation.isSuccess ? $" [{ProgressionManager.FormatStarString(lastEvaluation.starRating)}]" : "";
                    GUILayout.Label(lastEvaluation.statusTitle + stars, EditorBold(12));
                    GUI.color = Color.white;
                    GUILayout.Label(lastEvaluation.detailedMessage);
                }
                GUILayout.EndArea();
            }

            // 3. Alt Bilgilendirme ve Rehber Barı (Sade, Ferah, Tek Alan)
            float bottomW = Mathf.Min(860f, screenW - 40f);
            GUILayout.BeginArea(new Rect((screenW - bottomW) * 0.5f, screenH - 32f, bottomW, 26f));
            if (hoveredGenePiece != null && hoveredGenePiece.Data != null)
            {
                var g = hoveredGenePiece.Data;
                if (g.isDeformed)
                {
                    GUI.color = new Color(1f, 0.3f, 0.3f);
                    GUILayout.Label($"☣️ [DEFORME/YANMIŞ] {g.geneName}: 3 Yanlış deneme sonucu aşırı yüklendi! Sarmala takılırsa Kaotik Hilkat Garibesi oluşur.", EditorBold(11));
                }
                else
                {
                    GUI.color = new Color(1f, 0.95f, 0.4f);
                    string lSym = DNAHelper.GetNucleotideSymbol(g.leftConnector);
                    string rSym = DNAHelper.GetNucleotideSymbol(g.rightConnector);
                    GUILayout.Label($"🔬 {g.geneName} (SLOT {g.lacunaSlot}): {g.lacunaTraitTR} | Dizilim: {g.GetLacunaTopSequenceString()} [Uçlar: {lSym}⟷{rSym}]", EditorBold(11));
                }
            }
            else
            {
                GUI.color = new Color(0.40f, 0.88f, 1f);
                GUILayout.Label($"💡 İPUCU ({curCreature.creatureName}): \"{curCreature.landlordClueTR}\"  |  Eşlenik: ●↔■ ve ▲↔◆", EditorBold(11));
            }
            GUI.color = Color.white;
            GUILayout.EndArea();

            // 3.5. Ana Ekranda Onarılmış / Sarmaldaki Genomlar İçin Hızlı "Sarmala Tak" ve "Tekrar Düzenle" Butonları
            AnimatedGenePiece quickEquipPiece = null;
            AnimatedGenePiece quickEditPiece = null;
            AnimatedGenePiece quickUnequipPiece = null;
            Camera cam = puzzleCamera != null ? puzzleCamera : Camera.main;
            if (cam != null && !isSimulating && !showSynthesisModal && !showRulebookModal)
            {
                for (int i = 0; i < poolPieces.Count; i++)
                {
                    var p = poolPieces[i];
                    if (p == null || p.Data == null || !p.gameObject.activeInHierarchy) continue;

                    if (p.IsEquipped)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(p.transform.position + new Vector3(0f, -0.88f, 0f));
                        if (sp.z > 0f)
                        {
                            float gy = screenH - sp.y;
                            float gx = sp.x - 68f;
                            GUI.backgroundColor = new Color(0.2f, 0.75f, 1f, 0.92f);
                            if (GUI.Button(new Rect(gx, gy, 74f, 22f), "🔧 Düzenle", EditorBold(10)))
                            {
                                quickEditPiece = p;
                            }
                            GUI.backgroundColor = new Color(0.45f, 0.55f, 0.7f, 0.92f);
                            if (GUI.Button(new Rect(gx + 78f, gy, 58f, 22f), "↩️ Çıkar", EditorBold(10)))
                            {
                                quickUnequipPiece = p;
                            }
                            GUI.backgroundColor = Color.white;
                        }
                    }
                    else if (p.Data.isRepaired || !p.Data.isBroken)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(p.transform.position + new Vector3(0f, -0.78f, 0f));
                        if (sp.z > 0f)
                        {
                            float gy = screenH - sp.y;
                            float gx = sp.x - 74f;
                            GUI.backgroundColor = new Color(0.15f, 0.88f, 0.45f, 0.95f);
                            if (GUI.Button(new Rect(gx, gy, 72f, 22f), "🚀 Tak", EditorBold(10)))
                            {
                                quickEquipPiece = p;
                            }
                            GUI.backgroundColor = new Color(0.2f, 0.75f, 1f, 0.95f);
                            if (GUI.Button(new Rect(gx + 76f, gy, 74f, 22f), "🔧 Düzenle", EditorBold(10)))
                            {
                                quickEditPiece = p;
                            }
                            GUI.backgroundColor = Color.white;
                        }
                    }
                }
            }

            if (quickEditPiece != null)
            {
                lastClickFrame = Time.frameCount;
                EnterRepairZoomMode(quickEditPiece);
            }
            else if (quickEquipPiece != null)
            {
                lastClickFrame = Time.frameCount;
                EquipRepairedGenomeToMainStrand(quickEquipPiece);
                RefreshAllStates();
            }
            else if (quickUnequipPiece != null)
            {
                lastClickFrame = Time.frameCount;
                int sIdx = quickUnequipPiece.CurrentSlotIndex;
                if (sIdx >= 0 && sIdx < activeSlots.Count && activeSlots[sIdx] != null)
                {
                    activeSlots[sIdx].RemovePiece();
                    manager.RemoveGene(sIdx);
                }
                ReturnPieceToPool(quickUnequipPiece);
                audioManager?.PlaySnap();
                RefreshAllStates();
            }

            // Buton eylemlerini GUILayout alanları tamamen bittikten sonra güvenle çalıştır
            if (targetCreatureIdx >= 0)
            {
                audioManager?.PlaySnap();
                LoadCreature(targetCreatureIdx);
            }
            else if (targetPlanetIdx >= 0)
            {
                if (targetPlanetIdx != currentPlanetIdx)
                {
                    LoadPlanet(targetPlanetIdx);
                }
                else
                {
                    audioManager?.PlayResonance();
                    labScreen?.TriggerClickJuice(targetPlanetIdx);
                }
            }

            if (triggerTestSim && planet != null)
            {
                // Lacuna Genetics Gezegen Kuralları + 3D Kuluçka Odası Canlı Sentezi
                string zGene = poolPieces.Count > 0 && poolPieces[0]?.Data != null ? poolPieces[0].Data.geneName : curCreature.startGeneZ;
                string vGene = poolPieces.Count > 1 && poolPieces[1]?.Data != null ? poolPieces[1].Data.geneName : curCreature.startGeneV;
                string kGene = poolPieces.Count > 2 && poolPieces[2]?.Data != null ? poolPieces[2].Data.geneName : curCreature.startGeneK;
                string pGene = poolPieces.Count > 3 && poolPieces[3]?.Data != null ? poolPieces[3].Data.geneName : curCreature.startGeneP;

                bool allRepaired = true;
                foreach (var pp in poolPieces)
                {
                    if (pp != null && pp.Data != null && ((pp.Data.isBroken && !pp.Data.isRepaired) || pp.Data.isDeformed))
                    {
                        allRepaired = false;
                    }
                }

                bool zOk = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(LacunaSlotType.Z, zGene);
                bool vOk = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(LacunaSlotType.V, vGene);
                bool kOk = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(LacunaSlotType.K, kGene);
                bool pOk = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(LacunaSlotType.P, pGene);
                bool lacunaSuccess = allRepaired && zOk && vOk && kOk && pOk;

                SpawnLacunaCreatureInLabChamber(curCreature.creatureName, curPlanetName, zGene, vGene, kGene, pGene);

                lastEvaluation = null;
                simulator.StartSimulation(manager);
                if (dropCinematic != null && creatureEgg != null)
                {
                    dropCinematic.PlayDropSequence(planet, currentPlanetIdx, simulator, creatureEgg.transform.position);
                }
            }

            if (triggerClearStrand)
            {
                ClearStrand();
            }

            // Sürükle-Bırak Manyetik Geçmeli Yapboz Bilgilendirme Kartı (Interlocking HUD Banner)
            if (!string.IsNullOrEmpty(dragInterlockMessage))
            {
                float dragCardW = Mathf.Min(740f, screenW - 40f);
                float dragCardH = 34f;
                float dragCardX = (screenW - dragCardW) * 0.5f;
                float dragCardY = screenH - 128f;

                GUI.color = isSnapComplementary ? new Color(0.04f, 0.22f, 0.14f, 0.95f) : new Color(0.08f, 0.14f, 0.24f, 0.95f);
                GUI.Box(new Rect(dragCardX, dragCardY, dragCardW, dragCardH), GUIContent.none, EditorBox());

                GUI.color = isSnapComplementary ? new Color(0.25f, 1f, 0.5f) : new Color(1f, 0.88f, 0.35f);
                GUI.Label(new Rect(dragCardX + 12, dragCardY + 6, dragCardW - 24, 24), dragInterlockMessage, EditorBold(12));
                GUI.color = Color.white;
            }

            // Canlı Sentez ve Modelleme Sonuç Modalı
            if (showSynthesisModal && lastSynthesizedCreature != null && lastSynthesizedCreature.isValid && lastSynthesizedCreature.phenotype != null)
            {
                DrawCreatureSynthesisModal(screenW, screenH);
            }

            // Lacuna Genetics 20 Genom Kural Kitabı Modalı
            if (showRulebookModal)
            {
                DrawLacunaRulebookModal(screenW, screenH);
            }
        }

        private void DrawCreatureSynthesisModal(float screenW, float screenH)
        {
            if (lastSynthesizedCreature == null || !lastSynthesizedCreature.isValid || lastSynthesizedCreature.phenotype == null) return;

            float modalW = Mathf.Min(640f, screenW - 36f);
            float modalH = 490f;
            float modalX = (screenW - modalW) * 0.5f;
            float modalY = (screenH - modalH) * 0.5f;

            bool triggerDispatch = false;
            bool triggerClose = false;

            // Arka Plan Yarı Saydam Koyu Panel
            GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none);

            GUILayout.BeginArea(new Rect(modalX + 16, modalY + 14, modalW - 32, modalH - 28));

            // Başlık
            GUI.color = new Color(0.35f, 0.95f, 1f);
            GUILayout.Label("🧬 BİYO-SENTEZ VE CANLI MODELLEME RAPORU", EditorBold(14));
            GUI.color = Color.white;

            var p = lastSynthesizedCreature.phenotype;
            string icon = (p.bodyType == CreatureBodyType.Reptilian) ? "🦎" :
                          (p.bodyType == CreatureBodyType.CryoAvian) ? "🦅" :
                          (p.bodyType == CreatureBodyType.Insectoid) ? "🦂" :
                          (p.bodyType == CreatureBodyType.Leviathan) ? "🐋" :
                          (p.bodyType == CreatureBodyType.CosmicApex) ? "✨" : "🧬";

            GUILayout.Space(6);
            GUI.color = lastSynthesizedCreature.isSurvived ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
            string pName = lastSynthesizedCreature.targetPlanet != null ? lastSynthesizedCreature.targetPlanet.planetName : "Bilinmeyen";
            string statusTag = lastSynthesizedCreature.isSurvived ? 
                $"★ {pName} GEZEGENİNDE HAYATTA KALDI! ★" : 
                $"✖ {pName} GEZEGENİNDE ELENDİ!";
            GUILayout.Label($"{icon} {lastSynthesizedCreature.speciesName} - {statusTag}", EditorBold(12));
            GUI.color = Color.white;

            GUILayout.Space(6);
            GUILayout.BeginVertical("box");
            GUI.color = new Color(1f, 0.88f, 0.25f);
            GUILayout.Label("🎮 ARKADAŞLARININ SİSTEMİ İÇİN 3D MODEL VE OYNANIŞ BİLGİSİ:", EditorBold(11));
            GUI.color = Color.white;

            GUILayout.Label($"• Morfoloji: {p.bodyType} | Boyut: x{p.scaleMultiplier:F2} ({p.heightMeters:F1}m Boy, {p.weightKg:F0}kg Kütle)");
            GUILayout.Label($"• Kollar & Uzuvlar: {p.limbCount} Uzuv, Kol Uzanımı: {p.armLength:F1}m {(p.hasClaws ? $"(⚔️ {p.clawLength:F1}m Pençe)" : "")}");
            GUILayout.Label($"• Çene & Dişler: {p.jawType} (Diş Boyutu: x{p.toothSize:F1} {(p.hasSaberFangs ? "[Kılıç Diş]" : "")}) | Isırık: {p.biteForceNewton:F0} N");
            GUILayout.Label($"• Kükreme & Ses: {p.roarType} ({p.roarVolumeDb} dB, Perde: {p.roarPitch:F2}x)");
            GUILayout.Label($"• Kuyruk & Gözler: {p.tailType} ({p.tailLength:F1}m) | {p.eyeCount} Göz ({p.visionType})");
            GUILayout.Label($"• Deri & Doku: {p.skinTexture} (Işıma Deseni: {p.glowPattern})");

            string hexSkin = ColorUtility.ToHtmlStringRGB(p.primarySkinColor);
            string hexBelly = ColorUtility.ToHtmlStringRGB(p.underbellyColor);
            string hexGlow = ColorUtility.ToHtmlStringRGB(p.glowColor);
            GUILayout.Label($"• Renk Paleti: Gövde #{hexSkin} | Karın Altı #{hexBelly} | Biyo-Işık #{hexGlow}");
            
            // Aktif Organlar
            var organList = new List<string>();
            if (p.hasArmorPlates) organList.Add($"🛡️ Koruyucu Zırh (%{Mathf.RoundToInt(p.armorIntensity * 100)})");
            if (p.hasGillsOrFins) organList.Add("🫁 Biyo-Solungaç Kanatları");
            if (p.hasSpinalRidge) organList.Add("🦴 Omurga İskeleti");
            if (p.hasHorns) organList.Add($"⚔️ Boynuz (x{p.hornScale:F1})");
            if (p.hasBioluminescence) organList.Add("💡 Biyolüminesans");
            if (p.hasCosmicRings) organList.Add("✨ Kozmik Helezon");
            if (organList.Count == 0) organList.Add("Proto-Hücre (Temel)");

            GUILayout.Label($"• Aktif Organlar: {string.Join("  ", organList)}");
            GUILayout.Label($"• Oyun İçi Statlar: ❤️ HP: {Mathf.RoundToInt(p.maxHealth)}  |  🏃 Hız: {p.movementSpeed:F1}  |  ⚔️ Saldırı: {p.attackDamage:F0}  |  🛡️ Savunma: {p.defenseRating:F0}");
            GUILayout.EndVertical();

            GUILayout.Space(6);
            if (!string.IsNullOrEmpty(lastSynthesizedCreature.survivalSummary))
            {
                GUI.color = new Color(0.85f, 0.90f, 1f);
                GUILayout.Label($"📋 Teşhis: {lastSynthesizedCreature.survivalSummary}", EditorBold(10));
                GUI.color = Color.white;
            }

            if (!string.IsNullOrEmpty(teamDispatchMessage))
            {
                GUILayout.Space(4);
                GUI.color = new Color(0.2f, 1f, 0.5f);
                GUILayout.Label(teamDispatchMessage, EditorBold(11));
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.15f, 0.85f, 0.45f);
            if (GUILayout.Button("🚀 CANLIYI OYUNA / ARKADAŞLARINA AKTAR", GUILayout.Height(34)))
            {
                triggerDispatch = true;
            }

            if (isLabMode && chamberStation != null)
            {
                GUI.backgroundColor = new Color(0.2f, 0.95f, 0.6f);
                if (GUILayout.Button("🔬 KULUÇKA ODASINA BAK", GUILayout.Height(34)))
                {
                    ViewInLabChamber();
                }
            }

            GUI.backgroundColor = new Color(0.35f, 0.45f, 0.6f);
            if (GUILayout.Button("🔬 TAMAM (Puzzleye Dön)", GUILayout.Height(34)))
            {
                triggerClose = true;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (triggerDispatch)
            {
                CreatureSynthesisBridge.BuildFromPuzzle(
                    manager.StrandGenes,
                    manager.CalculateTotalStats(),
                    manager.GetActiveSynergies(),
                    manager.CurrentPlanet,
                    lastEvaluation
                );
                teamDispatchMessage = "✔ Canlı verisi 'CreatureSynthesisBridge' üzerinden arkadaşlarına ve diğer sistemlere iletildi!";
                audioManager?.PlayVictory();
            }

            if (triggerClose)
            {
                showSynthesisModal = false;
                teamDispatchMessage = "";
            }
        }

        private void RenderRepairZoomHUD(float screenW, float screenH)
        {
            if (zoomedGenome == null || zoomedGenome.Data == null) return;
            var data = zoomedGenome.Data;

            string curPlanetName = currentPlanetIdx == 1 ? "SNIBBLEX" : (currentPlanetIdx == 2 ? "OOMGARTH" : "FABAUP");
            var lacunaPlanetDef = LacunaGeneticsDatabase.GetPlanet(curPlanetName);
            bool isPlanetAcceptable = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(data.lacunaSlot, data.geneName);

            bool triggerClose = false;
            bool triggerEquip = false;
            bool triggerResetRepair = false;
            bool triggerPurify = false;
            AnimatedGenePiece nextBrokenToZoom = null;
            LacunaGeneDefinition targetTransformGene = null;

            // 1. Üst Laboratuvar Başlık Çubuğu + Tüm 4 Genom İçin Hızlı Geçiş Sekmeleri
            GUILayout.BeginArea(new Rect(18, 8, screenW - 36, 38));
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.35f, 0.9f, 1f);
            GUILayout.Label($"🔬 3D GENOM DÜZENLEME // SLOT {data.lacunaSlot}", EditorBold(13));
            GUI.color = Color.white;

            GUILayout.Space(8);
            for (int i = 0; i < poolPieces.Count; i++)
            {
                var p = poolPieces[i];
                if (p == null || p.Data == null) continue;
                bool isCur = (p == zoomedGenome);
                bool isRep = p.Data.isRepaired || !p.Data.isBroken;
                bool isDef = p.Data.isDeformed;
                bool slotOk = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(p.Data.lacunaSlot, p.Data.geneName);

                string badge = isDef ? "☣️" : (!isRep ? "🛠️" : (slotOk ? "✅" : "⚠️"));
                Color tabCol = isCur ? new Color(0.2f, 0.8f, 1f) : 
                               (isDef ? new Color(0.75f, 0.15f, 0.25f) : 
                               (!isRep ? new Color(0.55f, 0.35f, 0.18f) :
                               (slotOk ? new Color(0.18f, 0.58f, 0.35f) : new Color(0.75f, 0.45f, 0.15f))));
                GUI.backgroundColor = tabCol;
                if (GUILayout.Button($"{badge} Slot {p.Data.lacunaSlot}: {p.Data.geneName}", EditorBold(11), GUILayout.Height(26), GUILayout.Width(142)))
                {
                    if (!isCur) nextBrokenToZoom = p;
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6);
            GUI.backgroundColor = showRulebookModal ? new Color(0.2f, 0.9f, 0.55f) : new Color(0.95f, 0.68f, 0.15f);
            if (GUILayout.Button("📖 KURAL KİTABI", EditorBold(11), GUILayout.Height(26), GUILayout.Width(135)))
            {
                showRulebookModal = !showRulebookModal;
                audioManager?.PlaySnap();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("✕ KAPAT", EditorBold(11), GUILayout.Height(28), GUILayout.Width(95)))
            {
                triggerClose = true;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 2. Teşhis, Dizilim ve Gezegen Uygunluk Kartı (Üst Orta)
            float cardW = Mathf.Min(820, screenW - 36);
            float cardX = (screenW - cardW) * 0.5f;
            GUILayout.BeginArea(new Rect(cardX, 48, cardW, 60), "box");
            GUILayout.BeginVertical();

            string seqStr = $"Üst: [{data.GetLacunaTopSequenceString()}]  •  Alt: [{data.GetLacunaBottomSequenceString()}]";

            if (data.isDeformed)
            {
                GUI.color = new Color(1f, 0.25f, 0.25f);
                GUILayout.Label($"☣️ AŞIRI YÜKLENME HASARI: {data.geneName} (SLOT {data.lacunaSlot}) BİYOLOJİK OLARAK YANDI!", EditorBold(12));
                GUI.color = new Color(1f, 0.85f, 0.45f);
                GUILayout.Label($"Kalan Biyo-Bütünlük: 🖤🖤🖤 (0/3 Hak)  |  ⚠️ Yeni baz denemek için alttan 'Genomu Arındır & Sıfırla' butonuna tıklayın.", EditorLabel(11));
            }
            else if (data.isRepaired || !data.isBroken)
            {
                GUI.color = isPlanetAcceptable ? new Color(0.25f, 1f, 0.5f) : new Color(1f, 0.72f, 0.2f);
                string planetStatus = isPlanetAcceptable
                    ? $"✔ {curPlanetName} GEZEGENİ İÇİN UYGUN!"
                    : $"⚠️ {curPlanetName} İÇİN UYGUNSUZ GEN! Aşağıdaki Slot {data.lacunaSlot} genlerinden uygun olana dönüştürün!";
                GUILayout.Label($"✅ EŞLENİK BÜTÜNLÜĞÜ TAMAM: {data.geneName} ({data.lacunaTraitTR})  |  {planetStatus}", EditorBold(11));
                GUI.color = new Color(0.85f, 0.95f, 1f);
                GUILayout.Label($"Dizilim: {seqStr}  |  Eşlenik Kuralı: ●↔■ ve ▲↔◆", EditorLabel(11));
            }
            else
            {
                GUI.color = new Color(1f, 0.45f, 0.2f);
                GUILayout.Label($"⚠️ HASAR TESPİTİ: {data.defectDescription}  |  {data.repairHint}", EditorBold(11));
                GUI.color = new Color(0.4f, 0.9f, 1f);
                string hearts = "";
                for (int h = 0; h < data.maxRepairAttempts; h++)
                {
                    hearts += (h < data.remainingRepairAttempts) ? "❤️" : "🖤";
                }
                GUILayout.Label($"🛡️ BİYO-BÜTÜNLÜK: {hearts} ({data.remainingRepairAttempts}/3 Hak)  |  Dizilim: {seqStr}", EditorBold(11));
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 2.5. SLOT GENOM DÖNÜŞTÜRÜCÜ BAR (Kılavuzdaki 5 Gen Arasında Dönüştürme / Yanlış Geni Düzeltme)
            var slotGenes = LacunaGeneticsDatabase.GetGenesBySlot(data.lacunaSlot);
            var curGeneDef = LacunaGeneticsDatabase.GetGene(data.geneName);
            if (slotGenes != null && slotGenes.Count > 0)
            {
                GUILayout.BeginArea(new Rect(cardX, 110, cardW, 44), "box");
                GUILayout.BeginHorizontal();

                GUI.color = new Color(1f, 0.88f, 0.3f);
                GUILayout.Label($"🧬 SLOT {data.lacunaSlot} GENLERİ:", EditorBold(10), GUILayout.Width(105));
                GUI.color = Color.white;

                bool canMutate = (data.isRepaired || !data.isBroken) && !data.isDeformed;

                for (int gIdx = 0; gIdx < slotGenes.Count; gIdx++)
                {
                    var sg = slotGenes[gIdx];
                    bool isCurrent = (sg.geneName == data.geneName);
                    bool isAcceptable = lacunaPlanetDef == null || lacunaPlanetDef.IsGeneAcceptable(sg.slot, sg.geneName);
                    int diff = (curGeneDef != null) ? LacunaGeneticsDatabase.CountPairDifferences(curGeneDef, sg) : 1;

                    if (isCurrent)
                        GUI.backgroundColor = new Color(0.18f, 0.78f, 1f);
                    else if (isAcceptable)
                        GUI.backgroundColor = new Color(0.18f, 0.65f, 0.38f);
                    else
                        GUI.backgroundColor = new Color(0.32f, 0.36f, 0.45f);

                    string diffTag = isCurrent ? "Aktif" : $"{diff}ç fark";
                    string okMark = isAcceptable ? "✔" : "";
                    string btnLabel = $"{sg.geneName}{okMark} ({diffTag})";

                    if (GUILayout.Button(btnLabel, EditorBold(10), GUILayout.Height(28)))
                    {
                        if (!isCurrent)
                        {
                            targetTransformGene = sg;
                        }
                    }
                }
                GUI.backgroundColor = Color.white;

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            // 2.6. BAZ ÇİFTLERİ SEÇİCİ BAR (İstediğin Çifte Tıklayarak Seç ve Alttan Değiştir)
            if (data.isLacunaGene && data.lacunaTopStrand != null && data.lacunaTopStrand.Length > 0)
            {
                GUILayout.BeginArea(new Rect(cardX, 156, cardW, 36), "box");
                GUILayout.BeginHorizontal();

                GUI.color = new Color(0.35f, 0.95f, 1f);
                GUILayout.Label("🔬 ÇİFT DÜZENLE:", EditorBold(10), GUILayout.Width(105));
                GUI.color = Color.white;

                for (int r = 0; r < data.lacunaTopStrand.Length; r++)
                {
                    bool isSel = (data.selectedEditRungIndex == r);
                    bool isGap = (data.lacunaTopStrand[r] == LacunaShape.None);
                    string symTop = LacunaGeneticsDatabase.GetSymbol(data.lacunaTopStrand[r]);
                    string symBot = (data.lacunaBottomStrand != null && r < data.lacunaBottomStrand.Length)
                        ? LacunaGeneticsDatabase.GetSymbol(data.lacunaBottomStrand[r]) : "?";

                    if (isSel)
                        GUI.backgroundColor = new Color(0.2f, 0.95f, 1f);
                    else if (isGap)
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.15f);
                    else
                        GUI.backgroundColor = new Color(0.28f, 0.35f, 0.46f);

                    string rungLabel = $"#{r + 1} [{symTop}/{symBot}]";
                    if (GUILayout.Button(rungLabel, EditorBold(10), GUILayout.Height(24)))
                    {
                        data.selectedEditRungIndex = r;
                        audioManager?.PlaySnap();
                    }
                }
                GUI.backgroundColor = Color.white;

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            // 3. Alt Aksiyon Alanı
            if (data.isDeformed)
            {
                float btnW = 270f;
                float equipDeformedW = 340f;
                float backBtnW = 120f;
                float totalW = btnW + equipDeformedW + backBtnW + 20f;
                float startBtnX = (screenW - totalW) * 0.5f;
                float btnY = screenH - 54f;

                GUI.backgroundColor = new Color(0.2f, 0.85f, 0.95f);
                if (GUI.Button(new Rect(startBtnX, btnY, btnW, 40), "🔄 GENOMU ARINDIR & SIFIRLA", EditorBold(12)))
                {
                    triggerPurify = true;
                }
                GUI.backgroundColor = Color.white;

                float curX = startBtnX + btnW + 10f;

                GUI.backgroundColor = new Color(0.85f, 0.18f, 0.25f);
                if (GUI.Button(new Rect(curX, btnY, equipDeformedW, 40), "☣️ DEFORME GENİ TAK (Hilkat Garibesi Yarat)", EditorBold(11)))
                {
                    triggerEquip = true;
                }
                GUI.backgroundColor = Color.white;

                curX += equipDeformedW + 10f;

                GUI.backgroundColor = new Color(0.4f, 0.5f, 0.65f);
                if (GUI.Button(new Rect(curX, btnY, backBtnW, 40), "↩️ Tezgaha Bırak", EditorBold(11)))
                {
                    triggerClose = true;
                }
                GUI.backgroundColor = Color.white;
            }
            else if (data.isRepaired || !data.isBroken)
            {
                var anotherBroken = poolPieces.Find(p => p != null && p != zoomedGenome && !p.IsEquipped && p.Data != null && p.Data.isBroken && !p.Data.isRepaired && !p.Data.isDeformed);

                float btnW = 310f;
                float resetBtnW = 190f;
                float nextBtnW = anotherBroken != null ? 205f : 0f;
                float backBtnW = 120f;
                float totalW = btnW + resetBtnW + 10f + (anotherBroken != null ? (nextBtnW + 10f) : 0f) + backBtnW + 10f;
                float startBtnX = (screenW - totalW) * 0.5f;
                float btnY = screenH - 54f;

                GUI.backgroundColor = new Color(0.15f, 0.88f, 0.45f);
                if (GUI.Button(new Rect(startBtnX, btnY, btnW, 40), "🚀 ANA DNA SARMALINA YERLEŞTİR", EditorBold(12)))
                {
                    triggerEquip = true;
                }
                GUI.backgroundColor = Color.white;

                float curX = startBtnX + btnW + 10f;

                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.95f);
                if (GUI.Button(new Rect(curX, btnY, resetBtnW, 40), "🔄 Bazı Sök / Tekrar Düzenle", EditorBold(11)))
                {
                    triggerResetRepair = true;
                }
                GUI.backgroundColor = Color.white;
                curX += resetBtnW + 10f;

                if (anotherBroken != null)
                {
                    GUI.backgroundColor = new Color(0.95f, 0.65f, 0.15f);
                    if (GUI.Button(new Rect(curX, btnY, nextBtnW, 40), $"🛠️ SONRAKİ: {anotherBroken.Data.geneName}", EditorBold(11)))
                    {
                        nextBrokenToZoom = anotherBroken;
                    }
                    GUI.backgroundColor = Color.white;
                    curX += nextBtnW + 10f;
                }

                GUI.backgroundColor = new Color(0.4f, 0.5f, 0.65f);
                if (GUI.Button(new Rect(curX, btnY, backBtnW, 40), "↩️ Tezgaha Bırak", EditorBold(11)))
                {
                    triggerClose = true;
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                // Bozuk -> Alttan parça seçme yönlendirmesi
                float hintW = 720;
                float hintX = (screenW - hintW) * 0.5f;
                GUI.color = new Color(0.35f, 0.95f, 1f);
                string hearts = "";
                for (int h = 0; h < data.maxRepairAttempts; h++) hearts += (h < data.remainingRepairAttempts) ? "❤️" : "🖤";
                GUI.Label(new Rect(hintX, screenH - 42f, hintW, 24), $"▼ ALTTAKİ 3D BAZ KAPSÜLLERİNDEN (●↔■, ▲↔◆) DOĞRU OLANINA TIKLAYARAK ONARIN | Hak: {hearts} ▼", EditorBold(11));
                GUI.color = Color.white;
            }

            if (!string.IsNullOrEmpty(dragInterlockMessage))
            {
                float zoomDragW = Mathf.Min(740f, screenW - 40f);
                float zoomDragH = 34f;
                float zoomDragX = (screenW - zoomDragW) * 0.5f;
                float zoomDragY = screenH - 110f;

                GUI.color = new Color(0.04f, 0.22f, 0.14f, 0.95f);
                GUI.Box(new Rect(zoomDragX, zoomDragY, zoomDragW, zoomDragH), GUIContent.none, EditorBox());

                GUI.color = new Color(0.25f, 1f, 0.5f);
                GUI.Label(new Rect(zoomDragX + 12, zoomDragY + 6, zoomDragW - 24, 24), dragInterlockMessage, EditorBold(12));
                GUI.color = Color.white;
            }

            // GUILayout bittikten sonra eylemleri güvenle çalıştır
            if (targetTransformGene != null && zoomedGenome != null)
            {
                string prevName = zoomedGenome.Data.geneName;
                zoomedGenome.TransformToLacunaGene(targetTransformGene);
                audioManager?.PlayResonance();
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"🧬 DÖNÜŞTÜRÜLDÜ! {prevName} ➔ {targetTransformGene.geneName} ({targetTransformGene.traitDescriptionTR})!";
                RefreshAllStates();
            }
            else if (triggerPurify && zoomedGenome != null)
            {
                zoomedGenome.PurifyAndResetDeformed();
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"✨ {zoomedGenome.Data.geneName} arındırıldı ve 3 onarım hakkı yenilendi! Alttan doğru bazı seçebilirsiniz.";
            }
            else if (triggerResetRepair && zoomedGenome != null)
            {
                zoomedGenome.ResetRepairState();
                audioManager?.PlaySnap();
                repairFeedbackMessage = $"🔄 {zoomedGenome.Data.geneName} sıfırlandı! Alttaki bazlardan istediğini seçerek tekrar düzenleyebilirsin.";
            }
            else if (triggerEquip)
            {
                ExitRepairZoomMode(equipToMainStrand: true);
            }
            else if (triggerClose)
            {
                ExitRepairZoomMode(equipToMainStrand: false);
            }
            else if (nextBrokenToZoom != null)
            {
                EnterRepairZoomMode(nextBrokenToZoom);
            }
        }

        private void DrawLacunaRulebookModal(float screenW, float screenH)
        {
            float modalW = Mathf.Min(860f, screenW - 36f);
            float modalH = Mathf.Min(560f, screenH - 36f);
            float modalX = (screenW - modalW) * 0.5f;
            float modalY = (screenH - modalH) * 0.5f;

            GUI.color = new Color(0.02f, 0.04f, 0.09f, 0.98f);
            GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none, EditorBox());
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(modalX + 16f, modalY + 12f, modalW - 32f, modalH - 24f));

            GUILayout.BeginHorizontal();
            GUI.color = new Color(1f, 0.85f, 0.25f);
            GUILayout.Label("📖 LACUNA GENETICS — LABORATUVAR EL KİTABI (20 GENOM & EŞLENİK KURALLARI)", EditorBold(14));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("✕ KAPAT", EditorBold(11), GUILayout.Width(90), GUILayout.Height(26)))
            {
                showRulebookModal = false;
                audioManager?.PlaySnap();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUI.color = new Color(0.35f, 0.95f, 1f);
            GUILayout.Label("🔗 BAZ EŞLENİK KURALI:   ● (Daire)  ↔  ■ (Kare)     |     ▲ (Üçgen)  ↔  ◆ (Elmas)", EditorBold(12));
            GUI.color = Color.white;

            GUILayout.Space(4);
            rulebookScrollPos = GUILayout.BeginScrollView(rulebookScrollPos);

            // 3 Gezegen Kuralları
            GUILayout.BeginVertical("box");
            GUI.color = new Color(1f, 0.78f, 0.25f);
            GUILayout.Label("🌍 GEZEGEN KABUL KURALLARI (FABAUP • SNIBBLEX • OOMGARTH)", EditorBold(12));
            GUI.color = Color.white;
            foreach (var p in LacunaGeneticsDatabase.GetPlanets())
            {
                GUI.color = p.themeColor;
                GUILayout.Label($"• {p.planetName} ({p.subtitleTR}): Deri={p.skinRuleTR}  |  Boy={p.heightRuleTR}  |  Göz={p.eyesRuleTR}  |  Uzuv={p.limbsRuleTR}", EditorBold(11));
            }
            GUI.color = Color.white;
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // 4 Slot ve 20 Genom
            LacunaSlotType[] slots = { LacunaSlotType.Z, LacunaSlotType.V, LacunaSlotType.K, LacunaSlotType.P };
            string[] rankOrders = {
                "Sıralama (Küçükten Büyüğe): ZUUN (1) < ZIMMOK (2) < ZEMBO (3) < ZORP (4) < ZAKKO (5)",
                "Sıralama (Azdan Çoğa): VIXX (0) < VOLM (1) < VESHI (2) < VARQ (3) < VUNDO (4)",
                "Sıralama Yok (Renk Bazlı): KRELL (Turuncu), KUPPA (Sarı), KOSH (Beyaz), KIMBO (Camgöbeği), KLOOT (Mor)",
                "Sıralama (Azdan Çoğa): PHELM (0) < PONDO (1) < PLORT (2) < PUNKO (3) < PRIZZ (4)"
            };

            for (int s = 0; s < slots.Length; s++)
            {
                GUILayout.BeginVertical("box");
                GUI.color = new Color(0.35f, 0.95f, 1f);
                GUILayout.Label($"🧬 {LacunaGeneticsDatabase.GetSlotHeaderTR(slots[s])}  —  {rankOrders[s]}", EditorBold(11));
                GUI.color = Color.white;

                var gList = LacunaGeneticsDatabase.GetGenesBySlot(slots[s]);
                foreach (var g in gList)
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = g.displayColor;
                    GUILayout.Label($"• {g.geneName}", EditorBold(11), GUILayout.Width(90));
                    GUI.color = new Color(0.85f, 0.92f, 1f);
                    GUILayout.Label($"{g.traitDescriptionTR}", EditorLabel(11), GUILayout.Width(175));
                    GUI.color = new Color(0.4f, 1f, 0.65f);
                    GUILayout.Label($"Üst: {g.FormatTopSequence()}   |   Alt: {g.FormatBottomSequence()}", EditorBold(11));
                    GUILayout.EndHorizontal();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private GUIStyle EditorBox()
        {
            var s = new GUIStyle(GUI.skin.box);
            return s;
        }

        private GUIStyle EditorBold(int size)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontStyle = FontStyle.Bold;
            s.fontSize = size;
            s.wordWrap = true;
            return s;
        }

        private GUIStyle EditorLabel(int size)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.wordWrap = true;
            return s;
        }

        private GUIStyle EditorColor(Color c)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = c;
            return s;
        }

        #region Dokunmatik In-Game Tablet Ekranı (Oyun İçi Masaüstü Arayüzü)
        // Masadaki monitör ekranının 3B dünya uzayındaki üst, alt ve merkez referans noktaları
        private static readonly Vector3 MonitorTopWorld = new Vector3(0.8032f, 2.9445f, -4.33f);
        private static readonly Vector3 MonitorBottomWorld = new Vector3(-0.7177f, 2.2438f, -4.33f);
        private static readonly Vector3 MonitorCenterWorld = new Vector3(0.0427f, 2.5942f, -4.33f);





        private void ApplyTextureToTabletMesh()
        {
            if (tabletFrame == null) return;
            if (tabletScreenMaterial == null)
            {
                var tabletObj = GameObject.Find("Gen rehberi tableti");
                if (tabletObj != null)
                {
                    var cube = tabletObj.transform.Find("Cube");
                    if (cube != null)
                    {
                        var mr = cube.GetComponent<MeshRenderer>();
                        if (mr != null && mr.materials.Length > 1)
                        {
                            tabletScreenMaterial = mr.materials[1];
                        }
                    }
                }
            }
            if (tabletScreenMaterial != null && tabletScreenMaterial.mainTexture != tabletFrame)
            {
                tabletScreenMaterial.mainTexture = tabletFrame;
                tabletScreenMaterial.SetTexture("_BaseMap", tabletFrame);
                tabletScreenMaterial.SetTexture("_EmissionMap", tabletFrame);
                tabletScreenMaterial.SetColor("_EmissionColor", Color.white);
                tabletScreenMaterial.EnableKeyword("_EMISSION");
            }
        }

        private static void DrawTabletGlassReflection(float w, float h)
        {
            if (Event.current.type != EventType.Repaint) return;
            // Üst ve sol cam yansıması
            DrawSolidRect(new Rect(0, 0, w, 2f), new Color(0.4f, 0.9f, 1f, 0.28f));
            DrawSolidRect(new Rect(0, 0, 2f, h), new Color(0.4f, 0.9f, 1f, 0.18f));
            // Alt ve sağ iç çerçeve gölgesi
            DrawSolidRect(new Rect(0, h - 2f, w, 2f), new Color(0.02f, 0.08f, 0.14f, 0.45f));
            DrawSolidRect(new Rect(w - 2f, 0, 2f, h), new Color(0.02f, 0.08f, 0.14f, 0.45f));
        }

        private static Texture2D _whiteTex;
        private static void DrawSolidRect(Rect r, Color c)
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _whiteTex);
            GUI.color = prev;
        }

        private static void DrawBorder(Rect r, Color c, float thickness = 1.5f)
        {
            DrawSolidRect(new Rect(r.x, r.y, r.width, thickness), c);
            DrawSolidRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
            DrawSolidRect(new Rect(r.x, r.y, thickness, r.height), c);
            DrawSolidRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
        }

        private static void DrawRing(Vector2 center, float radius, Color color)
        {
            if (_ringTex == null)
            {
                int size = 64;
                _ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float rMid = size * 0.38f;
                float rWidth = size * 0.12f;
                Color32[] px = new Color32[size * size];
                Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), c);
                        float ringDist = Mathf.Abs(d - rMid);
                        float a = Mathf.Clamp01(1f - (ringDist / rWidth));
                        a = a * a;
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                }
                _ringTex.SetPixels32(px);
                _ringTex.Apply();
            }
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), _ringTex);
            GUI.color = prev;
        }

        private void DrawTabletHoloOverlays()
        {
            // 1. Ekran Açılış Siber Lazer Tarama Çizgisi (Laser Scanline)
            if (tabletOpenAnim < 1f && !isTabletClosing)
            {
                float scanY = tabletOpenAnim * 540f;
                float scanAlpha = (1f - tabletOpenAnim) * 0.9f;
                DrawSolidRect(new Rect(0, scanY - 2, 920, 4), new Color(0.4f, 0.95f, 1f, scanAlpha));
                DrawSolidRect(new Rect(0, scanY - 16, 920, 32), new Color(0.15f, 0.7f, 1f, scanAlpha * 0.35f));
            }

            // 2. Siber Köşe Çerçeve Braketleri (Sci-Fi Framing Brackets)
            Color bracketCol = new Color(0.3f, 0.85f, 1f, 0.45f * Mathf.Clamp01(tabletOpenAnim * 1.5f));
            float bLen = 22f;
            float bThick = 2f;
            // Sol Üst
            DrawSolidRect(new Rect(18, 18, bLen, bThick), bracketCol);
            DrawSolidRect(new Rect(18, 18, bThick, bLen), bracketCol);
            // Sağ Üst
            DrawSolidRect(new Rect(920 - 18 - bLen, 18, bLen, bThick), bracketCol);
            DrawSolidRect(new Rect(920 - 18 - bThick, 18, bThick, bLen), bracketCol);
            // Sol Alt
            DrawSolidRect(new Rect(18, 540 - 18 - bThick, bLen, bThick), bracketCol);
            DrawSolidRect(new Rect(18, 540 - 18 - bLen, bThick, bLen), bracketCol);
            // Sağ Alt
            DrawSolidRect(new Rect(920 - 18 - bLen, 540 - 18 - bThick, bLen, bThick), bracketCol);
            DrawSolidRect(new Rect(920 - 18 - bThick, 540 - 18 - bLen, bThick, bLen), bracketCol);

            // 3. Parça Yerleştiğinde Yayılan Biyo-Lüminesans Dalga Halkaları (Shockwave Rings)
            if (lastSnapFeedbackTimer > 0f)
            {
                lastSnapFeedbackTimer -= Time.unscaledDeltaTime;
                float normT = 1f - Mathf.Clamp01(lastSnapFeedbackTimer / 0.45f);

                // 1. Dalga Halkası (Genişleyen Zümrüt Neon)
                float r1 = Mathf.Lerp(14f, 85f, normT);
                float a1 = Mathf.Sin(normT * Mathf.PI);
                DrawRing(lastSnapPos, r1, new Color(0.35f, 1f, 0.7f, a1 * 0.9f));

                // 2. Dalga Halkası (Takip Eden Turkuaz Neon)
                float t2 = Mathf.Clamp01(normT * 1.35f);
                float r2 = Mathf.Lerp(10f, 65f, t2);
                float a2 = Mathf.Sin(t2 * Mathf.PI) * 0.6f;
                DrawRing(lastSnapPos, r2, new Color(0.3f, 0.85f, 1f, a2));

                // Merkez Parıltı Flaşı
                float flashAlpha = (1f - normT) * 0.5f;
                DrawSolidRect(new Rect(lastSnapPos.x - 24, lastSnapPos.y - 30, 48, 60), new Color(0.4f, 1f, 0.85f, flashAlpha));
            }
        }

        private string GetShapeIcon(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return "● Daire (A)";
                case LacunaShape.Square: return "■ Kare (T)";
                case LacunaShape.Triangle: return "▲ Üçgen (C)";
                case LacunaShape.Diamond: return "◆ Elmas (G)";
                default: return "? Boş";
            }
        }

        private string GetShapeShortIcon(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return "●";
                case LacunaShape.Square: return "■";
                case LacunaShape.Triangle: return "▲";
                case LacunaShape.Diamond: return "◆";
                default: return "?";
            }
        }

        private Color GetShapeColor(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return new Color(0.0f, 0.85f, 1.0f);   // Cyan
                case LacunaShape.Square: return new Color(1.0f, 0.55f, 0.1f);   // Orange
                case LacunaShape.Triangle: return new Color(0.15f, 0.95f, 0.35f); // Green
                case LacunaShape.Diamond: return new Color(0.9f, 0.25f, 0.95f);  // Magenta
                default: return new Color(1.0f, 0.3f, 0.3f);    // Red
            }
        }

        private GUIStyle CenteredBold(int size)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontStyle = FontStyle.Bold;
            s.fontSize = size;
            s.alignment = TextAnchor.MiddleCenter;
            s.wordWrap = true;
            return s;
        }

        private GUIStyle CenteredLabel(int size)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.alignment = TextAnchor.MiddleCenter;
            s.wordWrap = true;
            s.normal.textColor = new Color(0.75f, 0.88f, 0.98f);
            return s;
        }

        private string GetShapeTurkishName(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return "Daire";
                case LacunaShape.Square: return "Kare";
                case LacunaShape.Triangle: return "Üçgen";
                case LacunaShape.Diamond: return "Baklava";
                default: return "?";
            }
        }

        #region Gerçek Girintili & Çıkıntılı Yapboz Doku Üreteci (Procedural Jigsaw Engine)
        private static Dictionary<string, Texture2D> jigsawTexCache = new Dictionary<string, Texture2D>();

        private static Texture2D GetJigsawPieceTexture(LacunaShape shape, bool isTop, bool isSocket, bool isSelected, bool isHovered)
        {
            bool isRound = (shape == LacunaShape.Circle || shape == LacunaShape.Square || shape == LacunaShape.None);
            string key = $"tile_{shape}_{isTop}_{isSocket}_{isSelected}_{isHovered}";
            if (jigsawTexCache.TryGetValue(key, out Texture2D cached) && cached != null)
                return cached;

            int w = 96;
            int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[w * h];

            Color baseCol;
            switch (shape)
            {
                case LacunaShape.Circle: baseCol = new Color(0.12f, 0.52f, 0.98f); break; // Mavi A
                case LacunaShape.Square: baseCol = new Color(0.95f, 0.25f, 0.25f); break; // Kırmızı T
                case LacunaShape.Triangle: baseCol = new Color(0.18f, 0.82f, 0.42f); break; // Yeşil C
                case LacunaShape.Diamond: baseCol = new Color(0.98f, 0.60f, 0.10f); break; // Turuncu G
                default: baseCol = new Color(0.25f, 0.45f, 0.75f); break;
            }

            Color32 strokeCol = isSelected ? new Color32(255, 215, 0, 255) : (isHovered ? new Color32(255, 255, 255, 255) : new Color32(10, 20, 42, 255));
            int strokeWidth = isSelected || isHovered ? 4 : 3;

            // Şekil Sınırları ve Girinti/Çıkıntı Hesaplaması
            bool[,] insideMap = new bool[w, h];
            int cx = w / 2;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int bx_min = 14;
                    int bx_max = w - 14;
                    int by_min = isTop ? 28 : 10;
                    int by_max = isTop ? h - 10 : h - 28;

                    bool in_body = (x >= bx_min && x <= bx_max && y >= by_min && y <= by_max);

                    // Sağ Yan Yapboz Dişi (Çıkıntı)
                    int cy_side = (by_min + by_max) / 2;
                    bool in_side_tab = (x >= bx_max && (x - bx_max) * (x - bx_max) + (y - cy_side) * (y - cy_side) <= 10 * 10);

                    // Sol Yan Yapboz Oyuğu (Girinti)
                    bool in_side_indent = (x >= bx_min && (x - bx_min) * (x - bx_min) + (y - cy_side) * (y - cy_side) <= 10 * 10);

                    if (isTop)
                    {
                        // ÜST PARÇA: Alt ucu merkez çizgisine sarkar (Male Tab Çıkıntısı)
                        bool in_center_tab;
                        if (isRound)
                        {
                            // Dairesel Kilit: Yuvarlak kafa + boyun
                            in_center_tab = ((x - cx) * (x - cx) + (y - 16) * (y - 16) <= 12 * 12) || (Mathf.Abs(x - cx) <= 7 && y >= 16 && y <= 28);
                        }
                        else
                        {
                            // Açılı Kilit: Üçgen kama
                            in_center_tab = (y >= 6 && y <= 28 && Mathf.Abs(x - cx) <= (y - 6) * (14.0f / 22.0f));
                        }

                        insideMap[x, y] = (in_body || in_side_tab || in_center_tab) && !in_side_indent;
                    }
                    else
                    {
                        // ALT PARÇA: Üst ucunda merkezden oyuk açılır (Female Socket Girintisi)
                        bool in_center_cutout;
                        if (isRound)
                        {
                            // Dairesel Oyuk
                            in_center_cutout = ((x - cx) * (x - cx) + (y - (h - 40)) * (y - (h - 40)) <= 12 * 12) || (Mathf.Abs(x - cx) <= 7 && y >= (h - 40) && y <= (h - 28));
                        }
                        else
                        {
                            // Açılı Oyuk (V şeklinde üçgen yuva)
                            in_center_cutout = (y >= (h - 50) && y <= (h - 28) && Mathf.Abs(x - cx) <= ((h - 28) - y) * (14.0f / 22.0f));
                        }

                        insideMap[x, y] = (in_body || in_side_tab) && !in_side_indent && !in_center_cutout;
                    }
                }
            }

            // Piksel Renklendirme & Çerçeve / Gradyan Çizimi
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (!insideMap[x, y])
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // Kenar Çizgisi Testi
                    bool isBorder = false;
                    for (int dy = -strokeWidth; dy <= strokeWidth && !isBorder; dy++)
                    {
                        for (int dx = -strokeWidth; dx <= strokeWidth && !isBorder; dx++)
                        {
                            if (dx * dx + dy * dy <= strokeWidth * strokeWidth)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx < 0 || nx >= w || ny < 0 || ny >= h || !insideMap[nx, ny])
                                {
                                    isBorder = true;
                                }
                            }
                        }
                    }

                    if (isSocket)
                    {
                        // Boş Yuva (Dashed Socket)
                        if (isBorder)
                        {
                            bool dash = ((x + y) / 6) % 2 == 0;
                            pixels[idx] = dash ? (Color32)(isSelected ? Color.white : new Color(0.7f, 0.85f, 1f, 0.85f)) : new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            pixels[idx] = new Color32(24, 52, 105, 85);
                        }
                    }
                    else
                    {
                        // Dolu Parça
                        if (isBorder)
                        {
                            pixels[idx] = strokeCol;
                        }
                        else
                        {
                            // Yumuşak karikatür dikey gradyanı + üst parıltı
                            float grad = Mathf.Clamp01((float)y / h);
                            Color c = Color.Lerp(baseCol * 0.82f, baseCol * 1.15f, grad);

                            // Üst kenar parıltısı
                            if (y >= h - 18 && y <= h - 13 && x >= 24 && x <= w - 24)
                            {
                                c = Color.Lerp(c, Color.white, 0.28f);
                            }

                            pixels[idx] = (Color32)c;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            jigsawTexCache[key] = tex;
            return tex;
        }

        #endregion

        private string GetShapeLetter(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return "A";
                case LacunaShape.Square: return "T";
                case LacunaShape.Triangle: return "C";
                case LacunaShape.Diamond: return "G";
                default: return "?";
            }
        }

        private static string GetSlotTitle(LacunaSlotType slot)
        {
            switch (slot)
            {
                case LacunaSlotType.Z: return "Boyut Geni (Z)";
                case LacunaSlotType.V: return "Göz Geni (V)";
                case LacunaSlotType.K: return "Deri Geni (K)";
                case LacunaSlotType.P: return "Uzuv Geni (P)";
                default: return "Genom";
            }
        }

        private void StartTransition(int direction, System.Action onMidpoint)
        {
            CancelTabletDrag();
            transitionProgress = 0f;
            transitionDirection = direction;
            transitionMidpointAction = onMidpoint;
            hasFiredTransitionMidpoint = false;
            audioManager?.PlayClick();
        }

        private void EnsureJigsawLoaded(int creatureIdx)
        {
            CancelTabletDrag();
            slotCelebrationTimer = 0f;
            var roster = LacunaGeneticsDatabase.GetCreatures();
            currentJigsawCreature = Mathf.Clamp(creatureIdx, 0, roster.Count - 1);
            var creature = roster[currentJigsawCreature];
            var planet = LacunaGeneticsDatabase.GetPlanetByName(creature.planetName);
            jigsawStrands = new Dictionary<LacunaSlotType, EditableGeneStrand>();
            totalRepairs = 0;

            foreach (var slot in tabletSlots)
            {
                var gene = LacunaGeneticsDatabase.GetGeneByName(creature.GetInitialGeneName(slot));
                if (!planet.IsGeneAcceptable(slot, gene.geneName))
                {
                    gene = LacunaGeneticsDatabase.GetGenesBySlot(slot).Find(g => planet.IsGeneAcceptable(slot, g.geneName));
                }
                var strand = new EditableGeneStrand(gene, creature.gaps);
                for (int i = 0; i < strand.pairCount; i++)
                {
                    // Leave one reference base in each damaged pair.
                    if (strand.top[i] == LacunaShape.None && strand.bottom[i] == LacunaShape.None)
                    {
                        strand.bottom[i] = gene.bottomStrand[i];
                        strand.wasBottomMissing[i] = false;
                    }
                }
                jigsawStrands.Add(slot, strand);
                totalRepairs += strand.GetMissingCount();
            }
            jigsawMessage = "";
            jigsawVictoryOpen = false;
        }

        private bool NextTabletGap(out EditableGeneStrand strand, out int index, out LacunaSlotType activeSlot)
        {
            // Eğer bir genom yeni tamamlandıysa ve kutlama süresi işliyorsa, tamamlanan genomu ekranda tut
            if (slotCelebrationTimer > 0f && jigsawStrands != null && jigsawStrands.ContainsKey(celebratingSlot))
            {
                strand = jigsawStrands[celebratingSlot];
                index = -1; // Boşluk kalmadı, tamamlandı
                activeSlot = celebratingSlot;
                return true;
            }

            if (jigsawStrands != null)
            {
                foreach (var slot in tabletSlots)
                {
                    if (!jigsawStrands.ContainsKey(slot)) continue;
                    strand = jigsawStrands[slot];
                    for (index = 0; index < strand.pairCount; index++)
                    {
                        if (strand.top[index] == LacunaShape.None || strand.bottom[index] == LacunaShape.None)
                        {
                            activeSlot = slot;
                            return true;
                        }
                    }
                }
            }
            strand = null;
            index = -1;
            activeSlot = LacunaSlotType.Z;
            return false;
        }

        private void AnswerTabletGap(LacunaShape answer)
        {
            if (tabletScreenMode == TabletScreenMode.Sequencing) { AnswerOpeningGap(answer); return; }
            if (!NextTabletGap(out var strand, out int index, out var slot)) return;
            if (index < 0) return;

            bool topMissing = strand.top[index] == LacunaShape.None;
            LacunaShape partner = topMissing ? strand.bottom[index] : strand.top[index];
            if (!LacunaGeneticsDatabase.ArePartners(partner, answer))
            {
                string correct = GetShapeLetter(LacunaGeneticsDatabase.GetPartner(partner));
                jigsawMessage = $"Tekrar dene. {GetShapeLetter(partner)} harfinin eşi {correct}.";
                audioManager?.PlayMismatch();
                return;
            }

            // Kilitlenme biyo-lüminesans şok dalgası pozisyonu ve efekti
            float spacing = Mathf.Min(106, 814f / strand.pairCount);
            float start = 460 - spacing * (strand.pairCount - 1) * 0.5f;
            float focusX = start + index * spacing;
            lastSnapPos = TabletBaseCenter(focusX, topMissing);
            lastSnapFeedbackTimer = 0.45f;
            helixSpeedSurge = 1.0f;

            if (topMissing) strand.top[index] = answer;
            else strand.bottom[index] = answer;

            audioManager?.PlaySnap();
            audioManager?.PlayResonance();

            // Bu genomun tüm eksikleri kapandı mı?
            if (!strand.HasAnyMissing())
            {
                celebratingSlot = slot;
                slotCelebrationTimer = 0.75f;
                helixSpeedSurge = 1.6f;
                jigsawMessage = $"✨ {GetSlotTitle(slot)} KUSURSUZ ONARILDI! ✨";
                audioManager?.PlayVictory();
            }
            else
            {
                jigsawMessage = $"{GetShapeLetter(partner)} ve {GetShapeLetter(answer)} kusursuz eşleşti!";
            }
        }

        private void BuildTabletGlass()
        {
            if (tabletGlass != null) return;
            if (!TryBuildMonitorGlass())
                Debug.LogError("[DNA Puzzle] Sol monitörün ekran mesh'i bulunamadı. Ekran oluşturulamadı.");
        }

        private bool TryBuildMonitorGlass()
        {
            var monitorObj = GameObject.Find("uzun monitör 1");
            if (monitorObj == null) return false;

            var mf = monitorObj.GetComponentInChildren<MeshFilter>(true);
            Transform meshTransform = (mf != null) ? mf.transform : monitorObj.transform;

            Vector3 hi = new Vector3(1.37455f, 0.95004f, -0.12421f);
            Vector3 lo = new Vector3(-1.71277f, -0.95004f, -0.49281f);

            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable && mf.sharedMesh.subMeshCount >= 2)
            {
                var vertices = mf.sharedMesh.vertices;
                var indices = mf.sharedMesh.GetTriangles(1);
                if (indices != null && indices.Length > 0 && vertices != null && vertices.Length > 0)
                {
                    var faceBounds = new Bounds(vertices[indices[0]], Vector3.zero);
                    foreach (int index in indices) faceBounds.Encapsulate(vertices[index]);
                    lo = faceBounds.min;
                    hi = faceBounds.max;
                }
            }

            Vector3 cTR = meshTransform.TransformPoint(new Vector3(hi.x, hi.y, hi.z));
            Vector3 cBR = meshTransform.TransformPoint(new Vector3(lo.x, hi.y, hi.z));
            Vector3 cTL = meshTransform.TransformPoint(new Vector3(hi.x, lo.y, hi.z));
            Vector3 cBL = meshTransform.TransformPoint(new Vector3(lo.x, lo.y, hi.z));

            Vector3 center = (cTR + cBR + cBL + cTL) * 0.25f;

            Vector3 up = ((cTR - cBR) + (cTL - cBL)) * 0.5f;
            float height = up.magnitude;
            up.Normalize();

            Vector3 right = ((cTR - cTL) + (cBR - cBL)) * 0.5f;
            float width = right.magnitude;
            right.Normalize();

            // Normal: Ekran yüzeyinden dışarıya (odaya/oyuncuya) bakan dik vektör
            Vector3 normal = Vector3.Cross(up, right).normalized;

            Vector3 viewPos = (labStationController != null) ? labStationController.BasePosition
                : (Camera.main != null ? Camera.main.transform.position : new Vector3(-4.94f, 4.94f, -4.33f));

            if (Vector3.Dot(normal, viewPos - center) < 0f)
            {
                normal = -normal;
                right = -right;
            }

            Vector3 forward = -normal; // Unity Quad varsayılan olarak -Z'ye bakar, forward ekranın arkasına doğrudur

            // Ekran kavisli olduğundan ve bezellerin arkasında kalmaması için
            // camı normal yönünde 1.2 cm öne alıyoruz:
            center += normal * 0.012f;

            // Bezel içine tam oturması için %97 genişlik, %96 yükseklik (ekranı boşluksuz doldurur)
            float screenW = width * 0.97f;
            float screenH = height * 0.96f;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DNA Monitor Glass";
            quad.GetComponent<Collider>().enabled = false;
            tabletGlass = quad.transform;
            tabletGlass.SetPositionAndRotation(center, Quaternion.LookRotation(forward, up));
            tabletGlass.localScale = new Vector3(screenW, screenH, 1f);
            // Non-uniform parent scale kaynaklı bükülmeyi (diagonal line) önlemek için ölçeği (1,1,1) olan kök transform altına bağlanır:
            tabletGlass.SetParent(transform, true);

            tabletFrame = new RenderTexture(920, 540, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            tabletScreenMaterial = new Material(unlit);
            tabletScreenMaterial.mainTexture = tabletFrame;
            tabletScreenMaterial.renderQueue = 3000;
            tabletScreenMaterial.SetInt("_Cull", 0); // Çift taraflı çizim

            var glassRenderer = quad.GetComponent<Renderer>();
            glassRenderer.sharedMaterial = tabletScreenMaterial;
            glassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glassRenderer.receiveShadows = false;

            if (tabletStation != null) tabletStation.displaySurface = tabletGlass;
            Debug.Log($"[DNA Puzzle] Monitor glass built successfully: size={screenW:F2}x{screenH:F2}m at {center}, normal={normal}");
            return true;
        }

        private void RenderTabletScreen(bool isInteractive)
        {
            if (jigsawStrands == null || jigsawStrands.Count != tabletSlots.Length)
                EnsureJigsawLoaded(currentJigsawCreature);

            const float width = 920, height = 540;
            bool repaint = Event.current.type == EventType.Repaint;
            if (!isInteractive && !repaint && Event.current.type != EventType.Layout) return;
            var previousTarget = RenderTexture.active;
            var previousMouse = Event.current.mousePosition;
            if (isInteractive)
            {
                var cam = (labStationController != null && labStationController.cam != null) ? labStationController.cam : Camera.main;
                if (cam != null && tabletGlass != null)
                {
                    var ray = cam.ScreenPointToRay(new Vector2(previousMouse.x, cam.pixelHeight - previousMouse.y));
                    var plane = new Plane(tabletGlass.forward, tabletGlass.position);
                    if (plane.Raycast(ray, out float distance))
                    {
                        var local = tabletGlass.InverseTransformPoint(ray.GetPoint(distance));
                        Event.current.mousePosition = new Vector2((local.x + .5f) * width, (.5f - local.y) * height);
                    }
                }
            }
            if (repaint)
            {
                RenderTexture.active = tabletFrame;
                GL.Clear(false, true, new Color(.034f, .078f, .13f));
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, width, height, 0);
            }
            try
            {
                // Odadayken veya Ana Ekran modundayken daima Ana Ekranı göster
                if (tabletScreenMode == TabletScreenMode.Sequencing)
                {
                    DrawOpeningSequence(isInteractive);
                }
                else if (!isInteractive || tabletScreenMode == TabletScreenMode.Home)
                {
                    DrawTabletHomeScreen(width, height);
                }
                else
                {
                    DrawTabletBoard();
                }
                DrawTabletHoloOverlays();
                DrawTabletGlassReflection(width, height);
            }
            finally
            {
                if (isInteractive)
                {
                    Event.current.mousePosition = previousMouse;
                }
                if (repaint)
                {
                    GL.PopMatrix();
                    RenderTexture.active = previousTarget;
                }
            }

            // In the docked view the camera faces the glass squarely. Composite the
            // same frame above room effects using the actual projected glass corners.
            if (repaint && isInteractive)
            {
                var cam = labStationController.cam;
                var tl = cam.WorldToScreenPoint(tabletGlass.TransformPoint(new Vector3(-.5f, .5f)));
                var br = cam.WorldToScreenPoint(tabletGlass.TransformPoint(new Vector3(.5f, -.5f)));
                GUI.DrawTexture(Rect.MinMaxRect(tl.x, cam.pixelHeight - tl.y, br.x, cam.pixelHeight - br.y), tabletFrame);
            }
        }

        private void DrawTabletHomeScreen(float canvasW, float canvasH)
        {
            var roster = LacunaGeneticsDatabase.GetCreatures();
            var creature = roster[currentJigsawCreature];
            var planet = LacunaGeneticsDatabase.GetPlanetByName(creature.planetName);

            Color panel = new Color(0.07f, 0.14f, 0.21f);
            Color accent = new Color(0.39f, 0.91f, 0.83f);

            // 1. Üst Bar: Sistem Başlığı & Odaya Dönüş Butonu
            GUI.Label(new Rect(32, 16, 460, 36), "LACUNA GENETICS // DNA TERMİNALİ", EditorBold(22));
            GUI.Label(new Rect(34, 52, 460, 22), "HÜCRESEL BİYO-BÜTÜNLÜK & DNA ONARIM MODÜLÜ v2.4", EditorLabel(13));

            if (TabletButton(new Rect(770, 18, 118, 42), "Odaya dön", panel, 15))
            {
                StartTabletExit();
                return;
            }

            // 2. Canlı Seçim ve Teşhis Kartı
            DrawSolidRect(new Rect(32, 86, 856, 172), panel);
            DrawBorder(new Rect(32, 86, 856, 172), new Color(0.2f, 0.5f, 0.7f, 0.4f), 1.5f);

            // Sol ve Sağ Oklar (Canlı Değiştirme)
            bool canSwitch = (transitionProgress >= 1f);
            if (TabletButton(new Rect(50, 105, 46, 46), "‹", new Color(0.10f, 0.20f, 0.30f), 24))
            {
                if (canSwitch)
                {
                    int prev = (currentJigsawCreature + roster.Count - 1) % roster.Count;
                    StartTransition(-1, () => EnsureJigsawLoaded(prev));
                }
                return;
            }

            if (TabletButton(new Rect(822, 105, 46, 46), "›", new Color(0.10f, 0.20f, 0.30f), 24))
            {
                if (canSwitch)
                {
                    int next = (currentJigsawCreature + 1) % roster.Count;
                    StartTransition(1, () => EnsureJigsawLoaded(next));
                }
                return;
            }

            // Canlı İsmi, Gezegen ve Kimlik
            GUI.Label(new Rect(110, 98, 680, 34), creature.creatureName, CenteredBold(24));
            GUI.Label(new Rect(110, 134, 680, 24), $"KÖKEN GEZEGEN: {planet.planetName.ToUpper()}  |  ÖRNEK NO: #{currentJigsawCreature + 1:D2}", CenteredBold(13));

            // Eksik Baz Durumu ve Hasar Hesabı
            int remaining = 0;
            foreach (var item in jigsawStrands.Values) remaining += item.GetMissingCount();

            if (remaining > 0)
            {
                // Hasar Bildirim Rozeti (Turuncu/Kırmızımsı Parıltılı)
                DrawSolidRect(new Rect(180, 168, 540, 36), new Color(0.35f, 0.15f, 0.05f, 0.75f));
                DrawBorder(new Rect(180, 168, 540, 36), new Color(1f, 0.55f, 0.15f, 0.85f), 1.5f);
                GUI.color = new Color(1f, 0.85f, 0.3f);
                GUI.Label(new Rect(180, 173, 540, 26), $"⚠️ GENOM HASARI: {remaining} Adet Eksik Nükleotid Parçası Bulunuyor", CenteredBold(14));
                GUI.color = Color.white;
            }
            else
            {
                // Kusursuz Onarım Rozeti (Yeşil)
                DrawSolidRect(new Rect(180, 168, 540, 36), new Color(0.05f, 0.30f, 0.15f, 0.75f));
                DrawBorder(new Rect(180, 168, 540, 36), new Color(0.3f, 1f, 0.5f, 0.85f), 1.5f);
                GUI.color = new Color(0.4f, 1f, 0.6f);
                GUI.Label(new Rect(180, 173, 540, 26), "✅ GENOM BÜTÜNLÜĞÜ TAM: Canlı Doğrudan Test Edilebilir", CenteredBold(14));
                GUI.color = Color.white;
            }

            // İpucu / Bilgi Satırı
            string clue = string.IsNullOrEmpty(creature.landlordClueTR) 
                ? "Ev sahibi notu: Nükleotid eşleşmelerini kurala göre tamamlayarak biyo-bütünlüğü sağla."
                : creature.landlordClueTR;
            GUI.Label(new Rect(60, 216, 780, 30), $"💡 {clue}", CenteredLabel(12));

            // 3. BÜYÜK MERKEZİ START / BAŞLAT BUTONU
            float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.15f + 0.85f;
            Rect startBtnRect = new Rect(270, 300, 380, 72);

            Color btnBaseColor = (remaining > 0)
                ? new Color(0.08f, 0.42f, 0.38f)
                : new Color(0.12f, 0.48f, 0.22f);

            string btnText = (remaining > 0) ? "▶ ONARIMA BAŞLA" : "🚀 KAPSÜLÜ GÖNDER & TEST ET";

            if (TabletButton(startBtnRect, btnText, btnBaseColor, 22))
            {
                audioManager?.PlayClick();
                if (remaining > 0)
                {
                    StartTransition(1, () => { tabletScreenMode = TabletScreenMode.Puzzle; });
                }
                else
                {
                    ExecuteJigsawCapsuleAndTest(creature, planet);
                }
                return;
            }

            // Buton etrafına neon pulsatif çerçeve efekti
            DrawBorder(startBtnRect, new Color(accent.r, accent.g, accent.b, pulse), 3f);

            // 4. Alt Bilgilendirme ve Hızlı Kısayollar
            if (remaining == 0)
            {
                if (TabletButton(new Rect(340, 395, 240, 38), "↺ Bulmacayı İncele", panel, 14))
                {
                    StartTransition(1, () => { tabletScreenMode = TabletScreenMode.Puzzle; });
                    return;
                }
            }
            else
            {
                GUI.Label(new Rect(100, 398, 720, 24), "🎯 Eşleşme Kuralı:  A ↔ T   |   C ↔ G    —    [Esc] tuşu ile odaya dönebilirsiniz", CenteredLabel(13));
            }

            // 5. En Alt Bilgi Çubuğu
            int totalCount = roster.Count;
            GUI.Label(new Rect(32, 485, 856, 26), $"LACUNA DATABASE // Toplam {totalCount} canlı kataloğu yüklendi  •  İstasyon 1 (Sol Monitör)", CenteredLabel(11));
        }

        private void DrawTabletBoard()
        {
            var roster = LacunaGeneticsDatabase.GetCreatures();
            var creature = roster[currentJigsawCreature];
            var planet = LacunaGeneticsDatabase.GetPlanetByName(creature.planetName);
            if (jigsawVictoryOpen)
            {
                DrawJigsawVictoryModal(920, 540, creature, planet, roster);
                return;
            }

            Color panel = new Color(0.07f, 0.14f, 0.21f);
            Color accent = new Color(0.39f, 0.91f, 0.83f);

            // 1. Sabit Üst Bar (Başlık, Menü Butonu ve Yan Oklar)
            if (TabletButton(new Rect(32, 18, 96, 42), "‹ Menü", panel, 15))
            {
                StartTransition(-1, () => {
                    CancelTabletDrag();
                    tabletScreenMode = TabletScreenMode.Home;
                });
                return;
            }

            GUI.Label(new Rect(140, 12, 320, 43), "DNA onarımı", EditorBold(28));
            GUI.Label(new Rect(142, 56, 350, 26), "Eşleşme kuralı     A – T     /     C – G", EditorLabel(16));

            bool canSwitch = (transitionProgress >= 1f && slotCelebrationTimer <= 0f);

            if (TabletButton(new Rect(500, 22, 46, 42), "‹", panel, 24))
            {
                if (canSwitch)
                {
                    int prev = (currentJigsawCreature + roster.Count - 1) % roster.Count;
                    StartTransition(-1, () => EnsureJigsawLoaded(prev));
                }
                return;
            }

            if (TabletButton(new Rect(717, 22, 46, 42), "›", panel, 24))
            {
                if (canSwitch)
                {
                    int next = (currentJigsawCreature + 1) % roster.Count;
                    StartTransition(1, () => EnsureJigsawLoaded(next));
                }
                return;
            }

            if (TabletButton(new Rect(780, 22, 108, 42), "Odaya dön", panel, 15))
            {
                StartTabletExit();
                return;
            }

            // 2. Kayan Dinamik İçerik Bölgesi (Ok geçişlerinde ve otomatik genom geçişinde akıcı kayar)
            var prevMat = GUI.matrix;
            Color prevColor = GUI.color;

            if (transitionProgress < 1f)
            {
                float t = transitionProgress < 0.5f 
                    ? (transitionProgress / 0.5f) 
                    : ((transitionProgress - 0.5f) / 0.5f);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                float xOffset = transitionProgress < 0.5f 
                    ? (-transitionDirection * ease * 460f) 
                    : (transitionDirection * (1f - ease) * 460f);
                float alpha = transitionProgress < 0.5f ? (1f - ease) : ease;
                float s = Mathf.Lerp(1f, 0.93f, Mathf.Sin(t * Mathf.PI));

                GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * alpha);
                Vector2 pivot = new Vector2(460f, 270f);
                GUI.matrix = prevMat * Matrix4x4.TRS(
                    new Vector3(xOffset + pivot.x * (1f - s), pivot.y * (1f - s), 0),
                    Quaternion.identity,
                    new Vector3(s, s, 1f)
                );
            }

            try
            {
                // Canlı ve Gezegen Başlığı (Kayan bölgede, okların arasından yumuşakça geçer)
                GUI.Label(new Rect(550, 15, 163, 30), creature.creatureName, CenteredBold(18));
                GUI.Label(new Rect(550, 43, 163, 22), planet.planetName, CenteredBold(12));

                bool hasGap = NextTabletGap(out var strand, out int index, out var slot);
                int remaining = 0;
                foreach (var item in jigsawStrands.Values) remaining += item.GetMissingCount();
                int solved = totalRepairs - remaining;

                // İlerleme çubuğu
                DrawSolidRect(new Rect(32, 92, 856, 5), panel);
                DrawSolidRect(new Rect(32, 92, 856f * solved / Mathf.Max(totalRepairs, 1), 5), accent);

                if (!hasGap)
                {
                    GUI.Label(new Rect(80, 168, 760, 60), "DNA onarıldı", CenteredBold(36));
                    GUI.Label(new Rect(80, 238, 760, 38), $"{solved} eşleşme tamamlandı. {creature.creatureName} teste hazır.", CenteredBold(18));
                    if (TabletButton(new Rect(320, 323, 280, 64), "Kapsülü gönder", new Color(0.07f, 0.40f, 0.38f), 21))
                        ExecuteJigsawCapsuleAndTest(creature, planet);
                    GUI.Label(new Rect(40, 423, 840, 42), jigsawMessage, CenteredBold(16));
                    return;
                }

                // Genom Başlığı ve Soru Metni
                bool isCelebrating = (slotCelebrationTimer > 0f);
                string slotTitle = GetSlotTitle(slot);

                if (isCelebrating)
                {
                    GUI.Label(new Rect(34, 112, 600, 32), $"✨ {slotTitle} Kusursuz Onarıldı!", EditorBold(22));
                }
                else
                {
                    LacunaShape partner = strand.top[index] == LacunaShape.None ? strand.bottom[index] : strand.top[index];
                    GUI.Label(new Rect(34, 112, 600, 32), $"{slotTitle} — {GetShapeLetter(partner)} hangi harfle eşleşir?", EditorBold(22));
                }

                GUI.Label(new Rect(680, 115, 208, 28), $"{solved} / {totalRepairs} onarıldı", CenteredBold(15));

                // DNA Heliks Paneli
                DrawSolidRect(new Rect(32, 160, 856, 186), panel);

                if (isCelebrating)
                {
                    // Kutlama anında yeşil/turkuaz bio-enerji parıltısı
                    float pulse = Mathf.Sin(Time.unscaledTime * 8f) * 0.2f + 0.35f;
                    DrawSolidRect(new Rect(32, 160, 856, 186), new Color(0.05f, 0.35f, 0.22f, pulse));
                    DrawBorder(new Rect(32, 160, 856, 186), new Color(0.25f, 1f, 0.65f, 0.75f), 2.5f);
                }

                float spacing = Mathf.Min(106, 814f / strand.pairCount);
                float start = 460 - spacing * (strand.pairCount - 1) * 0.5f;
                bool topGap = (index >= 0) && (strand.top[index] == LacunaShape.None);
                float focusX = (index >= 0) ? (start + index * spacing) : 460f;
                Vector2 target = TabletBaseCenter(focusX, topGap);

                float focusAngle = focusX * (2 * Mathf.PI / HelixPeriod) + helixPhase;
                float focusDepth = topGap ? -Mathf.Sin(focusAngle) : Mathf.Sin(focusAngle);
                float focusS = Mathf.Lerp(0.90f, 1.04f, (focusDepth + 1f) * 0.5f);
                float sockW = 54f * focusS;
                float sockH = 68f * focusS;
                Rect socket = new Rect(target.x - sockW * 0.5f, target.y - sockH * 0.5f, sockW, sockH);

                // 1. Heliks DNA sarmal şeritleri (Cyan ve İndigo dalgalanan çift sarmal tüpleri)
                DrawHelixRibbons(start - spacing * 0.60f, spacing * (strand.pairCount + 0.20f));

                // 2. Baz çiftlerini 3B derinlik sırasına göre çiz (Arkadan öne doğru, asla iç içe geçmez!)
                for (int i = 0; i < strand.pairCount; i++)
                {
                    float x = start + i * spacing;
                    Vector2 top = TabletBaseCenter(x, true);
                    Vector2 bottom = TabletBaseCenter(x, false);

                    float angle = x * (2 * Mathf.PI / HelixPeriod) + helixPhase;
                    float depthTop = -Mathf.Sin(angle);
                    float depthBottom = Mathf.Sin(angle);

                    bool isFocus = (i == index);

                    if (depthTop < depthBottom)
                    {
                        DrawTabletBase(top.x, top.y, strand.top[i], strand.bottom[i], true, isFocus, depthTop);
                        DrawHelixRung(top, bottom);
                        DrawTabletBase(bottom.x, bottom.y, strand.bottom[i], strand.top[i], false, isFocus, depthBottom);
                    }
                    else
                    {
                        DrawTabletBase(bottom.x, bottom.y, strand.bottom[i], strand.top[i], false, isFocus, depthBottom);
                        DrawHelixRung(top, bottom);
                        DrawTabletBase(top.x, top.y, strand.top[i], strand.bottom[i], true, isFocus, depthTop);
                    }
                }

                if (index >= 0 && !isCelebrating)
                {
                    DrawBorder(new Rect(socket.x - 3, socket.y - 2, socket.width + 6, socket.height + 4), accent, 2.5f);
                    LacunaShape partner = strand.top[index] == LacunaShape.None ? strand.bottom[index] : strand.top[index];
                    DrawTabletTray(socket, partner, topGap);
                }
                else if (isCelebrating)
                {
                    // Kutlama anında tepsi yerine zafer mesajı
                    GUI.Label(new Rect(32, 380, 856, 36), $"✔ {slotTitle} TAMAMLANDI — SIRADAKİ DİZİLİME GEÇİLİYOR...", CenteredBold(19));
                    GUI.Label(new Rect(32, 420, 856, 26), "Otomatik aktarım hazırlanıyor...", CenteredBold(14));
                }
            }
            finally
            {
                GUI.matrix = prevMat;
                GUI.color = prevColor;
            }

            // 3. Holografik Geçiş Tarama Çizgisi (Ekranı tarayan neon çizgi efekti)
            if (transitionProgress < 1f)
            {
                float scanX = transitionDirection > 0
                    ? Mathf.Lerp(32f, 888f, transitionProgress)
                    : Mathf.Lerp(888f, 32f, transitionProgress);
                float scanAlpha = Mathf.Sin(transitionProgress * Mathf.PI) * 0.85f;
                DrawSolidRect(new Rect(scanX - 2f, 150f, 4f, 210f), new Color(0.35f, 0.95f, 1f, scanAlpha));
                DrawSolidRect(new Rect(scanX - 18f, 150f, 36f, 210f), new Color(0.35f, 0.95f, 1f, scanAlpha * 0.20f));
            }
        }

        private Vector2 TabletBaseCenter(float x, bool top)
        {
            float angle = x * (2 * Mathf.PI / HelixPeriod) + helixPhase;
            Vector2 offset = new Vector2(37 * Mathf.Sin(angle), -57 * Mathf.Cos(angle));
            return new Vector2(x, HelixMiddle) + offset * (top ? 0.70f : -0.70f);
        }

        private static void DrawHelixRung(Vector2 top, Vector2 bottom)
        {
            if (Event.current.type != EventType.Repaint) return;
            Vector2 dir = bottom - top;
            float dist = dir.magnitude;
            if (dist < 10f) return;

            // Hidrojen bağı: Parçalar arasındaki temiz bağlantı çizgisi (asla dışarı taşmaz)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, top);
            DrawSolidRect(new Rect(top.x, top.y - 1.5f, dist, 3f), new Color(0.35f, 0.85f, 1f, 0.35f));
            GUI.matrix = saved;
        }

        private void DrawHelixRibbons(float left, float width)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (helixRibbons == null) helixRibbons = BuildHelixRibbons();
            float offset = left / HelixPeriod + helixPhase / (2 * Mathf.PI);
            GUI.DrawTextureWithTexCoords(new Rect(left, HelixMiddle - 72, width, 144), helixRibbons,
                new Rect(offset, 0, width / HelixPeriod, 1));
        }

        // Paint one seamless turn. At each crossing the nearer strand covers the farther one.
        private static Texture2D BuildHelixRibbons()
        {
            const int width = 720, height = 288;
            var pixels = new Color32[width * height];
            var depths = new float[pixels.Length];
            for (int i = 0; i < depths.Length; i++) depths[i] = -2;
            for (int strand = 0; strand < 2; strand++)
            {
                Color tint = strand == 0 ? new Color(0.15f, 0.87f, 0.82f) : new Color(0.49f, 0.52f, 1f);
                for (int step = 0; step < 2 * width; step++)
                {
                    float angle = step * Mathf.PI / width;
                    float side = strand == 0 ? 1 : -1;
                    float depth = side * Mathf.Sin(angle);
                    float cx = step * 0.5f + 74 * depth;
                    float cy = height * 0.5f + 114 * side * Mathf.Cos(angle);
                    float radius = 7 + 2.5f * depth;
                    int yMin = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
                    int yMax = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius));
                    int xMin = Mathf.FloorToInt(cx - radius);
                    int xMax = Mathf.CeilToInt(cx + radius);
                    for (int y = yMin; y <= yMax; y++)
                    for (int x = xMin; x <= xMax; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance >= radius) continue;
                        int modX = ((x % width) + width) % width;
                        int pixel = y * width + modX;
                        float surface = depth + 0.15f * (1 - distance / radius);
                        if (surface < depths[pixel]) continue;
                        depths[pixel] = surface;
                        Color c = tint * (0.75f + 0.25f * depth);
                        c = Color.Lerp(c, Color.white, 0.35f * (1 - distance / radius));
                        c.a = Mathf.Clamp01(radius - distance);
                        pixels[pixel] = c;
                    }
                }
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (tabletFrame != null) { tabletFrame.Release(); Destroy(tabletFrame); }
            if (tabletGlass != null) Destroy(tabletGlass.gameObject);
            if (tabletScreenMaterial != null) Destroy(tabletScreenMaterial);
            if (helixRibbons != null) Destroy(helixRibbons);
        }

        private static Rect TrayPieceRect(int index)
        {
            return new Rect(126 + 190 * index, 392, 96, 118);
        }

        private void DrawTabletTray(Rect socket, LacunaShape partner, bool top)
        {
            Event input = Event.current;
            int id = GUIUtility.GetControlID("TabletBaseDrag".GetHashCode(), FocusType.Passive);
            var dropArea = Rect.MinMaxRect(socket.xMin - 11, socket.yMin - 6, socket.xMax + 11, socket.yMax + 6);
            string instruction = string.IsNullOrEmpty(jigsawMessage)
                ? "Bir parçayı tut ve soru işaretli yuvaya bırak." : jigsawMessage;
            GUI.Label(new Rect(32, 344, 856, 32), instruction, CenteredBold(17));

            for (int i = 0; i < tabletBases.Length; i++)
            {
                Rect tray = TrayPieceRect(i);
                bool hover = tray.Contains(input.mousePosition);
                if (pieceMotion == TabletPieceMotion.Idle && hover && GUIUtility.hotControl == 0
                    && transitionProgress >= 1f && slotCelebrationTimer <= 0f
                    && input.type == EventType.MouseDown && input.button == 0)
                {
                    dragControl = id;
                    GUIUtility.hotControl = id;
                    movingBaseIndex = i;
                    grabOffset = tray.center - input.mousePosition;
                    pieceCenter = tray.center;
                    pieceMotion = TabletPieceMotion.Held;
                    motionStarted = Time.unscaledTime;
                    jigsawMessage = "";
                    audioManager?.PlayClick();
                    input.Use();
                }

                bool lifted = pieceMotion != TabletPieceMotion.Idle && movingBaseIndex == i;
                Rect renderRect = tray;
                if (hover && !lifted && pieceMotion == TabletPieceMotion.Idle)
                {
                    renderRect.y -= 4f;
                    renderRect.height += 4f;
                }
                DrawMovingBase(renderRect, tabletBases[i], top, lifted, hover && !lifted, 34);
            }

            if (pieceMotion == TabletPieceMotion.Idle) return;
            LacunaShape piece = tabletBases[movingBaseIndex];
            Rect home = TrayPieceRect(movingBaseIndex);

            if (pieceMotion == TabletPieceMotion.Held)
            {
                if (input.type == EventType.Ignore || input.type == EventType.MouseLeaveWindow)
                {
                    CancelTabletDrag();
                    return;
                }

                if (input.type == EventType.MouseDrag || input.type == EventType.MouseUp)
                    pieceCenter = input.mousePosition + grabOffset;

                if (input.type == EventType.MouseUp && input.button == 0)
                {
                    bool overSocket = dropArea.Contains(input.mousePosition) || dropArea.Contains(pieceCenter);
                    bool fits = overSocket && LacunaGeneticsDatabase.ArePartners(partner, piece);
                    releaseCenter = pieceCenter;
                    motionStarted = Time.unscaledTime;
                    pieceMotion = fits ? TabletPieceMotion.Snapping : TabletPieceMotion.Returning;
                    ReleaseTabletPointer();
                    if (overSocket && !fits) AnswerTabletGap(piece);
                    else if (!overSocket) jigsawMessage = "Parçayı soru işaretli yuvaya bırakın.";
                    input.Use();
                }
            }

            float elapsed = Time.unscaledTime - motionStarted;
            Vector2 size = home.size;

            if (pieceMotion == TabletPieceMotion.Held)
            {
                size = home.size * 1.05f;
                bool near = dropArea.Contains(input.mousePosition) || dropArea.Contains(pieceCenter);
                if (near)
                {
                    float pulse = Mathf.Sin(Time.unscaledTime * 9f) * 0.35f + 0.65f;
                    DrawBorder(dropArea, new Color(0.4f, 1f, 0.8f, pulse), 3f);
                }
            }
            else
            {
                float t = Mathf.Clamp01(elapsed / 0.18f);
                float ease = Mathf.SmoothStep(0, 1, t);
                Vector2 end = (pieceMotion == TabletPieceMotion.Snapping) ? socket.center : home.center;
                pieceCenter = Vector2.Lerp(releaseCenter, end, ease);
                size = Vector2.Lerp(home.size * 1.05f, (pieceMotion == TabletPieceMotion.Snapping) ? socket.size : home.size, ease);
                if (t >= 1f)
                {
                    if (pieceMotion == TabletPieceMotion.Snapping) AnswerTabletGap(piece);
                    pieceMotion = TabletPieceMotion.Idle;
                    return;
                }
            }
            DrawSolidRect(new Rect(pieceCenter.x - size.x * 0.5f + 4f, pieceCenter.y - size.y * 0.5f + 6f, size.x, size.y), new Color(0f, 0f, 0f, 0.35f));
            Rect floating = new Rect(pieceCenter - size * 0.5f, size);
            DrawMovingBase(floating, piece, top, false, true, Mathf.RoundToInt(size.y * 0.31f));
        }

        private void DrawMovingBase(Rect rect, LacunaShape shape, bool top, bool empty, bool highlight, int fontSize)
        {
            var texture = GetJigsawPieceTexture(shape, top, empty, false, highlight);
            GUI.DrawTexture(rect, texture);
            if (!empty) GUI.Label(rect, GetShapeLetter(shape), CenteredBold(fontSize));
        }

        private void ReleaseTabletPointer()
        {
            if (dragControl != 0 && GUIUtility.hotControl == dragControl)
                GUIUtility.hotControl = 0;
            dragControl = 0;
        }

        private void CancelTabletDrag()
        {
            ReleaseTabletPointer();
            pieceMotion = TabletPieceMotion.Idle;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) CancelTabletDrag();
        }

        private void DrawTabletBase(float cx, float cy, LacunaShape shape, LacunaShape partner, bool top, bool focus, float depth = 0f)
        {
            bool missing = shape == LacunaShape.None;

            // 3B derinlik perspektif ölçeği (öndeki parça %104, arkadaki parça %90)
            float s = Mathf.Lerp(0.90f, 1.04f, (depth + 1f) * 0.5f);
            float w = 54f * s;
            float h = 68f * s;
            var rect = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);

            var texture = GetJigsawPieceTexture(missing ? partner : shape, top, missing, focus, false);
            Color saved = GUI.color;

            if (!focus)
            {
                // Arkada kalan parçaları hafifçe gölgele (3B derinlik hissi)
                float shade = Mathf.Lerp(0.68f, 1f, (depth + 1f) * 0.5f);
                GUI.color = new Color(0.82f * shade, 0.88f * shade, 0.93f * shade);
            }
            else
            {
                float shade = Mathf.Lerp(0.88f, 1.05f, (depth + 1f) * 0.5f);
                GUI.color = new Color(shade, shade, shade);
            }

            GUI.DrawTexture(rect, texture);
            GUI.color = saved;

            int fontSize = Mathf.RoundToInt(24f * s);
            GUI.Label(rect, GetShapeLetter(shape), CenteredBold(fontSize));
        }

        private bool TabletButton(Rect rect, string label, Color color, int size)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            if (hover)
            {
                color = Color.Lerp(color, Color.white, 0.16f);
                DrawBorder(rect, new Color(0.35f, 0.95f, 1f, 0.75f), 1.5f);
            }
            DrawSolidRect(rect, color);
            GUI.Label(rect, label, CenteredBold(size));
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }


        private void ExecuteJigsawCapsuleAndTest(LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet)
        {
            // 1. Tüm slotlarda eksik parça var mı?
            foreach (LacunaSlotType s in System.Enum.GetValues(typeof(LacunaSlotType)))
            {
                var st = jigsawStrands[s];
                if (st.HasAnyMissing())
                {
                    jigsawMessage = $"⚠️ Eksik Parça Var! Slot {s} içinde henüz doldurulmamış ({st.GetMissingCount()} adet '?') yuva var.";
                    audioManager?.PlayError();
                    return;
                }
            }

            // 2. Tüm slotlarda eşlenik kuralı (● ↔ ■, ▲ ↔ ◆) sağlanmış mı?
            foreach (LacunaSlotType s in System.Enum.GetValues(typeof(LacunaSlotType)))
            {
                var st = jigsawStrands[s];
                if (!st.AreAllPairsValidComplements())
                {
                    jigsawMessage = $"✖ Eşlenik Kuralı İhlali! Slot {s} içinde kurala uymayan eşleşmeler var. (● Daire ⟷ ■ Kare, ▲ Üçgen ⟷ ◆ Elmas).";
                    audioManager?.PlayError();
                    return;
                }
            }

            // 3. Tüm slotlar geçerli bir gene eşleşiyor mu?
            var matchedGenes = new Dictionary<LacunaSlotType, LacunaGeneDefinition>();
            foreach (LacunaSlotType s in System.Enum.GetValues(typeof(LacunaSlotType)))
            {
                var st = jigsawStrands[s];
                var m = LacunaGeneticsDatabase.MatchStrandToGene(s, st.top, st.bottom);
                if (m == null)
                {
                    jigsawMessage = $"❓ Tanımsız Gen! Slot {s} üzerindeki dizilim Kural Kitabındaki 5 geçerli genden hiçbirine uymuyor. Kural kitabına göre ayarlayın.";
                    audioManager?.PlayError();
                    return;
                }
                matchedGenes[s] = m;
            }

            // 4. Gezegen uyum kontrolleri (FABAUP, SNIBBLEX, OOMGARTH)
            foreach (LacunaSlotType s in System.Enum.GetValues(typeof(LacunaSlotType)))
            {
                var g = matchedGenes[s];
                if (!planet.IsGeneAcceptable(s, g.geneName))
                {
                    string ruleText = planet.GetRuleForSlot(s);
                    jigsawMessage = $"⚠️ GEZEGEN UYUMSUZLUĞU: {g.geneName} ({g.bodyTraitTR}) geni {planet.planetName} için uygun değil!\nKural: {ruleText}. Kural Kitabından geçerli gene dönüştürün!";
                    audioManager?.PlayError();
                    return;
                }
            }

            // TAM BAŞARI! 3D Canlıyı kuluçka odasında hayat buldur!
            string zG = matchedGenes[LacunaSlotType.Z].geneName;
            string vG = matchedGenes[LacunaSlotType.V].geneName;
            string kG = matchedGenes[LacunaSlotType.K].geneName;
            string pG = matchedGenes[LacunaSlotType.P].geneName;

            SpawnLacunaCreatureInLabChamber(entry.creatureName, planet.planetName, zG, vG, kG, pG);
            audioManager?.PlayVictory();

            jigsawVictoryOpen = true;
            jigsawVictoryText =
                $"Numune #{entry.number}: {entry.creatureName}\n" +
                $"Gezegen: {planet.planetName} ({planet.subtitleTR})\n\n" +
                $"Onarılan Genler:\n" +
                $"  • Boyut (Z): {zG} ({matchedGenes[LacunaSlotType.Z].bodyTraitTR})\n" +
                $"  • Göz (V): {vG} ({matchedGenes[LacunaSlotType.V].bodyTraitTR})\n" +
                $"  • Deri (K): {kG} ({matchedGenes[LacunaSlotType.K].bodyTraitTR})\n" +
                $"  • Uzuv (P): {pG} ({matchedGenes[LacunaSlotType.P].bodyTraitTR})\n\n" +
                $"Ev Sahibi'nin Notu: \"{entry.landlordClueTR}\"";
        }

        private void DrawJigsawVictoryModal(float screenW, float screenH, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet, List<LacunaCreatureRosterEntry> roster)
        {
            DrawSolidRect(new Rect(0, 0, screenW, screenH), new Color(0, 0, 0, 0.75f));

            float modalW = 540f;
            float modalH = 340f;
            float modalX = (screenW - modalW) * 0.5f;
            float modalY = (screenH - modalH) * 0.5f;

            Rect modalRect = new Rect(modalX, modalY, modalW, modalH);
            DrawSolidRect(modalRect, new Color(0.04f, 0.10f, 0.18f, 0.98f));
            DrawBorder(modalRect, new Color(0.2f, 1.0f, 0.55f), 3f);

            GUI.color = new Color(0.25f, 1.0f, 0.55f);
            GUI.Label(new Rect(modalX + 16f, modalY + 16f, modalW - 32f, 28f), "🎉 KAPSÜL MÜHÜRLENDİ & TEST BAŞARILI!", CenteredBold(15));
            GUI.color = Color.white;

            GUI.Label(new Rect(modalX + 24f, modalY + 54f, modalW - 48f, 180f), jigsawVictoryText, EditorLabel(12));

            float btnY = modalY + modalH - 52f;
            GUI.backgroundColor = new Color(0.12f, 0.65f, 0.35f);
            if (GUI.Button(new Rect(modalX + 20f, btnY, 240f, 36f), "➔ SONRAKİ NUMUNEYE GEÇ", EditorBold(11)))
            {
                int next = (currentJigsawCreature + 1) % roster.Count;
                StartTransition(1, () => {
                    jigsawVictoryOpen = false;
                    EnsureJigsawLoaded(next);
                });
                audioManager?.PlaySnap();
            }

            GUI.backgroundColor = new Color(0.2f, 0.4f, 0.65f);
            if (GUI.Button(new Rect(modalX + 280f, btnY, 240f, 36f), "🔬 ODAYA DÖN VE İZLE", EditorBold(11)))
            {
                jigsawVictoryOpen = false;
                ReturnToLabRoom();
            }
            GUI.backgroundColor = Color.white;
        }
        #endregion
        #endregion
    }
}
