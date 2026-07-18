# SandRunners Release Profile Without MCP Runtime

This pass does not remove MCP packages and does not create or verify a Player build.

Future release integration should use a separate release branch/profile and:

- Keep Windows Standalone x64 as the active platform.
- Put a lightweight bootstrap scene before heavy gameplay/prologue scenes in Build Settings.
- Include `Assets/Scenes/SandRunners/SebekBedroomIntro.unity` only when the release profile intentionally supports the full prologue route.
- Include `Assets/Scenes/SampleScene.unity` for RTS startup and safe fallback to the start menu.
- Exclude editor-only MCP runtime/test tooling from Player assemblies via asmdef platform filters, package profile split, or scripting define guards.
- Run `Sand Runners/Validation/Validate Startup Scenes (Read Only)` before any Player build.
- Run EditMode tests for `SandRunnersSessionBootstrap` and `SandRunnersBootstrap` before release packaging.

Do not remove MCP assets from the main development profile without a dedicated integration review.
