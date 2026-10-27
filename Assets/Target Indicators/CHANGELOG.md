---
uid: target-indicators-changelog
---
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 10/11/2026

### Added
- Added [TargetIndicatorManager.BoundaryScale](xref:TargetIndicators.TargetIndicatorManager.BoundaryScale) with a matching serialized field that multiplies the padding, width, and height values. When your Canvas scales with screen size, set it to your UI's scale factor, such as `Canvas.scaleFactor`, so the padding stays consistent with your visual indicators at every resolution. It defaults to `1`, so existing boundaries are unchanged. If you use your own visual indicator system, you are responsible for setting this value.
- Added `VisualIndicatorManager.SyncBoundaryScaleWithCanvas` with a matching serialized field to keep `TargetIndicatorManager.BoundaryScale` in sync with the Canvas's `scaleFactor`. It is disabled by default.
- Added the `target-indicators` AI Skill in `Assets/Target Indicators/AI/Skills/` so AI agents can set up and use Target Indicators in the Unity Editor. It covers how the package is structured, end-to-end setup, the `TargetIndicatorManager` API, boundaries, compass tapes, styling indicators, and debugging.

### Deprecated
- Deprecated `CompassTapeUIController`. It is not used by the samples and is no longer maintained. It is marked `[Obsolete]` and will be removed in a future version.

### Fixed
- Improved Inspector tooltips.
- Undoing **Add Boundary Visualizer** or **Remove Boundary Visualizer** in the `TargetIndicatorManager` Inspector returns the scene to its saved state, with no unsaved changes.
- **Remove Boundary Visualizer** can be undone and redone any number of times, and the Editor stays stable.
- Fixed `TargetIndicatorManager` returning default poses for all targets when `BoundaryType` is `CompassTape` and [CompassForwardReferenceOverride](xref:TargetIndicators.TargetIndicatorManager.CompassForwardReferenceOverride) is set but the `Camera` is `null`. The override is now used as the reference, and the missing camera warnings are no longer logged in this case.
- Fixed `TryAddTarget`, `TryGetTargetIndicator`, and `GetScreenPose` using the compass tape reference from the last update instead of the current reference when `BoundaryType` is `CompassTape`.
- Fixed the warning logged by `VisualIndicatorManager.RemoveTargetIndicator` that incorrectly referenced `Manual` mode when it is logged in `Auto` mode.
- Fixed the installation documentation to state that importing the package from the Asset Store places its contents in `Assets/Target Indicators` instead of `Packages`.
- Fixed the documentation tip for adding targets with the visual indicator manager to reference `VisualIndicatorManager.TryAddVisualIndicator` instead of the deprecated `AddTargetIndicator`.
- Fixed the [TargetIndicatorManager.IsOutsideBoundary](xref:TargetIndicators.TargetIndicatorManager.IsOutsideBoundary*) documentation and code example to pass a screen point instead of a world space position.
- Fixed the `GetScreenPose` code example comment that stated the depth from the camera is ignored when checking the boundary.
- Fixed the documentation for `VisualIndicatorManager.HideVisualIndicator` and `VisualIndicatorManager.ShowVisualIndicator` to describe that they deactivate and reactivate the visual indicator's GameObject.

## [1.4.0] - 06/21/2026

### Added
- Added [TargetIndicatorManager.CompassReferenceOverride](xref:TargetIndicators.TargetIndicatorManager.CompassForwardReferenceOverride) with a matching serialized field to allow overriding the forward reference vector for compass tapes as an alternative to the camera which is used by default.
- Added [TargetIndicator.Distance](xref:TargetIndicators.TargetIndicator.Distance) property to provide the world-space distance from the reference point (Camera or Compass Override) to the tracked target.
- Added [TargetIndicator.LookAtDot](xref:TargetIndicators.TargetIndicator.LookAtDot) property to provide the dot product between the reference forward vector and the target direction. This is useful for building "Look At" or focus-based UI mechanics such as showing the distance to the target when looking close to the target.
- Added [TargetIndicatorManager.CalculateLookAtDot](xref:TargetIndicators.TargetIndicatorManager.CalculateLookAtDot) configuration setting to explicitly enable or disable the dot product calculation for each tracked target.
- Added `DistanceLabelVisibility` enum (Never, Always, LookAt) to control when the distance text is rendered on a visual indicator.
- Added `VisualIndicator.LookAtDotThreshold` with a matching serialized field to allow configuring a viewing cone for distance label visibility.
- Added `VisualIndicator.DistanceLabelFadeTime` with a matching serialized field to support smooth, time-based opacity fading when targets enter or exit the player's focus cone.

### Changed
- Changed boundary visualizer to hide the line renderer component in the inspector to reduce clutter. To view hidden components, click the three-dot menu at the top right of the Inspector tab and selecting **Debug**.

### Fixed
- Fixed `TargetIndicatorBoundaryVisualizer` to correctly maintain and regenerate its rendering material across multiple undo and redo operations.
- Fixed a "dangling component" Unity Editor warning with `TargetIndicatorBoundaryVisualizer` by registering the dynamically created `LineRenderer` directly with the Editor's Undo stack.

## [1.3.0] - 03/29/2026

### Added
- Added new API `VisualIndicatorManager.TryAddVisualIndicator` and `CompassTapeVisualIndicatorManager.TryAddVisualIndicator`. This new Try-Get pattern returns a boolean for explicit success/failure states and outputs the generated `TargetIndicatorId` to allow easier indicator management when using the provided samples visual indicator managers.

