# Sand Runners Handoff Report - 2026-07-06

Project path: `C:\Users\Админ\My project`

First playable build: `C:\Games\sandrunnersplaybild1`

Game: `Battle for Universe: Sand runners`

Current player feeling: the prototype has become noticeably playable. The main loop is now understandable: build and release units, collect builders back into the pyramid economy, organize logistics and defenses, then move the huge pyramid forward with an army.

## Current Game State

The project is a Unity 6 prototype centered on one huge golden battle pyramid. The pyramid is both a mobile fortress and a factory. The enemy is Mandarinka's wheeled Chinese mobile castle, with builders, raiders, air units, field turrets, and Karl Gustav siege guns.

Main scene appears to be `SampleScene`.

Important scripts are under:

- `Assets/Scripts/SandRunners/SandRunnersPrototype.cs`
- `Assets/Scripts/SandRunners/SandRunnersRTSCore.cs`
- `Assets/Scripts/SandRunners/SandRunnersMandarinkaEncounter.cs`
- `Assets/Scripts/SandRunners/SandRunnersStrategicCanvas.cs`
- `Assets/Scripts/SandRunners/SandRunnersImmersiveBattlefield.cs`
- `Assets/Scripts/SandRunners/SandRunnersDuneWorld.cs`
- `Assets/Scripts/SandRunners/SandRunnersGoldenEngineering.cs`
- `Assets/Scripts/SandRunners/SandRunnersBattleEffects.cs`
- `Assets/Scripts/SandRunners/SandRunnersCombatReadability.cs`
- `Assets/Scripts/SandRunners/SandRunnersAudio.cs`
- `Assets/Scripts/SandRunners/SandRunnersMissionDirector.cs`
- `Assets/Scripts/SandRunners/SandRunnersPyramidArtKit.cs`

## What Was Implemented

### Input and Play Mode

- Fixed old `UnityEngine.Input` usage for the new Unity Input System.
- Movement works with `WASD` / arrows.
- `Tab` switches strategy/command mode.
- `C` starts/stops following selected RTS entity.
- Mouse wheel zoom was made stronger.
- `Esc` opens pause menu.

### Main Game Loop

- Added a match flow:
  - Main menu.
  - `NEW GAME`.
  - Pause.
  - Defeat screen.
  - Victory screen.
  - Restart/main menu/quit play options.
- Scene reload in Editor uses scene path, so it should work even if the scene is not in Build Settings.

### RTS Core

- Production queue exists.
- Ground/builder squads go into ready hangar.
- `G` releases ready ground squads from the pyramid.
- Air units auto-launch.
- Left-click and drag selection exist.
- Right-click issues move, attack, harvest, or build orders depending on target.
- Selection UI exists, including selected squads and orders.
- Builders can harvest resources, return cargo, and build from blueprints.
- Resource depot and cargo flyer loop exists.
- Defensive buildings exist in v1 form: depot, 30-mm turret, mirror beam turret, Gepard AA launcher.

### Enemy and Battle

- Mandarinka fortress starts far away and has AI phases:
  - BuildUp
  - ScoutRaid
  - ResourceRaid
  - SiegeProbe
  - FortressAdvance
- Enemy unit spam was reduced with budgets/cooldowns/role limits.
- Mandarinka fortress has shield and phase behavior.
- Karl Gustav siege gun exists and can seriously hurt the pyramid.
- Right-click targeting was improved: enemy assets now get colliders, and right-click also searches for enemies near the clicked point if the ray hits terrain.

### Map and Feel

- Map is much larger.
- Player and Mandarinka start far apart.
- Resources are divided into safe, contested, and enemy-side clusters.
- There are readable resource node types:
  - sand refinery
  - buried gold condenser
  - wind obelisk
- There are grouped ruins/points of interest.
- Pyramid movement was slowed and made heavier, but still responsive enough to test.

### UI and Narrative

- RTS Canvas HUD exists.
- Game flow/menu overlay exists.
- Dialogue now has larger portrait-style speaker panels instead of only tiny text.
- Mandarinka taunts and Sebek radio lines exist.
- Victory finale exists: Mandarinka's mobile castle collapses, she escapes in an emergency rocket, and the match enters victory.

### Audio and Effects

- Runtime procedural audio exists:
  - soft engine/ambience
  - UI/build/harvest/action sounds
  - weapon/impact sounds
- Combat VFX exists:
  - tracers
  - beams
  - artillery arcs
  - impact markers
  - flashes/sparks
  - Mandarinka jade/red visual language

## Known Current Problems

### Top Priority: Camera

The player likes that follow camera exists, but it is not yet the desired camera.

Current problem:

- The camera follows the selected unit only partially.
- It still feels too much like an offset from the overall battle/pyramid camera.
- The user wants the camera to really focus on the unit or squad, so the chosen unit becomes the center of play.
- It needs closer zoom and clearer cinematic profiles per unit type.
- It must remain commandable while following: right-click orders, selection, and UI should still work.

