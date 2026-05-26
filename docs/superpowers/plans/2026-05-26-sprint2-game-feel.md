# Sprint 2 — Game Feel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every player input produce visible/audible feedback within 1 frame via camera polish, combat hitstop tuning, player damage flash, low-HP vignette, combo tier pop, and near-death slow-mo.

**Architecture:** `CameraFeel.cs` owns all Main Camera transform/FOV writes; `ScreenShake.cs` is demoted to a data-only component exposing `PositionShake`/`RotationShake` offsets. Four new singletons (`CameraFeel`, `PlayerDamageFlash`, `LowHpVignette`, `NearDeathEffect`) subscribe to the existing EventBus. Two existing files (`Hitstop`, `ModernHUD`) receive surgical edits.

**Tech Stack:** Unity 6, URP 17, C# 9, NUnit (EditMode tests), EventBus<T> pub/sub, MaterialPropertyBlock, URP Volume/Vignette API.

---

## File Map

| Path | Action | Responsibility |
|------|--------|----------------|
| `Assets/_Game/Scripts/Combat/ScreenShake.cs` | Modify | Compute Perlin shake offsets, expose as properties — no longer writes transform |
| `Assets/_Game/Scripts/Player/PlayerController.cs` | Modify | Expose `VelocityX` (world units/s) for camera roll |
| `Assets/_Game/Scripts/Player/CameraFeel.cs` | **Create** | Sole writer of Main Camera localPosition/localRotation/FOV — composites lead lag, roll, directional impulse, shake |
| `Assets/_Game/Scripts/Combat/Hitstop.cs` | Modify | Bump grunt → 0.07s, elite → 0.15s |
| `Assets/_Game/Scripts/Player/PlayerDamageFlash.cs` | **Create** | Flash player renderers red on `PlayerDamaged` via MaterialPropertyBlock |
| `Assets/_Game/Scripts/UI/LowHpVignette.cs` | **Create** | Pulse URP Vignette intensity when HP < 30% |
| `Assets/_Game/Scripts/UI/ModernHUD.cs` | Modify | Scale-pop combo label on multiplier tier jump |
| `Assets/_Game/Scripts/Core/NearDeathEffect.cs` | **Create** | One-shot slow-mo + white flash when HP first drops below 20% |

---

## Task 1: Refactor ScreenShake — expose offsets, remove transform writes

**Files:**
- Modify: `Assets/_Game/Scripts/Combat/ScreenShake.cs`

- [ ] **Step 1: Open file and read current LateUpdate / Awake**

Read `Assets/_Game/Scripts/Combat/ScreenShake.cs` to confirm exact field names before editing.

- [ ] **Step 2: Replace ScreenShake.cs with the refactored version**

The new version removes `_restLocalPos`, `_restLocalRot`, and all `transform.localPosition/Rotation` writes. Adds `PositionShake` and `RotationShake` public properties. All trauma/Perlin/EventBus logic is unchanged.

```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private float traumaDecay    = 1.8f;
        [SerializeField] private float maxPosOffset   = 0.35f;
        [SerializeField] private float maxRotDegrees  = 1.5f;
        [SerializeField] private float noiseFrequency = 22f;

        private float _trauma;
        private float _noiseSeed;

        public Vector3    PositionShake { get; private set; }
        public Quaternion RotationShake { get; private set; }

        void Awake()
        {
            _noiseSeed = Random.value * 1000f;
        }

        void OnEnable()
        {
            EventBus<ShakeRequest>.Subscribe(OnShake);
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<PlayerDamaged>.Subscribe(OnHit);
        }

        void OnDisable()
        {
            EventBus<ShakeRequest>.Unsubscribe(OnShake);
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<PlayerDamaged>.Unsubscribe(OnHit);
        }

        void OnShake(ShakeRequest r) => Add(r.Amount);
        void OnKill(EnemyKilled k)   => Add(k.Type == EnemyType.Elite ? 0.45f : 0.18f);
        void OnHit(PlayerDamaged d)  => Add(0.55f);

        public void Add(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                PositionShake = Vector3.zero;
                RotationShake = Quaternion.identity;
                return;
            }

            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * noiseFrequency + _noiseSeed;

            float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
            float r = (Mathf.PerlinNoise(t, t)  - 0.5f) * 2f;

            PositionShake = new Vector3(x, y, 0f) * maxPosOffset * shake;
            RotationShake = Quaternion.Euler(0f, 0f, r * maxRotDegrees * shake);

            _trauma = Mathf.Max(0f, _trauma - traumaDecay * Time.unscaledDeltaTime);
        }
    }
}
```

