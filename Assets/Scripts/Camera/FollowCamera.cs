using UnityEngine;

namespace Driftworld
{
    // MECH-008 follow basics for the slice scaffold: the camera trails the
    // hero heading, pulls back in glide, and widens slightly on boost. The
    // full framing rules (combat framing, nudge and return, anti clip) land
    // with the MECH-008 build step.
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public PlayerController controller;

        private Camera cam;
        private float yawDeg;
        private float distance;
        private float height;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            distance = Tuning.Camera.follow_distance_m;
            height = Tuning.Camera.follow_height_m;
            if (target != null) yawDeg = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float targetYaw = Mathf.Atan2(target.forward.x, target.forward.z) * Mathf.Rad2Deg;
            yawDeg = Mathf.LerpAngle(yawDeg, targetYaw, 1f - Mathf.Exp(-3.5f * Time.deltaTime));

            bool gliding = controller != null && controller.IsGliding;
            float targetDistance = gliding ? Tuning.Camera.glide_distance_m : Tuning.Camera.follow_distance_m;
            distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-4f * Time.deltaTime));
            height = Mathf.Lerp(height, Tuning.Camera.follow_height_m, 1f - Mathf.Exp(-4f * Time.deltaTime));

            Quaternion yawRot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 back = yawRot * Vector3.back;
            Vector3 desired = target.position + back * distance + Vector3.up * height;
            transform.position = desired;
            transform.LookAt(target.position + Vector3.up * 1.4f);

            float targetFov = Tuning.Camera.fov_deg
                + (controller != null && controller.IsBoosting ? Tuning.Camera.fov_boost_deg : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
    }
}
