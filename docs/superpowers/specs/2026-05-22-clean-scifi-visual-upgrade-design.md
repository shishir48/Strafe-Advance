# Clean Sci-Fi Visual Upgrade — Design Spec

**Date:** 2026-05-22  
**Project:** Strafe Advance (Unity 6, URP, Android)  
**Style:** Clean Sci-Fi — navy/electric blue, Mass Effect/Destiny aesthetic  
**Approach:** Single editor script (`StrafAdvance > 10. Apply Sci-Fi Upgrade`) with 4 sub-functions  

---

## Palette

| Role | Hex | Usage |
|------|-----|-------|
| Background base | `#080e18` | Scene ambient, material base color |
| Electric blue | `#4fc3f7` | Player trim, HUD accent, bullets |
| Deep blue | `#0277bd` | HP bar gradient start, secondary accent |
| Cyan-green | `#00ffcc` | Flanker / Elite emissive |
| Enemy red | `#ff4444` | Grunt emissive (contrast to player) |
| Boss gold | `#ffd700` | Boss emissive, boss death VFX |
| Near-black navy | `#0d1a2e` | Corridor panels, material base |

---

## Pass 1 — Materials

All materials created in `Assets/_Game/Materials/SciFi/` and assigned to prefabs via script.

### Player (Astronaut)
- Base: `#0d1a2e` (navy metallic), metallic 0.8, smoothness 0.4
- Emissive: `#4fc3f7` at intensity 1.5

### Grunt (Alien)
- Base: `#1a0505` (dark gunmetal), metallic 0.6, smoothness 0.3
- Emissive: `#ff4444` at intensity 1.2

### Flanker / Elite (Speeder)
- Base: `#0a1a18` (charcoal), metallic 0.7, smoothness 0.5
- Emissive: `#00ffcc` at intensity 1.3

### Boss
- Base: `#0a0800` (near-black), metallic 0.9, smoothness 0.6
- Emissive: `#ffd700` at intensity 2.0

### Corridor Tile
- Base: `#0d1a2e` (dark panel), metallic 0.5, smoothness 0.3
- Emissive: `#4fc3f7` at intensity 0.4 (subtle trim)

### Bullet
- Emissive only: `#4fc3f7` at intensity 4.0 (bloom-ready)

---

## Pass 2 — Post-Processing (URP Volume Profile)

Edit `Assets/Settings/DefaultVolumeProfile.asset` via script or via `manage_graphics`.

| Effect | Setting |
|--------|---------|
| Bloom | Intensity 0.6, Threshold 0.8, Scatter 0.7 |
| Vignette | Intensity 0.35, Rounded |
| Color Adjustments | Post Exposure +0.1, Saturation +10 |
| Tonemapping | Mode: ACES |
| White Balance | Temperature -10 (cool/blue shift) |

---

## Pass 3 — HUD / UI

Targets: HP bar image, Wave label, Score label, all overlay panels (Tap to Start, Game Over, You Win).

### HP Bar
- Background: `#0d1a2e` with `#4fc3f733` border
- Fill: gradient `#0277bd` → `#4fc3f7`, emissive glow via `Outline` or Image color
- Label: "HULL INTEGRITY" in small caps above bar

### Labels (Wave, Score)
- Color: `#4fc3f7`
- Font: existing font at letter-spacing +2px equivalent (via `characterSpacing` in TMP if available)
- Score label added next to wave counter

### Overlay Panels (Tap to Start / Game Over / You Win)
- Panel background: `#080e18` at 88% alpha
- Border: 1px solid `#4fc3f744`
- Title text: `#4fc3f7` with emissive glow (outline + color)

---

## Pass 4 — VFX / Particles

**BulletTrail:** Trail Renderer added directly to existing `Bullet` prefab — no new prefab. Color `#4fc3f7`, width 0.05→0, 0.1s time, additive material.

New particle prefabs in `Assets/_Game/Prefabs/VFX/`, instantiated at death/hit positions:

| Prefab | Instantiated by | Description |
|--------|----------------|-------------|
| `HitSpark` | `Bullet.cs` on collision | 8–12 particles, cyan/white, 0.15s lifetime, burst |
| `EnemyDeath` | `EnemyBase.cs` on death | 15 particles, blue-white, 0.4s, sphere burst + flash |
| `BossDeath` | `BossController.cs` on death | 40 particles, gold/white, 1.2s, large burst + screen flash |

All particle materials: URP Unlit, additive blend, emissive color per palette above.

---

## Implementation

Single editor script addition to `Assets/_Game/Scripts/Editor/GameSetup.cs`:

```
StrafAdvance > 10. Apply Sci-Fi Upgrade
  ├── ApplyMaterials()       — creates materials, assigns to prefabs
  ├── ApplyPostProcessing()  — edits DefaultVolumeProfile
  ├── ApplyHUD()             — re-styles HUD elements
  └── ApplyVFX()             — creates particle prefabs, wires to Bullet/EnemyBase
```

Each sub-function is idempotent — safe to re-run.

---

## Success Criteria

- [ ] All prefabs render with correct materials (no magenta/pink fallbacks)
- [ ] Bloom visible on emissive objects in Game View
- [ ] HUD shows blue gradient HP bar + score label
- [ ] Bullet has cyan glow trail
- [ ] Enemy death spawns particle burst
- [ ] No compilation errors after script changes
- [ ] APK builds without errors after upgrade
