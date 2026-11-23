using UnityEngine;

namespace Driftworld
{
    // MECH-003: spear combat. A tap on the strike button strikes; holding
    // it charges a heavy strike that costs stamina on release; the guard
    // button trades movement for safety; the skill button throws the wind
    // spear on a cooldown. Hitstop is per actor and lands on heavy
    // connects only. Input during hitstun is buffered and executes on
    // recovery.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        public HoldButton strikeButton;
        public HoldButton guardButton;
        public HoldButton skillButton;
        public Vector3 respawnPoint;

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public bool IsGuarding { get; private set; }
        public bool IsDead { get; private set; }
        public float SkillCooldownRemaining { get; private set; }
        public float LastDamageAt { get; private set; }

        private CharacterController body;
        private PlayerController controller;
        private float strikeCooldown;
        private float skillCooldown;
        private float staminaRegenWait;
        private float hitstunTimer;
        private float hitstopTimer;
        private float bufferedStrikeUntil;
        private bool strikeHeld;
        private float strikeHoldTime;
        private bool keyStrikeWasDown;
        private Vector3 knockback;
        private GameObject spearVisualProto;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            controller = GetComponent<PlayerController>();
            Health = Tuning.Combat.hero_health_max;
            Stamina = Tuning.Combat.stamina_max;
            LastDamageAt = -100f;
        }

        private bool StrikeDown()
        {
            bool key = Input.GetKey(KeyCode.J);
            bool btn = strikeButton != null && strikeButton.IsHeld;
            return key || btn;
        }

        private bool StrikePressedEdge()
        {
            bool keyDown = Input.GetKey(KeyCode.J);
            bool edge = (keyDown && !keyStrikeWasDown) || (strikeButton != null && strikeButton.PressedThisFrame);
            keyStrikeWasDown = keyDown;
            return edge;
        }

        private bool StrikeReleasedEdge(out float heldSeconds)
        {
            heldSeconds = strikeHoldTime;
            bool keyUp = Input.GetKeyUp(KeyCode.J);
            bool btnUp = strikeButton != null && strikeButton.ReleasedThisFrame;
            if (btnUp) heldSeconds = strikeButton.HeldSecondsAtRelease;
            return keyUp || btnUp;
        }

        private bool GuardHeld()
        {
            return Input.GetKey(KeyCode.K) || (guardButton != null && guardButton.IsHeld);
        }

        private bool SkillPressed()
        {
            return Input.GetKeyDown(KeyCode.L) || (skillButton != null && skillButton.PressedThisFrame);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (strikeCooldown > 0f) strikeCooldown -= dt;
            if (skillCooldown > 0f) skillCooldown -= dt;
            SkillCooldownRemaining = Mathf.Max(0f, skillCooldown);

            if (hitstopTimer > 0f)
            {
                hitstopTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (staminaRegenWait > 0f) staminaRegenWait -= dt;
            else Stamina = Mathf.Min(Tuning.Combat.stamina_max, Stamina + Tuning.Combat.stamina_regen_per_s * dt);

            if (knockback.sqrMagnitude > 0.01f)
            {
                body.Move(knockback * dt);
                knockback = Vector3.MoveTowards(knockback, Vector3.zero, 14f * dt);
            }

            IsGuarding = GuardHeld() && !controller.IsGliding && hitstunTimer <= 0f;

            if (hitstunTimer > 0f)
            {
                hitstunTimer -= dt;
                if (StrikePressedEdge()) bufferedStrikeUntil = Time.time + Tuning.Combat.input_buffer_s;
                if (hitstunTimer <= 0f && Time.time <= bufferedStrikeUntil)
                {
                    DoStrike(false);
                }
                return;
            }

            if (controller.IsGliding) return;

            // Strike press, hold, release.
            if (StrikePressedEdge())
            {
                strikeHeld = true;
                strikeHoldTime = 0f;
            }
            if (strikeHeld)
            {
                strikeHoldTime += dt;
                if (StrikeReleasedEdge(out float held))
                {
                    strikeHeld = false;
                    if (held >= Tuning.Combat.heavy_charge_s)
                    {
                        DoStrike(true);
                    }
                    else if (strikeCooldown <= 0f)
                    {
                        DoStrike(false);
                    }
                }
            }

            if (SkillPressed() && skillCooldown <= 0f)
            {
                DoSkill();
            }
        }

        private MossHorn BestTarget(float range)
        {
            MossHorn best = null;
            float bestDist = range;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c == null || c.IsDefeated) continue;
                Vector3 to = c.Center - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        private void DoStrike(bool heavy)
        {
            if (heavy)
            {
                if (Stamina < Tuning.Combat.heavy_stamina_cost)
                {
                    // Not enough stamina: the heavy falls back to a strike.
                    heavy = false;
                }
                else
                {
                    Stamina -= Tuning.Combat.heavy_stamina_cost;
                    staminaRegenWait = Tuning.Combat.stamina_regen_delay_s;
                }
            }
            strikeCooldown = Tuning.Combat.strike_rate_s;

            // A short step in, never an auto lunge.
            body.Move(controller.HeadingForward * 0.3f);

            MossHorn target = BestTarget(Tuning.Combat.strike_range_m + 0.9f);
            if (target == null) return;
            Vector3 to = target.Center - transform.position;
            to.y = 0f;
            if (to.magnitude > 0.01f)
            {
                float angle = Vector3.Angle(controller.HeadingForward, to.normalized);
                if (angle > 80f && to.magnitude > 1.2f) return;
            }

            float damage = heavy ? Tuning.Combat.strike_damage * Tuning.Combat.heavy_damage_mult : Tuning.Combat.strike_damage;
            float stagger = heavy ? Tuning.MossHorn.stagger_heavy : Tuning.MossHorn.stagger_strike;
            target.TakeHit(damage, stagger, false);

            if (heavy)
            {
                float stop = Tuning.Combat.hitstop_ms / 1000f;
                hitstopTimer = stop;
                target.ApplyHitstop(stop);
            }
        }

        private void DoSkill()
        {
            skillCooldown = Tuning.Combat.skill_cooldown_s;

            MossHorn target = BestTarget(22f);
            Vector3 dir = controller.HeadingForward;
            if (target != null)
            {
                Vector3 to = target.Center - (transform.position + Vector3.up * 1.2f);
                if (to.sqrMagnitude > 0.01f) dir = to.normalized;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "WindSpear";
            go.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            var mat = Diag.SafeMaterial(new Color(0.55f, 0.95f, 1f));
            go.GetComponent<Renderer>().material = mat;
            Object.Destroy(go.GetComponent<Collider>());
            var spear = go.AddComponent<WindSpear>();
            spear.Launch(transform.position + Vector3.up * 1.2f + dir * 0.6f, dir);
        }

        // Damage arrives here from creatures. Guarding cuts it by the guard
        // factor and prevents knockback. Taking damage while charging a
        // heavy loses the charge and spends no stamina.
        public void TakeDamage(float amount, Vector3 sourcePos)
        {
            if (IsDead || controller.IsGliding) return;

            float dmg = amount;
            if (IsGuarding) dmg *= Tuning.Combat.guard_damage_mult;
            Health -= dmg;
            LastDamageAt = Time.time;

            if (strikeHeld)
            {
                strikeHeld = false;
                strikeHoldTime = 0f;
            }

            if (!IsGuarding)
            {
                Vector3 away = transform.position - sourcePos;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f) knockback = away.normalized * 4.5f;
                hitstunTimer = Tuning.Combat.hitstun_s;
            }

            if (Health <= 0f)
            {
                Health = 0f;
                Die();
            }
        }

        private void Die()
        {
            // Slice behavior: the hero returns to the start of the island at
            // full health. Expedition failure banking lands with MECH-007.
            IsDead = true;
            body.enabled = false;
            transform.position = respawnPoint;
            body.enabled = true;
            Health = Tuning.Combat.hero_health_max;
            Stamina = Tuning.Combat.stamina_max;
            knockback = Vector3.zero;
            hitstunTimer = 0f;
            IsDead = false;
        }
    }
}
