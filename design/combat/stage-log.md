# Combat implementation stage log

## Stage 2 (in progress)
- Audit read in full. License preflight: Unity Personal Assigned and active.
- Design decisions recorded in design/gdd/mech-009-monster-hunter-combat.md.
- Publish constraint found: the release asset swap and the Pages workflow
  dispatch need the GitHub web UI (the connector has no release asset or
  workflow dispatch tool, and gh is not authenticated on this VM). Code push
  via push-files, local build, rezip, and full CDP proof on the exact shipped
  artifact are done here; the asset swap and fresh workflow run are handed to
  the parent agent with exact steps at each stage boundary.

## Stage 2 code complete, build blocked on license (2026-11-24 09:53 UTC)
- License was Assigned at preflight (09:36 UTC) and build 1 started under
  it. It lapsed during the build window; `unity license status` now reads
  "License: none active." Build 2 (build-combat-s2b.log) failed at startup
  with "No valid Unity Editor license found." BLOCKER: the parent must
  reactivate Unity Personal from the main chat, as before. The subagent was
  instructed not to attempt activation itself.
- Build 1 (build-combat-s2.log) also taught a pipeline lesson: its first
  script compilation used a stale 15 file list (cold import race; the new
  scripts had no .meta files yet and were not in the Assembly-CSharp rsp).
  All 16 CS0246 errors trace to that one compilation, not to the code. The
  .meta files now exist for every new script, so the next build compiles
  the full set. No genuine code errors identified so far.
- Code state (all written, uncompiled as a full set):
  Tuning.cs extended (Dodge, LockOn, Weapons, Parts groups; Combat,
  MossHorn, Camera additions; literals swept from MossHorn/PlayerCombat).
  New: WeaponDefinition.cs (all five weapons as data), Damage.cs (ordered
  pipeline, CombatEvents, CombatDebug), Hurtbox.cs (8 parts, geometric arc
  tests, debug viz), Targeting.cs (one shared lock target), TestBridge.cs
  plus Plugins/WebGL/TestBridge.jslib (window.__dwState), CombatDebugInput.cs
  (weapon keys 1 to 5, viz toggles Z/X/N, crit override F, force keys
  R/T/Y/U/I, staging keys G/H/B/V), HitVfx.cs, WeaponRangeViz.cs,
  DamageNumbers.cs.
  Rewritten: PlayerCombat.cs (attack phase machine, combos, dodge with
  i frames, lock aim, hurtbox sweeps, hyper armor, Great Sword charge
  levels), MossHorn.cs (Approach, Flinch, Stunned, Knockdown, EnrageRoar,
  Exhausted, Retreat states; part break and tail sever in ApplyDamage;
  training reset), FollowCamera.cs (lock framing), CombatHud.cs (Dodge and
  Lock buttons, weapon label, debug panel, Targeting driven creature bar),
  WindSpear.cs (part pipeline), Bootstrap.cs (?sandbox=1 arena mode, new
  wiring), PlayerController.cs (SetHeading, WishDirection, commitment
  suppression), Diagnostics.cs (Combat field).
- Test harness ready: ~/workspace/browser-test-scratch/combat_test.py
  covers AC-1 movement (explicit), AC-2 whiff, AC-3 combo/no duplicates,
  AC-4 part multipliers with exact totals, AC-5 stagger, AC-6 dodge
  i frames with an undodged control, AC-7 lock, AC-8 weapon switch,
  AC-10 hammer stun. Serve builds/WebGL/WebGL on port 8941 and run it.
- Resume steps once the license is back:
  1. Rebuild via BuildSetup.BuildWebGL (detached, it outlives a turn).
  2. Confirm zero CS errors, run combat_test.py, fix and iterate.
  3. Stage 3 proof items: GS/SnS/DB fights (AC-8 style), part break and
     tail sever (AC-11), enrage then exhausted (AC-12). The data and
     systems for all of these are already in the code above.
  4. Stage 4: slice run on the plain URL (kill, harvest, loot via the
     same bridge), rezip the release asset, push code via push-files.
  5. Parent only: swap the slice-1 release asset in the GitHub web UI and
     trigger a FRESH pages.yml workflow run.