Recommended next implementation:

- Rework RTS follow into a true target-centered camera.
- When following a selected squad, focus on the representative unit or squad centroid, not the pyramid.
- Add per-unit camera profiles:
  - Builder: low, close, work-site camera.
  - Scarab/skimmer: low fast chase camera.
  - Flyers: higher, wider air camera.
  - Heavy units: low/wide, weighty camera.
  - Fortress/pyramid: large-scale wide camera.
- Add smooth zoom range per profile.
- Keep manual orbit with RMB/MMB or keyboard.
- Keep command mode active while following.
- Add a clear HUD label: `Following: Unit Name | scroll zoom | C release`.
- Consider `F`/`C` cycling between selected squad members, centroid, and tactical overhead.

### Models

The user says Meshy models are now inside the project folder. They should be inspected and wired into the prototype next.

Recommended next implementation:

- Search project for `.fbx`, `.glb`, `.gltf`, `.obj`, `.blend`.
- Identify models for:
  - golden pyramid
  - golden units
  - builder
  - Mandarinka fortress/enemy units
  - props/ruins/resources
- Replace procedural placeholder units with imported prefabs gradually.
- Prefer a small asset catalog script/serializable references over hard-coded asset paths.
- Keep procedural fallback if an imported model is missing.

### Unit Movement and Combat

- Units can still feel jammed or primitive.
- Formation movement needs improvement.
- Attack behavior should be more reliable around big targets and siege guns.
- Need better avoidance/separation.
- Consider adding simple steering first before full NavMesh.

### UI

- The UI is now functional but still not beautiful enough.
- It should move toward a holographic ancient Egyptian command interface.
- Unit buttons, queue, selected squads, logistics, and commands need stronger visual hierarchy.

### Effects and Readability

- Effects improved, but player still wants more readable and punchy battle feedback.
- Next VFX pass should focus on:
  - bigger muzzle flashes
  - clearer hit feedback
  - persistent damage/target brackets
  - more obvious artillery warning rings
  - better scale contrast between fortress weapons and small unit fire

## Validation Status

Important note:

- A full Roslyn compile was successfully run after moving menu/dialogue code into `SandRunnersPrototype.cs`.
- After that, a tiny `Esc` input-consumption/reload safety patch was added.
- A final compile after that small patch could not be run because the approval/usage system rejected another elevated Roslyn execution.
- Before continuing, the next chat should run Unity compile/Play Mode smoke test first.

Smoke test to run:

1. Open project at `C:\Users\Админ\My project`.
2. Press Play.
3. Confirm main menu appears.
4. Press `NEW GAME`.
5. Confirm gameplay starts.
6. Press `Esc`, confirm pause/resume works.
7. Build/release a unit, select it, press `C`, test follow camera and scroll zoom.
8. Right-click Karl Gustav or enemy unit, confirm squads attack.
9. Check console for new `Error`/`Exception`.

## Suggested Prompt For New Chat

Use this prompt in the new chat:

```text
Мы продолжаем Unity-проект Battle for Universe: Sand runners.

Проект: C:\Users\Админ\My project
Первый playable build: C:\Games\sandrunnersplaybild1

Сейчас игра уже стала играбельной: есть меню, новая игра, пауза, победа/поражение, RTS-цикл с пирамидой, билдерами, логистикой, ресурсами, обороной, отрядами, Мандаринкой и её мобильной крепостью.

Главная проблема сейчас: камера.
Мне нужно, чтобы камера при выборе юнита/отряда и нажатии C реально фокусировалась на юните или центре отряда, а не ощущалась как дальний вид от пирамиды. Нужно сделать полноценные профили камеры под типы юнитов: билдер, скарабей/наземка, флаер, тяжёлые юниты, здания/турели, пирамида/крепость. Должен быть нормальный zoom колесом, ручной orbit, возможность продолжать отдавать команды правой кнопкой в режиме слежения, и HUD-подсказка Following: имя юнита.

Ещё важно: в проекте уже лежат Meshy-модели. Найди их в папке проекта, составь список, и подготовь первый проход замены процедурных placeholder-моделей на реальные prefab/model assets. Начать лучше с камеры, затем подключить модели к самым заметным юнитам.

Сначала проверь компиляцию/Play Mode, потому что последний маленький фикс не был прогнан Roslyn-компиляцией из-за лимита approve. Не меняй ProjectSettings/Input System. Работай в Assets/Scripts/SandRunners.
```

## Recommended Next Pass

1. Compile/Play smoke test.
2. Camera pass:
   - true target-centered follow
   - per-unit profile
   - close zoom
   - orbit while commanding
   - HUD state
3. Meshy asset inventory:
   - find imported models
   - inspect names/scales/materials
   - decide first 3 replacements
4. Replace one or two high-impact placeholders:
   - builder
   - one golden combat unit
   - Mandarinka/Karl Gustav if model exists
5. Then do another playtest build.
