# Bear Enemy: Prince

## Inspection findings

- Bear source prefab: `Assets/Blink/Art/Animals/Stylized/Bear/Bear_Prefabs/Bear_4.prefab`.
- The imported rig is Generic (`animationType: 2`). The prefab already has an Animator and an Avatar; setup keeps the Avatar and assigns a generated controller.
- Available clips: Attack1, Attack2, Attack3, Attack5, Buff, Death, Eat, GetHitFromFront, Idle, IdleCombat, Jump, RunBackward, RunForward, Sit, Sleep, StunnedLoop, WalkBackward, and WalkForward.
- The generated controller uses Idle, WalkForward, RunForward, Attack1/2/3/5, GetHitFromFront, Death, and Buff. Buff is used for the `Roar` alert trigger because no clip named Roar was found. Eat, IdleCombat, Jump, backward movement, Sit, Sleep, and StunnedLoop are available but not used by this simple AI.
- The source material uses the built-in Standard shader and has albedo, normal, occlusion, and emissive textures. No fur/shell shader or LODGroup was found on the source prefab. Setup creates a project-owned URP/Lit material and copies the relevant source textures; it does not modify the imported bear assets.
- AI Navigation `com.unity.ai.navigation` version 2.0.13 is already present. No bear-specific roar, attack, hit, death, or footstep audio was found under `Assets/_Project/Audio`; the bear's optional 3D clip fields remain empty. `Assets/_Project/Audio/SFX/Sword/Nyerang Beruang.mp3` was added later as a player sword-hit sound and is mapped separately when running **Tools > Prince > Setup Weapon And Combat**.
- `PlayerHealth.TakeDamage` raises its existing defeated/knocked-out events, but no respawn handler was found in the runtime scripts inspected. Bear attacks use that existing damage API; no new player recovery system was added.

## Files added

- `Assets/_Project/Scripts/Runtime/BearAI.cs`: patrol, view-cone detection, alert delay, chase/leash/return, telegraphed claw hits, poise, knockback, death, and a manual terrain-movement fallback.
- `Assets/_Project/Scripts/Runtime/BearHealthBar.cs`: billboarded world-space health bar, shown while engaged and briefly after damage.
- `Assets/_Project/Scripts/Editor/PrinceBearEnemySetup.cs`: idempotent setup and NavMesh bake tools.

The setup tool generates these project assets when run:

- `Assets/_Project/Prefabs/Bear_Enemy.prefab`: variant based on the imported bear prefab.
- `Assets/_Project/Materials/Bear/Bear_Light_URP.mat`: lightweight URP/Lit material using the bear textures.
- `Assets/_Project/Animations/Bear/Bear_Animator.controller` and copied clips under `Assets/_Project/Animations/Bear/Clips/`.
- `Assets/_Project/Data/Forest_NavMesh.asset`: created by the bake tool.

No quest, dialogue, NPC, Starter Assets, Asset Store, package, or ProjectSettings files were changed. The bear has no quest requirement: the backpack remains independently collectible.

## Inspector parameters

BearAI defaults (all editable on the component):

| Setting | Default |
|---|---:|
| Max health | 120 |
| Attack damage | 18 |
| Attack range / hit radius | 2.1 / 0.8 |
| Attack cooldown | 2.4 s |
| Attack telegraph / recovery | 0.8 s / 0.7 s |
| Hit poise / knockback force | 0.45 s / 2.2 |
| Detection distance / view angle | 15 m / 110 degrees |
| Close detection distance / alert delay | 3.5 m / 1.2 s |
| Walk / chase speed | 1.35 / 2.7 m/s |
| Patrol radius / leash distance | 5 / 24 m |
| Health regeneration / knockback recovery | 4 / 7 per second |

Assign the optional `Roar Clip`, `Attack Clip`, `Hurt Clip`, and `Death Clip` fields if suitable audio is added later. The generated AudioSource is 3D with a 3–28 m range. Assign `Ground Terrain` if the setup tool did not find the forest Terrain.

When health reaches zero, BearAI disables damage colliders and movement once, plays the non-looping `Death` clip, and leaves the bear as a permanent, non-interactive corpse for the current session. It does not respawn or disappear.

The death transition in `Bear_Animator` must not transition to itself. Its `canTransitionToSelf` setting is disabled both in the generated controller and in **Tools > Prince > Setup Bear Enemy**. This prevents the persistent `Dead` bool from restarting the death animation every Animator update.

## Manual steps

1. Let Unity finish importing and compiling the new scripts.
2. If the backpack quest placement is not set up in `UNS_Demo`, open the forest scene and run **Tools > Prince > Setup Backpack Quest** first.
3. Run **Tools > Prince > Setup Bear Enemy**. It saves the bear variant/controller/material and places one bear to the side of the backpack area using `BackpackQuestPlacement`. If a `Bear_Enemy` already exists, its manual position is preserved and a duplicate is not created.
4. Run **Tools > Prince > Bake Forest NavMesh**. This creates or updates the forest `NavMeshSurface` and its project-owned NavMesh asset. The bake tool is intentionally not run automatically.
5. In Unity, verify the bear stays clear of Bram's approach, the backpack can still be picked up without defeating the bear, and the NavMesh has walkable paths. Assign optional bear sound clips if desired.
6. Test detection, the warning delay, all four claw animations, player damage, poise, leash/return, health-bar visibility, and death. Adjust the BearAI fields in the prefab or scene instance as needed.

## Not tested in this session

The Unity Editor was not opened, the setup/bake tools were not run, and Play Mode or screenshots were not used. Therefore the generated prefab/controller/material/NavMesh have not been imported by the Editor, and animation retargeting, actual path baking, visual placement, performance, attack timing/feel, and interaction with the live quest scene still need manual verification. Runtime and Editor C# assemblies were compiled statically against the project's Unity references; compilation succeeded, with only existing warnings elsewhere in the project.