- [ ] **Step 3: Check compile — no errors**

Use `mcp-for-unity read_console` filtering for errors. Expected: no errors, no warnings about ScreenShake. If the old `trauma` field was `[SerializeField]`, the Inspector will show a missing field warning — acceptable.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/Combat/ScreenShake.cs
git commit -m "refactor(camera): ScreenShake exposes offsets, CameraFeel will own transform writes"
```

---

## Task 2: PlayerController — add VelocityX property

**Files:**
- Modify: `Assets/_Game/Scripts/Player/PlayerController.cs`

- [ ] **Step 1: Add `_prevX` field and `VelocityX` property**

In `PlayerController.cs`, add after the existing sprint state block:

```csharp
// Velocity for camera roll
private float _prevX;
public float VelocityX { get; private set; }
```

- [ ] **Step 2: Seed `_prevX` in `Start()`**

At the end of the existing `Start()` method (after `health.Initialize` and `_stamina = maxStamina`):

```csharp
_prevX = transform.position.x;
```

- [ ] **Step 3: Compute `VelocityX` in `Update()` — first line, before anything else**

Insert as the very first line inside `Update()`:

```csharp
VelocityX = (transform.position.x - _prevX) / Mathf.Max(Time.deltaTime, 0.0001f);
_prevX = transform.position.x;
```

The `Mathf.Max` guard prevents divide-by-zero if `deltaTime` is 0 (e.g., on the exact hitstop frame).

- [ ] **Step 4: Compile check**

Read console. Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Player/PlayerController.cs
git commit -m "feat(player): expose VelocityX for camera roll"
```

---

## Task 3: Create CameraFeel.cs — camera lead lag, roll, impulse, FOV pulse

**Files:**
- Create: `Assets/_Game/Scripts/Player/CameraFeel.cs`

- [ ] **Step 1: Create the script**

