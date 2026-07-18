# SandRunners Handoff Report - 2026-07-07

## Current playable slice

Scene:
- `Assets/Scenes/SandRunners/SebekBedroomIntro.unity`

Main runtime script:
- `Assets/Scripts/SandRunners/Intro/SebekBedroomIntroController.cs`

Editor scene builder:
- `Assets/Editor/SandRunnersBedroomIntroSceneBuilder.cs`

The intro slice is now passable:
1. Sebek wakes in the bedroom.
2. Player finds/answers the radio.
3. Bedroom exit opens.
4. Player reaches the corridor security console.
5. Console releases the holding-room seal.
6. Player reaches the prisoner door.
7. Segment ends on the prisoner reveal hook.

## Controls

- `WASD` / arrows: move.
- Right mouse button + mouse: rotate over-shoulder camera.
- Mouse wheel: zoom camera in/out.
- `E`, `Space`, or left click: interact.
- `G`: Sebek archive overlay.

## Important fixes already made

- Camera now forces itself to active `Main Camera` with high depth and follows over the shoulder.
- Movement is camera-relative.
- Camera zoom and RMB orbit are implemented.
- Radio, security console, and prisoner door have fallback proximity triggers, so the slice should not hard-block if interactable lookup fails.
- UI was made more readable with darker panels and larger prompt/objective text.
- Sebek visual now has procedural walk bob/sway/lean fallback, because the imported animation is unreliable.
- Sebek visual no longer has the forced 180-degree yaw in new builds.

## Known weak spots

- The slice is playable but still blockout-quality.
- Interaction readability is still rough: prompts are functional, but object affordance needs proper outlines/icons.
- Sebek's real FBX animation is not reliably playing. Current procedural motion is a temporary patch.
- Camera works but can still feel abrupt near tight corridor walls. Needs collision/shoulder smoothing.
- The bedroom/corridor art is primitive cubes. Needs downloaded Asset Store furniture, modular Egyptian walls, props, decals, and lighting pass.
- The pinup gallery is imported, but should be curated into in-world murals/paintings later, not treated as core gameplay.

## Next pass priority

1. Replace primitive bedroom/corridor blockout with downloaded free assets.
2. Fix Sebek animation properly:
   - inspect FBX import clips,
   - verify legacy/humanoid settings,
   - decide final yaw offset,
   - remove procedural fake motion after real clips work.
3. Add proper interaction UX:
   - visible outline,
   - small world-space icon above target,
   - one reliable interact radius component for all story objects.
4. Improve camera:
   - softer shoulder follow,
   - wall collision,
   - reset camera key,
   - separate bedroom/corridor distance presets.
5. Build the prisoner-room reveal after the door:
   - door opens,
   - short camera beat,
   - prisoner visible,
   - end-of-slice marker.

## Files changed in this phase

- `Assets/Scripts/SandRunners/Intro/SebekBedroomIntroController.cs`
- `Assets/Editor/SandRunnersBedroomIntroSceneBuilder.cs`
- `Assets/Docs/SandRunners/SebekBedroomIntro_ArtPlan.md`
- `Assets/Docs/SandRunners/SebekPinupIntegrationPlan.md`
- `Assets/Docs/SandRunners/FreeAssetStoreDownloadPlan.md`
- `Assets/Resources/SandRunners/Art/Pinups/Sebek/*`
- `Assets/Resources/SandRunners/Models/Sebek/*`

## Notes for next Codex run

Do not spend time rethinking the whole intro. The current goal is clear: turn the playable blockout into a presentable pyramid intro slice. Start by checking the current Unity scene in Play Mode, then replace the worst-looking geometry and only after that touch deeper systems.

If limits are low again, the best single improvement is interaction readability: make every active story object visibly outlined and show one consistent `E` prompt in-world.

Suggested first message for next run:
`Продолжай с SandRunners_Handoff_Report_2026-07-07.md. Сначала сделай нормальную читаемость интерактивов и почини настоящую анимацию Себек, потом начинай заменять blockout ассетами.`
