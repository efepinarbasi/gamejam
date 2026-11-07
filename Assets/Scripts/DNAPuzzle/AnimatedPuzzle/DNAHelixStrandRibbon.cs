using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Gerçek bir DNA Çift Sarmalını (Double Helix) temsil eden sürekli 3D omurga şeridi.
    /// Resimdeki gibi biri Mavi (Azure Blue) diğeri Turuncu (Coral Orange) iki sürekli sarmal şerit,
    /// X ekseni boyunca sinüzoidal dalga çizerek birbirinin etrafında döner ve yuvaların (Genlerin)
    /// içinden geçer. Sınır noktalarında birbirini çaprazlar (Crossover nodes).
    /// </summary>
    public class DNAHelixStrandRibbon : MonoBehaviour
    {
        private List<Transform> ribbonSegmentsA = new List<Transform>(); // Mavi Şerit (Strand 1)
        private List<Transform> ribbonSegmentsB = new List<Transform>(); // Turuncu Şerit (Strand 2)
        private List<Transform> crossoverNodes = new List<Transform>();  // Çaprazlama Düğümleri
        private List<Transform> transferConduits = new List<Transform>();// Proteinlere inen enerji kanalları

        private Material matRibbonBlue;
        private Material matRibbonOrange;
        private Material matCrossover;

        private float startX;
        private float spacingX;
        private int slotCount;
        private float slotY;
        private float amplitude = 0.84f;
        private float depth = 0.32f;
        private float bioWaveTimer = 0f;

        public void Initialize(int count, float sX, float spX, float sY)
        {
            slotCount = count;
            startX = sX;
            spacingX = spX;
            slotY = sY;

            BuildDoubleHelixGeometry();
        }

        private void BuildDoubleHelixGeometry()
        {
            // Eski parçaları temizle
            foreach (var t in ribbonSegmentsA) if (t != null) Destroy(t.gameObject);
            foreach (var t in ribbonSegmentsB) if (t != null) Destroy(t.gameObject);
            foreach (var t in crossoverNodes) if (t != null) Destroy(t.gameObject);
            foreach (var t in transferConduits) if (t != null) Destroy(t.gameObject);

            ribbonSegmentsA.Clear();
            ribbonSegmentsB.Clear();
            crossoverNodes.Clear();
            transferConduits.Clear();

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            // 1. Mavi Şerit Materyali (Azure Blue - Resimdeki gibi)
            matRibbonBlue = new Material(litShader);
            matRibbonBlue.color = new Color(0.12f, 0.58f, 0.98f);
            if (matRibbonBlue.HasProperty("_Smoothness")) matRibbonBlue.SetFloat("_Smoothness", 0.92f);
            if (matRibbonBlue.HasProperty("_Metallic")) matRibbonBlue.SetFloat("_Metallic", 0.40f);
            if (matRibbonBlue.HasProperty("_EmissionColor"))
            {
                matRibbonBlue.EnableKeyword("_EMISSION");
                matRibbonBlue.SetColor("_EmissionColor", new Color(0.08f, 0.45f, 0.85f) * 0.55f);
            }

            // 2. Turuncu Şerit Materyali (Coral Flame - Resimdeki gibi)
            matRibbonOrange = new Material(litShader);
            matRibbonOrange.color = new Color(0.96f, 0.42f, 0.12f);
            if (matRibbonOrange.HasProperty("_Smoothness")) matRibbonOrange.SetFloat("_Smoothness", 0.92f);
            if (matRibbonOrange.HasProperty("_Metallic")) matRibbonOrange.SetFloat("_Metallic", 0.40f);
            if (matRibbonOrange.HasProperty("_EmissionColor"))
            {
                matRibbonOrange.EnableKeyword("_EMISSION");
                matRibbonOrange.SetColor("_EmissionColor", new Color(0.90f, 0.35f, 0.08f) * 0.55f);
            }

            // 3. Düğüm Materyali (Kromozom Birleşme Noktası)
            matCrossover = new Material(litShader);
            matCrossover.color = new Color(0.85f, 0.92f, 1f);
            if (matCrossover.HasProperty("_Smoothness")) matCrossover.SetFloat("_Smoothness", 0.95f);

            float totalLength = (slotCount - 1) * spacingX;
            float leftBound = startX - spacingX * 0.40f;
            float rightBound = (startX + totalLength) + spacingX * 0.40f;
            float totalSpan = rightBound - leftBound;

            int stepsPerSlot = 18;
            int totalSteps = Mathf.RoundToInt((slotCount + 0.80f) * stepsPerSlot);

            Vector3 prevPosA = Vector3.zero;
            Vector3 prevPosB = Vector3.zero;

            for (int s = 0; s <= totalSteps; s++)
            {
                float t = (float)s / totalSteps;
                float gx = Mathf.Lerp(leftBound, rightBound, t);

                // Matematiksel DNA Çift Sarmal Dalgası:
                // Her yuva aralığı (spacingX) tam bir yarı dalga boyuna denk gelir (PI fazı).
                // Böylece her yuvanın ortasında şeritler maksimum açılır, sınırlarında çaprazlar!
                float phase = ((gx - (startX - spacingX * 0.5f)) / spacingX) * Mathf.PI;

                float yA = slotY + Mathf.Sin(phase) * amplitude;
                float zA = Mathf.Cos(phase) * depth;

                float yB = slotY - Mathf.Sin(phase) * amplitude;
                float zB = -Mathf.Cos(phase) * depth;

                Vector3 curPosA = new Vector3(gx, yA, zA);
                Vector3 curPosB = new Vector3(gx, yB, zB);

                if (s > 0)
                {
                    // Mavi Şerit Tüp Segmenti
                    GameObject segA = CreateCylinderSegment(prevPosA, curPosA, 0.11f, matRibbonBlue, "RibbonA_Seg_" + s);
                    segA.transform.SetParent(transform, true);
                    ribbonSegmentsA.Add(segA.transform);

                    // Turuncu Şerit Tüp Segmenti
                    GameObject segB = CreateCylinderSegment(prevPosB, curPosB, 0.11f, matRibbonOrange, "RibbonB_Seg_" + s);
                    segB.transform.SetParent(transform, true);
                    ribbonSegmentsB.Add(segB.transform);
                }

                prevPosA = curPosA;
                prevPosB = curPosB;
            }

            // Çaprazlama Düğümleri (Her slotun sol ve sağ sınırında bulunan helezon halkaları)
            for (int i = 0; i <= slotCount; i++)
            {
                float nodeX = (startX - spacingX * 0.5f) + (i * spacingX);
                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = $"CrossoverNode_{i}";
                nodeObj.transform.SetParent(transform, false);
                nodeObj.transform.position = new Vector3(nodeX, slotY, 0f);
                nodeObj.transform.localScale = Vector3.one * 0.32f;
                Destroy(nodeObj.GetComponent<Collider>());
                nodeObj.GetComponent<MeshRenderer>().material = matCrossover;
                crossoverNodes.Add(nodeObj.transform);
            }

            // Resimdeki "Proteinler" / Organizma Yönü Okları (Her genin altından aşağıya inen zarif biyo-enerji hatları)
            for (int i = 0; i < slotCount; i++)
            {
                float slotCenterX = startX + i * spacingX;
                GameObject conduit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                conduit.name = $"ProteinConduit_{i}";
                conduit.transform.SetParent(transform, false);
                conduit.transform.position = new Vector3(slotCenterX, slotY - 1.35f, 0.2f);
                conduit.transform.localScale = new Vector3(0.04f, 0.35f, 0.04f);
                Destroy(conduit.GetComponent<Collider>());
                var conduitMat = new Material(unlitShader);
                conduitMat.color = new Color(0.2f, 0.85f, 1f, 0.35f);
                conduit.GetComponent<MeshRenderer>().material = conduitMat;
                transferConduits.Add(conduit.transform);
            }
        }

        private GameObject CreateCylinderSegment(Vector3 p1, Vector3 p2, float radius, Material mat, string name)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            Destroy(cyl.GetComponent<Collider>());

            Vector3 dir = p2 - p1;
            float len = dir.magnitude;
            cyl.transform.position = (p1 + p2) * 0.5f;
            cyl.transform.localScale = new Vector3(radius, len * 0.5f, radius);
            if (len > 0.0001f)
            {
                cyl.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            }
            cyl.GetComponent<MeshRenderer>().material = mat;
            return cyl;
        }

        private void Update()
        {
            bioWaveTimer += Time.deltaTime * 2.2f;

            // Düğümlerde hafif nabız parıldaması
            float pulse = 1f + Mathf.Sin(bioWaveTimer * 2f) * 0.08f;
            for (int i = 0; i < crossoverNodes.Count; i++)
            {
                if (crossoverNodes[i] != null)
                {
                    crossoverNodes[i].localScale = Vector3.one * (0.32f * pulse);
                }
            }

            // Aşağıya inen protein kanallarının biyo-akış hareketi
            for (int i = 0; i < transferConduits.Count; i++)
            {
                if (transferConduits[i] != null)
                {
                    float flow = Mathf.Sin(bioWaveTimer * 3f + i) * 0.08f;
                    transferConduits[i].localPosition = new Vector3(
                        startX + i * spacingX, 
                        slotY - 1.35f + flow, 
                        0.2f
                    );
                }
            }
        }
    }
}