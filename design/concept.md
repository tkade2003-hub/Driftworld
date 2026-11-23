# Game Concept: Driftworld
Date: 2026-11-22   Designer: Vashon Lorona Gonzales   Mode: Full session
Genre: 3D action exploration   Platform: Mobile first, iOS and Android   Team: Solo designer with coding agents building
Engine: Unity

## Pitch
Two sentences: Floating islands drift above a sea of cloud, and every island hides creatures whose parts become your gear. Glide out, hunt, harvest, and come home stronger, one short expedition at a time.
Target emotion: Wonder, with greed close behind it.
Unique hook: The harvest is the loot. Nothing drops from a menu or a box; every gear piece starts as a part you cut from a creature you found somewhere worth going.

## Originality
Closest existing games: Monster Hunter (hunt and harvest crafting), Genshin Impact (gliding exploration), Archero (short mobile action runs).
What is different: Expeditions are instanced island trips measured in minutes, not an open world. Traversal is three verbs only. Gear power comes only from harvested parts, never from a store.
Assets: Licensed packs and generated assets, each logged with its source before it ships.

## The feasibility limits (adopted 2026-11-22)
1. One island cluster at launch: a hub island plus expedition islands, each compact and instanced. Travel happens by expedition, never by walking one continuous map. No world streaming.
2. Islands are assembled from one modular kit of authored pieces, so ten islands come from one kit.
3. One biome at launch. Lighting and palette shifts do the variety work.
4. Three movement verbs only: run, glide, grapple. Glide lanes ride wind currents between islands. No climbing system, no swimming, no free flight physics.
5. Four or five creatures at launch, each with behavior variants and elder forms. Parts feed several gear pieces each, so the roster stays small while the crafting stays deep.
6. Single player. Social play is leaderboards and shared goals, never live multiplayer.
7. The world grows by season, one new island per season from the same kit, after revenue exists to fund it.

## Core loop
30 second loop: Move, strike, harvest.
- Player action: Drag to move, tap to strike, hold on a fallen creature to harvest.
- Feedback: Hit flash and damage numbers on the strike, parts and coins rising off the harvest with sound and a rising loot counter.
- Decision point: Which creature to engage, and whether to spend stamina on the heavy strike.
- Mastery gradient: A skilled player reads the creature tell, dodges through the attack, and harvests without taking a hit.

Five minute loop: One expedition.
- Setup: Pick an island, glide the wind lane in, land with a goal in view.
- Escalation: Creatures grow stronger toward the island heart, where the elder form waits.
- Resolution: Harvest the elder, open the island chest, glide home.
- Carry over: Parts and shards banked at the hub. A failed expedition keeps half the shards and none of the parts, so retreat is a real choice.

Session loop: About twenty minutes, three or four expeditions.
- Start hook: The next gear piece is one part away, and the island that holds it is named on the map.
- Natural stop: A completed gear piece, or the island set for the day finished.
- Return hook: Tomorrow brings a rotated island with a part the player still needs.

Progression loop: Gear sets and farther islands.
- Growth axes: Gear sets crafted from parts, glider upgrades that open longer wind lanes, hub buildings that unlock new crafting.
- Curve: Logarithmic on gear power, because early pieces should land fast and late pieces should feel earned.
- Milestone cadence: A new island cluster roughly every week of regular play.
- Absence recovery: Returning players find a welcome back harvest bundle of shards, never parts, so the hunt still matters.

## Controls
- Movement: Touch and drag anywhere on the left half of the screen. Nothing is drawn while the thumb is up. A soft ripple shows where the thumb rests. No fixed joystick anywhere.
- Combat: Buttons on the right for strike, guard, and the spear skill. All touch targets at least 44 points.
- Camera: Auto frames the creature being fought. A drag on the upper screen nudges it.
- Gliding: The same drag steers. One scheme covers walking and flying, so the player learns movement once.

## First session (first session value contract)
- Variant: fs_v1. Audience: new players from the store page.
- Promise kept in the first three minutes: Glide to an island, beat one creature, harvest it, and watch a gear piece craft from its part at the hub.
- No signup, no offer, and no wall before that promise lands. The first store surface appears only after the first crafted piece exists.
- Evidence to collect: The minute where new players quit, and the share who finish the first craft.

## Pillars and anti pillars
| Pillar | Design test |
|---|---|
| Every island can be crossed in under three minutes | If a layout takes longer, cut the layout, not the player speed |
| Every creature part crafts something a player can name | A part with no named use does not ship |
| The next goal is always visible | Any screen that hides the next part or island fails review |
| No purchase ever raises a combat stat | Any offer that touches power is rejected on sight |

Anti pillars: No energy timers. No paid randomness of any kind. No open world. No interstitial ads. No countdown pressure on offers.

## Player fit
Primary motivations: Explorers first, Achievers second. Competence from reading creatures and crafting builds, autonomy from choosing which island and which set to chase, relatedness from leaderboards and shared season goals.
Not for: Players who want a vast open world or live multiplayer hunts, because the pillars prioritize short complete expeditions.

## Economy (summary; full model gets simulated before build)
Business model: Free to play. Primary revenue is cosmetics and the season pass. Secondary revenue is optional rewarded ads. One primary plus one secondary, never more.
| Currency | Purpose in one sentence | Main faucets | Main sinks | Earned or purchased |
|---|---|---|---|---|
| Shards | The everyday crafting and upgrade money | Expeditions, daily goals, welcome back bundle | Gear upgrades, crafting fees, hub buildings | Earned only |
| Aether | The style money | Purchase, small amounts inside the paid pass track | Cosmetics, glider skins, emotes | Purchased |
| Creature parts | The crafting material that makes the hunt matter | Harvesting | Gear crafting | Earned only, never sold |

