using System.Collections;
using TMPro;
using UnityEngine;

namespace Lacuna
{
    /// <summary>
    /// The mysterious guy's messages: a panel that fades in, types the text out,
    /// holds for a moment and fades away. Press Space to skip. Works on any canvas,
    /// so it can later move onto a world-space canvas on the long monitor.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private string speakerName = "???";
        [SerializeField] private float charsPerSecond = 35f;
        [Tooltip("Seconds the finished message stays on screen.")]
        [SerializeField] private float holdTime = 1.6f;
        [SerializeField] private float fadeTime = 0.25f;

        private void Awake()
        {
            if (panel != null) panel.alpha = 0f;
        }

        public IEnumerator Show(string text)
        {
            if (string.IsNullOrEmpty(text) || bodyText == null) yield break;

            if (speakerText != null) speakerText.text = speakerName;
            bodyText.text = text;
            bodyText.maxVisibleCharacters = 0;
            yield return Fade(1f);

            // Typewriter. maxVisibleCharacters keeps rich-text tags intact.
            int total = text.Length;
            float typed = 0f;
            while (typed < total)
            {
                typed += Time.deltaTime * charsPerSecond;
                bodyText.maxVisibleCharacters = Mathf.Min(total, (int)typed);
                if (GameInput.SkipPressed()) break;
                yield return null;
            }
            bodyText.maxVisibleCharacters = total;

            // Hold, unless skipped.
            float held = 0f;
            while (held < holdTime)
            {
                held += Time.deltaTime;
                if (GameInput.SkipPressed()) break;
                yield return null;
            }

            yield return Fade(0f);
        }

        public IEnumerator ShowSequence(params string[] lines)
        {
            foreach (string line in lines)
                yield return Show(line);
        }

        private IEnumerator Fade(float target)
        {
            if (panel == null) yield break;

            float start = panel.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, fadeTime);
                panel.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t));
                yield return null;
            }
            panel.alpha = target;
        }
    }
}
