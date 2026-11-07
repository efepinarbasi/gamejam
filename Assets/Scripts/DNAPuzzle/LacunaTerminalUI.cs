using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Lacuna Genetics: Game Design Document (Alperen) birebir uyumlu
    /// Sequencer (Dizileyici), Rulebook (Kural Kitabı & Yan Yana Kıyaslama),
    /// Capsulator (QTE Mühürleme) ve 3D Yaratık Sentezleme Arayüzü.
    /// </summary>
    public class LacunaTerminalUI
    {
        private readonly DNAAnimatedPuzzleController controller;
        private readonly DNAPuzzleAudio audio;

        // Oyun İlerlemesi ve Durumu
        public int CurrentCreatureIndex { get; private set; } = 0; // 0..9 (1..10)
        public int RemainingLives { get; private set; } = 2;       // 2 Can (1. hata yaratığı öldürür, 2. hata Game Over)
        public bool[] CreatureCompleted { get; private set; } = new bool[10];
        public List<string> UnlockedClues { get; private set; } = new List<string>();

        // Aktif Yaratığın 4 Slottaki Düzenlenebilir Gen Dizilimleri (Z, V, K, P)
        private Dictionary<LacunaSlotType, EditableGeneStrand> activeStrands = new Dictionary<LacunaSlotType, EditableGeneStrand>();
        private LacunaSlotType selectedSequencerSlot = LacunaSlotType.Z;

        // Nükleotid Paleti Seçimi
        private LacunaShape selectedBrushShape = LacunaShape.Circle;
        private bool autoFillPartnerOnEdit = false;

        // Kural Kitabı (Rulebook) Durumu
        private bool showRulebook = true;
        private int rulebookTab = 0; // 0: Gen Dizilimleri, 1: Gezegen Kuralları & Sıralama, 2: Hikaye & Rehber
        private LacunaSlotType rulebookSlotFilter = LacunaSlotType.Z;
        private string compareGeneA = "";
        private string compareGeneB = "";

        // Geri Bildirim Mesajı
        private string statusMessage = "1. ADIM: Gezegeni incele  ➔  2. ADIM: Eksik bazları (?) doldur  ➔  3. ADIM: Gezegene uymayan geni bulup düzelt  ➔  4. ADIM: Kapsülle!";
        private Color statusColor = new Color(0.35f, 0.95f, 1f);

        // QTE (Quick-Time Event) Kapsülleme Durumu
        private bool isQteActive = false;
        private float qteCursorPos = 0f;
        private float qteDirection = 1f;
        private float qteTargetMin = 0.38f;
        private float qteTargetMax = 0.62f;
        private float qteSpeedValue = 0.9f;

        // Sonuç / Modal Durumu
        private bool showResultModal = false;
        private bool lastAttemptSuccess = false;
        private bool isGameOver = false;
        private bool isVictoryEnding = false;
        private string resultTitle = "";
        private string resultDetail = "";
        private string resultLoreClue = "";
        private string finalGeneZ = "";
        private string finalGeneV = "";
        private string finalGeneK = "";
        private string finalGeneP = "";

        // 1x1 Düz Renk Dokusu (Özel arka plan kutuları ve QTE barı için)
        private Texture2D solidWhiteTex;

        public LacunaTerminalUI(DNAAnimatedPuzzleController controller, DNAPuzzleAudio audio)
        {
            this.controller = controller;
            this.audio = audio;
            LoadCreature(0);
        }

        private Texture2D GetWhiteTex()
        {
            if (solidWhiteTex == null)
            {
                solidWhiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                for (int i = 0; i < 4; i++) solidWhiteTex.SetPixel(i % 2, i / 2, Color.white);
                solidWhiteTex.Apply();
            }
            return solidWhiteTex;
        }

        public bool IsModalOrQteOpen => isQteActive || showResultModal;

        public bool HandleEscapeOrBack()
        {
            if (isQteActive)
            {
                isQteActive = false;
                return true;
            }
            if (showResultModal && !isGameOver)
            {
                showResultModal = false;
                return true;
            }
            return false;
        }

        public void LoadCreature(int index)
        {
            var roster = LacunaGeneticsDatabase.GetCreatureRoster();
            CurrentCreatureIndex = Mathf.Clamp(index, 0, roster.Count - 1);
            var entry = roster[CurrentCreatureIndex];

            activeStrands.Clear();
            foreach (LacunaSlotType slot in Enum.GetValues(typeof(LacunaSlotType)))
            {
                string gName = entry.GetInitialGeneName(slot);
                var def = LacunaGeneticsDatabase.GetGeneByName(gName);
                if (def != null)
                {
                    activeStrands[slot] = new EditableGeneStrand(def, entry.gaps);
                }
            }

            selectedSequencerSlot = LacunaSlotType.Z;
            rulebookSlotFilter = LacunaSlotType.Z;

            var slotZGenes = LacunaGeneticsDatabase.GetGenesBySlot(LacunaSlotType.Z);
            if (slotZGenes.Count >= 2)
            {
                compareGeneA = slotZGenes[0].geneName;
                compareGeneB = slotZGenes[1].geneName;
            }

            isQteActive = false;
            showResultModal = false;
            isGameOver = false;

            statusMessage = $"🔬 NUMUNE #{entry.number}: {entry.creatureName} ({entry.planetName}) masaya alındı. Önce eksik bazları (?) tamamlayın!";
            statusColor = new Color(0.35f, 0.95f, 1f);
        }

        public void ResetCurrentSlotToInitial()
        {
            var roster = LacunaGeneticsDatabase.GetCreatureRoster();
            var entry = roster[CurrentCreatureIndex];
            string gName = entry.GetInitialGeneName(selectedSequencerSlot);
            var def = LacunaGeneticsDatabase.GetGeneByName(gName);
            if (def != null)
            {
                activeStrands[selectedSequencerSlot] = new EditableGeneStrand(def, entry.gaps);
                audio?.PlaySnap();
                statusMessage = $"↺ Slot {selectedSequencerSlot} ({gName}) başlangıçtaki hasarlı durumuna sıfırlandı.";
                statusColor = new Color(1f, 0.85f, 0.3f);
            }
        }

        public void Update()
        {
            // Klavye kısayolları ile hızlı şekil seçimi (1: ●, 2: ▲, 3: ■, 4: ◆)
            if (!isQteActive && !showResultModal)
            {
                if (DNAInput.GetKeyDown(KeyCode.Alpha1) || DNAInput.GetKeyDown(KeyCode.Keypad1)) selectedBrushShape = LacunaShape.Circle;
                else if (DNAInput.GetKeyDown(KeyCode.Alpha2) || DNAInput.GetKeyDown(KeyCode.Keypad2)) selectedBrushShape = LacunaShape.Triangle;
                else if (DNAInput.GetKeyDown(KeyCode.Alpha3) || DNAInput.GetKeyDown(KeyCode.Keypad3)) selectedBrushShape = LacunaShape.Square;
                else if (DNAInput.GetKeyDown(KeyCode.Alpha4) || DNAInput.GetKeyDown(KeyCode.Keypad4)) selectedBrushShape = LacunaShape.Diamond;
                else if (DNAInput.GetKeyDown(KeyCode.Tab))
                {
                    int nextSlot = ((int)selectedSequencerSlot + 1) % 4;
                    selectedSequencerSlot = (LacunaSlotType)nextSlot;
                    rulebookSlotFilter = selectedSequencerSlot;
                    audio?.PlaySnap();
                }
            }

            if (isQteActive)
            {
                qteCursorPos += qteDirection * qteSpeedValue * Time.deltaTime;
                if (qteCursorPos >= 1f)
                {
                    qteCursorPos = 1f;
                    qteDirection = -1f;
                }
                else if (qteCursorPos <= 0f)
                {
                    qteCursorPos = 0f;
                    qteDirection = 1f;
                }

                if (DNAInput.GetKeyDown(KeyCode.Space) || DNAInput.GetKeyDown(KeyCode.Return))
                {
                    CompleteQteAttempt();
                }
            }
        }

        private void StartCapsulateQTE()
        {
            var roster = LacunaGeneticsDatabase.GetCreatureRoster();
            var entry = roster[CurrentCreatureIndex];

            isQteActive = true;
            qteCursorPos = 0.05f;
            qteDirection = 1f;

            switch (entry.qteSpeed)
            {
                case LacunaQteSpeed.Slow:
                    qteSpeedValue = 0.85f;
                    qteTargetMin = 0.36f;
                    qteTargetMax = 0.64f;
                    break;
                case LacunaQteSpeed.Medium:
                    qteSpeedValue = 1.30f;
                    qteTargetMin = 0.40f;
                    qteTargetMax = 0.60f;
                    break;
                case LacunaQteSpeed.Fast:
                    qteSpeedValue = 1.80f;
                    qteTargetMin = 0.42f;
                    qteTargetMax = 0.58f;
                    break;
                case LacunaQteSpeed.Fastest:
                    qteSpeedValue = 2.35f;
                    qteTargetMin = 0.44f;
                    qteTargetMax = 0.56f;
                    break;
            }

            audio?.PlayResonance();
        }

        private void CompleteQteAttempt()
        {
            isQteActive = false;
            bool qteHit = (qteCursorPos >= qteTargetMin && qteCursorPos <= qteTargetMax);

            var roster = LacunaGeneticsDatabase.GetCreatureRoster();
            var entry = roster[CurrentCreatureIndex];
            var planet = LacunaGeneticsDatabase.GetPlanetByName(entry.planetName);

            // 1. QTE Zamanlama Kontrolü
            if (!qteHit)
            {
                RegisterFailure(
                    "✖ KAPSÜL MÜHÜRLEME (QTE) BAŞARISIZ!",
                    $"Zamanlama çubuğunu yeşil mühürleme bölgesinde yakalayamadınız! {entry.creatureName} kapsül basınç kaybından dolayı hayatını kaybetti."
                );
                return;
            }

            // 2. Tüm 4 Slotta Eksik Baz, Eşlenik ve Geçerli Gen Kontrolü
            var matchedGenes = new Dictionary<LacunaSlotType, LacunaGeneDefinition>();
            foreach (LacunaSlotType slot in Enum.GetValues(typeof(LacunaSlotType)))
            {
                var strand = activeStrands[slot];
                if (strand.HasAnyMissing())
                {
                    RegisterFailure(
                        $"✖ SLOT {slot} DİZİLİMİNDE EKSİK NÜKLEOTİD VAR!",
                        $"Slot {slot} içinde doldurulmamış ({strand.GetMissingCount()} adet '?') boşluk bıraktınız! Mühürleme öncesi tüm baz çiftleri eksiksiz tamamlanmalıdır."
                    );
                    return;
                }

                if (!strand.AreAllPairsValidComplements())
                {
                    RegisterFailure(
                        $"✖ SLOT {slot} EŞLENİK KURALI İHLALİ!",
                        $"Slot {slot} içinde yanlış eşleşmiş baz çifti var! Unutmayın: ● (Daire) her zaman ■ (Kare) ile, ▲ (Üçgen) her zaman ◆ (Elmas) ile eşleşmelidir."
                    );
                    return;
                }

                var matched = LacunaGeneticsDatabase.MatchStrandToGene(slot, strand.top, strand.bottom);
                if (matched == null)
                {
                    RegisterFailure(
                        $"✖ SLOT {slot} TANIMSIZ GEN DİZİLİMİ!",
                        $"Slot {slot} üzerindeki dizilim Kural Kitabındaki 5 geçerli {slot} geninden hiçbiriyle eşleşmiyor! Yanlış bir çifti değiştirmiş olabilirsiniz."
                    );
                    return;
                }

                matchedGenes[slot] = matched;
            }

            // 3. Gezegen Uyum Kontrolü (FABAUP, SNIBBLEX, OOMGARTH)
            foreach (LacunaSlotType slot in Enum.GetValues(typeof(LacunaSlotType)))
            {
                var g = matchedGenes[slot];
                if (!planet.IsGeneAcceptable(slot, g.geneName))
                {
                    string ruleText = planet.GetRuleForSlot(slot);
                    RegisterFailure(
                        $"✖ GEZEGEN UYUMSUZLUĞU: {g.geneName} ({planet.planetName} İçin Uygun Değil!)",
                        $"{entry.creatureName}, {planet.planetName} gezegeninde '{g.geneName}' (Slot {slot} - {g.bodyTraitTR}) geni yüzünden hayatta kalamadı!\n" +
                        $"• {planet.planetName} Slot {slot} Kuralı: {ruleText}\n" +
                        $"• Biyolojik Neden: {planet.rulebookReasonTR}\n" +
                        $"• İpucu: {entry.trapNoteTR}"
                    );
                    return;
                }
            }

            // 4. TAM BAŞARI!
            lastAttemptSuccess = true;
            showResultModal = true;
            CreatureCompleted[CurrentCreatureIndex] = true;

            finalGeneZ = matchedGenes[LacunaSlotType.Z].geneName;
            finalGeneV = matchedGenes[LacunaSlotType.V].geneName;
            finalGeneK = matchedGenes[LacunaSlotType.K].geneName;
            finalGeneP = matchedGenes[LacunaSlotType.P].geneName;

            if (!UnlockedClues.Contains(entry.landlordClueTR))
            {
                UnlockedClues.Add(entry.landlordClueTR);
            }

            bool allDone = true;
            for (int i = 0; i < 10; i++)
            {
                if (!CreatureCompleted[i]) { allDone = false; break; }
            }
            if (CurrentCreatureIndex == 9 || allDone)
            {
                isVictoryEnding = true;
            }

            resultTitle = $"✔ KAPSÜL MÜHÜRLENDİ: #{entry.number} {entry.creatureName} KURTARILDI!";
            resultDetail =
                $"Gezegen: {planet.planetName} ({planet.subtitleTR})\n" +
                $"Onarılan Gen Dizilimi:\n" +
                $"  • Slot Z (Boy): {finalGeneZ} ({matchedGenes[LacunaSlotType.Z].bodyTraitTR})\n" +
                $"  • Slot V (Göz): {finalGeneV} ({matchedGenes[LacunaSlotType.V].bodyTraitTR})\n" +
                $"  • Slot K (Deri): {finalGeneK} ({matchedGenes[LacunaSlotType.K].bodyTraitTR})\n" +
                $"  • Slot P (Uzuv): {finalGeneP} ({matchedGenes[LacunaSlotType.P].bodyTraitTR})";
            resultLoreClue = entry.landlordClueTR;

            audio?.PlayVictory();

            // 3D Yaratık Modelini Kuluçka Odasında (ve kapsülde) 4 final genine göre oluştur!
            controller.SpawnLacunaCreatureInLabChamber(entry.creatureName, planet.planetName, finalGeneZ, finalGeneV, finalGeneK, finalGeneP);
        }

        private void RegisterFailure(string title, string detail)
        {
            lastAttemptSuccess = false;
            showResultModal = true;
            RemainingLives--;
            audio?.PlayMismatch();

            resultTitle = title;
            if (RemainingLives > 0)
            {
                isGameOver = false;
                resultDetail = detail + "\n\n⚠️ 1 CAN KAYBETTİNİZ! (Kalan Can: ❤️🖤)\nEv Sahibi: \"Bu ilk ve son uyarım mühendis. Ailenin fotoğraflarına bakmayı bırak ve bu numuneyi düzgün onar. Bir hata daha yaparsan ölürsün.\"";
                resultLoreClue = "";
            }
            else
            {
                isGameOver = true;
                resultDetail = detail + "\n\n💀 İKİNCİ HATA — OYUN BİTTİ (🖤🖤)\nEv Sahibi eldivenli ellerini kavuşturdu ve laboratuvarın yaşam desteğini kapattı.";
                resultLoreClue = "";
            }
        }

        // =========================================================================
        // ANA EKRAN ÇİZİMİ (SEQUENCER + RULEBOOK + QTE + MODALS)
        // =========================================================================
        public void OnGUI(float screenW, float screenH, bool isLabMode)
        {
            // Koyu Sci-Fi Terminal Arka Plan Örtüsü
            DrawRect(new Rect(0, 0, screenW, screenH), new Color(0.02f, 0.04f, 0.08f, 0.92f));

            var roster = LacunaGeneticsDatabase.GetCreatureRoster();
            var entry = roster[CurrentCreatureIndex];
            var planet = LacunaGeneticsDatabase.GetPlanetByName(entry.planetName);

            // 1. ÜST BAR (Başlık, Canlı Seçici, Gezegen, Can Durumu ve Odaya Dön)
            DrawTopBar(screenW, entry, planet, isLabMode);

            // 2. EV SAHİBİ (LANDLORD) MESAJ BARI
            DrawLandlordBar(screenW, entry, planet);

            // 3. ANA ÇALIŞMA ALANI (Sol: Sequencer | Sağ: Rulebook)
            float topOffset = 94f;
            float workspaceH = screenH - topOffset - 10f;

            if (showRulebook)
            {
                float leftW = Mathf.Floor((screenW - 30f) * 0.54f);
                float rightW = (screenW - 30f) - leftW - 10f;

                DrawSequencerPanel(new Rect(12f, topOffset, leftW, workspaceH), entry, planet);
                DrawRulebookPanel(new Rect(12f + leftW + 10f, topOffset, rightW, workspaceH), entry, planet);
            }
            else
            {
                DrawSequencerPanel(new Rect(12f, topOffset, screenW - 24f, workspaceH), entry, planet);
            }

            // 4. QTE KAPSÜLLEME OVERLAY
            if (isQteActive)
            {
                DrawQteOverlay(screenW, screenH, entry);
            }

            // 5. SONUÇ / BAŞARI / HATA / FİNAL MODALI
            if (showResultModal)
            {
                DrawResultModal(screenW, screenH, entry, planet, isLabMode);
            }
        }

        private void DrawTopBar(float screenW, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet, bool isLabMode)
        {
            Rect barRect = new Rect(12f, 8f, screenW - 24f, 40f);
            DrawBoxWithBorder(barRect, new Color(0.04f, 0.08f, 0.15f, 0.98f), new Color(0.15f, 0.55f, 0.85f, 0.8f));

            GUILayout.BeginArea(new Rect(barRect.x + 12f, barRect.y + 6f, barRect.width - 24f, 30f));
            GUILayout.BeginHorizontal();

            GUI.color = new Color(0.25f, 0.95f, 1f);
            GUILayout.Label("🧬 LACUNA GENETICS", BoldStyle(14), GUILayout.Width(165f));

            // Canlı Gezinti (< #1/10 WIBBLE >)
            GUI.backgroundColor = new Color(0.15f, 0.35f, 0.55f);
            if (GUILayout.Button("◀", BoldStyle(12), GUILayout.Width(28f), GUILayout.Height(26f)))
            {
                int prev = (CurrentCreatureIndex - 1 + 10) % 10;
                LoadCreature(prev);
                audio?.PlaySnap();
            }

            bool isDone = CreatureCompleted[CurrentCreatureIndex];
            GUI.color = isDone ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.92f, 0.35f);
            string doneBadge = isDone ? "✔ " : "";
            GUILayout.Label($" {doneBadge}NUMUNE #{entry.number}/10: {entry.creatureName} ", BoldStyle(13), GUILayout.Width(215f));
            GUI.color = Color.white;

            if (GUILayout.Button("▶", BoldStyle(12), GUILayout.Width(28f), GUILayout.Height(26f)))
            {
                int next = (CurrentCreatureIndex + 1) % 10;
                LoadCreature(next);
                audio?.PlaySnap();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(12f);

            // Gezegen Rozeti
            if (planet != null)
            {
                GUI.color = planet.themeColor;
                GUILayout.Label($"🪐 GEZEGEN: {planet.planetName}", BoldStyle(13), GUILayout.Width(175f));
                GUI.color = Color.white;
            }

            // Can (Hata Hakkı) Göstergesi
            string hearts = RemainingLives >= 2 ? "❤️❤️ (2/2 CAN)" : (RemainingLives == 1 ? "❤️🖤 (1/2 CAN - SON HAK!)" : "🖤🖤 (0/2)");
            GUI.color = RemainingLives >= 2 ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.35f, 0.35f);
            GUILayout.Label(hearts, BoldStyle(12), GUILayout.Width(175f));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // Kural Kitabı Aç/Kapat
            GUI.backgroundColor = showRulebook ? new Color(0.15f, 0.75f, 0.95f) : new Color(0.25f, 0.35f, 0.5f);
            if (GUILayout.Button(showRulebook ? "📖 Kural Kitabı [AÇIK]" : "📖 Kural Kitabını Aç", BoldStyle(11), GUILayout.Width(155f), GUILayout.Height(26f)))
            {
                showRulebook = !showRulebook;
                audio?.PlaySnap();
            }
            GUI.backgroundColor = Color.white;

            if (isLabMode)
            {
                GUILayout.Space(6f);
                GUI.backgroundColor = new Color(0.85f, 0.22f, 0.25f);
                if (GUILayout.Button("↩️ ODAYA DÖN (ESC)", BoldStyle(11), GUILayout.Width(145f), GUILayout.Height(26f)))
                {
                    controller.ReturnToLabRoom();
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawLandlordBar(float screenW, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet)
        {
            Rect rect = new Rect(12f, 52f, screenW - 24f, 36f);
            DrawBoxWithBorder(rect, new Color(0.06f, 0.05f, 0.12f, 0.95f), new Color(0.45f, 0.30f, 0.75f, 0.7f));

            GUI.color = new Color(0.88f, 0.80f, 1f);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), entry.landlordIntroTR, BoldStyle(11));
            GUI.color = Color.white;
        }

        // =========================================================================
        // SOL PANEL: SEQUENCER (DİZİLEYİCİ & ÇİFT ZİNCİR ONARIM EKRANI)
        // =========================================================================
        private void DrawSequencerPanel(Rect panelRect, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet)
        {
            DrawBoxWithBorder(panelRect, new Color(0.03f, 0.06f, 0.12f, 0.96f), new Color(0.15f, 0.65f, 0.95f, 0.85f));

            float innerX = panelRect.x + 14f;
            float innerW = panelRect.width - 28f;
            float curY = panelRect.y + 10f;

            // Başlık
            GUI.color = new Color(0.3f, 0.95f, 1f);
            GUI.Label(new Rect(innerX, curY, innerW, 22f), "🔬 1. DİZİLEYİCİ (SEQUENCER) — GEN SLOTU SEÇİN (Z / V / K / P):", BoldStyle(13));
            GUI.color = Color.white;
            curY += 26f;

            // 4 Slot Sekmesi (Z: 4 çift, V: 6 çift, K: 8 çift, P: 10 çift)
            float tabW = (innerW - 18f) / 4f;
            int idx = 0;
            foreach (LacunaSlotType slot in Enum.GetValues(typeof(LacunaSlotType)))
            {
                var st = activeStrands[slot];
                var matched = LacunaGeneticsDatabase.MatchStrandToGene(slot, st.top, st.bottom);
                bool hasMissing = st.HasAnyMissing();
                bool hasInvalid = st.GetInvalidPairCount() > 0;

                string stateTag;
                Color tabColor;

                if (hasInvalid)
                {
                    stateTag = "✖ HATA";
                    tabColor = new Color(0.85f, 0.20f, 0.25f);
                }
                else if (hasMissing)
                {
                    stateTag = $"⚠️ {st.GetMissingCount()} EKSİK";
                    tabColor = new Color(0.85f, 0.55f, 0.12f);
                }
                else if (matched != null)
                {
                    stateTag = matched.geneName;
                    tabColor = new Color(0.15f, 0.68f, 0.45f);
                }
                else
                {
                    stateTag = "❓ TANIMSIZ";
                    tabColor = new Color(0.65f, 0.30f, 0.75f);
                }

                bool isSelected = (selectedSequencerSlot == slot);
                Rect tRect = new Rect(innerX + idx * (tabW + 6f), curY, tabW, 38f);

                DrawBoxWithBorder(
                    tRect,
                    isSelected ? new Color(tabColor.r * 0.55f, tabColor.g * 0.55f, tabColor.b * 0.55f, 0.98f) : new Color(0.06f, 0.10f, 0.18f, 0.95f),
                    isSelected ? Color.cyan : tabColor
                );

                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.01f);
                if (GUI.Button(tRect, GUIContent.none))
                {
                    selectedSequencerSlot = slot;
                    rulebookSlotFilter = slot;
                    audio?.PlaySnap();
                }
                GUI.backgroundColor = Color.white;

                GUI.color = isSelected ? Color.white : new Color(0.8f, 0.88f, 0.95f);
                GUI.Label(new Rect(tRect.x, tRect.y + 3f, tRect.width, 16f), $"SLOT {slot} ({st.pairCount} Çift)", CenteredBoldStyle(11));
                GUI.color = isSelected ? new Color(0.4f, 1f, 0.75f) : tabColor;
                GUI.Label(new Rect(tRect.x, tRect.y + 19f, tRect.width, 16f), stateTag, CenteredBoldStyle(11));
                GUI.color = Color.white;

                idx++;
            }
            curY += 46f;

            // Aktif Slottaki Genin Durum & Tanımlama Kutusu
            var strand = activeStrands[selectedSequencerSlot];
            var currentMatchedGene = LacunaGeneticsDatabase.MatchStrandToGene(selectedSequencerSlot, strand.top, strand.bottom);
            var compatibleGenes = LacunaGeneticsDatabase.FindCompatibleGenesForDamagedStrand(selectedSequencerSlot, strand.top, strand.bottom);

            Rect infoRect = new Rect(innerX, curY, innerW, 48f);
            DrawBoxWithBorder(infoRect, new Color(0.04f, 0.09f, 0.17f, 0.95f), new Color(0.2f, 0.5f, 0.8f, 0.6f));

            if (strand.GetInvalidPairCount() > 0)
            {
                GUI.color = new Color(1f, 0.35f, 0.35f);
                GUI.Label(new Rect(infoRect.x + 10f, infoRect.y + 6f, infoRect.width - 150f, 36f),
                    $"✖ EŞLENİK KURALI İHLALİ: Bu gende {strand.GetInvalidPairCount()} çift birbiriyle uyuşmuyor!\n" +
                    $"Kural: ● (Daire) her zaman ■ (Kare) ile, ▲ (Üçgen) her zaman ◆ (Elmas) ile eşleşmelidir.",
                    BoldStyle(11));
            }
            else if (strand.HasAnyMissing())
            {
                string compatNames = compatibleGenes.Count > 0
                    ? string.Join(", ", compatibleGenes.ConvertAll(g => g.geneName))
                    : "Yok (Mevcut bazları kontrol edin)";
                GUI.color = new Color(1f, 0.82f, 0.25f);
                GUI.Label(new Rect(infoRect.x + 10f, infoRect.y + 6f, infoRect.width - 150f, 36f),
                    $"⚠️ HASARLI DİZİLİM: {strand.GetMissingCount()} adet eksik baz (?) var. Alttan şekil seçip boşluklara tıklayın.\n" +
                    $"🔎 Kural Kitabında Bu Dizilimle Uyumlu Gen: [ {compatNames} ]",
                    BoldStyle(11));
            }
            else if (currentMatchedGene != null)
            {
                GUI.color = new Color(0.25f, 1f, 0.55f);
                GUI.Label(new Rect(infoRect.x + 10f, infoRect.y + 6f, infoRect.width - 150f, 36f),
                    $"🧬 TANIMLANAN GEN: [ {currentMatchedGene.geneName} ] — Tüm eşlenikler (●↔■, ▲↔◆) kusursuz!\n" +
                    $"📌 Şimdi Kural Kitabından '{planet.planetName}' gezegeninin Slot {selectedSequencerSlot} için '{currentMatchedGene.geneName}' kabul edip etmediğini kontrol edin.",
                    BoldStyle(11));
            }
            else
            {
                GUI.color = new Color(0.95f, 0.55f, 1f);
                GUI.Label(new Rect(infoRect.x + 10f, infoRect.y + 6f, infoRect.width - 150f, 36f),
                    $"❓ TANIMSIZ GEN DİZİLİMİ: Tüm eşlenikler doğru (●↔■, ▲↔◆) fakat bu dizilim Kural Kitabındaki 5 Slot {selectedSequencerSlot} geninden hiçbirine ait değil!\n" +
                    $"Sağdaki Kural Kitabından hedef genin dizilimini inceleyip 1 veya 2 çifti düzeltin.",
                    BoldStyle(11));
            }
            GUI.color = Color.white;

            // Sıfırla Butonu
            GUI.backgroundColor = new Color(0.35f, 0.45f, 0.65f);
            if (GUI.Button(new Rect(infoRect.xMax - 135f, infoRect.y + 10f, 125f, 28f), "↺ Geni Sıfırla", BoldStyle(11)))
            {
                ResetCurrentSlotToInitial();
            }
            GUI.backgroundColor = Color.white;
            curY += 56f;

            // =====================================================================
            // ÇİFT ZİNCİR (TOP STRAND & BOTTOM STRAND) GÖRSEL DİZİLİM ALANI
            // =====================================================================
            float strandBoxH = 195f;
            Rect strandBox = new Rect(innerX, curY, innerW, strandBoxH);
            DrawBoxWithBorder(strandBox, new Color(0.02f, 0.04f, 0.09f, 0.98f), new Color(0.12f, 0.45f, 0.75f, 0.7f));

            // Sol Etiket Sütunu (ÜST ZİNCİR / EŞLENİK / ALT ZİNCİR)
            float labelColW = 78f;
            GUI.color = new Color(0.6f, 0.85f, 1f);
            GUI.Label(new Rect(strandBox.x + 8f, strandBox.y + 8f, labelColW, 18f), "ÇİFT NO", BoldStyle(10));
            GUI.Label(new Rect(strandBox.x + 8f, strandBox.y + 38f, labelColW, 24f), "ÜST ZİNCİR\n(Top)", BoldStyle(10));
            GUI.Label(new Rect(strandBox.x + 8f, strandBox.y + 92f, labelColW, 20f), "EŞLENİK", BoldStyle(10));
            GUI.Label(new Rect(strandBox.x + 8f, strandBox.y + 132f, labelColW, 24f), "ALT ZİNCİR\n(Bottom)", BoldStyle(10));
            GUI.color = Color.white;

            int pairCount = strand.pairCount;
            float availColsW = strandBox.width - labelColW - 18f;
            float colW = Mathf.Min(56f, Mathf.Floor((availColsW - (pairCount - 1) * 4f) / pairCount));
            float totalColsW = pairCount * colW + (pairCount - 1) * 4f;
            float startColX = strandBox.x + labelColW + Mathf.Max(4f, (availColsW - totalColsW) * 0.5f);

            for (int i = 0; i < pairCount; i++)
            {
                float cx = startColX + i * (colW + 4f);
                LacunaShape topS = strand.top[i];
                LacunaShape botS = strand.bottom[i];
                bool pairValid = LacunaGeneticsDatabase.ArePartners(topS, botS);
                bool pairMissing = (topS == LacunaShape.None || botS == LacunaShape.None);

                // 1. Sütun Numarası (#1..#10)
                bool hadGap = strand.wasTopMissing[i] || strand.wasBottomMissing[i];
                GUI.color = hadGap ? new Color(1f, 0.8f, 0.25f) : new Color(0.65f, 0.78f, 0.9f);
                GUI.Label(new Rect(cx, strandBox.y + 6f, colW, 18f), $"#{i + 1}{(hadGap ? "*" : "")}", CenteredBoldStyle(10));
                GUI.color = Color.white;

                // 2. ÜST ZİNCİR HÜCRESİ (Top Strand)
                Rect topCellRect = new Rect(cx, strandBox.y + 28f, colW, 48f);
                if (DrawNucleotideCellButton(topCellRect, topS, strand.wasTopMissing[i]))
                {
                    ApplyBrushToCell(strand, i, isTop: true);
                }

                // 3. ORTA BAĞ & HIZLI EŞLE BUTONU
                Rect bondRect = new Rect(cx + 2f, strandBox.y + 80f, colW - 4f, 36f);
                if (pairValid)
                {
                    DrawBoxWithBorder(bondRect, new Color(0.05f, 0.28f, 0.18f, 0.9f), new Color(0.2f, 0.95f, 0.55f, 0.8f));
                    GUI.color = new Color(0.3f, 1f, 0.6f);
                    GUI.Label(bondRect, "✔\n║", CenteredBoldStyle(10));
                    GUI.color = Color.white;
                }
                else
                {
                    Color bCol = pairMissing ? new Color(0.95f, 0.65f, 0.15f) : new Color(0.95f, 0.25f, 0.3f);
                    GUI.backgroundColor = bCol;
                    if (GUI.Button(bondRect, "↕\nEşle", CenteredBoldStyle(9)))
                    {
                        AutoCompletePairAt(strand, i);
                    }
                    GUI.backgroundColor = Color.white;
                }

                // 4. ALT ZİNCİR HÜCRESİ (Bottom Strand)
                Rect botCellRect = new Rect(cx, strandBox.y + 120f, colW, 48f);
                if (DrawNucleotideCellButton(botCellRect, botS, strand.wasBottomMissing[i]))
                {
                    ApplyBrushToCell(strand, i, isTop: false);
                }

                // Alt sembol özeti
                GUI.color = new Color(0.75f, 0.85f, 0.95f);
                GUI.Label(new Rect(cx, strandBox.y + 170f, colW, 18f),
                    $"{LacunaGeneticsDatabase.GetSymbol(topS)}/{LacunaGeneticsDatabase.GetSymbol(botS)}",
                    CenteredBoldStyle(10));
                GUI.color = Color.white;
            }
            curY += strandBoxH + 8f;

            // =====================================================================
            // ŞEKİL SEÇİM PALETİ (● ▲ ■ ◆) & EŞLENİK KURALI KUTUSU
            // =====================================================================
            float paletteH = 68f;
            Rect paletteRect = new Rect(innerX, curY, innerW, paletteH);
            DrawBoxWithBorder(paletteRect, new Color(0.04f, 0.08f, 0.16f, 0.96f), new Color(0.2f, 0.6f, 0.9f, 0.7f));

            GUI.color = new Color(0.4f, 0.95f, 1f);
            GUI.Label(new Rect(paletteRect.x + 10f, paletteRect.y + 5f, 360f, 18f),
                "🎨 NÜKLEOTİD ŞEKİL PALETİ (Eşlenik Kuralı:  ● ↔ ■   ve   ▲ ↔ ◆ ):", BoldStyle(11));
            GUI.color = Color.white;

            // Otomatik eşlenik doldurma seçeneği
            autoFillPartnerOnEdit = GUI.Toggle(
                new Rect(paletteRect.xMax - 255f, paletteRect.y + 4f, 248f, 20f),
                autoFillPartnerOnEdit,
                " 🔗 Tıklayınca Karşı Eşleniğini de Doldur"
            );

            LacunaShape[] shapes = { LacunaShape.Circle, LacunaShape.Triangle, LacunaShape.Square, LacunaShape.Diamond };
            float sBtnW = (paletteRect.width - 28f - 18f) / 4f;
            for (int sIdx = 0; sIdx < shapes.Length; sIdx++)
            {
                LacunaShape sh = shapes[sIdx];
                LacunaShape partner = LacunaGeneticsDatabase.GetPartner(sh);
                bool isSel = (selectedBrushShape == sh);
                Color sCol = LacunaGeneticsDatabase.GetShapeColor(sh);

                Rect bRect = new Rect(paletteRect.x + 10f + sIdx * (sBtnW + 6f), paletteRect.y + 26f, sBtnW, 34f);
                DrawBoxWithBorder(
                    bRect,
                    isSel ? new Color(sCol.r * 0.45f, sCol.g * 0.45f, sCol.b * 0.45f, 0.98f) : new Color(0.06f, 0.10f, 0.18f, 0.95f),
                    isSel ? Color.white : sCol
                );

                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.01f);
                if (GUI.Button(bRect, GUIContent.none))
                {
                    selectedBrushShape = sh;
                    audio?.PlaySnap();
                }
                GUI.backgroundColor = Color.white;

                Texture2D icon = LacunaGeneticsDatabase.GetShapeIcon(sh);
                if (icon != null)
                {
                    GUI.color = sCol;
                    GUI.DrawTexture(new Rect(bRect.x + 8f, bRect.y + 5f, 24f, 24f), icon);
                }

                GUI.color = isSel ? Color.white : sCol;
                GUI.Label(
                    new Rect(bRect.x + 36f, bRect.y + 7f, bRect.width - 40f, 20f),
                    $"[{sIdx + 1}] {LacunaGeneticsDatabase.GetShapeNameTR(sh)} (Eş: {LacunaGeneticsDatabase.GetSymbol(partner)})",
                    BoldStyle(11)
                );
                GUI.color = Color.white;
            }
            curY += paletteH + 8f;

            // =====================================================================
            // ALT AKSİYON BARI: 4 GEN ÖZETİ & [ 🔒 KAPSÜLLE (QTE) ] BUTONU
            // =====================================================================
            float footerH = Mathf.Max(52f, panelRect.yMax - curY - 10f);
            Rect footerRect = new Rect(innerX, curY, innerW, footerH);
            DrawBoxWithBorder(footerRect, new Color(0.03f, 0.10f, 0.16f, 0.98f), new Color(0.2f, 0.85f, 0.55f, 0.85f));

            // Sol taraf: 4 Genin Özeti
            float sumW = footerRect.width - 235f;
            GUI.color = new Color(0.85f, 0.95f, 1f);
            GUI.Label(new Rect(footerRect.x + 10f, footerRect.y + 6f, sumW, 18f),
                $"📋 {entry.creatureName} GÜNCEL GENOM ÖZETİ ({planet.planetName}):", BoldStyle(11));

            string zName = GetSlotStatusShort(LacunaSlotType.Z);
            string vName = GetSlotStatusShort(LacunaSlotType.V);
            string kName = GetSlotStatusShort(LacunaSlotType.K);
            string pName = GetSlotStatusShort(LacunaSlotType.P);

            GUI.color = new Color(0.35f, 1f, 0.7f);
            GUI.Label(new Rect(footerRect.x + 10f, footerRect.y + 25f, sumW, 22f),
                $"Z: {zName}   |   V: {vName}   |   K: {kName}   |   P: {pName}", BoldStyle(11));
            GUI.color = Color.white;

            // Sağ taraf: KAPSÜLLE (CAPSULATE) QTE Butonu
            GUI.backgroundColor = new Color(0.15f, 0.88f, 0.48f);
            Rect capBtnRect = new Rect(footerRect.xMax - 220f, footerRect.y + 8f, 210f, footerRect.height - 16f);
            if (GUI.Button(capBtnRect, "🔒 4. ADIM: KAPSÜLLE\n(QTE MÜHÜRLEME)", CenteredBoldStyle(12)))
            {
                StartCapsulateQTE();
            }
            GUI.backgroundColor = Color.white;
        }

        private string GetSlotStatusShort(LacunaSlotType slot)
        {
            var st = activeStrands[slot];
            if (st.GetInvalidPairCount() > 0) return "✖HATA";
            if (st.HasAnyMissing()) return $"⚠️{st.GetMissingCount()}?";
            var m = LacunaGeneticsDatabase.MatchStrandToGene(slot, st.top, st.bottom);
            return m != null ? m.geneName : "❓TANIMSIZ";
        }

        private bool DrawNucleotideCellButton(Rect rect, LacunaShape shape, bool startedMissing)
        {
            Color sCol = LacunaGeneticsDatabase.GetShapeColor(shape);
            Color bgCol = (shape == LacunaShape.None)
                ? new Color(0.18f, 0.12f, 0.04f, 0.95f)
                : new Color(sCol.r * 0.22f, sCol.g * 0.22f, sCol.b * 0.22f, 0.95f);
            Color borderCol = (shape == LacunaShape.None)
                ? new Color(1f, 0.75f, 0.2f, 0.95f)
                : (startedMissing ? new Color(0.4f, 1f, 0.7f, 0.95f) : sCol);

            DrawBoxWithBorder(rect, bgCol, borderCol);

            bool clicked = false;
            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.01f);
            if (GUI.Button(rect, GUIContent.none))
            {
                clicked = true;
            }
            GUI.backgroundColor = Color.white;

            Texture2D icon = LacunaGeneticsDatabase.GetShapeIcon(shape);
            if (icon != null)
            {
                float iconSize = Mathf.Min(rect.width - 14f, 26f);
                Rect iconRect = new Rect(
                    rect.x + (rect.width - iconSize) * 0.5f,
                    rect.y + 5f,
                    iconSize,
                    iconSize
                );
                GUI.color = (shape == LacunaShape.None) ? new Color(1f, 0.8f, 0.25f) : sCol;
                GUI.DrawTexture(iconRect, icon);
            }

            GUI.color = (shape == LacunaShape.None) ? new Color(1f, 0.85f, 0.3f) : Color.white;
            GUI.Label(new Rect(rect.x, rect.yMax - 17f, rect.width, 16f), LacunaGeneticsDatabase.GetSymbol(shape), CenteredBoldStyle(11));
            GUI.color = Color.white;

            return clicked;
        }

        private void ApplyBrushToCell(EditableGeneStrand strand, int pairIdx, bool isTop)
        {
            LacunaShape current = isTop ? strand.top[pairIdx] : strand.bottom[pairIdx];
            LacunaShape nextShape = selectedBrushShape;

            // Eğer zaten seçili şekil hücrede varsa, tıklandığında sıradaki şekle döndür (● -> ▲ -> ■ -> ◆)
            if (current == selectedBrushShape)
            {
                switch (current)
                {
                    case LacunaShape.Circle: nextShape = LacunaShape.Triangle; break;
                    case LacunaShape.Triangle: nextShape = LacunaShape.Square; break;
                    case LacunaShape.Square: nextShape = LacunaShape.Diamond; break;
                    case LacunaShape.Diamond: nextShape = LacunaShape.Circle; break;
                    default: nextShape = selectedBrushShape; break;
                }
            }

            if (isTop)
            {
                strand.top[pairIdx] = nextShape;
                if (autoFillPartnerOnEdit)
                {
                    strand.bottom[pairIdx] = LacunaGeneticsDatabase.GetPartner(nextShape);
                }
            }
            else
            {
                strand.bottom[pairIdx] = nextShape;
                if (autoFillPartnerOnEdit)
                {
                    strand.top[pairIdx] = LacunaGeneticsDatabase.GetPartner(nextShape);
                }
            }

            audio?.PlaySnap();
        }

        private void AutoCompletePairAt(EditableGeneStrand strand, int pairIdx)
        {
            LacunaShape t = strand.top[pairIdx];
            LacunaShape b = strand.bottom[pairIdx];

            if (t != LacunaShape.None && b == LacunaShape.None)
            {
                strand.bottom[pairIdx] = LacunaGeneticsDatabase.GetPartner(t);
                audio?.PlaySnap();
            }
            else if (b != LacunaShape.None && t == LacunaShape.None)
            {
                strand.top[pairIdx] = LacunaGeneticsDatabase.GetPartner(b);
                audio?.PlaySnap();
            }
            else if (t != LacunaShape.None)
            {
                strand.bottom[pairIdx] = LacunaGeneticsDatabase.GetPartner(t);
                audio?.PlaySnap();
            }
            else
            {
                strand.top[pairIdx] = selectedBrushShape;
                strand.bottom[pairIdx] = LacunaGeneticsDatabase.GetPartner(selectedBrushShape);
                audio?.PlaySnap();
            }
        }

        // =========================================================================
        // SAĞ PANEL: RULEBOOK (KURAL KİTABI, GEZEGEN LİMİTLERİ VE YAN YANA KIYASLAMA)
        // =========================================================================
        private void DrawRulebookPanel(Rect panelRect, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet)
        {
            DrawBoxWithBorder(panelRect, new Color(0.04f, 0.06f, 0.13f, 0.96f), new Color(0.85f, 0.65f, 0.20f, 0.85f));

            float innerX = panelRect.x + 12f;
            float innerW = panelRect.width - 24f;
            float curY = panelRect.y + 10f;

            // Kural Kitabı Üst Sekmeleri
            string[] tabs = { "📖 1. GEN SÖZLÜĞÜ & KIYASLA", "🪐 2. GEZEGEN KURALLARI", "🕵️ 3. REHBER & İPUÇLARI" };
            float tabW = (innerW - 12f) / 3f;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool isSel = (rulebookTab == i);
                GUI.backgroundColor = isSel ? new Color(0.95f, 0.72f, 0.18f) : new Color(0.18f, 0.25f, 0.38f);
                if (GUI.Button(new Rect(innerX + i * (tabW + 6f), curY, tabW, 28f), tabs[i], CenteredBoldStyle(10)))
                {
                    rulebookTab = i;
                    audio?.PlaySnap();
                }
            }
            GUI.backgroundColor = Color.white;
            curY += 34f;

            Rect contentRect = new Rect(innerX, curY, innerW, panelRect.yMax - curY - 10f);
            if (rulebookTab == 0)
            {
                DrawRulebookGenesAndCompareTab(contentRect, entry, planet);
            }
            else if (rulebookTab == 1)
            {
                DrawRulebookPlanetsAndOrderTab(contentRect, entry, planet);
            }
            else
            {
                DrawRulebookLoreAndGuideTab(contentRect, entry);
            }
        }

        private void DrawRulebookGenesAndCompareTab(Rect area, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet)
        {
            float curY = area.y;

            // Slot Filtre Butonları (Z, V, K, P)
            float slotBtnW = (area.width - 18f) / 4f;
            int idx = 0;
            foreach (LacunaSlotType slot in Enum.GetValues(typeof(LacunaSlotType)))
            {
                bool isSel = (rulebookSlotFilter == slot);
                GUI.backgroundColor = isSel ? new Color(0.2f, 0.8f, 1f) : new Color(0.15f, 0.22f, 0.34f);
                if (GUI.Button(new Rect(area.x + idx * (slotBtnW + 6f), curY, slotBtnW, 25f),
                    $"Slot {slot} ({LacunaGeneticsDatabase.GetSlotPairCount(slot)} Çift)", CenteredBoldStyle(10)))
                {
                    rulebookSlotFilter = slot;
                    var gList = LacunaGeneticsDatabase.GetGenesBySlot(slot);
                    if (gList.Count >= 2)
                    {
                        compareGeneA = gList[0].geneName;
                        compareGeneB = gList[1].geneName;
                    }
                    audio?.PlaySnap();
                }
                idx++;
            }
            GUI.backgroundColor = Color.white;
            curY += 30f;

            // Bu slottaki gezegen kuralı hatırlatması
            Rect ruleRemindRect = new Rect(area.x, curY, area.width, 26f);
            DrawBoxWithBorder(ruleRemindRect, new Color(0.08f, 0.14f, 0.22f, 0.95f), planet.themeColor);
            GUI.color = planet.themeColor;
            GUI.Label(new Rect(ruleRemindRect.x + 8f, ruleRemindRect.y + 4f, ruleRemindRect.width - 16f, 18f),
                $"🪐 {planet.planetName} — Slot {rulebookSlotFilter} Kuralı: {planet.GetRuleForSlot(rulebookSlotFilter)}",
                BoldStyle(11));
            GUI.color = Color.white;
            curY += 30f;

            // Slottaki 5 Genin Listesi
            var slotGenes = LacunaGeneticsDatabase.GetGenesBySlot(rulebookSlotFilter);
            var activeStrand = activeStrands[rulebookSlotFilter];

            float cardH = 52f;
            foreach (var g in slotGenes)
            {
                Rect cardRect = new Rect(area.x, curY, area.width, cardH);

                // Aktif dizilimle kaç çift farkı var?
                int diffPairs = 0;
                bool canCompareWithActive = !activeStrand.HasAnyMissing() && activeStrand.AreAllPairsValidComplements();
                for (int i = 0; i < g.pairCount; i++)
                {
                    if (activeStrand.top[i] != LacunaShape.None && activeStrand.top[i] != g.topStrand[i])
                        diffPairs++;
                }

                bool isExactCurrent = canCompareWithActive && diffPairs == 0;
                DrawBoxWithBorder(
                    cardRect,
                    isExactCurrent ? new Color(0.06f, 0.24f, 0.16f, 0.95f) : new Color(0.03f, 0.07f, 0.14f, 0.95f),
                    isExactCurrent ? new Color(0.25f, 1f, 0.55f) : new Color(0.2f, 0.35f, 0.55f, 0.7f)
                );

                // Sol: Gen İsmi & Fark Bilgisi
                GUI.color = isExactCurrent ? new Color(0.3f, 1f, 0.6f) : new Color(1f, 0.9f, 0.4f);
                GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 4f, 92f, 20f), g.geneName, BoldStyle(12));

                GUI.color = new Color(0.75f, 0.85f, 0.95f);
                string diffText = isExactCurrent ? "✔ AKTİF GEN" : $"{diffPairs} çift farklı";
                GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 24f, 92f, 18f), diffText, BoldStyle(9));
                GUI.color = Color.white;

                // Orta: Üst ve Alt Zincir Sembolleri
                float seqStartX = cardRect.x + 102f;
                float cellW = Mathf.Min(24f, (cardRect.width - 195f) / g.pairCount);
                for (int pIdx = 0; pIdx < g.pairCount; pIdx++)
                {
                    float px = seqStartX + pIdx * cellW;
                    bool differsFromActive = (activeStrand.top[pIdx] != LacunaShape.None && activeStrand.top[pIdx] != g.topStrand[pIdx]);

                    if (differsFromActive)
                    {
                        DrawRect(new Rect(px, cardRect.y + 4f, cellW - 2f, cardH - 8f), new Color(0.95f, 0.45f, 0.15f, 0.28f));
                    }

                    GUI.color = LacunaGeneticsDatabase.GetShapeColor(g.topStrand[pIdx]);
                    GUI.Label(new Rect(px, cardRect.y + 5f, cellW, 20f), LacunaGeneticsDatabase.GetSymbol(g.topStrand[pIdx]), CenteredBoldStyle(12));

                    GUI.color = LacunaGeneticsDatabase.GetShapeColor(g.bottomStrand[pIdx]);
                    GUI.Label(new Rect(px, cardRect.y + 25f, cellW, 20f), LacunaGeneticsDatabase.GetSymbol(g.bottomStrand[pIdx]), CenteredBoldStyle(12));
                }
                GUI.color = Color.white;

                // Sağ: Yan Yana Kıyaslama Seçim Butonları (A / B)
                bool isA = string.Equals(compareGeneA, g.geneName, StringComparison.OrdinalIgnoreCase);
                bool isB = string.Equals(compareGeneB, g.geneName, StringComparison.OrdinalIgnoreCase);

                GUI.backgroundColor = isA ? Color.cyan : new Color(0.2f, 0.3f, 0.45f);
                if (GUI.Button(new Rect(cardRect.xMax - 82f, cardRect.y + 12f, 36f, 26f), "A", CenteredBoldStyle(10)))
                {
                    compareGeneA = g.geneName;
                    audio?.PlaySnap();
                }

                GUI.backgroundColor = isB ? new Color(1f, 0.65f, 0.2f) : new Color(0.2f, 0.3f, 0.45f);
                if (GUI.Button(new Rect(cardRect.xMax - 42f, cardRect.y + 12f, 36f, 26f), "B", CenteredBoldStyle(10)))
                {
                    compareGeneB = g.geneName;
                    audio?.PlaySnap();
                }
                GUI.backgroundColor = Color.white;

                curY += cardH + 4f;
            }

            // Alt Kısım: YAN YANA İKİ GEN KIYASLAMA KUTUSU (A vs B)
            float remH = area.yMax - curY;
            if (remH >= 65f)
            {
                Rect cmpRect = new Rect(area.x, curY + 2f, area.width, remH - 2f);
                DrawBoxWithBorder(cmpRect, new Color(0.05f, 0.09f, 0.18f, 0.98f), new Color(0.3f, 0.85f, 1f, 0.7f));

                var gA = LacunaGeneticsDatabase.GetGeneByName(compareGeneA);
                var gB = LacunaGeneticsDatabase.GetGeneByName(compareGeneB);

                if (gA != null && gB != null && gA.pairCount == gB.pairCount)
                {
                    int diffCount = LacunaGeneticsDatabase.CountPairDifferences(gA, gB);
                    GUI.color = new Color(0.4f, 0.95f, 1f);
                    GUI.Label(new Rect(cmpRect.x + 8f, cmpRect.y + 4f, cmpRect.width - 16f, 18f),
                        $"🔍 YAN YANA KIYASLAMA: [A] {gA.geneName}  ↔  [B] {gB.geneName}  ({diffCount} Çift Farklı)",
                        BoldStyle(11));

                    float startX = cmpRect.x + 95f;
                    float cW = Mathf.Min(26f, (cmpRect.width - 105f) / gA.pairCount);

                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(cmpRect.x + 8f, cmpRect.y + 24f, 85f, 18f), $"[A] {gA.geneName}:", BoldStyle(10));
                    GUI.color = new Color(1f, 0.7f, 0.25f);
                    GUI.Label(new Rect(cmpRect.x + 8f, cmpRect.y + 44f, 85f, 18f), $"[B] {gB.geneName}:", BoldStyle(10));

                    for (int i = 0; i < gA.pairCount; i++)
                    {
                        float px = startX + i * cW;
                        bool diff = gA.topStrand[i] != gB.topStrand[i];
                        if (diff)
                        {
                            DrawRect(new Rect(px, cmpRect.y + 22f, cW - 2f, 40f), new Color(1f, 0.3f, 0.3f, 0.35f));
                        }

                        GUI.color = LacunaGeneticsDatabase.GetShapeColor(gA.topStrand[i]);
                        GUI.Label(new Rect(px, cmpRect.y + 23f, cW, 18f), LacunaGeneticsDatabase.GetSymbol(gA.topStrand[i]), CenteredBoldStyle(11));

                        GUI.color = LacunaGeneticsDatabase.GetShapeColor(gB.topStrand[i]);
                        GUI.Label(new Rect(px, cmpRect.y + 43f, cW, 18f), LacunaGeneticsDatabase.GetSymbol(gB.topStrand[i]), CenteredBoldStyle(11));
                    }
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawRulebookPlanetsAndOrderTab(Rect area, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition activePlanet)
        {
            float curY = area.y;

            GUI.color = new Color(1f, 0.88f, 0.3f);
            GUI.Label(new Rect(area.x, curY, area.width, 20f), "🪐 GEZEGEN KABUL TABLOSU (GDD Sayfa 5):", BoldStyle(12));
            GUI.color = Color.white;
            curY += 22f;

            var planets = LacunaGeneticsDatabase.GetAllPlanets();
            foreach (var p in planets)
            {
                bool isCurrent = string.Equals(p.planetName, activePlanet.planetName, StringComparison.OrdinalIgnoreCase);
                Rect pRect = new Rect(area.x, curY, area.width, 82f);
                DrawBoxWithBorder(
                    pRect,
                    isCurrent ? new Color(0.08f, 0.16f, 0.24f, 0.98f) : new Color(0.03f, 0.06f, 0.12f, 0.95f),
                    isCurrent ? p.themeColor : new Color(0.25f, 0.35f, 0.5f, 0.7f)
                );

                GUI.color = p.themeColor;
                string activeTag = isCurrent ? " ◀ [BU NUMUNENİN GEZEGENİ]" : "";
                GUI.Label(new Rect(pRect.x + 8f, pRect.y + 4f, pRect.width - 16f, 18f),
                    $"🪐 {p.planetName} — {p.subtitleTR}{activeTag}", BoldStyle(11));

                GUI.color = Color.white;
                GUI.Label(new Rect(pRect.x + 8f, pRect.y + 23f, pRect.width - 16f, 36f),
                    $"• Deri (K): {p.skinRuleTR}   |   • Boy (Z): {p.heightRuleTR}\n" +
                    $"• Göz (V): {p.eyesRuleTR}   |   • Uzuv (P): {p.limbsRuleTR}",
                    BoldStyle(10));

                GUI.color = new Color(0.75f, 0.88f, 1f);
                GUI.Label(new Rect(pRect.x + 8f, pRect.y + 60f, pRect.width - 16f, 18f),
                    $"💡 Biyolojik Kural: {p.rulebookReasonTR}", RegularStyle(10));
                GUI.color = Color.white;

                curY += 86f;
            }

            // Gen Sıralama (Rank Order) Rehberi — "At least ZORP" / "At most VESHI" ne demek?
            Rect rankRect = new Rect(area.x, curY, area.width, area.yMax - curY);
            DrawBoxWithBorder(rankRect, new Color(0.04f, 0.09f, 0.16f, 0.98f), new Color(0.25f, 0.85f, 0.65f, 0.8f));

            GUI.color = new Color(0.35f, 1f, 0.7f);
            GUI.Label(new Rect(rankRect.x + 8f, rankRect.y + 5f, rankRect.width - 16f, 18f),
                "📏 BİYOLOJİK GEN SIRALAMASI (Alt ve Üst Sınır Rehberi):", BoldStyle(11));

            GUI.color = new Color(0.9f, 0.95f, 1f);
            GUI.Label(new Rect(rankRect.x + 8f, rankRect.y + 25f, rankRect.width - 16f, rankRect.height - 28f),
                "• SLOT Z (Boy — Küçükten Büyüğe):\n" +
                "   ZUUN  <  ZIMMOK  <  ZEMBO  <  ZORP  <  ZAKKO\n" +
                "• SLOT V (Göz — Azdan Çoğa):\n" +
                "   VIXX  <  VOLM  <  VESHI  <  VARQ  <  VUNDO\n" +
                "• SLOT P (Uzuv — Azdan Çoğa):\n" +
                "   PHELM  <  PONDO  <  PLORT  <  PUNKO  <  PRIZZ\n" +
                "• SLOT K (Deri — Sıralamasız): Gezegenin istediği 2 genden biri seçilmelidir.\n" +
                "📌 Not: \"En az ZORP\" = ZORP veya üstündekiler (ZORP, ZAKKO). \"En fazla VESHI\" = VESHI veya altındakiler.",
                BoldStyle(10));
            GUI.color = Color.white;
        }

        private void DrawRulebookLoreAndGuideTab(Rect area, LacunaCreatureRosterEntry entry)
        {
            DrawBoxWithBorder(area, new Color(0.04f, 0.07f, 0.14f, 0.96f), new Color(0.5f, 0.35f, 0.85f, 0.8f));

            GUILayout.BeginArea(new Rect(area.x + 10f, area.y + 8f, area.width - 20f, area.height - 16f));

            GUI.color = new Color(0.4f, 0.95f, 1f);
            GUILayout.Label("🛠️ 4 ADIMLI ONARIM DÖNGÜSÜ (NASIL OYNANIR?):", BoldStyle(12));
            GUI.color = Color.white;
            GUILayout.Label(
                "1. Gezegeni Oku: Yaratığın gezegeninin (FABAUP / SNIBBLEX / OOMGARTH) hangi genleri kabul ettiğine bak.\n" +
                "2. Boşlukları Doldur: ● her zaman ■ ile, ▲ her zaman ◆ ile eşleşir. İki tarafı da eksik çiftleri Kural Kitabındaki gen diziliminden bul.\n" +
                "3. Yanlış Geni Bul ve Düzelt: Yaratığın 4 geninden 3'ü gezegene uygundur, 1 tanesi yanlıştır! Yanlış geni 1 veya 2 çift değiştirerek gezegenin kabul ettiği bir gene dönüştür.\n" +
                "4. Kapsülle (QTE): Mühürleme çubuğunu yeşil bölgede yakala!",
                RegularStyle(11));

            GUILayout.Space(8f);
            GUI.color = new Color(1f, 0.85f, 0.35f);
            GUILayout.Label($"📋 10 YARATIK İLERLEME LİSTESİ:", BoldStyle(11));
            var roster = LacunaGeneticsDatabase.GetCreatureRoster();
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster[i];
                bool done = CreatureCompleted[i];
                GUI.color = done ? new Color(0.3f, 1f, 0.55f) : (i == CurrentCreatureIndex ? Color.cyan : new Color(0.75f, 0.8f, 0.88f));
                GUILayout.Label($"  {(done ? "✔" : "•")} #{c.number} {c.creatureName} ({c.planetName}) — {(done ? "KAPSÜLLENDİ" : "Bekliyor")}", BoldStyle(10));
            }

            if (UnlockedClues.Count > 0)
            {
                GUILayout.Space(8f);
                GUI.color = new Color(0.85f, 0.7f, 1f);
                GUILayout.Label("🕵️ EV SAHİBİ HAKKINDA AÇILAN SIRLAR:", BoldStyle(11));
                foreach (var clue in UnlockedClues)
                {
                    GUI.color = new Color(0.9f, 0.85f, 1f);
                    GUILayout.Label("• " + clue, RegularStyle(10));
                }
            }
            GUI.color = Color.white;
            GUILayout.EndArea();
        }

        // =========================================================================
        // QTE (QUICK-TIME EVENT) KAPSÜL MÜHÜRLEME EKRANI
        // =========================================================================
        private void DrawQteOverlay(float screenW, float screenH, LacunaCreatureRosterEntry entry)
        {
            DrawRect(new Rect(0, 0, screenW, screenH), new Color(0f, 0f, 0f, 0.78f));

            float boxW = 560f;
            float boxH = 230f;
            Rect box = new Rect((screenW - boxW) * 0.5f, (screenH - boxH) * 0.5f, boxW, boxH);
            DrawBoxWithBorder(box, new Color(0.04f, 0.09f, 0.18f, 0.98f), new Color(0.2f, 0.95f, 0.6f, 0.95f));

            GUI.color = new Color(0.3f, 1f, 0.65f);
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 24f),
                $"🔒 BİYOKAPSÜL MÜHÜRLEME (QTE) — #{entry.number} {entry.creatureName}", CenteredBoldStyle(15));

            GUI.color = new Color(0.85f, 0.95f, 1f);
            GUI.Label(new Rect(box.x, box.y + 42f, box.width, 20f),
                $"İmleç yeşil mühürleme bölgesindeyken [SPACE] tuşuna veya aşağıdaki butona basın! (Hız: {entry.qteSpeed})",
                CenteredBoldStyle(11));

            // QTE Barı
            Rect barBg = new Rect(box.x + 40f, box.y + 82f, box.width - 80f, 42f);
            DrawBoxWithBorder(barBg, new Color(0.08f, 0.12f, 0.20f, 1f), Color.white);

            // Yeşil Hedef Bölgesi
            float targetX = barBg.x + qteTargetMin * barBg.width;
            float targetW = (qteTargetMax - qteTargetMin) * barBg.width;
            DrawRect(new Rect(targetX, barBg.y + 2f, targetW, barBg.height - 4f), new Color(0.15f, 0.88f, 0.45f, 0.85f));

            // Hareketli İmleç
            float cursorX = barBg.x + qteCursorPos * barBg.width;
            DrawRect(new Rect(cursorX - 4f, barBg.y - 6f, 8f, barBg.height + 12f), new Color(1f, 0.95f, 0.25f, 1f));

            GUI.backgroundColor = new Color(0.15f, 0.90f, 0.50f);
            if (GUI.Button(new Rect(box.x + 110f, box.y + 145f, 220f, 44f), "⚡ ŞİMDİ MÜHÜRLE! (SPACE)", CenteredBoldStyle(13)))
            {
                CompleteQteAttempt();
            }

            GUI.backgroundColor = new Color(0.55f, 0.25f, 0.30f);
            if (GUI.Button(new Rect(box.x + 345f, box.y + 145f, 110f, 44f), "İptal", CenteredBoldStyle(12)))
            {
                isQteActive = false;
            }
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
        }

        // =========================================================================
        // SONUÇ / KAPSÜL REVEAL / HATA / FİNAL EKRANI
        // =========================================================================
        private void DrawResultModal(float screenW, float screenH, LacunaCreatureRosterEntry entry, LacunaPlanetDefinition planet, bool isLabMode)
        {
            DrawRect(new Rect(0, 0, screenW, screenH), new Color(0f, 0f, 0f, 0.82f));

            float boxW = Mathf.Min(680f, screenW - 40f);
            float boxH = isVictoryEnding ? 420f : 360f;
            Rect box = new Rect((screenW - boxW) * 0.5f, (screenH - boxH) * 0.5f, boxW, boxH);

            Color borderCol = lastAttemptSuccess ? new Color(0.25f, 1f, 0.55f) : new Color(1f, 0.28f, 0.32f);
            DrawBoxWithBorder(box, new Color(0.04f, 0.08f, 0.15f, 0.98f), borderCol);

            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));

            GUI.color = borderCol;
            GUILayout.Label(resultTitle, BoldStyle(15));
            GUILayout.Space(8f);

            GUI.color = Color.white;
            GUILayout.Label(resultDetail, BoldStyle(12));

            if (!string.IsNullOrEmpty(resultLoreClue))
            {
                GUILayout.Space(10f);
                GUI.color = new Color(0.88f, 0.78f, 1f);
                GUILayout.Label(resultLoreClue, BoldStyle(11));
            }

            if (isVictoryEnding && lastAttemptSuccess)
            {
                GUILayout.Space(10f);
                GUI.color = new Color(1f, 0.88f, 0.30f);
                GUILayout.Label(
                    "🏆 LACUNA GENETICS FİNALİ TAMAMLANDI!\n" +
                    "10 yaratığın tamamını onardın. Ev Sahibi aileni serbest bıraktı ve onardığın tüm canlıları sana hediye etti. " +
                    "Onunla evlendin, emekli oldun ve huzurlu bir hayat yaşadın!",
                    BoldStyle(12));
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (lastAttemptSuccess)
            {
                if (isLabMode)
                {
                    GUI.backgroundColor = new Color(0.2f, 0.85f, 0.95f);
                    if (GUILayout.Button("🔬 KULUÇKA ODASINDA 3D MODELİ GÖR", BoldStyle(12), GUILayout.Height(38f)))
                    {
                        showResultModal = false;
                        controller.ViewInLabChamber();
                    }
                }

                if (CurrentCreatureIndex < 9)
                {
                    GUI.backgroundColor = new Color(0.2f, 0.90f, 0.50f);
                    var nextCreature = LacunaGeneticsDatabase.GetCreatureRoster()[CurrentCreatureIndex + 1];
                    if (GUILayout.Button($"➡️ SONRAKİ NUMUNE: #{nextCreature.number} {nextCreature.creatureName}", BoldStyle(12), GUILayout.Height(38f)))
                    {
                        LoadCreature(CurrentCreatureIndex + 1);
                    }
                }
                else
                {
                    GUI.backgroundColor = new Color(0.95f, 0.78f, 0.20f);
                    if (GUILayout.Button("🔄 1. NUMUNEDEN TEKRAR OYNA", BoldStyle(12), GUILayout.Height(38f)))
                    {
                        RemainingLives = 2;
                        LoadCreature(0);
                    }
                }

                GUI.backgroundColor = new Color(0.35f, 0.45f, 0.6f);
                if (GUILayout.Button("Kapat", BoldStyle(12), GUILayout.Width(90f), GUILayout.Height(38f)))
                {
                    showResultModal = false;
                }
            }
            else if (isGameOver)
            {
                GUI.backgroundColor = new Color(0.2f, 0.85f, 0.55f);
                if (GUILayout.Button($"🔄 #{entry.number} {entry.creatureName} İLE TEKRAR DENE (2 Can)", BoldStyle(12), GUILayout.Height(38f)))
                {
                    RemainingLives = 2;
                    LoadCreature(CurrentCreatureIndex);
                }

                GUI.backgroundColor = new Color(0.85f, 0.45f, 0.20f);
                if (GUILayout.Button("🔄 1. CANLIDAN BAŞTAN BAŞLA", BoldStyle(12), GUILayout.Height(38f)))
                {
                    RemainingLives = 2;
                    for (int i = 0; i < 10; i++) CreatureCompleted[i] = false;
                    UnlockedClues.Clear();
                    LoadCreature(0);
                }
            }
            else
            {
                // 1. Hata sonrası aynı yaratığı tekrar deneme (GDD Sayfa 2: "retry the same creature")
                GUI.backgroundColor = new Color(0.95f, 0.65f, 0.18f);
                if (GUILayout.Button($"🔄 #{entry.number} {entry.creatureName} DİZİLİMİNE DÖN VE DÜZELT (1 Can Kaldı)", BoldStyle(12), GUILayout.Height(38f)))
                {
                    showResultModal = false;
                }
            }

            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // =========================================================================
        // YARDIMCI ÇİZİM & STİL METOTLARI
        // =========================================================================
        private void DrawRect(Rect r, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(r, GetWhiteTex());
            GUI.color = prev;
        }

        private void DrawBoxWithBorder(Rect r, Color bgCol, Color borderCol)
        {
            DrawRect(r, borderCol);
            DrawRect(new Rect(r.x + 1.5f, r.y + 1.5f, r.width - 3f, r.height - 3f), bgCol);
        }

        private GUIStyle BoldStyle(int size)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = size,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            s.normal.textColor = Color.white;
            return s;
        }

        private GUIStyle CenteredBoldStyle(int size)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = size,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            s.normal.textColor = Color.white;
            return s;
        }

        private GUIStyle RegularStyle(int size)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = size,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            s.normal.textColor = Color.white;
            return s;
        }
    }
}
