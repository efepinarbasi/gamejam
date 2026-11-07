using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Lacuna
{
    /// <summary>
    /// Everything physical about a creature's trip: the glass door opens, the wheel-rail
    /// carries the creature to the center, the capsule descends over it, and later the
    /// creature leaves with the capsule (or dies). GameManager only calls the four
    /// coroutines at the bottom of the public section.
    /// Keep the platform's scale at 1, because the creature is parented to it.
    /// </summary>
    public class CreatureTransport : MonoBehaviour
    {
        [Header("Door (left side, inside the glass)")]
        [SerializeField] private Transform door;
        [Tooltip("Local-space slide when the door opens (default: up).")]
        [SerializeField] private Vector3 doorOpenLocalOffset = new Vector3(0f, 1.5f, 0f);
        [Tooltip("Extra local rotation when open, for a hinged door. Leave 0 for a sliding door.")]
        [SerializeField] private Vector3 doorOpenEulerOffset = Vector3.zero;
        [SerializeField] private float doorMoveTime = 0.8f;

        [Header("Wheel rail")]
        [Tooltip("The carriage the creature stands on. Leave empty to auto-create an invisible one.")]
        [SerializeField] private Transform platform;
        [Tooltip("Where the carriage waits, outside the door.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Where the creature is examined.")]
        [SerializeField] private Transform centerPoint;
        [Tooltip("Optional: where the creature is sent. Empty = back out through the door.")]
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float railTravelTime = 2.5f;
        [Tooltip("The door starts closing once the carriage has covered this fraction of the trip.")]
        [SerializeField, Range(0.1f, 1f)] private float closeDoorAtProgress = 0.6f;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private Vector3 wheelAxis = Vector3.right;
        [SerializeField] private float wheelDegreesPerMeter = 360f;

        [Header("Capsule (above the creature)")]
        [SerializeField] private Transform capsule;
        [Tooltip("Optional. Empty = lower by 'Capsule Lower Distance'.")]
        [SerializeField] private Transform capsuleLowPoint;
        [SerializeField] private float capsuleLowerDistance = 0.5f;
        [SerializeField] private float capsuleMoveTime = 1.2f;

        [Header("Creature")]
        [SerializeField] private Vector3 creatureLocalOffset = Vector3.zero;
        [Tooltip("Size of the placeholder shape used when a creature has no model yet.")]
        [SerializeField] private Vector3 placeholderScale = new Vector3(0.25f, 0.25f, 0.25f);
        [SerializeField] private float deathShrinkTime = 0.7f;

        [Header("Hooks for sound and VFX")]
        public UnityEvent onDoorOpening = new UnityEvent();
        public UnityEvent onDoorClosing = new UnityEvent();
        public UnityEvent onCreatureArrived = new UnityEvent();
        public UnityEvent onCapsuleLowered = new UnityEvent();
        public UnityEvent onCreatureDied = new UnityEvent();
        public UnityEvent onCreatureSentOff = new UnityEvent();

        public GameObject CurrentCreature { get; private set; }

        private Vector3 doorClosedPos;
        private Quaternion doorClosedRot;
        private Vector3 capsuleHighPos;
        private Transform capsuleHome;
        private bool capsuleLowered;
        private bool doorBusy;

        private Vector3 SpawnPos => spawnPoint != null ? spawnPoint.position : transform.position;
        private Vector3 CenterPos => centerPoint != null ? centerPoint.position : transform.position;
        private Vector3 ExitPos => exitPoint != null ? exitPoint.position : SpawnPos;

        private void Awake()
        {
            if (platform == null)
            {
                platform = new GameObject("CreaturePlatform").transform;
                platform.SetParent(transform, false);
            }
            platform.position = SpawnPos;

            if (door != null)
            {
                doorClosedPos = door.localPosition;
                doorClosedRot = door.localRotation;
            }

            if (capsule != null)
            {
                capsuleHighPos = capsule.position;
                capsuleHome = capsule.parent;
            }
        }

        // ------------------------------------------------------------ public coroutines

        /// <summary>Door opens, the rail brings the creature to the center, the door closes.</summary>
        public IEnumerator EnterCreature(CreatureEntry entry)
        {
            SpawnCreature(entry);
            yield return OpenDoor();
            yield return MoveRail(CenterPos, BeginCloseDoor);
            yield return WaitForDoor();
            onCreatureArrived.Invoke();
        }

        /// <summary>The capsule descends over the creature.</summary>
        public IEnumerator LowerCapsule()
        {
            if (capsule == null) yield break;

            Vector3 target = capsuleLowPoint != null
                ? capsuleLowPoint.position
                : capsuleHighPos + Vector3.down * capsuleLowerDistance;

            yield return MoveTransform(capsule, target, capsuleMoveTime);
            capsuleLowered = true;
            onCapsuleLowered.Invoke();
        }

        /// <summary>Success: the creature leaves inside its capsule.</summary>
        public IEnumerator SendOffCreature()
        {
            if (capsule != null) capsule.SetParent(platform, true);

            yield return OpenDoor();
            yield return MoveRail(ExitPos, BeginCloseDoor);
            yield return WaitForDoor();

            onCreatureSentOff.Invoke();
            DespawnCreature();
            ResetCapsule();
            platform.position = SpawnPos;
        }

        /// <summary>Fail: the creature dies and the empty carriage goes back out.</summary>
        public IEnumerator KillCreature()
        {
            onCreatureDied.Invoke();

            if (CurrentCreature != null)
                yield return ShrinkAway(CurrentCreature.transform);
            DespawnCreature();

            if (capsule != null && capsuleLowered)
            {
                yield return MoveTransform(capsule, capsuleHighPos, capsuleMoveTime);
                capsuleLowered = false;
            }

            yield return OpenDoor();
            yield return MoveRail(SpawnPos, BeginCloseDoor);
            yield return WaitForDoor();
        }

        // ------------------------------------------------------------ creature

        private void SpawnCreature(CreatureEntry entry)
        {
            DespawnCreature();

            GameObject go;
            if (entry.modelPrefab != null)
            {
                go = Instantiate(entry.modelPrefab, platform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = entry.creatureName + " (placeholder)";
                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.SetParent(platform, false);
                go.transform.localScale = placeholderScale;
            }

            go.transform.localPosition = creatureLocalOffset;
            go.transform.localRotation = Quaternion.identity;
            CurrentCreature = go;
        }

        private void DespawnCreature()
        {
            if (CurrentCreature != null) Destroy(CurrentCreature);
            CurrentCreature = null;
        }

        private IEnumerator ShrinkAway(Transform t)
        {
            Vector3 startScale = t.localScale;
            Vector3 startPos = t.localPosition;
            float time = 0f;

            while (time < deathShrinkTime)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / Mathf.Max(0.01f, deathShrinkTime));
                t.localScale = Vector3.Lerp(startScale, Vector3.zero, k * k);
                t.localPosition = startPos + UnityEngine.Random.insideUnitSphere * 0.02f * (1f - k);
                yield return null;
            }
        }

        private void ResetCapsule()
        {
            if (capsule == null) return;
            capsule.SetParent(capsuleHome, true);
            capsule.position = capsuleHighPos;
            capsuleLowered = false;
        }

        // ------------------------------------------------------------ door

        private IEnumerator OpenDoor()
        {
            doorBusy = true;
            onDoorOpening.Invoke();
            yield return MoveDoor(true);
        }

        private IEnumerator CloseDoor()
        {
            onDoorClosing.Invoke();
            yield return MoveDoor(false);
        }

        private void BeginCloseDoor()
        {
            doorBusy = true;
            StartCoroutine(CloseDoor());
        }

        private IEnumerator WaitForDoor()
        {
            while (doorBusy) yield return null;
        }

        private IEnumerator MoveDoor(bool open)
        {
            if (door == null)
            {
                doorBusy = false;
                yield break;
            }

            Vector3 fromPos = door.localPosition;
            Quaternion fromRot = door.localRotation;
            Vector3 toPos = open ? doorClosedPos + doorOpenLocalOffset : doorClosedPos;
            Quaternion toRot = open ? doorClosedRot * Quaternion.Euler(doorOpenEulerOffset) : doorClosedRot;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, doorMoveTime);
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                door.localPosition = Vector3.Lerp(fromPos, toPos, eased);
                door.localRotation = Quaternion.Slerp(fromRot, toRot, eased);
                yield return null;
            }

            door.localPosition = toPos;
            door.localRotation = toRot;
            doorBusy = false;
        }

        // ------------------------------------------------------------ movement

        private IEnumerator MoveRail(Vector3 target, System.Action atProgress)
        {
            Vector3 start = platform.position;
            Vector3 last = start;
            float t = 0f;
            bool fired = false;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, railTravelTime);
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                Vector3 pos = Vector3.Lerp(start, target, eased);

                SpinWheels(Vector3.Distance(last, pos));
                last = pos;
                platform.position = pos;

                if (!fired && atProgress != null && t >= closeDoorAtProgress)
                {
                    fired = true;
                    atProgress();
                }
                yield return null;
            }

            platform.position = target;
            if (!fired && atProgress != null) atProgress();
        }

        private IEnumerator MoveTransform(Transform t, Vector3 target, float duration)
        {
            Vector3 start = t.position;
            float time = 0f;

            while (time < 1f)
            {
                time += Time.deltaTime / Mathf.Max(0.01f, duration);
                t.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time)));
                yield return null;
            }

            t.position = target;
        }

        private void SpinWheels(float meters)
        {
            if (wheels == null) return;
            float degrees = meters * wheelDegreesPerMeter;
            foreach (Transform wheel in wheels)
            {
                if (wheel != null) wheel.Rotate(wheelAxis, degrees, Space.Self);
            }
        }
    }
}
