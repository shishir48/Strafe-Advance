# Strafe Advance — Session Progress

## Project
- **Path:** `/Users/shishirsingh/Strafe-Advance`
- **GitHub:** https://github.com/shishir48/Strafe-Advance
- **Engine:** Unity 6 (6000.4.7f1), URP, Android target
- **APK:** `/Users/shishirsingh/StrafeAdvance.apk` (last built 2026-05-22, Mono2x)

---

## What Works ✅

- **6 waves** spawn and advance correctly (Grunt×3 → Flanker×2 → Elite)
- **Enemies die** when shot (non-kinematic bullet Rigidbody triggers)
- **Wave never stalls** — `EscapeOffScreen()` calls `ReportKill()` when enemy passes player
- **Race condition fixed** — if all enemies die during spawn phase, wave still advances
- **Tap to start** screen (device: tap, editor: 3s auto-start)
- **GAME OVER** overlay on player death, **YOU WIN** overlay on boss kill
- **HUD**: green HP bar + Wave N/6 label (self-wiring via `HUDController.AutoWire()`)
- **Kenney Space Kit models** (CC0): astronaut player, alien grunts, speeder flankers/elites
- **Neon emissive materials** on all objects
- **Score system**: 100pts per kill (`GameManager.Score`, `GameManager.KillCount`)

---

## Known Issues / TODO

- `AutoShooter.firePoint` is null after mesh swap (model upgrade removed FirePoint child) → fix: AutoShooter now falls back to `transform` if firePoint null (committed, needs compile)
- **Corridor tile** may look wrong after Kenney model swap (material was magenta) → re-run `StrafAdvance > 9. Apply Kenney 3D Models`
- **URP Bloom** not enabled — run `StrafAdvance > 8. Upgrade Graphics` then tweak Volume Profile in Settings folder
- **Boss fight**: triggers after wave 6 but boss prefab needs BossConfig assigned; `Resources/Boss.prefab` is loaded at runtime
- **Debug logs** in WaveSpawner still active (`[Wave] Starting` and `[Wave] Kill reported`) — remove before release build
- **Coplay MCP** installed but only works in a NEW session (`claude` terminal restart)



---

## Key Files

| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/Core/GameManager.cs` | Singleton + game loop + auto-start coroutine |
| `Assets/_Game/Scripts/Level/WaveSpawner.cs` | Wave logic + kill tracking |
| `Assets/_Game/Scripts/Combat/Bullet.cs` | Non-kinematic Rigidbody + layer detection |
| `Assets/_Game/Scripts/Enemies/EnemyBase.cs` | EscapeOffScreen() fixes infinite wave |
| `Assets/_Game/Scripts/Editor/GameSetup.cs` | All setup menu items (1-9) |
| `Assets/_Game/Scripts/Editor/BatchBuilder.cs` | Mono2x APK builder |
| `Assets/Resources/Level1.asset` | Level config loaded at runtime |
| `Assets/Resources/Boss.prefab` | Boss prefab loaded at runtime |

---

## StrafAdvance Menu Items

| Item | Action |
|------|--------|
| Play Game | Toggle play mode |
| 1. Add Enemy Layer | Add Enemy tag+layer to TagManager |
| 4. Setup GameScene | Re-create GameScene hierarchy |
| 6. Upgrade Graphics | Apply neon emissive materials |
| 7. Wire HUD | Wire HUD sliders/labels |
| 8. Upgrade Graphics (Sci-Fi Neon) | Neon materials + compound shapes |
| 9. Apply Kenney 3D Models | Swap primitives for FBX characters |
| Build Android APK | Build Mono2x APK via BuildPipeline |
| Rewire Player Prefab | Re-assign config/firePoint refs |

---

## Next Session: Use Coplay MCP

Coplay MCP is installed. Open a **new terminal** and run `claude` — all MCP tools will be available to directly control Unity without AppleScript.

```bash
claude mcp list  # should show coplay-mcp: ✓ Connected
```
