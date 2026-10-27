# Main Menu Notes

## Assets verified

- Menu video: `Assets/_Project/UI/MainMenu/gemini_generated_video_728ee3eb.mp4`.
- Still fallback: `Assets/_Project/UI/MainMenu/Gambar backup.jpeg`.
- Selected BGM: `Assets/_Project/Audio/MAIN MENU/royaltyfreemusicstudio-medieval-fantasy-music-462199.mp3`.
- Extracted title font: `Assets/_Project/Fonts/Cinzel_Decorative,Cormorant_Garamond,MedievalSharp/Cinzel_Decorative/CinzelDecorative-Bold.ttf`.
- Extracted menu font: `Assets/_Project/Fonts/Cinzel_Decorative,Cormorant_Garamond,MedievalSharp/Cormorant_Garamond/static/CormorantGaramond-SemiBold.ttf`.
- Cinzel Decorative and MedievalSharp also appear in the separately extracted `Cinzel_Decorative,MedievalSharp` directory. The setup uses the combined extraction's Cinzel Bold and Cormorant SemiBold files; it does not modify or delete either extracted copy.
- `Tools > Prince > Setup Main Menu` creates persistent TMP Font Assets in `Assets/_Project/Fonts/TMP` when absent, reuses them on later runs, and assigns Cinzel Decorative Bold to the title and Cormorant Garamond SemiBold to the rest of the menu. If font generation fails, it falls back to LiberationSans SDF.
- No Kenney UI border sprite or AudioMixer asset/group was found under `Assets`.

## Changes

- `Tools > Prince > Setup Main Menu` now reuses the existing MainMenu camera, Canvas, fader, and EventSystem. It only refreshes the video background layers and updates audio/UI component references, avoiding duplicate menu objects.
- The left background gradient uses a soft blue-gray tint, starts at alpha 0.6, and fades smoothly across 55% of the screen. Its color, alpha, and width are configurable.
- TMP labels use a subtle dark blue-gray shadow. Main menu text, settings labels, and confirmation strings are English and centralized in `MenuStrings`.
- Menu text uses warm parchment and muted gold instead of plain white; the title has a brighter gold accent.
- The startup `SceneFader` now releases raycast blocking after its initial fade. Previously its transparent overlay continued intercepting pointer input across the entire menu.
- Removed the stale `MenuAudioChannel` component and its embedded MonoScript reference from `MenuVideoPlayer`, which caused Unity's missing-script warning. The menu's dedicated music and video-background components already handle volume routing without this marker.
- The Input System UI setup restores its default actions if pointer, click, navigation, or submit bindings are missing; keyboard/controller navigation remains enabled.
- The main title, subtitle, buttons, and two-sided studio/version footer fade together with unscaled time; the fade is capped at 1.5 seconds.
- Footer displays `ExceedOG`, a copyright line using the current year, `BETA TEST`, and `Application.version`. If the application version is empty, the displayed fallback is `v0.0.0`. Current Player Settings version inspected read-only: `0.1.0`.
- Setup disables video audio (`useVideoAudio = false`, VideoPlayer audio output `None`, and its AudioSource muted). A separate `MenuMusic` AudioSource is configured to play the selected BGM in a loop, fade in over 1.5 seconds, and fade out with `SceneFader`.
- The setup configures the BGM importer for Streaming, background loading, and audio preload. Before running setup, the existing MP3 importer still has `loadType: 0` and `preloadAudioData: 0`. The Music slider controls `MenuMusic` source volume and Master controls `AudioListener.volume`.
- The setup keeps exactly one AudioListener on the menu camera, adds it if missing, and warns when it finds none or removes duplicates.
- Static scene inspection found one existing AudioListener on `MainMenuCamera` and a `MenuMusic` AudioSource referencing the chosen MP3. Before setup, that source has looping enabled, is unmuted, and has volume 0.48, but Play On Awake is off and it has no mixer group. The setup tool applies the final playback settings.
- No AudioMixer exists in the project, so there is no `Music` group to connect. BGM uses AudioSource volume directly; an absent mixer is an intentional fallback and does not produce a runtime warning.
- The project Product Name remains `Prince`; neither it nor any ProjectSettings value was changed. The old Indonesian game title was present only in the previous main-menu title and a notes heading; both were updated. Dialogue and quest content was left untouched.

## Tunable values

- `MainMenuController`: `titleFont` (Cinzel Decorative Bold), `menuFont` (Cormorant Garamond SemiBold), `backgroundDim` (0.6), `gradientWidth` (0.55), `gradientColor` (blue-gray), `screenMargin` (76 reference pixels), `titleFadeDuration` (1.5 seconds).
- `MenuVideoBackground`: `useVideoAudio` (false), gradient color/alpha/width, and video transition duration.
- `MenuMusicPlayer`: `baseVolume` (0.48) and `fadeInDuration` (1.5 seconds).
- Footer version is read from `Application.version`; copyright year is read from `DateTime.Now.Year`.

## Verification limits

Static inspection and source checks were performed without opening Unity. Actual cloud/white-sky text contrast, BGM playback, mixer/device output, and the fade at runtime cannot be verified here.

## Manual steps

1. Open the project in Unity and allow compilation and font asset import to finish.
2. Run `Tools > Prince > Setup Main Menu`. The tool creates either missing TMP Font Asset under `Assets/_Project/Fonts/TMP`, updates the existing scene, and does not change Build Settings or other ProjectSettings.
3. Check Console for unexpected warnings. Check that `MenuMusic` references the selected MP3, loops, is unmuted, and has nonzero serialized volume.
4. Inspect the camera for exactly one AudioListener and the video player for disabled audio output.
5. In Game view, check Cinzel/Cormorant rendering, readability, and footer placement at 1280x720, 1920x1080, and 2560x1440; test BGM, Music/Master sliders, and fade-out when starting or continuing.
6. Unity may report that the source MP4 has unexpected H.264 timestamps and correct them during playback. This is a non-fatal decoder warning; the clip is intentionally kept untranscoded, and correcting the source timestamps would require replacing or re-encoding the video.
