# Strafe Advance

A hyper-casual third-person corridor shooter for Android, built with Unity 6 (URP).

## Gameplay

- **Drag left/right** to strafe the player
- **Auto-fire** targets the nearest enemy
- Survive **6 waves** of enemies (Grunts, Flankers, Elites)
- Defeat the **Boss** to complete each level

## Screenshots

> *(add gameplay screenshots here)*

## Features

- Corridor runner with infinite tile scrolling
- Three enemy types with distinct behaviors
  - **Grunt** — walks forward, fires at player
  - **Flanker** — curves toward player X position
  - **Elite** — slow charge attack, drops power-ups on death
- Phase-based Boss fight after wave 6
- Auto-targeting with slight bullet homing
- HP bar + wave counter HUD
- Colored URP materials (player blue, enemies red/orange/purple)
- IAP framework (level packs + skin bundles)
- 35+ unit tests (Unity Test Runner)

## Build

### Play in Editor
Open with Unity 6.4.7f1 (URP template). Press Play — tap the start screen to begin.

### Android APK (batch mode)
```bash
/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app/Contents/MacOS/Unity \
  -projectPath . \
  -executeMethod StrafAdvance.Editor.BatchBuilder.BuildAndroid \
  -batchmode -quit \
  -buildTarget Android \
  -logFile build.log
```
Output: `StrafeAdvance.apk` in the parent directory.

### Install on device
```bash
adb install StrafeAdvance.apk
```

## Tech Stack

| | |
|---|---|
| Engine | Unity 6.4.7f1 |
| Render Pipeline | Universal Render Pipeline (URP) |
| Platform | Android (ARM64 + ARMv7, API 26+) |
| Scripting | C# / Mono |
| IAP | Unity Purchasing 4.x |
| Camera FX | Cinemachine 3.x |
| UI | TextMeshPro |
| Tests | Unity Test Framework 1.6 |

## Project Structure

```
Assets/_Game/
  Scripts/
    Core/        — GameManager, ObjectPool, interfaces
    Player/      — PlayerController, AutoShooter, PlayerHealth
    Enemies/     — EnemyBase, Grunt, Flanker, Elite, Boss
    Level/       — WaveSpawner, CorridorScroller, LevelConfig
    Combat/      — Bullet, DamageSystem, PowerUp
    IAP/         — IAPManager, UnlockRegistry
    UI/          — HUDController, all screen controllers
    Audio/       — AudioManager
    Editor/      — GameSetup (scene/prefab automation), BatchBuilder
  Prefabs/       — Player, enemies, bullets, corridor tile
  ScriptableObjects/ — LevelConfigs, WaveConfigs, EnemyConfigs
  Scenes/        — Bootstrap, GameScene
```

## License

MIT
