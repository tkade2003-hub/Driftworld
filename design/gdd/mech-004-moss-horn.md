# MECH-004 The Moss Horn   (status: Draft)
Fantasy: A walking hillside with a temper. It warns you before it charges, and the warning is the game.

## Rules
1. States: graze, notice, threaten, charge, recover, staggered, defeated. Transitions are driven by range, line of sight, damage, and the stagger pool.
2. Every charge is preceded by the threaten state for the full tell duration: head down, pawing the ground, a rising tone. No charge ever fires without its tell. This is a hard rule, not a tuning value.
3. Stagger: strikes and heavy strikes add stagger points. At the threshold the creature enters staggered for the stagger duration, taking multiplied damage. Hitting the creature during its tell adds bonus stagger; reading the tell is the skill the game teaches.
4. Wind weakness: the wind spear skill (MECH-003) deals multiplied stagger to the Moss Horn.
5. The elder variant shares the rig, the clips, and the state machine. It adds one move, the horn slam, with its own tell, plus scaled health and damage. Variants and elders reuse the base creature by design, per the feasibility limits in the concept document.
6. On defeat the creature becomes harvestable (MECH-005) and stays in the world until harvested or the expedition ends.

## Tuning Constants  (file: Tuning ScriptableObject set, group MossHorn)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| hp_common | points | 260 | 200 to 340 | Set against MECH-003 targets |
| hp_elder | points | 1400 | 1100 to 1800 | Set against MECH-003 targets |
| charge_damage | points | 18 | 12 to 26 | Against hero health of 100 |
| slam_damage_elder | points | 26 | 18 to 34 | Elder only |
| tell_duration_s | s | 0.9 | 0.7 to 1.2 | Never below 0.7 |
| charge_speed_mps | m per s | 11 | 9 to 14 | Dodgeable by a sidestep at move speed |
| stagger_threshold | points | 100 | 80 to 130 | Strike adds 8, heavy adds 22, skill adds 30 |
| stagger_duration_s | s | 3.5 | 2.5 to 4.5 | The punish window |
| stagger_damage_mult | ratio | 1.5 | 1.3 to 1.8 | Damage taken while staggered |
| notice_range_m | m | 14 | 10 to 18 | Graze to notice |

## Edge Cases
- Charge meets a cliff edge: the creature stops at the edge and enters recover. It never falls; a falling creature cannot be harvested.
- Player leaves notice range during threaten: the creature returns to graze after the tell completes, and the stagger pool decays to zero.
- Two creatures notice at once: only the nearer one charges; the other circles. The slice never asks a new player to read two tells at once.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | In 500 simulated charges, every charge is preceded by a full tell, zero exceptions | scripted test | agent |
| AC-2 | A sidestep at base move speed, started at tell start, avoids the charge in 100 of 100 trials | scripted test | agent |
| AC-3 | Stagger triggers exactly at threshold and ends after stagger_duration_s | unit test | agent |
| AC-4 | Elder uses the same state machine with the slam added, verified by state trace comparison | scripted test | agent |
| AC-5 | Testers say the charge felt fair when it hit them, in four of five interviews | playtest | human |

## Kill Criteria
If AC-5 fails, the tell grows before damage falls. If the elder reads as a copy rather than a threat in testing, the slam and the scale change stay and the elder gains a second phase at half health before any new creature is commissioned.
