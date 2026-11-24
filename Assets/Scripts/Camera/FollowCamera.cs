using UnityEngine;

namespace Driftworld
{
    // MECH-008 follow basics plus the MECH-009 lock on mode. Unlocked, the
    // camera trails the hero heading, pulls back in glide, and widens on
    // boost. Locked, it frames the hero and the locked monster together:
    // yaw follows the hero to target line and the pull back grows with the
    // separation, so the monster stays in frame while the hero closes in.
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public PlayerController controller;

        private Camera cam;
        private float yawDeg;
        private float distance;
        private float height;
        private bool yawSnapped;

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

            bool gliding = controller != null && controller.IsGliding;
            MossHorn locked = !gliding && Targeting.Locked ? Targeting.Current : null;

            float targetYaw;
            float targetDistance;
            Vector3 lookPoint;
            if (locked != null)
            {
                Vector3 toMonster = locked.Center - target.position;
                toMonster.y = 0f;
                float separation = toMonster.magnitude;
                targetYaw = Mathf.Atan2(toMonster.x, toMonster.z) * Mathf.Rad2Deg;
                targetDistance = Tuning.Camera.follow_distance_m
                    + Mathf.Min(Tuning.Camera.lock_distance_bonus_m, separation * Tuning.Camera.lock_distance_factor);
                lookPoint = (target.position + locked.transform.position) * 0.5f + Vector3.up * 1.3f;
            }
            else
            {
                targetYaw = Mathf.Atan2(target.forward.x, target.forward.z) * Mathf.Rad2Deg;
                targetDistance = gliding ? Tuning.Camera.glide_distance_m : Tuning.Camera.follow_distance_m;
                lookPoint = target.position + Vector3.up * 1.4f;
            }

            // Snap on the first frame: the target is assigned after Awake,
            // so yawDeg starts at zero and a lerp from there swings the
            // camera, and the player's first drag, around at spawn.
            if (!yawSnapped)
            {
                yawDeg = targetYaw;
                yawSnapped = true;
            }
            yawDeg = Mathf.LerpAngle(yawDeg, targetYaw, 1f - Mathf.Exp(-3.5f * Time.deltaTime));

            distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-4f * Time.deltaTime));
            height = Mathf.Lerp(height, Tuning.Camera.follow_height_m, 1f - Mathf.Exp(-4f * Time.deltaTime));

            Quaternion yawRot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 back = yawRot * Vector3.back;
            Vector3 desired = target.position + back * distance + Vector3.up * height;
            transform.position = desired;
            transform.LookAt(lookPoint);

            float targetFov = Tuning.Camera.fov_deg
                + (controller != null && controller.IsBoosting ? Tuning.Camera.fov_boost_deg : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-6f * Time.deltaTime));

            Diag.CamStatus = "cam " + transform.position.ToString("F1") + (locked != null ? " lock " + locked.DisplayName : "");
        }
    }
}
