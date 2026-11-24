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

        // MECH-009: combat rotates the hero directly (aim snap during
        // startup, dodge direction). It goes through here so the movement
        // heading never desyncs from the visible rotation.
        public void SetHeading(float deg)
        {
            headingDeg = deg;
            transform.rotation = Quaternion.Euler(0f, headingDeg, 0f);
        }

        // The current camera relative move direction, for dodge.
        public Vector3 WishDirection()
        {
            return CameraRelativeDirection(input.MoveVector());
        }

        // World direction for a raw screen space direction. The flick
        // dodge reads its direction through here so flicks and drags
        // share one camera relative mapping.
        public Vector3 ScreenToWorldDir(Vector2 screenDir)
        {
            return CameraRelativeDirection(screenDir);
        }

        private CharacterController body;
        private DragInput input;
        private PlayerCombat combat;

        private float headingDeg;
        private float currentSpeed;
        private float verticalVelocity;
        private float airTime;
        private float boostTimer;
        private Vector3 lastGroundedPos;
        private float glideLockout;
        private float glideSampleTimer;
        private Vector3 lastGlideSamplePos;
        private float statusTimer;

        // Instrumentation (temporary, comes out with the diag readout):
        // counts that show exactly how the hero leaves the ground.
        private int groundFrames;
        private int glideEntries;
        private int rayExits;
        private int stuckExits;
        private int groundExits;
        private float lastInputMag;
        public float LastInputMagnitude => lastInputMag;
        private string lastRayHit = "none";

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            input = GetComponent<DragInput>();
            headingDeg = transform.eulerAngles.y;
            lastGroundedPos = transform.position;
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(false);
        }

        private void OnEnable() { input.OnTap += HandleTap; }
        private void OnDisable() { input.OnTap -= HandleTap; }

        private void HandleTap()
        {
            // MECH-002: while gliding, a tap spends the boost gauge.
            // (Controls overhaul 2026-11-24: the tap is anywhere now;
            // on the ground the same gesture is the strike, which
            // PlayerCombat reads from the same gesture stream.)
            if (!IsGliding || IsBoosting) return;
            if (BoostGauge < Tuning.Glide.boost_cost) return;
            BoostGauge -= Tuning.Glide.boost_cost;
            boostTimer = Tuning.Glide.boost_duration_s;
            IsBoosting = true;
        }

        private void Update()
        {
            if (glideLockout > 0f) glideLockout -= Time.deltaTime;

            Vector2 move = input.MoveVector();
            lastInputMag = move.magnitude;
            Vector3 wishDir = CameraRelativeDirection(move);

            if (IsGliding)
            {
                UpdateGlide(move, wishDir);
            }
            else if (body.isGrounded)
            {
                if (combat == null) combat = GetComponent<PlayerCombat>();
                // MECH-009 commitment: mid swing or mid dodge, movement
                // input does not translate the hero. Combat drives the
                // body through its own step in and dodge motion; here we
                // only keep him settled on the ground.
                if (combat != null && combat.IsCommitted) UpdateCommitted();
                else UpdateGround(wishDir, move.magnitude);
            }
            else
            {
                UpdateAir(wishDir);
            }

            if (transform.position.y < rescueY)
            {
                RescueToLastGround();
            }

            statusTimer -= Time.deltaTime;
            if (statusTimer <= 0f)
            {
                statusTimer = 0.25f;
                Diag.Status = "hero " + transform.position.ToString("F1")
                    + " gnd " + body.isGrounded
                    + " glide " + IsGliding
                    + " lg " + lastGroundedPos.ToString("F1")
                    + " inp " + lastInputMag.ToString("F2")
                    + " grd " + groundFrames
                    + " ge " + glideEntries
                    + " xr " + rayExits
                    + " xs " + stuckExits
                    + " xg " + groundExits
                    + " hit " + lastRayHit
                    + " spd " + currentSpeed.ToString("F1")
                    + " vv " + verticalVelocity.ToString("F1");
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

        private void UpdateCommitted()
        {
            groundFrames++;
            airTime = 0f;
            lastGroundedPos = transform.position;
            currentSpeed = 0f;
            verticalVelocity = -2f;
            body.Move(Vector3.down * 2f * Time.deltaTime);
        }

        private void UpdateGround(Vector3 wishDir, float magnitude)
        {
            groundFrames++;
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

            // MECH-002: the glider opens once the hero is airborne, falling,
            // and has dropped well below the ledge he left. The drop gate is
            // what keeps a step off a curb or a ruin block from forcing a
            // glide the player never asked for.
            float dropBelowLedge = lastGroundedPos.y - transform.position.y;
            if (airTime >= Tuning.Glide.deploy_delay_s && verticalVelocity < 0f
                && dropBelowLedge >= 1.5f && glideLockout <= 0f)
            {
                EnterGlide();
            }
        }

        private void EnterGlide()
        {
            glideEntries++;
            IsGliding = true;
            glideSampleTimer = 0f;
            lastGlideSamplePos = transform.position;
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(true);
        }

        private void ExitGlide()
        {
            IsGliding = false;
            IsBoosting = false;
            boostTimer = 0f;
            verticalVelocity = 0f;
            // A short lockout so a landing exit cannot bounce straight back
            // into a glide while the hero settles onto the ground.
            glideLockout = 0.6f;
            if (gliderVisual != null) gliderVisual.gameObject.SetActive(false);
        }

        private void UpdateGlide(Vector2 move, Vector3 wishDir)
        {
            // Landing assist: when ground is just below, the glide is over.
            // Without this, arriving a hair under an island lip pressed the
            // hero against its side and sank him slowly with dead controls.
            RaycastHit rayHit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out rayHit, 1.8f))
            {
                rayExits++;
                lastRayHit = rayHit.collider != null ? rayHit.collider.gameObject.name : "unnamed";
                ExitGlide();
                return;
            }

            // Stuck exit: if the glide is barely moving, the hero is pressed
            // against something. Drop him; gravity and the rescue sort it
            // out in about a second instead of a long hang.
            glideSampleTimer += Time.deltaTime;
            if (glideSampleTimer >= 0.75f)
            {
                float covered = Vector3.Distance(transform.position, lastGlideSamplePos);
                if (covered < 0.4f)
                {
                    stuckExits++;
                    ExitGlide();
                    return;
                }
                glideSampleTimer = 0f;
                lastGlideSamplePos = transform.position;
            }

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

            // Lane lift only carries the hero up to cruising height inside
            // the lane; above that he holds level instead of porpoising out
            // of the top of the lane volume and sinking back in.
            float vertical;
            if (lane != null)
            {
                float cruiseY = lane.transform.position.y + 2f;
                vertical = transform.position.y < cruiseY ? Tuning.Glide.lane_lift_mps : 0f;
            }
            else
            {
                vertical = -Tuning.Glide.sink_rate_mps;
            }

            Vector3 motion = HeadingForward * speed;
            motion.y = vertical;
            body.Move(motion * Time.deltaTime);

            if (body.isGrounded)
            {
                groundExits++;
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
