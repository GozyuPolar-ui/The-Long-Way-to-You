# Closed Beta QA Report

## Recommendation: NO-GO

Do not release based on this audit alone. Static project review found a few configuration and completeness risks, and the required current Unity compile, gameplay run, performance measurements and Windows build were not performed. Historical editor logs contain compiler errors, but their source locations no longer match current code; they cannot be used to certify either a failure or a clean compile.

## Scope and method

- Inspected serialized build/player settings, the three enabled game scenes and the forest scene structure, relevant runtime code paths, input bindings, menu text configuration, selected repository hygiene checks, and existing Editor logs.
- Did not open/focus Unity, start Play Mode, run Test Runner, build or launch the game, change project settings, or edit third-party/Starter Assets.
- Unity was already open for this project. No editor control/test bridge was available in this session, so no second editor was started.
- A sanitized historical log summary is in [`QA~/EditorLog_Summary.txt`](./QA~/EditorLog_Summary.txt).

## Findings, priority order

### Blocker

1. **Release-critical behavior is unverified.** Reproduction: run the game and complete menu → forest → bear → backpack → Bram → Closing → menu. Impact: no evidence for compilation, scene transitions, quest correctness, input/cursor recovery, or repeat-start state. Status: reported, not fixed; this is a test limitation, not a confirmed runtime defect. Files changed for this finding: none.
2. **No release build was produced.** The requested `D:\Builds\QA_CBT` build, executable smoke test, Player.log review, package-size review and build-folder inspection were not possible. Status: reported. Files changed: none.

### Major

3. **Current compile status is unknown.** `Logs/Editor.log` contains six historical error entries for two diagnostics in `PrinceBackpackQuestFix.cs`; the current source no longer matches the logged expressions. `Logs/Editor-prev.log` contains earlier errors in `MainMenuController`, `LoadingScreenController` and `ClosingSceneController`. Historical warnings include obsolete API notices and `CS0472` in `PrinceDialogueQuestSetup.cs`. None is a post-change compile result. Status: needs a fresh Unity compile and Console review; no source fix made without current evidence.
4. **Player configuration differs from the requested release baseline.** `ProjectSettings/ProjectSettings.asset` has `companyName: ' ExceedOG'` (leading space) and default resolution 1024x768, not 1920x1080. Product name and version are `The Long Way to You` and `0.1.0`. Standalone has no explicit `scriptingBackend` entry in the serialized settings; Mono is therefore not confirmed. Status: reported, not changed because this pass avoided project-setting edits. File changed: none.
5. **No pause-menu implementation was found in the project runtime scripts.** Esc closes the map when it is open; this audit found no separate pause menu. Status: reported as a missing requested QA surface, not added because this audit must not add features.

### Minor / Polish

6. **Optional Closing external links are empty.** `ClosingData.asset` has empty feedback, GitHub, Instagram and developer-email URLs, while a feedback QR sprite is assigned. Optional external buttons/links may therefore be hidden. Verify this is intended before release. Status: reported, not changed.
7. **A forest terrain material reference is an unconfirmed candidate for a missing asset.** Its serialized GUID did not resolve during static inspection. No pink material or shader failure was confirmed; inspect it in Unity before treating it as broken. Status: reported, not changed.
8. **Working-tree whitespace warning.** `git diff --check` reports trailing whitespace on two `m_Name` lines in the already-modified `UNS_Demo.unity`; Git also reports line-ending normalization warnings for modified assets. These are not runtime findings and were left untouched.

## Phase 1 - static audit

| Area | Result |
|---|---|
| Compilation and Console | **Not verified.** Historical logs contain errors/warnings; current Unity compilation was unavailable. No current clean compile is claimed. |
| Missing scripts | No `m_Script: {fileID: 0}` entry was found in the three serialized scenes. This does not replace inspecting every prefab and imported asset in Unity. |
| Scene audio/camera/EventSystem | Main-Menu and Closing each serialize one AudioListener, one camera and one EventSystem. UNS_Demo has one AudioListener and two camera objects (the map camera is inactive); no EventSystem is serialized there, and dialogue/map code creates one at runtime. Runtime behavior remains untested. |
| Forest navigation and lighting | A NavMeshSurface references baked data. Forest lighting/ambient configuration is present. NavMesh pathing and actual bear navigation were not tested. |
| Scenes in build | Main-Menu is enabled at index 0, UNS_Demo at index 1, SampleScene is disabled, and Closing is enabled at index 2. |
| Player Settings | Product/version match. Company name has a leading space; default resolution is 1024x768. Standalone icon references are present. Splash logo entries serialize Unity first and the ExceedOG logo second; actual sequential display and standalone backend were not verified. |
| Quality / URP | PC quality uses index 1 with a PC URP asset; Android uses a Mobile profile. No visual or device-quality test was performed. |
| Input | The Input System action asset has Player and UI maps. Project scripts separately bind E for interaction/dialogue, R for weapon toggle, M for map and Escape for map close. Dialogue guards interaction while open; map opening disables PlayerInteractor. Some controls are implemented in code rather than the shared action asset, so physical keyboard/gamepad conflict testing remains outstanding. |
| Asset references | The three scenes contain no explicit missing-script marker. One terrain material GUID remains unresolved as a candidate. Full prefab/audio/font/shader reference validation and TMP Essential Resources validation require Unity. |
| Repo docs/config | README.md is tracked; CREDITS.md is not. See Phase 5 below. |

## Phase 2 - gameplay flow

All rows below are **Tidak bisa diuji** in runtime. Static code/scene checks are listed only as evidence; they do not count as a pass.

