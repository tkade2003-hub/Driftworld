# MECH-006 Crafting and the First Gear Set   (status: Draft)
Fantasy: The bench at home turns a horn and a pile of hide into the reason for the next expedition.

## Rules
1. Crafting happens at the hub bench. It is instant, it always succeeds, and its full cost and full stat result are shown before the player confirms. No crafting randomness exists anywhere in the game.
2. The first set has three pieces, matching the harvest screen in the concept frames: the Talon Cloak, the Harvester Spearhead, and the Grounded Amulet. Each piece costs named parts plus shards (currency rules live in the concept document economy section).
3. Crafting deducts parts and shards as one atomic transaction. If any part of the cost is missing, nothing is deducted and the missing item is named, with the island that holds its creature.
4. A crafted piece equips from the same screen in one tap. Equipping is free and reversible forever.
5. The collection screen lists every part, its source creature, and what it crafts, including the horn pity counter from MECH-005. A part with no named use does not ship; that is a pillar, and this screen is where it is enforced.

## The First Set  (costs and stats are tuning defaults)
| Piece | Part cost | Shard cost | Stat result |
|---|---|---|---|
| Talon Cloak | Hide 6 | 40 | Evasion up 12 percent |
| Harvester Spearhead | Horn 1, Hide 3 | 60 | Strike damage up 8 |
| Grounded Amulet | Meat 4, Hide 4 | 30 | Health up 25 |

## Tuning Constants  (file: Tuning ScriptableObject set, group Crafting)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| first_craft_target_min | min | 3 | 2 to 4 | New player, start to first crafted piece |
| expeditions_to_spearhead | runs | 3 | 2 to 4 | Median player, includes one elder horn at pity pace |
| set_completion_target | sessions | 2 | 1 to 3 | Sessions to finish the set |

## Edge Cases
- Player crafts a piece already owned: the bench offers an upgrade path in later phases; in the slice, the piece shows as owned and cannot be crafted twice.
- Player spends the shards a piece needed on an upgrade (later phases): the bench names the shortfall and the fastest source, never a store link. Shards are earned only, and the bench respects that.
- Save restored from an older version: crafted pieces and the pity counter survive; both are durable state, not session state.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | A scripted new player route reaches the first craft inside first_craft_target_min | route timing | agent |
| AC-2 | A failed craft deducts nothing and names the missing part and its source island | scripted test | agent |
| AC-3 | Median simulated player completes the set inside set_completion_target sessions | economy sim | agent |
| AC-4 | Every part in the drop tables appears in the collection screen with a named use | data audit | agent |
| AC-5 | Four of five testers can say what they will craft next and what it costs, unprompted, after one session | playtest | human |

## Kill Criteria
If AC-1 fails, expedition one gets richer, never the bench cheaper, because the hunt is the game and the bench is its receipt. If AC-5 fails, the collection screen is redesigned before any second set is designed.