```csharp
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    [RequireComponent(typeof(Camera))]
    public class CameraFeel : MonoBehaviour
    {
        [Header("Lead Lag (2.1)")]
        [SerializeField] private float leadScale      = 0.12f;
        [SerializeField] private float leadSmoothTime = 0.10f;

        [Header("Roll (2.3)")]
        [SerializeField] private float rollGain        = 0.40f;  // degrees per world-unit/s; strafeSpeed=8 → ~3.2° max
        [SerializeField] private float rollSmoothTime  = 0.08f;
        [SerializeField] private float maxRollDegrees  = 3f;

        [Header("Impulse (2.4) — spring-damper")]
        [SerializeField] private float impulseSpring   = 12f;
        [SerializeField] private float impulseDamping  = 0.7f;   // <1 = underdamped (overshoot)

        [Header("FOV Pulse (2.2)")]
        [SerializeField] private float baseFov   = 55f;
        [SerializeField] private float dodgeFov  = 60f;
        [SerializeField] private float fovInTime = 0.10f;
        [SerializeField] private float fovOutTime = 0.20f;

        private Camera           _cam;
        private PlayerController _player;
        private ScreenShake      _shake;

        private Vector3    _restLocalPos;
        private Quaternion _restLocalRot;

        // Lead lag
        private float _leadX;
        private float _leadVelX;

        // Roll
        private float _rollAngle;
        private float _rollVel;

        // Impulse spring
        private Vector3 _impulseOffset;
        private Vector3 _impulseVelocity;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            EventBus<DodgePerformed>.Subscribe(OnDodge);
            EventBus<PlayerDamaged>.Subscribe(OnPlayerHit);
            EventBus<EnemyKilled>.Subscribe(OnEnemyKilled);
        }

        void OnDestroy()
        {
            EventBus<DodgePerformed>.Unsubscribe(OnDodge);
            EventBus<PlayerDamaged>.Unsubscribe(OnPlayerHit);
            EventBus<EnemyKilled>.Unsubscribe(OnEnemyKilled);
        }

        void Start()
        {
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;
            _leadX        = 0f;
            _cam.fieldOfView = baseFov;

            _player = FindFirstObjectByType<PlayerController>();
            _shake  = FindFirstObjectByType<ScreenShake>();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        void OnDodge(DodgePerformed d)
        {
            StopAllCoroutines();
            StartCoroutine(FovPulseRoutine());
        }

        void OnPlayerHit(PlayerDamaged d)   => AddImpulse(new Vector3(0f,  0.15f, 0f));
        void OnEnemyKilled(EnemyKilled k)
        {
            if (k.Type == EnemyType.MiniBoss) AddImpulse(new Vector3(0f, -0.25f, 0f));
        }

        void AddImpulse(Vector3 direction)
        {
            _impulseOffset   += direction;
            _impulseVelocity  = Vector3.zero;   // reset so spring starts from new offset
        }

        // ── LateUpdate — sole transform writer ───────────────────────────────

        void LateUpdate()
        {
            Vector3    posOffset = Vector3.zero;
            Quaternion rotOffset = Quaternion.identity;

            // 1. Lead lag
            if (_player != null)
            {
                float targetLeadX = _player.transform.position.x * leadScale;
                _leadX  = Mathf.SmoothDamp(_leadX, targetLeadX, ref _leadVelX, leadSmoothTime);
                posOffset.x += _leadX;
            }

            // 2. Roll
            if (_player != null)
            {
                float targetRoll = Mathf.Clamp(-_player.VelocityX * rollGain, -maxRollDegrees, maxRollDegrees);
                _rollAngle = Mathf.SmoothDamp(_rollAngle, targetRoll, ref _rollVel, rollSmoothTime);
                rotOffset  = Quaternion.Euler(0f, 0f, _rollAngle);
            }

            // 3. Impulse spring (Time.deltaTime freezes during hitstop — intentional; camera holds kick)
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                float critDamp   = 2f * impulseDamping * Mathf.Sqrt(impulseSpring);
                Vector3 spring   = -impulseSpring * _impulseOffset - critDamp * _impulseVelocity;
                _impulseVelocity += spring * dt;
                _impulseOffset   += _impulseVelocity * dt;
            }
            posOffset += _impulseOffset;

            // 4. Shake (ScreenShake computes Perlin offsets in its own LateUpdate;
            //    script execution order must be: ScreenShake BEFORE CameraFeel)
            if (_shake != null)
            {
                posOffset += _shake.PositionShake;
                rotOffset *= _shake.RotationShake;
            }

            transform.localPosition = _restLocalPos + posOffset;
            transform.localRotation = _restLocalRot * rotOffset;
        }

        // ── FOV coroutine ─────────────────────────────────────────────────────

        IEnumerator FovPulseRoutine()
        {
            float t = 0f;
            while (t < fovInTime)
            {
                t += Time.unscaledDeltaTime;
                _cam.fieldOfView = Mathf.Lerp(baseFov, dodgeFov, t / fovInTime);
                yield return null;
            }
            _cam.fieldOfView = dodgeFov;

            t = 0f;
            while (t < fovOutTime)
            {
                t += Time.unscaledDeltaTime;
                _cam.fieldOfView = Mathf.Lerp(dodgeFov, baseFov, t / fovOutTime);
                yield return null;
            }
            _cam.fieldOfView = baseFov;
        }
    }
}
```

- [ ] **Step 2: Compile check**

Read console. Expected: no errors.

- [ ] **Step 3: Set script execution order — ScreenShake before CameraFeel**

ScreenShake writes `PositionShake`/`RotationShake` in its LateUpdate. CameraFeel reads them in its LateUpdate. ScreenShake must run first.

Use Unity MCP `execute_menu_item` → `Edit > Project Settings` then via `manage_script_capabilities` or directly edit `ProjectSettings/ProjectSettings.asset`. Alternatively use `execute_code`:

