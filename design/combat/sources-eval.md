# Driftworld Combat Sources Evaluation, Stage 1B

Scope: evaluation only. No code from any repository below was copied into the Unity project. Target context: Unity 6000.0.23f1, WebGL delivery, touch input, fully code built greybox, no reliance on editor heavy scene setup, only a handful of combatants in a fight, Monster Hunter style combat as the design goal.

Method: shallow clones into `design/combat/repos/` on 24 Nov 2026, plus web verification for the repository that failed to clone and for Jolt Physics. Dates below are commit dates shown by the clones or by the GitHub pages fetched on that day.

## Clone sizes

| Repo folder | Cloned size |
|---|---|
| CGCombatFramework | 6.8 MB |
| soulslike | 760 KB |
| ThirdPerson | 477 MB |
| EGamePlay (given URL) | 0, clone failed |
| Total on disk for `repos/` | 484 MB |

ThirdPerson accounts for almost all of the total. Its size is environment art, textures, skyboxes and models, not combat code. It has only 36 C# files. CGCombatFramework has 366 C# files in 6.8 MB. soulslike has 37 C# files in 760 KB.

## 1. CGCombatFramework

URL: https://github.com/Contradictive-Games/CGCombatFramework

### Existence and activity

Exists and is very active. Latest cloned commit: 5 Nov 2026, `Release v0.3.1`. The changelog shows v0.1.0 on 12 Oct 2026 and v0.2.0, v0.3.0 and v0.3.1 all within the following month, each carrying multiple breaking changes. It is a young and fast moving package, not a settled API.

### License

License file: `LICENSE.md`. License type, quoted: "MIT License", copyright Contradictive Games. No GPL, LGPL, noncommercial or extra restriction found. Clean for commercial reuse with attribution of the copyright notice.

### Unity version and dependencies

`package.json` states Unity `6000.0`, version 0.3.1, and a single dependency: `com.unity.modules.physics` at 1.0.0. README badge states Unity 6000.0 plus. This is the only candidate that matches Driftworld at Unity 6000.0.23f1 on its stated requirement. No Input System, Cinemachine, URP or networking package is required. Render pipeline agnostic, since it ships no visuals.

### Setup model

Mixed, and this is the central tension. The runtime is headless by design: combat runs with no visuals and works in tests and on servers. Units, stats, resources, status effects, damage and firables (its word for weapons and abilities) are composed from small components and interfaces. That part fits code driven construction well.

The authoring model does not. The README headline is that everything is built in the inspector. Definitions are `GameDefinition` ScriptableObject assets assembled with `SerializeReference` building blocks, supported by a large editor suite: Combat Studio, Database, Combat Doctor, Pipeline Tester, Firable Lab, Effect Timeline and more under `Editor/`. Driftworld builds everything in code, so adopting it as shipped means either creating and wiring definition assets in the editor, which breaks the code built rule, or constructing ScriptableObjects and serialized reference block lists in code, which is possible in Unity but fights the intended workflow and loses the editor tooling that is much of the value.

### Known issues and unfinished areas

From the README roadmap and Known Limitations section, plus the changelog:

1. Single threaded.
2. Homing projectiles find targets by scanning the unit registry, and large counts may cause performance spikes. Irrelevant at Driftworld scale.
3. Netcode hooks are still on the roadmap, listed as next. No networking today.
4. Package samples are still on the roadmap: presentation examples and a combat sandbox are not yet shipped.
5. API stability is the real risk. Breaking changes landed in v0.2.0, v0.3.0 and v0.3.1, including removed public events, renamed event fields and changed component lifecycle calls. Pinning a version and never updating is workable; tracking upstream is not.
6. Strengths worth noting: about 450 automated tests claimed on the README badge, with EditMode, PlayMode and performance suites in `Tests/`, and allocation tests that fail a run if a hot path allocates. Units, projectiles, status effects and hit collections are pooled.

### Genuinely reusable components and patterns

Named from `Runtime/`:

