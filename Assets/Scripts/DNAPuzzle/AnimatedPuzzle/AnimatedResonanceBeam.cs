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
        private int segments = 10;

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
                lineRenderer.material.color = new Color(0.1f, 1f, 0.45f);
                lineRenderer.startWidth = 0.16f;
                lineRenderer.endWidth = 0.16f;
            }
            else if (isMismatch)
            {
                lineRenderer.material.color = new Color(1f, 0.25f, 0.25f);
                lineRenderer.startWidth = 0.08f;
                lineRenderer.endWidth = 0.08f;
            }
        }

        private void Update()
        {
            if (!lineRenderer.enabled || startTarget == null || endTarget == null)
                return;

            zapTimer += Time.deltaTime * 25f;

            Vector3 startPos = startTarget.position + new Vector3(1.46f, 0f, 0f);
            Vector3 endPos = endTarget.position + new Vector3(-1.46f, 0f, 0f);

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 p = Vector3.Lerp(startPos, endPos, t);

                if (i > 0 && i < segments - 1)
                {
                    // Elektrik kıvılcımı dalgalanması (Jitter)
                    float jitterAmount = isResonant ? 0.06f : 0.14f;
                    float offset = Mathf.Sin(zapTimer + i * 2f) * jitterAmount;
                    p.y += offset;
                    p.z += Mathf.Cos(zapTimer + i * 1.5f) * jitterAmount;
                }

                lineRenderer.SetPosition(i, p);
            }
        }
    }
}
