using System;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Gerçek bir Genom Dizilim Yuvası (DNA Helix Docking Cradle).
    /// Dönen holografik DNA çift sarmalı, manyetik kenetleme halkaları ve
    /// neon kılavuz braketleriyle modellenmiştir.
    /// </summary>
    public class AnimatedSlot : MonoBehaviour
    {
        public int SlotIndex { get; private set; }
        public AnimatedGenePiece CurrentPiece { get; private set; }
        public bool IsOccupied => CurrentPiece != null;

        private Transform holoHelixCore;
        private Material holoHelixMaterial;
        private Light slotLight;
        private TextMesh indexTextMesh;
        private float pulseTimer;
        private float dockPulse = 0f;
        private Vector3 baseScale = Vector3.one * 0.95f;

        public event Action<int> OnSlotClicked;

        public void Initialize(int index)
        {
            SlotIndex = index;
            pulseTimer = UnityEngine.Random.Range(0f, 5f);
            BuildGenomeDockVisuals();
        }

        private void BuildGenomeDockVisuals()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Sprites/Default");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            // 1. Sol ve Sağ Manyetik Ray Direkleri (Docking Pylons - Genişletilmiş Gövdeye Uygun)
            CreateDockPylon(new Vector3(-1.34f, 0f, 0.1f), litShader);
            CreateDockPylon(new Vector3(1.34f, 0f, 0.1f), litShader);

            // 2. Üst ve Alt Manyetik Kılavuz Braketleri (Horizontal Guides)
            CreateGuideBracket(new Vector3(0f, 0.82f, 0.1f), litShader);
            CreateGuideBracket(new Vector3(0f, -0.82f, 0.1f), litShader);

            // 3. Dönen Holografik DNA Çift Sarmalı (Boşken Dönen Heliks)
            GameObject holoContainer = new GameObject("HoloDNASequence");
            holoContainer.transform.SetParent(transform, false);
            holoContainer.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            holoHelixCore = holoContainer.transform;

            holoHelixMaterial = new Material(unlitShader);
            holoHelixMaterial.color = new Color(0.15f, 0.75f, 1f, 0.45f);

            // Heliks Basamakları (Küçük Çapraz Çubuklar)
            for (int i = 0; i < 4; i++)
            {
                float y = -0.45f + (i * 0.3f);
                float rot = i * 45f;

                GameObject rung = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rung.transform.SetParent(holoHelixCore, false);
                rung.transform.localPosition = new Vector3(0f, y, 0f);
                rung.transform.localRotation = Quaternion.Euler(0f, rot, 90f);
                rung.transform.localScale = new Vector3(0.06f, 0.7f, 0.06f);
                Destroy(rung.GetComponent<Collider>());
                rung.GetComponent<MeshRenderer>().material = holoHelixMaterial;
            }

            // 4. Yuva Numarası (Yüksek Çözünürlüklü Vektörel Netlik, yuvaya yakın)
            GameObject textObj = new GameObject("SlotLabel");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            indexTextMesh = textObj.AddComponent<TextMesh>();
            indexTextMesh.text = $"⟵ GEN #{SlotIndex + 1} ⟶";
            indexTextMesh.characterSize = 0.022f;
            indexTextMesh.fontSize = 96;
            indexTextMesh.alignment = TextAlignment.Center;
            indexTextMesh.anchor = TextAnchor.MiddleCenter;
            indexTextMesh.color = new Color(0.35f, 0.9f, 1f);
            indexTextMesh.fontStyle = FontStyle.Bold;

            // 5. Tıklama Çerçevesi (Collider - Sadece yuva içi)
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(2.8f, 2.2f, 0.5f);
            col.center = new Vector3(0f, 0f, 0f);

            // 6. Neon Noktasal Işık
            GameObject lightObj = new GameObject("DockLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            slotLight = lightObj.AddComponent<Light>();
            slotLight.type = LightType.Point;
            slotLight.range = 2.8f;
            slotLight.intensity = 1.1f;
            slotLight.color = new Color(0.2f, 0.8f, 1f);
        }

        private void CreateDockPylon(Vector3 localPos, Shader shader)
        {
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pylon.transform.SetParent(transform, false);
            pylon.transform.localPosition = localPos;
            pylon.transform.localScale = new Vector3(0.18f, 0.75f, 0.18f);
            Destroy(pylon.GetComponent<Collider>());

            var mat = new Material(shader);
            mat.color = new Color(0.1f, 0.18f, 0.32f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.9f);
            pylon.GetComponent<MeshRenderer>().material = mat;

            // Uç Kılavuz Halkası
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.transform.SetParent(pylon.transform, false);
            ring.transform.localPosition = new Vector3(0f, 1f, 0f);
            ring.transform.localScale = new Vector3(1.4f, 0.3f, 1.4f);
            Destroy(ring.GetComponent<Collider>());
            var ringMat = new Material(shader);
            ringMat.color = new Color(0.2f, 0.85f, 1f);
            ring.GetComponent<MeshRenderer>().material = ringMat;
        }

        private void CreateGuideBracket(Vector3 localPos, Shader shader)
        {
            GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.transform.SetParent(transform, false);
            bracket.transform.localPosition = localPos;
            bracket.transform.localScale = new Vector3(2.7f, 0.08f, 0.2f);
            Destroy(bracket.GetComponent<Collider>());

            var mat = new Material(shader);
            mat.color = new Color(0.15f, 0.25f, 0.45f);
            bracket.GetComponent<MeshRenderer>().material = mat;
        }

        private void Update()
        {
            pulseTimer += Time.deltaTime * 3f;
            if (dockPulse > 0f)
            {
                dockPulse = Mathf.Max(0f, dockPulse - Time.deltaTime * 4f);
                float bounce = 1f + Mathf.Sin(dockPulse * Mathf.PI) * 0.14f;
                transform.localScale = baseScale * bounce;
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 10f);
            }

            if (!IsOccupied)
            {
                // Boşken holografik DNA çift sarmalı döner (Spinning Helix)
                if (holoHelixCore != null)
                {
                    holoHelixCore.gameObject.SetActive(true);
                    holoHelixCore.Rotate(Vector3.up * 55f * Time.deltaTime, Space.Self);
                }

                float pulse = 0.6f + Mathf.Sin(pulseTimer) * 0.3f;
                if (slotLight != null) slotLight.intensity = 0.8f + pulse * 0.6f;
                if (holoHelixMaterial != null)
                {
                    holoHelixMaterial.color = new Color(0.15f, 0.75f, 1f, 0.3f + pulse * 0.35f);
                }
            }
            else
            {
                // Doluyken holografik önizleme gizlenir, gerçek DNA parçası kilitlenir
                if (holoHelixCore != null)
                {
                    holoHelixCore.gameObject.SetActive(false);
                }
                if (slotLight != null) slotLight.intensity = 0.5f + dockPulse * 1.5f;
            }
        }

        public void TriggerDockPulse()
        {
            dockPulse = 1.0f;
        }

        public void NotifyClicked()
        {
            TriggerDockPulse();
            OnSlotClicked?.Invoke(SlotIndex);
        }

        public void PlacePiece(AnimatedGenePiece piece)
        {
            CurrentPiece = piece;
            dockPulse = 1.0f;
            if (piece != null)
            {
                piece.CurrentSlotIndex = SlotIndex;
                piece.SetTargetScale(Vector3.one * 0.95f);
                piece.MoveTo(transform.position + new Vector3(0f, 0f, -0.12f), transform.rotation, 0.32f);
            }
        }

        public AnimatedGenePiece RemovePiece()
        {
            var piece = CurrentPiece;
            dockPulse = 0.8f;
            if (piece != null)
            {
                piece.CurrentSlotIndex = -1;
            }
            CurrentPiece = null;
            return piece;
        }
    }
}