```csharp
// Run in Unity via MCP execute_code:
using UnityEditor;
MonoScript shake  = MonoScript.FromMonoBehaviour(FindAnyObjectByType<ScreenShake>());
MonoScript feel   = MonoScript.FromMonoBehaviour(FindAnyObjectByType<CameraFeel>());
// ScreenShake = -10, CameraFeel = 0 (default runs after)
MonoImporter.SetExecutionOrder(shake, -10);
```

Or: In Unity Editor manually → Project Settings → Script Execution Order → add `ScreenShake` at `-10`.

- [ ] **Step 4: Add CameraFeel to Main Camera in GameScene**

Using MCP `manage_components`:
```json
{
  "action": "add",
  "gameObjectName": "Main Camera",
  "componentType": "StrafAdvance.CameraFeel"
}
```

If Main Camera has a different name, use `find_gameobjects` with tag `MainCamera` first.

- [ ] **Step 5: Play-mode verification**

Enter play mode. Strafe left/right — camera should lean slightly in strafe direction and roll ±3°. Dodge — FOV should pulse wider visibly. Take a hit — camera should kick up then spring back with slight overshoot.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Player/CameraFeel.cs
git commit -m "feat(camera): CameraFeel — lead lag, roll, directional impulse, FOV pulse on dodge"
```

---

## Task 4: Hitstop — bump grunt and elite durations

**Files:**
- Modify: `Assets/_Game/Scripts/Combat/Hitstop.cs`

- [ ] **Step 1: Change the two default values**

In `Hitstop.cs`, change:
```csharp
[SerializeField] private float gruntDuration = 0.04f;
[SerializeField] private float eliteDuration = 0.10f;
```
to:
```csharp
[SerializeField] private float gruntDuration = 0.07f;
[SerializeField] private float eliteDuration = 0.15f;
```

> **Note:** If Hitstop is already placed in the scene and the values were overridden in the Inspector, update the scene prefab/component too (or use "Reset" in Inspector to pick up new defaults).

- [ ] **Step 2: Compile check + verify defaults**

Read console. No errors expected. In play mode, kill a grunt — freeze should be noticeably longer than before (0.07s ≈ 4 frames at 60fps, clearly perceptible).

- [ ] **Step 3: Commit**

```bash
git add Assets/_Game/Scripts/Combat/Hitstop.cs
git commit -m "feat(combat): hitstop grunt 0.04→0.07s, elite 0.10→0.15s"
```

---

## Task 5: PlayerDamageFlash — renderer flash on hit

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerDamageFlash.cs`

- [ ] **Step 1: Create the script**

```csharp
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerDamageFlash : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.08f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[]          _renderers;
        private MaterialPropertyBlock _mpb;
        private Color[]             _originalColors;
        private Coroutine           _flashRoutine;

        void Awake()
        {
            _renderers      = GetComponentsInChildren<Renderer>(includeInactive: true);
            _mpb            = new MaterialPropertyBlock();
            _originalColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].sharedMaterial;
                _originalColors[i] = mat != null && mat.HasProperty(BaseColorId)
                    ? mat.GetColor(BaseColorId)
                    : Color.white;
            }

            EventBus<PlayerDamaged>.Subscribe(OnDamaged);
        }

        void OnDestroy() => EventBus<PlayerDamaged>.Unsubscribe(OnDamaged);

        void OnDamaged(PlayerDamaged d)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            // Instant red
            SetAllColor(Color.red);

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flashDuration);
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _mpb.SetColor(BaseColorId, Color.Lerp(Color.red, _originalColors[i], t));
                    _renderers[i].SetPropertyBlock(_mpb);
                }
                yield return null;
            }

            RestoreAll();
            _flashRoutine = null;
        }

        void SetAllColor(Color c)
        {
            _mpb.SetColor(BaseColorId, c);
            foreach (var r in _renderers) r.SetPropertyBlock(_mpb);
        }

        void RestoreAll()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _mpb.SetColor(BaseColorId, _originalColors[i]);
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }
    }
}
```

- [ ] **Step 2: Compile check**

Read console. No errors expected.

- [ ] **Step 3: Add PlayerDamageFlash to Player prefab**

