using TMPro;
using UnityEngine;

namespace Lacuna
{
    public enum StationState { Locked, Active, Completed, Failed }

    /// <summary>
    /// One of the three screens (sequencer, gene editor, capsule).
    /// Right now it is a STUB: with "Use Stub Controls" on, pressing its number key
    /// completes it and Backspace fails it. Later, make a subclass (for example
    /// SequencerScreen : StationScreen), turn the stub off, and call Complete() or
    /// Fail() from your real mechanic. GameManager never needs to change.
    /// </summary>
    public class StationScreen : MonoBehaviour
    {
        [SerializeField] private string stationName = "STATION";

        [Header("Optional visuals")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private GameObject completedVisual;

        [Header("Stub controls (turn off once the real mechanic exists)")]
        [SerializeField] private bool useStubControls = true;
        [SerializeField, Range(1, 3)] private int stubKey = 1;

        public StationState State { get; private set; } = StationState.Locked;
        public bool IsCompleted => State == StationState.Completed;

        public event System.Action Completed;
        public event System.Action Failed;

        protected virtual void Awake()
        {
            Refresh();
        }

        protected virtual void Update()
        {
            if (!useStubControls || State != StationState.Active) return;
            if (!PlayerIsAtThisScreen()) return;

            if (GameInput.NumberPressed(stubKey)) Complete();
            else if (GameInput.BackspacePressed()) Fail();
        }

        /// <summary>
        /// Stub keys only count while the camera is at this screen (screen number = stub key:
        /// 1 = left, 2 = center, 3 = right). Without a camera controller, or when its
        /// Loop Stations list is empty, they count from anywhere.
        /// </summary>
        private bool PlayerIsAtThisScreen()
        {
            CameraStationController cam = CameraStationController.Instance;
            if (cam == null || !cam.HasLoopStations) return true;
            return cam.CurrentLoopIndex == stubKey - 1;
        }

        // ---- called by GameManager ----

        public void ResetStation()
        {
            State = StationState.Locked;
            Refresh();
            OnReset();
        }

        public void Unlock()
        {
            if (State != StationState.Locked) return;
            State = StationState.Active;
            Refresh();
            OnUnlocked();
        }

        /// <summary>Called when time runs out while this screen is still active.</summary>
        public void Freeze()
        {
            if (State != StationState.Active) return;
            State = StationState.Locked;
            Refresh();
            OnFrozen();
        }

        // ---- called by the real mechanic later ----

        public void Complete()
        {
            if (State != StationState.Active) return;
            State = StationState.Completed;
            Refresh();
            OnCompleted();
            Completed?.Invoke();
        }

        public void Fail()
        {
            if (State != StationState.Active) return;
            State = StationState.Failed;
            Refresh();
            OnFailed();
            Failed?.Invoke();
        }

        // ---- hooks for subclasses ----

        protected virtual void OnReset() { }
        protected virtual void OnUnlocked() { }
        protected virtual void OnFrozen() { }
        protected virtual void OnCompleted() { }
        protected virtual void OnFailed() { }

        private void Refresh()
        {
            if (lockedVisual != null) lockedVisual.SetActive(State == StationState.Locked);
            if (activeVisual != null) activeVisual.SetActive(State == StationState.Active);
            if (completedVisual != null) completedVisual.SetActive(State == StationState.Completed);

            if (statusText == null) return;

            string line = State == StationState.Locked
                ? stationName + "\nNOT AVAILABLE"
                : stationName + "\n" + State.ToString().ToUpper();
            if (useStubControls && State == StationState.Active)
                line += "\n(stub) press " + stubKey + " to finish, Backspace to fail";
            statusText.text = line;
        }
    }
}