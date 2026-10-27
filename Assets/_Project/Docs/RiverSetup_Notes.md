# River Water, Safety, and Ambience

## What was found

- The forest scene overrides its water prefabs with the project-owned `Assets/_Project/Materials/Nature/UNS_Water.mat`. The initial setup tool incorrectly searched for the Asset Store material, so it did not recognize the scene renderers even though their material name looked similar.
- The project already contains a URP water shader with time-driven waves, but it was not assigned to the river scene water.
- The river ambience clip `dragon-studio-soothing-river-flow-372456.mp3` is present and is about 22 seconds long.

## Changes

- Added `RiverAmbience`, which loops the river ambience clip and smoothly raises its volume as the player approaches the river, fading it out with distance.
- Added `RiverEnvironment` to hold editable river center, flow direction, barrier dimensions, and audio distances. Its selected gizmo shows the barriers and sound radius.
- Added **Tools > Prince > Setup River Environment**. It finds the nearest renderer using the project-owned water material near the existing backpack placement, creates a separate project-owned animated water material, assigns it only to the matching nearby renderer(s), adds four solid invisible box colliders along the outer edges of the island Terrain, and creates the river ambience source. Existing water-relative barrier colliders are disabled when the tool is rerun.
- The tool is idempotent. It backs up `UNS_Demo.unity` to `Assets/_Project/Docs/RiverSetupBackup~/UNS_Demo.unity.bak` before its first scene change. It does not modify Asset Store packages.

## Parameters

The four island-edge colliders are derived from the Terrain bounds. They are 1 m thick and extend from below the terrain base to above its maximum height; their selected gizmos can be inspected in the Scene view. `River_Environment_Setup` also exposes ambience fade distances and maximum volume. The generated `UNS_River_Water.mat` uses a wave speed of `1.15`, wave height of `0.035`, wave scale of `1.2`, and normal strength of `0.65`.

## Applying and testing

Open the forest scene, allow compilation to finish, then run **Tools > Prince > Setup River Environment** again. The tool places the four walls at the terrain's north, south, east, and west edges, rather than around the water renderer. Inspect the `Island_InvisibleWall_*` collider boxes in the Scene view using the `RiverEnvironment` selected gizmo before Play Mode. Runtime behavior and perceived music balance cannot be verified without a Unity scene/gameplay test.