1. Damage pipeline under `Runtime/Health/Damage/`, used through `HealthComponent` and `IHealth`: an ordered list of damage steps (mitigation, shield absorb, immunity by tag and similar) that a hit passes through. This maps directly onto Monster Hunter style needs such as part damage, elemental resistance and hitzone multipliers.
2. Status effects under `Runtime/StatusEffects/`: `StatusEffectDefinition`, `StatusEffectInstance`, `StatusEffectsComponent`, `StatusEffectPool`, plus built in behaviours and triggers in `BuiltinBehaviors/` and `BuiltinTriggers/`. Stacking modes, tick payloads and trigger conditions are the pattern to copy for poison, paralysis, stun buildup and part breaks.
3. Stats and resources under `Runtime/Stats/` and `Runtime/Resources/`: `Stat`, `StatBlock`, `StatModifier`, `Resource`, `ResourceComponent` and regen rules. A clean model for health, stamina and sharpness style resources.
4. Queries and payloads under `Runtime/Core/Queries/` and `Runtime/Core/Payloads/`, with `HitCollector` and `HitData` in `Runtime/Context/`: sphere, box, cone, chain and unit queries that produce hit candidates for a payload. A good model for weapon hitbox sweeps.
5. Firables under `Runtime/Firables/`: `Firable`, `FirableDefinition`, `FirableLoadout`, fire modes and fire gates (cooldown, charges, ammo). The gate idea fits weapon combo finishers and item use.
6. Events under `Runtime/Events/`: `EventBus` with struct events passed by reference, plus `Unit`, `UnitHandle`, `UnitRegistry` and pooled spawning under `Runtime/Units/`. The versioned handle for pooled units is a sound pattern for a greybox that will pool monsters and effects.

### Verdict

Adapt patterns only. Reason: the license and Unity version are perfect and the runtime design is the best of the four, but the package is at version 0.3.1 with breaking changes every release, and its authoring workflow is inspector and ScriptableObject centred, which contradicts the fully code built constraint. Take the damage pipeline order, the status effect stacking and trigger model, and the stat and resource model as designs, and write a small Driftworld owned version sized for a handful of combatants.

## 2. soulslike (TeaYololo)

URL: https://github.com/TeaYololo/soulslike

### Existence and activity

Exists. The shallow clone shows a single commit, dated 5 Feb 2026, titled as an initial release, by Toprak Eren Akpinar. README status badge says Work In Progress. There is no release history or update trail to rely on.

### License

License file: `LICENSE`. License type, quoted: "Apache License, Version 2.0". Copyright Toprak Eren Akpinar. Clean for commercial reuse, with the Apache patent grant and notice conditions. No red flag.

### Unity version and dependencies

README states Unity 2022.3 LTS or newer. It requires FishNet networking from the Asset Store for its multiplayer support, and it requires the user to supply their own animation set, naming paid Asset Store packs as recommendations, because no animations are included for licensing reasons. Configuration is through a `CombatSettings` ScriptableObject. Driftworld is single player WebGL, so FishNet is dead weight, and Unity 6000 compatibility is untested by the author.

### Setup model

Scene and animator based. The quick start is: open the cloned project, import FishNet, import an animation set, set up the Animator following a setup guide, then add `CombatStateMachine`, `PlayerCombat`, `PlayerStaminaSystem` and an Animator with a proper controller to the character. That is prefab, animator controller and ScriptableObject setup, the opposite of code built. Nothing in the repo shows a code only construction path.

### Known issues and unfinished areas

The repo is unusually honest. `KNOWN_ISSUES.md` lists 13 issues, including three marked Critical:

1. Dual state tracking desync between `PlayerCombat._currentState` and `CombatStateMachine.CurrentStateType`, which can produce unpredictable behaviour when hit during an attack and then blocking.
2. Parry timing mismatch: `PlayerCombat.cs` uses a hardcoded 300 ms parry window while `DefenseSystem.cs` uses 180 ms, so parry success is inconsistent.
3. Missing `AnimatorHash` constants for `IsStunned` and `Destroy`, which are used but not defined.
4. High priority: network state desync, dodge invincibility frames unreliable at high latency, and a stamina race condition that can drive stamina negative and allow attacks without stamina in multiplayer.
5. Medium and low: string animator lookups in network code, duplicated helper patterns across seven or more files, legacy animator code, 18 unused animator hash constants, more than 70 magic numbers, a `CameraController.cs` of 1,391 lines described as a god class, and no unit tests at all.

### Genuinely reusable components and patterns

Named from the README architecture and `Assets/SoulslikeCombat/Scripts/`:

