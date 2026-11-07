using UnityEngine;

namespace DNAPuzzle
{
    public class AnimatedResonanceBeam : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private Transform startTarget;
        private Transform endTarget;
        private bool isResonant = false;
        private bool isMismatch = false;

        private float zapTimer = 0f;
        private int segments = 24;

        private void Awake()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = segments;
            lineRenderer.startWidth = 0.12f;
            lineRenderer.endWidth = 0.12f;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            lineRenderer.material = new Material(shader);
            lineRenderer.enabled = false;
        }

        public void SetTargets(Transform start, Transform end)
        {
            startTarget = start;
            endTarget = end;
        }

        public void SetState(bool resonant, bool mismatch)
        {
            isResonant = resonant;
            isMismatch = mismatch;

            lineRenderer.enabled = (isResonant || isMismatch);

            if (isResonant)
            {
                lineRenderer.material.color = new Color(0.15f, 1f, 0.45f);
                lineRenderer.startWidth = 0.10f;
                lineRenderer.endWidth = 0.10f;
                lineRenderer.loop = true;
            }
            else if (isMismatch)
            {
                lineRenderer.material.color = new Color(1f, 0.25f, 0.25f);
                lineRenderer.startWidth = 0.08f;
                lineRenderer.endWidth = 0.08f;
                lineRenderer.loop = false;
            }
        }

        private void Update()
        {
            if (!lineRenderer.enabled || startTarget == null || endTarget == null)
                return;

            zapTimer += Time.deltaTime * 20f;

            Vector3 seamCenter = (startTarget.position + endTarget.position) * 0.5f + new Vector3(0f, 0f, -0.10f);

            if (isResonant)
            {
                // Geçmeli yapboz ucu kilit halkası (Interlocking Puzzle Ring Glow)
                float pulseRadius = 0.32f + Mathf.Sin(zapTimer * 0.4f) * 0.03f;
                for (int i = 0; i < segments; i++)
                {
                    float angle = ((float)i / segments) * Mathf.PI * 2f + (zapTimer * 0.15f);
                    Vector3 p = seamCenter + new Vector3(Mathf.Cos(angle) * pulseRadius, Mathf.Sin(angle) * pulseRadius, 0f);
                    lineRenderer.SetPosition(i, p);
                }
            }
            else if (isMismatch)
            {
                // Uyumsuzluk kıvılcımı
                Vector3 p1 = seamCenter + new Vector3(-0.35f, 0f, 0f);
                Vector3 p2 = seamCenter + new Vector3(0.35f, 0f, 0f);
                for (int i = 0; i < segments; i++)
                {
                    float t = (float)i / (segments - 1);
                    Vector3 p = Vector3.Lerp(p1, p2, t);
                    if (i > 0 && i < segments - 1)
                    {
                        p.y += Mathf.Sin(zapTimer + i * 2.5f) * 0.15f;
                    }
                    lineRenderer.SetPosition(i, p);
                }
            }
        }
    }
}
