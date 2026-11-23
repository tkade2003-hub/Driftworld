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
            public const float deploy_delay_s = 0.2f;      // s airborne and falling before the glider opens
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
            public const float skill_range_m = 26f;         // m, expires at max range
            public const float hitstop_ms = 45f;            // ms, range 25 to 70, heavy connects only
            public const float input_buffer_s = 0.12f;      // s, range 0.08 to 0.18
            public const float hitstun_s = 0.25f;           // s, greybox recovery after a hit taken
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
            public const float stagger_duration_s = 3.5f;   // s, range 2.5 to 4.5
            public const float stagger_damage_mult = 1.5f;  // ratio, range 1.3 to 1.8
            public const float notice_range_m = 14f;        // m, range 10 to 18
            public const float aggro_range_m = 10f;         // m, greybox range where notice becomes threaten
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
        }
    }
}
