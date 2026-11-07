using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Hiçbir harici ses dosyasına (.wav, .mp3) ihtiyaç duymadan,
    /// tamamen matematiksel olarak prosedürel Sci-Fi sesleri (fırlatma, atmosfer kükremesi,
    /// rezonans, kalkan, iniş, patlama) üreten hafif ve bağımsız ses motoru.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class DNAPuzzleAudio : MonoBehaviour
    {
        public static DNAPuzzleAudio Instance { get; private set; }

        private AudioSource audioSource;

        private AudioClip clipSnap;
        private AudioClip clipResonance;
        private AudioClip clipMismatch;
        private AudioClip clipVictory;
        private AudioClip clipPhaseStep;
        private AudioClip clipLaunch;
        private AudioClip clipAtmosphericRoar;
        private AudioClip clipShieldDeflection;
        private AudioClip clipExplosion;
        private AudioClip clipTouchdown;

        private float lastSnapTime = -10f;
        private float lastResonanceTime = -10f;
        private float lastMismatchTime = -10f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                audioSource.volume = 0.85f;
            }

            if (clipSnap == null)
            {
                GenerateAudioClips();
            }
        }

        private void GenerateAudioClips()
        {
            clipSnap = CreateToneClip("Snap", 580f, 0.09f, Waveform.Sine, 0.35f);
            clipResonance = CreateHarmonicChordClip("Resonance", new float[] { 440f, 659.25f, 880f }, 0.30f, 0.40f);
            clipMismatch = CreateToneClip("Mismatch", 165f, 0.18f, Waveform.Triangle, 0.40f);
            clipVictory = CreateMajorChordClip("Victory", 0.58f, 0.48f);
            clipPhaseStep = CreateToneClip("Step", 520f, 0.12f, Waveform.Sine, 0.35f);
            clipLaunch = CreateSweepClip("Launch", 180f, 680f, 0.60f, 0.50f);
            clipAtmosphericRoar = CreateNoiseClip("AtmoRoar", 0.80f, 0.50f);
            clipShieldDeflection = CreateHarmonicChordClip("Shield", new float[] { 587.33f, 880f, 1174.66f }, 0.24f, 0.35f);
            clipExplosion = CreateNoiseClip("Explosion", 0.70f, 0.55f);
            clipTouchdown = CreateToneClip("Touchdown", 120f, 0.36f, Waveform.Triangle, 0.45f);
        }

        public void PlaySnap()
        {
            EnsureInitialized();
            if (Time.unscaledTime - lastSnapTime < 0.04f) return;
            lastSnapTime = Time.unscaledTime;
            if (clipSnap != null && audioSource != null)
                audioSource.PlayOneShot(clipSnap, 0.55f);
        }

        public void PlayResonance()
        {
            EnsureInitialized();
            if (Time.unscaledTime - lastResonanceTime < 0.08f) return;
            lastResonanceTime = Time.unscaledTime;
            if (clipResonance != null && audioSource != null)
                audioSource.PlayOneShot(clipResonance, 0.65f);
        }

        public void PlayMismatch()
        {
            EnsureInitialized();
            if (Time.unscaledTime - lastMismatchTime < 0.08f) return;
            lastMismatchTime = Time.unscaledTime;
            if (clipMismatch != null && audioSource != null)
                audioSource.PlayOneShot(clipMismatch, 0.60f);
        }

        public void PlayBurnout()
        {
            EnsureInitialized();
            if (audioSource != null)
            {
                if (clipExplosion != null) audioSource.PlayOneShot(clipExplosion, 0.75f);
                if (clipMismatch != null) audioSource.PlayOneShot(clipMismatch, 0.85f);
            }
        }

        public void PlayVictory()
        {
            EnsureInitialized();
            if (clipVictory != null && audioSource != null)
                audioSource.PlayOneShot(clipVictory, 0.75f);
        }

        public void PlayPhaseStep()
        {
            EnsureInitialized();
            if (clipPhaseStep != null && audioSource != null)
                audioSource.PlayOneShot(clipPhaseStep, 0.50f);
        }

        public void PlayLaunch()
        {
            EnsureInitialized();
            if (clipLaunch != null && audioSource != null)
                audioSource.PlayOneShot(clipLaunch, 0.65f);
        }

        public void PlayAtmosphericRoar()
        {
            EnsureInitialized();
            if (clipAtmosphericRoar != null && audioSource != null)
                audioSource.PlayOneShot(clipAtmosphericRoar, 0.55f);
        }

        public void PlayShieldDeflection()
        {
            EnsureInitialized();
            if (clipShieldDeflection != null && audioSource != null)
                audioSource.PlayOneShot(clipShieldDeflection, 0.60f);
        }

        public void PlayExplosion()
        {
            EnsureInitialized();
            if (clipExplosion != null && audioSource != null)
                audioSource.PlayOneShot(clipExplosion, 0.70f);
        }

        public void PlayTouchdown()
        {
            EnsureInitialized();
            if (clipTouchdown != null && audioSource != null)
                audioSource.PlayOneShot(clipTouchdown, 0.70f);
        }

        #region Prosedürel Dalga Üretimi
        private enum Waveform { Sine, Triangle, Sawtooth }

        private static float ApplySmoothWindow(int i, int totalSamples, int sampleRate, float decay)
        {
            float progress = (float)i / totalSamples;
            float attack = Mathf.Clamp01(i / (sampleRate * 0.008f));
            float release = Mathf.Clamp01((totalSamples - i) / (sampleRate * 0.010f));
            float envelope = Mathf.Exp(-progress * (1f / Mathf.Max(0.05f, decay)));
            return envelope * attack * release;
        }

        private AudioClip CreateToneClip(string name, float freq, float duration, Waveform wave, float decay)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = ApplySmoothWindow(i, totalSamples, sampleRate, decay);

                float val = 0f;
                float phase = (t * freq) % 1f;

                switch (wave)
                {
                    case Waveform.Sine:
                        val = Mathf.Sin(t * freq * 2f * Mathf.PI);
                        break;
                    case Waveform.Triangle:
                        val = (Mathf.PingPong(phase * 4f, 2f) - 1f);
                        break;
                    case Waveform.Sawtooth:
                        val = (phase * 2f - 1f) * 0.6f;
                        break;
                }

                samples[i] = val * env;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSweepClip(string name, float startFreq, float endFreq, float duration, float decay)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];
            float currentPhase = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float progress = (float)i / totalSamples;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                currentPhase += (freq / sampleRate);
                float env = ApplySmoothWindow(i, totalSamples, sampleRate, decay);
                samples[i] = Mathf.Sin(currentPhase * 2f * Mathf.PI) * env;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration, float decay)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];
            float filtered = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float env = ApplySmoothWindow(i, totalSamples, sampleRate, decay);
                float rawNoise = (UnityEngine.Random.value * 2f - 1f);
                // Yumuşak alçak geçiren filtre (Low-pass) ile cızırtısız sinematik bas gürlemesi
                filtered = Mathf.Lerp(filtered, rawNoise, 0.15f);
                samples[i] = filtered * env * 0.8f;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateHarmonicChordClip(string name, float[] freqs, float duration, float decay)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = ApplySmoothWindow(i, totalSamples, sampleRate, decay);

                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float subT = t + (f * 0.02f);
                    sum += Mathf.Sin(subT * freqs[f] * 2f * Mathf.PI);
                }

                samples[i] = (sum / freqs.Length) * env;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateMajorChordClip(string name, float duration, float decay)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6 Arpej

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / totalSamples;
                float env = ApplySmoothWindow(i, totalSamples, sampleRate, decay);

                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(progress * 4f), 0, 3);
                float noteLocal = (progress * 4f) - noteIndex;
                float noteFade = Mathf.Clamp01(noteLocal * 12f) * Mathf.Clamp01((1f - noteLocal) * 12f);
                float val = Mathf.Sin(t * notes[noteIndex] * 2f * Mathf.PI) * noteFade;

                samples[i] = val * env;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }
}
