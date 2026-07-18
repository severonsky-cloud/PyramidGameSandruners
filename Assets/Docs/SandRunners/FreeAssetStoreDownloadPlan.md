# Free Asset Store Download Plan

Goal: make the Sebek pyramid intro segment feel close to a real playable game without spending more Meshy credits.

## Download First

1. 3D Free Modular Kit
   Use for: pyramid corridor, security checkpoint, prisoner-door hallway, modular walls/floors/ceilings.
   Why: small, free, modular, interior/sci-fi keywords, easier than the larger sci-fi kit.

2. Cute Furniture FREE - Low Poly 3D Models Pack
   Use for: bedroom filler props, vanity replacement, tables, cabinets, small furniture.
   Why: small, free, directly useful for the current bedroom.

3. Human Basic Motions FREE
   Use for: retargetable idle/walk/run/turn motions if Sebek's current GLB animations are not enough.
   Why: small, current Unity 6-compatible animation pack.

4. RPG_Animations_Pack_FREE
   Use for: interaction, recovery, cautious movement, door/ladder/combat-adjacent motions.
   Why: broader animation vocabulary for a playable character segment.

5. Particle Pack
   Use for: radio static sparks, door power effects, pyramid reactor ambience, combat carry-over later.
   Why: Unity-made, free, URP-compatible in Unity 6, good production value.

6. POLYGON - Starter Pack - Art by Synty
   Use for: extra blockout props, readable low-poly style pieces, quick filler.
   Why: free, trusted starter kit, useful for fast iteration.

## Download If Disk/Time Is Fine

7. 3D Scifi Kit Starter Kit
   Use for: richer high-tech pyramid interiors.
   Caution: very large package; download after the small modular kit.

8. Fake Interiors FREE
   Use for: fake depth behind the panoramic bedroom window and pyramid exterior windows.
   Caution: shader integration may need checking in URP.

9. Human Melee Animations FREE
   Use for: guards, prisoner/security moments, later combat intros.
   Caution: not essential for the bedroom route.

10. Starter Assets - ThirdPerson | URP
   Use for: controller/camera reference, not necessarily to replace our current code.
   Caution: can bring project/input setup changes; import carefully.

## Skip For Now

- Networking packages: FishNet, Photon, PUN.
- XR packages: Meta XR tools.
- Big terrain/world tools: MapMagic, Gaia-related extras, vegetation spawners.
- Heavy character frameworks: UMA.
- Multiple controller frameworks at once: do not import Invector and Starter Assets together unless we choose one deliberately.
- Audio middleware unless needed: Bro Audio/FMOD are optional; we can start with plain AudioSource placeholders.

## Import Order

1. Download the first six into My Assets.
2. Import one package at a time.
3. After each import, let Unity compile, then check Console.
4. Do not import sample scenes into the main scene unless needed; import assets/prefabs/materials first.
5. Tell Codex after each import so it can inspect the asset folders and wire the best pieces into the intro scene.

## Replacement Targets In Current Scene

- `Round_Bed_By_Panoramic_Window` -> real/custom bed later; temporary furniture pack props can fill surroundings.
- `Vanity_Table_With_Mirror` -> furniture pack vanity/table + custom mirror material.
- `Private_Bathroom_Door` -> modular door piece or custom pyramid door.
- `Hidden_Radio_On_Bedside_Table` -> keep custom blockout unless a good radio/electronics prop appears.
- Corridor not built yet -> use `3D Free Modular Kit` first.
- Prisoner door/checkpoint -> use modular kit plus custom gold/black materials.
