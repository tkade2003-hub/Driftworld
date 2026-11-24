# Driftworld Stage 1A Audit

Read only inspection performed 2026-11-24. No Unity build or editor run was attempted. Unity version confirmed from ProjectSettings/ProjectVersion.txt as 6000.0.23f1.

## Checkpoint

| Item | Value |
|---|---|
| Local checkpoint path | ~/workspace/driftworld-backups/checkpoint-2026-11-24-pre-combat/ |
| Contents copied | Assets, Packages, ProjectSettings |
| Excluded | Library, Logs, builds, UserSettings, loose log files at project root |
| Size on disk | 400K total (Assets 280K, ProjectSettings 112K, Packages 8.0K) |
| File count | 75 files |
| Verification | diff -r against the source for each of the three folders returned no differences |
| Copy note | cp reported ownership preservation warnings on folders only. File contents verified identical by diff after the copy. |

The local project at ~/workspace/driftworld has no .git directory. The remote repo tkade2003-hub/Driftworld at HEAD f395d8d is the canonical checkpoint. The folder copy above is the local safety net only.

## Engine and pipeline

| Question | Finding | Evidence |
|---|---|---|
| Unity version | 6000.0.23f1 (revision 1c4764c07fb4) | ProjectSettings/ProjectVersion.txt |
| Render pipeline | Built in Render Pipeline. Not URP. Not HDRP. | Packages/manifest.json has no render pipeline package. ProjectSettings/GraphicsSettings.asset has m_CustomRenderPipeline fileID 0 and an empty m_RenderPipelineGlobalSettingsMap. |
| Rendering path | m_DefaultRenderingPath 1 and m_DefaultMobileRenderingPath 1 in GraphicsSettings.asset | GraphicsSettings.asset |
| Strip Engine Code | stripEngineCode 0 in ProjectSettings/ProjectSettings.asset. Confirmed and not changed. | ProjectSettings.asset |
| Input handling mode | activeInputHandler 0, meaning the legacy Input Manager only | ProjectSettings.asset |
| Scripting and API level | apiCompatibilityLevel 6. scriptingBackend empty per platform in the file read. | ProjectSettings.asset |
| Materials and shaders | All gameplay materials are created at runtime by cloning Assets/Resources/Greybox/White.mat, a Standard shader material. Diag.SafeMaterial in Diagnostics.cs is the single factory, with Shader.Find("Standard") and a primitive default material as fallbacks. | Diagnostics.cs, Bootstrap.cs, BuildSetup.EnsureGreyboxMaterials |
| Lighting in scene | Scene RenderSettings plus a code built directional Sun in Bootstrap.MakeSun (intensity 1.15, no shadows) and a scene Directional Light from the default scene template | Main.unity, Bootstrap.cs |

Pipeline verdict: plain Built in pipeline with legacy input, uGUI, and IMGUI diagnostics. Any new combat work that assumes URP, Cinemachine, the new Input System, or Animation Rigging would require adding packages first. Nothing in the current code needs any of them.

## Packages

Full contents of Packages/manifest.json, read verbatim on 2026-11-24:

```json
{"dependencies":{"com.unity.modules.animation":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.imageconversion":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.jsonserialize":"1.0.0","com.unity.modules.particlesystem":"1.0.0","com.unity.modules.physics":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.uielements":"1.0.0","com.unity.ugui":"2.0.0"}}
```

| Package | Version | Notes |
|---|---|---|
| com.unity.modules.animation | 1.0.0 | Built in module. No Animator use found in gameplay scripts. |
| com.unity.modules.audio | 1.0.0 | Built in module. AudioListener only, on the scene camera and in Bootstrap fallback camera creation. |
| com.unity.modules.imageconversion | 1.0.0 | Built in module. |
| com.unity.modules.imgui | 1.0.0 | Built in module. Used by DiagOverlay OnGUI. |
| com.unity.modules.jsonserialize | 1.0.0 | Built in module. |
| com.unity.modules.particlesystem | 1.0.0 | Built in module. No ParticleSystem use found in gameplay scripts. |
| com.unity.modules.physics | 1.0.0 | Built in module. Load bearing. See Physics dependencies. |
| com.unity.modules.ui | 1.0.0 | Built in module. |
| com.unity.modules.uielements | 1.0.0 | Built in module. No UIElements use found in gameplay scripts. |
| com.unity.ugui | 2.0.0 | The only non module package. Canvas, CanvasScaler, GraphicRaycaster, Image, Text, EventSystem and StandaloneInputModule all come from here. |

Specifically checked and absent:

| Package sought | Present |
|---|---|
| Universal Render Pipeline (com.unity.render-pipelines.universal) | No |
| Cinemachine (com.unity.cinemachine) | No |
| Input System (com.unity.inputsystem) | No |
| Animation Rigging (com.unity.animation.rigging) | No |
| TextMeshPro as a separate package entry | No entry in manifest. HUD text uses UnityEngine.UI Text with the LegacyRuntime.ttf builtin font. |

## Script inventory

All scripts live in namespace Driftworld except the two editor files, which live in Driftworld.EditorTools. Line counts are wc -l counts taken on 2026-11-24. Total 2613 lines across 17 files.

### Assets/Scripts/Core/Bootstrap.cs (272 lines)

Purpose: Static greybox world builder. Builds the entire playable world in code after a scene loads.

Key API and behaviour: [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] Build() with a guard that returns early if a PlayerController already exists. Private builders MakeSun, MakeIslandA, MakeIslandB, MakeTree, MakeWindLane, MakeHero, MakeCreatures, MakeCamera, MakeRippleCanvas, MakeCombatHud, plus helpers Box and Mat. Hero spawn at (8, 0.3, 0) facing west (Euler y -90). Island A centred at origin, size 30 by 2 by 30, top at y 0. Island B centred at (58, -7, 0), size 22 by 2 by 22, top at y -6. Wind lane at (31, -1, 0) rotated 90 degrees, size (14, 12, 44). Five MossHorn creatures placed by MakeCreatures, three on Island A and a common plus the elder on Island B, all placed outside aggro range of spawn after a 2026-11-23 fix recorded in comments. MakeHero adds CharacterController (height 1.8, radius 0.35, centre y 0.9), DragInput, PlayerController, PlayerCombat, Harvester and LootBank to one Hero GameObject, and builds capsule visual and glider wing visual with their primitive colliders destroyed. MakeCombatHud creates an EventSystem with StandaloneInputModule if none exists, then wires CombatHud buttons into PlayerCombat public fields and Harvester.

Dependencies: PlayerController, PlayerCombat, Harvester, LootBank, MossHorn, WindLane, FollowCamera, CombatHud, TouchRipple, DragInput, Diag, Tuning indirectly through the components it creates. UnityEngine.EventSystems and UnityEngine.UI for the EventSystem.

### Assets/Scripts/Core/Diagnostics.cs (94 lines)

Purpose: Build diagnostics that survive uGUI or material failures. Contains two types.

Key API: Static class Diag with public string fields Step, Error, Note, Status, CamStatus, methods SetStep, SetNote, Fail(Exception), and SafeMaterial(Color) which clones the Resources Greybox White material. Class DiagOverlay is a MonoBehaviour created BeforeSceneLoad via RuntimeInitializeOnLoadMethod, marked DontDestroyOnLoad, and draws step, note and error with IMGUI GUI.Label in OnGUI at the top left of the screen.

Dependencies: UnityEngine only, plus Resources/Greybox/White.mat at runtime. PlayerController writes Diag.Status, FollowCamera writes Diag.CamStatus, CombatHud reads all Diag fields, Bootstrap and most visual builders call Diag.SafeMaterial.

### Assets/Scripts/Tuning/Tuning.cs (104 lines)

Purpose: The single tuning source for the slice. Static class Tuning with nested static groups Movement, Glide, Combat, MossHorn, Harvest and Camera. Full contents reproduced in the Tuning values section below. Every gameplay script reads it and, per its own header comment, gameplay code should carry no tuning literals of its own. Note that a small number of literals do exist outside it in practice, for example gravity 22 in PlayerController, knockback 4.5 and step in 0.3 in PlayerCombat, and charge hit distance 1.9 in MossHorn.

### Assets/Scripts/Input/DragInput.cs (133 lines)

Purpose: MECH-001 drag movement input plus the right half tap context action.

