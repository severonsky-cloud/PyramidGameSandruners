# Sebek Pyramid Intro - Next Pass Plan

## How To Play Current Slice

Open `Assets/Scenes/SandRunners/SebekBedroomIntro.unity` in Unity and press Play.

Controls:
- `WASD` or arrow keys: move Sebek.
- `E`, `Space`, or left mouse button: interact with the highlighted object.

Current flow:
1. Sebek wakes up in the bedroom.
2. After a short waking pause, explore the room.
3. Inspect a few objects or wait several seconds.
4. The radio begins calling.
5. Find the radio on the bedside table.
6. Interact with it to receive the prisoner objective.

## Target For Next Pass

Make a nearly complete playable pyramid intro segment, not just a bedroom mockup.

Player fantasy:
Sebek wakes in her private pyramid quarters, regains control, handles small personal-room interactions, receives a radio order, leaves the bedroom, moves through a pyramid corridor, and reaches the first prisoner/security checkpoint.

## Scene Structure

Keep this as a separate intro scene:
- `Assets/Scenes/SandRunners/SebekBedroomIntro.unity`

Expand it into connected zones:
1. Bedroom
2. Private bathroom door / grooming optional beat
3. Bedroom exit
4. Short pyramid corridor
5. Guard/security node
6. Prisoner holding-room door

## Gameplay Beats

1. Wake Up
   Short camera sway, delayed control, tired idle.

2. Bedroom Exploration
   Interactables: bed, panoramic window, wardrobe, vanity, work table, bathroom door.

3. Radio Call
   Radio pulses red, audio/static text appears, objective changes.

4. Prepare / Leave
   Optional wardrobe or vanity interaction can mark Sebek as "ready".

5. Corridor Walk
   Camera changes from bedroom framing to tighter third-person corridor framing.

6. Security Checkpoint
   Add one guard/intercom/door console. Interaction opens route to prisoner.

7. Prisoner Hook
   End the slice at the holding-room door or first look at the prisoner.

## Technical Tasks

1. Play Mode verify current scene.
   Check movement, camera, interaction prompts, radio progression, and console errors.

2. Fix current controller rough spots.
   Tune camera offset, movement speed, interaction radius, and waking timing.

3. Add scene navigation.
   Door interaction should unlock/open bedroom exit and move objective to corridor.

4. Add corridor blockout.
   Use modular wall/floor/ceiling pieces, warm pyramid lighting, and simple collision.

5. Add objective state machine polish.
   Make each story beat explicit and resistant to sequence breaks.

6. Add audio placeholders.
   Radio static, room hum, footsteps, door open, objective sting.

7. Add UI polish.
   Replace debug-like labels with compact cinematic objective/prompt UI.

8. Add art replacement plan.
   Convert bedroom props from primitive blockout to modular assets while preserving object names and interactable components.

## Art Tasks

Priority assets:
- Nightwear Sebek variant or clothing overlay.
- Round bed with bedding.
- Panoramic window frame kit.
- Wardrobes and vanity.
- Radio prop.
- Bathroom door.
- Pyramid corridor modular kit.
- Security door / holding-room door.
- Distant desert/pyramid exterior vista.

Concept images to generate first:
1. Wide bedroom shot.
2. Vanity/radio prop sheet.
3. Corridor/security checkpoint shot.
4. Nightwear Sebek character sheet.

## Known Issues

- Current Sebek is a placeholder material on the combat model, not a real nightwear mesh.
- Bedroom is still primitive blockout.
- Camera is playable but not cinematic enough yet.
- No real audio yet.
- Corridor and prisoner route are not built yet.
- Unity plugin warns about the project path containing spaces.

## Definition Of Done For Next Pass

- User can press Play and complete the intro route from bed to prisoner objective.
- No blocking console errors.
- Camera feels intentional in bedroom and corridor.
- All required interactions have prompts and objective updates.
- Radio moment is readable.
- Scene has saved blockout geometry for bedroom, exit, corridor, and prisoner door.
- Art plan is updated with exact asset names to replace.
