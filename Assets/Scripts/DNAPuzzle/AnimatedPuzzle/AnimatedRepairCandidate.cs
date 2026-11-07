using System;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Ekranın alt kısmında yer alan, bozuk genomları onarmak için seçilen 3D Biyo-Onarım Parçası (Nükleotid Kapsülü).
    /// Oyuncu yukarıdaki 4 bozuk genomdan birini seçtiğinde, alttaki bu doğru parçalardan birine tıklayarak
    /// genomun eksik baz çiftini onarır.
    /// </summary>
    public class AnimatedRepairCandidate : MonoBehaviour
    {
        public NucleotideType Nucleotide { get; private set; }
        public int CandidateIndex { get; private set; }
        public bool IsUsed { get; private set; } = false;
        public bool IsBeingDragged { get; set; } = false;

        private Material baseMat;
        private Material coreMat;
        private Light glowLight;
        private TextMesh symbolTextMesh;
        private TextMesh labelTextMesh;
        private TextMesh pairHintTextMesh;

        private Vector3 targetLocalPosition;
        private Vector3 basePosition;
        private Vector3 baseScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;
        private bool isHovered = false;
        private float hoverPhase = 0f;
        private float pulseTimer = 0f;

        public event Action<AnimatedRepairCandidate> OnCandidateClicked;

        public void Initialize(int index, NucleotideType nucType, Vector3 worldPos)
        {
            CandidateIndex = index;
            Nucleotide = nucType;
            basePosition = worldPos;
            targetLocalPosition = worldPos;
            hoverPhase = UnityEngine.Random.Range(0f, 6.28f);

            transform.position = worldPos;
            Build3DVisuals();
        }

        private void Build3DVisuals()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Sprites/Default");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            Color nucColor = DNAHelper.GetNucleotideColor(Nucleotide);

            // 1. Dış Biyo-Kapsül Standı (Hex/Cylinder Pedestal)
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Stand";
            stand.transform.SetParent(transform, false);
            stand.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            stand.transform.localScale = new Vector3(0.95f, 0.12f, 0.95f);
            Destroy(stand.GetComponent<Collider>());
            baseMat = new Material(litShader);
            baseMat.color = new Color(0.08f, 0.12f, 0.18f);
            if (baseMat.HasProperty("_Smoothness")) baseMat.SetFloat("_Smoothness", 0.85f);
            stand.GetComponent<MeshRenderer>().material = baseMat;

            // 2. Neon Siber Halka (Taban kenar halkası)
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "NeonRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            ring.transform.localScale = new Vector3(1.05f, 0.04f, 1.05f);
            Destroy(ring.GetComponent<Collider>());
            var ringMat = new Material(unlitShader);
            ringMat.color = nucColor * 0.9f;
            ring.GetComponent<MeshRenderer>().material = ringMat;

            // 3. Kristallin Nükleotid Çekirdeği (Glowing Core)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "NucCore";
            core.transform.SetParent(transform, false);
            core.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            core.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            Destroy(core.GetComponent<Collider>());
            coreMat = new Material(litShader);
            coreMat.color = nucColor;
            if (coreMat.HasProperty("_EmissionColor"))
            {
                coreMat.EnableKeyword("_EMISSION");
                coreMat.SetColor("_EmissionColor", nucColor * 0.75f);
            }
            core.GetComponent<MeshRenderer>().material = coreMat;

            // 4. Parıltı Işığı
            GameObject lightObj = new GameObject("CoreLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.2f, -0.3f);
            glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.range = 2.4f;
            glowLight.intensity = 1.2f;
            glowLight.color = nucColor;

            // 5. Nükleotid Harfi (A, T, C, G) (Kristal Netlikte Anti-Aliased Yazı Tipi)
            GameObject symText = new GameObject("SymbolText");
            symText.transform.SetParent(transform, false);
            symText.transform.localPosition = new Vector3(0f, 0.12f, -0.45f);
            symbolTextMesh = symText.AddComponent<TextMesh>();
            symbolTextMesh.text = Nucleotide.ToString();
            symbolTextMesh.characterSize = 0.036f;
            symbolTextMesh.fontSize = 110;
            symbolTextMesh.alignment = TextAlignment.Center;
            symbolTextMesh.anchor = TextAnchor.MiddleCenter;
            symbolTextMesh.color = Color.white;
            symbolTextMesh.fontStyle = FontStyle.Bold;

            // 6. İsim Etiketi (Adenin, Timin vb.)
            GameObject lblText = new GameObject("LabelText");
            lblText.transform.SetParent(transform, false);
            lblText.transform.localPosition = new Vector3(0f, -0.48f, -0.1f);
            labelTextMesh = lblText.AddComponent<TextMesh>();
            labelTextMesh.text = GetNucFullName(Nucleotide);
            labelTextMesh.characterSize = 0.020f;
            labelTextMesh.fontSize = 80;
            labelTextMesh.alignment = TextAlignment.Center;
            labelTextMesh.anchor = TextAnchor.MiddleCenter;
            labelTextMesh.color = new Color(0.85f, 0.92f, 1f);
            labelTextMesh.fontStyle = FontStyle.Bold;

            // 7. Eşleşme İpucu (A ↔ T, C ↔ G)
            GameObject hintText = new GameObject("HintText");
            hintText.transform.SetParent(transform, false);
            hintText.transform.localPosition = new Vector3(0f, -0.66f, -0.1f);
            pairHintTextMesh = hintText.AddComponent<TextMesh>();
            pairHintTextMesh.text = $"↔ {DNAHelper.GetComplement(Nucleotide)} Eşi";
            pairHintTextMesh.characterSize = 0.016f;
            pairHintTextMesh.fontSize = 72;
            pairHintTextMesh.alignment = TextAlignment.Center;
            pairHintTextMesh.anchor = TextAnchor.MiddleCenter;
            pairHintTextMesh.color = new Color(0.6f, 0.75f, 0.9f, 0.85f);

            // 8. Tıklama Collider'ı
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.6f, 0.8f);
            col.center = new Vector3(0f, -0.1f, 0f);
        }

        private string GetNucFullName(NucleotideType nuc)
        {
            switch (nuc)
            {
                case NucleotideType.A: return "Adenin (A)";
                case NucleotideType.T: return "Timin (T)";
                case NucleotideType.C: return "Sitozin (C)";
                case NucleotideType.G: return "Guanin (G)";
                default: return nuc.ToString();
            }
        }

        public void SetBasePosition(Vector3 newPos, Vector3 newScale)
        {
            basePosition = newPos;
            baseScale = newScale;
            targetScale = isHovered ? baseScale * 1.15f : baseScale;
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
            targetScale = hovered ? baseScale * 1.15f : baseScale;
            if (glowLight != null) glowLight.intensity = hovered ? 2.2f : 1.2f;
        }

        public void TriggerClickPulse()
        {
            pulseTimer = 0.35f;
            transform.localScale = baseScale * 1.3f;
        }

        public void TriggerErrorShake()
        {
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(ErrorShakeRoutine());
        }

        private void OnDisable()
        {
            isHovered = false;
            transform.position = basePosition;
            transform.localScale = baseScale;
            if (coreMat != null) coreMat.color = DNAHelper.GetNucleotideColor(Nucleotide);
        }

        private System.Collections.IEnumerator ErrorShakeRoutine()
        {
            float elapsed = 0f;
            Vector3 origin = basePosition;
            Color origColor = coreMat != null ? coreMat.color : Color.white;

            if (coreMat != null) coreMat.color = Color.red;

            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 45f) * 0.12f;
                transform.position = origin + new Vector3(xOffset, 0f, 0f);
                yield return null;
            }

            transform.position = origin;
            if (coreMat != null) coreMat.color = origColor;
        }

        private void Update()
        {
            if (IsBeingDragged) return;

            // Hover süzülme animasyonu
            float floatY = Mathf.Sin(Time.time * 2.5f + hoverPhase) * (isHovered ? 0.08f : 0.04f);
            Vector3 hoverTarget = basePosition + new Vector3(0f, floatY, isHovered ? -0.25f : 0f);
            transform.position = Vector3.Lerp(transform.position, hoverTarget, Time.deltaTime * 8f);

            // Kristal çekirdeğin kendi etrafında yavaşça dönmesi
            Transform coreTrans = transform.Find("NucCore");
            if (coreTrans != null)
            {
                coreTrans.Rotate(Vector3.up, 35f * Time.deltaTime, Space.Self);
                coreTrans.Rotate(Vector3.right, 18f * Time.deltaTime, Space.Self);
            }

            // Ölçek interpolasyonu
            if (pulseTimer > 0f)
            {
                pulseTimer -= Time.deltaTime;
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 6f);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
            }
        }

        public void OnClick()
        {
            TriggerClickPulse();
            OnCandidateClicked?.Invoke(this);
        }
    }
}