| # | Test | Result and reason |
|---|---|---|
| 1 | Main menu, Continue/New Game/Settings/Feedback/Quit, focus and UI sounds | **Tidak bisa diuji:** no Unity runtime. Static menu settings and UI sound resources exist; the main menu's animated video, button state, settings persistence, URL validity and audio cannot be verified here. |
| 2 | New Game transition, player spawn/camera/cursor and movement | **Tidak bisa diuji:** no Play Mode/build. |
| 3 | Sword toggle, 3-hit combo/buffer, hit stop, effects and damage | **Tidak bisa diuji:** no combat runtime test. |
| 4 | Bear patrol, detection/chase/attack/reaction/death/leash | **Tidak bisa diuji:** static NavMesh reference only; AI behavior and permanent death not exercised. |
| 5 | Bram interaction/dialogue, choices, input lock and repeat dialogue | **Tidak bisa diuji:** dialogue source was not run; cursor/input recovery unverified. |
| 6 | Quest order, locked backpack, rejection, compass/map and notifications | **Tidak bisa diuji:** no playthrough. Prior quest/tracking fixes and scene edits need an end-to-end rerun. |
| 7 | Turn-in, closing dialogue, one-time Closing transition and clean restart | **Tidak bisa diuji:** no playthrough. |
| 8 | Bear defeat/knockout/respawn soft fail | **Tidak bisa diuji:** no damage/respawn test. |
| 9 | Pause menu | **Tidak bisa diuji / tidak ditemukan:** no pause-menu runtime component was found. |
| 10 | Esc/M/attack during dialogue, map during combat, spam clicks, Alt-Tab, resolution/fullscreen, death in dialogue, save/Continue | **Tidak bisa diuji:** no runtime or device testing. |
| 11 | Music, ambience, sword/UI SFX and volume sliders | **Tidak bisa diuji:** audio playback and relative volume require runtime. A UI sound-set asset exists; an older log warning about it is historical. |
| 12 | English text/branding/version consistency | **Sebagian statis:** product/version are correct and a language selector/localization system exists. No automated full-text scan/playthrough established that all visible strings are English or that the footer matches the built executable. |

## Phase 3 - performance

No FPS, frame-time, draw-call, batch or memory samples were collected. The required 1920x1080 menu and forest comparisons were not run. No performance optimizations were made.

## Phase 4 - Windows build

No build was created in `D:\Builds\QA_CBT`; no executable or Player.log exists from this audit. Build size, shipped folders, splash, icon, version and the menu → quest → Closing → menu path remain unverified. There are no test build folders from this run to assess for `_BurstDebugInformation_DoNotShip` or `_BackUpThisFolder_ButDontShipItWithYourGame`.

## Phase 5 - repository review

- Five tracked files exceed 50 MB: four Fantasy knight TGA textures at about 64 MB each, and `UNS_HDRI.hdr` (under the Ultimate Nature Starter environment Skyboxes folder) at about 250.3 MB.
- Third-party asset content is tracked. No assets were deleted.
- Root `Library`, `Temp`, `Logs`, `UserSettings` and `Builds` content is not tracked.
- `README.md` is tracked; `CREDITS.md` is absent.
- Git was used only for read-only inspection and `diff --check`; no commit, push, pull, reset, checkout, stash or ignore-file change was made.

## Menu polish change and backups

This pass changed only:

- [`MenuTextStyle.cs`](../Scripts/Runtime/UI/MenuTextStyle.cs)
- [`MenuTextStyle.asset`](../Resources/MenuTextStyle.asset)

Backups exist at `MenuText2Backup~/MenuTextStyle.cs.bak`, `MenuText2Backup~/MenuTextStyle.asset.bak`, `QABackup~/MenuTextStyle.cs.bak` and `QABackup~/MenuTextStyle.asset.bak`.

Static fallback-image contrast results, menu values, the font and manual visual checks are documented in [`MenuText2_Notes.md`](./MenuText2_Notes.md). The editor Setup Main Menu tool was not run, and the moving video was not evaluated.

Other modifications already present in the worktree from earlier requests were preserved and not changed by this QA pass: river setup/runtime files, `UNS_Demo.unity`, river material/notes, and the TMP font assets. Their earlier backup/notes are in `RiverSetupBackup~` and `RiverSetup_Notes.md`; the island-edge walls and river audio still require editor inspection and Play Mode testing.

## What remains untested

- Current Unity compile and Console state.
- All gameplay and UI flows, including physical gamepad input, dialogue edge cases, scene load timing and state reset.
- Moving-video contrast, rendering at the requested resolutions, audio mix and device-specific quality.
- Performance, Windows build, executable launch, Player.log, packaging and end-user machine compatibility.

## Langkah manual sebelum rilis

1. Let the open Unity editor finish compiling; resolve all current Console errors and review warnings. Do not rely on historical log entries.
2. Run **Tools > Prince > Setup Main Menu**, then **Tools > Prince > Audit Main Menu Text Contrast**. Check the moving video and all three target resolutions.
3. Run **Tools > Prince > Setup River Environment** and inspect the island-edge wall gizmos; verify the perimeter follows the island rather than cutting across playable terrain.
4. Play through every Phase 2 item, especially the full bear → backpack → Bram → Closing → menu loop, then test restart and the listed edge cases.
5. Measure menu and forest performance at 1920x1080.
6. Build a non-development Windows player to a clean `D:\Builds\QA_CBT` directory and smoke-test the executable, logs, splash, icon, version and packaged scenes.
7. Before release, decide whether to correct the company-name whitespace, set the intended default resolution/Standalone backend, configure Closing external links, and add/omit a pause menu intentionally.