Key API: Properties Active, Anchor, Current, DragPx, BeganThisFrame. Event Action OnRightTap. Method Vector2 MoveVector() returns a shaped direction with magnitude 0 to 1 using the Tuning deadzone and max drag, scaled by Screen.width / 1080. Private HandleTouches and HandleMouse. OnApplicationFocus clears all tracked input on focus loss.

Dependencies: Tuning.Movement, legacy UnityEngine.Input (Input.touchCount, Input.GetTouch, Input.GetMouseButtonDown, Input.GetMouseButton, Input.GetMouseButtonUp, Input.mousePosition), TouchPhase. Consumers are PlayerController (MoveVector and OnRightTap), TouchRipple (Active and Anchor) and Bootstrap (creation).

### Assets/Scripts/Player/PlayerController.cs (295 lines)

Purpose: Hero movement. MECH-001 ground movement and MECH-002 glide, boost and wind lane travel, plus rescue after a fall.

Key API: [RequireComponent(CharacterController)] and [RequireComponent(DragInput)]. Public fields gliderVisual and rescueY. Properties IsGliding, IsBoosting, BoostGauge, HeadingForward. Private state includes headingDeg, currentSpeed, verticalVelocity, airTime, boostTimer, lastGroundedPos and glideLockout. Update branches to UpdateGround, UpdateAir or UpdateGlide. EnterGlide requires airTime at or above Tuning.Glide.deploy_delay_s, falling velocity, and a drop of at least 1.5 m below the last grounded position. UpdateGlide exits on a downward Physics.Raycast hit within 1.8 m, on a stuck test (less than 0.4 m covered in 0.75 s), or on body.isGrounded. RescueToLastGround teleports to lastGroundedPos plus 0.5 m up when y falls below rescueY, which Bootstrap sets to -30. Ground speed is halved while PlayerCombat.IsGuarding. CameraRelativeDirection maps the drag vector through Camera.main.

Dependencies: DragInput, PlayerCombat, WindLane (static LaneContaining), Tuning.Movement, Tuning.Glide, CharacterController, Physics.Raycast, Diag.Status. Gravity is a private constant GravityMps2 of 22, not a Tuning value.

### Assets/Scripts/Camera/FollowCamera.cs (62 lines)

Purpose: MECH-008 follow basics. The camera trails the hero heading, pulls back in glide, and widens field of view on boost.

Key API: [RequireComponent(Camera)]. Public fields target (Transform) and controller (PlayerController). LateUpdate lerps yaw toward target forward with a first frame snap, lerps distance between Tuning.Camera.follow_distance_m and glide_distance_m, positions the camera behind and above the target, calls LookAt at target plus 1.4 m up, and lerps field of view toward Tuning.Camera.fov_deg plus fov_boost_deg while boosting. Writes Diag.CamStatus every frame.

Dependencies: PlayerController (IsGliding, IsBoosting), Tuning.Camera, Diag. There is no camera collision, no offset input, and no target framing other than the hero. No Cinemachine anywhere.

### Assets/Scripts/World/WindLane.cs (45 lines)

Purpose: MECH-002 wind lane volume between islands.

Key API: Public Vector3 size. Static registry All with OnEnable and OnDisable registration. Contains(Vector3) does a manual local box test via InverseTransformPoint. Static LaneContaining(Vector3) returns the first containing lane or null. OnDrawGizmos draws the volume in the editor.

Dependencies: UnityEngine and System.Collections.Generic only. Consumer is PlayerController.UpdateGlide. Deliberately uses no physics triggers, per its header comment.

### Assets/Scripts/Combat/PlayerCombat.cs (282 lines)

Purpose: MECH-003 spear combat on the hero. Tap strike, hold to charge heavy strike, guard, and the Wind Spear skill, with hitstop, hitstun and input buffering.

Key API: [RequireComponent(CharacterController)] and [RequireComponent(PlayerController)]. Public fields strikeButton, guardButton, skillButton (all HoldButton) and respawnPoint. Properties Health, Stamina, IsGuarding, IsDead, SkillCooldownRemaining, LastDamageAt. Public method TakeDamage(float amount, Vector3 sourcePos). Private DoStrike(bool heavy), DoSkill(), BestTarget(float range), Die(), and input helpers StrikeDown, StrikePressedEdge, StrikeReleasedEdge, GuardHeld, SkillPressed. Keyboard fallbacks are J for strike, K for guard, L for skill. Heavy strike requires stamina at or above Tuning.Combat.heavy_stamina_cost and otherwise falls back to a normal strike. Strike targeting is BestTarget over MossHorn.All within strike_range_m plus 0.9, then an 80 degree facing gate that only applies beyond 1.2 m. Strike moves the body 0.3 m along HeadingForward. Hitstop applies only on heavy connects, to both the hero timer and target.ApplyHitstop. During hitstun, a strike press is buffered for Tuning.Combat.input_buffer_s and fired on recovery. TakeDamage applies the guard multiplier, cancels a held charge, applies knockback of 4.5 away from the source and hitstun unless guarding, and is ignored while gliding or dead. Die respawns instantly at respawnPoint at full health and stamina.

Dependencies: PlayerController (HeadingForward, IsGliding), MossHorn (All, Center, TakeHit, ApplyHitstop), WindSpear (Launch), HoldButton, Tuning.Combat and Tuning.MossHorn stagger values, CharacterController, Diag.SafeMaterial for the skill projectile visual.

### Assets/Scripts/Combat/WindSpear.cs (52 lines)

Purpose: MECH-003 Wind Spear projectile.

Key API: Launch(Vector3 origin, Vector3 direction) sets position and velocity from Tuning.Combat.skill_speed_mps. Update moves in a straight line, accumulates travelled distance, and tests every living MossHorn by horizontal distance to its Center, hitting at under 1.5 m with Tuning.Combat.skill_damage and stagger_skill. Expires and destroys itself at Tuning.Combat.skill_range_m.

Dependencies: MossHorn, Tuning.Combat, Tuning.MossHorn. Explicitly no physics collisions, per its header comment. Its GameObject and sphere visual are created inside PlayerCombat.DoSkill, not in a prefab.

### Assets/Scripts/Combat/LootBank.cs (94 lines)

Purpose: MECH-005 loot ledger. One grant per creature transaction, with seeded reproducible drop rolls.

Key API: Static Instance set in Awake. Counters Shards, Hide, Meat, ElderHide, Horn, Kills, Harvests, ElderHornMisses. Event Action OnChanged. RegisterKill(). GrantHarvest(MossHorn creature, out string summary) returns false and grants nothing if the creature TransactionId is already in the granted set. Rolls use System.Random seeded 1337. Elder horn uses Tuning.Harvest.horn_rate_pct and the pity rule at Tuning.Harvest.pity_max misses.

Dependencies: MossHorn (TransactionId, IsElder), Tuning.Harvest. Consumers are Harvester, MossHorn.Defeat and CombatHud.

### Assets/Scripts/Combat/Harvester.cs (149 lines)

Purpose: MECH-005 harvest hold beside a defeated creature, plus the loot flight visual. Contains two types: Harvester and LootFlight.

Key API: Public fields harvestButton (HoldButton) and combat (PlayerCombat). Properties Nearby, Progress, LastGrantSummary, LastGrantAt. Update finds the nearest defeated and unharvested MossHorn within Tuning.Harvest.harvest_range_m, tracks a hold of Tuning.Harvest.harvest_time_s, cancels the hold if PlayerCombat.LastDamageAt advances after the hold started, and on completion calls LootBank.Instance.GrantHarvest, then target.MarkHarvested, then SpawnLootFlight. Keyboard fallback is H. LootFlight.Begin(Transform hero, float delaySeconds) flies one loot cube to the hero over Tuning.Harvest.loot_flight_s with an arc, then destroys itself.

Dependencies: MossHorn, LootBank, PlayerCombat, HoldButton, Tuning.Harvest, Diag.SafeMaterial.

### Assets/Scripts/Creatures/MossHorn.cs (375 lines)

Purpose: MECH-004 creature. The only creature in the game, in common and elder variants. This is already a full state machine, not a placeholder.