Using MCP `manage_prefabs` or `manage_components` on the Player prefab:
```json
{
  "action": "add",
  "prefabPath": "_Game/Prefabs/Player.prefab",
  "componentType": "StrafAdvance.PlayerDamageFlash"
}
```

- [ ] **Step 4: Play-mode verification**

Enter play mode. Walk into an enemy or let an enemy fire. Player should flash red instantly, then fade back to original color over 0.08s. No material instances should appear in the Profiler (check Memory tab → Materials count stays constant).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Player/PlayerDamageFlash.cs Assets/_Game/Prefabs/Player.prefab
git commit -m "feat(player): damage flash — red MaterialPropertyBlock on PlayerDamaged"
```

---

## Task 6: LowHpVignette — URP vignette pulse when HP < 30%

**Files:**
- Create: `Assets/_Game/Scripts/UI/LowHpVignette.cs`

- [ ] **Step 1: Create the script**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StrafAdvance
{
    public class LowHpVignette : MonoBehaviour
    {
        public static LowHpVignette Instance { get; private set; }

        [SerializeField] private float hpThreshold   = 0.30f;
        [SerializeField] private float minIntensity  = 0.25f;
        [SerializeField] private float maxIntensity  = 0.55f;
        [SerializeField] private float minFreq       = 0.8f;   // Hz at threshold
        [SerializeField] private float maxFreq       = 2.0f;   // Hz near 0 HP
        [SerializeField] private float fadeOutSpeed  = 2.0f;   // intensity/second when HP > threshold

        private Vignette      _vignette;
        private PlayerHealth  _playerHealth;
        private float         _hpRatio = 1f;
        private float         _currentIntensity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged += OnHealthChanged;

            // Create a dedicated Volume so we never mutate shared scene Volume profiles.
            var volGO = new GameObject("LowHpVignetteVolume");
            volGO.transform.SetParent(transform);
            var vol       = volGO.AddComponent<Volume>();
            vol.isGlobal  = true;
            vol.priority  = 10f;                    // above default scene volume
            vol.profile   = ScriptableObject.CreateInstance<VolumeProfile>();
            _vignette     = vol.profile.Add<Vignette>(overrides: true);
            _vignette.active = true;
            _vignette.intensity.Override(0f);
            _vignette.color.Override(Color.red);
            _vignette.rounded.Override(true);
        }

        void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= OnHealthChanged;
            if (Instance == this) Instance = null;
        }

        void OnHealthChanged(int cur, int max)
            => _hpRatio = max > 0 ? (float)cur / max : 1f;

        void Update()
        {
            if (_vignette == null) return;

            if (_hpRatio < hpThreshold)
            {
                // t: 0 at threshold boundary, 1 at 0 HP
                float t    = 1f - (_hpRatio / hpThreshold);
                float freq = Mathf.Lerp(minFreq, maxFreq, t);
                // sin pulse: smooth 0..1 oscillation
                float pulse = (Mathf.Sin(Time.time * freq * Mathf.PI * 2f) + 1f) * 0.5f;
                _currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
            }
            else
            {
                _currentIntensity = Mathf.MoveTowards(
                    _currentIntensity, 0f, fadeOutSpeed * Time.deltaTime);
            }

            _vignette.intensity.Override(_currentIntensity);
        }
    }
}
```

- [ ] **Step 2: Compile check**

Read console. If `UnityEngine.Rendering.Universal` is not resolving, check that `Unity.RenderPipelines.Universal.Runtime` is in the assembly definition references for the `_Game` scripts asmdef (or the folder has no asmdef and uses global namespace — in that case it should resolve automatically).

- [ ] **Step 3: Add LowHpVignette to scene**

Add a new GameObject `LowHpVignette` to GameScene and attach the component. Or attach to an existing singleton holder. Using MCP:

```json
// manage_gameobject: create
{ "action": "create", "name": "LowHpVignette" }
// manage_components: add
{ "action": "add", "gameObjectName": "LowHpVignette", "componentType": "StrafAdvance.LowHpVignette" }
```

- [ ] **Step 4: Play-mode verification**

