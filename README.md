# The Long Way to You

> A calm light fantasy exploration game about a prince, a long road, and a question waiting at the end.

**Developed by ExceedOG** · Status: **Closed Beta (v0.1.0)** · Platform: **Windows PC** · Engine: **Unity 6 (URP)**

---

## About

*The Long Way to You* follows a young prince traveling to a distant kingdom, carrying a letter and a question for the princess who waits there. Along the road he meets the people who live in the wilds, helps them with small favors, and defends himself when the wild stands in his way.

The game is built around a low-pressure, low-stress experience: no rush, no Game Over, just the journey.

## Features (Closed Beta)

- **Third-person exploration** of a handcrafted forest area (one scene per area, not an open world)
- **Characters and dialogue**: talk to the people you meet and take on small quests
- **Quest tracking** with an on-screen compass and off-screen target indicators
- **Light combat**: sword draw and sheath, a three-hit combo, and wild animal enemies
- **Combat feel system**: input buffering, hit stop, camera impulse, hit effects, with a gentle "Lenient" preset
- **Map system**: minimap and full map
- **Cinematic main menu** with a looping video background and a built-in feedback option
- **The opening chapter** of the story

> This is an early build. Expect bugs, unfinished features, and placeholder content. Performance has not been fully optimized and may be demanding on lower-end systems. Progress may reset between versions.

## Controls

| Action | Keyboard & Mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Attack | Left mouse button | West button |
| Draw / sheath sword | R | North button |
| Interact / talk | E | *(see in-game prompts)* |
| Map | M | — |

Controls may change between versions.

## Download

Beta builds are shared with testers directly. Link: _[add download or sign-up link here]_

Windows may show a SmartScreen warning because the build is not code-signed. Choose **More info → Run anyway**.

## Feedback

Your feedback shapes the game. Use the **Feedback** option in the main menu (scan the QR code or open the form in your browser). If the game crashes, please attach your `Player.log`, usually found at:

```
C:\Users\<your name>\AppData\LocalLow\ExceedOG\The Long Way to You\Player.log
```

## Roadmap

Planned for the next update, scene by scene:

- [ ] Campfire to rest and sleep at
- [ ] HP system
- [ ] More varied wild animals along the road
- [ ] More NPCs and quests
- [ ] Additional areas (village, castle, interiors)

## Development

### Requirements

- Unity **6000.5.0f1** (Unity 6.5)
- Universal Render Pipeline (URP)
- Packages: Input System, Cinemachine 3, TextMesh Pro (install *TMP Essential Resources* on first open)

### Open the project

1. Clone the repository.
2. Add the project folder in Unity Hub and open it with the Unity version above.
3. Import third-party packages listed in [CREDITS.md](CREDITS.md) (they are not redistributed in this repository).
4. Open `Assets/_Project/Scene/Main-Menu` and press Play.

### Project structure

```
Assets/
└── _Project/
    ├── Scene/        Main-Menu, forest scene, Closing
    ├── Scripts/      Runtime and Editor scripts
    ├── Audio/        Music and sound effects
    ├── UI/           Menu, icon, feedback assets
    ├── Fonts/        UI fonts
    ├── Data/         ScriptableObject settings
    └── Docs/         Notes for each system
```

Editor tools live under **Tools → Prince** (for example, *Setup Combat Feel*).

### Build

1. **File → Build Profiles**, platform **Windows**.
2. Scene order: `Main-Menu` (0), forest scene (1), `Closing` (2).
3. Turn off *Development Build* and click **Build**.

## Credits

- **Developed by ExceedOG**
- **Story by an anonymous writer**
- Third-party assets, fonts, and tools are listed in [CREDITS.md](CREDITS.md). Each remains under its own license and is not licensed for redistribution through this repository.

## License

Copyright © 2026 ExceedOG. All rights reserved, unless a file states otherwise.

Third-party assets are the property of their respective authors and are used under their own licenses.

## Contact

- GitHub: [GozyuPolar-ui](https://github.com/GozyuPolar-ui)
