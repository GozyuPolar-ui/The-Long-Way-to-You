---
name: target-indicators
description: "Use this skill for any work with Target Indicators in Unity: off-screen indicators, waypoint or objective markers, arrows pointing to targets, damage-direction rings, or compass bars. Covers how the package is structured, end-to-end setup, the TargetIndicatorManager API, boundaries (padding, oval, fixed size, compass tape), update timing, debugging, and styling indicators (icons, arrows, visibility, per-target prefabs)."
metadata:
  asset: "Target Indicators"
  publisher: "Jake Manfre"
  asset-version: "1.5.0"
  skill-version: "2.5.0"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/gui"
  asset-store-url: "https://assetstore.unity.com/packages/slug/324272"
  documentation-url: "https://jakemanfre.github.io/target-indicators.github.io/manual/index.html"
  support-url: "https://jakemanfre.github.io/target-indicators.github.io/manual/support-and-feedback.html"
  last-verified: "2026-10-10"
---

# Target Indicators

Target Indicators turns the world positions of tracked `Transform`s into screen poses (position and rotation) clamped to a boundary. Two layers:

- **Core** (`TargetIndicators`, `Runtime/`): `TargetIndicatorManager` tracks targets, computes poses every frame, and raises events. It draws **no UI**.
- **Sample UI layer** (`TargetIndicators.Samples`, `Samples/Core Assets/`): `VisualIndicatorManager` listens to a core manager and spawns uGUI indicator prefabs; `CompassTapeVisualIndicatorManager` does the same for compass bars. Replaceable with your own code.

## When to use this skill

- Markers that stick to the screen edge, tracking spawned enemies, compass bars
- Keeping indicators off the HUD, oval or fixed-size boundaries
- Restyling indicators, or "my markers don't show up"

## Prerequisites

- Installed if `System.Type.GetType("TargetIndicators.TargetIndicatorManager, TargetIndicators") != null`; otherwise point to https://assetstore.unity.com/packages/slug/324272.
- Samples need `com.unity.ugui`, `com.unity.inputsystem`, and TMP Essential Resources (**Window > TextMeshPro > Import TMP Essential Resources**).
- Scripts in an `.asmdef` must reference `TargetIndicators` (and `TargetIndicators.Samples`).
- In Editor code snippets, fully qualify types (`UnityEngine.Object`, not the ambiguous `Object`) and skip `using` lines: execute-C# tools often reject them.

## Quick start

Markers clamped to the screen edge, as in `Samples/Scenes/Simple Demo.unity`:

1. Add a GameObject `Target Indicators Manager` with a `TargetIndicatorManager` (it finds the main camera). Keep **Boundary Type** `Padded` with an even padding of 60 on every side, so indicators sit inside the screen edge (the default is 0). If the scene has HUD near an edge (health bar, minimap), raise that side to clear it, and tell the user the values.
2. Add a **Screen Space - Overlay** Canvas `Target Indicators Canvas` (Scale With Screen Size, 1920×1080) with a child `Visual Indicators Content`: a `RectTransform` stretched over it (anchors (0,0)–(1,1), zero offsets).
3. Add a separate GameObject `Visual Indicator Manager` with a `VisualIndicatorManager` in `Auto` mode. Assign its manager, canvas, content, and default prefab `Samples/Core Assets/Prefabs/Visual Indicator.prefab`, and enable **Sync Boundary Scale With Canvas** so the padding is in canvas units. These are private fields (`_targetIndicatorManager`, `_canvas`, `_content`, `_defaultVisualIndicatorPrefab`, `_syncBoundaryScaleWithCanvas`); in code, set them with `SerializedObject` in edit mode.
4. Add targets with `targetIndicatorManager.TryAddTarget(target, out var indicator)`, or the sample `SimpleTargetIndicatorSetupExample` with a `_targets` list.
5. Save the scene and enter Play mode.

Expected result: one `Visual Indicator(Clone)` per target, on the target when visible and pinned to the edge with an arrow when not.

## Workflows

### Workflow: Track targets from code

`TryAddTarget(transform, out var indicator)` returns `false` for null or a full manager (100); keep `indicator.Id`. `TryRemoveTarget(id)` stops tracking; destroyed targets are removed automatically. The events `TargetIndicatorsAdded`/`Updated` (`ReadOnlySpan<TargetIndicator>`) and `TargetIndicatorsRemoved` (`ReadOnlySpan<TargetIndicatorId>`) fire in the manager's `Update`; copy values out, since the span is reused. `TargetIndicator` holds `ScreenPose` (pixels from the bottom-left; for `CompassTape`, x is a 0–1 heading, 0.5 = ahead), `IsOutsideBoundary`, `Distance`, `LookAtDot` (1 = looking at it) and `Target`.

