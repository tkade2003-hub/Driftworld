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