Key API: Static readonly List MossHorn.All. Public enum State with values Graze, Notice, Threaten, Charge, Recover, Staggered, Defeated, exposed as CurrentState. Configure(Vector3 pos, float ground, bool elder, Vector3 center, Vector2 half, int index) sets identity, TransactionId as mosshorn plus index, health from Tuning, builds the visual, and registers in All. Properties IsElder, IsHarvested, TransactionId, Hp, MaxHp, IsDefeated, Center (position plus 1.1 m up), DisplayName. Public methods TakeHit(float damage, float stagger, bool isSkill), ApplyHitstop(float seconds) and MarkHarvested(). The elder variant scales the visual by 1.45, uses hp_elder, and adds a horn slam after a shorter tell when the player is within 3.4 m. Every charge is preceded by the full Threaten tell, with head lowering and pawing motion. Charge moves at Tuning speed toward a locked chargeDir, stops at the island edge via InsideIsland, hits the player once per charge within 1.9 m, and caps at charge_max_m. Stagger accumulates from strikes, heavies and the skill, is multiplied by 1.5 for hits taken during Threaten, and triggers Staggered at stagger_threshold for stagger_duration_s with a damage multiplier while staggered. The health bar is two code built boxes above the creature that billboard toward Camera.main on the horizontal plane. Wander picks targets from a hash of TransactionId, which is deterministic but crude.

Dependencies: PlayerCombat (found via FindAnyObjectByType, TakeDamage), PlayerController reference held but only used through the player lookup, LootBank.Instance.RegisterKill, Tuning.MossHorn, Diag.SafeMaterial. Movement is direct transform.position writes. No CharacterController, no Rigidbody, no NavMesh, and no colliders on the creature at all, since every primitive collider in BuildVisual is destroyed.

### Assets/Scripts/UI/CombatHud.cs (244 lines)

Purpose: The combat HUD, built entirely in code. Hero health and stamina bars, loot counter, engaged creature bar and label, objective line, toast, three combat buttons, harvest panel, and the bottom left diagnostics readout.

Key API: Public HoldButton fields strikeButton, guardButton, skillButton, harvestButton, created inside Build. Bind(PlayerCombat, Harvester) stores the two gameplay references. Awake builds a ScreenSpaceOverlay Canvas with sortingOrder 10, CanvasScaler at reference resolution 1080 by 1920, and a GraphicRaycaster. Update reads PlayerCombat Health and Stamina, LootBank counters, MossHorn.All for the engaged creature (nearest damaged or non Graze creature within 30 m), Harvester Nearby and Progress and LastGrant fields, and writes the objective line from kill and harvest state. The diagnostics readout is a uGUI Text named through the local field diagText, anchored bottom left, updated every frame in Update with Diag.Step, Diag.Note, Diag.Error, Diag.Status and Diag.CamStatus.

Dependencies: PlayerCombat, Harvester, LootBank, MossHorn, Tuning.Combat, Diag, HoldButton, UnityEngine.UI. Font is Resources.GetBuiltinResource for LegacyRuntime.ttf.

### Assets/Scripts/UI/HoldButton.cs (68 lines)

Purpose: A uGUI button that reports press, hold and release edges, for strike charge, guard and harvest holds.

Key API: Implements IPointerDownHandler, IPointerUpHandler, IPointerExitHandler. Properties IsHeld, PressedThisFrame, ReleasedThisFrame, HeldSeconds, HeldSecondsAtRelease. Held time accumulates with Time.unscaledDeltaTime in Update. LateUpdate clears PressedThisFrame and, one frame later through a pending flag, ReleasedThisFrame. OnDisable clears IsHeld. Mouse clicks arrive through the same pointer handlers, so desktop WebGL works.

Dependencies: UnityEngine.EventSystems only. Consumers are PlayerCombat, Harvester and CombatHud. Note the frame timing subtlety: ReleasedThisFrame stays true through the Update of the frame after release because clearing happens in LateUpdate behind releasePending, and PlayerCombat relies on HeldSecondsAtRelease for the heavy threshold.

### Assets/Scripts/UI/TouchRipple.cs (73 lines)

Purpose: MECH-001 presentation. A soft ring drawn at the movement anchor while the drag is held. Nothing is drawn when the thumb is up, and nothing sits at a fixed position.

Key API: [RequireComponent(Image)]. Public field input (DragInput). Awake generates a 128 by 128 ring sprite in code with MakeRingSprite and sets raycastTarget false. Update positions the rect at DragInput.Anchor while Active, pulses scale slightly, and fades alpha toward 0.55 using Tuning.Movement.ripple_fade_s.

Dependencies: DragInput, Tuning.Movement, UnityEngine.UI.

### Assets/Editor/BuildSetup.cs (112 lines)

Purpose: Cloud build entry points and WebGL page patching. See the Build and publish pipeline section for the full account.

Key API: MenuItem Driftworld/Create Main Scene, EnsureScene, BuildWebGL, BuildIOS, EnsureGreyboxMaterials, private Build and PatchWebGLIndex. Namespace Driftworld.EditorTools. Editor only, never in a player build.

Dependencies: UnityEditor, UnityEditor.Build.Reporting, UnityEditor.SceneManagement, System.IO.

### Assets/Editor/PlayTest.cs (159 lines)

Purpose: Headless editor play test harness. Editor only, never ships.

Key API: [InitializeOnLoad] static class with Run() arming the test through SessionState, a Tick driven by EditorApplication.update, a 90 second failsafe, one log line per second for 6 seconds of untouched play, and a one shot Probe that reports island collider state, hero CharacterController state, a downward Physics.Raycast at the spawn column, Physics.CheckSphere at spawn, and Physics.GetIgnoreLayerCollision(0, 0). SessionState is used because entering play mode reloads the domain and wipes plain statics, as recorded in its header comment.

Dependencies: UnityEditor, PlayerController, Diag, Physics query APIs.

## Tuning values

Full contents of Assets/Scripts/Tuning/Tuning.cs, reproduced verbatim. This file is the single source of tuning truth for the slice.

```csharp
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
            public const float stagger_damage_mult = 1.5f;  // ratio, range 1.3 to 1.8
            public const float stagger_duration_s = 3.5f;   // s, range 2.5 to 4.5
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
```

Values the new combat work will most likely touch: Movement deadzone_px 12, max_drag_px 90, move_speed_mps 4.2. Combat stamina_max 100, stamina_regen_per_s 18, stamina_regen_delay_s 0.8, heavy_stamina_cost 30, strike_range_m 2.3, hitstun_s 0.25, input_buffer_s 0.12. Camera return_delay_s and nudge_return_s exist in Tuning but are not yet read by FollowCamera, which only reads follow_distance_m, follow_height_m, glide_distance_m, fov_deg and fov_boost_deg.

## Input and diagnostics

### How movement input works (MECH-001)

DragInput is the only movement input source. In Update it branches on Input.touchCount: above zero it runs HandleTouches, otherwise HandleMouse. Only one touch is tracked, by fingerId. A touch that begins on the left half of the screen becomes the movement anchor and sets Active, Anchor and Current. A touch that begins on the right half fires OnRightTap instead and is never tracked for movement. Mouse mirrors this exactly: left button down on the left half starts a drag, left button down on the right half fires OnRightTap, and mouse position updates Current while held. Focus loss clears the tracked touch, the mouse flag and Active.

PlayerController.Update calls DragInput.MoveVector once per frame. MoveVector returns zero inside the deadzone, then a normalized direction scaled by (magnitude minus deadzone) over (max drag minus deadzone), clamped to 1. Both pixel constants are scaled from their 1080p authored values by Screen.width / 1080. PlayerController turns that screen vector into a camera relative world direction using Camera.main forward and right flattened to the ground plane. Ground speed is Tuning.Movement.move_speed_mps times the vector magnitude, with acceleration shaped by accel_time_s and stop_time_s.

There is no fixed joystick object anywhere. Nothing is drawn at a fixed screen position for movement. The only movement visual is TouchRipple, a ring placed at the live anchor point while the drag is held, which fades out on release. CombatHud states the same rule in its header comment.

### How combat input works

Combat input does not pass through DragInput at all, except that OnRightTap is consumed by PlayerController for the glide boost. Combat buttons are uGUI HoldButton components on the CombatHud canvas, delivered through the EventSystem and StandaloneInputModule that Bootstrap creates, via IPointerDownHandler, IPointerUpHandler and IPointerExitHandler. Bootstrap assigns the HUD button instances into PlayerCombat.strikeButton, guardButton and skillButton, and into Harvester.harvestButton. PlayerCombat polls those buttons each frame in Update alongside keyboard fallbacks J, K and L. Harvester polls its button plus the H key. Guard is a held state. Strike is a press, hold and release pattern where release timing against heavy_charge_s decides normal versus heavy. Skill and harvest prompts are edge and hold reads respectively.