1. The eight state combat model in `Combat/StateMachine/`: `CombatStateMachine`, `CombatStateFactory`, `ICombatState`, `CombatStateBase`, and states for Idle, Attacking, Blocking, Dodging, Staggered, Recovering, UsingSkill and Dead. This state list is almost exactly the player state list Driftworld needs.
2. `Player/Combat/PlayerComboSystem.cs` for light and heavy attack chains, and `Combat/Core/AttackExecutor.cs` for attack execution.
3. `Combat/Core/DefenseSystem.cs` for block and parry resolution, and `Combat/Core/HitReactionSystem.cs` for hit reactions.
4. `Player/Core/PlayerStaminaSystem.cs` plus the stamina cost table in `CombatSettings`: light attack 15, heavy attack 25, dodge 20, dodge duration 0.6 s, invincibility starting at 0.05 s and lasting 0.35 s, parry window 0.18 s. Those default numbers are a sensible starting tuning set for a greybox.
5. The poise idea: a poise value that, when broken, forces the Staggered state. Directly reusable for monster stagger and part break thresholds.

### Verdict

Adapt patterns only. Reason: the license is clean and the combat feel vocabulary is the closest match to the target game, but the code itself ships with documented critical desync and timing bugs, depends on an animator controller, a supplied animation set, ScriptableObjects and FishNet, and has no tests and a single commit of history. Borrow the state list, the combo chaining idea and the tuning defaults. Write the implementation fresh with one source of truth for state.

## 3. ThirdPerson (mangelsr)

URL: https://github.com/mangelsr/ThirdPerson

### Existence and activity

Exists and is active as a personal prototype. Latest cloned commit: 7 Sep 2026, a project settings and URP configuration change. It reads as one developer course style project that is still being tinkered with, not a maintained framework with releases.

### License

Red flag. There is no LICENSE, LICENCE or COPYING file anywhere in the clone, at the top level or below it. A missing license means no right to reuse: default copyright applies and the code may not be copied, modified or distributed in Driftworld. This alone disqualifies adoption regardless of code quality. Reading it to understand general state machine ideas is one thing; lifting files or code is not permitted.

### Unity version and dependencies

The README states Unity 2022.3.62f2 and URP, with Cinemachine 2.10.5, Input System 1.14.2, TextMeshPro 3.0.9 and AI Navigation 1.1.7. The project itself tells a different story: `ProjectSettings/ProjectVersion.txt` in the clone says 6000.3.22f1, and `Packages/manifest.json` lists URP 17.3.0, Cinemachine 2.10.7, Input System 1.20.0 and AI Navigation 2.0.14. So the project was moved to Unity 6.3 and the README was not updated. For Driftworld at 6000.0.23f1 that means an untested version gap in the other direction, plus a hard URP commitment. If the parallel audit lands on the Built in render pipeline, this project does not transfer at all.

### Setup model

Fully scene, prefab and animator based. It ships 6 scenes, 23 prefabs and 2 animator controllers, with `SampleScene.unity` as the entry point, a `Player.prefab`, `Enemy.prefab` and `PlayerRig.prefab`, a generated `Controls.cs` from a `Controls.inputactions` asset, and large third party environment packs (Fantasy Forest Environment, Fantasy Skybox). Almost all of the 477 MB is that art. None of it fits a code built greybox, and the environment assets carry their own unknown licensing on top of the missing code license.

### Known issues and unfinished areas

1. No license, as above.
2. README and project version disagree, as above, which signals low maintenance discipline.
3. No tests, no changelog, no releases, no known issues list.
4. Includes a `Ragdoll.cs` and ledge hanging and pull up states that Driftworld does not need, while its combat is thin: one `Attack.cs`, `WeaponDamage.cs` and `Health.cs`.
5. Input is built on the new Input System action asset and Cinemachine, both of which would have to be added to Driftworld and then driven from code.

### Genuinely reusable components and patterns

Because there is no license, nothing here is reusable as code. As general patterns to reimplement independently, the useful ideas are:

