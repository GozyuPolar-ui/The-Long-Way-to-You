# UI SFX Setup Notes

## Inspection

- `Assets/_Project/Audio/SFX/UI` contains 100 audio files. All are `.ogg` files using Vorbis audio; durations below were read with `ffprobe`.
- The existing menu is built at runtime by `MainMenuController` and `MenuUIFactory`. Settings controls are built by `SettingsPanel`; the menu already has Master, Music, and Effects sliders.
- `MapSystem` builds its map controls and already has open/close audio fields, both currently unassigned in `MapSettings.asset`. It has a `MapMarkerTooltip` pointer-focus hook.
- `QuestDialogueUI` builds dialogue choice buttons at runtime. `QuestManager` already exposes `OnQuestChanged` and `OnQuestCompleted`; no quest event was needed.
- `QuestItem_Backpack.prefab` has an unassigned `pickupSound`. Its bear-guarded rejection and successful pickup now use UI SFX.
- The Closing feedback control reveals a QR panel. There is no separate `FeedbackPanel` or pause-menu implementation under `Assets/_Project/Scripts`.
- There was no `UISoundPlayer` before this change. No project AudioMixer assets/groups were found, so the sound source defaults to the AudioListener/Master output. The existing Effects slider already applies to non-music AudioSources.

## Audio inventory

All entries are Vorbis `.ogg`; numbers after each family prefix are suffixes. Durations are seconds.

| Family | Files and duration |
|---|---|
| back | `back_001` 0.064, `back_002` 0.070, `back_003` 0.093, `back_004` 0.073 |
| bong | `bong_001` 0.123 |
| click | `click_001` 0.100, `click_002` 0.010, `click_003` 0.010, `click_004` 0.010, `click_005` 0.010 |
| close | `close_001` 0.148, `close_002` 0.314, `close_003` 0.314, `close_004` 0.323 |
| confirmation | `confirmation_001` 0.290, `confirmation_002` 0.539, `confirmation_003` 0.322, `confirmation_004` 0.490 |
| drop | `drop_001` 0.110, `drop_002` 0.191, `drop_003` 0.191, `drop_004` 0.287 |
| error | `error_001` 0.165, `error_002` 0.165, `error_003` 0.533, `error_004` 0.103, `error_005` 0.500, `error_006` 0.500, `error_007` 0.192, `error_008` 0.139 |
| glass | `glass_001` 0.278, `glass_002` 0.125, `glass_003` 0.124, `glass_004` 0.692, `glass_005` 0.111, `glass_006` 0.111 |
| glitch | `glitch_001` 0.020, `glitch_002` 0.030, `glitch_003` 0.010, `glitch_004` 0.023 |
| maximize | `maximize_001` 0.258, `maximize_002` 0.258, `maximize_003` 0.212, `maximize_004` 0.418, `maximize_005` 0.526, `maximize_006` 0.380, `maximize_007` 0.186, `maximize_008` 0.225, `maximize_009` 0.225 |
| minimize | `minimize_001` 0.258, `minimize_002` 0.258, `minimize_003` 0.212, `minimize_004` 0.418, `minimize_005` 0.526, `minimize_006` 0.380, `minimize_007` 0.186, `minimize_008` 0.225, `minimize_009` 0.225 |
| open | `open_001` 0.148, `open_002` 0.314, `open_003` 0.314, `open_004` 0.323 |
| pluck | `pluck_001` 0.102, `pluck_002` 0.165 |
| question | `question_001` 0.491, `question_002` 0.333, `question_003` 0.332, `question_004` 0.332 |
| scratch | `scratch_001` 0.139, `scratch_002` 0.139, `scratch_003` 0.123, `scratch_004` 0.325, `scratch_005` 0.325 |
| scroll | `scroll_001` 1.000, `scroll_002` 1.000, `scroll_003` 1.000, `scroll_004` 1.000, `scroll_005` 1.000 |
| select | `select_001` 0.043, `select_002` 0.043, `select_003` 0.383, `select_004` 0.383, `select_005` 0.383, `select_006` 1.944, `select_007` 0.047, `select_008` 0.047 |
| switch | `switch_001` 0.618, `switch_002` 0.611, `switch_003` 0.500, `switch_004` 0.500, `switch_005` 0.612, `switch_006` 0.611, `switch_007` 0.614 |
| tick | `tick_001` 0.023, `tick_002` 0.023, `tick_004` 0.055 (`tick_003` is absent) |
| toggle | `toggle_001` 0.139, `toggle_002` 0.139, `toggle_003` 0.139, `toggle_004` 0.066 |

Duration and level analysis was possible. For the selected candidates, decoded RMS/peak examples include `tick_001` 0.0423/0.4822, `click_001` 0.0449/0.7121, `open_001` 0.0315/0.1703, `close_001` 0.0318/0.2254, `error_008` 0.0935/0.9191, `select_008` 0.0291/0.2355, `maximize_003` 0.0423/0.4960, `minimize_003` 0.0427/0.4055, `scroll_001` 0.0447/0.7702, and `pluck_002` 0.0926/0.6307. The long scroll sample is kept very quiet and rate-limited.

## Sound mapping

The `Tools > Prince > Setup UI Sounds` tool creates or updates `Assets/_Project/Resources/UISoundSet.asset`. Each mapped type uses ±5% pitch variation. The setup tool refreshes clip assignments but preserves existing per-type volume, pitch, and cooldown edits in an existing asset.