Enter play mode. Use `execute_code` to set player HP below 30%:
```csharp
FindFirstObjectByType<StrafAdvance.PlayerHealth>().TakeDamage(70);
```
Expected: red vignette appears around screen edges and pulses. Heal back above threshold: vignette fades out over ~0.5s.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/UI/LowHpVignette.cs Assets/_Game/Scenes/GameScene.unity
git commit -m "feat(ui): low-HP vignette — URP pulse when HP < 30%"
```

---

## Task 7: ModernHUD — combo tier scale-pop

**Files:**
- Modify: `Assets/_Game/Scripts/UI/ModernHUD.cs`

- [ ] **Step 1: Add `_prevMultiplier` field and `_popRoutine` field**

In the class body of `ModernHUD`, after existing private fields, add:

```csharp
private int      _prevMultiplier = 1;
private Coroutine _popRoutine;
```

- [ ] **Step 2: Replace the existing `OnCombo` method**

```csharp
void OnCombo(ComboChanged c)
{
    if (_comboLabel == null) return;

    if (c.Streak == 0)
    {
        _comboLabel.text = "";
        _prevMultiplier  = 1;
        return;
    }

    _comboLabel.text = c.Multiplier > 1
        ? $"<color=#ffd166>×{c.Multiplier}</color>  <size=22>x{c.Streak}</size>"
        : $"<size=22>x{c.Streak}</size>";

    if (c.Multiplier > _prevMultiplier)
    {
        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(ComboPopRoutine());
    }

    _prevMultiplier = c.Multiplier;
}
```

- [ ] **Step 3: Add `ComboPopRoutine` method** (inside the class, after `OnCombo`)

```csharp
IEnumerator ComboPopRoutine()
{
    Transform t     = _comboLabel.transform;
    float     upT   = 0.08f;
    float     downT = 0.12f;

    float elapsed = 0f;
    while (elapsed < upT)
    {
        elapsed += Time.unscaledDeltaTime;
        float s = Mathf.Lerp(1f, 1.5f, elapsed / upT);
        t.localScale = new Vector3(s, s, 1f);
        yield return null;
    }

    elapsed = 0f;
    while (elapsed < downT)
    {
        elapsed += Time.unscaledDeltaTime;
        float s = Mathf.Lerp(1.5f, 1f, elapsed / downT);
        t.localScale = new Vector3(s, s, 1f);
        yield return null;
    }

    t.localScale = Vector3.one;
    _popRoutine  = null;
}
```

- [ ] **Step 4: Compile check**

Read console. No errors expected.

- [ ] **Step 5: Play-mode verification**

Enter play mode. Kill 5 enemies quickly (combo ×2 threshold) — combo label should visibly scale-pop. Kill 5 more (×4 threshold) — another pop. No pop on individual streak increments within the same tier.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/UI/ModernHUD.cs
git commit -m "feat(ui): combo HUD scale-pop on multiplier tier jump (×1→2→4→8)"
```

---

## Task 8: NearDeathEffect — one-shot slow-mo + white flash

**Files:**
- Create: `Assets/_Game/Scripts/Core/NearDeathEffect.cs`

