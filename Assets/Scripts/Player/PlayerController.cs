using UnityEngine;

namespace Driftworld
{
    // The hero controller for slice build steps 1 and 3: MECH-001 drag
    // movement on the ground and MECH-002 gliding between islands. Combat
    // (MECH-003) extends this class in its own build step.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(DragInput))]
    public class PlayerController : MonoBehaviour
    {
        // World constant for the greybox. Gravity gets a spec home if tuning
        // ever needs to move it; it is not a feel constant from the specs.
        private const float GravityMps2 = 22f;

        public Transform gliderVisual;
        public float rescueY = -30f;

        public bool IsGliding { get; private set; }
        public bool IsBoosting { get; private set; }
        public float BoostGauge { get; private set; }
        public Vector3 HeadingForward => Quaternion.Euler(0f, headingDeg, 0f) * Vector3.forward;

        private CharacterController body;
        private DragInput input;
        private PlayerCombat combat;

        private float headingDeg;
        private float currentSpeed;
        private float verticalVelocity;
        private float airTime;
        private float boostTimer;
        private Vector3 lastGroundedPos;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            input = GetComponent<DragInput>();
            headingDeg = transform.eulerAngles.y;
            lastGroundedPos = transform.position;
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(false);
        }

        private void OnEnable() { input.OnRightTap += HandleRightTap; }
        private void OnDisable() { input.OnRightTap -= HandleRightTap; }

        private void HandleRightTap()
        {
            // MECH-002: while gliding, a right side tap spends the boost gauge.
            if (!IsGliding || IsBoosting) return;
            if (BoostGauge < Tuning.Glide.boost_cost) return;
            BoostGauge -= Tuning.Glide.boost_cost;
            boostTimer = Tuning.Glide.boost_duration_s;
            IsBoosting = true;
        }

        private void Update()
        {
            Vector2 move = input.MoveVector();
            Vector3 wishDir = CameraRelativeDirection(move);

            if (IsGliding)
            {
                UpdateGlide(move, wishDir);
            }
            else if (body.isGrounded)
            {
                UpdateGround(wishDir, move.magnitude);
            }
            else
            {
                UpdateAir(wishDir);
            }

            if (transform.position.y < rescueY)
            {
                RescueToLastGround();
            }
        }

        private Vector3 CameraRelativeDirection(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;
            Camera cam = Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();
            Vector3 dir = right * move.x + fwd * move.y;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private void UpdateGround(Vector3 wishDir, float magnitude)
        {
            airTime = 0f;
            lastGroundedPos = transform.position;

            float targetSpeed = Tuning.Movement.move_speed_mps * magnitude;
            // MECH-003: guarding trades movement for safety at half speed.
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.IsGuarding) targetSpeed *= 0.5f;
            float rateTime = targetSpeed > currentSpeed
                ? Tuning.Movement.accel_time_s
                : Tuning.Movement.stop_time_s;
            float rate = Tuning.Movement.move_speed_mps / Mathf.Max(rateTime, 0.01f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

            if (wishDir.sqrMagnitude > 0.001f && currentSpeed > 0.05f)
            {
                float targetHeading = Mathf.Atan2(wishDir.x, wishDir.z) * Mathf.Rad2Deg;
                headingDeg = Mathf.LerpAngle(headingDeg, targetHeading, 1f - Mathf.Exp(-12f * Time.deltaTime));
            }
            transform.rotation = Quaternion.Euler(0f, headingDeg, 0f);

            verticalVelocity = -2f;
            Vector3 motion = HeadingForward * currentSpeed;
            motion.y = verticalVelocity;
            body.Move(motion * Time.deltaTime);
        }

        private void UpdateAir(Vector3 wishDir)
        {
            airTime += Time.deltaTime;
            verticalVelocity -= GravityMps2 * Time.deltaTime;

            Vector3 motion = wishDir * (Tuning.Movement.move_speed_mps * 0.6f);
            motion.y = verticalVelocity;
            body.Move(motion * Time.deltaTime);

            // MECH-002: the glider opens once the hero is airborne and falling.
            if (airTime >= Tuning.Glide.deploy_delay_s && verticalVelocity < 0f)
            {
                EnterGlide();
            }
        }

        private void EnterGlide()
        {
            IsGliding = true;
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(true);
        }

        private void ExitGlide()
        {
            IsGliding = false;
            IsBoosting = false;
            boostTimer = 0f;
            verticalVelocity = 0f;
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(false);
        }

        private void UpdateGlide(Vector2 move, Vector3 wishDir)
        {
            if (boostTimer > 0f)
            {
                boostTimer -= Time.deltaTime;
                if (boostTimer <= 0f) IsBoosting = false;
            }

            // Steering: the drag direction turns the heading within the rate.
            if (wishDir.sqrMagnitude > 0.001f)
            {
                float targetHeading = Mathf.Atan2(wishDir.x, wishDir.z) * Mathf.Rad2Deg;
                headingDeg = Mathf.MoveTowardsAngle(headingDeg, targetHeading,
                    Tuning.Glide.turn_rate_deg_s * Time.deltaTime);
            }
            transform.rotation = Quaternion.Euler(0f, headingDeg, 0f);

            WindLane lane = WindLane.LaneContaining(transform.position);
            if (lane != null)
            {
                BoostGauge = Mathf.Min(Tuning.Glide.boost_gauge_max,
                    BoostGauge + Tuning.Glide.boost_charge_per_s * Time.deltaTime);
            }

            float speed = Tuning.Glide.glide_speed_mps;
            if (lane != null) speed *= Tuning.Glide.lane_speed_mult;
            if (IsBoosting) speed *= Tuning.Glide.boost_speed_mult;

            float vertical = lane != null ? Tuning.Glide.lane_lift_mps : -Tuning.Glide.sink_rate_mps;

            Vector3 motion = HeadingForward * speed;
            motion.y = vertical;
            body.Move(motion * Time.deltaTime);

            if (body.isGrounded)
            {
                ExitGlide();
                lastGroundedPos = transform.position;
            }
        }

        private void RescueToLastGround()
        {
            // MECH-002: falling into the cloud sea is a navigation error, not a
            // failure. Return to the last ledge with no loot loss.
            ExitGlide();
            body.enabled = false;
            transform.position = lastGroundedPos + Vector3.up * 0.5f;
            body.enabled = true;
            verticalVelocity = 0f;
            airTime = 0f;
        }
    }
}