1. `Assets/Scripts/StateMachines/StateMachine.cs` and `State.cs`: a small generic state machine base shared by player and enemy, with player states including `PlayerFreeLookState`, `PlayerTargetingState`, `PlayerAttackingState`, `PlayerBlockingState`, `PlayerDodgingState`, `PlayerParryState`, `PlayerImpactState`, `PlayerJumpingState`, `PlayerFallingState` and `PlayerDeadState`.
2. `Assets/Scripts/Combat/Targeting/Targeter.cs` and `Target.cs`: a targeter that holds a current target and a target component that registers and deregisters, the standard lock on pairing.
3. `Assets/Scripts/ForceReceiver.cs`: a single component that accumulates external forces such as knockback and applies them to movement, which keeps hit reactions out of the state code.
4. `Assets/Scripts/InputReader.cs`: an intermediary that turns Input System events into plain C# events the state machine subscribes to. The same intermediary shape works for touch input.

### Verdict

Reject. Reason: no license, so the code cannot legally be used, and it is also a scene, prefab, animator, Cinemachine and URP based prototype whose bulk is third party environment art. Note the four pattern ideas above and implement them independently if they are wanted.

## 4. EGamePlay (given URL: egametang/EGamePlay)

URL given: https://github.com/egametang/EGamePlay

### Existence and activity

The given URL does not exist. The clone failed with a git credential prompt, which is what git reports for a private or missing repository, and a direct fetch of that URL on 24 Nov 2026 returned HTTP 404. Web verification found the real project under a different owner: https://github.com/m969/EGamePlay, which describes itself as a flexible combat and skill framework based on the Entity Component pattern, with configuration by ScriptableObject or Excel. That repository is public, shows 274 commits, about 2,411 stars and 546 forks, reports version 3.0 as current with 2.0 and 1.0 on separate branches, and is not archived. Its documentation and wiki material indexed by DeepWiki was last crawled about a year before this evaluation, so a precise recent commit date could not be confirmed from web sources. The confusion is understandable: EGamePlay references egametang/ET as its server framework, and the given URL appears to have merged the two projects into one address that never existed.

Because the assigned URL is wrong, no clone of EGamePlay exists in `repos/`. The assessment below is from the GitHub page and documentation of m969/EGamePlay fetched on 24 Nov 2026, not from local code inspection.

### License

For the real project, m969/EGamePlay, GitHub reports "MIT License (MIT)" and the repository root contains a `LICENSE` file. On that basis the framework code itself is reusable. Two cautions: first, that finding applies to the m969 repository, not to the dead URL in the brief, so the correct repository should be confirmed before any use. Second, the mandatory and optional paid plugins below have their own commercial licenses and are not covered by the MIT license. No GPL or LGPL flag was found in the framework itself.

### Unity version and dependencies

Stated Unity version: 2022.3.53f1. Dependencies are the disqualifying part:

1. Odin Inspector is mandatory, not optional. The documentation calls it a hard requirement for all integration paths, used for serialization and editor tooling throughout, and it must be installed before EGamePlay. Odin is a paid Asset Store plugin.
2. DOTween Pro and Animancer Pro are listed by the author as paid plugins the project uses; documentation treats them as optional but recommended, with Animancer recommended for animation heavy work.
3. Version 3.0 also embeds the EcsNode framework from m969/EcsNode as its Entity Component System foundation.
4. Integration requires adding a `UNITY` conditional compilation symbol, and the ET path requires an `EGAMEPLAY_ET` symbol and ET Framework 8.1 or later.

Driftworld at Unity 6000.0 would be two major versions beyond the tested 2022.3 baseline, with a paid editor plugin as a hard dependency.

### Setup model

Editor, ScriptableObject and spreadsheet based. Making a skill means adding a row to `AbilityConfig.xlsx`, creating an ability configuration asset from a right click menu, creating an Execution asset the same way, and attaching the skill to a `CombatEntity` at runtime through a spell component. Demos are scenes: `RpgExample` and `ExecutionLinkScene`. Integration into another project means copying eight named directories by hand, and the author states plainly that integration is not yet perfect and that conflicts or missing pieces must be resolved as needed. This is the furthest of the four from code built construction, and much of the documentation is in Chinese, with support through a QQ group and Zhihu articles.

### Known issues and unfinished areas

1. Hard dependency on a paid editor plugin, Odin Inspector.
2. Author admitted imperfect integration, with conflicts to be resolved manually.
3. Tested only against Unity 2022.3.53f1; no Unity 6 support claim.
4. Version 3.0 was a large runtime rewrite around EcsNode, so older tutorials and the 1.0 and 2.0 branches describe different code.
5. The turn based demo was removed in 3.0; only the RPG demo remains.
6. Excel based configuration adds a spreadsheet toolchain and generated configuration classes that a code built project does not want.

