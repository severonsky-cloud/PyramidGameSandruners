# SandRunners Cinematic Clip Handoff - 2026-07-07

Project: `C:\Users\Админ\My project`

Build folder mentioned by user: `C:\Games\sandrunnersplaybild1`

Goal: finish a short cinematic battle clip/trailer for `Battle for Universe: Sand runners` using the new in-game trailer director, then record/export it as MP4. User wants big vehicle parades, large battles, strong camera angles, and about 90 seconds from the supplied track first; 3 minutes is acceptable later if budget/time allows.

## What Is Already Implemented

New file:

- `Assets/Scripts/SandRunners/SandRunnersCinematicDirector.cs`

Modified files:

- `Assets/Scripts/SandRunners/SandRunnersPrototype.cs`
- `Assets/Scripts/SandRunners/SandRunnersImmersiveBattlefield.cs`
- `Assets/Scripts/SandRunners/SandRunnersRTSCore.cs`

Music asset created:

- `Assets/Resources/SandRunners/Audio/GlacierAndCliff_Intro90.ogg`

Source mp3:

- `C:\Users\Админ\Downloads\Godspeed You! Black Emperor - Glacier and Cliff (live 2019-08-10).mp3`

The OGG is the first 90 seconds, with fade in and fade out. It is loaded by:

```csharp
Resources.Load<AudioClip>("SandRunners/Audio/GlacierAndCliff_Intro90")
```

## Controls And Launch

In Play Mode / build:

- `F9` starts the 90-second trailer director.
- `F10` starts the 180-second extended version.
- `F8` stops the trailer director.

Build command-line flags:

- `-sandTrailer` starts the 90-second trailer automatically.
- `-sandTrailer180` starts the 180-second version automatically.
- `-sandTrailerDuration=180` starts with a custom duration.

## Current Behavior

The director stages the battle using real game mechanics, not just fake animation:

- Golden armor parade.
- Pyramid and armored columns.
- Mandarinka fortress reveal.
- Ground crawlers, air junks, builders, Gustav siege guns.
- Golden scarabs, sand skimmers, fortress crushers, Wrath of Ra, heavy flyers, Thoth's Embrace.
- Golden defensive turrets.
- Hostile artillery barrages.
- Cruise missiles, Sun-core missile, Apex beam.
- Camera shot sequence from close parade shots to huge battlefield views.
- HUD is hidden during trailer mode for clean recording.

Important fix already added:

- During trailer mode, `MarkDefeatIfNeeded()` keeps pyramid hull above a safe threshold, so the normal defeat overlay should not interrupt the clip.
- The director also now holds the final shot after reaching the requested duration instead of dropping back into a normal match immediately.

## Validation Already Done

Unity `AssetDatabase.Refresh(ForceSynchronousImport)` succeeded after the final script changes.

Runtime smoke tests were partially run in `Assets/Scenes/SampleScene.unity`:

- Early trailer phase worked.
- Mid-battle phase worked with no runtime `Error` or `Exception`.
- A previous issue showed `PYRAMID LOST`; this was fixed by making trailer mode invulnerable and adding final-hold behavior.

Known non-project error in Console:

- The MCP plugin logs an Error saying the project path contains spaces. This comes from `com.ivanmurzak.unity.mcp`, not from SandRunners gameplay scripts.

## Next Agent: Exact Steps To Finish

1. Open Unity project at `C:\Users\Админ\My project`.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play.
4. Press `F9`.
5. Watch the full 90 seconds plus a few seconds of final hold.
6. Confirm no `PYRAMID LOST` overlay appears.
7. Confirm Console has no new SandRunners `Error`/`Exception`.
8. Record the Game View.
9. Export MP4.

If time is limited, prioritize a clean 90-second MP4 over the 180-second version.

## Recording Options

Best practical path:

- Use OBS or another screen recorder to capture Unity Game View or the standalone build.
- Record at 1920x1080 or the current Game View resolution.
- Start recording, then press `F9`.
- Stop after the final held wide battlefield shot.

Alternative standalone build path:

- Build the project after confirming scripts compile.
- Launch with `-sandTrailer`.
- Capture the build window.

Example launch idea, adapt exe path if a new build is made:

```powershell
& "C:\Games\sandrunnersplaybild1\My project.exe" -sandTrailer
```

## Suggested Final Edit

If raw footage is recorded with game audio/music already inside:

```powershell
ffmpeg -y -i raw_capture.mp4 -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -c:a aac -b:a 192k SandRunners_Trailer_90s.mp4
```

If raw footage has no audio and needs the OGG attached:

```powershell
ffmpeg -y -i raw_capture.mp4 -i "Assets\Resources\SandRunners\Audio\GlacierAndCliff_Intro90.ogg" -map 0:v:0 -map 1:a:0 -t 90 -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -c:a aac -b:a 192k -shortest SandRunners_Trailer_90s.mp4
```

## If You Need To Improve The Clip Before Recording

Likely highest-value improvements:

- Make the first 10 seconds lower and closer to the vehicle parade.
- Increase visibility of golden units in the red fog, possibly with brighter marker lights or a slightly lower fog density during trailer mode.
- Add a title card only if the user asks; current request is more about battle spectacle than text.
- If final hold still shows defeat overlay, keep `cinematicDirectorActive` true and force `gameFlowState = Playing` while holding.

## Files To Inspect First

Read these before changing anything:

- `Assets/Scripts/SandRunners/SandRunnersCinematicDirector.cs`
- `Assets/Scripts/SandRunners/SandRunnersPrototype.cs`
- `Assets/Scripts/SandRunners/SandRunnersImmersiveBattlefield.cs`
- `Assets/Scripts/SandRunners/SandRunnersRTSCore.cs`
- `Assets/Scripts/SandRunners/SandRunnersMandarinkaEncounter.cs`

Avoid touching `ProjectSettings/Input System` unless explicitly requested.

## User Preference

The user wants spectacle: more vehicle parades, more combat, bigger battles, cool angles. They are fine starting with about 90 seconds due to cost/limits, but ideally want 3 minutes later.
