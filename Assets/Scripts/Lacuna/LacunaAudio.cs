using System.Collections;
using UnityEngine;

namespace Lacuna
{
    /// <summary>
    /// All the game's audio in one place: background music that fades out and in when
    /// you fail, each creature's own sounds at random intervals, and the Landlord's
    /// voices (speaking, angry "hurry up", and shouting when you fail).
    /// GameManager calls the public methods at the right moments. It creates its own
    /// AudioSources, so you only assign clips here.
    /// </summary>
    public class LacunaAudio : MonoBehaviour
    {
        [Header("Music (loops)")]
        [Tooltip("Element 0 plays at the start, 1 after the first fail, 2 after the second fail, and so on. If there are more fails than clips, the last clip stays.")]
        [SerializeField] private AudioClip[] musicByFails = new AudioClip[3];
        [Tooltip("Optional: plays after the last creature. Empty = the current music keeps playing.")]
        [SerializeField] private AudioClip endingMusic;
        [Tooltip("Optional: plays on Game Over. Empty = the music fades to silence.")]
        [SerializeField] private AudioClip gameOverMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
        [Tooltip("Seconds the old theme takes to fade out.")]
        [SerializeField, Min(0f)] private float musicFadeOutTime = 1.5f;
        [Tooltip("Seconds the new theme takes to fade in.")]
        [SerializeField, Min(0f)] private float musicFadeInTime = 2f;

        [Header("Creature sounds (the clips live on each creature in GameManager)")]
        [Tooltip("Min and max seconds between two sounds, counted from the moment the timer starts.")]
        [SerializeField] private Vector2 creatureSoundInterval = new Vector2(10f, 15f);
        [SerializeField, Range(0f, 1f)] private float creatureVolume = 0.8f;
        [Tooltip("0 = plain stereo, 1 = fully 3D (louder when the camera is near the creature).")]
        [SerializeField, Range(0f, 1f)] private float creatureSpatialBlend = 0.6f;

        [Header("The Landlord's voice")]
        [Tooltip("Played with his messages: 'I'm sending you WIBBLE.' and the ending lines.")]
        [SerializeField] private AudioClip[] speakingVoices;
        [Tooltip("Angry, played now and then while the timer runs, because he wants you to hurry.")]
        [SerializeField] private AudioClip[] hurryVoices;
        [Tooltip("Shouting, played the moment you fail.")]
        [SerializeField] private AudioClip[] failVoices;
        [Tooltip("Min and max seconds between two angry voices, counted from the moment the timer starts.")]
        [SerializeField] private Vector2 hurryInterval = new Vector2(15f, 20f);
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;

        [Header("'Hurry!' text (shown when an angry voice plays)")]
        [Tooltip("Empty = the dialogue UI in the scene is found automatically.")]
        [SerializeField] private DialogueUI dialogue;
        [Tooltip("Shown in the message box together with each angry voice. It stays for 'Quick Hold Time' on the DialogueUI.")]
        [SerializeField] private string hurryMessage = "Hurry!";

        private AudioSource musicSource;
        private AudioSource creatureSource;
        private AudioSource voiceSource;

        private AudioClip currentMusic;
        private Coroutine musicRoutine;
        private Coroutine creatureRoutine;
        private Coroutine hurryRoutine;
        private AudioClip lastSpeaking;
        private AudioClip lastFail;

        private void Awake()
        {
            if (dialogue == null) dialogue = FindFirstObjectByType<DialogueUI>();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;

            // The creature source is a child, so it can be moved to wherever the creature is.
            GameObject child = new GameObject("CreatureSoundSource");
            child.transform.SetParent(transform, false);
            creatureSource = child.AddComponent<AudioSource>();
            creatureSource.playOnAwake = false;
            creatureSource.loop = false;
            creatureSource.spatialBlend = creatureSpatialBlend;
        }

        // ------------------------------------------------------------ music

        /// <summary>0 = start of the game, 1 = after the first fail, 2 = after the second, ...</summary>
        public void PlayMusicForFails(int fails)
        {
            if (musicByFails == null || musicByFails.Length == 0) return;
            int index = Mathf.Clamp(fails, 0, musicByFails.Length - 1);
            SwitchMusic(musicByFails[index]);
        }

        public void PlayEndingMusic()
        {
            if (endingMusic != null) SwitchMusic(endingMusic);
        }

