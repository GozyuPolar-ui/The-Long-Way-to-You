# Main Menu Text Polish - Stage 2

## Changes

- Dark-on-light remains the default. The title is now warm dark brown `#422D16`; the subtitle and buttons use night blue `#22304F`.
- Menu buttons use the Cormorant Garamond SemiBold TMP asset, Bold weight, a 1.6875 size multiplier (28 to 47.25), and character spacing 3.
- The subtitle is 31.25 with character spacing 2. Footer labels are 24 and copyright is 22.
- Menu outline is 0.06 with softness 0.03. A separate TMP material instance receives the cream underlay (`#FFF6E0`, alpha 0.6, offset (0, -0.5), softness 0.3, dilate 0.1). Copyright has no outline and uses a lighter underlay.
- A shared 76-pixel left margin and 78-pixel button spacing are stored with the style. Text sizes and colors remain adjustable through `MenuTextStyle`.
- Cinzel Decorative Bold remains the title font; the menu button font is Cormorant Garamond SemiBold. Existing layout, button actions, video, audio, feedback and scene flow were not intentionally changed.

## Static contrast check

The fallback still image is `../UI/MainMenu/Gambar backup.jpeg` (2752 x 1536). The checker samples the named 1920 x 1080 reference rectangles at a four-pixel interval and compares mean background luminance with the text color. These are area averages, not per-pixel guarantees and not measurements of the moving video.

| Element | Mean-area contrast |
|---|---:|
| Title | 4.31:1 |
| Subtitle | 6.37:1 |
| Continue | 6.30:1 |
| New Game | 8.55:1 |
| Settings | 11.27:1 |
| Quit | 10.74:1 |
| Footer ExceedOG | 7.90:1 |
| Footer copyright | 7.57:1 |
| Footer BETA TEST | 8.09:1 |
| Footer version | 8.31:1 |

The title exceeds the 3:1 target; subtitle and buttons exceed 4.5:1. Footer values are reported for reference. The title's previous estimated ratio was about 1.79:1; darkening it is what brought it above target.

## Changed files and backups

- `../Scripts/Runtime/UI/MenuTextStyle.cs`
- `../Resources/MenuTextStyle.asset`
- Before-edit copies: `MenuText2Backup~/MenuTextStyle.cs.bak` and `MenuText2Backup~/MenuTextStyle.asset.bak`
- QA copies: `QABackup~/MenuTextStyle.cs.bak` and `QABackup~/MenuTextStyle.asset.bak`

The Setup Main Menu editor tool was not run in this pass. The style asset is applied at runtime by the menu controller, but the updated editor-generated scene/canvas state and final rendering have not been verified.

## Not tested / manual steps

The animated video cannot be assessed with a static image, and Unity was not opened or driven for this pass. After the editor compiles the changes:

1. Run **Tools > Prince > Setup Main Menu**.
2. Run **Tools > Prince > Audit Main Menu Text Contrast** and confirm the values above.
3. Inspect the menu at 1920x1080, 2560x1440 and 1280x720 over the moving video; check focus, disabled Continue, no clipping, and both footer sides.
