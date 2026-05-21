# Strafe Advance — Game Design Spec

**Date:** 2026-05-21
**Platform:** Android
**Engine:** Unity (URP)
**Genre:** Hyper Casual — Third-Person Auto-Fire Strafe Shooter

---

## Overview

Strafe Advance is a third-person corridor runner shooter. The player strafes left/right to dodge and auto-fires at advancing enemies. Each level consists of enemy waves followed by a boss fight. IAP covers level packs and cosmetic skins.

---

## 1. Architecture

### Project Structure

```
Assets/
  _Game/
    Scripts/
      Player/        # movement, health, auto-fire
      Enemies/       # base enemy, variants, boss
      Level/         # wave spawner, level config SO
      Combat/        # bullet pool, damage system
      IAP/           # purchase manager, unlock registry
      UI/            # HUD, level complete, shop
    Prefabs/
    ScriptableObjects/  # level defs, enemy configs, shop items
    Scenes/
      Bootstrap      # init only, loads GameScene
      GameScene      # single persistent scene, all UI panels swap here
```

### Core Systems

- **GameManager** — singleton, owns state machine: `Menu → Playing → BossFight → LevelComplete → GameOver`
- **WaveSpawner** — reads `LevelConfig` SO, spawns enemy waves on timer/kill-count trigger
- **ObjectPool** — bullets, enemy projectiles, VFX (no GC spikes on mobile)
- **IAPManager** — wraps Unity IAP SDK, fires events on purchase, writes to `UnlockRegistry`

### Data Flow

```
LevelConfig SO → WaveSpawner → EnemyAI → DamageSystem → GameManager state
```

---

## 2. Player & Core Gameplay

### Input

- Single finger drag left/right → moves player on X-axis only
- Player Z position fixed (world scrolls toward player)
- Strafe bounds clamped to ±3 units (corridor width)

### Auto-Fire

- `AutoShooter` fires every 0.3s (configurable per level/upgrade)
- Target: nearest enemy in 180° front arc, raycast confirmed
- Bullet from `ObjectPool`, straight forward + 5°/frame homing correction
- Feels responsive without removing skill requirement

### Player Stats (PlayerConfig SO)

- HP, fire rate, bullet damage — all configurable
- Power-ups (temp buffs): rapid fire, shield, multishot — IAP consumable or elite drop

### Corridor Movement

- Corridor tiles spawn ahead, despawn behind — infinite scroll illusion
- Player never moves forward; world scrolls at fixed speed
- Scroll speed increases slightly each wave within a level

### Boss Fight

- Wave clear triggers arena expansion (corridor widens, scroll stops)
- Boss spawns with phase-based HP bar
- Player strafes; boss uses telegraphed attack patterns

---

## 3. Enemies & Levels

### Enemy Types

| Type | Behavior |
|------|----------|
| `Grunt` | Walks straight, fires single shot on timer |
| `Flanker` | Spawns at edge, curves toward player's current X |
| `Elite` | Tanky, telegraphed charge attack, drops power-up on death |

### Boss Structure

- Unique prefab per level pack
- Phase 1: ranged attacks
- Phase 2 (50% HP): adds melee charge, visual flash, speed increase

### LevelConfig ScriptableObject

```csharp
Level {
  waves: [ { enemyType, count, spawnInterval }, ... ]
  bossRef: BossPrefab
  corridorTheme: Material/tileset ref
  worldScrollSpeed: float
  unlockCost: string  // IAP product ID or "free"
}
```

### Progression

- Levels 1–3: free
- Level packs 4–6, 7–9, 10–12: IAP bundles
- Each level ~2–3 min play time

### Difficulty Curve (per level)

| Wave | Enemies |
|------|---------|
| 1–3 | Grunts only |
| 4–5 | Grunts + Flankers |
| 6 | Elites + mixed → triggers boss |

---

## 4. IAP & Shop

### Products (Unity IAP)

| Product ID | Type | Content | Price |
|------------|------|---------|-------|
| `level_pack_2` | Non-consumable | Levels 4–6 | $1.99 |
| `level_pack_3` | Non-consumable | Levels 7–9 | $1.99 |
| `level_pack_4` | Non-consumable | Levels 10–12 | $1.99 |
| `skin_bundle_1` | Non-consumable | 3 character skins | $2.99 |
| `skin_bundle_2` | Non-consumable | 3 weapon skins | $2.99 |
| `powerup_pack` | Consumable | 10× power-up charges | $0.99 |

### Shop Flow

- Accessible from main menu and level complete screen
- Skins preview on rotating 3D model before purchase
- Purchased skins persist via `UnlockRegistry` (PlayerPrefs)
- Restore purchases button included

### IAPManager

- Wraps `UnityEngine.Purchasing`
- On success → fires `OnItemUnlocked(productId)` event
- `UnlockRegistry` listens, writes PlayerPrefs, broadcasts to UI

---

## 5. UI & Visual Polish

### Screens

| Screen | Contents |
|--------|----------|
| `MainMenu` | Play, Shop, Settings, character preview |
| `LevelSelect` | Scrollable grid, locked = padlock + price |
| `HUD` | HP bar, wave counter, boss HP bar (boss phase only) |
| `LevelComplete` | Score (enemies killed × multiplier), 1–3 stars (1=boss killed, 2=no deaths, 3=under par time), next level or shop CTA |
| `GameOver` | Retry, main menu, shop shortcut |
| `Shop` | Tabs: Skins / Levels / Power-ups |

### URP Visual Targets

- Bloom + color grading post-process stack
- Particle burst on enemy death (pooled VFX)
- Screen shake on boss hit (Cinemachine impulse)
- Trail renderer on player bullets
- Art style: stylized low-poly, emissive neon trim lights (sci-fi corridor theme)

### Audio

- Background music per level theme (looping)
- SFX: shoot, enemy hit, player hit, death, level complete, boss roar
- Unity Audio Mixer: separate Music/SFX sliders in settings

### Performance Targets (Android)

- 60fps on mid-range devices (Snapdragon 680+)
- Object pooling for all runtime-spawned objects
- Texture atlasing for UI sprites
- Max 100 draw calls/frame during gameplay

---

## 6. Out of Scope

- Multiplayer
- Cloud save / server-side purchase validation (v1)
- iOS port (v1 Android only)
- Leaderboards (post-launch addition)
