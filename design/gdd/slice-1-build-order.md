# Slice 1 Build Order and Shared Rules
Slice ID: driftworld_slice_1, revision 1. Contract: design/concept.md, section The first build.
Specs: design/gdd/mech-001 to mech-008. Every number in every spec lives in one Tuning ScriptableObject set, grouped as the specs group them. No tuning literal appears in gameplay code; the code audits in the specs enforce this.

## Build order
| Step | Spec | Depends on | Why this order |
|---|---|---|---|
| 1 | MECH-001 Drag Movement | none | Everything else is touched through movement |
| 2 | MECH-008 Camera | MECH-001 | Combat and glide both judge framing |
| 3 | MECH-002 Glide and Wind Lanes | MECH-001, MECH-008 | The fantasy gets proven before the fighting does |
| 4 | MECH-003 Spear Combat | MECH-001 | Verbs before opponents |
| 5 | MECH-004 The Moss Horn | MECH-003 | The opponent exists to test the verbs |
| 6 | MECH-005 Harvest | MECH-004 | The payoff of the fight |
| 7 | MECH-006 Crafting and the First Gear Set | MECH-005 | The receipt that sends the player out again |
| 8 | MECH-007 Expedition Flow | all above | The glue: banking, retreat, failure |

## Shared build rules
1. One mechanic per build task and one spec ID per commit, in the form: feat(glide): lane lift and boost [MECH-002].
2. Before building a spec, list its ambiguities and any check that cannot be automated. Resolve them in the spec, not in code.
3. After building, run every agent verified check in the spec and paste the output with the result. A check that was not run is reported as not run.
4. Tuning defaults and ranges change only through the spec. An agent changing a constant to make a check pass is a defect, and the change is reverted.
5. Observability ships with the slice: a debug panel bound to the tuning set, a seeded random generator for all drops and combat rolls, and a state dump the test scripts can read.
6. Human checks are fun passes on a real phone. Agent checks prove correctness; only people prove fun, and the slice gate in the concept document needs both.

## Content requirements the specs place on island one
- At least three common Moss Horns and one elder, spaced so a new player meets one tell at a time (MECH-004, MECH-006 route math).
- One chest, spawning on the elder harvest (MECH-007).
- One launch ledge and one wind lane from the hub, with the return portal at the landing point (MECH-002, MECH-007).
- Distances set so a crossing of the island on foot stays under three minutes at base move speed (concept document pillar one).

## Slice evidence pack (what gets collected for the gate decision)
- The agent check output for all eight specs, in one folder, named by spec ID.
- One unedited phone recording of a natural start to finish run: hub, glide, fight, harvest, craft, bank.
- Five outside tester sessions with the human checks scored, including the second expedition question from the concept document: did the tester choose to go again unprompted.

## Build and delivery pipeline (cloud only, adopted 2026-11-22 by founder direction)
No local Unity exists on this project. The founder computer cannot carry the Unity download and the agent machine cannot run Unity, so every build is a cloud build. This section is the official route and overrides any step anywhere in these documents that implies a local editor.

1. Source of truth: the GitHub repo (created by the founder, named driftworld). All code, specs, and the tuning set live there. Luna pushes through the GitHub connector; binaries and large assets follow the standing rule that the founder uploads what the connector cannot push.
2. Everyday lane: every push triggers a cloud build (GitHub Actions running the game-ci Unity builder, or Unity Build Automation if the project moves there) that produces a WebGL build published to GitHub Pages. The founder and testers play it from a link in any browser. This lane verifies loops, layout, flow, and correctness. It never signs off on feel or frame rate, because browser performance is not phone performance.
3. Milestone lane: at each spec milestone and always before a gate decision, the same pipeline produces a native build. iOS builds deliver through TestFlight; the founder already holds an Apple Developer account (confirmed 2026-11-22), so this lane is open from the first milestone with no enrollment step. Android builds deliver as an installable file with no account or fee, as an additional check on a second platform.
4. Human checks run on the milestone lane only: a real build, on the founder phone, judged by the founder. The founder verdict is the gate evidence, the same standing rule as every other project in this workspace.
5. Build failures are reported with their cause from the pipeline log. A green code check is never reported as a working build; only a produced build that someone opened counts.
