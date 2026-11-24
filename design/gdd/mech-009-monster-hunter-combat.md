# MECH-009 Monster Hunter Style Combat (status: Built, verifying)

Fantasy: committed, weighty weapon combat against a large monster whose body
parts matter. You pick a weapon, lock on, read the tell, dodge through the
charge, and break the monster down part by part until it staggers, topples,
and falls.

## Rules

1. Weapons are data. A WeaponDefinition (ScriptableObject built in code by
   WeaponCatalog) carries power, stagger power, element, guard factor,
   special cooldown, a light combo chain, a heavy move, and a special move.
   Every AttackStep carries startup, active, and recovery seconds, damage
   multiplier, stagger multiplier, stamina cost, range, arc, step in
   distance, hit count and interval, damage kind (Cut or Blunt), stun
   multiplier, dodge cancel permission, and a hitstop value.
2. Attacks are committed. During startup, active, and recovery the hero does
   not take movement input; the step in comes from the attack data. Dodge
   cancels recovery, and startup only where the step allows it.
3. Hits resolve only during active frames. A swing gets an id; a monster can
   be hit once per hit index of that swing, never twice by the same hit.
   Hit tests are geometric tests against per part hurtbox spheres (head,
   neck, torso, both forelegs, both hind legs, tail), in the WindLane
   Contains idiom: no physics callbacks, no new stripping surface.
4. Damage runs one ordered pipeline: weapon power times step multiplier
   times part physical multiplier, plus element power times part element
   multiplier, critical hits multiply the physical part, monster state
   (staggered, stunned, knocked down) multiplies the total, and elder damage
   reduction applies last.
5. Reactions are pooled. Stagger pool triggers Staggered. Head hits add stun
   pool (scaled by the step stun multiplier, so the hammer stuns fastest)
   and trigger Stunned. Total stagger dealt feeds a knockdown pool that
   triggers Knockdown. Single heavy hits flinch the monster out of
   non committed states only; the tell (Threaten) and Charge can never be
   interrupted, preserving the MECH-004 tell guarantee.
6. Part damage accumulates per part. Crossing a part break threshold breaks
   the part (it then takes bonus damage and the monster flinches). The tail
   severs only from Cut damage crossing the sever threshold; a severed tail
   hurtbox goes inert and its visual drops.
7. Enrage and exhaustion: at half health the monster enrages once (faster
   charges, shorter tells never below the 0.7 s floor, shorter recovery).
   When enrage ends or health falls to a quarter, it is Exhausted once:
   slow, long recovery, easy to punish.
8. Dodge costs stamina, moves the hero a fixed distance in the input
   direction (backward with no input), and grants invulnerability between
   the iframe start and end. Dodge input during hitstun is buffered like
   strike input. There is no air dodge; glide ignores combat as before.
9. Lock on is one shared target. Targeting.Current is consumed by attack
   aim, the follow camera lock framing, and the HUD creature bar. The lock
   button toggles; pressing it while locked cycles to the next monster.
   Attacks snap facing to the locked target during startup.
10. The spear of MECH-003 is the Long Sword class. It keeps the Wind Spear
    skill as its special, resolved through the same part pipeline.

## Tuning constants

All values live in Assets/Scripts/Tuning/Tuning.cs (groups Combat, Dodge,
LockOn, Weapons, Parts, MossHorn) or in the WeaponCatalog factory that reads
them. No gameplay literals.

## Acceptance checks (agent verified on the shipped artifact, CDP harness)

| ID | Check | Method |
|---|---|---|
| AC-1 | Left half touch drag moves the hero; inp reads above 0 while dragging | shipped build, state bridge |
| AC-2 | A strike whiffed out of range produces zero damage events | state bridge event log |
| AC-3 | One swing produces at most one damage event per hit index per monster | event log swing ids |
| AC-4 | Head hits deal more than tail hits by the part multiplier ratio (crits off) | event log totals |
| AC-5 | Stagger state begins on the hit that crosses the stagger threshold | event log plus monster state |
| AC-6 | A monster charge during dodge iframes deals zero hero damage; the same charge outside iframes damages | hero hp in state bridge |
| AC-7 | Lock on engages, the camera frames the target, attacks hit the locked monster | state bridge plus screenshots |
| AC-8 | Weapon switch changes the active weapon and its step timings | state bridge |
| AC-9 | The slice loop still completes: kill a Moss Horn, harvest it, loot granted | state bridge plus HUD |
| AC-10 | Hammer head hits stun faster than Long Sword head hits | event log stun state timing |
| AC-11 | Part break and tail sever fire at their thresholds (Stage 3) | event log flags |
| AC-12 | Enrage begins at half health and Exhausted follows (Stage 3) | monster state in state bridge |

Human checks: combat feel, weight, and readability are Vashon's phone pass.
