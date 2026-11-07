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
    public class DNAAnimatedPuzzleController : MonoBehaviour
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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        private void Start()
        {
            EnsurePuzzleInitialized();
        }

        private void EnsurePuzzleInitialized()
        {
            if (!Application.isPlaying) return;
            if (Instance == null) Instance = this;

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
                Destroy(transform.GetChild(i).gameObject);
            }

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
            dropObj.transform.SetParent(transform, false);
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

            RefreshAllStates();
        }

        private void SetupCameraAndLighting()
        {
            puzzleCamera = Camera.main;
            if (puzzleCamera == null)
            {
                var camGo = new GameObject("MainCamera");
                puzzleCamera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            // Eğer sahnede CameraStationController (Lab sahnesi) varsa kameranın açısını ve ışıklarını bozma!
            if (FindAnyObjectByType<CameraStationController>() == null && puzzleCamera.GetComponent<CameraStationController>() == null)
            {
                puzzleCamera.transform.position = new Vector3(0f, 0.2f, -13.2f);
                puzzleCamera.transform.rotation = Quaternion.identity;
                puzzleCamera.backgroundColor = new Color(0.012f, 0.015f, 0.035f);
                puzzleCamera.clearFlags = CameraClearFlags.SolidColor;
                puzzleCamera.fieldOfView = 56f;
            }

            RenderSettings.ambientLight = new Color(0.2f, 0.28f, 0.42f);

            // 1. Ana Işık (Key Light - Sadece Directional Light ara, Spot/Point ışıkları bozma)
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

            // 2. Sinematik Yan Neon Işık (Elektrik Camgöbeği)
            var rimLightGo = new GameObject("CyanRimLight");
            rimLightGo.transform.SetParent(transform, false);
            var rimLight = rimLightGo.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.transform.position = new Vector3(-8f, 4f, -4f);
            rimLight.color = new Color(0.1f, 0.8f, 1f);
            rimLight.range = 14f;
            rimLight.intensity = 2.2f;

            // 3. Karşı Sinematik Yan Işık (Plazma Menekşesi)
            var warmLightGo = new GameObject("PlasmaRimLight");
            warmLightGo.transform.SetParent(transform, false);
            var warmLight = warmLightGo.AddComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.range = 14f;
            warmLight.transform.position = new Vector3(8f, -2f, -3f);
            warmLight.color = new Color(0.75f, 0.2f, 1f);
            warmLight.intensity = 1.8f;
        }

        private void BuildStunningSciFiChamber()
        {
            if (backgroundChamber != null) return;

            backgroundChamber = new GameObject("SciFiBioChamberBackdrop");
            backgroundChamber.transform.SetParent(transform, false);
            backgroundChamber.transform.position = Vector3.zero;

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
            GameObject eggObj = new GameObject("AnimatedCreatureEgg");
            eggObj.transform.SetParent(transform, false);
            eggObj.transform.position = new Vector3(-7.85f, 1.45f, 0f);
            eggObj.transform.localScale = Vector3.one * 0.95f;
            creatureEgg = eggObj.AddComponent<AnimatedCreatureEgg>();
        }

        public void LoadPlanet(int idx)
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
        }

        private void BuildSlotsAndStrand(int slotCount)
        {
            foreach (var s in activeSlots) if (s != null) Destroy(s.gameObject);
            activeSlots.Clear();

            foreach (var b in resonanceBeams) if (b != null) Destroy(b.gameObject);
            resonanceBeams.Clear();

            float spacingX = (slotCount >= 5) ? 2.85f : 2.95f;
            float totalWidth = (slotCount - 1) * spacingX;
            float startX = 1.35f - (totalWidth * 0.5f);
            float slotY = 1.45f; // Ferah ve temiz aralık

            if (helixRibbon != null) Destroy(helixRibbon.gameObject);
            GameObject ribbonObj = new GameObject("DNAHelixStrandRibbon");
            ribbonObj.transform.SetParent(transform, false);
            helixRibbon = ribbonObj.AddComponent<DNAHelixStrandRibbon>();
            helixRibbon.Initialize(slotCount, startX, spacingX, slotY);

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObj = new GameObject($"GenomeSlot_{i}");
                slotObj.transform.SetParent(transform, false);
                slotObj.transform.position = new Vector3(startX + i * spacingX, slotY, 0f);
                slotObj.transform.localScale = Vector3.one * 0.95f;

                var slot = slotObj.AddComponent<AnimatedSlot>();
                slot.Initialize(i);
                slot.OnSlotClicked += HandleSlotClicked;

                activeSlots.Add(slot);

                if (i > 0)
                {
                    GameObject beamObj = new GameObject($"ResonanceBeam_{i - 1}_{i}");
                    beamObj.transform.SetParent(transform, false);
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

            int count = brokenGenes.Count;
            float spacingX = 2.95f;
            float startX = 1.35f - ((count - 1) * spacingX * 0.5f);
            float benchY = -0.58f; // Yukarıdaki yuvalarla ferah aralık

            for (int i = 0; i < count; i++)
            {
                GameObject pieceObj = new GameObject($"GenomePiece_{i}_{brokenGenes[i].geneName}");
                pieceObj.transform.SetParent(transform, false);
                Vector3 targetPos = new Vector3(startX + i * spacingX, benchY, 0f);
                pieceObj.transform.position = targetPos + Vector3.down * 2.5f;
                pieceObj.transform.localScale = Vector3.one * 0.82f;

                var piece = pieceObj.AddComponent<AnimatedGenePiece>();
                piece.Initialize(brokenGenes[i]);
                piece.SetTargetScale(Vector3.one * 0.82f);
                piece.MoveTo(targetPos, Quaternion.identity, 0.35f + i * 0.05f);
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

            int count = candidates.Count;
            float spacingX = 1.95f;
            float startX = 1.35f - ((count - 1) * spacingX * 0.5f);
            float trayY = -2.60f;

            for (int i = 0; i < count; i++)
            {
                GameObject candObj = new GameObject($"RepairCandidate_{i}_{candidates[i]}");
                candObj.transform.SetParent(transform, false);
                Vector3 worldPos = new Vector3(startX + i * spacingX, trayY, 0f);

                var cand = candObj.AddComponent<AnimatedRepairCandidate>();
                cand.Initialize(i, candidates[i], worldPos);
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
                        zoomedGenome.transform.position = new Vector3(stX + prevIdx * spX, -0.58f, 0f);
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

            Vector3 centerPos = new Vector3(0f, 0.72f, -1.8f);
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
                    Vector3 pos = new Vector3(startX + i * spacingX, trayY, -1.8f);
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
                    Vector3 pos = new Vector3(startX + i * spacingX, trayY, 0f);
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
            if (isSimulating || showSynthesisModal || candidate == null) return;
            if (Time.frameCount == lastClickFrame) return;
            lastClickFrame = Time.frameCount;

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

                // Odak modunda oyuncu baz yerleştiriyor
                bool zoomSuccess = zoomedGenome.TryRepairWith(candidate.Nucleotide);
                if (zoomSuccess)
                {
                    candidate.TriggerClickPulse();
                    audioManager?.PlayResonance();
                    audioManager?.PlaySnap();
                    repairFeedbackMessage = $"✨ HARİKA! {gData.geneName} ({candidate.Nucleotide}) ile onarıldı! İstersen tekrar değiştirebilir veya sarmala yerleştirebilirsin.";
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
                        repairFeedbackMessage = $"⚠️ UYUMSUZ BAZ! Kalan Onarım Hakkı: {gData.remainingRepairAttempts}/{gData.maxRepairAttempts}. {gData.repairHint}";
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
                repairFeedbackMessage = $"✨ HARİKA! {targetGenome.Data.geneName} onarıldı! Üzerine tıklayarak ana DNA sarmalına yerleştirin.";

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
                    repairFeedbackMessage = $"❌ EŞLEŞME HATALI! Kalan Hak: {rem}/3. {targetGenome.Data.repairHint}";
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
            Vector3 homePos = new Vector3(startX + idx * spacingX, -0.58f, 0f);
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
            if (showSynthesisModal) return true;
            Vector3 mp = DNAInput.MousePosition;
            float guiY = Screen.height - mp.y;
            if (isRepairZoomMode)
            {
                if (guiY < 120f) return true; // Üst laboratuvar başlık ve teşhis kartı alanı
                if (guiY > Screen.height - 62f) return true; // Alt aksiyon ve yerleştir butonu
                return false;
            }
            if (guiY < 48f) return true; // Üst gezegen & durum barı
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
            EnsurePuzzleInitialized();
            if (isSimulating || showSynthesisModal) return;

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
                                    matchDetails += $"Sol: [{leftPiece.Data.rightConnector} ⟷ {draggedPiece.Data.leftConnector} {(isMatch ? "✓ UYUMLU" : "✕")}] ";
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
                                    matchDetails += $"Sağ: [{draggedPiece.Data.rightConnector} ⟷ {rightPiece.Data.leftConnector} {(isMatch ? "✓ UYUMLU" : "✕")}] ";
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
                            dragInterlockMessage = $"🧬 [{draggedCandidate.Nucleotide}] Bazını {targetGene.Data.geneName} Genomuna Yerleştirmek İçin Bırakın!";
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
                            if (draggedPiece.transform.position.y < 0.2f)
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
            float screenW = Screen.width;
            float screenH = Screen.height;

            // Tam Ekran Genom Biyo-Onarım & İnceleme Modu
            if (isRepairZoomMode && zoomedGenome != null)
            {
                RenderRepairZoomHUD(screenW, screenH);
                return;
            }

            if (planets == null || planets.Count == 0)
                planets = DNAPuzzleSampleData.CreateSamplePlanets();

            PlanetData planet = manager != null ? manager.CurrentPlanet : null;

            int targetPlanetIdx = -1;
            bool triggerTestSim = false;
            bool triggerClearStrand = false;

            // 1. Üst Bar: Başlık, Canlı Gereksinimleri & Gezegen Butonları
            GUILayout.BeginArea(new Rect(18, 8, screenW - 36, 38));
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.35f, 0.9f, 1f);
            GUILayout.Label("🔬 BİYO-MÜHENDİSLİK LABORATUVARI // GENOM TERMİNALİ", EditorBold(13));
            GUI.color = Color.white;

            if (planet != null && manager != null)
            {
                GUILayout.Space(14);
                var curStats = manager.CalculateTotalStats();
                int curEnergy = manager.CalculateTotalEnergy();
                bool energyOk = curEnergy <= planet.maxEnergyBudget;

                GUI.color = energyOk ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.35f, 0.35f);
                GUILayout.Label($"⚡ {curEnergy}/{planet.maxEnergyBudget}E", EditorBold(12));

                GUI.color = new Color(0.55f, 0.65f, 0.75f);
                GUILayout.Label("•", EditorBold(12));

                foreach (var req in planet.requirements)
                {
                    int val = curStats.ContainsKey(req.stat) ? curStats[req.stat] : 0;
                    bool ok = val >= req.requiredValue;
                    GUI.color = ok ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.45f, 0.45f);
                    string sName = DNAHelper.GetStatDisplayName(req.stat)
                        .Replace("Direnci", "")
                        .Replace("Dayanımı", "")
                        .Replace("Kalkanı", "")
                        .Replace("Filtresi", "")
                        .Trim();
                    GUILayout.Label($"{sName}: {val}/{req.requiredValue}{(ok ? "✔" : "")}", EditorBold(12));
                }
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            GUI.color = new Color(1f, 0.88f, 0.2f);
            GUILayout.Label($"⭐ {ProgressionManager.GetTotalStars()}/9", EditorBold(12));
            GUI.color = Color.white;
            GUILayout.Space(8);

            for (int i = 0; i < planets.Count; i++)
            {
                bool isSel = (i == currentPlanetIdx);
                GUI.color = isSel ? Color.cyan : new Color(0.7f, 0.7f, 0.7f);
                string shortPName = i == 0 ? "🔥 ODA-1 Pyros" : (i == 1 ? "❄️ ODA-2 Cryo" : "☣️ ODA-3 Toxis");
                string stars = ProgressionManager.FormatStarString(ProgressionManager.GetStars(i));

                if (GUILayout.Button($"{shortPName} {stars}", GUILayout.Height(26), GUILayout.Width(130)))
                {
                    targetPlanetIdx = i;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 1.5. Orta Rehber & Onarım Bildirim Alanı
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

            float bannerW = Mathf.Min(680f, screenW - 320f);
            float bannerX = 20f;
            GUILayout.BeginArea(new Rect(bannerX, 48, bannerW, 44));
            GUILayout.BeginVertical();

            string stepTitle = (repairedCount < 4)
                ? (deformedCount > 0 
                    ? $"🛠️ 1. ADIM: GENOMLARI ONAR ({repairedCount}/4 Hazır, ☣️ {deformedCount} Genom Yandı!)"
                    : $"🛠️ 1. ADIM: BOZUK GENOMLARI ONAR ({repairedCount}/4 Hazır)")
                : "🎉 2. ADIM: TÜM GENOMLAR ONARILDI! Tıklayarak Ana DNA Sarmalına yerleştirin.";
            Color stepCol = (deformedCount > 0) ? new Color(1f, 0.35f, 0.25f) :
                            (repairedCount < 4) ? new Color(1f, 0.75f, 0.2f) : new Color(0.3f, 1f, 0.5f);

            GUI.color = stepCol;
            GUILayout.Label(stepTitle, EditorBold(12));
            GUI.color = new Color(0.85f, 0.95f, 1f);
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

                // Aktif Sinerjiler Listesi
                var activeSyns = manager.GetActiveSynergies();
                if (activeSyns != null && activeSyns.Count > 0)
                {
                    GUILayout.Space(6);
                    GUI.color = new Color(0.3f, 1f, 0.7f);
                    GUILayout.Label("⚡ AKTİF SİNERJİLER:", EditorBold(11));
                    foreach (var syn in activeSyns)
                    {
                        GUI.color = syn.auraColor;
                        GUILayout.Label($"• {syn.synergyName}", EditorBold(10));
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
            float bottomW = Mathf.Min(840f, screenW - 40f);
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
                    GUILayout.Label($"🔬 {g.geneName} ({DNAHelper.GetCategoryDisplayName(g.category)}): {g.description} [⚡{g.energyCost}E | {g.leftConnector}⟷{g.rightConnector}]", EditorBold(11));
                }
            }
            else
            {
                GUI.color = new Color(0.40f, 0.88f, 1f);
                GUILayout.Label("💡 REHBER: 1. Bozuk genoma tıkla  ➜  2. Alttan doğru bazı seç (A↔T, C↔G)  ➜  3. Sarmala yerleştir!", EditorBold(11));
            }
            GUI.color = Color.white;
            GUILayout.EndArea();

            // 3.5. Ana Ekranda Onarılmış / Sarmaldaki Genomlar İçin Hızlı "Sarmala Tak" ve "Tekrar Düzenle" Butonları
            AnimatedGenePiece quickEquipPiece = null;
            AnimatedGenePiece quickEditPiece = null;
            AnimatedGenePiece quickUnequipPiece = null;
            Camera cam = puzzleCamera != null ? puzzleCamera : Camera.main;
            if (cam != null && !isSimulating && !showSynthesisModal)
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
            if (targetPlanetIdx >= 0)
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

            bool triggerClose = false;
            bool triggerEquip = false;
            bool triggerResetRepair = false;
            bool triggerPurify = false;
            AnimatedGenePiece nextBrokenToZoom = null;

            // 1. Üst Laboratuvar Başlık Çubuğu + Tüm 4 Genom İçin Hızlı Geçiş Sekmeleri (Onarılanlar ve Sarmaldakiler Dahil!)
            GUILayout.BeginArea(new Rect(20, 10, screenW - 40, 42));
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.35f, 0.9f, 1f);
            GUILayout.Label("🔬 GENOM ONARIM & DÜZENLEME LABI", EditorBold(13));
            GUI.color = Color.white;

            GUILayout.Space(10);
            for (int i = 0; i < poolPieces.Count; i++)
            {
                var p = poolPieces[i];
                if (p == null || p.Data == null) continue;
                bool isCur = (p == zoomedGenome);
                bool isRep = p.Data.isRepaired || !p.Data.isBroken;
                bool isDef = p.Data.isDeformed;

                string badge = isDef ? "☣️" : (p.IsEquipped ? "🔗" : (isRep ? "✅" : "🛠️"));
                Color tabCol = isCur ? new Color(0.2f, 0.8f, 1f) : 
                               (isDef ? new Color(0.75f, 0.15f, 0.25f) : 
                               (isRep ? new Color(0.2f, 0.55f, 0.35f) : new Color(0.45f, 0.3f, 0.2f)));
                GUI.backgroundColor = tabCol;
                if (GUILayout.Button($"{badge} {p.Data.geneName}", GUILayout.Height(26), GUILayout.Width(132)))
                {
                    if (!isCur) nextBrokenToZoom = p;
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("✕ KAPAT", GUILayout.Height(30), GUILayout.Width(100)))
            {
                triggerClose = true;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 2. Teşhis ve Hasar Kartı (Üst Orta)
            float cardW = Mathf.Min(740, screenW - 40);
            float cardX = (screenW - cardW) * 0.5f;
            GUILayout.BeginArea(new Rect(cardX, 52, cardW, 64), "box");
            GUILayout.BeginVertical();

            if (data.isDeformed)
            {
                GUI.color = new Color(1f, 0.25f, 0.25f);
                GUILayout.Label($"☣️ AŞIRI YÜKLENME HASARI: {data.geneName} BİYOLOJİK OLARAK YANDI!", EditorBold(12));
                GUI.color = new Color(1f, 0.85f, 0.45f);
                GUILayout.Label($"Kalan Biyo-Bütünlük: 🖤🖤🖤 (0/3 Hak Tükendi)  |  ⚠️ Bu bozuk gen sarmala takılırsa İSTENMEYEN BİR HİLKAT GARİBESİ doğar!", EditorLabel(11));
            }
            else if (data.isRepaired || !data.isBroken)
            {
                GUI.color = new Color(0.3f, 1f, 0.5f);
                GUILayout.Label($"✅ GENOM BÜTÜNLÜĞÜ SAĞLANDI! {data.geneName} aktif ve kusursuz.", EditorBold(12));
                GUI.color = new Color(0.85f, 0.95f, 1f);
                GUILayout.Label($"Statlar: {data.GetStatsSummary()}  |  Metabolik Enerji: {data.energyCost}E  |  Biyo-Bütünlük: ❤️❤️❤️ (3/3 Hak)", EditorLabel(11));
            }
            else
            {
                GUI.color = new Color(1f, 0.45f, 0.2f);
                GUILayout.Label($"⚠️ HASAR TESPİTİ: {data.defectDescription}", EditorBold(12));
                GUI.color = new Color(0.4f, 0.9f, 1f);
                string hearts = "";
                for (int h = 0; h < data.maxRepairAttempts; h++)
                {
                    hearts += (h < data.remainingRepairAttempts) ? "❤️" : "🖤";
                }
                GUILayout.Label($"🛡️ BİYO-BÜTÜNLÜK: {hearts} ({data.remainingRepairAttempts}/3 Hak)  |  💡 İPUCU: {data.repairHint}", EditorBold(11));
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUILayout.EndArea();

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
                float hintW = 680;
                float hintX = (screenW - hintW) * 0.5f;
                GUI.color = new Color(0.35f, 0.95f, 1f);
                string hearts = "";
                for (int h = 0; h < data.maxRepairAttempts; h++) hearts += (h < data.remainingRepairAttempts) ? "❤️" : "🖤";
                GUI.Label(new Rect(hintX, screenH - 42f, hintW, 24), $"▼ ALTTAN DOĞRU BAZ KAPSÜLÜNE TIKLAYARAK GENOMU ONARIN | Kalan Hak: {hearts} ({data.remainingRepairAttempts}/3) ▼", EditorBold(11));
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
            if (triggerPurify && zoomedGenome != null)
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
        #endregion
    }
}