Monetization rules:
- Cosmetics and glider skins are bought directly at a stated price in real money next to every price. No boxes, no odds, no randomness that money touches. This keeps the game clear of the paid random item ratings that begin at PEGI 16 under the June 2026 criteria.
- Season pass at 9.99 dollars per season, free and paid lanes, completable at roughly an hour a day, carrying cosmetics and Aether, never parts or power.
- Rewarded ads are optional and player started: a bonus shard cache after an expedition, or one harvest reroll. Never interstitial, never required for progress.
- Starter pack at 4.99 dollars, one time, cosmetics plus Aether.
- Premium currency never buys competitive advantage. Parts are never sold at any price, because selling the hunt would end the game.
- Store fees are priced into every forecast at the reduced indie rates: enrol in the Apple App Store Small Business Program and the Google Play Games Level Up program before launch, and plan on a blended 20 percent store fee rather than the standard 30 percent.

## The first build (slice contract)
Slice ID: driftworld_slice_1, revision 1.
Contents: The hub island, two expedition islands from the kit, one wind lane, one creature plus its elder variant, the full harvest and craft loop, one craftable gear set of three pieces, drag movement, and the glide.
Scope exclusions: No store, no pass, no ads, no second biome, no other creatures. Monetization enters only after the slice proves fun.
Acceptance evidence: A natural start to finish run on a phone, recorded. Five outside testers play unprompted. The slice passes when at least four of five choose to run a second expedition without being asked, and the first craft lands inside three minutes for a new player.
Exit criteria: Pass means expand content. Fail twice after tuning means the combat or the glide gets redesigned before any new content is built. This gate is strict by direction of the market research: a failed slice stops all content work, full stop, because the base rate for new titles leaves no room for building volume on top of a weak first session.
Build route: Cloud only, adopted 2026-11-22. No machine on this project runs Unity locally, not the founder computer and not the agent machine. The full pipeline is specified in design/gdd/slice-1-build-order.md.

## Scope tiers
Must: The slice above, plus three more islands, four creatures total, gear crafting, daily goals, the store with cosmetics, and the season pass for launch. All player facing text is externalized from the first build, so localization never needs rework.
Should: Japanese localization, planned from the start and shipped shortly after the English launch, because Japan is the richest market for this loop per the research brief. Glider skins, a second island cluster, elder hunts as a weekly event.
Could: A photo mode, guilds in name only with shared goals, a second weapon beside the spear.
Won't: Open world, multiplayer, climbing, swimming, paid randomness, energy timers, a second premium currency.

## Timeline (planning heuristics, solo builder, buffered)
Phase 1, the slice: Weeks 1 to 6.
Phase 2, launch content: Weeks 7 to 16. Islands, creatures, crafting depth, daily goals.
Phase 3, money and live systems: Weeks 17 to 22. Store, pass, rewarded ads, analytics funnels, live ops calendar.
Phase 4, soft launch: Weeks 23 to 26. One store, one region, retention and economy measured against targets before any wider release.

## Top risks
| Risk | Mitigation |
|---|---|
| Islands feel empty, and exploration dies | Every island carries a named goal, a chest, and an elder; density over size |
| Glide feels weak, and the fantasy collapses | The slice tests glide first; feel tuning gets its own pass before content grows |
| Creature animation costs explode | Four creatures, variants and elders reuse rigs and clips |
| Nobody finds the game | Market research runs before Phase 2 spending; store page and short clips lead with the glide and the harvest |
| Scope creep toward an open world | The anti pillars are written down, and this document is the reference that settles arguments |

## Success targets and gates (reset by market research, 2026-11-22)
Pass gates for soft launch, set at top quarter benchmarks (GameAnalytics 2025 figures, sourced in the research brief): day one retention at or above 30 percent, day seven at or above 6 percent. The original targets, day one at or above 40 percent and day seven at or above 20 percent, stand as stretch goals, because they describe a top tenth product. First craft inside three minutes for the median new player. Pass completion inside the season for a regular player at an hour a day. Revenue claims wait for measured analytics with cohort and window named, per the monetization skill.
Pivot conditions: if soft launch misses the pass gates after one tuning cycle, or if the first ninety days of downloads track to the low case in the research sizing, the project pivots instead of spending into Phase 2 content.

## Market research verdict (2026-11-22)
Go with changes. The brief lives at design/market-research/driftworld-2026-11-22.md and carries the sourced evidence: the hunt and harvest loop has proven current demand at scale, the revenue concentrates in Japan and in famous franchises, and the base rate for new titles is severe. Its five required changes are folded into this document: retention gates reset to top quarter benchmarks with the original targets kept as stretch goals, Japanese localization planned from the start, indie store fee programs priced into every forecast, discovery run as a staffed workstream, and the slice gate kept strict with written Pivot conditions.

## Discovery (a staffed workstream)
Discovery has hours attached, like any other workstream, because the marketing budget is near zero and being found is the single biggest risk in the research brief. The glide and the harvest are the two most clippable moments in the design, and they are the acquisition plan: short clips of a glide entry and a harvest payoff, posted on a steady cadence from the slice onward, plus a store page that leads with those same two moments in its first screenshot and its preview video. The workstream owns the clip calendar, the store page, and a simple weekly count of clip views, store page visits, and installs, so the team can see whether discovery is working before launch rather than after.

## Next steps
1. Market research: done 2026-11-22, verdict Go with changes, brief at design/market-research/driftworld-2026-11-22.md.
2. Build the slice to the contract above, glide and combat feel first, against the mechanic specs in design/gdd/.
3. Simulate the economy from this model before Phase 3, with archetype projections for casual, regular, and dedicated players.
