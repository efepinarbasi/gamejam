using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Lacuna
{
    /// <summary>
    /// Everything physical about a creature's trip: the glass door opens, the wheel-rail
    /// carries the creature to the center, the capsule descends over it and is sized to fit.
    /// Success: the capsule launches like a rocket. Fail: it explodes. Then the empty
    /// carriage rolls back out. GameManager only calls the four coroutines at the bottom
    /// of the public section.
    /// Neither the creature nor the capsule is a child of the platform, so the platform's
    /// scale does not matter. The creature follows the platform by hand (see LateUpdate).
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
        [SerializeField] private float railTravelTime = 2.5f;
        [Tooltip("The door starts closing once the carriage has covered this fraction of the trip.")]
        [SerializeField, Range(0.1f, 1f)] private float closeDoorAtProgress = 0.6f;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private Vector3 wheelAxis = Vector3.right;
        [SerializeField] private float wheelDegreesPerMeter = 360f;

        [Header("Capsule (built at runtime to fit each creature)")]
        [Tooltip("Optional: your capsule model. Empty = a default capsule shape. Either way it is stretched to fit the creature.")]
        [SerializeField] private GameObject capsulePrefab;
        [Tooltip("Optional: material for the default capsule shape. Make it transparent so the creature stays visible.")]
        [SerializeField] private Material capsuleMaterial;
        [Tooltip("How many times bigger than the creature the capsule is. The rounded ends cut its corners, so keep it above 1.2.")]
        [SerializeField, Min(1f)] private float capsuleFit = 1.3f;
        [Tooltip("Extra room on every side, in meters.")]
        [SerializeField, Min(0f)] private float capsulePadding = 0.03f;
        [Tooltip("Optional: the capsule starts at this transform's height, above the creature. Empty = it starts 'Capsule Drop Height' above its final position.")]
        [SerializeField] private Transform capsuleStartPoint;
        [SerializeField] private float capsuleDropHeight = 0.6f;
        [SerializeField] private float capsuleMoveTime = 1.2f;

        [Header("Capsule launch (success: rocket)")]
        [SerializeField] private Vector3 rocketDirection = Vector3.up;
        [Tooltip("The capsule shakes this long before liftoff.")]
        [SerializeField] private float rocketShakeTime = 0.6f;
        [SerializeField] private float rocketShakeAmount = 0.01f;
        [SerializeField] private float rocketAcceleration = 12f;
        [SerializeField] private float rocketFlightTime = 1.6f;
        [Tooltip("Optional: flame or smoke prefab that follows the bottom of the capsule while it flies.")]
        [SerializeField] private GameObject rocketEffect;

        [Header("Capsule explosion (fail)")]
        [SerializeField, Min(0)] private int debrisCount = 16;
        [SerializeField] private float explosionForce = 3f;
        [Tooltip("Pause after the explosion, before the empty carriage rolls back.")]
        [SerializeField] private float explosionPause = 1f;
        [Tooltip("Optional: explosion particles, spawned where the capsule was.")]
        [SerializeField] private GameObject explosionEffect;

        [Header("Creature")]
        [Tooltip("Where the creature stands, relative to the platform (platform local space). Use it to put the creature's feet on the carriage.")]
        [SerializeField] private Vector3 creatureLocalOffset = Vector3.zero;
        [Tooltip("Size of the placeholder shape used when a creature has no model yet.")]
        [SerializeField] private Vector3 placeholderScale = new Vector3(0.25f, 0.25f, 0.25f);
        [SerializeField] private float deathShrinkTime = 0.7f;

        [Header("Sound effects")]
        [Tooltip("Plays when the capsule starts its launch (when it begins to shake, before it lifts off).")]
        [SerializeField] private AudioClip rocketLaunchSound;
        [SerializeField, Range(0f, 1f)] private float rocketLaunchVolume = 1f;
        [Tooltip("Plays every time the door opens or closes.")]
        [SerializeField] private AudioClip doorSlideSound;
        [SerializeField, Range(0f, 1f)] private float doorSlideVolume = 1f;
        [Tooltip("0 = plain stereo, 1 = fully 3D (louder when the camera is near the door or capsule).")]
        [SerializeField, Range(0f, 1f)] private float sfxSpatialBlend = 0.5f;

        [Header("Hooks for sound and VFX")]
        public UnityEvent onDoorOpening = new UnityEvent();
        public UnityEvent onDoorClosing = new UnityEvent();
        public UnityEvent onCreatureArrived = new UnityEvent();
        public UnityEvent onCapsuleLowered = new UnityEvent();
        public UnityEvent onCapsuleLaunched = new UnityEvent();
        public UnityEvent onCapsuleExploded = new UnityEvent();
        public UnityEvent onCreatureDied = new UnityEvent();
        public UnityEvent onCreatureSentOff = new UnityEvent();

        public GameObject CurrentCreature { get; private set; }
        public GameObject CurrentCapsule { get; private set; }

        private Vector3 doorClosedPos;
        private Quaternion doorClosedRot;
        private bool doorBusy;

        private Vector3 creatureJitter;
        private bool creatureRidesCapsule;
        private Vector3 creatureRideOffset;
        private AudioSource sfxSource;

        private Vector3 capsulePivotOffset; // capsule pivot minus the center of its body
        private Vector3 capsuleSize;        // size of the capsule's body after fitting
        private Vector3 capsuleFinalPivot;  // where the capsule's pivot ends up over the creature

        private Vector3 SpawnPos => spawnPoint != null ? spawnPoint.position : transform.position;
        private Vector3 CenterPos => centerPoint != null ? centerPoint.position : transform.position;

        private void Awake()
        {
            if (platform == null)
            {
                platform = new GameObject("CreaturePlatform").transform;
                platform.SetParent(transform, false);
            }
            platform.position = SpawnPos;

            // A child object carries the sound effects, so it can be moved to the door or capsule.
            GameObject sfx = new GameObject("SfxSource");
            sfx.transform.SetParent(transform, false);
            sfxSource = sfx.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;

            if (door != null)
            {
                doorClosedPos = door.localPosition;
                doorClosedRot = door.localRotation;
            }
        }

        private void LateUpdate()
        {
            // The creature lives at the scene root, so it follows its ride by hand.
            if (CurrentCreature == null) return;

            if (creatureRidesCapsule && CurrentCapsule != null)
                CurrentCreature.transform.position = CurrentCapsule.transform.position + creatureRideOffset;
            else
                CurrentCreature.transform.position = platform.TransformPoint(creatureLocalOffset) + creatureJitter;
        }

        // ------------------------------------------------------------ public coroutines

        /// <summary>Door opens, the rail brings the creature to the center, the door closes.</summary>
        public IEnumerator EnterCreature(CreatureEntry entry)
        {
            DespawnCapsule();
            SpawnCreature(entry);
            yield return OpenDoor();
            yield return MoveRail(CenterPos, BeginCloseDoor);
            yield return WaitForDoor();
            onCreatureArrived.Invoke();
        }

        /// <summary>A capsule sized to the creature descends over it.</summary>
        public IEnumerator LowerCapsule()
        {
            if (CurrentCreature == null) yield break;

            BuildCapsule();
            yield return MoveTransform(CurrentCapsule.transform, capsuleFinalPivot, capsuleMoveTime);
            onCapsuleLowered.Invoke();
        }

        /// <summary>Success: the capsule launches like a rocket, then the empty carriage returns.</summary>
        public IEnumerator SendOffCreature()
        {
            if (CurrentCapsule != null) yield return LaunchCapsule();
            else DespawnCreature();

            onCreatureSentOff.Invoke();
            yield return ReturnEmptyCarriage();
        }

        /// <summary>
        /// Fail: the capsule explodes (if it is already down), otherwise the creature just
        /// fades away. Then the empty carriage returns.
        /// </summary>
        public IEnumerator KillCreature()
        {
            onCreatureDied.Invoke();

            if (CurrentCapsule != null)
            {
                ExplodeCapsule();
                yield return new WaitForSeconds(explosionPause);
            }
            else if (CurrentCreature != null)
            {
                yield return ShrinkAway(CurrentCreature.transform);
                DespawnCreature();
            }

            yield return ReturnEmptyCarriage();
        }

        // ------------------------------------------------------------ creature

        private void SpawnCreature(CreatureEntry entry)
        {
            DespawnCreature();

            GameObject go;
            if (entry.modelPrefab != null)
            {
                // No parent: it spawns directly at the scene root and keeps the
                // prefab's own rotation and scale.
                go = Instantiate(entry.modelPrefab);
                go.name = entry.creatureName;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = entry.creatureName + " (placeholder)";
                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.localScale = placeholderScale;
            }

            creatureJitter = Vector3.zero;
            creatureRidesCapsule = false;
            go.transform.position = platform.TransformPoint(creatureLocalOffset);
            CurrentCreature = go;
        }

        private void DespawnCreature()
        {
            creatureRidesCapsule = false;
            if (CurrentCreature != null) Destroy(CurrentCreature);
            CurrentCreature = null;
        }

        private IEnumerator ShrinkAway(Transform t)
        {
            Vector3 startScale = t.localScale;
            float time = 0f;

            while (time < deathShrinkTime)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / Mathf.Max(0.01f, deathShrinkTime));
                t.localScale = Vector3.Lerp(startScale, Vector3.zero, k * k);
                creatureJitter = Random.insideUnitSphere * 0.02f * (1f - k);
                yield return null;
            }
        }

        // ------------------------------------------------------------ capsule

        private void BuildCapsule()
        {
            DespawnCapsule();

            // 1. Measure the creature in the world (renderer bounds, no raycasts needed).
            Bounds creature = MeasureBounds(CurrentCreature, new Vector3(0.3f, 0.3f, 0.3f));

            // 2. Target size. The footprint is treated as a circle around the creature,
            //    so the capsule is wide enough whichever way the creature is turned.
            float footprint = Mathf.Sqrt(creature.size.x * creature.size.x + creature.size.z * creature.size.z);
            float width = footprint * capsuleFit + capsulePadding * 2f;
            float height = creature.size.y * capsuleFit + capsulePadding * 2f;
            Vector3 target = new Vector3(width, height, width);

            // 3. Create the capsule shape: your prefab, or a default capsule.
            GameObject go;
            if (capsulePrefab != null)
            {
                go = Instantiate(capsulePrefab);
                go.name = "Capsule";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Capsule (placeholder)";
                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                if (capsuleMaterial != null) go.GetComponent<Renderer>().sharedMaterial = capsuleMaterial;
            }

            // 4. Measure the shape at scale 1, then stretch it to the target size.
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Bounds own = MeasureBounds(go, new Vector3(1f, 2f, 1f));

            Vector3 scale = new Vector3(
                target.x / Mathf.Max(0.0001f, own.size.x),
                target.y / Mathf.Max(0.0001f, own.size.y),
                target.z / Mathf.Max(0.0001f, own.size.z));
            go.transform.localScale = scale;

            // Scaling happens around the pivot, so the center of the body moves too.
            Vector3 centerFromPivot = Vector3.Scale(own.center, scale);
            capsulePivotOffset = -centerFromPivot;
            capsuleSize = target;

            // 5. Final spot: body centered on the creature. Start: above it.
            Vector3 finalCenter = creature.center;
            capsuleFinalPivot = finalCenter + capsulePivotOffset;

            Vector3 startCenter = capsuleStartPoint != null
                ? new Vector3(finalCenter.x, capsuleStartPoint.position.y, finalCenter.z)
                : finalCenter + Vector3.up * capsuleDropHeight;

            go.transform.position = startCenter + capsulePivotOffset;
            CurrentCapsule = go;
        }

        private void DespawnCapsule()
        {
            if (CurrentCapsule != null) Destroy(CurrentCapsule);
            CurrentCapsule = null;
        }

        private IEnumerator LaunchCapsule()
        {
            onCapsuleLaunched.Invoke();
            PlaySfx(rocketLaunchSound, rocketLaunchVolume, CurrentCapsule.transform.position);

            Transform cap = CurrentCapsule.transform;
            Vector3 basePos = cap.position;
            Vector3 dir = rocketDirection.sqrMagnitude > 0.0001f ? rocketDirection.normalized : Vector3.up;

            // The creature rides inside the capsule from now on.
            if (CurrentCreature != null)
            {
                creatureRideOffset = CurrentCreature.transform.position - basePos;
                creatureRidesCapsule = true;
            }

            // 1. The capsule shakes harder and harder while the engines spin up.
            float t = 0f;
            while (t < rocketShakeTime)
            {
                t += Time.deltaTime;
                float strength = Mathf.Clamp01(t / Mathf.Max(0.01f, rocketShakeTime));
                cap.position = basePos + Random.insideUnitSphere * rocketShakeAmount * strength;
                yield return null;
            }
            cap.position = basePos;

            // 2. Liftoff: it keeps accelerating along the launch direction.
            GameObject flame = null;
            if (rocketEffect != null)
                flame = Instantiate(rocketEffect, basePos, Quaternion.LookRotation(-dir));

            Vector3 velocity = Vector3.zero;
            t = 0f;
            while (t < rocketFlightTime)
            {
                float dt = Time.deltaTime;
                t += dt;
                velocity += dir * rocketAcceleration * dt;
                cap.position += velocity * dt;

                if (flame != null)
                {
                    Vector3 bodyCenter = cap.position - capsulePivotOffset;
                    flame.transform.position = bodyCenter - dir * (capsuleSize.y * 0.5f);
                }
                yield return null;
            }

            if (flame != null) Destroy(flame, 2f);

            DespawnCreature();
            DespawnCapsule();
        }

        private void ExplodeCapsule()
        {
            Vector3 center = CurrentCapsule.transform.position - capsulePivotOffset;
            Vector3 size = capsuleSize;
            onCapsuleExploded.Invoke();

            if (explosionEffect != null)
            {
                GameObject fx = Instantiate(explosionEffect, center, Quaternion.identity);
                Destroy(fx, 5f);
            }

            // Built-in fallback: a burst of small glowing pieces.
            for (int i = 0; i < debrisCount; i++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = "CapsuleDebris";

                float s = Mathf.Max(0.02f, size.x * Random.Range(0.08f, 0.18f));
                piece.transform.localScale = new Vector3(s, s, s);

                Vector3 offset = Vector3.Scale(Random.insideUnitSphere, size * 0.4f);
                piece.transform.position = center + offset;
                piece.transform.rotation = Random.rotation;
                piece.GetComponent<Renderer>().material.color =
                    Color.Lerp(new Color(1f, 0.35f, 0.05f), new Color(1f, 0.8f, 0.2f), Random.value);

                Rigidbody body = piece.AddComponent<Rigidbody>();
                body.mass = 0.05f;
                Vector3 away = offset.sqrMagnitude > 0.0001f ? offset.normalized : Random.onUnitSphere;
                body.AddForce((away + Vector3.up * 0.6f) * explosionForce * Random.Range(0.6f, 1.2f), ForceMode.VelocityChange);
                body.AddTorque(Random.onUnitSphere * 5f, ForceMode.VelocityChange);

                Destroy(piece, 2.5f);
            }

            DespawnCreature();
            DespawnCapsule();
        }

        /// <summary>Union of all visible renderers, or colliders, or a fallback box.</summary>
        private static Bounds MeasureBounds(GameObject root, Vector3 fallbackSize)
        {
            bool found = false;
            Bounds result = new Bounds(root.transform.position, Vector3.zero);

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

                if (!found) { result = r.bounds; found = true; }
                else result.Encapsulate(r.bounds);
            }

            if (!found)
            {
                foreach (Collider c in root.GetComponentsInChildren<Collider>())
                {
                    if (!found) { result = c.bounds; found = true; }
                    else result.Encapsulate(c.bounds);
                }
            }

            if (!found)
                result = new Bounds(root.transform.position + Vector3.up * fallbackSize.y * 0.5f, fallbackSize);

            return result;
        }

        private IEnumerator ReturnEmptyCarriage()
        {
            yield return OpenDoor();
            yield return MoveRail(SpawnPos, BeginCloseDoor);
            yield return WaitForDoor();
        }

        private void PlaySfx(AudioClip clip, float volume, Vector3 position)
        {
            if (clip == null || sfxSource == null) return;

            sfxSource.transform.position = position;
            sfxSource.spatialBlend = sfxSpatialBlend;
            sfxSource.PlayOneShot(clip, volume);
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

            PlaySfx(doorSlideSound, doorSlideVolume, door.position);

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