## Session 2026-11-24 (resumed, license back): sandbox suite GREEN 24/24

- Builds: build 1 cold import CS0246 race (missing .meta, since landed).
  Build 2 shipped the unscaled strike hold fix (strikePressUnscaled in
  PlayerCombat.cs, matching HoldButton unscaledDeltaTime) so quick taps
  stop reading as heavies at low frame rates. Build 3 shipped the
  StrikePressedEdge fix: Input.GetKeyDown event edge instead of level
  polling, because sub frame taps vanished entirely. Both are genuine
  robustness fixes, proven on the shipped artifact.
- combat_test.py run 14 against build 3: 24/24 PASS. AC-1 movement with
  inp 1.00, AC-2 whiff zero events, AC-3 combo with no duplicate
  (swing,hit) pairs and all hits in Active, AC-4 head exactly 24.50 and
  tail exactly 18.20 with crits off, AC-5 stagger (pool 98 before the
  crossing hit), AC-7 lock, AC-8 switch plus GS Overhead Chop 33,
  AC-10 hammer stun in 5 head hits.
- AC-6 was the long fight. Ground truth from the shipped code: the
  charge hit test uses the frame start distance, so from the 5.5 m
  staging the hit lands on the second Charge frame; dodge
  invulnerability needs dodgeTime in [0.04, 0.34] then, and one frame
  of dodgeTime is 0.3333 (the deltaTime cap at this lane's frame rate),
  so the dodge must start exactly on the first Charge frame and go
  forward (a backward dodge clears the 1.9 m check before it fires).
  Roughly thirty externally timed attempts (Charge sighting dispatch,
  Threaten offset polling, fixed real time delays 0.0 to 2.3 s, fresh
  page, fresh browser, small viewport) produced one lucky evade: CDP
  round trips and bridge polling carry more latency than the one frame
  window is wide. The working solution runs inside the page: a watcher
  polls __dwState every 25 ms, stages with a synthetic V key, starts a
  synthetic forward drag on the Threaten push, and dispatches a
  synthetic Space on the first push at or past t_s + 0.50. Synthetic
  keyboard and touch events reach the engine (inp reached 1.0, Dodge
  phase observed). Run 14 evaded on attempt 1: hp 100 to 100 with the
  hit recorded evaded; the undodged control in the same run lost
  exactly 18 (100 to 82). AC-6 PROVEN.
- Stage 3 run 1 (stage3_test.py): 15/18. AC-11 full pass (head break in
  3 hits with the crossing event flagged broke, tail sever in 2 hits
  flagged severed, severed tail inert). Weapons: GS short hold heavy
  matched the L1 total exactly (38.72 on Tail), SnS Quick Slash, DB
  Twin Fang two hits on one swing. AC-12 enrage began at hp 669.93
  (line is 700). Two harness side failures, both fixed for run 2:
  (a) GS L3: the 1.7 s charge hold at 2.3 m staging outlasts the elder
  slam tell (0.6 s) and the slam hitstun killed the release; run 2
  forces a stun (U key, 4 game s) before the hold. (b) AC-12
  exhaustion: the grind loop kept attacking inside the shared 420 s
  cap and the window closed before the 45 game s enrage timer
  expired; run 2 breaks the grind at enrage and adds a no attack watch
  phase (dodge only, up to 200 s) so BeginExhaustion fires from the
  timer.
- Next: stage 3 run 2 verdict, then stage4_test.py on the plain URL,
  final rebuild, rezip, push.

## Session close 2026-11-24: Stage 3 and 4 GREEN, release packaged

- Stage 3 run 4: 18/18. AC-11 head break and tail sever with flagged
  crossing events and an inert severed tail; GS short hold heavy at a
  catalog charge level; GS L3 judged on the event phys field (Total
  carries the 1.5 vulnerable state factor while the monster is
  stunned, phys is always Power x step x part): exactly 99.0 on Head
  (22 x 1.6 x 1.875 x 1.5); SnS Quick Slash; DB Twin Fang two hits on
  one swing; AC-12 enrage at hp 691.71 and exhaustion from the enrage
  timer at unchanged hp.
- Stage 4 run 1: 7/7 on the plain URL. A common Moss Horn killed in
  the main slice (hero at 46 hp), kills 0 to 1, harvest hold completed
  (harvests 0 to 1), shards 0 to 10. MECH-006 and MECH-007 untouched.
- Final release build through BuildSetup (Strip Engine Code OFF,
  touch patch in index.html, diagnostics in): Build Finished, Result
  Success, zero CS errors. All three suites rerun against the final
  artifact itself: combat 24/24, stage 4 7/7, stage 3 18/18. AC-6
  evaded on attempt 1 again in the final run.
- Release zip: ~/workspace/driftworld/builds/driftworld-webgl-slice1.zip
  (19 entries, index.html at the root, 11,266,608 bytes,
  sha256 9f2734a400620fa1dfebe88bb1b35a4055b11b4500a1c91a57220a72ca6d8801).
- Code pushed via push-files: 114 files (Assets, Packages,
  ProjectSettings, design docs without the reference repo clones,
  README, .gitignore) to tkade2003-hub/Driftworld main at commit
  6e8789f747c0e9052003b9edb0f9e92758234e83. Remote PlayerCombat.cs
  verified to contain both input fixes.
- Remaining, parent only: swap the slice-1 release asset in the GitHub
  web UI with the zip above and trigger a FRESH pages.yml workflow run
  (never rerun old jobs). Then Vashon's phone check on the live Pages
  build is the final sign off, per the standing rule that only the
  shipped artifact on his device proves a build.

## Controls overhaul 2026-11-24: diagnosis proven, code written, BUILD BLOCKED on the Unity license

- Vashon played the published combat build on his phone: he still
  cannot move the character, and he directed the button free gesture
  scheme (drag moves anywhere, flick at the right time dodges, tap
  attacks, the fewer buttons the better).
- Preflight: `unity license status` reports "License: none active."
  The license that was Assigned at 05:24 PT has lapsed again. Per the
  brief, no activation was attempted from this session. The parent
  reactivates Unity Personal from the main chat; everything below the
  build step is done and waiting.
- DIAGNOSIS, proven on the shipped artifact itself
  (~/workspace/browser-test-scratch/diag_probe.py against the current
  builds/WebGL/WebGL): an identical upward CDP drag starting on the
  RIGHT half read movement input 0.00 for its entire duration, while
  the same drag on the left half read 1.00 and displaced the hero
  24.68 m. Root cause: DragInput only began movement tracking for
  touches starting on the left half of the screen (MECH-001 rule 4
  plus the MECH-003 right half combat split); a right half touch
  fired OnRightTap and was never tracked. Every right thumb drag was
  discarded by design. The acceptance harness never caught it
  because every synthetic drag it ever ran started on the left half.
  Secondary findings: uGUI elements never blocked the low level touch
  stream (they only consume pointer events, and the buttons sat on
  the right, reinforcing the dead side); the index touch-action patch
  was present and correct in the shipped file, but browsers that
  ignore it (older iOS Safari) can still cancel a drag mid gesture,
  which CDP and in page synthetic touches cannot reproduce.
- CODE, complete in the working tree, not yet compiled or built:
  - DragInput.cs rewritten in place as a single whole screen gesture
    classifier (execution order -50, edge flags cleared in
    LateUpdate, public movement surface unchanged for PlayerController
    and TouchRipple). Sustained drag steers from anywhere. Release
    classification uses touch event timestamps with an unscaled clock
    fallback: tap (at most 0.28 s, peak drift at or under 26 px at
    1080p scale), still hold (charge and harvest), flick (at most
    0.32 s AND at least 55 px AND average velocity at least 700 px
    per s, all scaled; a sustained drag fails the duration and
    average velocity gates no matter how fast it ends). A press that
    becomes a drag or a flick cancels the pending strike. A touch
    first seen as Ended (sub frame tap folded by the platform) is
    replayed as an instant tap. New Tuning group Gestures holds the
    five constants.
  - PlayerCombat: the five HoldButton fields are replaced by the
    DragInput stream plus a Harvester reference. Tap and hold drive
    the existing press, release, heavy, and charge logic with the
    gesture held duration. Flick calls the same DodgePressed seam as
    Space and carries its direction into StartDodge as an override.
    Guard and the special are keyboard only now (K, L). Beside a
    harvestable corpse, strike presses are suppressed so the harvest
    hold cannot charge a heavy.
  - Targeting.SoftUpdate: the soft target is the nearest living
    monster inside acquire range, refreshed every frame from
    PlayerCombat.Update. Lock on is automatic and invisible; aim,
    camera framing, HUD bar, and bridge lock fields read it as
    before. ToggleOrCycle and Validate remain in the file, uncalled.
  - CombatHud: all action buttons (Strike, Guard, Dodge, Wind,
    Lock), the harvest HoldButton, the skill and lock labels, and
    the whole DBG debug panel are deleted. The harvest panel is
    progress display only. The canvas no longer adds a
    GraphicRaycaster, so the HUD cannot intercept any touch. Kept:
    health and stamina bars, weapon label, loot counter, creature
    bar (LOCK prefix removed), objective line, toast, diag readout.
  - Harvester: the hold is DragInput.HeldStill (still touch) or H.
  - Bootstrap: no EventSystem is created anymore; combat and the
    harvester are wired to the hero DragInput.
  - BuildSetup.PatchWebGLIndex: keeps the touch-action style and
    adds a dw-gesture-guard script that preventDefaults touchmove
    on the canvas (passive false) for browsers that ignore
    touch-action.
  - TestBridge: hero JSON gains gest (last classified gesture),
    touch (a touch is held), and peak (peak drift px).
  - HoldButton.cs is now unreferenced; left in the tree as history.
  - Grep verified: no remaining references to the removed button
    fields, OnRightTap, DebugPanelStartsOpen, or the old targeting
    calls.
- HARNESS, written and waiting: touch_test.py (new) covers T0 soft
  lock with no input, T1 right half drag moves (the headline case),
  T2 drag starting on the health bar moves, T3 tap strikes and
  damages in range, T4 flick during Threaten evades with the
  undodged control losing exactly 18 (in page watcher mirroring the
  proven AC-6 alignment, retries while health allows), T5 sustained
  drags (plain and with a fast final yank) never dodge, T6 a tap
  never registers movement input, plus a HUD screenshot saved for
  the by eye no buttons read. combat_test.py AC-7 no longer presses
  C: it asserts the soft lock engages with no input. stage3_test.py
  and stage4_test.py still press C and use left half hold drags with
  Space dodges; the C presses are now harmless no-ops (soft lock is
  already on) and their drags are seconds long, so they classify as
  drags; both suites must still be rerun green on the new artifact.
- GDD: controls addenda appended to mech-001 and mech-009 recording
  the gesture scheme, the button removal, the automatic lock, and
  the superseded screen split. MECH-006 and MECH-007 untouched.
- RESUME SEQUENCE once the license is Assigned again: (1) build via
  BuildSetup.BuildWebGL (detached), confirm the built index.html
  contains dw-gesture-guard; (2) serve builds/WebGL/WebGL on 8941
  and run touch_test.py, then combat_test.py, stage3_test.py,
  stage4_test.py against that exact artifact; (3) read
  touch-hud.png and confirm no action buttons remain; (4) iterate
  on any failure; (5) rezip to
  ~/workspace/driftworld/builds/driftworld-webgl-slice1.zip;
  (6) push code via /opt/hatch/bin/github push-files; (7) hand the
  publish step (release asset swap plus a FRESH pages run) back to
  the parent with the new zip hash. Strip Engine Code stays OFF.

## Session 2026-11-24 (later): gesture build proven, fight mode added

Vashon played the published combat build and reported: "I still
can't move my character and there shouldn't be buttons to dodge or
attack. I should be able to move by swiping the screen and when I
swipe at the right time I perform a dodge and when I tap on the
screen I attack... the less buttons on screen the better." Then,
while the gesture build was being proven: "When in fight mode,
there should be only swipe gestures like in Monster Hunter Now, no
walking or running." Both directions are in this build.

Gesture overhaul (from the previous session, proven in this one):
DragInput is a single whole screen gesture classifier (the old
left half only tracking is why right thumb drags never moved the
hero; the old harness never caught it because every synthetic drag
started on the left). Tap strikes, a still hold charges and
harvests, a flick dodges, a sustained drag steers from anywhere.
CombatHud lost every action button and the canvas its
GraphicRaycaster. AC-1's failure on the first gesture artifact was
a mouse tracker wedge in DragInput (a same frame mousedown and
mouseup lost the up edge and latched the tracker to the mouse id);
the fix ends the mouse touch when the button is no longer held.

Fight mode (this session): the engagement signal is the Moss Horn
AI state, computed in Targeting.SoftUpdate as Targeting.FightMode:
on while any living Moss Horn is out of Graze. The 22 m soft lock
was rejected as the signal because it is held across most of an
arena and would forbid walking almost anywhere a monster exists;
the AI state is the game's own record of "this monster is
engaged". In fight mode MoveVector returns zero (a sustained drag
displaces nothing), a tap attacks, a still hold charges, and any
decisive swipe dodges in the swipe direction: a release
displacement past the flick minimum inside
Tuning.Gestures.fight_swipe_max_duration_s (1.2 s) at any speed,
because with no movement drag to confuse it with, the flick
duration and velocity gates do not apply in a fight. Mid gesture
transitions re anchor the held touch and restart its clock, so a
drag cannot keep walking the hero into a fight or lurch out of
one. Outside a fight the scheme is unchanged. Debug staging gained
the P key (reset the monster to Graze and place the hero 20 m
away, outside the 14 m notice range) so movement cases stage in
exploration. GDD addenda: mech-009 (fight mode rule) and mech-001
(locomotion is exploration only), both dated 2026-11-24.

