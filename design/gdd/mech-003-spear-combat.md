# MECH-003 Spear Combat   (status: Draft)
Fantasy: Read the creature, slip its charge, and answer with the spear. Every hit lands with weight.

## Rules
1. Strike: a tap on the strike button performs one spear strike toward the faced target. Strikes chain while tapped at the strike rate.
2. Heavy strike: holding the strike button charges a heavy strike that costs stamina on release. Heavy strikes deal multiplied damage and multiplied stagger (MECH-004).
3. Guard: holding the guard button reduces incoming damage by the guard factor and prevents knockback. Guarding drains no stamina; it trades movement for safety, and the hero moves at half speed while guarding.
4. Spear skill: a dedicated button throws a wind spear at range, on a cooldown. This is the wind answer the combat screen promises against wind weak creatures.
5. Input during hitstun is buffered for the buffer window and executes on recovery.
6. Hitstop applies per actor on heavy connects only. Global time is never frozen for combat feel.

## Tuning Constants  (file: Tuning ScriptableObject set, group Combat)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| strike_damage | points | 12 | 8 to 18 | Starter spear |
| strike_rate_s | s | 0.45 | 0.35 to 0.6 | Time between strikes |
| heavy_damage_mult | ratio | 2.2 | 1.8 to 2.8 | Against strike damage |
| heavy_stamina_cost | points | 30 | 20 to 40 | Of stamina_max |
| stamina_max | points | 100 | fixed | Shared pool, heavy strikes only in the slice |
| stamina_regen_per_s | points per s | 18 | 12 to 26 | After the regen delay |
| stamina_regen_delay_s | s | 0.8 | 0.5 to 1.2 | After last spend |
| guard_damage_mult | ratio | 0.3 | 0.2 to 0.45 | Damage taken while guarding |
| skill_damage | points | 45 | 35 to 60 | Wind spear |
| skill_cooldown_s | s | 8 | 6 to 12 | Wind spear |
| hitstop_ms | ms | 45 | 25 to 70 | On heavy connect, per actor |
| input_buffer_s | s | 0.12 | 0.08 to 0.18 | Buffered inputs |

## Balance Targets
| Metric | Target | Acceptable | Method |
|---|---|---|---|
| Time to defeat, common creature, strikes only, starter gear | 21 s | 18 to 25 s | sim, 1,000 fights |
| Time to defeat, elder, mixed strikes and heavies, first set crafted | 75 s | 60 to 90 s | sim plus playtest median |
| Player deaths per first elder attempt | 0.4 | 0.2 to 0.7 | playtest |

## Edge Cases
- Strike with no target in range: the hero strikes forward; no auto lunge beyond a short step.
- Heavy charge interrupted by damage: charge is lost, stamina is not spent.
- Skill fired with no target: travels straight ahead and expires at max range.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | Simulated time to defeat lands inside the acceptable band for both creature classes | sim report | agent |
| AC-2 | Hitstop of hitstop_ms is present on heavy connects in frame captures, and absent on light strikes | frame capture | agent |
| AC-3 | Guarding a charge reduces its damage by the guard factor within 2 percent | scripted test | agent |
| AC-4 | A buffered strike during hitstun executes within one frame of recovery | scripted test | agent |
| AC-5 | Four of five testers use guard against a charge unprompted by the third fight | playtest | human |

## Kill Criteria
If AC-1 cannot be met without breaking the feel pass, the creature health values move (MECH-004), never the strike feel. If AC-5 fails twice, the charge tell gets longer and louder before any damage numbers change, because unreadable danger is a teaching failure, not a balance failure.
