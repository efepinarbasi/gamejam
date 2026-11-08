using UnityEngine;

namespace Lacuna
{
    /// <summary>
    /// Put this on an eye object inside a creature's model prefab (the eyeball, or the pivot
    /// that holds it). The eye turns to look at the camera while the body keeps its own
    /// direction. A creature with three eyes gets the script on each of the three.
    ///
    /// The eye turns around its own pivot, so the pivot should sit at the center of the
    /// eyeball. If it does not, put the eyeball under an empty pivot object and add the
    /// script to that pivot.
    /// </summary>
    public class EyeLookAtCamera : MonoBehaviour
    {
        [Tooltip("What the eye stares at. Empty = the main camera.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Turn the aim if the eye's front is not along its forward (Z) axis. If the eye looks sideways or up, try (90,0,0), (-90,0,0), (0,90,0), (0,-90,0) or (0,180,0).")]
        [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;

        [Tooltip("How fast the eye turns, in degrees per second.")]
        [SerializeField, Min(1f)] private float turnSpeed = 360f;

        [Tooltip("How far the eye may turn away from its resting direction, in degrees. 180 = no limit.")]
        [SerializeField, Range(0f, 180f)] private float maxTurnAngle = 80f;

        private Quaternion restLocalRotation;

        private void Awake()
        {
            restLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            // Start by staring right away instead of turning in from the resting direction.
            Aim(true);
        }

        private void LateUpdate()
        {
            Aim(false);
        }

        private void Aim(bool instant)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Vector3 toCamera = cam.transform.position - transform.position;
            if (toCamera.sqrMagnitude < 0.0001f) return;

            // The rotation that makes the eye look at the camera, in the parent's space.
            Quaternion lookWorld = Quaternion.LookRotation(toCamera) * Quaternion.Euler(rotationOffsetEuler);
            Quaternion lookLocal = transform.parent != null
                ? Quaternion.Inverse(transform.parent.rotation) * lookWorld
                : lookWorld;

            // The eye cannot turn further than its limit from the resting direction.
            Quaternion target = Quaternion.RotateTowards(restLocalRotation, lookLocal, maxTurnAngle);

            transform.localRotation = instant
                ? target
                : Quaternion.RotateTowards(transform.localRotation, target, turnSpeed * Time.deltaTime);
        }
    }
}