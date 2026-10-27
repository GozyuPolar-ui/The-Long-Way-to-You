# Visual styling

How indicators look: the sample prefabs, their visibility settings, and giving targets different prefabs. Read this when the user wants to change icons, arrows, colors, labels, or when each part appears.

## Rules

- Never edit the sample prefabs in `Assets/Target Indicators/Samples/`. Package updates overwrite them. Make a **prefab variant** (or a copy) in the user's own folder.
- Keep the prefab's visibility settings as shipped unless the user wants different behavior; they match the samples. Styling that helps tell targets apart (a color or icon per kind of target) is expected even when not asked for.
- Sample sprites: `Samples/Core Assets/Textures/` (`star_icon`, `triangle_icon`, `cube_icon`, `arrow_icon`, `square_outline`). Ready-made category prefabs: `Samples/Example Assets/Prefabs/`.
- TMP Essential Resources must be imported or distance labels show no text.

## Make a variant

In the Project window: right-click `Samples/Core Assets/Prefabs/Visual Indicator.prefab` > **Create > Prefab Variant**, move it to the user's folder, edit it, then assign it as the `VisualIndicatorManager`'s **Default Visual Indicator Prefab**.

In code: `PrefabUtility.InstantiatePrefab` the source, `PrefabUtility.SaveAsPrefabAsset(instance, newPath)` (this creates a variant), destroy the instance, then edit with `PrefabUtility.LoadPrefabContents(newPath)`, `SaveAsPrefabAsset`, `UnloadPrefabContents`. Assign through `visualIndicatorManager.DefaultVisualIndicatorPrefab`.

## Prefab structure

```
Visual Indicator            VisualIndicator component
└─ Offset
   ├─ Rotation Content      rotated toward the target (assigned as Rotation Content)
   │  └─ Arrow              art points right (+X), offset along +X
   ├─ Distance Label        TextMeshPro text, shows "12 m" (assigned as Distance Label)
   └─ Core Content          assigned as Core Indicator Content
      └─ Icon, Background, Background Shadow, Outline
```

- Restyle anything under `Core Content` and `Rotation Content` (sprites, colors, sizes). The icon is `Offset/Core Content/Icon`.
- **Core Indicator Content** and **Rotation Content** must stay assigned; `Awake` throws without them.
- A new arrow must point right and sit offset along +X inside `Rotation Content`. The package rotates that node toward the target.
- Indicator roots get their anchors and pivot set automatically.

`Compass Tape VisualIndicator.prefab` follows the same idea with a `CompassTapeVisualIndicator` component. Compass managers only accept prefabs with that component.

## When parts appear

Set on the prefab's `VisualIndicator` (or at runtime on an instance):

| Setting (property) | Options | Default (screen / compass) |
|---|---|---|
| Core Indicator Visibility (`CoreContentVisibility`) | `Never`, `Always`, `OutsideBoundary`, `InsideBoundary` | `Always` / `InsideBoundary` |
| Rotation Content Visibility (`RotationContentVisibility`), the arrow | same | `OutsideBoundary` / ignored |
| Distance Label Visibility (`DistanceLabelVisibility`) | `Never`, `Always`, `LookAt` | `LookAt` / `Always` |
| Look At Dot Threshold (`LookAtDotThreshold`) | 0–1; higher = look more directly | 0.95 |
| Distance Label Fade Time (`DistanceLabelFadeTime`) | seconds | 0.15 |

Examples, only when the user asks for that behavior: `OutsideBoundary` on the core shows markers only for off-screen targets; on a compass prefab, `Always` pins out-of-view markers to the tape ends instead of hiding them (the shipped `InsideBoundary` hides them).

## A different prefab per target

1. Make one variant per category.
2. Set the `VisualIndicatorManager` (or `CompassTapeVisualIndicatorManager`) **Add Indicator Mode** to `Manual`.
3. Add each target through the UI layer: `visualIndicatorManager.TryAddVisualIndicator(target, prefab, out var id)`. Remove with `RemoveTargetIndicator(id)`; hide without untracking with `HideVisualIndicator(id)` / `ShowVisualIndicator(id)`.

In `Auto` mode every target gets the default prefab and targets are added on the core manager. `Multiple Categories Demo.unity` and `Compass Tape Demo.unity` show `Manual` mode.

## Common styling issues

| Symptom | Fix |
|---|---|
| Changes lost after a package update | A sample prefab was edited. Use a variant in the user's folder. |
| `NullReferenceException` in `VisualIndicator.Awake` | Assign Core Indicator Content and Rotation Content. |
| Arrow points the wrong way or orbits oddly | Arrow art must point right, offset along +X inside Rotation Content. |
| Per-target prefab ignored | Layer is in `Auto` mode, or the target was added on the core manager. |
| Compass manager rejects the prefab | It needs `CompassTapeVisualIndicator`, not `VisualIndicator`. |
