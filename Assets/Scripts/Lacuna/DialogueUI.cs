using System.Collections;
using TMPro;
using UnityEngine;

namespace Lacuna
{
    /// <summary>
    /// The mysterious guy's messages: a panel that fades in, types the text out,
    /// holds for a moment, fades away and then CLEARS both texts, so nothing like "???"
    /// stays on screen. Press Space to skip the typing. Works on any canvas, so it can
    /// later move onto a world-space canvas on the long monitor.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private string speakerName = "???";
        [SerializeField] private float charsPerSecond = 35f;
        [Tooltip("Seconds a finished message stays on screen before it disappears.")]
        [SerializeField] private float holdTime = 2f;
        [SerializeField] private float fadeTime = 0.25f;
        [Tooltip("Seconds a short message (like 'Hurry!') stays on screen.")]
        [SerializeField] private float quickHoldTime = 2f;

        private bool showing;
        private Coroutine quickRoutine;

        private void Awake()
        {
            if (panel != null) panel.alpha = 0f;
            ClearTexts();
        }

        public IEnumerator Show(string text)
        {
            if (string.IsNullOrEmpty(text) || bodyText == null) yield break;

            // A short message in progress (like 'Hurry!') makes room for this one.
            StopQuick();
            showing = true;

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
            ClearTexts();
            showing = false;
        }

        public IEnumerator ShowSequence(params string[] lines)
        {
            foreach (string line in lines)
                yield return Show(line);
        }

        /// <summary>
        /// A short message that appears at once (no typing), stays for "Quick Hold Time"
        /// seconds and then clears. Ignored while a normal message is on screen.
        /// </summary>
        public void ShowQuick(string text)
        {
            if (showing || string.IsNullOrEmpty(text) || bodyText == null) return;

            StopQuick();
            quickRoutine = StartCoroutine(QuickRoutine(text));
        }

        private IEnumerator QuickRoutine(string text)
        {
            if (speakerText != null) speakerText.text = speakerName;
            bodyText.text = text;
            bodyText.maxVisibleCharacters = text.Length;
            yield return Fade(1f);

            yield return new WaitForSeconds(quickHoldTime);

            yield return Fade(0f);
            ClearTexts();
            quickRoutine = null;
        }

        private void StopQuick()
        {
            if (quickRoutine == null) return;
            StopCoroutine(quickRoutine);
            quickRoutine = null;
        }

        private void ClearTexts()
        {
            if (speakerText != null) speakerText.text = "";
            if (bodyText != null) bodyText.text = "";
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