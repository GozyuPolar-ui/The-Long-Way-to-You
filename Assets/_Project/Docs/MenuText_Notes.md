# Main Menu and Closing Text Readability

## Inspection and cause

- The existing menu background layers are ordered `MenuFallbackBackground` (RawImage), `MenuVideoImage` (RawImage), `MenuBackgroundGradient` (Image), then `MenuVideoPlayer`. The runtime-created menu text is added afterward, so the gradient is above the video and below the text. Its `raycastTarget` is disabled.
- `MenuBackgroundGradient` is serialized with a centered 100 x 100 RectTransform, but `MenuVideoBackground.BuildGradient()` expands it at runtime. The runtime gradient starts at 0.6 alpha on the left edge, falls quadratically, and ends at 55% of the screen width. Its opacity is therefore much lower across the right side of the text area; meanwhile, the original cream labels have little contrast against the bright fallback/video.
- Static fallback image: `Gambar backup.jpeg`, 2752 x 1536. The same still image is used when the video is unavailable.
- The Main Menu CanvasScaler is already configured for Scale With Screen Size, 1920 x 1080 reference resolution, and Match 0.5. The setup continues to preserve that configuration.

## Changes

- Added the editable `MenuTextStyle` ScriptableObject at `../Resources/MenuTextStyle.asset`. `Tools > Prince > Setup Main Menu` now assigns it to the existing controller and runs the static contrast audit. The setup remains repeatable and does not recreate menu logic.
- Default `DarkOnLight` style:
  - Buttons: night blue `#22304F`, 35 px (28 px base x 1.25), SemiBold, left-aligned to the title margin; hover/focus `#9A6B1F`, pressed `#6E4A12`, and disabled Continue text/outline at 45% alpha.
  - Title: warm dark gold `#925F15`, 78 px, Bold. The requested `#B7791F` was tested but reached only 2.4:1 over the title image region; `#925F15` keeps the warm-gold look and passes the 3:1 target.
  - Subtitle: `#3B2A1A`, 25 px, SemiBold.
  - Footer: night blue `#22304F`; ExceedOG, BETA TEST, and version are 24 px; copyright is 21 px.
  - TMP outline and underlay use per-label font material instances, so the shared font materials are not edited. Cream outline thicknesses are 0.12 for buttons, 0.15 for the title, 0.08 for the subtitle, and 0.1 for the footer. Underlay offsets are 0.35 / -0.35 with softness 0.05.
  - The left gradient is disabled in this default mode. No background box was added.
- Optional `LightOnDark` mode is available in the same asset. It uses cream text and a night-blue gradient at 0.65 alpha across 50% of the screen. The gradient remains raycast-disabled and is positioned between the video and text layers.
- Closing scene text and button labels use the same style asset. In `DarkOnLight` mode, its dark bottom gradient is disabled so it does not further darken text backgrounds; Closing fade, navigation, feedback, and scene transitions are unchanged.
- Button actions, settings, video, music, feedback, keyboard/gamepad focus, title fade, and scene flow were not modified.

## Static contrast audit

Ratios use average pixel luminance in each element's approximate 1920 x 1080 text region on the still fallback image, compared with the face text color using the WCAG relative-luminance formula. The editor tool reports each region separately.

| Element | Mean background luminance | Final face-color contrast |
|---|---:|---:|
| Title | 0.619 | 3.45:1 |
| Subtitle | 0.695 | 9.72:1 |
| Continue | 0.832 | 10.99:1 |
| New Game | 0.819 | 10.84:1 |
| Settings | 0.845 | 11.16:1 |
| Quit | 0.691 | 9.24:1 |
| Footer ExceedOG | 0.140 | 2.36:1 |
| Footer copyright | 0.133 | 2.28:1 |
| Footer BETA TEST | 0.081 | 1.64:1 |
| Footer version | 0.082 | 1.65:1 |

Title, subtitle, and button face colors meet their specified minimum ratios. Footer face-color ratios are lower because those text boxes cover the dark forest/flower area; their cream outline and offset underlay provide an additional visible edge. The static audit measures face color against the area mean and does not model glyph coverage, outline contribution, or motion in the video.

## Files and backups

Modified existing scripts (originals copied before editing):

- `../Scripts/Editor/PrinceMainMenuSetup.cs`
- `../Scripts/Runtime/UI/MainMenuController.cs`
- `../Scripts/Runtime/UI/MenuButtonFX.cs`
- `../Scripts/Runtime/UI/ClosingSceneController.cs`

Backups are in [`MenuFixBackup~`](./MenuFixBackup~) with the `.bak` suffix. New files:

- `../Scripts/Runtime/UI/MenuTextStyle.cs` and its Unity `.meta`
- `../Scripts/Editor/PrinceMainMenuTextContrastAudit.cs` and its Unity `.meta`
- `../Resources/MenuTextStyle.asset` and its Unity `.meta`

The Main-Menu and Closing scenes were not opened, saved, or modified by this task.

## Not tested and manual steps

- Unity compilation, final appearance over the moving video, TMP shader rendering, pointer hover, and the Closing scene were not runtime-tested. No Unity window or Play Mode was used.
- In Unity, run **Tools > Prince > Setup Main Menu** once. Confirm the setup log reports passing title, subtitle, and button contrast values.
- Select `Assets/_Project/Resources/MenuTextStyle.asset` and confirm `Text Style` is `DarkOnLight`. To preview the optional alternative, set it to `LightOnDark`, then run the setup tool again; set it back to `DarkOnLight` and rerun to restore the default.
- Run **Tools > Prince > Audit Main Menu Text Contrast** to print per-region measurements at any time.
- Enter Play Mode and inspect the menu at 1920 x 1080, 2560 x 1440, and 1280 x 720. Check title fade, keyboard/gamepad focus, hover/pressed states, and Continue with and without a save.
- Complete the backpack turn-in flow to inspect Closing text and buttons over the active video. Confirm the fade, feedback controls, and return-to-menu action still work.