Two input risks follow from this split. First, a right half touch that lands on empty canvas fires OnRightTap, while a right half touch that lands on a button goes to the button through the GraphicRaycaster. The split is positional and component based, not unified. Second, DragInput tracks only the first qualifying touch, so a second left half touch during a movement drag is ignored, and a right half tap during a movement drag fires OnRightTap immediately. Any new dodge or lock on control placed on the left half, or any multi touch gesture, will collide with this single tracked touch model and needs a deliberate design decision before code.

### Diagnostics overlay and the inp readout

There are three connected diagnostics surfaces, all fed by the static Diag class in Diagnostics.cs.

1. DiagOverlay, in Diagnostics.cs. An IMGUI overlay created BeforeSceneLoad and kept alive with DontDestroyOnLoad. OnGUI draws one label at (12, 10) reading DRIFTWORLD DIAG step plus Diag.Step, an optional note label at (12, 38), and an optional error label at (12, 66). It draws step, note and error only. It does not draw Status or CamStatus.

2. Diag.Status, written by PlayerController.Update on a 0.25 second timer. The string reads hero position, gnd (CharacterController.isGrounded), glide, lg (last grounded position), inp, grd (grounded frame count), ge (glide entry count), xr (glide ray exit count), xs (stuck exit count), xg (grounded glide exit count), hit (last raycast hit object name), spd (current speed) and vv (vertical velocity). The inp field is lastInputMag, the magnitude of DragInput.MoveVector captured at the top of the same Update, formatted to two decimals. An inp of 1.00 means a full magnitude drag, 0.00 means no movement input or a drag inside the deadzone. FollowCamera separately writes Diag.CamStatus with its own position each LateUpdate.

3. CombatHud diagText, in CombatHud.cs. A uGUI Text anchored bottom left, size 24, updated every frame in CombatHud.Update with Diag.Step, Diag.Note, Diag.Error, Diag.Status and Diag.CamStatus concatenated. This is the surface that actually shows the inp readout in the shipped HUD. PlayTest also logs Diag.Status once per second in editor play tests, which is how the same readout appears in playtest log files.

Bootstrap drives Diag.Step through world settings, sun, island A, island B, wind lane, hero, creatures, camera, ripple canvas, combat hud and ready, and calls Diag.Fail on any exception so the failing step and error stay on screen.

## Physics dependencies

Shipped gameplay code depends on a narrow slice of Unity physics. There is no Rigidbody anywhere in Assets/Scripts or Assets/Editor. There is no joint, no PhysicMaterial, no collision callback (no OnCollisionEnter, OnTriggerEnter or equivalents), no trigger volume, and no NavMesh use. Combat and creature interaction are distance tests in plain code, by design.

| Dependency | Where | Load bearing behaviour |
|---|---|---|
| CharacterController | PlayerController, PlayerCombat, created in Bootstrap.MakeHero | All hero movement goes through body.Move. Grounding comes from body.isGrounded. Rescue and death toggle body.enabled to teleport. This is the class family at the centre of the earlier Strip Engine Code failure, where a shipped build lost a runtime only physics class and the hero fell through the world while local builds passed. stripEngineCode is 0 and must stay 0. |
| BoxCollider on primitive islands and blocks | Created implicitly by GameObject.CreatePrimitive in Bootstrap.Box and MakeTree, and kept on islands, ruin blocks, pillars, the chest and trunks | These are the only surfaces the CharacterController collides with and the only colliders the glide landing ray can hit. Canopy, hero visual and glider wing primitive colliders are explicitly destroyed. |
| Collider (base type) | Bootstrap, PlayerCombat.DoSkill, Harvester.SpawnLootFlight, MossHorn.BuildVisual | Used only as GetComponent<Collider>() followed by Object.Destroy, to strip the collider that CreatePrimitive adds to visual only objects. The type reference itself must survive any stripping or linking change. |
| Physics.Raycast | PlayerController.UpdateGlide | Downward ray from position plus 0.3 m up, length 1.8 m, used as the glide landing assist. Returns the hit collider name into the diagnostics readout. |
| RaycastHit | PlayerController.UpdateGlide | Carries the landing ray result and collider reference. |
| Physics.CheckSphere, Physics.GetIgnoreLayerCollision, Physics.Raycast | PlayTest.Probe only, editor only | Diagnostic queries at the spawn column. Not in any player build, but they show the intended probe for separating a missing collider from a CharacterController sweep failure. |
| GraphicRaycaster | CombatHud.Build | uGUI raycasting for the combat buttons. This is UI event raycasting, not 3D physics, but button input fails without it. |

Implication for new combat work: adding real hitboxes as colliders or triggers would be the first use of collision or trigger callbacks in the project, and the first new physics surface since the stripping incident. It would need a shipped build check that the hero still grounds, exactly as the project rules require, plus a PlayTest style probe for the new colliders. Distance based hit tests, in the style of BestTarget and WindSpear, carry no such risk and match every existing combat path.

## Build and publish pipeline

### Scenes and boot

One scene file exists: Assets/Scenes/Main.unity. ProjectSettings/EditorBuildSettings.asset lists it as the only enabled build scene, with guid 4e0fcc2b1df8442d0abeadc7e737eb17. The scene contains only the default template objects: a Main Camera with Camera and AudioListener at (0, 1, -10), and a Directional Light. It contains no Bootstrap object, no hero, and no gameplay component. Bootstrap is not in the scene at all.

Boot sequence: the scene loads with its camera and light, then Bootstrap.Build runs through RuntimeInitializeOnLoadMethod AfterSceneLoad, returns early if a PlayerController already exists, and otherwise constructs the sun, both islands, the wind lane, the hero with all gameplay components, five creatures, the follow camera behaviour on Camera.main, the ripple canvas, and the combat HUD with its EventSystem. DiagOverlay is created earlier, BeforeSceneLoad. BuildSetup.EnsureScene can recreate Main.unity as a default scene and register it in EditorBuildSettings if the file is ever missing, and BuildSetup.EnsureGreyboxMaterials recreates the Greybox White material if missing.

### BuildSetup.cs in detail

File: Assets/Editor/BuildSetup.cs, namespace Driftworld.EditorTools, static class, 112 lines.

| Member | Behaviour |
|---|---|
| CreateMainScene | Menu item under Driftworld. Calls EnsureScene. |
| EnsureScene | If Assets/Scenes/Main.unity does not exist, creates the Scenes folder, makes a new scene with DefaultGameObjects in Single mode, and saves it to that path. Always rewrites EditorBuildSettings.scenes to that single enabled scene. |
| BuildWebGL | The WebGL build entry point called by the cloud pipeline. Sets PlayerSettings.WebGL.compressionFormat to Disabled first, with a comment explaining that GitHub Pages sends no Content Encoding header for compressed files and the loader refuses them, then builds to builds/WebGL/WebGL, then calls PatchWebGLIndex on builds/WebGL/WebGL/index.html. Note that ProjectSettings.asset itself records webGLCompressionFormat 2, so the disabled setting exists only because this method sets it at build time. |
| PatchWebGLIndex | If the file exists and does not already contain the text touch-action, inserts one style element before the closing head tag and writes the file back, logging the patch. The inserted CSS is #unity-canvas { touch-action: none; } plus html, body { overscroll-behavior: none; }. The header comment records why: the default template never sets touch action on the canvas and the framework does not preventDefault drags, so a phone browser claims every drag as a scroll gesture and cancels it before the game sees it, which also starves the combat buttons of pointer events. Proven on 2026-11-23, per the comment, by simulated drags moving the hero in Chromium while the same build on a phone could not move him. The patch is skipped if the file is missing or already contains touch action anywhere. |
| BuildIOS | Builds to builds/iOS/iOS with no index patching. |
| EnsureGreyboxMaterials | If Assets/Resources/Greybox/White.mat cannot be loaded, creates the folder and a white Standard material asset there and saves assets. Called by every build. This forces the Standard shader into the build so runtime cloned materials do not render in the magenta error colour. |
| Build (private) | Calls EnsureScene and EnsureGreyboxMaterials, builds BuildPlayerOptions with the single scene, the target path, and BuildOptions.None through BuildPipeline.BuildPlayer, throws an exception naming the target and result if the result is not Succeeded, and logs success with the path. |

Per README.md, no machine on the project runs Unity locally. Builds are cloud builds through GitHub Actions and game ci, the WebGL lane publishes to GitHub Pages on pushes to main, and the iOS lane runs on demand and produces an Xcode project artifact. The workflow files themselves were outside the scope of this audit, which covered Assets, Packages and ProjectSettings only. PlayTest.cs is the headless editor harness described in the inventory.

## Reuse assessment

