# Sebek Bedroom Intro Art Plan

## Scene Goal

Separate intro slice inside Sebek's personal pyramid quarters: she wakes up in a broad bedroom, moves with difficulty through moonlit luxury, hears a radio, finds it, and receives the order to check the prisoner.

## What Exists Now

- Playable greybox room: wide bedroom, panoramic window, round bed, wardrobes, vanity, desk, bathroom door, radio.
- Current Sebek biped FBX is used as the playable placeholder.
- Radio objective flow exists: wake up, explore, radio static, find radio, prisoner objective.

## Art Needed

1. Sebek bedroom key art: wide horizontal view, round bed by panoramic window, moonlight bands on polished floor.
2. Bed/wardrobe detail sheet: circular bed, bedding, robe/nightwear storage, bedside table.
3. Vanity/radio detail sheet: mirror, cosmetics, command jewelry, compact radio with warning light.
4. Bathroom door concept: private bath entrance, steam/glow language, later grooming beat.
5. Nightwear Sebek variant: same character silhouette, softer clothing mesh/material, no combat outfit.
6. Pyramid exterior/window view: desert horizon plus visible golden pyramid ribs/structures outside the glass.

## Production Path

1. Blockout in Unity first.
   Keep scale, camera, interaction points, and story beats working before high art.

2. Generate or paint 3 concept images.
   Use them as target references, not final in-game assets.

3. Build modular 3D kit.
   Walls, floor panels, window frames, bed, wardrobe, vanity, tables, bathroom door, radio, small props.

4. Replace greybox objects one by one.
   Preserve object names and interaction components so the script keeps working.

5. Character pass.
   Either create a new nightwear Sebek mesh, or add a clothing overlay/skinned mesh that follows the current rig.

## Suggested Concept Prompts

Wide bedroom:
"Ancient Egyptian science fantasy pyramid bedroom, broad luxurious chamber, circular bed placed near a huge panoramic window, moonlight flooding polished gold-stone floor, wardrobes and vanity around the bed, desert horizon and golden pyramid ribs visible outside, cinematic game concept art, readable prop layout, no text."

Vanity and radio:
"Close view of a royal sci-fi Egyptian vanity table, bronze mirror, cosmetics, jewelry, compact military radio with small red signal light, warm bedside lamp, moonlit room atmosphere, high-detail game prop concept art, no text."

Nightwear Sebek:
"Female Egyptian crocodile-deity inspired humanoid queen character, elegant nightwear robe, tired waking posture, same regal silhouette, game character concept sheet, full body front and side, no text."

## Asset Specs

- Unity units: 1 unit = 1 meter.
- Room target size: about 18m wide, 12m deep, 4.5m tall.
- Character height target: about 2m.
- Export models as FBX when possible. GLB is acceptable for source, then convert/import to FBX.
- Use separate pivots for interactables: radio, bathroom door, wardrobe doors, vanity.
- Keep collision simple: box/capsule colliders on final props.
