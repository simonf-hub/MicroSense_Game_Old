# Micro-Sense Prototype Skeleton

Open `Assets/Scenes/MicroSense_Prototype.unity` and press Play.

## Controls

| Action | Player 1 | Player 2 |
| --- | --- | --- |
| Move | A / D | Left / Right Arrow |
| Jump or climb | W / S | Up / Down Arrow |
| Primary ability | Q | K |
| Secondary ability | E | L |
| Reset prototype | R | R |

## Prototype Loop

1. Each player collects one ability pickup. A player cannot take another while only one slot is unlocked.
2. Sight reveals a hidden platform in the facing direction, making it visible and solid.
3. Sound breaks marked calcified objects in a configurable radius.
4. Taste uses the ability key to enter shooting mode. Movement is locked; left/right adjusts the angle and jump eats a nearby teammate or launches the held teammate.
5. Touch toggles climbing when the player is beside a marked wall.
6. Once both players own an ability, later slot-upgrade collectibles can unlock a second ability slot.
7. Both players enter the membrane gate to complete the level.
8. Pressure buttons open their assigned door while at least one player remains on a linked plate. The demo uses plates on both sides so players can trade places across the gate.
9. Optional gems are shared level collectibles. The HUD reports the collected and total gem count, and reset restores every gem.

The Sound target-count rule is exposed through `SoundAbility._affectAllTargets`. It defaults to affecting all eligible objects, but can be changed per player or prefab while the design decision remains open.

## Scene Components

- `GameManager`: level reset, ability reset, environment reset, and completion state.
- `PlayerController`: shared movement, jump, facing, ground check, and movement states.
- `PlayerAbilityController`: ownership and ability activation routing.
- `SightAbility`, `SoundAbility`, `TasteAbility`, `TouchAbility`: isolated ability behavior.
- `HiddenPlatform`, `BreakablePlatform`, `ClimbableSurface`: explicit valid ability targets. Hidden platforms are pass-through triggers until Sight reveals them.
- `AbilitySlotUpgrade`: unlocks slot two for the collecting player only after every player owns a first ability.
- `PressureButton` and `LinkedDoor`: reusable linked puzzle prefabs. Multiple buttons can hold the same door open.
- `GemCollectible`: reusable shared collectible prefab included automatically in the level HUD count.
- `SharedCameraController`: one camera framing both local players.
- `LevelExit`: requires both players in the exit trigger.

## Regenerating Prototype Assets

Use `Micro-Sense > Build Prototype Scene` in the Unity menu. This regenerates the placeholder sprite assets, reusable prefabs, and prototype scene. It is intended for early iteration; replace generated art assets once the final visual style is established.

Double jump is disabled by default. Enable `Allow Double Jump` on a player's `PlayerController`, or change `PlayerOneAllowDoubleJump` / `PlayerTwoAllowDoubleJump` in `MicroSenseSceneBuilder` before regenerating the scene.

Set `LevelMaxAbilitiesPerPlayer` in `MicroSenseSceneBuilder` to `1` or `2` before regenerating a level. Players always begin with one unlocked slot; when the level maximum is two, the builder adds reusable `AbilitySlotUpgrade` prefab instances later in the route.