### Extend, do not replace

| System | Verdict | Reason |
|---|---|---|
| Tuning | Extend | Add new groups or values here for dodge, lock on and any new stamina costs. It is already read by every gameplay script and its header sets the no literals rule. |
| DragInput | Extend with care | MoveVector, the anchor model and OnRightTap are the movement contract for PlayerController, TouchRipple and the glide boost. New combat input should arrive through HoldButton style UI or through a new event on this class, not through a parallel input reader competing for touches. |
| PlayerController | Extend | Ground, air and glide movement, heading, rescue and the guard speed trade already live here, and combat already reads IsGliding and HeadingForward. Dodge belongs here or in a sibling component on the same hero object that moves through the same CharacterController, because two components calling body.Move in the same frame will fight. PlayerCombat already calls body.Move for its step in and knockback, which is a known and currently tolerated overlap to keep in mind. |
| PlayerCombat | Extend | Health, stamina with regen and regen delay, guard, hitstun, input buffering, hitstop, targeting and damage intake already exist and are wired to the HUD and to MossHorn. Dodge stamina cost, lock on target selection and hit reactions should build on TakeDamage, BestTarget and the stamina fields rather than a second combat component. |
| MossHorn state machine | Extend | Graze, Notice, Threaten, Charge, Recover, Staggered and Defeated already form a complete monster AI state machine with tells, stagger and an elder variant. A new monster AI should generalise or subclass this pattern and reuse TakeHit, ApplyHitstop and the All registry, not introduce a second creature framework. CombatHud, Harvester, PlayerCombat and WindSpear all iterate MossHorn.All directly, so any new creature type that is not a MossHorn will be invisible to targeting, harvesting and the HUD until those loops change. That is the single largest hidden coupling in the codebase. |
| MossHorn.All registry pattern | Reuse | WindLane.All with LaneContaining and MossHorn.All are the same static registry idiom. A lock on target list or a hitbox registry should follow it. |
| HoldButton | Reuse | Press, hold, release edges and held duration already solve tap versus hold for strike, guard, skill and harvest, on touch and mouse alike. A dodge button should be another HoldButton from CombatHud.MakeButton. |
| CombatHud | Extend | Bars, buttons, creature bar, objective line and toasts are built in one Build method with MakePanel, MakeText and MakeButton helpers. New stamina states, a lock on indicator and a dodge button belong in this build method. |
| FollowCamera | Extend | Yaw follow, glide pullback and boost field of view are here, and Tuning.Camera already carries unused return_delay_s and nudge_return_s values waiting for the full MECH-008 framing rules. Lock on framing belongs in this LateUpdate, not in a second camera script, and there is no Cinemachine to conflict with. |
| LootBank and Harvester | Leave alone | The transaction guard, pity rule and harvest hold are complete for the slice and orthogonal to combat feel work. |
| Diagnostics | Reuse during development | Any new state (dodge active, locked target name, stamina spent) can be appended to Diag.Status in the same style as inp, which immediately surfaces it in the HUD readout and in PlayTest logs. The file header says diagnostics come out once the loop is proven on a real device, so new permanent UI should not be built on Diag. |

### Feature by feature conflicts

Lock on: No lock on exists today. BestTarget in PlayerCombat picks the nearest living MossHorn in range with no facing test for the skill (22 m hardcoded, not a Tuning value) and only a partial facing gate for strikes. FollowCamera always frames behind the hero heading, so a locked target off heading will leave the frame. A lock on system needs one shared current target concept consumed by PlayerCombat targeting, FollowCamera framing and CombatHud creature bar, which today each compute their own nearest creature independently (CombatHud uses a 30 m search with a damaged or alerted filter). Unifying those three searches is the real work, not the button. Target loss rules also need care because creatures move by direct transform writes and can be at an island edge.

Dodge: No dodge exists today. The natural home is PlayerController movement through the CharacterController, with stamina cost and invulnerability timing in PlayerCombat, mirroring how guard already splits movement effect (PlayerController reads IsGuarding) from combat state. Conflicts to plan for: hitstun currently returns early from PlayerCombat.Update and only buffers strikes, so a dodge pressed during hitstun needs a buffer decision. Guard is a held state with no priority rules against dodge. Glide currently ignores combat input entirely (PlayerCombat returns when IsGliding, and TakeDamage is ignored while gliding), so air dodge behaviour must be an explicit decision. The right half tap is already spent on glide boost, so a dodge gesture on touch cannot reuse it without breaking boost.

Stamina: Stamina already exists and is not greenfield. It is spent only by heavy strikes, regenerated at 18 per second after a 0.8 second delay, displayed by CombatHud, and reset on death. Adding dodge cost or strike cost means extending the existing spend and regen delay pattern (set staminaRegenWait on spend, as DoStrike does) and updating the HUD only if a new spend needs feedback. One gap: there is no stamina insufficient feedback anywhere; a failed heavy silently becomes a normal strike. A dodge that silently fails on low stamina would repeat that gap.

Hitboxes: Combat today is entirely distance and angle tests against MossHorn.Center, with a 1.5 m projectile catch radius in WindSpear. No hitbox, hurtbox, collider or trigger exists on the hero or any creature, and creature visuals deliberately destroy their primitive colliders. Adding physics hitboxes would introduce the first collision callbacks in the project, new stripping exposure (see Physics dependencies), and a second source of truth beside the distance tests, which would disagree at edges (strike uses strike_range_m plus 0.9 for candidate selection, then a separate angle gate). The lower risk path that matches the codebase is distance and shape tests in code (capsule or sector tests against Center, in the WindLane.Contains idiom), keeping hit detection deterministic and testable like WindLane. If real hitboxes are chosen anyway, plan a shipped build grounding proof and a PlayTest probe addition before calling it done.

Monster AI state machine: One already exists in MossHorn, described above. Extending it means adding states or variants inside that switch, where stateTimer, staggerPool and hitstopTimer conventions are established. Risks inside the existing machine: states are a single switch in Update with transitions scattered across cases and TakeHit, so new states must respect the tell guarantee (Threaten always completes before Charge or slam). The elder slam is folded into Notice and Threaten through slamPending rather than its own state, so a generalised machine should expect to untangle that. Wander targets derive from TransactionId.GetHashCode, which is stable per run setup but not a designed random source. Creature movement ignores the hero CharacterController entirely and resolves hits by distance, so a new monster that needs body blocking would be a first.

### Surprises and risks found during the audit

1. Combat is more complete than a greybox label suggests. Stamina, guard, hitstun, input buffering, hitstop, stagger and a seeded loot ledger with a pity rule are all implemented and tuned. New combat design should assume these numbers are deliberate and read the Tuning comments, which carry spec ranges for nearly every value.

2. The engaged creature is computed three times in three places with three different rules: PlayerCombat.BestTarget, Harvester.FindNearby and the CombatHud engaged scan. They can disagree about which creature is the target, the harvest candidate and the displayed creature in the same frame.

3. A few gameplay literals live outside Tuning despite its header rule: gravity 22 and glide drop gate 1.5 m in PlayerController, knockback 4.5 and strike step 0.3 m in PlayerCombat, skill target search 22 m in PlayerCombat.DoSkill, charge hit distance 1.9 m and slam ranges 3.4 and 3.6 m in MossHorn, and wander speed 1.1 in MossHorn. Any tuning pass for the new combat system should sweep these into Tuning or consciously leave them.

4. Tuning.Camera return_delay_s and nudge_return_s are defined and ranged but read by no code. They are spec intent waiting for the camera build step, and a lock on camera should account for them.

5. HoldButton release timing is subtle. ReleasedThisFrame clears in LateUpdate behind a pending flag, so it remains readable through the following Update. PlayerCombat heavy detection depends on this and on HeldSecondsAtRelease. Rewriting the button or moving combat reads to a different phase could silently break heavy strikes.

6. The project root holds many build and playtest log files and a .github folder, but no .git directory. Local history and status checks are impossible here; the remote repo is the only version control record, which is why the checkpoint note above matters.

7. WebGL compression is disabled only inside BuildWebGL at build time, while ProjectSettings.asset records compression format 2. A build path that bypasses BuildSetup.BuildWebGL would ship a compressed build that GitHub Pages cannot serve correctly.

8. Diag.SafeMaterial depends on one Resources material to force the Standard shader into builds. Any pipeline change, including a future URP conversion, breaks this assumption and the Bootstrap material path with it. That is a further reason the current Built in pipeline should be treated as settled for the combat work.