### Deprecated
- Deprecated `VisualIndicatorManager.AddTargetIndicator(Transform)` and `CompassTapeVisualIndicatorManager.AddTargetIndicator(Transform, VisualIndicator)` in favor of the safer `TryAddVisualIndicator` API. These are marked `[Obsolete]` meaning they will still function but will log a warning for you to migrate to the new API.

### Fixed
- Fixed a bug so that the `VisualIndicatorManager.RemoveTargetIndicator` correctly removes both the UI `VisualIndicator` and the tracked target from the `TargetIndicatorManager`.
- Minor performance improvements with `VisualIndicatorManager` and `CompassTapeVisualIndicatorManager`.
- Fixed an issue where `CompassTapeVisualIndicatorManager` would silently fail to use invalid prefabs for visual indicators. The prefab for a visual indicator now require `CompassTapeVisualIndicator` components on them.
- Fixed an issue where the `VisualIndicatorManager` and `CompassTapeVisualIndicatorManager` would spam the console every frame instead of logging a single warning when trying to use the `TryAddVisualIndicator` and `TryRemoveVisualIndicator` APIs with the `AddIndicatorMode` set to `Auto`.

## [1.2.1] - 02/28/2026

### Fixed
- Fixed an issue where `NullReferenceExceptions` would be thrown from the `BoundaryVisualizer` if the `TargetIndicatorManager.Camera` was `null`.

## [1.2.0]

### Added
- Added a console warning to the `Camera` property setter to alert developers if the tracked camera is explicitly set to `null` at runtime.

### Changed
- `TargetIndicatorManager` no longer disables itself in `Awake` if a camera cannot be found. It now logs a warning and remains enabled, waiting for a valid camera to be set.
- Optimizations for calculation screen poses for targets when using `BoundaryType.Ellipse` and `BoundaryType.CompassTape`.

### Fixed
- Fixed `NullReferenceException`s that occurred when tracking active targets, or when calling `TryAddTarget`, `TryGetTargetIndicator`, `GetScreenPose`, and `IsOutsideBoundary` with a null `Camera`. The `TargetIndicatorManager` now safely returns default indicator data (`Pose.identity` and `false`) instead of throwing exceptions.
- Fixed a potential `DivideByZeroException` in `RectangleScreenPose.ProjectOnRectangle` that could occur if a tracked target was perfectly aligned vertically with the center of the screen.
- Fixed a potential `DivideByZeroException` in `EllipseScreenPose` that could occur if extreme padding values or a zero-size resolution completely collapsed the bounding box.

## [1.1.4]

### Changed
- Updated and corrected API docs for `TargetIndicatorManager`, `VisualIndicatormanager`, `CompassTapeVisualIndicatorManager`, `VisualIndicator`, and `AddIndicatorMode`.

## [1.1.3]

### Changed
- Updated released package from Unity `6000.3` to `6000.0` to allow older projects to download the package. If you already have the package this should not impact your ability to use Target Indicators.
- Updated the package name from `com.companyname.targetindicators` to `com.jakemanfre.targetindicators` to avoid name collisions. If you were using the Target Indicators package as a local package and getting errors after this update, you will need to remove it from your project's manifest and reimport the package.
- Changed the sample's assembly definition file to be Auto Referenced so it can be used by default in Unity projects without requiring an assembly reference.

## [1.1.2]

### Removed
- Removed `TMPEssentialsChecker` that would check for Text Mesh Pro Essentials in the users project. This would run every domain reload potentially spamming the user with popups if Text Mesh Pro Essentials was not present in the project and the user didn't want it in their project.

## [1.1.1]

### Changed
- Changed the samples use of the `Inter` font to now use `LiberationSans` that is provided in the Unity editor.

### Removed
- Removed the package manifest file from the asset as it was redundant. All package dependencies are listed in the `package.json` file.
- Removed the `Documentation~` folder that included the source docs pages used to generate the docFX. This reduces the files needed to download when adding the package to your project. You can find the docs web page at https://jakemanfre.github.io/target-indicators.github.io/manual/index.html

## [1.1.0]

### Added
- Added [TargetIndicatorManager.GetChanges](xref:TargetIndicators.TargetIndicatorManager.GetChanges) API to allow for manual synchronization of world-to-screen transformations and explicit dispatching of indicator events outside of the standard Unity Update loop.

### Fixed
- Fixed an issue where adding targets could cause heap allocations due to dictionary resizing. Internal collections are now strictly pre-allocated during initialization to maintain zero allocations.
- Fixed an issue where calling `TargetIndicatorManager.RemoveAllTargets` failed to invoke the `TargetIndicatorsRemoved` event.
- Fixed an issue where adding and removing a target within the exact same frame would erroneously trigger both the added and removed events.
- Fixed a `MissingReferenceException` that would be thrown if a tracked target's GameObject was destroyed by an external script before being explicitly unregistered from the manager.
- Fixed an issue where adding the same target multiple times would create redundant tracking entries.
- Fixed an issue where `TargetIndicatorManager.IsOutsideBoundary` would incorrectly return `true` when the boundary type was set to `CompassTape` and the passed in screen point's `Z` value was negative. Refer to [Compass tape boundary check](https://jakemanfre.github.io/target-indicators.github.io/manual/samples/visual-indicator-component.html#compass-tape-boundary-check) in the documentation for more information.

## [1.0.1]

### Changed
- Updated sample scene names to remove redundant "Target Indicators" name.
- Updated parameters using `in` keyword for several APIs for improved performance with Vector3 and Quaternion.
