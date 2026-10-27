# River Water, Safety, and Ambience

## What was found

- The forest scene water renderers still use the Asset Store `UNS_Water` material. That material does not animate.
- The project already contains a URP water shader with time-driven waves, but it was not assigned to the river scene water.
- The river ambience clip `dragon-studio-soothing-river-flow-372456.mp3` is present and is about 22 seconds long.

## Changes

- Added `RiverAmbience`, which loops the river ambience clip and smoothly raises its volume as the player approaches the river, fading it out with distance.
- Added `RiverEnvironment` to hold editable river center, flow direction, barrier dimensions, and audio distances. Its selected gizmo shows the barriers and sound radius.
- Added **Tools > Prince > Setup River Environment**. It finds the nearest water surface to the existing backpack placement, creates a project-owned animated water material, assigns it only to that nearby water surface, adds two renderer-free solid box colliders along the banks, and creates the river ambience source.
- The tool is idempotent. It backs up `UNS_Demo.unity` to `Assets/_Project/Docs/RiverSetupBackup~/UNS_Demo.unity.bak` before its first scene change. It does not modify Asset Store packages.

## Parameters

The generated `River_Environment_Setup` object exposes flow direction, wall length, bank half-width, wall height, fade-in/fade-out distances, and maximum river volume in the Inspector. The generated `UNS_River_Water.mat` uses a wave speed of `1.15`, wave height of `0.035`, wave scale of `1.2`, and normal strength of `0.65`.

## Applying and testing

Open the forest scene, allow compilation to finish, then run **Tools > Prince > Setup River Environment**. Inspect and, if needed, adjust the two bank colliders in the Scene view using the `RiverEnvironment` selected gizmo before Play Mode. Runtime appearance, whether barriers align exactly with the river banks, and perceived music balance cannot be verified without a Unity scene/gameplay test.