## Source evaluation
# Driftworld Combat Sources Evaluation, Stage 1B

Scope: evaluation only. No code from any repository below was copied into the Unity project. Target context: Unity 6000.0.23f1, WebGL delivery, touch input, fully code built greybox, no reliance on editor heavy scene setup, only a handful of combatants in a fight, Monster Hunter style combat as the design goal.

Method: shallow clones into `design/combat/repos/` on 24 Nov 2026, plus web verification for the repository that failed to clone and for Jolt Physics. Dates below are commit dates shown by the clones or by the GitHub pages fetched on that day.

## Clone sizes

| Repo folder | Cloned size |
|---|---|
| CGCombatFramework | 6.8 MB |
| soulslike | 760 KB |
| ThirdPerson | 477 MB |
| EGamePlay (given URL) | 0, clone failed |
| Total on disk for `repos/` | 484 MB |

ThirdPerson accounts for almost all of the total. Its size is environment art, textures, skyboxes and models, not combat code. It has only 36 C# files. CGCombatFramework has 366 C# files in 6.8 MB. soulslike has 37 C# files in 760 KB.

## 1. CGCombatFramework

URL: https://github.com/Contradictive-Games/CGCombatFramework

### Existence and activity

Exists and is very active. Latest cloned commit: 5 Nov 2026, `Release v0.3.1`. The changelog shows v0.1.0 on 12 Oct 2026 and v0.2.0, v0.3.0 and v0.3.1 all within the following month, each carrying multiple breaking changes. It is a young and fast moving package, not a settled API.

### License

License file: `LICENSE.md`. License type, quoted: "MIT License", copyright Contradictive Games. No GPL, LGPL, noncommercial or extra restriction found. Clean for commercial reuse with attribution of the copyright notice.

### Unity version and dependencies

`package.json` states Unity `6000.0`, version 0.3.1, and a single dependency: `com.unity.modules.physics` at 1.0.0. README badge states Unity 6000.0 plus. This is the only candidate that matches Driftworld at Unity 6000.0.23f1 on its stated requirement. No Input System, Cinemachine, URP or networking package is required. Render pipeline agnostic, since it ships no visuals.

### Setup model

Mixed, and this is the central tension. The runtime is headless by design: combat runs with no visuals and works in tests and on servers. Units, stats, resources, status effects, damage and firables (its word for weapons and abilities) are composed from small components and interfaces. That part fits code driven construction well.

The authoring model does not. The README headline is that everything is built in the inspector. Definitions are `GameDefinition` ScriptableObject assets assembled with `SerializeReference` building blocks, supported by a large editor suite: Combat Studio, Database, Combat Doctor, Pipeline Tester, Firable Lab, Effect Timeline and more under `Editor/`. Driftworld builds everything in code, so adopting it as shipped means either creating and wiring definition assets in the editor, which breaks the code built rule, or constructing ScriptableObjects and serialized reference block lists in code, which is possible in Unity but fights the intended workflow and loses the editor tooling that is much of the value.

### Known issues and unfinished areas

From the README roadmap and Known Limitations section, plus the changelog:

1. Single threaded.
2. Homing projectiles find targets by scanning the unit registry, and large counts may cause performance spikes. Irrelevant at Driftworld scale.
3. Netcode hooks are still on the roadmap, listed as next. No networking today.
4. Package samples are still on the roadmap: presentation examples and a combat sandbox are not yet shipped.
5. API stability is the real risk. Breaking changes landed in v0.2.0, v0.3.0 and v0.3.1, including removed public events, renamed event fields and changed component lifecycle calls. Pinning a version and never updating is workable; tracking upstream is not.
6. Strengths worth noting: about 450 automated tests claimed on the README badge, with EditMode, PlayMode and performance suites in `Tests/`, and allocation tests that fail a run if a hot path allocates. Units, projectiles, status effects and hit collections are pooled.

### Genuinely reusable components and patterns

Named from `Runtime/`:

1. Damage pipeline under `Runtime/Health/Damage/`, used through `HealthComponent` and `IHealth`: an ordered list of damage steps (mitigation, shield absorb, immunity by tag and similar) that a hit passes through. This maps directly onto Monster Hunter style needs such as part damage, elemental resistance and hitzone multipliers.
2. Status effects under `Runtime/StatusEffects/`: `StatusEffectDefinition`, `StatusEffectInstance`, `StatusEffectsComponent`, `StatusEffectPool`, plus built in behaviours and triggers in `BuiltinBehaviors/` and `BuiltinTriggers/`. Stacking modes, tick payloads and trigger conditions are the pattern to copy for poison, paralysis, stun buildup and part breaks.
3. Stats and resources under `Runtime/Stats/` and `Runtime/Resources/`: `Stat`, `StatBlock`, `StatModifier`, `Resource`, `ResourceComponent` and regen rules. A clean model for health, stamina and sharpness style resources.
4. Queries and payloads under `Runtime/Core/Queries/` and `Runtime/Core/Payloads/`, with `HitCollector` and `HitData` in `Runtime/Context/`: sphere, box, cone, chain and unit queries that produce hit candidates for a payload. A good model for weapon hitbox sweeps.
5. Firables under `Runtime/Firables/`: `Firable`, `FirableDefinition`, `FirableLoadout`, fire modes and fire gates (cooldown, charges, ammo). The gate idea fits weapon combo finishers and item use.
6. Events under `Runtime/Events/`: `EventBus` with struct events passed by reference, plus `Unit`, `UnitHandle`, `UnitRegistry` and pooled spawning under `Runtime/Units/`. The versioned handle for pooled units is a sound pattern for a greybox that will pool monsters and effects.

### Verdict

Adapt patterns only. Reason: the license and Unity version are perfect and the runtime design is the best of the four, but the package is at version 0.3.1 with breaking changes every release, and its authoring workflow is inspector and ScriptableObject centred, which contradicts the fully code built constraint. Take the damage pipeline order, the status effect stacking and trigger model, and the stat and resource model as designs, and write a small Driftworld owned version sized for a handful of combatants.

## 2. soulslike (TeaYololo)

URL: https://github.com/TeaYololo/soulslike

### Existence and activity

Exists. The shallow clone shows a single commit, dated 5 Feb 2026, titled as an initial release, by Toprak Eren Akpinar. README status badge says Work In Progress. There is no release history or update trail to rely on.

### License

License file: `LICENSE`. License type, quoted: "Apache License, Version 2.0". Copyright Toprak Eren Akpinar. Clean for commercial reuse, with the Apache patent grant and notice conditions. No red flag.

### Unity version and dependencies

README states Unity 2022.3 LTS or newer. It requires FishNet networking from the Asset Store for its multiplayer support, and it requires the user to supply their own animation set, naming paid Asset Store packs as recommendations, because no animations are included for licensing reasons. Configuration is through a `CombatSettings` ScriptableObject. Driftworld is single player WebGL, so FishNet is dead weight, and Unity 6000 compatibility is untested by the author.

### Setup model

Scene and animator based. The quick start is: open the cloned project, import FishNet, import an animation set, set up the Animator following a setup guide, then add `CombatStateMachine`, `PlayerCombat`, `PlayerStaminaSystem` and an Animator with a proper controller to the character. That is prefab, animator controller and ScriptableObject setup, the opposite of code built. Nothing in the repo shows a code only construction path.

### Known issues and unfinished areas

The repo is unusually honest. `KNOWN_ISSUES.md` lists 13 issues, including three marked Critical:

1. Dual state tracking desync between `PlayerCombat._currentState` and `CombatStateMachine.CurrentStateType`, which can produce unpredictable behaviour when hit during an attack and then blocking.
2. Parry timing mismatch: `PlayerCombat.cs` uses a hardcoded 300 ms parry window while `DefenseSystem.cs` uses 180 ms, so parry success is inconsistent.
3. Missing `AnimatorHash` constants for `IsStunned` and `Destroy`, which are used but not defined.
4. High priority: network state desync, dodge invincibility frames unreliable at high latency, and a stamina race condition that can drive stamina negative and allow attacks without stamina in multiplayer.
5. Medium and low: string animator lookups in network code, duplicated helper patterns across seven or more files, legacy animator code, 18 unused animator hash constants, more than 70 magic numbers, a `CameraController.cs` of 1,391 lines described as a god class, and no unit tests at all.

### Genuinely reusable components and patterns

Named from the README architecture and `Assets/SoulslikeCombat/Scripts/`:

1. The eight state combat model in `Combat/StateMachine/`: `CombatStateMachine`, `CombatStateFactory`, `ICombatState`, `CombatStateBase`, and states for Idle, Attacking, Blocking, Dodging, Staggered, Recovering, UsingSkill and Dead. This state list is almost exactly the player state list Driftworld needs.
2. `Player/Combat/PlayerComboSystem.cs` for light and heavy attack chains, and `Combat/Core/AttackExecutor.cs` for attack execution.
3. `Combat/Core/DefenseSystem.cs` for block and parry resolution, and `Combat/Core/HitReactionSystem.cs` for hit reactions.
4. `Player/Core/PlayerStaminaSystem.cs` plus the stamina cost table in `CombatSettings`: light attack 15, heavy attack 25, dodge 20, dodge duration 0.6 s, invincibility starting at 0.05 s and lasting 0.35 s, parry window 0.18 s. Those default numbers are a sensible starting tuning set for a greybox.
5. The poise idea: a poise value that, when broken, forces the Staggered state. Directly reusable for monster stagger and part break thresholds.

### Verdict

Adapt patterns only. Reason: the license is clean and the combat feel vocabulary is the closest match to the target game, but the code itself ships with documented critical desync and timing bugs, depends on an animator controller, a supplied animation set, ScriptableObjects and FishNet, and has no tests and a single commit of history. Borrow the state list, the combo chaining idea and the tuning defaults. Write the implementation fresh with one source of truth for state.

## 3. ThirdPerson (mangelsr)

URL: https://github.com/mangelsr/ThirdPerson

### Existence and activity

Exists and is active as a personal prototype. Latest cloned commit: 7 Sep 2026, a project settings and URP configuration change. It reads as one developer course style project that is still being tinkered with, not a maintained framework with releases.

### License

Red flag. There is no LICENSE, LICENCE or COPYING file anywhere in the clone, at the top level or below it. A missing license means no right to reuse: default copyright applies and the code may not be copied, modified or distributed in Driftworld. This alone disqualifies adoption regardless of code quality. Reading it to understand general state machine ideas is one thing; lifting files or code is not permitted.

### Unity version and dependencies

The README states Unity 2022.3.62f2 and URP, with Cinemachine 2.10.5, Input System 1.14.2, TextMeshPro 3.0.9 and AI Navigation 1.1.7. The project itself tells a different story: `ProjectSettings/ProjectVersion.txt` in the clone says 6000.3.22f1, and `Packages/manifest.json` lists URP 17.3.0, Cinemachine 2.10.7, Input System 1.20.0 and AI Navigation 2.0.14. So the project was moved to Unity 6.3 and the README was not updated. For Driftworld at 6000.0.23f1 that means an untested version gap in the other direction, plus a hard URP commitment. If the parallel audit lands on the Built in render pipeline, this project does not transfer at all.

### Setup model

Fully scene, prefab and animator based. It ships 6 scenes, 23 prefabs and 2 animator controllers, with `SampleScene.unity` as the entry point, a `Player.prefab`, `Enemy.prefab` and `PlayerRig.prefab`, a generated `Controls.cs` from a `Controls.inputactions` asset, and large third party environment packs (Fantasy Forest Environment, Fantasy Skybox). Almost all of the 477 MB is that art. None of it fits a code built greybox, and the environment assets carry their own unknown licensing on top of the missing code license.

### Known issues and unfinished areas

1. No license, as above.
2. README and project version disagree, as above, which signals low maintenance discipline.
3. No tests, no changelog, no releases, no known issues list.
4. Includes a `Ragdoll.cs` and ledge hanging and pull up states that Driftworld does not need, while its combat is thin: one `Attack.cs`, `WeaponDamage.cs` and `Health.cs`.
5. Input is built on the new Input System action asset and Cinemachine, both of which would have to be added to Driftworld and then driven from code.

### Genuinely reusable components and patterns

Because there is no license, nothing here is reusable as code. As general patterns to reimplement independently, the useful ideas are:

1. `Assets/Scripts/StateMachines/StateMachine.cs` and `State.cs`: a small generic state machine base shared by player and enemy, with player states including `PlayerFreeLookState`, `PlayerTargetingState`, `PlayerAttackingState`, `PlayerBlockingState`, `PlayerDodgingState`, `PlayerParryState`, `PlayerImpactState`, `PlayerJumpingState`, `PlayerFallingState` and `PlayerDeadState`.
2. `Assets/Scripts/Combat/Targeting/Targeter.cs` and `Target.cs`: a targeter that holds a current target and a target component that registers and deregisters, the standard lock on pairing.
3. `Assets/Scripts/ForceReceiver.cs`: a single component that accumulates external forces such as knockback and applies them to movement, which keeps hit reactions out of the state code.
4. `Assets/Scripts/InputReader.cs`: an intermediary that turns Input System events into plain C# events the state machine subscribes to. The same intermediary shape works for touch input.

### Verdict

Reject. Reason: no license, so the code cannot legally be used, and it is also a scene, prefab, animator, Cinemachine and URP based prototype whose bulk is third party environment art. Note the four pattern ideas above and implement them independently if they are wanted.

## 4. EGamePlay (given URL: egametang/EGamePlay)

URL given: https://github.com/egametang/EGamePlay

### Existence and activity

The given URL does not exist. The clone failed with a git credential prompt, which is what git reports for a private or missing repository, and a direct fetch of that URL on 24 Nov 2026 returned HTTP 404. Web verification found the real project under a different owner: https://github.com/m969/EGamePlay, which describes itself as a flexible combat and skill framework based on the Entity Component pattern, with configuration by ScriptableObject or Excel. That repository is public, shows 274 commits, about 2,411 stars and 546 forks, reports version 3.0 as current with 2.0 and 1.0 on separate branches, and is not archived. Its documentation and wiki material indexed by DeepWiki was last crawled about a year before this evaluation, so a precise recent commit date could not be confirmed from web sources. The confusion is understandable: EGamePlay references egametang/ET as its server framework, and the given URL appears to have merged the two projects into one address that never existed.

Because the assigned URL is wrong, no clone of EGamePlay exists in `repos/`. The assessment below is from the GitHub page and documentation of m969/EGamePlay fetched on 24 Nov 2026, not from local code inspection.

### License

For the real project, m969/EGamePlay, GitHub reports "MIT License (MIT)" and the repository root contains a `LICENSE` file. On that basis the framework code itself is reusable. Two cautions: first, that finding applies to the m969 repository, not to the dead URL in the brief, so the correct repository should be confirmed before any use. Second, the mandatory and optional paid plugins below have their own commercial licenses and are not covered by the MIT license. No GPL or LGPL flag was found in the framework itself.

### Unity version and dependencies

Stated Unity version: 2022.3.53f1. Dependencies are the disqualifying part:

1. Odin Inspector is mandatory, not optional. The documentation calls it a hard requirement for all integration paths, used for serialization and editor tooling throughout, and it must be installed before EGamePlay. Odin is a paid Asset Store plugin.
2. DOTween Pro and Animancer Pro are listed by the author as paid plugins the project uses; documentation treats them as optional but recommended, with Animancer recommended for animation heavy work.
3. Version 3.0 also embeds the EcsNode framework from m969/EcsNode as its Entity Component System foundation.
4. Integration requires adding a `UNITY` conditional compilation symbol, and the ET path requires an `EGAMEPLAY_ET` symbol and ET Framework 8.1 or later.

Driftworld at Unity 6000.0 would be two major versions beyond the tested 2022.3 baseline, with a paid editor plugin as a hard dependency.

### Setup model

Editor, ScriptableObject and spreadsheet based. Making a skill means adding a row to `AbilityConfig.xlsx`, creating an ability configuration asset from a right click menu, creating an Execution asset the same way, and attaching the skill to a `CombatEntity` at runtime through a spell component. Demos are scenes: `RpgExample` and `ExecutionLinkScene`. Integration into another project means copying eight named directories by hand, and the author states plainly that integration is not yet perfect and that conflicts or missing pieces must be resolved as needed. This is the furthest of the four from code built construction, and much of the documentation is in Chinese, with support through a QQ group and Zhihu articles.

### Known issues and unfinished areas

1. Hard dependency on a paid editor plugin, Odin Inspector.
2. Author admitted imperfect integration, with conflicts to be resolved manually.
3. Tested only against Unity 2022.3.53f1; no Unity 6 support claim.
4. Version 3.0 was a large runtime rewrite around EcsNode, so older tutorials and the 1.0 and 2.0 branches describe different code.
5. The turn based demo was removed in 3.0; only the RPG demo remains.
6. Excel based configuration adds a spreadsheet toolchain and generated configuration classes that a code built project does not want.

