using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-004 plus MECH-009: the Moss Horn. A walking hillside with a
    // temper. States: graze, notice, approach, threaten, charge, recover,
    // flinch, staggered, stunned, knockdown, enrage roar, exhausted,
    // retreat, defeated. Hard rule from the spec: every charge is preceded
    // by the full tell, no exceptions. Reactions may interrupt a tell (the
    // charge then never fires, which is the reward for reading it), but a
    // charge never fires without one. The elder shares the rig and the
    // state machine and adds the horn slam plus scaled health and damage.
    public class MossHorn : MonoBehaviour
    {
        public static readonly List<MossHorn> All = new List<MossHorn>();

        public enum State
        {
            Graze, Notice, Approach, Threaten, Charge, Recover, Flinch,
            Staggered, Stunned, Knockdown, EnrageRoar, Exhausted, Retreat,
            Defeated
        }

        public bool IsElder { get; private set; }
        public bool IsHarvested { get; private set; }
        public string TransactionId { get; private set; }
        public State CurrentState { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public bool IsDefeated => CurrentState == State.Defeated;
        public Vector3 Center => transform.position + Vector3.up * 1.1f;
        public string DisplayName => IsElder ? "Elder Moss Horn" : "Moss Horn";

        // MECH-009 surface.
        public HurtboxSet Hurtboxes { get; private set; }
        public float PartScale => visualScale;
        public Vector3 PartCenterOffset { get; private set; }
        public bool IsDamageVulnerable => CurrentState == State.Staggered
            || CurrentState == State.Stunned || CurrentState == State.Knockdown;
        public bool IsEnraged { get; private set; }
        public bool IsExhausted { get; private set; }
        public float StaggerPool => staggerPool;
        public float StunPool => stunPool;
        public float KnockdownPool => knockdownPool;
        public bool TrainingMode { get; set; }

        private float groundY;
        private Vector3 islandCenter;
        private Vector2 islandHalf;
        private Vector3 homePos;
        private Transform visual;
        private Transform head;
        private Transform tailVisual;
        private Material tailMaterial;
        private Vector3 headRestPos;
        private float visualScale = 1f;
        private Transform barGroup;
        private Transform barFill;
        private Material bodyMaterial;
        private Color bodyBaseColor;

        private float stateTimer;
        private float staggerPool;
        private float stunPool;
        private float knockdownPool;
        private float hitstopTimer;
        private float flinchReadyAt;
        private float enrageTimer;
        private bool enragedOnce;
        private bool exhaustedOnce;
        private bool retreatedOnce;
        private bool slamPending;
        private bool aggroedByDamage;
        private bool hitPlayerThisCharge;
        private Vector3 chargeDir;
        private float chargeTraveled;
        private Vector3 wanderTarget;
        private float wanderTimer;
        private float defeatTimer;
        private PlayerCombat player;
        private PlayerController playerBody;

        public void Configure(Vector3 pos, float ground, bool elder, Vector3 center, Vector2 half, int index)
        {
            IsElder = elder;
            groundY = ground;
            islandCenter = center;
            islandHalf = half;
            homePos = pos;
            TransactionId = "mosshorn_" + index;
            MaxHp = elder ? Tuning.MossHorn.hp_elder : Tuning.MossHorn.hp_common;
            Hp = MaxHp;
            CurrentState = State.Graze;
            transform.position = pos;
            wanderTarget = pos;
            BuildVisual();
            Hurtboxes = gameObject.AddComponent<HurtboxSet>();
            Hurtboxes.Build(this);
            All.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void BuildVisual()
        {
            float s = IsElder ? 1.45f : 1f;
            visualScale = s;
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);

            bodyBaseColor = IsElder ? new Color(0.32f, 0.36f, 0.2f) : new Color(0.42f, 0.48f, 0.28f);
            bodyMaterial = Diag.SafeMaterial(bodyBaseColor);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(visual, false);
            body.transform.localPosition = new Vector3(0f, 1.05f * s, 0f);
            body.transform.localScale = new Vector3(1.25f * s, 1.3f * s, 2.3f * s);
            body.GetComponent<Renderer>().material = bodyMaterial;
            Object.Destroy(body.GetComponent<Collider>());

            // Legs, so the leg hurtboxes have something to read against.
            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.name = "Leg";
                    leg.transform.SetParent(visual, false);
                    leg.transform.localPosition = new Vector3(0.45f * s * ix, 0.45f * s, 0.75f * s * iz);
                    leg.transform.localScale = new Vector3(0.3f * s, 0.9f * s, 0.3f * s);
                    leg.GetComponent<Renderer>().material = bodyMaterial;
                    Object.Destroy(leg.GetComponent<Collider>());
                }
            }

            // The tail: severable, so it gets its own transform and material.
            tailMaterial = Diag.SafeMaterial(bodyBaseColor * 0.9f);
            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(visual, false);
            tail.transform.localPosition = new Vector3(0f, 1.15f * s, -1.5f * s);
            tail.transform.localScale = new Vector3(0.28f * s, 0.28f * s, 0.9f * s);
            tail.GetComponent<Renderer>().material = tailMaterial;
            Object.Destroy(tail.GetComponent<Collider>());
            tailVisual = tail.transform;

            head = new GameObject("Head").transform;
            head.SetParent(visual, false);
            headRestPos = new Vector3(0f, 1.35f * s, 1.35f * s);
            head.localPosition = headRestPos;

            var skull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skull.name = "Skull";
            skull.transform.SetParent(head, false);
            skull.transform.localScale = new Vector3(0.95f * s, 0.8f * s, 0.9f * s);
            skull.GetComponent<Renderer>().material = bodyMaterial;
            Object.Destroy(skull.GetComponent<Collider>());

            var hornMat = Diag.SafeMaterial(new Color(0.75f, 0.7f, 0.6f));
            for (int i = -1; i <= 1; i += 2)
            {
                var horn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                horn.name = "Horn";
                horn.transform.SetParent(head, false);
                horn.transform.localPosition = new Vector3(0.42f * s * i, 0.45f * s, -0.1f);
                horn.transform.localRotation = Quaternion.Euler(0f, 0f, -0.5f * i);
                horn.transform.localScale = new Vector3(0.16f * s, 1.1f * s, 0.16f * s);
                horn.GetComponent<Renderer>().material = hornMat;
                Object.Destroy(horn.GetComponent<Collider>());
            }

            // Health bar: two thin boxes riding above the creature, shown
            // once it has taken damage and hidden again on defeat.
            barGroup = new GameObject("HealthBar").transform;
            barGroup.SetParent(transform, false);
            barGroup.localPosition = new Vector3(0f, 2.9f * s, 0f);

            var bgMat = Diag.SafeMaterial(new Color(0.08f, 0.08f, 0.08f));
            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.transform.SetParent(barGroup, false);
            bg.transform.localScale = new Vector3(2.2f, 0.18f, 0.06f);
            bg.GetComponent<Renderer>().material = bgMat;
            Object.Destroy(bg.GetComponent<Collider>());

            var fillMat = Diag.SafeMaterial(new Color(0.45f, 0.8f, 0.35f));
            barFill = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            barFill.SetParent(barGroup, false);
            barFill.localScale = new Vector3(2.1f, 0.12f, 0.08f);
            barFill.GetComponent<Renderer>().material = fillMat;
            Object.Destroy(barFill.GetComponent<Collider>());
            barGroup.gameObject.SetActive(false);
        }

        private void EnsurePlayer()
        {
            if (player != null) return;
            player = Object.FindAnyObjectByType<PlayerCombat>();
            if (player != null) playerBody = player.GetComponent<PlayerController>();
        }

        // After exhaustion the monster stays weakened while it is below
        // the exhaustion health line: slower charges, longer recoveries.
        private bool Weakened => exhaustedOnce && Hp <= MaxHp * Tuning.MossHorn.exhaust_hp_frac;

        private float RecoverDuration()
        {
            float d = Tuning.MossHorn.recover_s;
            if (IsEnraged) d *= Tuning.MossHorn.enrage_recover_mult;
            if (Weakened) d *= Tuning.MossHorn.exhaust_recover_mult;
            return d;
        }

        private void Update()
        {
            if (hitstopTimer > 0f)
            {
                hitstopTimer -= Time.unscaledDeltaTime;
                return;
            }
            if (CurrentState == State.Defeated)
            {
                // The sandbox training monster resets itself so the next
                // drill starts without a scene reload.
                if (TrainingMode)
                {
                    defeatTimer += Time.deltaTime;
                    if (defeatTimer >= 4f) ResetFight();
                }
                return;
            }
            EnsurePlayer();
            if (player == null) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            float dt = Time.deltaTime;

            if (IsEnraged)
            {
                enrageTimer -= dt;
                if (enrageTimer <= 0f) BeginExhaustion();
            }

            switch (CurrentState)
            {
                case State.Graze:
                    Wander(dt);
                    if (dist < Tuning.MossHorn.notice_range_m) SetState(State.Notice);
                    break;

                case State.Notice:
                    FaceTowards(toPlayer, 6f, dt);
                    if (IsElder && dist < Tuning.MossHorn.slam_trigger_range_m)
                    {
                        slamPending = true;
                        stateTimer = Tuning.MossHorn.slam_tell_s;
                        SetState(State.Threaten);
                    }
                    else if (dist < Tuning.MossHorn.aggro_range_m)
                    {
                        slamPending = false;
                        stateTimer = TellDuration();
                        SetState(State.Threaten);
                    }
                    else if (dist < Tuning.MossHorn.notice_range_m + 2f)
                    {
                        SetState(State.Approach);
                    }
                    else if (dist > Tuning.MossHorn.notice_range_m + 6f)
                    {
                        aggroedByDamage = false;
                        staggerPool = 0f;
                        SetState(State.Graze);
                    }
                    break;

                case State.Approach:
                    FaceTowards(toPlayer, 5f, dt);
                    {
                        float speed = Tuning.MossHorn.approach_speed_mps * (Weakened ? 0.6f : 1f);
                        Vector3 next = transform.position + toPlayer.normalized * speed * dt;
                        if (InsideIsland(next)) transform.position = next;
                    }
                    if (IsElder && dist < Tuning.MossHorn.slam_trigger_range_m)
                    {
                        slamPending = true;
                        stateTimer = Tuning.MossHorn.slam_tell_s;
                        SetState(State.Threaten);
                    }
                    else if (dist < Tuning.MossHorn.aggro_range_m)
                    {
                        slamPending = false;
                        stateTimer = TellDuration();
                        SetState(State.Threaten);
                    }
                    else if (dist > Tuning.MossHorn.notice_range_m + 8f)
                    {
                        aggroedByDamage = false;
                        staggerPool = 0f;
                        SetState(State.Graze);
                    }
                    break;

                case State.Threaten:
                    // The tell: head down, pawing. It always runs its full
                    // duration before anything fires.
                    FaceTowards(toPlayer, 3f, dt);
                    if (head != null) head.localPosition = headRestPos + Vector3.down * (0.28f * visualScale) + Vector3.down * 0.05f * visualScale * Mathf.Sin(Time.time * 18f);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        if (head != null) head.localPosition = headRestPos;
                        if (dist > 24f)
                        {
                            staggerPool = 0f;
                            aggroedByDamage = false;
                            SetState(State.Graze);
                        }
                        else if (slamPending)
                        {
                            if (dist < Tuning.MossHorn.slam_range_m) player.TakeDamage(Tuning.MossHorn.slam_damage_elder, transform.position);
                            stateTimer = RecoverDuration();
                            SetState(State.Recover);
                        }
                        else
                        {
                            chargeDir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
                            chargeTraveled = 0f;
                            hitPlayerThisCharge = false;
                            SetState(State.Charge);
                        }
                    }
                    break;

                case State.Charge:
                    {
                        float speed = Tuning.MossHorn.charge_speed_mps
                            * (IsEnraged ? Tuning.MossHorn.enrage_charge_speed_mult : 1f)
                            * (Weakened ? 0.7f : 1f);
                        float step = speed * dt;
                        Vector3 next = transform.position + chargeDir * step;
                        if (!InsideIsland(next))
                        {
                            // The creature stops at the edge and recovers. It
                            // never falls; a falling creature cannot be harvested.
                            stateTimer = RecoverDuration();
                            SetState(State.Recover);
                            break;
                        }
                        transform.position = next;
                        chargeTraveled += step;
                        FaceTowards(chargeDir, 10f, dt);
                        if (!hitPlayerThisCharge && dist < Tuning.MossHorn.charge_hit_distance_m)
                        {
                            hitPlayerThisCharge = true;
                            player.TakeDamage(Tuning.MossHorn.charge_damage, transform.position);
                        }
                        if (chargeTraveled >= Tuning.MossHorn.charge_max_m)
                        {
                            stateTimer = RecoverDuration();
                            SetState(State.Recover);
                        }
                    }
                    break;

                case State.Recover:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) SetState(dist < Tuning.MossHorn.notice_range_m ? State.Notice : State.Graze);
                    break;

                case State.Flinch:
                    if (head != null) head.localPosition = headRestPos + Vector3.right * 0.08f * visualScale * Mathf.Sin(Time.time * 40f);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        if (head != null) head.localPosition = headRestPos;
                        SetState(State.Notice);
                    }
                    break;

                case State.Staggered:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) SetState(State.Notice);
                    break;

                case State.Stunned:
                    if (head != null) head.localPosition = headRestPos + Vector3.down * (0.42f * visualScale);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        if (head != null) head.localPosition = headRestPos;
                        SetState(State.Notice);
                    }
                    break;

                case State.Knockdown:
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        if (visual != null) visual.rotation = Quaternion.identity;
                        if (head != null) head.localPosition = headRestPos;
                        PartCenterOffset = Vector3.zero;
                        staggerPool = 0f;
                        knockdownPool = 0f;
                        SetState(State.Notice);
                    }
                    break;

                case State.EnrageRoar:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) SetState(State.Notice);
                    break;

                case State.Exhausted:
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        IsExhausted = false;
                        ApplyTint();
                        SetState(State.Notice);
                    }
                    break;

                case State.Retreat:
                    {
                        Vector3 away = -toPlayer.normalized;
                        Vector3 next = transform.position + away * Tuning.MossHorn.approach_speed_mps * dt;
                        if (InsideIsland(next)) transform.position = next;
                        FaceTowards(away, 5f, dt);
                        stateTimer -= dt;
                        if (stateTimer <= 0f) SetState(State.Notice);
                    }
                    break;
            }

            UpdateBar();
        }

        private float TellDuration()
        {
            float t = Tuning.MossHorn.tell_duration_s;
            if (IsEnraged) t *= Tuning.MossHorn.enrage_tell_mult;
            return t;
        }

        private void Wander(float dt)
        {
            wanderTimer -= dt;
            if (wanderTimer <= 0f)
            {
                wanderTimer = 3f + (TransactionId.GetHashCode() % 100) / 50f;
                float x = islandCenter.x + ((TransactionId.GetHashCode() % 997) / 997f - 0.5f) * 2f * (islandHalf.x - 3f);
                float z = islandCenter.z + (((TransactionId.GetHashCode() >> 4) % 991) / 991f - 0.5f) * 2f * (islandHalf.y - 3f);
                wanderTarget = new Vector3(x, groundY, z);
            }
            Vector3 to = wanderTarget - transform.position;
            to.y = 0f;
            if (to.magnitude > 0.4f)
            {
                transform.position += to.normalized * Tuning.MossHorn.wander_speed_mps * dt;
                FaceTowards(to, 4f, dt);
            }
        }

        private bool InsideIsland(Vector3 p)
        {
            return Mathf.Abs(p.x - islandCenter.x) < islandHalf.x - 1.1f
                && Mathf.Abs(p.z - islandCenter.z) < islandHalf.y - 1.1f;
        }

        private void FaceTowards(Vector3 dir, float rate, float dt)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            float target = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float y = Mathf.LerpAngle(transform.eulerAngles.y, target, 1f - Mathf.Exp(-rate * dt));
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }

        private void SetState(State next)
        {
            CurrentState = next;
        }

        private void UpdateBar()
        {
            if (barGroup == null) return;
            bool show = Hp < MaxHp && !IsDefeated;
            barGroup.gameObject.SetActive(show);
            if (!show) return;
            float frac = Mathf.Clamp01(Hp / MaxHp);
            barFill.localScale = new Vector3(2.1f * frac, 0.12f, 0.08f);
            barFill.localPosition = new Vector3(-1.05f + 1.05f * frac, 0f, 0f);
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - barGroup.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.01f)
                    barGroup.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }
        }

        private void ApplyTint()
        {
            if (bodyMaterial == null) return;
            if (IsEnraged) bodyMaterial.color = bodyBaseColor * new Color(1.25f, 0.75f, 0.7f);
            else if (IsExhausted) bodyMaterial.color = bodyBaseColor * new Color(1.15f, 1.15f, 1.1f);
            else bodyMaterial.color = bodyBaseColor;
            if (tailMaterial != null && !Hurtboxes.Get(PartId.Tail).Severed)
                tailMaterial.color = bodyMaterial.color * 0.9f;
        }

        // MECH-009 damage entry point. The pipeline has already applied the
        // part multiplier, crit, vulnerable state multiplier, and elder
        // reduction to r.Total. This method owns the consequences: part
        // records, the reaction pools, enrage and exhaustion, and death.
        // Returns 1 when the hit breaks a part, 2 when it severs the tail.
        public int ApplyDamage(DamageResult r, DamageKind kind)
        {
            if (IsDefeated) return 0;
            EnsurePlayer();

            HurtboxPart part = Hurtboxes.Get(r.Part);
            int outcome = Hurtboxes.RecordHit(part, r, kind);
            if (outcome == 2) DropTail();

            Hp -= r.Total;

            float stag = r.StaggerDealt;
            if (CurrentState == State.Threaten) stag *= 1.5f;
            staggerPool += stag;
            knockdownPool += stag;
            stunPool += r.StunDealt;
            aggroedByDamage = true;

            if (Hp <= 0f)
            {
                Hp = 0f;
                Defeat();
                return outcome;
            }

            // Enrage begins the moment health crosses the line, whatever
            // else the hit would have caused.
            if (!enragedOnce && Hp <= MaxHp * Tuning.MossHorn.enrage_hp_frac)
            {
                BeginEnrage();
                return outcome;
            }
            if (IsEnraged && Hp <= MaxHp * Tuning.MossHorn.exhaust_hp_frac)
            {
                BeginExhaustion();
                return outcome;
            }

            if (knockdownPool >= Tuning.MossHorn.stagger_threshold * Tuning.MossHorn.knockdown_pool_mult
                && CurrentState != State.Knockdown && CurrentState != State.Stunned)
            {
                EnterKnockdown();
            }
            else if (stunPool >= Tuning.MossHorn.stun_threshold
                && CurrentState != State.Stunned && CurrentState != State.Knockdown)
            {
                stunPool = 0f;
                stateTimer = Tuning.MossHorn.stun_duration_s;
                SetState(State.Stunned);
            }
            else if (staggerPool >= Tuning.MossHorn.stagger_threshold
                && CurrentState != State.Staggered && CurrentState != State.Knockdown && CurrentState != State.Stunned)
            {
                staggerPool = 0f;
                stateTimer = Tuning.MossHorn.stagger_duration_s;
                SetState(State.Staggered);
            }
            else if (outcome == 1 && CurrentState != State.Knockdown && CurrentState != State.Stunned && CurrentState != State.Staggered)
            {
                // A break always flinches, even below the damage threshold.
                stateTimer = Tuning.MossHorn.flinch_duration_s;
                flinchReadyAt = Time.time + Tuning.MossHorn.flinch_cooldown_s;
                SetState(State.Flinch);
            }
            else if (r.Total >= Tuning.MossHorn.flinch_damage && Time.time >= flinchReadyAt
                && (CurrentState == State.Graze || CurrentState == State.Notice
                    || CurrentState == State.Approach || CurrentState == State.Recover))
            {
                stateTimer = Tuning.MossHorn.flinch_duration_s;
                flinchReadyAt = Time.time + Tuning.MossHorn.flinch_cooldown_s;
                SetState(State.Flinch);
            }
            else if (!IsElder && !retreatedOnce && !IsEnraged
                && Hp <= MaxHp * Tuning.MossHorn.retreat_hp_frac
                && (CurrentState == State.Notice || CurrentState == State.Recover || CurrentState == State.Approach))
            {
                retreatedOnce = true;
                stateTimer = Tuning.MossHorn.retreat_duration_s;
                SetState(State.Retreat);
            }
            else if (CurrentState == State.Graze)
            {
                SetState(State.Notice);
            }
            UpdateBar();
            return outcome;
        }

        // Legacy MECH-003 entry point, kept as a shim for any caller that
        // still speaks raw damage: it routes a torso hit through the same
        // consequences with no part multiplier.
        public void TakeHit(float damage, float stagger, bool isSkill)
        {
            if (IsDefeated || Hurtboxes == null) return;
            var r = new DamageResult
            {
                Target = this,
                Part = PartId.Torso,
                WeaponId = "legacy",
                StepName = isSkill ? "Wind Spear" : "Strike",
                Total = damage,
                StaggerDealt = stagger,
                HitPoint = Center,
                Phase = "Legacy",
            };
            ApplyDamage(r, DamageKind.Cut);
        }

        private void BeginEnrage()
        {
            enragedOnce = true;
            IsEnraged = true;
            enrageTimer = Tuning.MossHorn.enrage_duration_s;
            stateTimer = Tuning.MossHorn.enrage_roar_s;
            if (head != null) head.localPosition = headRestPos;
            ApplyTint();
            SetState(State.EnrageRoar);
        }

        private void BeginExhaustion()
        {
            if (exhaustedOnce) { IsEnraged = false; ApplyTint(); return; }
            exhaustedOnce = true;
            IsEnraged = false;
            IsExhausted = true;
            stateTimer = Tuning.MossHorn.exhaust_duration_s;
            ApplyTint();
            SetState(State.Exhausted);
        }

        private void EnterKnockdown()
        {
            knockdownPool = 0f;
            staggerPool = 0f;
            if (visual != null) visual.rotation = Quaternion.Euler(0f, 0f, 78f);
            PartCenterOffset = new Vector3(0f, -0.45f, 0f);
            stateTimer = Tuning.MossHorn.knockdown_duration_s;
            SetState(State.Knockdown);
        }

        private void DropTail()
        {
            if (tailVisual == null) return;
            tailVisual.SetParent(null);
            tailVisual.position = new Vector3(tailVisual.position.x, groundY + 0.15f, tailVisual.position.z);
            tailVisual.rotation = Quaternion.Euler(70f, 0f, 20f);
            if (tailMaterial != null) tailMaterial.color *= 0.6f;
        }

        // Sandbox training monsters carry the elder sized health pool and
        // reset themselves after a defeat (handled in Update).
        public void SetTraining()
        {
            TrainingMode = true;
            MaxHp = Tuning.MossHorn.training_hp;
            Hp = MaxHp;
        }

        public void ApplyHitstop(float seconds)
        {
            hitstopTimer = Mathf.Max(hitstopTimer, seconds);
        }

        private void Defeat()
        {
            SetState(State.Defeated);
            defeatTimer = 0f;
            if (visual != null) visual.rotation = Quaternion.Euler(0f, 0f, 78f);
            if (bodyMaterial != null) bodyMaterial.color *= 0.55f;
            if (barGroup != null) barGroup.gameObject.SetActive(false);
            if (LootBank.Instance != null) LootBank.Instance.RegisterKill();
        }

        public void MarkHarvested()
        {
            IsHarvested = true;
            if (visual != null) visual.position += Vector3.down * 0.35f;
            if (bodyMaterial != null) bodyMaterial.color *= 0.6f;
        }

        // Debug and sandbox controls.
        public void ForceStagger()
        {
            if (IsDefeated) return;
            staggerPool = 0f;
            stateTimer = Tuning.MossHorn.stagger_duration_s;
            SetState(State.Staggered);
        }

        public void ForceKnockdown()
        {
            if (IsDefeated) return;
            EnterKnockdown();
        }

        public void ForceStun()
        {
            if (IsDefeated) return;
            stunPool = 0f;
            stateTimer = Tuning.MossHorn.stun_duration_s;
            SetState(State.Stunned);
        }

        public void ForceEnrage()
        {
            if (IsDefeated || enragedOnce) return;
            BeginEnrage();
        }

        // Full reset for the sandbox and the debug panel: health, pools,
        // parts, enrage flags, tail, tint, position, and state.
        public void ResetFight()
        {
            Hp = MaxHp;
            staggerPool = 0f;
            stunPool = 0f;
            knockdownPool = 0f;
            hitstopTimer = 0f;
            aggroedByDamage = false;
            slamPending = false;
            enragedOnce = false;
            exhaustedOnce = false;
            retreatedOnce = false;
            IsEnraged = false;
            IsExhausted = false;
            defeatTimer = 0f;
            PartCenterOffset = Vector3.zero;
            transform.position = homePos;
            if (visual != null)
            {
                visual.rotation = Quaternion.identity;
                visual.position = transform.position;
            }
            if (head != null) head.localPosition = headRestPos;
            Vector3 toCenter = new Vector3(islandCenter.x - transform.position.x, 0f, islandCenter.z - transform.position.z);
            if (toCenter.sqrMagnitude > 0.25f)
                transform.rotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up);
            if (Hurtboxes != null) Hurtboxes.ResetParts();
            if (tailVisual != null && visual != null)
            {
                tailVisual.SetParent(visual, false);
                tailVisual.localPosition = new Vector3(0f, 1.15f * visualScale, -1.5f * visualScale);
                tailVisual.localRotation = Quaternion.identity;
                tailVisual.localScale = new Vector3(0.28f * visualScale, 0.28f * visualScale, 0.9f * visualScale);
            }
            ApplyTint();
            SetState(State.Graze);
            UpdateBar();
        }
    }
}
