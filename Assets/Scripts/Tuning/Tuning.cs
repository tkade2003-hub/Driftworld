namespace Driftworld
{
    // The single tuning source for the slice. Every gameplay number in the
    // specs (design/gdd/) lives here under its spec name. Gameplay code reads
    // these values and never carries literals of its own; the spec code
    // audits grep for exactly that. Groups match the spec groups.
    public static class Tuning
    {
        // MECH-001 Drag Movement (group Movement)
        public static class Movement
        {
            public const float deadzone_px = 12f;        // px at 1080p width, range 8 to 20
            public const float max_drag_px = 90f;        // px at 1080p width, range 60 to 130
            public const float move_speed_mps = 4.2f;    // m per s, range 3.4 to 5.2
            public const float accel_time_s = 0.08f;     // s, range 0.04 to 0.15
            public const float stop_time_s = 0.06f;      // s, range 0.03 to 0.12
            public const float ripple_fade_s = 0.25f;    // s, range 0.15 to 0.4
        }

        // MECH-002 Glide and Wind Lanes (group Glide)
        public static class Glide
        {
            public const float glide_speed_mps = 9f;       // m per s, range 7 to 12
            public const float sink_rate_mps = 1.6f;       // m per s, range 1.2 to 2.2
            public const float turn_rate_deg_s = 90f;      // deg per s, range 60 to 130
            public const float lane_lift_mps = 3.5f;       // m per s, range 2.5 to 5
            public const float lane_speed_mult = 1.3f;     // ratio, range 1.15 to 1.5
            public const float boost_gauge_max = 100f;     // points, fixed
            public const float boost_cost = 40f;           // points, range 30 to 50
            public const float boost_speed_mult = 1.5f;    // ratio, range 1.3 to 1.7
            public const float boost_duration_s = 1.2f;    // s, range 0.8 to 1.6
            public const float boost_charge_per_s = 25f;   // points per s in a lane, range 15 to 40
            public const float deploy_delay_s = 0.2f;      // s, airborne and falling before the glider opens
        }

        // MECH-003 Spear Combat (group Combat)
        public static class Combat
        {
            public const float hero_health_max = 100f;      // points, fixed for the slice
            public const float strike_damage = 12f;         // points, range 8 to 18
            public const float strike_rate_s = 0.45f;       // s, range 0.35 to 0.6
            public const float strike_range_m = 2.3f;       // m, greybox reach of the spear
            public const float heavy_damage_mult = 2.2f;    // ratio, range 1.8 to 2.8
            public const float heavy_stamina_cost = 30f;    // points, range 20 to 40
            public const float heavy_charge_s = 0.3f;       // s of hold before a release is a heavy
            public const float stamina_max = 100f;          // points, fixed
            public const float stamina_regen_per_s = 18f;   // points per s, range 12 to 26
            public const float stamina_regen_delay_s = 0.8f;// s, range 0.5 to 1.2
            public const float guard_damage_mult = 0.3f;    // ratio, range 0.2 to 0.45
            public const float skill_damage = 45f;          // points, range 35 to 60
            public const float skill_cooldown_s = 8f;       // s, range 6 to 12
            public const float skill_speed_mps = 26f;       // m per s, greybox projectile speed
            public const float skill_range_m = 26f;          // m, expires at max range
            public const float hitstop_ms = 45f;            // ms, range 25 to 70, heavy connects only
            public const float input_buffer_s = 0.12f;      // s, range 0.08 to 0.18
            public const float hitstun_s = 0.25f;           // s, greybox recovery after a hit taken
            // MECH-009 additions.
            public const float crit_chance = 0.25f;         // ratio, range 0.1 to 0.4
            public const float crit_mult = 1.5f;            // ratio applied to the physical part, range 1.3 to 1.8
            public const float knockback_mps = 4.5f;        // m per s shove on an unguarded hit taken (was a literal in PlayerCombat)
            public const float combo_window_s = 1.2f;       // s after a combo step ends to chain the next tap
            public const float combo_cancel_frac = 0.45f;   // fraction of recovery elapsed before a buffered tap starts the next step
            public const float attack_turn_rate_deg_s = 720f; // deg per s the hero may rotate toward the aim target during startup
        }

        // MECH-009 Dodge (group Dodge). Timing defaults follow the soulslike
        // reference table recorded in design/combat/audit-and-sources.md.
        public static class Dodge
        {
            public const float stamina_cost = 20f;          // points, range 15 to 30
            public const float duration_s = 0.42f;          // s, range 0.35 to 0.6
            public const float iframe_start_s = 0.04f;      // s into the dodge when invulnerability opens
            public const float iframe_end_s = 0.34f;        // s into the dodge when invulnerability closes
            public const float distance_m = 2.7f;           // m covered by one dodge, range 2.2 to 3.4
            public const float cooldown_s = 0.12f;          // s after a dodge before another action
        }

        // MECH-009 Lock On (group LockOn)
        public static class LockOn
        {
            public const float max_range_m = 30f;           // m, a lock drops beyond this
            public const float acquire_range_m = 22f;       // m, a lock may be taken inside this
            public const float aim_assist_bonus_m = 1.2f;   // m added to step range when picking the aim target
        }

        // MECH-009 Weapons (group Weapons). Base power and stagger per class;
        // per move multipliers live in WeaponCatalog, built from these.
        public static class Weapons
        {
            public const float longsword_power = 12f;       // matches MECH-003 strike_damage so slice balance holds
            public const float longsword_stagger = 8f;      // matches MECH-004 stagger_strike
            public const float longsword_wind = 5f;         // wind element power
            public const float greatsword_power = 22f;
            public const float greatsword_stagger = 16f;
            public const float swordshield_power = 10f;
            public const float swordshield_stagger = 6f;
            public const float swordshield_guard_mult = 0.15f; // best guard in the set
            public const float dualblades_power = 7f;       // per hit; steps land several hits
            public const float dualblades_stagger = 4f;     // per hit
            public const float dualblades_wind = 3f;       // wind element power
            public const float hammer_power = 16f;
            public const float hammer_stagger = 14f;
        }

        // MECH-009 Monster Parts (group Parts). Damage multipliers and the
        // part damage pools at which parts break or sever. Offsets and radii
        // are authored in Hurtbox, scaled by the creature visual scale.
        public static class Parts
        {
            public const float head_phys = 1.5f;
            public const float head_elem = 1.3f;
            public const float head_break = 60f;
            public const float neck_phys = 1.2f;
            public const float neck_elem = 1.1f;
            public const float neck_break = 80f;
            public const float torso_phys = 1.0f;
            public const float torso_elem = 1.0f;
            public const float torso_break = 120f;
            public const float foreleg_phys = 0.8f;
            public const float foreleg_elem = 0.9f;
            public const float foreleg_break = 70f;
            public const float hindleg_phys = 0.8f;
            public const float hindleg_elem = 0.9f;
            public const float hindleg_break = 70f;
            public const float tail_phys = 1.1f;
            public const float tail_elem = 1.0f;
            public const float tail_sever = 50f;            // Cut damage only
            public const float broken_bonus = 0.25f;        // extra phys multiplier fraction on a broken part
        }

        // MECH-004 The Moss Horn (group MossHorn)
        public static class MossHorn
        {
            public const float hp_common = 260f;            // points, range 200 to 340
            public const float hp_elder = 1400f;            // points, range 1100 to 1800
            public const float charge_damage = 18f;         // points, range 12 to 26
            public const float slam_damage_elder = 26f;     // points, range 18 to 34
            public const float tell_duration_s = 0.9f;      // s, range 0.7 to 1.2, never below 0.7
            public const float slam_tell_s = 0.6f;          // s, elder slam tell
            public const float charge_speed_mps = 11f;      // m per s, range 9 to 14
            public const float charge_max_m = 16f;          // m, greybox charge length cap
            public const float recover_s = 1.1f;            // s, greybox recovery after a charge
            public const float stagger_threshold = 100f;    // points, range 80 to 130
            public const float stagger_strike = 8f;         // points per strike
            public const float stagger_heavy = 22f;         // points per heavy strike
            public const float stagger_skill = 30f;         // points per wind spear (wind weakness)
            public const float stagger_damage_mult = 1.5f;  // ratio, range 1.3 to 1.8
            public const float stagger_duration_s = 3.5f;   // s, range 2.5 to 4.5
            public const float notice_range_m = 14f;        // m, range 10 to 18
            public const float aggro_range_m = 10f;         // m, greybox range where notice becomes threaten
            // MECH-009 additions. Values marked "was a literal" were swept
            // out of MossHorn and PlayerCombat during the combat build.
            public const float approach_speed_mps = 2.6f;   // m per s closing on the player, range 2 to 3.4
            public const float wander_speed_mps = 1.1f;     // m per s (was a literal in MossHorn.Wander)
            public const float charge_hit_distance_m = 1.9f;// m, charge connects inside this (was a literal)
            public const float slam_trigger_range_m = 3.4f; // m, elder starts the slam tell inside this (was a literal)
            public const float slam_range_m = 3.6f;         // m, the slam lands inside this (was a literal)
            public const float flinch_damage = 26f;         // points, a single hit at or above this flinches a non committed monster
            public const float flinch_duration_s = 0.35f;   // s
            public const float flinch_cooldown_s = 1.6f;    // s between flinches
            public const float stun_threshold = 60f;        // stun points, head hits only
            public const float stun_duration_s = 4f;        // s
            public const float stun_head_factor = 0.6f;     // stun gained per stagger point dealt to the head, before the step stun multiplier
            public const float knockdown_pool_mult = 2f;    // knockdown threshold is the stagger threshold times this
            public const float knockdown_duration_s = 4f;   // s on the ground
            public const float enrage_hp_frac = 0.5f;       // health fraction that triggers enrage
            public const float enrage_duration_s = 45f;     // s
            public const float enrage_roar_s = 0.9f;        // s, the roar pause when enrage begins
            public const float enrage_charge_speed_mult = 1.15f;
            public const float enrage_tell_mult = 0.85f;    // tell 0.9 times 0.85 stays above the 0.7 floor
            public const float enrage_recover_mult = 0.8f;
            public const float exhaust_hp_frac = 0.25f;     // health fraction that ends enrage in exhaustion
            public const float exhaust_duration_s = 6f;     // s
            public const float exhaust_recover_mult = 1.6f;
            public const float retreat_hp_frac = 0.2f;      // common only, once per fight
            public const float retreat_duration_s = 2.5f;   // s
            public const float damage_reduction_elder = 0.1f; // ratio of damage the elder shrugs off
            public const float training_hp = 1400f;         // sandbox training monster health pool
        }

        // MECH-005 Harvest (group Harvest)
        public static class Harvest
        {
            public const float harvest_time_s = 1.6f;       // s, range 1.2 to 2.2
            public const float harvest_range_m = 2.8f;      // m, greybox prompt range
            public const float horn_rate_pct = 35f;         // percent, range 25 to 45
            public const int pity_max = 3;                  // misses before a guaranteed horn
            public const float loot_flight_s = 0.7f;        // s, range 0.5 to 0.9
        }

        // MECH-008 Camera (group Camera). Follow basics ship with the slice
        // scaffold; the full framing rules land with the MECH-008 build step.
        public static class Camera
        {
            public const float follow_distance_m = 6.5f;   // m, range 5 to 8
            public const float follow_height_m = 3.2f;     // m, range 2.4 to 4
            public const float glide_distance_m = 9f;      // m, range 7.5 to 11
            public const float fov_deg = 60f;              // deg, range 55 to 65
            public const float fov_boost_deg = 6f;         // deg, range 3 to 9
            public const float return_delay_s = 2.5f;      // s, range 1.8 to 3.5
            public const float nudge_return_s = 0.8f;      // s, range 0.5 to 1.2
            // MECH-009 lock on framing.
            public const float lock_distance_bonus_m = 3f; // m, max extra pull back while locked
            public const float lock_distance_factor = 0.2f;// ratio of hero to target distance added to the pull back
        }
    }
}
