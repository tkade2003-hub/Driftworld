# MECH-005 Harvest   (status: Draft)
Fantasy: The kill was the question. The harvest is the answer, and it rains parts and coins.

## Rules
1. A defeated creature (MECH-004) shows a harvest prompt. Holding the prompt for the harvest duration completes one harvest. Each creature can be harvested once.
2. Damage taken during a harvest cancels the hold without penalty. The player may start again. Nothing is lost by an interrupted harvest, ever.
3. A completed harvest grants parts from the drop table and shards. Grants are written as one transaction with a stable identity per creature, so a retry, a crash, or a repeated callback can never grant twice.
4. Bad luck protection: the elder horn carries a pity counter. If a player has harvested pity_max elders without a horn, the next elder harvest always grants one. The counter is visible in the collection screen, and the drop rates and the pity rule are published in the game info. No money touches these drops; the rule exists because fairness reads as respect.
5. Parts and shards fly to the hero with sound and a rising loot counter, matching the harvest screen in the concept frames. The loot counter is the score the player watches.

## Drop Table  (Moss Horn; rates are design targets, verified by simulation)
| Source | Grant | Rate or amount |
|---|---|---|
| Common | Hide | 2 to 3, always |
| Common | Meat | 1 to 2, always |
| Common | Shards | 8 to 14 |
| Elder | Elder hide | 3 to 5, always |
| Elder | Horn | 35 percent, pity at 3 misses |
| Elder | Shards | 40 to 60 |

## Tuning Constants  (file: Tuning ScriptableObject set, group Harvest)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| harvest_time_s | s | 1.6 | 1.2 to 2.2 | Long enough to feel like work, short enough to spam happily |
| horn_rate_pct | percent | 35 | 25 to 45 | Elder horn |
| pity_max | harvests | 3 | 2 to 4 | Misses before a guaranteed horn |
| loot_flight_s | s | 0.7 | 0.5 to 0.9 | Presentation timing for parts flying to the hero |

## Edge Cases
- Expedition ends with a defeated creature unharvested: the creature and its drops are lost. The banking screen says so in plain words before the player leaves (MECH-007).
- Harvest completes at the same moment the hero takes lethal damage: the grant stands, because the transaction committed first, and the failure banking of MECH-007 applies to shards only.
- App killed mid harvest: on return, the creature is either fully granted or fully present, never both, never neither.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | Ten thousand simulated harvests land inside 2 percent of every table rate, and the horn pity fires exactly at pity_max misses | Monte Carlo | agent |
| AC-2 | Replaying a harvest grant with the same transaction identity changes no balance | unit test | agent |
| AC-3 | An interrupted harvest grants nothing and can be restarted with no state error | scripted test | agent |
| AC-4 | The kill, relaunch, and return path leaves each creature either granted or present, verified across 100 trials | scripted test | agent |
| AC-5 | Testers name the harvest as a reason to fight another creature, unprompted, in three of five interviews | playtest | human |

## Kill Criteria
If AC-5 fails, the harvest presentation gets the work first: more weight, more sound, clearer part identity. Drop rates move only after presentation has had its pass, because a generous table that feels flat still fails.
