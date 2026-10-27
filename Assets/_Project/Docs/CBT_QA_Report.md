# Closed Beta QA Report

## Recommendation: NO-GO

Do not release based on this audit alone. A non-development Windows build succeeded and its executable launched to the main menu, but input-driven menu actions and the required gameplay loop could not be validated. Performance measurements and a complete current Console review are also outstanding.

## Scope and method

- Inspected serialized build/player settings, the three enabled game scenes and the forest scene structure, relevant runtime code paths, input bindings, menu text configuration, selected repository hygiene checks, and existing Editor logs.
- Unity was already open; it was not closed, and no second editor was started. Play Mode was entered and stopped from the open editor. A Windows player was built and launched.
- Main-menu visuals were observed in Play Mode and in the player. Attempts to activate menu actions using screen clicks, keyboard events (`keybd_event`, `SendInput`) and window messages did not trigger an action. This leaves input behavior unverified; the injection path itself may be unreliable, so this is not recorded as a confirmed game defect.
- The standalone Player.log and build output were inspected. No ProjectSettings, package manifest, third-party or Starter Assets were edited during this QA continuation.
- A sanitized historical log summary is in [`QA~/EditorLog_Summary.txt`](./QA~/EditorLog_Summary.txt).

## Findings, priority order

### Blocker

1. **Release-critical gameplay loop remains unverified.** Reproduction: activate New Game, then complete menu → forest → bear → backpack → Bram → Closing → menu. Impact: no evidence for gameplay, quest correctness, combat, scene transitions, input/cursor recovery, or repeat-start state. The visible menu remained open after injected input attempts, but that result is inconclusive because the input mechanism may not emulate physical input faithfully. Status: reported, not fixed; no product defect is confirmed. Files changed for this finding: none.
2. **Runtime input path needs a real-input smoke test.** Reproduction: at the standalone main menu, try New Game using a physical mouse or keyboard. Synthetic clicks, keyboard events and window messages did not activate it in this session. Impact: if reproduced with physical input, the game cannot be started from the menu. Status: not confirmed; verify manually before release. Files changed: none.

### Major

3. **Build-time compilation succeeded, but the live Unity Console was not audited exhaustively.** The Windows build completed successfully, which confirms the build's compilation step. Historical `Logs/Editor.log` errors in `PrinceBackpackQuestFix.cs` and older errors in `MainMenuController`, `LoadingScreenController` and `ClosingSceneController` refer to earlier source states. The build Player.log contains no startup exception/error; it reports the expected absence of a `ThirdPersonController` on Main-Menu and a nonfatal H.264 timestamp correction warning. Status: build compilation passed; inspect the current editor Console for warnings/errors before release.
4. **Player configuration differs from the requested release baseline.** `ProjectSettings/ProjectSettings.asset` has `companyName: ' ExceedOG'` (leading space) and default resolution 1024x768, not 1920x1080. Product name and version are `The Long Way to You` and `0.1.0`. Standalone has no explicit `scriptingBackend` entry in the serialized settings; Mono is therefore not confirmed. Status: reported, not changed because this pass avoided project-setting edits. File changed: none.
5. **No pause-menu implementation was found in the project runtime scripts.** Esc closes the map when it is open; this audit found no separate pause menu. Status: reported as a missing requested QA surface, not added because this audit must not add features.

### Minor / Polish

6. **Optional Closing external links are empty.** `ClosingData.asset` has empty feedback, GitHub, Instagram and developer-email URLs, while a feedback QR sprite is assigned. Optional external buttons/links may therefore be hidden. Verify this is intended before release. Status: reported, not changed.
7. **A forest terrain material reference is an unconfirmed candidate for a missing asset.** Its serialized GUID did not resolve during static inspection. No pink material or shader failure was confirmed; inspect it in Unity before treating it as broken. Status: reported, not changed.
8. **Working-tree whitespace warning.** `git diff --check` reports trailing whitespace on two `m_Name` lines in the already-modified `UNS_Demo.unity`; Git also reports line-ending normalization warnings for modified assets. These are not runtime findings and were left untouched.

## Phase 1 - static audit

| Area | Result |
|---|---|
| Compilation and Console | **Build compilation passed** (Windows build succeeded). Current Unity Console was not reviewed exhaustively. Historical logs contain stale errors/warnings; they are not treated as current. |
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
| 1 | Main menu, Continue/New Game/Settings/Feedback/Quit, focus and UI sounds | **Sebagian lulus / sebagian tidak bisa diuji:** Main-Menu rendered in Play Mode and the standalone build; its background video visibly played. Continue/New Game/Settings/Feedback/Quit, keyboard/gamepad focus, settings persistence, browser URL and UI audio were not validated. Injected mouse/key attempts did not activate a menu action, but the input injection may be unreliable and does not establish a game bug. |
| 2 | New Game transition, player spawn/camera/cursor and movement | **Tidak bisa diuji:** no Play Mode/build. |
| 3 | Sword toggle, 3-hit combo/buffer, hit stop, effects and damage | **Tidak bisa diuji:** no combat runtime test. |
| 4 | Bear patrol, detection/chase/attack/reaction/death/leash | **Tidak bisa diuji:** static NavMesh reference only; AI behavior and permanent death not exercised. |
| 5 | Bram interaction/dialogue, choices, input lock and repeat dialogue | **Tidak bisa diuji:** dialogue source was not run; cursor/input recovery unverified. |
| 6 | Quest order, locked backpack, rejection, compass/map and notifications | **Tidak bisa diuji:** no playthrough. Prior quest/tracking fixes and scene edits need an end-to-end rerun. |
| 7 | Turn-in, closing dialogue, one-time Closing transition and clean restart | **Tidak bisa diuji:** no playthrough. |
| 8 | Bear defeat/knockout/respawn soft fail | **Tidak bisa diuji:** no damage/respawn test. |
| 9 | Pause menu | **Tidak bisa diuji / tidak ditemukan:** no pause-menu runtime component was found. |
| 10 | Esc/M/attack during dialogue, map during combat, spam clicks, Alt-Tab, resolution/fullscreen, death in dialogue, save/Continue | **Tidak bisa diuji:** no gameplay interaction was reached; physical keyboard/gamepad and edge cases remain outstanding. |
| 11 | Music, ambience, sword/UI SFX and volume sliders | **Tidak bisa diuji:** audio playback and relative volume require runtime. A UI sound-set asset exists; an older log warning about it is historical. |
| 12 | English text/branding/version consistency | **Sebagian statis:** product/version are correct and a language selector/localization system exists. No automated full-text scan/playthrough established that all visible strings are English or that the footer matches the built executable. |