        /// <summary>Plays the Game Over theme, or fades to silence if there is none.</summary>
        public void PlayGameOverMusic()
        {
            SwitchMusic(gameOverMusic);
        }

        private void SwitchMusic(AudioClip clip)
        {
            if (clip == currentMusic) return;
            currentMusic = clip;

            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(FadeToMusic(clip));
        }

        private IEnumerator FadeToMusic(AudioClip clip)
        {
            // Old theme fades out first, then the new one fades in.
            if (musicSource.isPlaying)
                yield return FadeMusicVolume(0f, musicFadeOutTime);

            musicSource.Stop();
            if (clip == null) yield break;

            musicSource.clip = clip;
            musicSource.volume = 0f;
            musicSource.Play();
            yield return FadeMusicVolume(musicVolume, musicFadeInTime);
        }

        private IEnumerator FadeMusicVolume(float target, float duration)
        {
            float start = musicSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            musicSource.volume = target;
        }

        // ------------------------------------------------------------ one attempt (timer running)

        /// <summary>The timer just started: creature sounds and the Landlord's hurry-up voices begin.</summary>
        public void BeginAttempt(CreatureEntry entry, GameObject creature)
        {
            EndAttempt();
            creatureRoutine = StartCoroutine(CreatureSoundLoop(entry, creature));
            hurryRoutine = StartCoroutine(HurryLoop());
        }

        /// <summary>The attempt is over (success, fail or timeout). Sounds that are already playing finish.</summary>
        public void EndAttempt()
        {
            if (creatureRoutine != null) StopCoroutine(creatureRoutine);
            if (hurryRoutine != null) StopCoroutine(hurryRoutine);
            creatureRoutine = null;
            hurryRoutine = null;
        }

        private IEnumerator CreatureSoundLoop(CreatureEntry entry, GameObject creature)
        {
            if (entry == null || entry.sounds == null || entry.sounds.Length == 0) yield break;

            AudioClip last = null;
            while (true)
            {
                yield return new WaitForSeconds(RandomInterval(creatureSoundInterval));

                AudioClip clip = PickDifferent(entry.sounds, last);
                if (clip == null) continue;
                last = clip;

                if (creature != null) creatureSource.transform.position = creature.transform.position;
                creatureSource.spatialBlend = creatureSpatialBlend;
                creatureSource.PlayOneShot(clip, creatureVolume);
            }
        }

        private IEnumerator HurryLoop()
        {
            if (hurryVoices == null || hurryVoices.Length == 0) yield break;

            AudioClip last = null;
            while (true)
            {
                yield return new WaitForSeconds(RandomInterval(hurryInterval));

                // Never talk over himself.
                while (voiceSource.isPlaying) yield return null;

                AudioClip clip = PickDifferent(hurryVoices, last);
                if (clip == null) continue;
                last = clip;
                PlayVoice(clip);

                if (dialogue != null && !string.IsNullOrEmpty(hurryMessage))
                    dialogue.ShowQuick(hurryMessage);
            }
        }

        // ------------------------------------------------------------ voices

        /// <summary>His normal speaking voice, for the messages on screen.</summary>
        public void PlaySpeakingVoice()
        {
            AudioClip clip = PickDifferent(speakingVoices, lastSpeaking);
            if (clip == null) return;
            lastSpeaking = clip;
            PlayVoice(clip);
        }

        /// <summary>The Landlord shouts. It cuts off whatever he was saying.</summary>
        public void PlayFailShout()
        {
            AudioClip clip = PickDifferent(failVoices, lastFail);
            if (clip == null) return;
            lastFail = clip;
            PlayVoice(clip);
        }

        private void PlayVoice(AudioClip clip)
        {
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.volume = voiceVolume;
            voiceSource.Play();
        }

        // ------------------------------------------------------------ helpers

        private static float RandomInterval(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return Mathf.Max(0.5f, Random.Range(min, max));
        }

        /// <summary>A random non-empty clip, different from the previous one when possible.</summary>
        private static AudioClip PickDifferent(AudioClip[] clips, AudioClip last)
        {
            if (clips == null || clips.Length == 0) return null;

            AudioClip pick = null;
            for (int i = 0; i < 8; i++)
            {
                pick = clips[Random.Range(0, clips.Length)];
                if (pick != null && pick != last) return pick;
            }

            // Only one usable clip (or bad luck): take the first non-empty one.
            foreach (AudioClip clip in clips)
                if (clip != null) return clip;
            return null;
        }
    }
}