### Genuinely reusable components and patterns

Patterns only, and only from the documented architecture:

1. The Entity Component separation itself: combat entities holding components, with systems acting on them, and business logic separated from view logic. Driftworld can use a much lighter version of this without EcsNode.
2. The ability model: an Ability with a configuration, an Execution made of timed clips that coordinate animation, effects and hit moments, and a spell component that casts it. The timed execution clip idea is a strong fit for Monster Hunter weapon moves, where hit windows sit at fixed points in a swing.
3. The numeric attribute system covered in the author article series: layered attributes with base, added and percentage values feeding combat maths.
4. The Status effect model from the same series: status definitions separate from status instances on an entity, which matches the CGCombatFramework model and confirms the shape.
5. The buff and skill configuration split between static configuration data and runtime state.

### Verdict

Reject as a dependency, adapt patterns only. Reason: the assigned URL is dead, the real project demands a paid Odin Inspector license as a hard requirement plus paid tween and animation plugins, targets Unity 2022.3 rather than Unity 6, and is configured through Excel files, editor windows and ScriptableObject assets. Its ability execution clip and attribute layering ideas are worth borrowing on paper.

## 5. Jolt Physics (assessment only, not cloned)

URL: https://github.com/jrouwe/JoltPhysics

### What it is

A C++ rigid body physics and collision library, MIT licensed, very active and credible: about 12,411 stars, 1,838 commits and 17 releases on the GitHub page fetched 24 Nov 2026, used in Horizon Forbidden West and Death Stranding 2. Its strengths are multithreaded simulation, deterministic simulation, large worlds, vehicles, soft bodies, and animated ragdolls with constraint motors. None of those strengths addresses a Driftworld need.

### Would it help a WebGL mobile browser game with a handful of combatants?

No. Concrete reasoning:

1. WebGL support is not there in the Unity path. Jolt core lists WebAssembly as a supported platform only through a separate project, JoltPhysics.js, which is a JavaScript binding for web apps, not a Unity plugin. The Unity binding found, seep/JoltPhysicsUnity, describes itself as work in progress, has no published releases and no registry package, and is built on a native `joltc` library loaded through PInvoke `DllImport`. Its build documentation lists native libraries for Windows x64, macOS x64 and Linux x64 only. Unity WebGL cannot load desktop native libraries; a WebGL build needs a library compiled for the WebGL platform and linked as a Unity WebGL plugin, and no such build is offered. So the most likely outcome of attempting it is a build or runtime failure on the actual delivery target, discovered late.
2. Binary size and download cost. Unity WebGL already ships one physics engine inside the build. Adding a second native physics engine compiled to WebAssembly adds its full compiled size to a download that mobile browser players must fetch before playing, plus the binding layer, for zero player visible gain. On mobile data connections, build size is a real product cost.
3. Double physics maintenance. Driftworld would then have two physics worlds to keep in agreement: Unity physics for character controllers, triggers, raycasts, animation driven motion and every existing greybox behaviour, and Jolt for whatever was moved over. Colliders would need to exist in both worlds or be mirrored frame by frame, hit detection would split across two systems, and bugs would present as objects that collide in one world and pass through in the other. With a code built project and no dedicated physics engineer, that is a permanent tax on every combat feature.
4. No load to justify it. Jolt advantage appears with many simultaneous dynamic bodies, large worlds, heavy multithreading and background loading and unloading of simulation islands. Driftworld fights involve a handful of combatants in a greybox arena. Unity built in physics handles a few capsule characters, weapon hitbox overlaps and ground checks without measurable strain, and its multithreading limits in WebGL apply to any engine running in a browser tab.
5. No ragdoll requirement. The one Jolt feature set that could change game feel, animated ragdolls with motors and skeleton mapping, is not a Driftworld requirement. Monster hit reactions, stagger, topple and part breaks in a Monster Hunter style game are animation and state driven, not ragdoll driven. If a topple is ever wanted, a simple scripted fall or a Unity physics ragdoll on death is enough.
6. Touch and browser constraints dominate performance anyway. Frame cost in a mobile browser build will be set by rendering, scripting and memory pressure long before physics at this body count becomes the bottleneck. There is no measurable advantage to measure.