## Phase 3 - performance

No FPS, frame-time, draw-call, batch or memory samples were collected. The required 1920x1080 menu and forest comparisons were not run. No performance optimizations were made.

## Phase 4 - Windows build

The non-development Windows build succeeded in about 104 seconds at `D:\Builds\QA_CBT` and launched to Main-Menu. The folder contains 210 files totaling 344,163,033 bytes (about 0.32 GiB); the executable is 667,648 bytes. `_BurstDebugInformation_DoNotShip` is present and contains 605,693 bytes; `_BackUpThisFolder_ButDontShipItWithYourGame` is absent. Do not distribute the Burst debug-information folder without checking release policy.

Player.log: `C:\Users\Gozyu\AppData\LocalLow\ExceedOG\The Long Way to You\Player.log`. Startup and scene load reached Main-Menu with no exception/error observed. It logged that Main-Menu has no `ThirdPersonController` for position restore/save and reported an H.264 timestamp correction for the menu video; neither prevented startup. Splash/icon/version were not independently validated. Only the main menu was reached; the forest, quest and Closing flow were not exercised. The player remained open on Main-Menu at the end of this audit.

## Phase 5 - repository review

- Five tracked files exceed 50 MB: four Fantasy knight TGA textures at about 64 MB each, and `UNS_HDRI.hdr` (under the Ultimate Nature Starter environment Skyboxes folder) at about 250.3 MB.
- Third-party asset content is tracked. No assets were deleted.
- Root `Library`, `Temp`, `Logs`, `UserSettings` and `Builds` content is not tracked.
- `README.md` is tracked; `CREDITS.md` is absent.
- Git was used only for read-only inspection and `diff --check`; no commit, push, pull, reset, checkout, stash or ignore-file change was made.

## Menu polish change and backups

For the separate menu-polish task, the implementation changed only:

- [`MenuTextStyle.cs`](../Scripts/Runtime/UI/MenuTextStyle.cs)
- [`MenuTextStyle.asset`](../Resources/MenuTextStyle.asset)

Backups exist at `MenuText2Backup~/MenuTextStyle.cs.bak`, `MenuText2Backup~/MenuTextStyle.asset.bak`, `QABackup~/MenuTextStyle.cs.bak` and `QABackup~/MenuTextStyle.asset.bak`.

Static fallback-image contrast results, menu values, the font and manual visual checks are documented in [`MenuText2_Notes.md`](./MenuText2_Notes.md). The editor Setup Main Menu tool was not run, and the moving video was not evaluated.

Other modifications already present in the worktree from earlier requests were preserved and not changed by this QA pass: river setup/runtime files, `UNS_Demo.unity`, river material/notes, and the TMP font assets. Their earlier backup/notes are in `RiverSetupBackup~` and `RiverSetup_Notes.md`; the island-edge walls and river audio still require editor inspection and Play Mode testing.

## What remains untested

- Current Unity Console warning/error review.
- All gameplay and UI flows, including physical mouse/keyboard/gamepad input, dialogue edge cases, scene load timing and state reset.
- Moving-video contrast, rendering at the requested resolutions, audio mix and device-specific quality.
- Performance, splash/icon/version confirmation, full gameplay in the Windows build, and end-user machine compatibility.

## QA artifacts and backups

- Play Mode and build/input-attempt screenshots are under [`QA~/`](./QA~/). They document the visible menu/build state, not a completed gameplay test.
- Before updating this report, its previous version was copied to [`QABackup~/CBT_QA_Report.md.pre-runtime-update.bak`](./QABackup~/CBT_QA_Report.md.pre-runtime-update.bak).
- The earlier `MenuTextStyle.cs` compile fix has its own backup at [`QABackup~/MenuTextStyle.cs.pre-CS0103.bak`](./QABackup~/MenuTextStyle.cs.pre-CS0103.bak).

## Langkah manual sebelum rilis

1. In the already-open Unity editor, review the current Console and confirm no compile errors remain.
2. Run **Tools > Prince > Setup Main Menu**, then **Tools > Prince > Audit Main Menu Text Contrast**. Check the moving video and all three target resolutions.
3. Run **Tools > Prince > Setup River Environment** and inspect the island-edge wall gizmos; verify the perimeter follows the island rather than cutting across playable terrain.
4. With physical keyboard/mouse input, first confirm New Game responds; then play through every Phase 2 item, especially the full bear → backpack → Bram → Closing → menu loop, and test restart and listed edge cases.
5. Measure menu and forest performance at 1920x1080.
6. Smoke-test the existing non-development Windows player in `D:\Builds\QA_CBT`, inspect logs/splash/icon/version and packaged scenes; rebuild after any release fixes.
7. Before release, decide whether to correct the company-name whitespace, set the intended default resolution/Standalone backend, configure Closing external links, and add/omit a pause menu intentionally.
