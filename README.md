# AR Survival Shooter

A mobile AR survival shooter built with Unity and AR Foundation (ARCore). The
player places the game on a detected horizontal surface, then fights off two
types of enemies (Melee and Shooter) within a time limit.

## Requirements

- Unity 6 (6000.4.7f1 or later)
- Android device with ARCore support
- Android minimum API level 24+

## Setup

1. Clone the repository.
2. Open the project in Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Connect an Android device with USB debugging enabled, or use XR
   Simulation in the Editor to test without a device.
5. `File > Build Settings > Build And Run` to deploy to an Android device.

## How to Play

1. Launch the app and press **Start** on the main menu.
2. Choose a difficulty (**Easy** or **Hard**).
3. Slowly move the phone over a flat, textured surface (floor or table)
   until the plane tracker grid appears.
4. Tap the grid to place the game world.
5. Survive the round: shoot approaching enemies with the **SHOOT** button.
   Melee enemies close to short range and attack on a cooldown; Shooter
   enemies stop at a distance and fire projectiles.
6. The round ends when the timer runs out or your health reaches zero.
7. View your results on the end screen, or check the **Leaderboard** from
   the main menu for your last 5 sessions.

## Project Structure

```
Assets/
  Scripts/        Gameplay, state machine, UI, leaderboard, sound scripts
  Prefabs/         MeleeEnemy, ShooterEnemy, Bullet, EnemyBullet, GameWorld,
                   CustomPlaneTracker
  Scenes/          SampleScene (the only scene in the build)
  Sprites/         Imported third-party character and weapon models
  Animations/      Generated Animator Controllers
  Settings/        URP renderer assets (Mobile_Renderer, PC_Renderer)
  Audio/           Sound effect clips
```

## Architecture Summary

In short: a `GameManager` singleton drives a state machine (Menu, Placement, Play, End).
`Enemy` is an abstract base class with `MeleeEnemy` and `ShooterEnemy`
subclasses. Bullets are pooled through `BulletPool` (one pool for the
player, one for enemies) with no `Instantiate`/`Destroy` calls during
gameplay. `EnemySpawner` acts as a factory, choosing which enemy type to
build. UI and sound react to C# events raised by `GameManager` and
`PlayerHealth` rather than being called directly. `UIManager` also applies
a runtime visual skin (themed panels, buttons, and a health bar) on top of
the base UI layout.

## Editor Tooling

`Tools > AR Shooter > Model Swap Tool` is a custom Editor window used
during development to speed up fixing FBX import/avatar settings,
generating basic Animator Controllers, and swapping placeholder enemy
meshes for imported character models. It is development-only and has no
effect at runtime.

## Assets

Third-party character and weapon models sourced from Quaternius
(quaternius.com), used under their CC0 license.
UI sprites sourced from Kenney