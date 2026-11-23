# MECH-002 Glide and Wind Lanes   (status: Draft)
Fantasy: Stepping off a ledge is a promise. The wind catches you, and the next island rushes up to meet you.

## Rules
1. At a marked launch ledge, walking off the edge or pressing the glide prompt deploys the glider. Gliding is traversal, never a consumable: no stamina cost, no meter for staying aloft.
2. A glide descends at the sink rate while moving forward at glide speed. Drag input (MECH-001) steers heading within the turn rate.
3. Wind lanes are authored volumes between islands. Inside a lane the glider gains lift and speed along the lane direction. Lanes are visible as drifting leaves and light ribbons, readable before entry.
4. Lane travel charges a boost gauge. The boost button spends gauge on a speed burst.
5. Touching any island surface lands the hero and ends the glide. Touching open cloud sea triggers a rescue: the hero returns to the last ledge with no loot loss, because falling is a navigation error, not a combat failure.

## Tuning Constants  (file: Tuning ScriptableObject set, group Glide)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| glide_speed_mps | m per s | 9 | 7 to 12 | Base forward speed |
| sink_rate_mps | m per s | 1.6 | 1.2 to 2.2 | Descent outside lanes |
| turn_rate_deg_s | deg per s | 90 | 60 to 130 | Steering authority |
| lane_lift_mps | m per s | 3.5 | 2.5 to 5 | Climb inside a lane |
| lane_speed_mult | ratio | 1.3 | 1.15 to 1.5 | Speed inside a lane |
| boost_gauge_max | points | 100 | fixed | Filled by lane travel |
| boost_cost | points | 40 | 30 to 50 | Per burst |
| boost_speed_mult | ratio | 1.5 | 1.3 to 1.7 | Burst strength |
| boost_duration_s | s | 1.2 | 0.8 to 1.6 | Burst length |
| boost_charge_per_s | points per s | 25 | 15 to 40 | Gauge gained per second inside a lane. Added 2026-11-22 during the first code pass, when the controller needed the rate the rules describe; recorded here so spec and code stay one source |
| deploy_delay_s | s | 0.2 | 0.1 to 0.35 | Air time while falling before the glider opens. Named here from the deploy rule so the code constant has a spec home |

## Edge Cases
- Player steers out of a lane mid crossing: normal sink resumes; the lane can be entered again.
- Player lands on a ledge too small to stand on: slide to the nearest standable surface on that island.
- Boost pressed with gauge below cost: nothing happens, gauge pulses as feedback.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | The hub to island one lane crossing completes on the median steering line with at least 15 percent of starting altitude remaining | scripted flight | agent |
| AC-2 | Outside a lane, altitude loss per second equals sink_rate_mps within 5 percent | headless test | agent |
| AC-3 | Boost spends exactly boost_cost and applies boost_speed_mult for boost_duration_s | unit test | agent |
| AC-4 | Cloud sea rescue returns the hero to the last ledge with inventory unchanged | scripted test | agent |
| AC-5 | Four of five new testers reach island one within two attempts, unprompted | playtest | human |
| AC-6 | Testers describe the glide as the best part of the slice, or name it unprompted when asked what they remember | playtest | human |

## Kill Criteria
If AC-5 fails after lane width and lift tuning, crossings become assisted: steering stays, sink pauses inside a generous corridor around the lane. If AC-6 fails after a feel pass on speed, sound, and camera, the glide gets a dedicated redesign before any content is built, because the glide is the fantasy the whole game sells.
