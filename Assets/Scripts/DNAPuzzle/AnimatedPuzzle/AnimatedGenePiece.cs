using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Gerçek bir 3D DNA Çift Sarmal Genom Parçası (DNA Helix Base-Pair Segment).
    /// Kullanıcının yüklediği şemadaki gibi:
    /// - Üstte ve altta sinüzoidal kıvrımlı çift sarmal şeritleri (Mavi & Turuncu omurga).
    /// - Aralarında 4 adet dikey Watson-Crick nükleotid basamağı (Base Pair rungs).
    /// - Her basamağın üstünde ve altında nükleotid harfleri (A-T, C-G, vb.) ve iki renkli nükleotid çubukları.
    /// - Ortada hidrojen bağı düğümleri ve yarı saydam siber gen bilgi plaketi.
    /// </summary>
    public class AnimatedGenePiece : MonoBehaviour
    {
        public GeneData Data { get; private set; }
        public int CurrentSlotIndex { get; set; } = -1;
        public bool IsEquipped => CurrentSlotIndex >= 0;
        public bool IsBeingDragged { get; set; } = false;

        private Material primaryMaterial;
        private Material borderPlaqueMat;
        private Material matRibbonBlue;
        private Material matRibbonOrange;
        private Material matHBarCore;
        private Material leftPlugMat;
        private Material rightPlugMat;
        private Material leftCollarMat;
        private Material rightKeyMat;
        private Light coreLight;

        private TextMesh nameTextMesh;
        private TextMesh statsTextMesh;
        private TextMesh leftNucMesh;
        private TextMesh rightNucMesh;

        // 4 Dikey Basamak (Base-Pair Rungs) Referansları
        private struct BasePairRung
        {
            public Transform upperHalf;
            public Transform lowerHalf;
            public Transform hBondNode;
            public Material upperMat;
            public Material lowerMat;
            public TextMesh topText;
            public TextMesh botText;
            public float localX;
        }

        private BasePairRung[] basePairs = new BasePairRung[4];
        private Vector3[] origRungScales = new Vector3[4];
        private List<Transform> topArchSegments = new List<Transform>();
        private List<Transform> botArchSegments = new List<Transform>();
        private Transform centerPlaqueTrans;
        private Transform borderPlaqueTrans;
        private TextMesh statusBadgeMesh;
        public bool IsSelectedForRepair { get; private set; } = false;
        private bool isZoomedInLab = false;

        // Animasyon Değişkenleri
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 targetScale = Vector3.one;
        private float hoverSpeed = 2.2f;
        private float hoverAmount = 0.07f;
        private float randomPhase = 0f;
        private bool isHovered = false;
        private bool isMoving = false;
        private float touchPulseTimer = 0f;
        private float baseLightIntensity = 1.3f;

        public event Action<AnimatedGenePiece> OnPieceClicked;
        public event Action<AnimatedGenePiece, bool> OnPieceHoverStateChanged;

        public void Initialize(GeneData geneData)
        {
            Data = geneData;
            randomPhase = UnityEngine.Random.Range(0f, 10f);
            BuildWavyDoubleHelixGeometry();
            UpdateVisuals();
        }

        private void BuildWavyDoubleHelixGeometry()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Sprites/Default");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            Color geneColor = Data.customColor;
            primaryMaterial = new Material(litShader);
            primaryMaterial.color = geneColor;
            if (primaryMaterial.HasProperty("_Smoothness")) primaryMaterial.SetFloat("_Smoothness", 0.9f);
            if (primaryMaterial.HasProperty("_Metallic")) primaryMaterial.SetFloat("_Metallic", 0.45f);

            // 1. Mavi Şerit Materyali (Şemadaki Üst Omurga)
            matRibbonBlue = new Material(litShader);
            matRibbonBlue.color = new Color(0.12f, 0.58f, 0.98f);
            if (matRibbonBlue.HasProperty("_Smoothness")) matRibbonBlue.SetFloat("_Smoothness", 0.92f);
            if (matRibbonBlue.HasProperty("_EmissionColor"))
            {
                matRibbonBlue.EnableKeyword("_EMISSION");
                matRibbonBlue.SetColor("_EmissionColor", new Color(0.08f, 0.45f, 0.85f) * 0.45f);
            }

            // 2. Turuncu Şerit Materyali (Şemadaki Alt Omurga)
            matRibbonOrange = new Material(litShader);
            matRibbonOrange.color = new Color(0.96f, 0.42f, 0.12f);
            if (matRibbonOrange.HasProperty("_Smoothness")) matRibbonOrange.SetFloat("_Smoothness", 0.92f);
            if (matRibbonOrange.HasProperty("_EmissionColor"))
            {
                matRibbonOrange.EnableKeyword("_EMISSION");
                matRibbonOrange.SetColor("_EmissionColor", new Color(0.90f, 0.35f, 0.08f) * 0.45f);
            }

            matHBarCore = new Material(unlitShader);
            matHBarCore.color = new Color(0.95f, 0.98f, 1f, 0.95f);

            // 3. Kıvrımlı Üst ve Alt Heliks Yayları (Curved Double Helix Rails)
            float halfW = 1.34f;
            float archAmp = 0.82f;
            int archSegments = 10;

            Vector3 prevTop = new Vector3(-halfW, 0f, 0f);
            Vector3 prevBot = new Vector3(-halfW, 0f, 0f);

            for (int s = 1; s <= archSegments; s++)
            {
                float t = (float)s / archSegments;
                float x = Mathf.Lerp(-halfW, halfW, t);

                // Sinüs yay profili (uçlarda 0, ortada maksimum yay)
                float u = (x / halfW) * (Mathf.PI * 0.5f);
                float yVal = Mathf.Cos(u) * archAmp;
                float zVal = Mathf.Sin(u) * 0.20f;

                Vector3 curTop = new Vector3(x, yVal, zVal);
                Vector3 curBot = new Vector3(x, -yVal, -zVal);

                var segTop = CreateRailSegment(prevTop, curTop, 0.10f, matRibbonBlue, "TopRailSeg_" + s);
                segTop.transform.SetParent(transform, false);
                topArchSegments.Add(segTop.transform);

                var segBot = CreateRailSegment(prevBot, curBot, 0.10f, matRibbonOrange, "BotRailSeg_" + s);
                segBot.transform.SetParent(transform, false);
                botArchSegments.Add(segBot.transform);

                prevTop = curTop;
                prevBot = curBot;
            }

            // 4. Şemadaki 4 Dikey Watson-Crick Nükleotid Basamağı (Base-Pair Rungs)
            float[] rungX = new float[] { -0.80f, -0.27f, 0.27f, 0.80f };
            for (int r = 0; r < 4; r++)
            {
                float rx = rungX[r];
                float u = (rx / halfW) * (Mathf.PI * 0.5f);
                float rHeight = Mathf.Cos(u) * archAmp;

                var rung = new BasePairRung();
                rung.localX = rx;

                // Üst Nükleotid Yarım Çubuğu
                GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                topBar.name = $"Rung_{r}_Top";
                topBar.transform.SetParent(transform, false);
                topBar.transform.localPosition = new Vector3(rx, rHeight * 0.5f, 0f);
                topBar.transform.localScale = new Vector3(0.08f, rHeight * 0.5f, 0.08f);
                Destroy(topBar.GetComponent<Collider>());
                rung.upperMat = new Material(litShader);
                topBar.GetComponent<MeshRenderer>().material = rung.upperMat;
                rung.upperHalf = topBar.transform;

                // Alt Nükleotid Yarım Çubuğu
                GameObject botBar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                botBar.name = $"Rung_{r}_Bot";
                botBar.transform.SetParent(transform, false);
                botBar.transform.localPosition = new Vector3(rx, -rHeight * 0.5f, 0f);
                botBar.transform.localScale = new Vector3(0.08f, rHeight * 0.5f, 0.08f);
                origRungScales[r] = botBar.transform.localScale;
                Destroy(botBar.GetComponent<Collider>());
                rung.lowerMat = new Material(litShader);
                botBar.GetComponent<MeshRenderer>().material = rung.lowerMat;
                rung.lowerHalf = botBar.transform;

                // Merkez Hidrojen Bağı Düğümü (H-Bond Node)
                GameObject hNode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hNode.name = $"Rung_{r}_HNode";
                hNode.transform.SetParent(transform, false);
                hNode.transform.localPosition = new Vector3(rx, 0f, 0f);
                hNode.transform.localScale = Vector3.one * 0.16f;
                Destroy(hNode.GetComponent<Collider>());
                hNode.GetComponent<MeshRenderer>().material = matHBarCore;
                rung.hBondNode = hNode.transform;

                // Üst Nükleotid Harfi (C, A, T, G) (Net Yüksek Çözünürlüklü Vektör Kalitesi)
                rung.topText = CreateTextMesh($"Rung_{r}_TopText", new Vector3(rx, rHeight + 0.18f, -0.08f), 0.026f, 96, Color.white, FontStyle.Bold);

                // Alt Nükleotid Harfi (G, T, A, C)
                rung.botText = CreateTextMesh($"Rung_{r}_BotText", new Vector3(rx, -rHeight - 0.18f, -0.08f), 0.026f, 96, Color.white, FontStyle.Bold);

                basePairs[r] = rung;
            }

            // 5. Merkez Siber Gen Bilgi Plaketi (Sarmalın Derinlik Alanında Yüzen Saydam Kart)
            GameObject centerPlaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerPlaque.name = "CenterPlaque";
            centerPlaque.transform.SetParent(transform, false);
            centerPlaque.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            centerPlaque.transform.localScale = new Vector3(1.88f, 0.48f, 0.02f);
            Destroy(centerPlaque.GetComponent<Collider>());
            var plaqueMat = new Material(unlitShader);
            plaqueMat.color = new Color(0.03f, 0.06f, 0.11f, 0.94f);
            centerPlaque.GetComponent<MeshRenderer>().material = plaqueMat;
            centerPlaqueTrans = centerPlaque.transform;

            // Plaket Siber Çerçevesi (Neon Vurgulu Kenar)
            GameObject borderPlaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            borderPlaque.name = "BorderPlaque";
            borderPlaque.transform.SetParent(transform, false);
            borderPlaque.transform.localPosition = new Vector3(0f, 0f, -0.14f);
            borderPlaque.transform.localScale = new Vector3(1.94f, 0.54f, 0.015f);
            Destroy(borderPlaque.GetComponent<Collider>());
            var borderMat = new Material(unlitShader);
            borderMat.color = new Color(geneColor.r * 0.95f, geneColor.g * 0.95f, geneColor.b * 0.95f, 0.85f);
            borderPlaque.GetComponent<MeshRenderer>().material = borderMat;
            borderPlaqueTrans = borderPlaque.transform;
            borderPlaqueMat = borderMat;

            // 6. Sol Puzzle Ucu (Nükleotid Dişi Soketi & Geçmeli Bilezik)
            GameObject leftPlug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftPlug.name = "LeftConnector";
            leftPlug.transform.SetParent(transform, false);
            leftPlug.transform.localPosition = new Vector3(-1.38f, 0f, 0f);
            leftPlug.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            Destroy(leftPlug.GetComponent<Collider>());
            leftPlugMat = new Material(litShader);
            leftPlugMat.color = DNAHelper.GetNucleotideColor(Data.leftConnector);
            leftPlug.GetComponent<MeshRenderer>().material = leftPlugMat;

            // Sol Dişi Geçmeli Soket Bileziği (Female Puzzle Ring)
            GameObject leftCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftCollar.name = "LeftPuzzleCollar";
            leftCollar.transform.SetParent(transform, false);
            leftCollar.transform.localPosition = new Vector3(-1.40f, 0f, 0f);
            leftCollar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            leftCollar.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);
            Destroy(leftCollar.GetComponent<Collider>());
            leftCollarMat = new Material(litShader);
            leftCollarMat.color = new Color(0.18f, 0.28f, 0.42f);
            if (leftCollarMat.HasProperty("_Smoothness")) leftCollarMat.SetFloat("_Smoothness", 0.9f);
            leftCollar.GetComponent<MeshRenderer>().material = leftCollarMat;

            // 7. Sağ Puzzle Ucu (Nükleotid Erkek Ucu & Geçmeli Kilit Pimi)
            GameObject rightPlug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightPlug.name = "RightConnector";
            rightPlug.transform.SetParent(transform, false);
            rightPlug.transform.localPosition = new Vector3(1.38f, 0f, 0f);
            rightPlug.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            Destroy(rightPlug.GetComponent<Collider>());
            rightPlugMat = new Material(litShader);
            rightPlugMat.color = DNAHelper.GetNucleotideColor(Data.rightConnector);
            rightPlug.GetComponent<MeshRenderer>().material = rightPlugMat;

            // Sağ Erkek Geçmeli Kilit Pimi (Male Puzzle Key Tab)
            GameObject rightKey = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightKey.name = "RightPuzzleKey";
            rightKey.transform.SetParent(transform, false);
            rightKey.transform.localPosition = new Vector3(1.50f, 0f, 0f);
            rightKey.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rightKey.transform.localScale = new Vector3(0.24f, 0.14f, 0.24f);
            Destroy(rightKey.GetComponent<Collider>());
            rightKeyMat = new Material(litShader);
            rightKeyMat.color = DNAHelper.GetNucleotideColor(Data.rightConnector);
            if (rightKeyMat.HasProperty("_Smoothness")) rightKeyMat.SetFloat("_Smoothness", 0.9f);
            rightKey.GetComponent<MeshRenderer>().material = rightKeyMat;

            // 8. İç Parıltı Işığı
            GameObject lightObj = new GameObject("CoreLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            coreLight = lightObj.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 2.6f;
            coreLight.intensity = 1.3f;
            coreLight.color = geneColor;

            // 9. Cömert Dokunma Collider'ı (Tüm Kıvrımlı Alana Uygun)
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(2.85f, 2.20f, 0.65f);

            // 10. İsim ve Kompakt Metinler (Kristal Netlikte Anti-Aliased Yazı Tipi)
            nameTextMesh = CreateTextMesh("NameText", new Vector3(0f, 0.10f, -0.19f), 0.024f, 96, Color.white, FontStyle.Bold);
            statsTextMesh = CreateTextMesh("StatsText", new Vector3(0f, -0.10f, -0.19f), 0.020f, 84, new Color(0.85f, 0.95f, 1f), FontStyle.Bold);

            leftNucMesh = CreateTextMesh("LeftNuc", new Vector3(-1.38f, 0f, -0.22f), 0.028f, 96, Color.white, FontStyle.Bold);
            rightNucMesh = CreateTextMesh("RightNuc", new Vector3(1.38f, 0f, -0.22f), 0.028f, 96, Color.white, FontStyle.Bold);

            // 11. Durum Rozeti (⚠️ BOZUK GENOM / ✅ ONARILDI) - Üst nükleotid harflerine basmaması için y=1.22f'ye alındı
            statusBadgeMesh = CreateTextMesh("StatusBadge", new Vector3(0f, 1.22f, -0.20f), 0.020f, 84, new Color(1f, 0.4f, 0.2f), FontStyle.Bold);
        }

        private GameObject CreateRailSegment(Vector3 p1, Vector3 p2, float radius, Material mat, string name)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            Destroy(cyl.GetComponent<Collider>());

            Vector3 dir = p2 - p1;
            float len = dir.magnitude;
            cyl.transform.localPosition = (p1 + p2) * 0.5f;
            cyl.transform.localScale = new Vector3(radius, len * 0.5f, radius);
            if (len > 0.0001f)
            {
                cyl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }
            cyl.GetComponent<MeshRenderer>().material = mat;
            return cyl;
        }

        private TextMesh CreateTextMesh(string objName, Vector3 localPos, float charSize, int fontSize, Color color, FontStyle style)
        {
            GameObject textObj = new GameObject(objName);
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = localPos;

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.characterSize = charSize;
            tm.fontSize = fontSize;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            tm.fontStyle = style;
            return tm;
        }

        public void UpdateVisuals()
        {
            if (Data == null) return;

            // 1. İsim ve Stat Özeti (Yüksek Çözünürlüklü Vektörel Netlik)
            FitTextMesh(nameTextMesh, Data.geneName, maxAllowedWidth: 1.76f, baseCharSize: 0.024f);
            string compactStats = FormatUltraCompactStats(Data);
            FitTextMesh(statsTextMesh, compactStats, maxAllowedWidth: 1.76f, baseCharSize: 0.020f);

            // 2. Sol ve Sağ Puzzle Uçları
            string leftLetter = Data.leftConnector == NucleotideType.Universal ? "★" : Data.leftConnector.ToString();
            string rightLetter = Data.rightConnector == NucleotideType.Universal ? "★" : Data.rightConnector.ToString();
            leftNucMesh.text = leftLetter;
            leftNucMesh.color = DNAHelper.GetNucleotideColor(Data.leftConnector);
            rightNucMesh.text = rightLetter;
            rightNucMesh.color = DNAHelper.GetNucleotideColor(Data.rightConnector);

            if (leftPlugMat != null) leftPlugMat.color = DNAHelper.GetNucleotideColor(Data.leftConnector);
            if (rightPlugMat != null) rightPlugMat.color = DNAHelper.GetNucleotideColor(Data.rightConnector);
            if (rightKeyMat != null) rightKeyMat.color = DNAHelper.GetNucleotideColor(Data.rightConnector);

            // 3. 4 Adet Watson-Crick Nükleotid Basamağını Yapılandır (Şemadaki gibi)
            NucleotideType[] topSeq = new NucleotideType[4];
            NucleotideType[] botSeq = new NucleotideType[4];

            // Rung 0: Sol Uç Nükleotidi & Tamamlayıcısı
            topSeq[0] = Data.leftConnector;
            botSeq[0] = DNAHelper.GetComplement(Data.leftConnector);

            // Rung 1 & 2: Gen Kategorisine Göre Nükleotid Eşleri
            NucleotideType inner1 = NucleotideType.A;
            switch (Data.category)
            {
                case GeneCategory.Exoskeleton: inner1 = NucleotideType.C; break;
                case GeneCategory.Metabolic: inner1 = NucleotideType.T; break;
                case GeneCategory.Cellular: inner1 = NucleotideType.A; break;
                case GeneCategory.Structural: inner1 = NucleotideType.G; break;
                case GeneCategory.Cosmic: inner1 = NucleotideType.C; break;
                case GeneCategory.Mutagen: inner1 = NucleotideType.Universal; break;
            }
            topSeq[1] = inner1;
            botSeq[1] = DNAHelper.GetComplement(inner1);

            NucleotideType inner2 = DNAHelper.GetComplement(inner1);
            topSeq[2] = inner2;
            botSeq[2] = DNAHelper.GetComplement(inner2);

            // Rung 3: Sağ Uç Nükleotidi & Tamamlayıcısı
            topSeq[3] = Data.rightConnector;
            botSeq[3] = DNAHelper.GetComplement(Data.rightConnector);

            // Renkleri ve Harfleri Basamaklara Ata
            for (int r = 0; r < 4; r++)
            {
                var bp = basePairs[r];
                var topN = topSeq[r];
                var botN = botSeq[r];

                Color cTop = DNAHelper.GetNucleotideColor(topN);
                Color cBot = DNAHelper.GetNucleotideColor(botN);

                if (bp.upperMat != null) bp.upperMat.color = cTop;
                if (bp.lowerMat != null) bp.lowerMat.color = cBot;

                string tStr = topN == NucleotideType.Universal ? "★" : topN.ToString();
                string bStr = botN == NucleotideType.Universal ? "★" : botN.ToString();

                if (bp.topText != null)
                {
                    bp.topText.text = tStr;
                    bp.topText.color = cTop;
                }
                if (bp.botText != null)
                {
                    bp.botText.text = bStr;
                    bp.botText.color = cBot;
                }

                // Bozuk / Düzenlenen Basamak Efekti
                bool isTargetRung = (r == Mathf.Clamp(Data.brokenRungIndex, 0, 3));
                bool isThisRungBroken = (Data.isBroken && !Data.isRepaired && isTargetRung);
                if (isThisRungBroken)
                {
                    if (Data.hasPlacedNucleotide)
                    {
                        Color wrongCol = DNAHelper.GetNucleotideColor(Data.currentPlacedNucleotide);
                        if (bp.botText != null)
                        {
                            bp.botText.text = $"{Data.currentPlacedNucleotide}!";
                            bp.botText.color = new Color(1f, 0.45f, 0.25f);
                        }
                        if (bp.lowerMat != null)
                        {
                            bp.lowerMat.color = wrongCol * 0.75f;
                        }
                        if (bp.lowerHalf != null)
                        {
                            Vector3 orig = origRungScales[r];
                            bp.lowerHalf.localScale = new Vector3(orig.x * 0.85f, orig.y * 0.78f, orig.z * 0.85f);
                        }
                    }
                    else
                    {
                        if (bp.botText != null)
                        {
                            bp.botText.text = "?";
                            bp.botText.color = new Color(1f, 0.85f, 0.2f);
                        }
                        if (bp.lowerMat != null)
                        {
                            bp.lowerMat.color = new Color(0.9f, 0.2f, 0.2f, 0.45f);
                        }
                        if (bp.lowerHalf != null)
                        {
                            Vector3 orig = origRungScales[r];
                            bp.lowerHalf.localScale = new Vector3(orig.x * 0.45f, orig.y * 0.35f, orig.z * 0.45f);
                        }
                    }

                    if (bp.hBondNode != null)
                    {
                        var mr = bp.hBondNode.GetComponent<MeshRenderer>();
                        if (mr != null) mr.material.color = new Color(1f, 0.25f, 0.2f);
                    }
                }
                else
                {
                    if (bp.lowerHalf != null)
                    {
                        bp.lowerHalf.localScale = origRungScales[r];
                    }
                    if (bp.hBondNode != null)
                    {
                        var mr = bp.hBondNode.GetComponent<MeshRenderer>();
                        if (mr != null) mr.material.color = isTargetRung ? new Color(0.35f, 1f, 0.6f) : Color.white;
                    }
                }
            }

            // Durum Rozeti ve Deformasyon (Yanma) Görselleri
            if (Data.isDeformed)
            {
                if (statusBadgeMesh != null)
                {
                    statusBadgeMesh.text = "☣️ DEFORME GENOM (Yandı!)";
                    statusBadgeMesh.color = new Color(1f, 0.22f, 0.22f);
                }
                if (primaryMaterial != null)
                {
                    primaryMaterial.color = new Color(0.22f, 0.08f, 0.12f);
                }
                if (borderPlaqueMat != null)
                {
                    borderPlaqueMat.color = new Color(0.95f, 0.15f, 0.20f, 0.95f);
                }
                if (coreLight != null)
                {
                    coreLight.color = new Color(1f, 0.15f, 0.15f);
                    coreLight.intensity = 1.8f;
                }
            }
            else
            {
                if (primaryMaterial != null)
                {
                    primaryMaterial.color = Data.customColor;
                }
                if (borderPlaqueMat != null)
                {
                    borderPlaqueMat.color = new Color(Data.customColor.r * 0.95f, Data.customColor.g * 0.95f, Data.customColor.b * 0.95f, 0.85f);
                }
                if (coreLight != null)
                {
                    coreLight.color = Data.customColor;
                    coreLight.intensity = baseLightIntensity;
                }

                if (statusBadgeMesh != null)
                {
                    if (IsEquipped)
                    {
                        statusBadgeMesh.text = "🔗 SARMALDA";
                        statusBadgeMesh.color = new Color(0.35f, 0.9f, 1f);
                    }
                    else if (Data.isRepaired || !Data.isBroken)
                    {
                        statusBadgeMesh.text = "✅ ONARILDI";
                        statusBadgeMesh.color = new Color(0.3f, 1f, 0.5f);
                    }
                    else
                    {
                        statusBadgeMesh.text = "⚠️ BOZUK GENOM";
                        statusBadgeMesh.color = new Color(1f, 0.35f, 0.2f);
                    }
                }
            }
        }

        private void FitTextMesh(TextMesh tm, string text, float maxAllowedWidth, float baseCharSize)
        {
            tm.text = text;
            tm.characterSize = baseCharSize;

            var rend = tm.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                float w = rend.bounds.size.x;
                float lossyX = transform.lossyScale.x;
                if (lossyX > 0.001f) w /= lossyX;

                if (w > maxAllowedWidth && w > 0.001f)
                {
                    float factor = maxAllowedWidth / w;
                    tm.characterSize = baseCharSize * factor;
                }
            }
        }

        private string FormatUltraCompactStats(GeneData data)
        {
            if (data == null || data.statModifiers == null || data.statModifiers.Count == 0)
                return $"[{data?.energyCost ?? 0}E]";

            var parts = new List<string>();
            foreach (var mod in data.statModifiers)
            {
                string sign = mod.value >= 0 ? "+" : "";
                string sName = "";
                switch (mod.stat)
                {
                    case StatType.HeatResistance: sName = "Isı"; break;
                    case StatType.ColdResistance: sName = "Soğuk"; break;
                    case StatType.RadiationShield: sName = "Rad"; break;
                    case StatType.ToxicityFilter: sName = "Zehir"; break;
                    case StatType.PressureTolerance: sName = "Basınç"; break;
                    case StatType.OxygenEfficiency: sName = "Oks"; break;
                    default: sName = mod.stat.ToString().Substring(0, 3); break;
                }
                parts.Add($"{sign}{mod.value} {sName}");
            }

            string statsJoined = string.Join("  ", parts);
            return $"{statsJoined}  [{data.energyCost}E]";
        }

        public void TriggerTouchPulse()
        {
            touchPulseTimer = 1.0f;
        }

        public void NotifyClicked()
        {
            TriggerTouchPulse();
            OnPieceClicked?.Invoke(this);
        }

        public void SetZoomedInRepairLab(bool zoomed)
        {
            isZoomedInLab = zoomed;
            // Odak modunda plaket alt nükleotid ve soru işaretlerine basmaması için -1.42f seviyesine indirildi
            float plaqueY = zoomed ? -1.42f : 0f;
            if (centerPlaqueTrans != null)
                centerPlaqueTrans.localPosition = new Vector3(0f, plaqueY, -0.15f);
            if (borderPlaqueTrans != null)
                borderPlaqueTrans.localPosition = new Vector3(0f, plaqueY, -0.14f);
            if (nameTextMesh != null)
                nameTextMesh.transform.localPosition = new Vector3(0f, plaqueY + 0.10f, -0.19f);
            if (statsTextMesh != null)
                statsTextMesh.transform.localPosition = new Vector3(0f, plaqueY - 0.10f, -0.19f);

            // Odak modundayken üst durum rozetini gizle ki üst nükleotid harflerini kapatmasın
            if (statusBadgeMesh != null)
            {
                statusBadgeMesh.gameObject.SetActive(!zoomed);
            }
        }

        public void MoveTo(Vector3 worldPos, Quaternion worldRot, float duration = 0.32f)
        {
            targetPosition = worldPos;
            targetRotation = worldRot;
            touchPulseTimer = 1.0f;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (gameObject.activeInHierarchy && duration > 0.01f)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateToTargetRoutine(worldPos, worldRot, duration));
            }
            else
            {
                isMoving = false;
                transform.position = worldPos;
                transform.rotation = worldRot;
                transform.localScale = targetScale;
                if (coreLight != null) coreLight.intensity = baseLightIntensity;
            }
        }

        private void OnDisable()
        {
            isMoving = false;
            isHovered = false;
            if (targetScale.sqrMagnitude > 0.001f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                transform.localScale = targetScale;
            }
            if (coreLight != null) coreLight.intensity = baseLightIntensity;
        }

        private IEnumerator AnimateToTargetRoutine(Vector3 endPos, Quaternion endRot, float duration)
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float bounceT = Mathf.Sin(t * Mathf.PI * 0.5f);
                float arcY = Mathf.Sin(t * Mathf.PI) * 0.75f;
                float arcZ = -Mathf.Sin(t * Mathf.PI) * 0.45f;

                Vector3 currentPos = Vector3.Lerp(startPos, endPos, bounceT);
                currentPos.y += arcY;
                currentPos.z += arcZ;

                transform.position = currentPos;
                transform.rotation = Quaternion.Slerp(startRot, endRot, bounceT) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 8f);

                float squash = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                transform.localScale = targetScale * squash;

                if (coreLight != null)
                {
                    coreLight.intensity = baseLightIntensity + Mathf.Sin(t * Mathf.PI) * 2.2f;
                }

                yield return null;
            }

            // İniş anında elastik yaylanma
            float settleElapsed = 0f;
            float settleDur = 0.14f;
            while (settleElapsed < settleDur)
            {
                settleElapsed += Time.deltaTime;
                float st = Mathf.Clamp01(settleElapsed / settleDur);
                float elastic = 1f + Mathf.Sin(st * Mathf.PI * 2f) * 0.14f * (1f - st);
                transform.position = endPos;
                transform.rotation = endRot;
                transform.localScale = new Vector3(targetScale.x * elastic, targetScale.y * (2f - elastic), targetScale.z * elastic);
                yield return null;
            }

            transform.position = endPos;
            transform.rotation = endRot;
            transform.localScale = targetScale;
            if (coreLight != null) coreLight.intensity = baseLightIntensity;
            isMoving = false;
        }

        private void Update()
        {
            if (IsBeingDragged) return;

            if (touchPulseTimer > 0f)
            {
                touchPulseTimer = Mathf.Max(0f, touchPulseTimer - Time.deltaTime * 4f);
            }

            if (isMoving) return;

            float pulseBoost = Mathf.Sin(touchPulseTimer * Mathf.PI) * 0.12f;

            if (!IsEquipped)
            {
                float floatY = Mathf.Sin((Time.time * hoverSpeed) + randomPhase) * hoverAmount;
                float hoverZ = isHovered ? -0.35f : 0f;
                transform.position = Vector3.Lerp(transform.position, targetPosition + new Vector3(0f, floatY, hoverZ), Time.deltaTime * 12f);

                float targetRotY = isHovered ? 12f : 0f;
                float targetRotX = isHovered ? -6f : 0f;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetRotX, targetRotY, 0f), Time.deltaTime * 8f);

                float hoverMul = (isHovered ? 1.14f : 1.0f) + pulseBoost;
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale * hoverMul, Time.deltaTime * 12f);

                if (coreLight != null)
                {
                    coreLight.intensity = Mathf.Lerp(coreLight.intensity, isHovered ? 2.6f : baseLightIntensity, Time.deltaTime * 10f);
                }
            }
            else
            {
                float hoverMul = (isHovered ? 1.08f : 1.0f) + pulseBoost;
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale * hoverMul, Time.deltaTime * 12f);
                if (coreLight != null)
                {
                    coreLight.intensity = Mathf.Lerp(coreLight.intensity, isHovered ? 2.4f : 1.6f, Time.deltaTime * 10f);
                }
            }
        }

        public void SetTargetScale(Vector3 scale)
        {
            targetScale = scale;
        }

        public void SetHovered(bool hovered)
        {
            if (isHovered != hovered)
            {
                isHovered = hovered;
                OnPieceHoverStateChanged?.Invoke(this, hovered);
            }
        }

        public void SetSelectedForRepair(bool selected)
        {
            IsSelectedForRepair = selected;
            if (coreLight != null)
            {
                coreLight.intensity = selected ? 2.8f : baseLightIntensity;
                coreLight.range = selected ? 3.8f : 2.6f;
            }
        }

        public void SetConnectorGlow(bool isLeft, bool glow, bool complementary)
        {
            if (Data == null) return;
            Color baseCol = DNAHelper.GetNucleotideColor(isLeft ? Data.leftConnector : Data.rightConnector);
            Color glowCol = complementary ? new Color(0.2f, 1f, 0.45f) : new Color(1f, 0.4f, 0.3f);
            if (isLeft)
            {
                if (leftCollarMat != null) leftCollarMat.color = glow ? glowCol : new Color(0.18f, 0.28f, 0.42f);
                if (leftPlugMat != null) leftPlugMat.color = glow ? glowCol : baseCol;
            }
            else
            {
                if (rightKeyMat != null) rightKeyMat.color = glow ? glowCol : baseCol;
                if (rightPlugMat != null) rightPlugMat.color = glow ? glowCol : baseCol;
            }
        }

        public void PurifyAndResetDeformed()
        {
            if (Data == null) return;
            Data.ResetRepairAttempts();
            Data.isBroken = true;
            Data.isRepaired = false;
            Data.hasPlacedNucleotide = false;
            UpdateVisuals();
            TriggerTouchPulse();
        }

        public void ResetRepairState()
        {
            if (Data == null) return;
            Data.isBroken = true;
            Data.isRepaired = false;
            Data.hasPlacedNucleotide = false;
            UpdateVisuals();
            TriggerTouchPulse();
        }

        public bool TryRepairWith(NucleotideType nuc)
        {
            if (Data == null) return false;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            Data.hasPlacedNucleotide = true;
            Data.currentPlacedNucleotide = nuc;

            if (nuc == Data.requiredRepairNucleotide)
            {
                Data.isRepaired = true;
                Data.isBroken = false;
                Data.isDeformed = false;
                UpdateVisuals();
                if (gameObject.activeInHierarchy)
                {
                    StopCoroutine("RepairSuccessRoutine");
                    StartCoroutine(RepairSuccessRoutine());
                }
                return true;
            }

            // Uyumsuz / Yanlış nükleotid seçildi! Kalan hakkı azalt
            Data.isRepaired = false;
            Data.isBroken = true;
            if (Data.remainingRepairAttempts > 0)
            {
                Data.remainingRepairAttempts--;
            }

            if (Data.remainingRepairAttempts <= 0)
            {
                Data.MarkDeformed();
            }

            UpdateVisuals();
            TriggerGlitchShake();
            return false;
        }

        private IEnumerator RepairSuccessRoutine()
        {
            float elapsed = 0f;
            Vector3 origScale = targetScale;
            while (elapsed < 0.38f)
            {
                elapsed += Time.deltaTime;
                float s = 1f + Mathf.Sin((elapsed / 0.38f) * Mathf.PI) * 0.28f;
                transform.localScale = origScale * s;
                yield return null;
            }
            transform.localScale = origScale;
        }

        public void TriggerGlitchShake()
        {
            if (!gameObject.activeInHierarchy) return;
            StopCoroutine("GlitchShakeRoutine");
            StartCoroutine(GlitchShakeRoutine());
        }

        private IEnumerator GlitchShakeRoutine()
        {
            float elapsed = 0f;
            Vector3 baseP = targetPosition;
            while (elapsed < 0.30f)
            {
                elapsed += Time.deltaTime;
                float xOff = Mathf.Sin(elapsed * 55f) * 0.14f;
                transform.position = baseP + new Vector3(xOff, 0f, 0f);
                yield return null;
            }
            transform.position = baseP;
        }
    }
}