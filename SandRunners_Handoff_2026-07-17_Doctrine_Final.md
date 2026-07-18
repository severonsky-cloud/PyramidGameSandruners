# SandRunners — checkpoint 2026-07-17

## Completed

- Added persistent squad doctrines: Pyramid Escort, Hold Area 85m, Search and Destroy 260m.
- Fixed squad-local formation indexing and completion of temporary air Move orders.
- Ground squads, builders, ordinary air wings and Thoth aircraft return to their doctrine after temporary orders.
- Thoth aircraft automatically return, service, rearm and relaunch while automation is enabled.
- Added Strategic Canvas doctrine buttons and current doctrine status.
- Added SandRunnersDoctrinePresentation.cs with a pulsing radius, anchor/target tether, doctrine colors and autonomous/target-acquired label.
- Corrected the label to use camera rotation and to follow the selected squad in Escort mode.

## Verification

- Unity compilation completed without C# errors.
- EditMode tests: 39/39 passed.
- Play Mode stress scenario: two Combat Flyer groups, two Scarab Tank groups, Builder, Fortress Crusher and Thoth's Embrace with battle simulation and salvage active.
- Observed Editor samples: first mass-spawn frame about 30 FPS; settled command-mode frame about 60 FPS.
- Doctrine radius and tether rendered correctly. The final camera-facing label patch compiled after the screenshot and needs one fresh visual confirmation.

## Development build

- BuildPipeline was invoked for Windows x64 Development Build.
- Intended output: Builds/SandRunners_Doctrine_Dev/SandRunners_Doctrine_Dev.exe
- It used enabled EditorBuildSettings scenes, with Assets/Scenes/SampleScene.unity as fallback.
- Unity spent roughly 90 seconds building, then the connector returned Response data is null.
- Artifact verification was attempted, but the external command was blocked because the weekly tool limit was exhausted.
- The build artifact is not yet verified. First next action: check the folder and repeat the Development Build only if the exe is absent.

## Files changed

- Assets/Scripts/SandRunners/SandRunnersRTSCore.cs
- Assets/Scripts/SandRunners/SandRunnersAviation.cs
- Assets/Scripts/SandRunners/SandRunnersThothCarrier.cs
- Assets/Scripts/SandRunners/SandRunnersStrategicCanvas.cs
- Assets/Scripts/SandRunners/SandRunnersDoctrinePresentation.cs

## Next-session first five minutes

1. Confirm Unity is outside Play Mode and compilation is clean.
2. Inspect Builds/SandRunners_Doctrine_Dev/.
3. Start a fresh RTS session, deploy and select one squad in Command Mode.
4. Confirm the world label is readable rather than mirrored.
5. If confirmed, rebuild only if the exe is missing; otherwise launch it for a one-minute smoke test.
