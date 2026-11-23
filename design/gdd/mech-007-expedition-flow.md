# MECH-007 Expedition Flow   (status: Draft)
Fantasy: Out light, home heavy. Every expedition is a small story with a door at both ends.

## Rules
1. An expedition begins at the hub map: the player picks an island, sees its creatures and its named goal (its elder and its chest), and glides in on the wind lane (MECH-002).
2. The island chest spawns when the elder is harvested. Opening it grants the chest shards and completes the expedition goal.
3. A return portal stands at the landing point from the moment the player lands. Retreating is always available, always free, and never shamed by the interface. Leaving banks everything carried.
4. If the player leaves with a defeated creature unharvested, the banking screen names what was left behind before the portal confirms. Informed choices only.
5. Failure: at zero health the hero wakes at the hub. Banked items stay banked. Of the shards carried on that expedition, half are kept. Parts carried are lost. This asymmetry is the expedition tension: shards are money, parts are the hunt, and the hunt is what you protect by retreating.
6. An expedition has a soft guidance cap, not a timer. Nothing expires, nothing counts down, and no offer is attached to the cap. Timers on play are an anti pillar.

## Tuning Constants  (file: Tuning ScriptableObject set, group Expedition)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| chest_shards_min | shards | 60 | 40 to 80 | Island one chest |
| chest_shards_max | shards | 90 | 70 to 120 | Island one chest |
| failure_shard_keep_pct | percent | 50 | 40 to 60 | Of shards carried this expedition |
| soft_cap_min | min | 8 | 6 to 10 | Guidance for island size, never shown as a countdown |
| islands_in_slice | count | 2 | fixed | Plus the hub, per the slice contract |

## Edge Cases
- Player idles on an island indefinitely: nothing punishes it. Creatures respawn on their graze cycles; the elder does not.
- Player opens the chest, then dies before returning: the chest shards count as carried and follow the failure rule. The banking screen explains the outcome in one line.
- Player retreats mid fight: combat ends, creature health resets on the next visit, harvested and defeated state persists for that visit only.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | A full loop, hub to island to elder to chest to banked, completes in a scripted run with every balance matching the ledger | scripted test | agent |
| AC-2 | Retreat at five different moments banks exactly what was carried at that moment | scripted test | agent |
| AC-3 | Failure banks half the carried shards and no carried parts, across 100 trials | scripted test | agent |
| AC-4 | Leaving with an unharvested creature always shows the warning before the portal confirms | scripted test | agent |
| AC-5 | Testers can explain the retreat bargain in their own words after one failure or one retreat | playtest | human |

## Kill Criteria
If testers report that failure feels confiscatory rather than fair, the shard keep rate rises before anything else moves, because loss aversion at twice the weight of gains is a known constant of the audience, not a tuning opinion.
