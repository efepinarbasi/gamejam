using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lacuna
{
    public enum GameState
    {
        Idle,
        Intro,
        Messaging,        // the Landlord is typing
        CreatureEntering, // door opens, rail brings the creature in; screens locked
        Sequencer,        // left screen
        GeneEditor,       // center screen
        Capsule,          // right screen (QTE)
        CreatureLeaving,  // capsule + creature sent away
        CreatureDying,    // fail: timeout or QTE
        Ending,
        GameOver
    }

    /// <summary>One creature of the game. Add one entry per creature, in order.</summary>
    [System.Serializable]
    public class CreatureEntry
    {
        public string creatureName = "CREATURE";
        [TextArea(1, 3)] public string introMessage = "I'm sending you {name}.";
        [Tooltip("Seconds to finish all three screens.")]
        public float timeLimit = 120f;
        [Tooltip("Leave empty to use a placeholder capsule shape.")]
        public GameObject modelPrefab;
    }

    /// <summary>
    /// Runs the whole loop: message, creature enters, three screens against one timer,
    /// capsule, creature leaves, next creature. Gene mechanics are NOT in here: each
    /// screen just reports Completed or Failed.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Creatures (one entry per creature, in order)")]
        [SerializeField] private List<CreatureEntry> creatures = new List<CreatureEntry>();

        [Header("Rules")]
        [Tooltip("Total fails (timeout or failed QTE) before the Landlord kills you.")]
        [SerializeField] private int maxFails = 2;
        [Tooltip("After a fail, the same creature is sent again. Off = move on to the next one.")]
        [SerializeField] private bool retrySameCreatureAfterFail = true;
        [SerializeField] private float startDelay = 1.5f;
        [Tooltip("Pause after the creature stops, before the timer starts.")]
        [SerializeField] private float settleTime = 1f;
        [SerializeField] private string menuSceneName = "MainMenu";

        [Header("Screens (left, center, right)")]
        [SerializeField] private StationScreen sequencerScreen;
        [SerializeField] private StationScreen geneEditorScreen;
        [SerializeField] private StationScreen capsuleScreen;

        [Header("Systems")]
        [SerializeField] private CreatureTransport transport;
        [SerializeField] private DialogueUI dialogue;

        [Header("HUD (all optional)")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text creatureNameText;
        [SerializeField] private TMP_Text failsText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject endingPanel;

        [Header("Landlord lines ({name} becomes the creature's name)")]
        [SerializeField, TextArea(1, 3)] private string retryLine = "{name} again. Do not disappoint me twice.";
        [SerializeField, TextArea(1, 3)] private string failLine = "{name} is gone. Do not let it happen again.";
        [SerializeField, TextArea(1, 3)] private string gameOverLine = "Twice, now. Your family will be so disappointed in you.";
        [SerializeField, TextArea(1, 3)]
        private string[] endingLines =
        {
            "Every creature, mended. I did not think it possible.",
            "They are yours now. All of them.",
            "Stay a while. Perhaps this is what was missing."
        };

        public GameState State { get; private set; } = GameState.Idle;
        public float TimeRemaining { get; private set; }
        public int Fails { get; private set; }
        public int CreatureIndex { get; private set; }
        public CreatureEntry CurrentCreature =>
            CreatureIndex >= 0 && CreatureIndex < creatures.Count ? creatures[CreatureIndex] : null;

        public event System.Action<GameState> StateChanged;

        private bool timerRunning;
        private bool timedOut;
        private bool attemptSucceeded;

        // ------------------------------------------------------------ setup

        private void Reset()
        {
            // Called when the component is first added: pre-fill the 10 creatures,
            // with time limits from 120 s (WIBBLE) down to 60 s (GROMBLE).
            string[] names =
            {
                "WIBBLE", "SNORFLE", "BAABLEBO", "GLUMP", "TWIZZIT",
                "MOOPSY", "KRAKKO", "ZEEBO-ZEEBO", "PHLORB", "GROMBLE"
            };

            creatures = new List<CreatureEntry>();
            for (int i = 0; i < names.Length; i++)
            {
                creatures.Add(new CreatureEntry
                {
                    creatureName = names[i],
                    introMessage = "I'm sending you {name}.",
                    timeLimit = Mathf.Lerp(120f, 60f, i / (names.Length - 1f))
                });
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (!Validate()) return;
            StartCoroutine(GameLoop());
        }

        private bool Validate()
        {
            bool ok = true;
            if (creatures.Count == 0) { Debug.LogError("GameManager: the creature list is empty.", this); ok = false; }
            if (sequencerScreen == null) { Debug.LogError("GameManager: Sequencer Screen is not assigned.", this); ok = false; }
            if (geneEditorScreen == null) { Debug.LogError("GameManager: Gene Editor Screen is not assigned.", this); ok = false; }
            if (capsuleScreen == null) { Debug.LogError("GameManager: Capsule Screen is not assigned.", this); ok = false; }
            if (transport == null) { Debug.LogError("GameManager: Creature Transport is not assigned.", this); ok = false; }
            if (dialogue == null) { Debug.LogError("GameManager: Dialogue UI is not assigned.", this); ok = false; }
            return ok;
        }

        // ------------------------------------------------------------ main loop

        private IEnumerator GameLoop()
        {
            SetState(GameState.Intro);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
            ResetAllStations();
            UpdateHud();
            UpdateTimerText();

            yield return new WaitForSeconds(startDelay);

            int index = 0;
            bool isRetry = false;

            while (index < creatures.Count)
            {
                CreatureIndex = index;
                CreatureEntry entry = creatures[index];

                yield return RunAttempt(entry, isRetry);

                if (attemptSucceeded)
                {
                    index++;
                    isRetry = false;
                    continue;
                }

                Fails++;
                UpdateHud();

                if (Fails >= maxFails)
                {
                    yield return GameOverRoutine(entry);
                    yield break;
                }

                yield return dialogue.Show(Format(failLine, entry));

                if (retrySameCreatureAfterFail)
                {
                    isRetry = true;
                }
                else
                {
                    index++;
                    isRetry = false;
                }
            }

            yield return EndingRoutine();
        }

        private IEnumerator RunAttempt(CreatureEntry entry, bool isRetry)
        {
            attemptSucceeded = false;
            timedOut = false;
            timerRunning = false;
            TimeRemaining = entry.timeLimit;
            ResetAllStations();
            if (creatureNameText != null) creatureNameText.text = "";
            UpdateTimerText();

            // 1. The Landlord's message.
            SetState(GameState.Messaging);
            string line = isRetry ? retryLine : entry.introMessage;
            yield return dialogue.Show(Format(line, entry));

            // 2. The creature enters. Screens stay locked until it settles.
            SetState(GameState.CreatureEntering);
            yield return transport.EnterCreature(entry);
            yield return new WaitForSeconds(settleTime);
            if (creatureNameText != null) creatureNameText.text = entry.creatureName;

            // 3. The timer starts and the three screens open one after another.
            timerRunning = true;

            bool ok = false;
            yield return RunStage(sequencerScreen, GameState.Sequencer);
            if (sequencerScreen.IsCompleted)
            {
                yield return RunStage(geneEditorScreen, GameState.GeneEditor);
                if (geneEditorScreen.IsCompleted)
                {
                    yield return transport.LowerCapsule();
                    yield return RunStage(capsuleScreen, GameState.Capsule);
                    ok = capsuleScreen.IsCompleted;
                }
            }

            timerRunning = false;

            // 4. Success: capsule + creature leave. Fail: the creature dies.
            if (ok)
            {
                SetState(GameState.CreatureLeaving);
                yield return transport.SendOffCreature();
                attemptSucceeded = true;
            }
            else
            {
                SetState(GameState.CreatureDying);
                FreezeAllStations();
                yield return transport.KillCreature();
            }
        }

        private IEnumerator RunStage(StationScreen station, GameState stage)
        {
            if (timedOut) yield break;

            SetState(stage);
            station.Unlock();

            while (station.State == StationState.Active && !timedOut)
                yield return null;

            if (timedOut) station.Freeze();
        }

        private IEnumerator GameOverRoutine(CreatureEntry entry)
        {
            SetState(GameState.GameOver);
            yield return dialogue.Show(Format(gameOverLine, entry));
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        private IEnumerator EndingRoutine()
        {
            SetState(GameState.Ending);
            foreach (string line in endingLines)
                yield return dialogue.Show(line);
            if (endingPanel != null) endingPanel.SetActive(true);
        }

        // ------------------------------------------------------------ timer

        private void Update()
        {
            if (!timerRunning) return;

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0f)
            {
                timerRunning = false;
                timedOut = true;
            }
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            if (timerText == null) return;

            int seconds = Mathf.CeilToInt(TimeRemaining);
            timerText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            timerText.color = TimeRemaining <= 10f && timerRunning ? Color.red : Color.white;
        }

        // ------------------------------------------------------------ helpers

        /// <summary>
        /// Your camera script should ask this before moving to a screen.
        /// index: 0 = left (sequencer), 1 = center (gene editor), 2 = right (capsule).
        /// Screens only work during the three work stages, so this is false while the
        /// Landlord talks, the creature enters, leaves or dies.
        /// </summary>
        public bool CanVisitScreen(int index)
        {
            bool working = State == GameState.Sequencer
                        || State == GameState.GeneEditor
                        || State == GameState.Capsule;
            if (!working) return false;

            // Any screen can be visited during the work stages, even one that is still
            // locked (it shows NOT AVAILABLE). Only the stub keys care about the order.
            return index >= 0 && index <= 2;
        }

        /// <summary>For the Game Over panel's Retry button.</summary>
        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>For the Game Over and Ending panels' Menu button.</summary>
        public void ReturnToMenu()
        {
            SceneManager.LoadScene(menuSceneName);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            StateChanged?.Invoke(newState);
        }

        private void ResetAllStations()
        {
            sequencerScreen.ResetStation();
            geneEditorScreen.ResetStation();
            capsuleScreen.ResetStation();
        }

        private void FreezeAllStations()
        {
            sequencerScreen.Freeze();
            geneEditorScreen.Freeze();
            capsuleScreen.Freeze();
        }

        private void UpdateHud()
        {
            if (failsText != null) failsText.text = "FAILS " + Fails + "/" + maxFails;
        }

        private static string Format(string text, CreatureEntry entry)
        {
            return string.IsNullOrEmpty(text) ? "" : text.Replace("{name}", entry.creatureName);
        }
    }
}