### Verdict

Reject. Keep Unity built in physics as the single physics system. Revisit only if Driftworld gains a hard requirement that Unity physics cannot meet, such as large scale dynamic destruction, and only after a WebGL build of the binding is proven on the target.

## Summary table

| Repo | License | Unity compat | Verdict |
|---|---|---|---|
| CGCombatFramework | MIT | States Unity 6000.0, matches 6000.0.23f1, physics module only | Adapt patterns only |
| soulslike (TeaYololo) | Apache License 2.0 | States 2022.3 LTS or newer, Unity 6 untested, needs FishNet and a supplied animation set | Adapt patterns only |
| ThirdPerson (mangelsr) | No license file, no right to reuse | README says 2022.3.62f2 with URP, project files say 6000.3.22f1 with URP 17.3 | Reject |
| EGamePlay (given URL egametang/EGamePlay) | URL is 404; real project m969/EGamePlay reports MIT | States 2022.3.53f1, mandatory paid Odin Inspector | Reject, adapt patterns only |
| Jolt Physics (jrouwe) | MIT | C++ library; Unity binding is work in progress with no WebGL native build | Reject |

## Recommended sourcing strategy

Build the combat system as Driftworld owned code, borrowing designs from two of these projects and nothing else.

### Take as design, not as code

1. From CGCombatFramework: the ordered damage pipeline, status effects with stacking modes, tick behaviours and triggers, layered stats with modifiers, resources with regen rules, hit queries that return hit candidates, and pooled units with versioned handles. It is MIT licensed and Unity 6 native, so if a later stage wants to trial the actual package on a pinned version, the legal and version doors are open. Do not build on it now, because version 0.3.1 is still breaking its own API monthly and its authoring is inspector led.
2. From soulslike: the eight state player combat model, light and heavy combo chaining, poise breaking into stagger, and its published tuning defaults for stamina costs, dodge timing, invincibility frames and parry window as starting numbers for the greybox.
3. From EGamePlay, on paper only: abilities as timed execution clips so that hit windows, effects and recovery sit at authored points in a move, and attributes layered as base plus additions plus percentage multipliers.
4. From ThirdPerson, as general knowledge only and never as copied code, because it has no license: a shared generic state machine for player and monster, a targeter and target pairing for lock on, a force receiver for knockback, and an input reader that converts raw input into game events. That last shape is how touch input should enter the system.

### Write fresh

1. A small state machine with one source of truth for combat state. The soulslike desync bug is the cautionary example: state lives in exactly one place.
2. Player and monster controllers built entirely in code: no prefabs, no animator controllers, no ScriptableObjects required to run a fight. Animation, when it arrives, should be driven by the state machine rather than driving it.
3. A compact damage and status implementation sized for a handful of combatants: health, stamina, poise or stagger buildup, part health for breaks, elemental values, and a short list of status effects. A few hundred lines owned by Driftworld beats a general framework.
4. Weapon moves as data defined in code: startup, active hit window, recovery, stamina cost, poise damage, and whether the move can be cancelled into a dodge. That is the Monster Hunter feel, and none of the four projects ships it in a form that fits.
5. Lock on and camera written for touch first, with a soft lock that favours the monster in front of the player, rather than a Cinemachine and mouse and keyboard setup adapted afterwards.
6. Hit detection on Unity built in physics only: capsule characters, overlap and sweep queries for weapon arcs, and trigger volumes for monster attacks. One physics system, no Jolt.

### Why

Licensing removes ThirdPerson outright and makes EGamePlay expensive, since Odin Inspector is a paid hard dependency. Setup model removes the rest as drop in solutions: three of the four assume scenes, prefabs, animator controllers, ScriptableObjects or spreadsheets, and Driftworld builds everything in code. Maturity removes the remaining candidate: CGCombatFramework is the best designed and the only Unity 6 match, but at version 0.3.1 with breaking changes in three straight releases it is a design reference, not a foundation. What Driftworld actually needs for Monster Hunter style combat is narrow and well understood: committed attacks with real recovery, stamina, stagger and part breaks, readable monster states, and a lock on camera that works with thumbs. Writing that narrow system fresh, informed by the patterns above, is less work than bending any of these frameworks into a code built WebGL shape, and it leaves no license, paid plugin or version risk in the shipping game.
