# Sprint 2 — Game Feel Design

**Date:** 2026-05-26  
**Scope:** 9 polish items — camera, combat feedback, UI juice  
**Target:** every input has a visible/audible response within 1 frame

---

## 1. Camera System (2.1–2.4)

### Architecture

`CameraFeel.cs` is the **sole writer** of the Main Camera's transform and FOV.  
`ScreenShake.cs` is demoted to a **data-only** component — keeps trauma/Perlin math, exposes offsets as properties instead of applying them.

### ScreenShake.cs changes (surgical)

- Remove `_restLocalPos`, `_restLocalRot` fields
- Remove all `transform.localPosition = ...` and `transform.localRotation = ...` writes
- Add:
  ```csharp
  public Vector3 PositionShake { get; private set; }
  public Quaternion RotationShake { get; private set; }
  ```
- Perlin computation, trauma decay, EventBus subscriptions — unchanged

### PlayerController.cs addition

Add one property:
```csharp
private float _prevX;
public float VelocityX { get; private set; }
// in Start(), after existing init:
_prevX = transform.position.x;
// in Update(), before position write:
VelocityX = (transform.position.x - _prevX) / Time.deltaTime;
_prevX = transform.position.x;
```
> **Impl note:** `_prevX` must be seeded in `Start()` to avoid a garbage spike on frame 0.

### CameraFeel.cs (new — on Main Camera)

Runs in `LateUpdate`. Applies effects in this order:

| # | Effect | Mechanic |
|---|--------|----------|
| 1 | **Lead lag (2.1)** | `SmoothDamp` cam `localPos.x` toward `player.x * leadScale` (0.12f), `smoothTime = 0.1f` |
| 2 | **Roll (2.3)** | `SmoothDamp` z-angle toward `-player.VelocityX * rollGain`, clamped ±3°, `smoothTime = 0.08f` |
| 3 | **Impulse (2.4)** | Spring simulation: `_impulseOffset` decays via `spring = 12f, damping = 0.7f` (overshoot then settle) |
| 4 | **Shake** | Add `ScreenShake.Instance.PositionShake` and `.RotationShake` |
| 5 | **FOV (2.2)** | Coroutine on `DodgePerformed`: 55→60 over 0.1s, 60→55 over 0.2s |

**Impulse directions (2.4):**
- `PlayerDamaged` → `(0, +0.15f, 0)` local (camera kicks up)
- `EnemyKilled` where `Type == EnemyType.MiniBoss` → `(0, -0.25f, 0)` (cinematic pull-back)

> **Impl note:** Verify `BossController.cs` publishes `EnemyKilled(MiniBoss, ...)`. If it uses a custom event, adapt the subscription accordingly.

**Acceptance:** camera never stutters; dodge gives visible FOV whoosh; strafe adds roll energy without nausea; player hit kicks camera up with spring overshoot.

---

## 2. Combat Feel (2.5–2.6)

### 2.5 — Hitstop.cs

Two field changes only:

| Field | Old | New |
|-------|-----|-----|
| `gruntDuration` | 0.04f | 0.07f |
| `eliteDuration` | 0.10f | 0.15f |

`playerHitDuration` (0.06f) unchanged.

**Acceptance:** grunt kills feel punchier; elite kills feel heavy.

### 2.6 — PlayerDamageFlash.cs (new — on Player prefab)

- `Awake`: `GetComponentsInChildren<Renderer>()` — no serialized refs
- Subscribe `EventBus<PlayerDamaged>`
- On damage: stop any running coroutine, restart flash:
  - Capture original `_BaseColor` per renderer via `MaterialPropertyBlock`
  - Set to `Color.red` immediately
  - Lerp back to original over `0.08f` seconds
- `MaterialPropertyBlock` used throughout — zero material instances, zero GC

**Acceptance:** hit feedback is instant; no material instance leaks.

---

## 3. UI / Post-Process (2.7–2.9)

### 2.7 — LowHpVignette.cs (new singleton)

- Finds scene `Volume` via `FindFirstObjectByType<Volume>()`, or creates global Volume if absent
- `volume.profile.TryGet<Vignette>(out var v)` — adds override at runtime if missing
- Subscribes to `PlayerHealth.OnHealthChanged`
- Each `Update`:
  - If `hp/maxHp < 0.3f`: `intensity = Lerp(0.25f, 0.55f, PingPong(Time.time * freq, 1f))`  
    where `freq = Lerp(0.8f, 2.0f, 1f - (hp/maxHp / 0.3f))` (faster = more panic)
  - If `hp/maxHp >= 0.3f`: lerp intensity → 0 over 0.5s

**Acceptance:** red vignette pulses clearly when HP < 30%; disappears cleanly above threshold.

### 2.8 — ModernHUD.cs (extended, no new file)

Add to existing `OnCombo(ComboChanged c)`:

```csharp
private int _prevMultiplier = 1;
private Coroutine _popRoutine;

// in OnCombo:
if (c.Multiplier > _prevMultiplier)
{
    if (_popRoutine != null) StopCoroutine(_popRoutine);
    _popRoutine = StartCoroutine(ComboPopRoutine());
}
if (c.Streak == 0) _prevMultiplier = 1;
else _prevMultiplier = c.Multiplier;
```

`ComboPopRoutine`: scale `_comboLabel.transform` from `(1,1,1)` → `(1.5f,1.5f,1f)` over 0.08s → back to `(1,1,1)` over 0.12s. Uses `WaitForSecondsRealtime` (immune to hitstop timeScale).

**Acceptance:** tier transitions 1→2→4→8 each trigger a visible pop; no pop on streak increment within same tier.

### 2.9 — NearDeathEffect.cs (new singleton)

- `_triggered` bool; reset in `GameStateChanged` → `Playing` handler
- Subscribes `EventBus<PlayerDamaged>`
- On each hit: check `_playerHealth.CurrentHp / (float)_playerHealth.MaxHp < 0.2f`
- First time (and `!_triggered`): set `_triggered = true`, run coroutine:
  1. `Time.timeScale = 0.15f`
  2. Create/reuse full-screen UI Image (white) — alpha `0 → 0.5f` over 0.1s real-time
  3. Alpha `0.5f → 0f` over 0.2s real-time  
  4. `Time.timeScale = 1f`
- Finds `PlayerHealth` via `FindFirstObjectByType` in `Start`

**Acceptance:** first sub-20% HP hit triggers 0.3s cinematic slow-mo + white flash exactly once per run.

---

## Files Changed

| File | Action |
|------|--------|
| `Combat/ScreenShake.cs` | Modify — remove transform writes, expose offset properties |
| `Player/PlayerController.cs` | Modify — add `VelocityX` property |
| `Combat/Hitstop.cs` | Modify — two float constants |
| `UI/ModernHUD.cs` | Modify — combo pop in `OnCombo` |
| `Player/CameraFeel.cs` | **New** |
| `Player/PlayerDamageFlash.cs` | **New** |
| `VFX/LowHpVignette.cs` or `UI/LowHpVignette.cs` | **New** |
| `Core/NearDeathEffect.cs` | **New** |

---

## Implementation Order

1. ScreenShake.cs refactor (unblocks CameraFeel)
2. PlayerController.cs VelocityX
3. CameraFeel.cs
4. Hitstop.cs constants
5. PlayerDamageFlash.cs
6. LowHpVignette.cs
7. ModernHUD.cs combo pop
8. NearDeathEffect.cs