### Workflow: Choose the boundary

`BoundaryType`: `Padded` (default; screen edges inset by four paddings), `Absolute` (fixed `Width` × `Height` at center, for damage rings or XR), `Unbounded`, `CompassTape`. `BoundaryShape = Ellipse` makes an oval. Sizes are pixels × `BoundaryScale`; **Sync Boundary Scale With Canvas** switches to canvas units. Several managers can run side by side, each with its own UI layer. **Add Boundary Visualizer** draws the boundary; its outline appears on the editor's next update and marks the scene modified, so save again then. Remove it before shipping.

### Workflow: Compass bar

1. A `TargetIndicatorManager` with `BoundaryType = CompassTape`.
2. On a Screen Space - Overlay canvas, the visible tape: a `RectTransform` anchored and pivoted at top center (0.5, 1), for example 800×10 at y = -95, with a faint `Image`. Its width is the visible tape length.
3. A `CompassTapeVisualIndicatorManager` (not `VisualIndicatorManager`) in `Auto` mode, with the Quick start fields, the tape as its content, and `Samples/Core Assets/Prefabs/Compass Tape VisualIndicator.prefab` as the default prefab.
4. **Always set `FullTapeToVisibleTapeRatio`** (private `_fullTapeToVisibleTapeRatio`): the tape spans 360° / ratio. Use 3.6 (100°, the sample) unless the user asks for a different span. The default 0 bunches all markers in the middle.

Keep the prefab's visibility as shipped: markers for targets outside the tape's span hide, as in the sample. `CompassForwardReferenceOverride` measures heading from a body instead of the camera.

### Workflow: Style indicators and tell targets apart

Never edit the sample prefabs; make prefab variants in the user's folder. If targets are meant to be told apart, give each kind its own marker even when the user doesn't say so: when they differ by name or color (`Flag_Red`, `Flag_Blue`), team, or category. Make one variant per kind (for example tinted red and blue), set the UI layer's **Add Indicator Mode** to `Manual`, and add each target with `TryAddVisualIndicator(target, prefab, out var id)`, as `Compass Tape Demo.unity` does per player. Identical targets can share the default prefab in `Auto` mode. **Read `references/visual-styling.md`** for the prefab structure, visibility settings, and examples.

### Workflow: Timing, cameras, custom UI

- **One-frame lag** (camera moves in `LateUpdate` or Cinemachine): disable the manager and call `GetChanges()` after the camera moves. Never both.
- **Camera swap:** set `manager.Camera`.
- **Own UI** (pooling, UI Toolkit): handle the three events yourself; `references/custom-ui-layers.md` has implementations.

## Verification

- No Console errors in Play mode; `TrackedTargetsCount` matches the targets added, with one indicator each.
- A target in front of the camera reports `IsOutsideBoundary == false`, one behind it `true` (`CompassTape` and `Unbounded` always report `false`).
- Nothing under `Assets/Target Indicators/` was modified, and the scene is saved.
- If Play mode doesn't advance (`Time.frameCount` stays put) while Unity is in the background, **Run In Background** is off in Player Settings. Don't wait or change the user's settings: call `manager.GetChanges()` and check with `TryGetTargetIndicator`, or ask the user to focus Unity.

## API quick reference

| Member | Purpose |
|---|---|
| `TryAddTarget`, `TryRemoveTarget`, `TryGetTargetIndicator` | Track and query targets |
| `TargetIndicatorsAdded` / `Updated` / `Removed` | Per-frame events |
| `GetScreenPose(world, out isOutside)`; `IsOutsideBoundary(screenPoint)` | One-off queries (screen point, not world) |
| `GetChanges()` | Run one update manually |
| `VisualIndicatorManager`: `AddIndicatorMode`, `TryAddVisualIndicator`, `RemoveTargetIndicator` | Sample UI layer |

## Common issues

| Symptom | Fix |
|---|---|
| Targets tracked but no UI | Add a UI layer. In `Manual` mode, add through `TryAddVisualIndicator`. |
| Two indicators per target | Two `Auto` UI layers on one manager; keep one. |
| Indicators offset or wrong size | Stretch the content over a Screen Space - Overlay canvas. |
| Compass markers all in the middle | `FullTapeToVisibleTapeRatio` is 0. |
| Warning "Camera is missing", poses zero | Assign `manager.Camera`. |
| `NullReferenceException` in a sample component | Assign its fields in edit mode; on prefabs, assign Core Indicator Content and Rotation Content. |
| No distance text | Import TMP Essential Resources. |

## Boundaries

- At most 100 targets per manager; the core has no rendering; the sample layer is uGUI only and doesn't pool.
- World-space XR UI isn't supported; XR uses a Screen Space - Camera canvas with an `Absolute` boundary.