| UI sound type | Clip variants |
|---|---|
| Hover | `tick_001.ogg`, `tick_002.ogg` |
| Click | `click_001.ogg`, `click_004.ogg` |
| Back | `close_001.ogg`, `close_002.ogg` |
| Open | `open_001.ogg`, `open_002.ogg` |
| Close | `close_001.ogg`, `close_003.ogg` |
| Toggle | `toggle_003.ogg`, `toggle_004.ogg` |
| SliderTick | `tick_001.ogg`, `tick_002.ogg` |
| DropdownOpen | `open_001.ogg`, `open_002.ogg` |
| DropdownSelect | `select_008.ogg`, `select_007.ogg` |
| Error | `error_008.ogg`, `error_002.ogg` |
| Confirmation | `confirmation_003.ogg`, `confirmation_004.ogg` |
| QuestUpdated | `confirmation_003.ogg`, `confirmation_004.ogg` |
| QuestCompleted | `confirmation_001.ogg`, `confirmation_004.ogg` |
| DialogueAdvance | `tick_001.ogg`, `tick_002.ogg` |
| DialogueChoice | `select_008.ogg`, `select_007.ogg` |
| MapOpen | `maximize_003.ogg`, `maximize_001.ogg` |
| MapClose | `minimize_003.ogg`, `minimize_001.ogg` |
| MapMarker | `click_001.ogg`, `click_004.ogg` |
| MapZoom | `scroll_001.ogg`, `scroll_004.ogg` |
| MapPan | `scroll_001.ogg`, `scroll_004.ogg` |
| Pickup | `pluck_001.ogg`, `pluck_002.ogg` |

Every mapped type has a clip match; `None` intentionally has no clip. `bong`, `glass`, `glitch`, `scratch`, `drop`, and `question` variants are not used. No hard/distorted `glitch` or `scratch` file is selected. `scroll_001` is the quietest scroll candidate by RMS; all scroll clips are one second long.

## Changes and behavior

- Added `UISoundSet`, `UISoundPlayer`, and `UIButtonSound`. The player creates a persistent 2D source pool, uses `PlayOneShot`, honors per-type cooldowns, selects random variants/pitch, and subscribes to the existing quest events.
- `UISoundPlayer` automatically attaches `UIButtonSound` once to Selectables in `Main-Menu`, `UNS_Demo`, and `Closing`. Shared runtime button factories attach it before action listeners are registered, including dynamically generated dialog choices and map buttons. `playOnFirstSelect` defaults off; disabled controls do not hover, and a disabled click/submit gets a rate-limited Error sound.
- Added small hooks for map open/close/zoom/pan and marker focus/ping; dialog open/close/line advance; the existing quest changed/completed events; rejected backpack interaction; and pickup. Existing map sounds are retained when assigned. The backpack's existing pickup clip is currently empty, so it uses the configured UI pickup variants.
- Menu confirmation open/close buttons and Closing feedback/contact panel toggles avoid double-playing generic and panel sounds.
- The existing Effects slider already applies its value to non-music AudioSources, including the persistent UI SFX player. No SettingsPanel layout or logic change was needed.
- No separate pause menu or FeedbackPanel exists in the project code. Closing's feedback QR toggle is wired.

## Inspector parameters and defaults

`UISoundSet` entries expose `AudioClip[]`, `volume` (0-1), `pitchRange` (default 0.95-1.05), and `cooldown` in seconds. The initial volumes/cooldowns are intentionally lower for hover, errors, quests, map pan/zoom, and dialogue ticks. Slider ticks use 0.06 seconds; map zoom and pan use 0.12 seconds. The player has eight 2D playback channels to keep overlapping sounds' pitch variation independent. Select a UI or SFX AudioMixer group in the asset if one is added later; otherwise the source uses AudioListener/Master output.

## Backups

Before editing the existing scripts, copies were created in `Assets/_Project/Docs/UiSfxBackup~`:

- `MainMenuController.cs.bak`
- `MenuUIFactory.cs.bak`
- `MapSystem.cs.bak`
- `MapMarkerTooltip.cs.bak`
- `QuestDialogueUI.cs.bak`
- `QuestPickup.cs.bak`
- `ClosingSceneController.cs.bak`

## Validation and manual steps

Static inspection confirmed the named clip files and durations, all 100 files decode as Vorbis, all 21 mapped sound types have clip assignments, the existing quest events/settings slider are reused, and the `QuestItem_Backpack` pickup clip is unassigned. Unity compilation, actual playback, perceived loudness against music, dropdown/keyboard/gamepad behavior, interaction timing, and scene transitions were not tested because the Editor and Play mode were not opened.

1. In Unity, run **Tools > Prince > Setup UI Sounds** once. It creates the Resources sound-set asset and logs that the project currently has no UI/SFX/Master AudioMixer group; AudioListener/Master output is used.
2. Inspect `Assets/_Project/Resources/UISoundSet.asset`; tune per-type volume, pitch range, cooldown, or variants as desired. Keep the Effects slider as the global level control.
3. Test menu hover/focus, New Game/Continue and confirmation sounds while the scene fades; test Effects slider, fullscreen toggle, language/quality dropdowns, and Settings back.
4. In `UNS_Demo`, test map open/close, marker focus/ping, zoom/pan, dialogue lines/choices, quest start/update/turn-in, backpack rejection while the bear lives, and pickup after defeat.
5. In Closing, test feedback/contact panel open and close and confirm the sounds continue normally after scene transitions.
6. To restore an edited source, close Unity and copy the matching `.bak` from `Assets/_Project/Docs/UiSfxBackup~` over that source file. Do not restore unrelated files.
