using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-003 grown into MECH-009: weapon based action combat. The hero
    // carries one weapon from WeaponCatalog; every move is an AttackStep
    // with startup, active, and recovery phases, and hits resolve only
    // during active frames against per part hurtboxes. Attacks are
    // committed: movement input does not move the hero mid swing, the step
    // in comes from the move data, and dodge is the universal cancel out of
    // recovery. Guard, hitstun, input buffering, and hitstop carry over
    // from the spear build; the spear itself is now the Long Sword class
    // and keeps the Wind Spear as its special.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        public HoldButton strikeButton;
        public HoldButton guardButton;
        public HoldButton skillButton;
        public HoldButton dodgeButton;
        public HoldButton lockButton;
        public Vector3 respawnPoint;

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public bool IsGuarding { get; private set; }
        public bool IsDead { get; private set; }
        public float SkillCooldownRemaining { get; private set; }
        public float LastDamageAt { get; private set; }
        public float LastStaminaDeniedAt { get; private set; }

        public enum Phase { Free, Startup, Active, Recovery, Dodge }
        public Phase CurrentPhase { get; private set; }
        public bool IsCommitted => CurrentPhase != Phase.Free;
        public bool IsDodging => CurrentPhase == Phase.Dodge;
        public WeaponDefinition Weapon { get; private set; }
        public string CurrentStepName => currentStep != null && CurrentPhase != Phase.Free && CurrentPhase != Phase.Dodge
            ? currentStep.Name : "";
        public bool IsInvulnerable => CurrentPhase == Phase.Dodge
            && dodgeTime >= Tuning.Dodge.iframe_start_s && dodgeTime <= Tuning.Dodge.iframe_end_s;

        private CharacterController body;
        private PlayerController controller;
        private float skillCooldown;
        private float staminaRegenWait;
        private float hitstunTimer;
        private float hitstopTimer;
        private float bufferedStrikeUntil;
        private float bufferedDodgeUntil;
        private bool strikeHeld;
        private float strikeHoldTime;
        private float strikePressUnscaled;
        private Vector3 knockback;

        // Attack state.
        private AttackStep currentStep;
        private int currentComboIndex = -1;
        private float phaseTime;
        private int swingIdCounter;
        private int currentSwingId;
        private int hitsDone;
        private float nextHitAt;
        private readonly Dictionary<MossHorn, int> swingHits = new Dictionary<MossHorn, int>();
        private bool bufferedTap;
        private int pendingComboIndex;
        private float comboWindowUntil;

        // Dodge state.
        private float dodgeTime;
        private Vector3 dodgeDir;
        private float dodgeCooldown;

        // Weapon presentation: a code built pivot at the hero's right side.
        // Presentation only; it never creates hits (juice skill rule).
        private Transform weaponPivot;
        private GameObject weaponVisual;
        private GameObject shieldVisual;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            controller = GetComponent<PlayerController>();
            Health = Tuning.Combat.hero_health_max;
            Stamina = Tuning.Combat.stamina_max;
            LastDamageAt = -100f;
            LastStaminaDeniedAt = -100f;
            EquipWeapon("longsword");
        }

        public void EquipWeapon(string weaponId)
        {
            Weapon = WeaponCatalog.ById(weaponId);
            currentStep = null;
            CurrentPhase = Phase.Free;
            pendingComboIndex = 0;
            BuildWeaponVisual();
        }

        private void BuildWeaponVisual()
        {
            if (weaponPivot == null)
            {
                weaponPivot = new GameObject("WeaponPivot").transform;
                weaponPivot.SetParent(transform, false);
                weaponPivot.localPosition = new Vector3(0.45f, 1.15f, 0.1f);
            }
            if (weaponVisual != null) Object.Destroy(weaponVisual);
            if (shieldVisual != null) Object.Destroy(shieldVisual);
            weaponVisual = new GameObject("Weapon_" + Weapon.WeaponId);
            weaponVisual.transform.SetParent(weaponPivot, false);

            var steel = Diag.SafeMaterial(new Color(0.62f, 0.68f, 0.75f));
            var grip = Diag.SafeMaterial(new Color(0.3f, 0.22f, 0.15f));
            switch (Weapon.WeaponId)
            {
                case "greatsword":
                    Blade(new Vector3(0f, 0f, 1.15f), new Vector3(0.3f, 0.06f, 2.3f), steel);
                    Blade(new Vector3(0f, 0f, 0.05f), new Vector3(0.1f, 0.1f, 0.35f), grip);
                    break;
                case "swordshield":
                    Blade(new Vector3(0f, 0f, 0.55f), new Vector3(0.09f, 0.09f, 1.1f), steel);
                    shieldVisual = new GameObject("Shield");
                    shieldVisual.transform.SetParent(transform, false);
                    shieldVisual.transform.localPosition = new Vector3(-0.5f, 1.1f, 0.15f);
                    var sh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sh.transform.SetParent(shieldVisual.transform, false);
                    sh.transform.localScale = new Vector3(0.75f, 0.95f, 0.09f);
                    sh.GetComponent<Renderer>().material = Diag.SafeMaterial(new Color(0.45f, 0.35f, 0.2f));
                    Object.Destroy(sh.GetComponent<Collider>());
                    break;
                case "dualblades":
                    Blade(new Vector3(-0.22f, 0f, 0.45f), new Vector3(0.07f, 0.07f, 0.9f), steel);
                    Blade(new Vector3(0.22f, 0f, 0.45f), new Vector3(0.07f, 0.07f, 0.9f), steel);
                    break;
                case "hammer":
                    Blade(new Vector3(0f, 0f, 0.6f), new Vector3(0.09f, 0.09f, 1.3f), grip);
                    Blade(new Vector3(0f, 0f, 1.3f), new Vector3(0.48f, 0.48f, 0.48f), steel);
                    break;
                default: // longsword
                    Blade(new Vector3(0f, 0f, 1.1f), new Vector3(0.09f, 0.09f, 2.2f), steel);
                    Blade(new Vector3(0f, 0f, 0.02f), new Vector3(0.16f, 0.16f, 0.12f), grip);
                    break;
            }
            weaponPivot.localRotation = Quaternion.Euler(25f, 0f, 0f);
        }

        private void Blade(Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(weaponVisual.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = mat;
            Object.Destroy(go.GetComponent<Collider>());
        }

        private bool StrikeDown()
        {
            bool key = Input.GetKey(KeyCode.J);
            bool btn = strikeButton != null && strikeButton.IsHeld;
            return key || btn;
        }

        private bool StrikePressedEdge()
        {
            // GetKeyDown is event based: a tap shorter than one frame still
            // registers. A level based edge built on GetKey misses those
            // taps entirely whenever the frame rate dips.
            return Input.GetKeyDown(KeyCode.J) || (strikeButton != null && strikeButton.PressedThisFrame);
        }

        private bool StrikeReleasedEdge(out float heldSeconds)
        {
            // Hold duration is judged in unscaled time, matching HoldButton,
            // so a quick tap stays a light attack even when the frame rate
            // drops and one frame of scaled deltaTime would cross the heavy
            // threshold on its own.
            heldSeconds = Time.unscaledTime - strikePressUnscaled;
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

        private bool DodgePressed()
        {
            return Input.GetKeyDown(KeyCode.Space) || (dodgeButton != null && dodgeButton.PressedThisFrame);
        }

        private bool LockPressed()
        {
            return Input.GetKeyDown(KeyCode.C) || (lockButton != null && lockButton.PressedThisFrame);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (skillCooldown > 0f) skillCooldown -= dt;
            SkillCooldownRemaining = Mathf.Max(0f, skillCooldown);
            if (dodgeCooldown > 0f) dodgeCooldown -= dt;

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

            Targeting.Validate(transform.position);
            if (LockPressed() && !controller.IsGliding) Targeting.ToggleOrCycle(transform.position);

            IsGuarding = GuardHeld() && !controller.IsGliding && hitstunTimer <= 0f && CurrentPhase == Phase.Free;

            if (hitstunTimer > 0f)
            {
                hitstunTimer -= dt;
                if (StrikePressedEdge()) bufferedStrikeUntil = Time.time + Tuning.Combat.input_buffer_s;
                if (DodgePressed()) bufferedDodgeUntil = Time.time + Tuning.Combat.input_buffer_s;
                if (hitstunTimer <= 0f)
                {
                    if (Time.time <= bufferedDodgeUntil) StartDodge();
                    else if (Time.time <= bufferedStrikeUntil) StartComboStep(0);
                }
                return;
            }

            if (controller.IsGliding) return;

            // Committed phases run their own clocks.
            if (CurrentPhase == Phase.Dodge) { UpdateDodge(dt); AnimateWeapon(dt); return; }
            if (CurrentPhase != Phase.Free) { UpdateAttack(dt); AnimateWeapon(dt); return; }

            // Free: dodge first, then skill, then the strike press pattern.
            if (DodgePressed()) { StartDodge(); AnimateWeapon(dt); return; }
            if (SkillPressed() && skillCooldown <= 0f) DoSpecial();

            if (StrikePressedEdge())
            {
                strikeHeld = true;
                strikeHoldTime = 0f;
                strikePressUnscaled = Time.unscaledTime;
            }
            if (strikeHeld)
            {
                strikeHoldTime += dt;
                if (StrikeReleasedEdge(out float held))
                {
                    strikeHeld = false;
                    if (held >= Tuning.Combat.heavy_charge_s) StartHeavy(held);
                    else StartComboStep(Time.time <= comboWindowUntil ? pendingComboIndex : 0);
                }
            }
            AnimateWeapon(dt);
        }

        private void StartComboStep(int index)
        {
            if (Weapon.Combo.Count == 0) return;
            index = Mathf.Clamp(index, 0, Weapon.Combo.Count - 1);
            StartStep(Weapon.Combo[index], index, 1f);
        }

        private void StartHeavy(float heldSeconds)
        {
            float scale = 1f;
            if (Weapon.WeaponId == "greatsword")
            {
                // Charge levels: the hold that makes a heavy is level 1;
                // holding on scales the multiplier up to level 3.
                int level = 1;
                if (heldSeconds >= Tuning.Combat.heavy_charge_s * 5f) level = 3;
                else if (heldSeconds >= Tuning.Combat.heavy_charge_s * 2.5f) level = 2;
                scale = WeaponCatalog.GreatSwordChargeMult[level];
            }
            StartStep(Weapon.Heavy, -1, scale);
        }

        private void StartStep(AttackStep step, int comboIndex, float damageScale)
        {
            if (step.StaminaCost > 0f)
            {
                if (Stamina < step.StaminaCost)
                {
                    // The spear era rule survives: a heavy the hero cannot
                    // afford falls back to a plain combo opener. A special
                    // with a cost simply refuses.
                    if (comboIndex < 0 && step != Weapon.Special) { StartComboStep(0); return; }
                    LastStaminaDeniedAt = Time.time;
                    return;
                }
                Stamina -= step.StaminaCost;
                staminaRegenWait = Tuning.Combat.stamina_regen_delay_s;
            }

            if (damageScale != 1f)
            {
                step = CloneStep(step);
                step.DamageMult *= damageScale;
            }
            currentStep = step;
            currentComboIndex = comboIndex;
            phaseTime = 0f;
            bufferedTap = false;
            CurrentPhase = Phase.Startup;
        }

        private static AttackStep CloneStep(AttackStep s)
        {
            return new AttackStep
            {
                Name = s.Name, Startup = s.Startup, Active = s.Active, Recovery = s.Recovery,
                DamageMult = s.DamageMult, StaggerMult = s.StaggerMult, ElementMult = s.ElementMult,
                StaminaCost = s.StaminaCost, Range = s.Range, ArcDeg = s.ArcDeg, StepIn = s.StepIn,
                Hits = s.Hits, HitInterval = s.HitInterval, Kind = s.Kind, StunMult = s.StunMult,
                DodgeCancelStartup = s.DodgeCancelStartup, HyperArmor = s.HyperArmor, HitstopMs = s.HitstopMs,
            };
        }

        private void UpdateAttack(float dt)
        {
            // Taps during a swing buffer into the combo at the cancel point;
            // dodge cancels recovery outright and startup where allowed.
            if (StrikePressedEdge()) bufferedTap = true;
            if (DodgePressed())
            {
                bool canCancel = CurrentPhase == Phase.Recovery
                    || (CurrentPhase == Phase.Startup && currentStep.DodgeCancelStartup);
                if (canCancel) { StartDodge(); return; }
            }

            phaseTime += dt;
            float stepWindow = Mathf.Max(currentStep.Startup + currentStep.Active, 0.01f);

            if (CurrentPhase == Phase.Startup || CurrentPhase == Phase.Active)
            {
                // Step in: the move carries the hero, spread across the
                // startup and active window.
                body.Move(controller.HeadingForward * (currentStep.StepIn / stepWindow) * dt);
            }

            if (CurrentPhase == Phase.Startup)
            {
                RotateTowardAim(dt);
                if (phaseTime >= currentStep.Startup)
                {
                    CurrentPhase = Phase.Active;
                    phaseTime = 0f;
                    hitsDone = 0;
                    nextHitAt = 0f;
                    currentSwingId = ++swingIdCounter;
                    swingHits.Clear();
                }
            }
            else if (CurrentPhase == Phase.Active)
            {
                while (hitsDone < currentStep.Hits && phaseTime >= nextHitAt)
                {
                    RunSweep(hitsDone);
                    hitsDone++;
                    nextHitAt += Mathf.Max(currentStep.HitInterval, 0.01f);
                    if (hitstopTimer > 0f) return; // hitstop freezes the swing mid window
                }
                if (phaseTime >= currentStep.Active)
                {
                    CurrentPhase = Phase.Recovery;
                    phaseTime = 0f;
                }
            }
            else if (CurrentPhase == Phase.Recovery)
            {
                if (bufferedTap && phaseTime >= currentStep.Recovery * Tuning.Combat.combo_cancel_frac)
                {
                    int next = currentComboIndex >= 0 && currentComboIndex + 1 < Weapon.Combo.Count
                        ? currentComboIndex + 1 : 0;
                    StartComboStep(next);
                    return;
                }
                if (phaseTime >= currentStep.Recovery)
                {
                    pendingComboIndex = currentComboIndex >= 0 && currentComboIndex + 1 < Weapon.Combo.Count
                        ? currentComboIndex + 1 : 0;
                    comboWindowUntil = Time.time + Tuning.Combat.combo_window_s;
                    currentStep = null;
                    currentComboIndex = -1;
                    CurrentPhase = Phase.Free;
                }
            }
        }

        // One hit index of one swing, tested against every living monster.
        // A monster already recorded for this hit index is never hit twice
        // by it (MECH-009 AC-3); later hit indices of a multi hit step may
        // connect again by design.
        private void RunSweep(int hitIndex)
        {
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 facing = controller.HeadingForward;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn m = MossHorn.All[i];
                if (m == null || m.IsDefeated || m.Hurtboxes == null) continue;
                int applied;
                if (swingHits.TryGetValue(m, out applied) && applied > hitIndex) continue;

                HurtboxPart bestPart = null;
                float bestDist = float.MaxValue;
                var parts = m.Hurtboxes.Parts;
                for (int p = 0; p < parts.Count; p++)
                {
                    if (parts[p].Inert) continue;
                    float d;
                    if (HurtboxSet.TestPart(origin, facing, currentStep, parts[p], out d) && d < bestDist)
                    {
                        bestDist = d;
                        bestPart = parts[p];
                    }
                }
                if (bestPart == null) continue;

                DamageResult r = DamagePipeline.Resolve(Weapon, currentStep, m, bestPart,
                    currentSwingId, hitIndex, CurrentPhase.ToString());
                int outcome = m.ApplyDamage(r, currentStep.Kind);
                r.BrokePart = outcome == 1;
                r.SeveredPart = outcome == 2;
                swingHits[m] = hitIndex + 1;
                CombatEvents.PublishMonsterHit(r);

                if (currentStep.HitstopMs > 0f)
                {
                    float stop = currentStep.HitstopMs / 1000f;
                    hitstopTimer = stop;
                    m.ApplyHitstop(stop);
                }
            }
        }

        private void RotateTowardAim(float dt)
        {
            MossHorn aim = Targeting.AimTarget(transform.position,
                currentStep != null ? currentStep.Range : Tuning.Combat.strike_range_m);
            if (aim == null) return;
            Vector3 to = aim.Center - transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float currentYaw = Mathf.Atan2(controller.HeadingForward.x, controller.HeadingForward.z) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(currentYaw, targetYaw, Tuning.Combat.attack_turn_rate_deg_s * dt);
            controller.SetHeading(next);
        }

        private void StartDodge()
        {
            if (CurrentPhase == Phase.Dodge || dodgeCooldown > 0f) return;
            if (Stamina < Tuning.Dodge.stamina_cost)
            {
                LastStaminaDeniedAt = Time.time;
                return;
            }
            Stamina -= Tuning.Dodge.stamina_cost;
            staminaRegenWait = Tuning.Combat.stamina_regen_delay_s;
            Vector3 wish = controller.WishDirection();
            dodgeDir = wish.sqrMagnitude > 0.01f ? wish.normalized : -controller.HeadingForward;
            float yaw = Mathf.Atan2(dodgeDir.x, dodgeDir.z) * Mathf.Rad2Deg;
            controller.SetHeading(yaw);
            dodgeTime = 0f;
            currentStep = null;
            currentComboIndex = -1;
            strikeHeld = false;
            CurrentPhase = Phase.Dodge;
        }

        private void UpdateDodge(float dt)
        {
            dodgeTime += dt;
            float u = Mathf.Clamp01(dodgeTime / Tuning.Dodge.duration_s);
            // Ease out profile whose integral over the dodge is exactly the
            // authored distance.
            float profile = 2f * (1f - u);
            body.Move(dodgeDir * Tuning.Dodge.distance_m * profile * (dt / Tuning.Dodge.duration_s));
            if (dodgeTime >= Tuning.Dodge.duration_s)
            {
                CurrentPhase = Phase.Free;
                dodgeCooldown = Tuning.Dodge.cooldown_s;
                comboWindowUntil = 0f;
            }
        }

        private void DoSpecial()
        {
            if (Weapon.SpecialIsProjectile)
            {
                skillCooldown = Weapon.SpecialCooldown;
                MossHorn target = Targeting.AimTarget(transform.position, Tuning.Combat.skill_range_m);
                Vector3 dir = controller.HeadingForward;
                if (target != null)
                {
                    Vector3 to = target.Center - (transform.position + Vector3.up * 1.2f);
                    if (to.sqrMagnitude > 0.01f) dir = to.normalized;
                }
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "WindSpear";
                go.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                go.GetComponent<Renderer>().material = Diag.SafeMaterial(new Color(0.55f, 0.95f, 1f));
                Object.Destroy(go.GetComponent<Collider>());
                var spear = go.AddComponent<WindSpear>();
                spear.Launch(transform.position + Vector3.up * 1.2f + dir * 0.6f, dir);
            }
            else if (Weapon.Special != null)
            {
                skillCooldown = Weapon.SpecialCooldown;
                StartStep(Weapon.Special, -1, 1f);
            }
        }

        // Damage arrives here from creatures. Dodge iframes turn it into a
        // clean evade (no damage, no hitstun, and the harvest hold is not
        // interrupted, because LastDamageAt does not move). Guarding cuts
        // damage by the weapon guard factor. A hit cancels the current
        // swing unless the move carries hyper armor.
        public void TakeDamage(float amount, Vector3 sourcePos)
        {
            if (IsDead || controller.IsGliding) return;

            if (IsInvulnerable)
            {
                CombatEvents.PublishHeroHit(amount, true);
                return;
            }

            float dmg = amount;
            if (IsGuarding) dmg *= Weapon.GuardMult;
            Health -= dmg;
            LastDamageAt = Time.time;
            CombatEvents.PublishHeroHit(dmg, false);

            if (strikeHeld)
            {
                strikeHeld = false;
                strikeHoldTime = 0f;
            }

            bool armored = CurrentPhase != Phase.Free && CurrentPhase != Phase.Dodge
                && currentStep != null && currentStep.HyperArmor;
            if (!IsGuarding && !armored)
            {
                if (CurrentPhase != Phase.Free)
                {
                    currentStep = null;
                    currentComboIndex = -1;
                    CurrentPhase = Phase.Free;
                }
                Vector3 away = transform.position - sourcePos;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f) knockback = away.normalized * Tuning.Combat.knockback_mps;
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
            currentStep = null;
            CurrentPhase = Phase.Free;
            IsDead = false;
        }

        // Procedural weapon presentation: the pivot winds back through
        // startup, sweeps through active, and settles in recovery. Pure
        // presentation driven by the authoritative phase clock.
        private void AnimateWeapon(float dt)
        {
            if (weaponPivot == null) return;
            float targetX = 25f;
            if (CurrentPhase == Phase.Startup && currentStep != null)
            {
                float u = Mathf.Clamp01(phaseTime / Mathf.Max(currentStep.Startup, 0.01f));
                targetX = Mathf.Lerp(25f, -65f, u);
            }
            else if (CurrentPhase == Phase.Active)
            {
                targetX = 55f;
            }
            else if (CurrentPhase == Phase.Recovery)
            {
                targetX = 25f;
            }
            else if (strikeHeld && Weapon != null && Weapon.WeaponId == "greatsword" && strikeHoldTime > 0.1f)
            {
                targetX = -80f + Mathf.Sin(Time.time * 30f) * 2f;
            }
            var e = weaponPivot.localRotation.eulerAngles;
            float x = Mathf.LerpAngle(e.x, targetX, 1f - Mathf.Exp(-18f * dt));
            weaponPivot.localRotation = Quaternion.Euler(x, 0f, 0f);
        }
    }
}