### Genuinely reusable components and patterns

Patterns only, and only from the documented architecture:

1. The Entity Component separation itself: combat entities holding components, with systems acting on them, and business logic separated from view logic. Driftworld can use a much lighter version of this without EcsNode.
2. The ability model: an Ability with a configuration, an Execution made of timed clips that coordinate animation, effects and hit moments, and a spell component that casts it. The timed execution clip idea is a strong fit for Monster Hunter weapon moves, where hit windows sit at fixed points in a swing.
3. The numeric attribute system covered in the author article series: layered attributes with base, added and percentage values feeding combat maths.
4. The Status effect model from the same series: status definitions separate from status instances on an entity, which matches the CGCombatFramework model and confirms the shape.
5. The buff and skill configuration split between static configuration data and runtime state.

### Verdict

Reject as a dependency, adapt patterns only. Reason: the assigned URL is dead, the real project demands a paid Odin Inspector license as a hard requirement plus paid tween and animation plugins, targets Unity 2022.3 rather than Unity 6, and is configured through Excel files, editor windows and ScriptableObject assets. Its ability execution clip and attribute layering ideas are worth borrowing on paper.

## 5. Jolt Physics (assessment only, not cloned)

URL: https://github.com/jrouwe/JoltPhysics

### What it is

A C++ rigid body physics and collision library, MIT licensed, very active and credible: about 12,411 stars, 1,838 commits and 17 releases on the GitHub page fetched 24 Nov 2026, used in Horizon Forbidden West and Death Stranding 2. Its strengths are multithreaded simulation, deterministic simulation, large worlds, vehicles, soft bodies, and animated ragdolls with constraint motors. None of those strengths addresses a Driftworld need.

### Would it help a WebGL mobile browser game with a handful of combatants?

No. Concrete reasoning:

1. WebGL support is not there in the Unity path. Jolt core lists WebAssembly as a supported platform only through a separate project, JoltPhysics.js, which is a JavaScript binding for web apps, not a Unity plugin. The Unity binding found, seep/JoltPhysicsUnity, describes itself as work in progress, has no published releases and no registry package, and is built on a native `joltc` library loaded through PInvoke `DllImport`. Its build documentation lists native libraries for Windows x64, macOS x64 and Linux x64 only. Unity WebGL cannot load desktop native libraries; a WebGL build needs a library compiled for the WebGL platform and linked as a Unity WebGL plugin, and no such build is offered. So the most likely outcome of attempting it is a build or runtime failure on the actual delivery target, discovered late.
2. Binary size and download cost. Unity WebGL already ships one physics engine inside the build. Adding a second native physics engine compiled to WebAssembly adds its full compiled size to a download that mobile browser players must fetch before playing, plus the binding layer, for zero player visible gain. On mobile data connections, build size is a real product cost.
3. Double physics maintenance. Driftworld would then have two physics worlds to keep in agreement: Unity physics for character controllers, triggers, raycasts, animation driven motion and every existing greybox behaviour, and Jolt for whatever was moved over. Colliders would need to exist in both worlds or be mirrored frame by frame, hit detection would split across two systems, and bugs would present as objects that collide in one world and pass through in the other. With a code built project and no dedicated physics engineer, that is a permanent tax on every combat feature.
4. No load to justify it. Jolt advantage appears with many simultaneous dynamic bodies, large worlds, heavy multithreading and background loading and unloading of simulation islands. Driftworld fights involve a handful of combatants in a greybox arena. Unity built in physics handles a few capsule characters, weapon hitbox overlaps and ground checks without measurable strain, and its multithreading limits in WebGL apply to any engine running in a browser tab.
5. No ragdoll requirement. The one Jolt feature set that could change game feel, animated ragdolls with motors and skeleton mapping, is not a Driftworld requirement. Monster hit reactions, stagger, topple and part breaks in a Monster Hunter style game are animation and state driven, not ragdoll driven. If a topple is ever wanted, a simple scripted fall or a Unity physics ragdoll on death is enough.
6. Touch and browser constraints dominate performance anyway. Frame cost in a mobile browser build will be set by rendering, scripting and memory pressure long before physics at this body count becomes the bottleneck. There is no measurable advantage to measure.

### Verdict

Reject. Keep Unity built in physics as the single physics system. Revisit only if Driftworld gains a hard requirement that Unity physics cannot meet, such as large scale dynamic destruction, and only after a WebGL build of the binding is proven on the target.

## Summary table

| Repo | License | Unity compat | Verdict |
|---|---|---|---|
| CGCombatFramework | MIT | States Unity 6000.0, matches 6000.0.23f1, physics module only | Adapt patterns only |
| soulslike (TeaYololo) | Apache License 2.0 | States 2022.3 LTS or newer, Unity 6 untested, needs FishNet and a supplied animation set | Adapt patterns only |
| ThirdPerson (mangelsr) | No license file, no right to reuse | README says 2022.3.62f2 with URP, project files say 6000.3.22f1 with URP 17.3 | Reject |
| EGamePlay (given URL egametang/EGamePlay) | URL is 404; real project m969/EGamePlay reports MIT | States 2022.3.53f1, mandatory paid Odin Inspector | Reject, adapt patterns only |
| Jolt Physics (jrouwe) | MIT | C++ library; Unity binding is work in progress with no WebGL native build | Reject |

## Recommended sourcing strategy

Build the combat system as Driftworld owned code, borrowing designs from two of these projects and nothing else.

### Take as design, not as code

1. From CGCombatFramework: the ordered damage pipeline, status effects with stacking modes, tick behaviours and triggers, layered stats with modifiers, resources with regen rules, hit queries that return hit candidates, and pooled units with versioned handles. It is MIT licensed and Unity 6 native, so if a later stage wants to trial the actual package on a pinned version, the legal and version doors are open. Do not build on it now, because version 0.3.1 is still breaking its own API monthly and its authoring is inspector led.
2. From soulslike: the eight state player combat model, light and heavy combo chaining, poise breaking into stagger, and its published tuning defaults for stamina costs, dodge timing, invincibility frames and parry window as starting numbers for the greybox.
3. From EGamePlay, on paper only: abilities as timed execution clips so that hit windows, effects and recovery sit at authored points in a move, and attributes layered as base plus additions plus percentage multipliers.
4. From ThirdPerson, as general knowledge only and never as copied code, because it has no license: a shared generic state machine for player and monster, a targeter and target pairing for lock on, a force receiver for knockback, and an input reader that converts raw input into game events. That last shape is how touch input should enter the system.

### Write fresh

1. A small state machine with one source of truth for combat state. The soulslike desync bug is the cautionary example: state lives in exactly one place.
2. Player and monster controllers built entirely in code: no prefabs, no animator controllers, no ScriptableObjects required to run a fight. Animation, when it arrives, should be driven by the state machine rather than driving it.
3. A compact damage and status implementation sized for a handful of combatants: health, stamina, poise or stagger buildup, part health for breaks, elemental values, and a short list of status effects. A few hundred lines owned by Driftworld beats a general framework.
4. Weapon moves as data defined in code: startup, active hit window, recovery, stamina cost, poise damage, and whether the move can be cancelled into a dodge. That is the Monster Hunter feel, and none of the four projects ships it in a form that fits.
5. Lock on and camera written for touch first, with a soft lock that favours the monster in front of the player, rather than a Cinemachine and mouse and keyboard setup adapted afterwards.
6. Hit detection on Unity built in physics only: capsule characters, overlap and sweep queries for weapon arcs, and trigger volumes for monster attacks. One physics system, no Jolt.

### Why

Licensing removes ThirdPerson outright and makes EGamePlay expensive, since Odin Inspector is a paid hard dependency. Setup model removes the rest as drop in solutions: three of the four assume scenes, prefabs, animator controllers, ScriptableObjects or spreadsheets, and Driftworld builds everything in code. Maturity removes the remaining candidate: CGCombatFramework is the best designed and the only Unity 6 match, but at version 0.3.1 with breaking changes in three straight releases it is a design reference, not a foundation. What Driftworld actually needs for Monster Hunter style combat is narrow and well understood: committed attacks with real recovery, stamina, stagger and part breaks, readable monster states, and a lock on camera that works with thumbs. Writing that narrow system fresh, informed by the patterns above, is less work than bending any of these frameworks into a code built WebGL shape, and it leaves no license, paid plugin or version risk in the shipping game.