- [ ] **Step 1: Create the script**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class NearDeathEffect : MonoBehaviour
    {
        public static NearDeathEffect Instance { get; private set; }

        [SerializeField] private float slowMoScale  = 0.15f;
        [SerializeField] private float hpThreshold  = 0.20f;
        [SerializeField] private float flashAlpha   = 0.50f;

        private bool          _triggered;
        private PlayerHealth  _playerHealth;
        private Image         _flashImage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<PlayerDamaged>.Subscribe(OnDamaged);
            EventBus<GameStateChanged>.Subscribe(OnStateChanged);
        }

        void OnDestroy()
        {
            EventBus<PlayerDamaged>.Unsubscribe(OnDamaged);
            EventBus<GameStateChanged>.Unsubscribe(OnStateChanged);
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            BuildFlashOverlay();
        }

        void BuildFlashOverlay()
        {
            var canvasGO = new GameObject("NearDeathCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas        = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGO.AddComponent<CanvasScaler>();

            var imgGO = new GameObject("FlashImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            var rt      = imgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _flashImage               = imgGO.AddComponent<Image>();
            _flashImage.color         = new Color(1f, 1f, 1f, 0f);
            _flashImage.raycastTarget = false;
        }

        void OnStateChanged(GameStateChanged e)
        {
            if (e.Current == GameState.Playing) _triggered = false;
        }

        void OnDamaged(PlayerDamaged d)
        {
            if (_triggered || _playerHealth == null) return;

            float ratio = _playerHealth.MaxHp > 0
                ? (float)_playerHealth.CurrentHp / _playerHealth.MaxHp
                : 1f;

            if (ratio < hpThreshold)
            {
                _triggered = true;
                StartCoroutine(NearDeathRoutine());
            }
        }

        IEnumerator NearDeathRoutine()
        {
            float restoreScale  = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale      = slowMoScale;

            // Fade in — 0.1s real time
            float t = 0f;
            while (t < 0.10f)
            {
                t += Time.unscaledDeltaTime;
                _flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, flashAlpha, t / 0.10f));
                yield return null;
            }

            // Fade out — 0.2s real time
            t = 0f;
            while (t < 0.20f)
            {
                t += Time.unscaledDeltaTime;
                _flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(flashAlpha, 0f, t / 0.20f));
                yield return null;
            }

            _flashImage.color  = new Color(1f, 1f, 1f, 0f);
            Time.timeScale     = restoreScale;
        }
    }
}
```

- [ ] **Step 2: Compile check**

Read console. No errors expected.

- [ ] **Step 3: Add NearDeathEffect to GameScene**

```json
// manage_gameobject: create
{ "action": "create", "name": "NearDeathEffect" }
// manage_components: add
{ "action": "add", "gameObjectName": "NearDeathEffect", "componentType": "StrafAdvance.NearDeathEffect" }
```

- [ ] **Step 4: Play-mode verification**

Enter play mode. Use `execute_code` to spike damage:
```csharp
var h = FindFirstObjectByType<StrafAdvance.PlayerHealth>();
h.TakeDamage(h.MaxHp - Mathf.FloorToInt(h.MaxHp * 0.19f)); // drop to ~19%
```
Expected: 0.3s cinematic slow-mo, white flash fades in then out. Damage player again — effect does NOT repeat. Start a new game (GameStateChanged → Playing) — `_triggered` resets, effect can fire again.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Core/NearDeathEffect.cs Assets/_Game/Scenes/GameScene.unity
git commit -m "feat(core): near-death effect — one-shot 0.3s slow-mo + white flash on first sub-20% HP hit"
```

---

## Self-Review

**Spec coverage:**
- 2.1 Camera lead lag ✓ (Task 3 — `leadScale`, `leadSmoothTime=0.1s`)
- 2.2 FOV pulse on dodge ✓ (Task 3 — `FovPulseRoutine`, 55→60→55)
- 2.3 Camera roll on strafe ✓ (Task 3 — `rollGain`, ±3° clamp)
- 2.4 Cinemachine impulse on hit ✓ (Task 3 — directional spring impulse via `AddImpulse`)
- 2.5 Hitstop values ✓ (Task 4 — grunt 0.07s, elite 0.15s)
- 2.6 Player damage flash ✓ (Task 5 — `PlayerDamageFlash`, 0.08s, MaterialPropertyBlock)
- 2.7 Low-HP vignette ✓ (Task 6 — URP Vignette, pulse freq scales with HP drop)
- 2.8 Combo tier flash ✓ (Task 7 — scale-pop on `c.Multiplier > _prevMultiplier`)
- 2.9 Slow-mo near-death ✓ (Task 8 — `NearDeathEffect`, once per run, 0.3s)

**Placeholder scan:** No TBDs. All code complete. Verification steps have exact execute_code snippets.

**Type consistency:**
- `PositionShake` / `RotationShake` defined Task 1, read Task 3 ✓
- `VelocityX` defined Task 2, read Task 3 ✓
- `_prevMultiplier` field + `ComboPopRoutine()` both in Task 7 ✓
- `EnemyType.MiniBoss` used in Task 3 — confirmed published by `WaveSpawner.cs:202` ✓