Proof on the shipped artifact (build of 2026-11-24 16:54, Strip
Engine Code off, gesture guard and touch-action none in the built
index): combat_test.py 24/24, touch_test.py 41/41 (24 gesture
checks plus T7 to T10 fight mode cases and reconciled movement
staging), stage3_test.py 18/18, stage4_test.py 7/7. The HUD
screenshot was read by eye: bars and labels only, no action
buttons, hero grounded in front of the Moss Horn.

Harness reconciliations this session, no assertion weakened and no
game code changed to pass a test: fixed wall clock budgets measure
the lane, not the game (game time here runs at a variable fraction
of wall time), so movement and charge cases now hold until the
asserted quantity is itself observed (AC-1 displacement, T1 and
T2 settle, stage 3 waits for the Free phase before charge holds
after probe_l3.py proved the charge math exact at 99.0). Gesture
classification reads wait for the release to be visible in the
bridge (probe_t10.py showed a roughly 500 ms push lag that
otherwise serves the staging click's stale classification). T2
asserts the peak sampled displacement: probe_t2.py showed a down
screen drag walks the hero into the follow camera's pivot, the
camera relative direction inverts, and the net displacement
measures the camera swing, not the drag. T4 and combat AC-6 dodge
watchers fire on the charge closing inside 2.5 m (every tell timed
offset missed deterministically in probes; the successful trigger
was the closing distance), and T4's flick is lateral, clearing the
charge line instead of dodging through the body where the evade
hinged on iframe alignment under load. One run was lost to box
load (load average near 7 on 1.5 shared CPUs with stale test
browsers); the browsers were cleared and the suite passed clean.

Release asset: builds/driftworld-webgl-slice1.zip, 11,263,458
bytes, sha256
337e918da78363a3e98fb9adf30d3cef063e70274c19486199a5626ee3107934,
index.html at the zip root. Publishing (release asset swap and a
fresh Pages workflow run) is handed back to the coordinator; the
play link is proven only when the swapped artifact itself is
served and Vashon confirms on his phone.
