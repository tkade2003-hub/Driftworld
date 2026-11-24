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
