# Clean Sci-Fi Visual Upgrade — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform Strafe Advance's visuals to a Clean Sci-Fi aesthetic (navy/electric blue palette, Mass Effect/Destiny feel) across materials, post-processing, HUD, and VFX.

**Architecture:** Single editor script function `StrafAdvance > 10. Apply Sci-Fi Upgrade` added to the existing `GameSetup.cs`. It calls four sub-functions: `ApplySciFiMaterials()`, `ApplyPostProcessing()`, `ApplyVFX()`. HUD is restyled by editing `HUDController.cs` color constants directly (HUD self-creates at runtime). Particle VFX prefabs saved to `Assets/Resources/VFX/` and loaded via `Resources.Load` in `Bullet.cs` and `EnemyBase.cs`.

**Tech Stack:** Unity 6 URP, C# Editor scripting, `PrefabUtility.EditPrefabContentsScope`, `UnityEngine.Rendering.Universal` Volume API, `ParticleSystem` API, `Resources.Load`.

---

## File Map

| File | Change |
|------|--------|
| `Assets/_Game/Scripts/Editor/GameSetup.cs` | Add menu item 10 + `ApplySciFiMaterials()`, `ApplyPostProcessing()`, `ApplyVFX()` sub-functions |
| `Assets/_Game/Scripts/UI/HUDController.cs` | Update color constants in `CreateSlider()` and `CreateLabel()` |
| `Assets/_Game/Scripts/Combat/Bullet.cs` | Add `SpawnHitVFX()` call in `OnTriggerEnter` |
| `Assets/_Game/Scripts/Enemies/EnemyBase.cs` | Add `SpawnDeathVFX()` call in `Die()` |
| `Assets/Resources/VFX/*.prefab` | Created by `ApplyVFX()` at runtime |

---

## Task 1: Materials — Clean Sci-Fi Palette

Updates existing material assets in `Assets/_Game/Art/Materials/` to the sci-fi palette. Does NOT create new prefabs; existing prefab references stay valid.

**Files:**
- Modify: `Assets/_Game/Scripts/Editor/GameSetup.cs`

- [ ] **Step 1.1: Add menu item entry point (materials only for now)**

At the top of `GameSetup.cs`, the existing usings are fine — no new ones needed for this task.

Add this block **before** the `RewirePlayerPrefab` method (after the const declarations):

```csharp
// ─── 10. Apply Sci-Fi Upgrade ────────────────────────────────────────────────
[MenuItem("StrafAdvance/10. Apply Sci-Fi Upgrade", priority = 100)]
public static void ApplySciFiUpgrade()
{
    ApplySciFiMaterials();
    // ApplyPostProcessing() added in Task 2
    // ApplyVFX() added in Task 4
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("[SciFiUpgrade] All passes complete. Check console for warnings.");
}
```

- [ ] **Step 1.2: Add `ApplySciFiMaterials()` and `UpdateSciFiMat()` helper**

Add this block immediately after the `ApplySciFiUpgrade()` method:

```csharp
static void ApplySciFiMaterials()
{
    var urp = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    // Electric blue (player + corridor)
    Color navyBase  = new Color(0.05f, 0.10f, 0.18f, 1f);
    Color blueGlow  = new Color(0.31f, 0.76f, 0.97f, 1f);
    // Enemy red (grunts)
    Color redBase   = new Color(0.10f, 0.02f, 0.02f, 1f);
    Color redGlow   = new Color(1.00f, 0.27f, 0.27f, 1f);
    // Cyan-green (flanker + elite)
    Color tealBase  = new Color(0.02f, 0.10f, 0.09f, 1f);
    Color tealGlow  = new Color(0.00f, 1.00f, 0.80f, 1f);
    // Gold (boss)
    Color blackBase = new Color(0.04f, 0.03f, 0.00f, 1f);
    Color goldGlow  = new Color(1.00f, 0.85f, 0.00f, 1f);
    // Wall (dark panel)
    Color wallBase  = new Color(0.03f, 0.05f, 0.09f, 1f);
    Color wallGlow  = new Color(0.31f, 0.76f, 0.97f, 1f);

    UpdateSciFiMat("Player",  navyBase,  blueGlow,  2.0f, 0.8f, 0.4f, urp);
    UpdateSciFiMat("Grunt",   redBase,   redGlow,   1.5f, 0.6f, 0.3f, urp);
    UpdateSciFiMat("Flanker", tealBase,  tealGlow,  1.6f, 0.7f, 0.5f, urp);
    UpdateSciFiMat("Elite",   tealBase,  tealGlow,  1.8f, 0.7f, 0.5f, urp);
    UpdateSciFiMat("Boss",    blackBase, goldGlow,  2.5f, 0.9f, 0.6f, urp);
    UpdateSciFiMat("Bullet",  Color.black, blueGlow, 5.0f, 0.0f, 0.0f, urp);
    UpdateSciFiMat("Tile",    navyBase,  blueGlow,  0.5f, 0.5f, 0.3f, urp);
    UpdateSciFiMat("Wall",    wallBase,  wallGlow,  0.3f, 0.5f, 0.3f, urp);

    Debug.Log("[SciFiUpgrade] Materials updated.");
}

static void UpdateSciFiMat(string name, Color baseColor, Color emissiveColor,
    float emissiveIntensity, float metallic, float smoothness, Shader urp)
{
    string path = $"Assets/_Game/Art/Materials/{name}.mat";
    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (mat == null) { Debug.LogWarning($"[SciFiUpgrade] Material not found: {path}"); return; }

    if (urp != null) mat.shader = urp;
    mat.color = baseColor;
    mat.SetColor("_BaseColor", baseColor);
    mat.SetColor("_EmissionColor", emissiveColor * emissiveIntensity);
    mat.EnableKeyword("_EMISSION");
    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    mat.SetFloat("_Metallic",    metallic);
    mat.SetFloat("_Smoothness",  smoothness);
    EditorUtility.SetDirty(mat);
}
```

- [ ] **Step 1.3: Check for compile errors**

Via MCP: `read_console(types=["error"], count=10)`. Expected: zero errors. If errors, fix before continuing.

- [ ] **Step 1.4: Run the menu item (materials only — full upgrade runs all passes, that's fine)**

Via MCP: `execute_menu_item(menu_path="StrafAdvance/10. Apply Sci-Fi Upgrade")`

- [ ] **Step 1.5: Take screenshot to verify materials applied**

Via MCP: `manage_camera(action="screenshot", capture_source="scene_view", include_image=True, max_resolution=512)`

Expected: All prefab meshes in the scene should show dark navy/blue for player and corridor, red glow for grunts, teal for flankers/elites, gold for boss. No magenta/pink fallback materials.

- [ ] **Step 1.6: Commit**

```bash
git add Assets/_Game/Scripts/Editor/GameSetup.cs Assets/_Game/Art/Materials/
git commit -m "feat: add sci-fi upgrade script — materials pass (navy/blue/gold palette)"
```

---

## Task 2: Post-Processing — Bloom, Vignette, ACES Tonemapping

Modifies `Assets/Settings/DefaultVolumeProfile.asset` to add URP post-processing effects.

**Files:**
- Modify: `Assets/_Game/Scripts/Editor/GameSetup.cs`

- [ ] **Step 2.1: Add URP rendering namespace**

Add this using directive at the top of `GameSetup.cs` with the existing usings:

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
```

- [ ] **Step 2.2: Add `ApplyPostProcessing()` method**

Add this method after `ApplySciFiMaterials()` / `UpdateSciFiMat()`:

```csharp
static void ApplyPostProcessing()
{
    var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
    if (profile == null)
    {
        Debug.LogError("[SciFiUpgrade] DefaultVolumeProfile not found at Assets/Settings/DefaultVolumeProfile.asset");
        return;
    }

    // Bloom — makes emissive surfaces glow
    if (!profile.TryGet<Bloom>(out var bloom))
        bloom = profile.Add<Bloom>(false);
    bloom.active = true;
    bloom.intensity.Override(0.6f);
    bloom.threshold.Override(0.8f);
    bloom.scatter.Override(0.7f);

    // Vignette — cinematic dark edges
    if (!profile.TryGet<Vignette>(out var vignette))
        vignette = profile.Add<Vignette>(false);
    vignette.active = true;
    vignette.intensity.Override(0.35f);
    vignette.rounded.Override(true);

    // Color Adjustments — subtle brightness + saturation boost
    if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
        colorAdj = profile.Add<ColorAdjustments>(false);
    colorAdj.active = true;
    colorAdj.postExposure.Override(0.1f);
    colorAdj.saturation.Override(10f);

    // Tonemapping — ACES for cinematic contrast
    if (!profile.TryGet<Tonemapping>(out var tonemap))
        tonemap = profile.Add<Tonemapping>(false);
    tonemap.active = true;
    tonemap.mode.Override(TonemappingMode.ACES);

    // White Balance — cool blue tint
    if (!profile.TryGet<WhiteBalance>(out var wb))
        wb = profile.Add<WhiteBalance>(false);
    wb.active = true;
    wb.temperature.Override(-10f);

    EditorUtility.SetDirty(profile);
    Debug.Log("[SciFiUpgrade] Post-processing configured.");
}
```

- [ ] **Step 2.2b: Uncomment `ApplyPostProcessing()` call in `ApplySciFiUpgrade()`**

In `GameSetup.cs`, find:

```csharp
    // ApplyPostProcessing() added in Task 2
```

Replace with:

```csharp
    ApplyPostProcessing();
```

- [ ] **Step 2.3: Check for compile errors**

Via MCP: `read_console(types=["error"], count=10)`. Expected: zero errors.

If you see `CS0234: The type or namespace 'Universal' does not exist` — the URP package is present but the assembly reference may be missing. In that case, replace the URP-specific types with the try/catch approach or check that `com.unity.render-pipelines.universal` is in `Packages/manifest.json` (it is, per project context).

- [ ] **Step 2.4: Run upgrade and verify bloom**

Via MCP: `execute_menu_item(menu_path="StrafAdvance/10. Apply Sci-Fi Upgrade")`

Then enter play mode and take screenshot:
- Via MCP: `manage_editor(action="play")`
- Wait 2 seconds
- Via MCP: `manage_camera(action="screenshot", include_image=True, max_resolution=512)`
- Via MCP: `manage_editor(action="stop")`

Expected: Emissive objects (player, enemies, bullets) should show visible bloom glow halos. Dark vignette around screen edges. Scene should look cooler/bluer overall.

- [ ] **Step 2.5: Commit**

```bash
git add Assets/_Game/Scripts/Editor/GameSetup.cs Assets/Settings/DefaultVolumeProfile.asset
git commit -m "feat: sci-fi upgrade — post-processing pass (bloom, vignette, ACES, cool grade)"
```

---

## Task 3: HUD Restyle — Electric Blue Sci-Fi UI

Edits `HUDController.cs` to use sci-fi colors for the HP bar and wave label that are self-created at runtime.

**Files:**
- Modify: `Assets/_Game/Scripts/UI/HUDController.cs`

- [ ] **Step 3.1: Update HP bar background color**

In `HUDController.cs` `CreateSlider()`, find the background image color line:

```csharp
var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
```

Replace with:

```csharp
var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.03f, 0.07f, 0.13f, 0.88f);
```

- [ ] **Step 3.2: Update HP bar fill color**

In `CreateSlider()`, find the fill color line:

```csharp
fillImg.color = name.Contains("Boss") ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 0.8f, 0.2f);
```

Replace with:

```csharp
fillImg.color = name.Contains("Boss") ? new Color(1f, 0.27f, 0.27f) : new Color(0.31f, 0.76f, 0.97f);
```

- [ ] **Step 3.3: Update wave label color**

In `CreateLabel()`, find:

```csharp
tmp.text = "Wave 1"; tmp.fontSize = 22; tmp.color = Color.white;
```

Replace with:

```csharp
tmp.text = "Wave 1"; tmp.fontSize = 22; tmp.color = new Color(0.31f, 0.76f, 0.97f);
```

- [ ] **Step 3.4: Update AutoWire HP bar color call**

In `AutoWire()` coroutine, find:

```csharp
if (playerHpSlider == null) playerHpSlider = CreateSlider(canvas.transform, "HPBar",
    new Color(0.2f, 0.8f, 0.2f), new Vector2(10, -10), new Vector2(220, 30));
```

Replace with:

```csharp
if (playerHpSlider == null) playerHpSlider = CreateSlider(canvas.transform, "HPBar",
    new Color(0.31f, 0.76f, 0.97f), new Vector2(10, -10), new Vector2(220, 30));
```

- [ ] **Step 3.5: Check for compile errors**

Via MCP: `read_console(types=["error"], count=10)`. Expected: zero errors.

- [ ] **Step 3.6: Verify in play mode**

Via MCP: `manage_editor(action="play")`

Wait 2 seconds, then screenshot:
Via MCP: `manage_camera(action="screenshot", include_image=True, max_resolution=512)`

Expected: HP bar should be dark navy background with electric blue fill. Wave label should be electric blue text. If HUD doesn't appear (off-screen), check console — HUDController.AutoWire() logs nothing on failure, check that Canvas is in scene.

Via MCP: `manage_editor(action="stop")`

- [ ] **Step 3.7: Commit**

```bash
git add Assets/_Game/Scripts/UI/HUDController.cs
git commit -m "feat: sci-fi upgrade — HUD restyle (electric blue HP bar + wave label)"
```

---

## Task 4: VFX — Particle Effects + Trail Renderer

Creates particle prefabs at `Assets/Resources/VFX/` via editor script, updates `PlayerBullet` trail, and wires spawning into `Bullet.cs` and `EnemyBase.cs`.

**Files:**
- Modify: `Assets/_Game/Scripts/Editor/GameSetup.cs`
- Modify: `Assets/_Game/Scripts/Combat/Bullet.cs`
- Modify: `Assets/_Game/Scripts/Enemies/EnemyBase.cs`

### Step 4.1-4.3: Editor script — ApplyVFX()

- [ ] **Step 4.1: Add `ApplyVFX()` method to `GameSetup.cs`**

Add after `ApplyPostProcessing()`:

```csharp
static void ApplyVFX()
{
    EnsureDir("Assets/Resources");
    EnsureDir("Assets/Resources/VFX");

    var urp = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    // Update player bullet trail to electric blue
    UpdateBulletTrail("Combat/PlayerBullet", new Color(0.31f, 0.76f, 0.97f), urp);

    // HitSpark — cyan/white burst on bullet hit
    CreateVFXPrefab("HitSpark",
        new Color(0.31f, 0.76f, 0.97f, 1f),
        new Color(1.00f, 1.00f, 1.00f, 0f),
        lifetime: 0.15f, speed: 3.5f, size: 0.08f, burst: 12, urp: urp);

    // EnemyDeath — blue-white sphere burst
    CreateVFXPrefab("EnemyDeath",
        new Color(0.31f, 0.76f, 0.97f, 1f),
        new Color(0.50f, 0.90f, 1.00f, 0f),
        lifetime: 0.40f, speed: 5f, size: 0.15f, burst: 20, urp: urp);

    // BossDeath — gold/orange large burst
    CreateVFXPrefab("BossDeath",
        new Color(1.00f, 0.85f, 0.00f, 1f),
        new Color(1.00f, 0.40f, 0.00f, 0f),
        lifetime: 1.20f, speed: 7f, size: 0.28f, burst: 40, urp: urp);

    Debug.Log("[SciFiUpgrade] VFX prefabs created in Assets/Resources/VFX/");
}

static void UpdateBulletTrail(string prefabSubPath, Color trailColor, Shader urp)
{
    string path = $"{PrefabPath}/{prefabSubPath}.prefab";
    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

    using var scope = new PrefabUtility.EditPrefabContentsScope(path);
    var root = scope.prefabContentsRoot;

    var trail = root.GetComponent<TrailRenderer>();
    if (trail == null) trail = root.AddComponent<TrailRenderer>();
    trail.time        = 0.1f;
    trail.startWidth  = 0.06f;
    trail.endWidth    = 0f;
    trail.startColor  = trailColor;
    trail.endColor    = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

    var trailMat = new Material(urp ?? Shader.Find("Standard"));
    trailMat.color = trailColor;
    trailMat.SetColor("_BaseColor", trailColor);
    trailMat.SetColor("_EmissionColor", trailColor * 3f);
    trailMat.EnableKeyword("_EMISSION");
    trail.material = trailMat;
}

static void CreateVFXPrefab(string name, Color startColor, Color endColor,
    float lifetime, float speed, float size, int burst, Shader urp)
{
    string savePath = $"Assets/Resources/VFX/{name}.prefab";

    var go = new GameObject(name);
    var ps = go.AddComponent<ParticleSystem>();

    var main = ps.main;
    main.startLifetime  = lifetime;
    main.startSpeed     = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
    main.startSize      = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
    main.startColor     = new ParticleSystem.MinMaxGradient(startColor, endColor);
    main.loop           = false;
    main.playOnAwake    = true;
    main.stopAction     = ParticleSystemStopAction.Destroy;
    main.gravityModifier = 0f;
    main.simulationSpace = ParticleSystemSimulationSpace.World;

    var emission = ps.emission;
    emission.enabled       = true;
    emission.rateOverTime  = 0;
    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, burst) });

    var shape = ps.shape;
    shape.enabled    = true;
    shape.shapeType  = ParticleSystemShapeType.Sphere;
    shape.radius     = 0.05f;

    var renderer = go.GetComponent<ParticleSystemRenderer>();
    var mat = new Material(urp ?? Shader.Find("Standard"));
    mat.color = startColor;
    mat.SetColor("_BaseColor", startColor);
    mat.SetColor("_EmissionColor", startColor * 3f);
    mat.EnableKeyword("_EMISSION");
    renderer.material = mat;
    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    renderer.receiveShadows = false;

    PrefabUtility.SaveAsPrefabAsset(go, savePath);
    Object.DestroyImmediate(go);
}
```

- [ ] **Step 4.1b: Uncomment `ApplyVFX()` call in `ApplySciFiUpgrade()`**

In `GameSetup.cs`, find:

```csharp
    // ApplyVFX() added in Task 4
```

Replace with:

```csharp
    ApplyVFX();
```

- [ ] **Step 4.2: Check for compile errors**

Via MCP: `read_console(types=["error"], count=10)`. Expected: zero errors.

- [ ] **Step 4.3: Run upgrade and verify VFX prefabs created**

Via MCP: `execute_menu_item(menu_path="StrafAdvance/10. Apply Sci-Fi Upgrade")`

Verify the prefabs were created:
```bash
ls Assets/Resources/VFX/
```
Expected output: `BossDeath.prefab  BossDeath.prefab.meta  EnemyDeath.prefab  EnemyDeath.prefab.meta  HitSpark.prefab  HitSpark.prefab.meta`

### Step 4.4-4.6: Wire VFX into runtime scripts

- [ ] **Step 4.4: Update `Bullet.cs` to spawn HitSpark**

In `Bullet.cs`, find the `OnTriggerEnter` method's successful hit block:

```csharp
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                _pool?.Return(this);
            }
```

Replace with:

```csharp
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                SpawnHitVFX();
                _pool?.Return(this);
            }
```

Add this private method to `Bullet.cs` (inside the class, after `OnReturnToPool`):

```csharp
        private void SpawnHitVFX()
        {
            var prefab = Resources.Load<GameObject>("VFX/HitSpark");
            if (prefab != null) Instantiate(prefab, transform.position, Quaternion.identity);
        }
```

- [ ] **Step 4.5: Update `EnemyBase.cs` to spawn death VFX**

In `EnemyBase.cs`, find:

```csharp
        protected virtual void Die() => Destroy(gameObject);
```

Replace with:

```csharp
        protected virtual void Die()
        {
            SpawnDeathVFX();
            Destroy(gameObject);
        }

        protected virtual void SpawnDeathVFX()
        {
            var prefab = Resources.Load<GameObject>("VFX/EnemyDeath");
            if (prefab != null) Instantiate(prefab, transform.position, Quaternion.identity);
        }
```

- [ ] **Step 4.6: Check for compile errors**

Via MCP: `read_console(types=["error"], count=10)`. Expected: zero errors.

- [ ] **Step 4.7: Verify VFX in play mode**

Via MCP: `manage_editor(action="play")`

Wait 3 seconds for gameplay to start, then observe. Enemy deaths should emit particle bursts. Bullet trail should be electric blue.

Via MCP: `manage_camera(action="screenshot", include_image=True, max_resolution=512)`

Expected: Visible cyan trail on player bullets, particle burst when enemies die.

Via MCP: `manage_editor(action="stop")`

- [ ] **Step 4.8: Commit**

```bash
git add Assets/_Game/Scripts/Editor/GameSetup.cs Assets/_Game/Scripts/Combat/Bullet.cs Assets/_Game/Scripts/Enemies/EnemyBase.cs Assets/Resources/
git commit -m "feat: sci-fi upgrade — VFX pass (particle death/hit effects, blue bullet trail)"
```

---

## Final Verification

- [ ] **Full upgrade run + screenshot**

Via MCP: `execute_menu_item(menu_path="StrafAdvance/10. Apply Sci-Fi Upgrade")`

Enter play mode, let game run through first wave, screenshot:
Via MCP: `manage_camera(action="screenshot", include_image=True, max_resolution=512)`

Expected: dark navy corridor, electric blue player + bullet, red grunts dying with cyan particle bursts, blue gradient HP bar, wave label in electric blue, bloom visible on all emissive objects.

- [ ] **Checklist against spec**

- [ ] All prefabs render with correct materials — no magenta
- [ ] Bloom visible on emissive objects
- [ ] HUD shows blue gradient HP bar + wave label
- [ ] Bullet has cyan glow trail
- [ ] Enemy death spawns particle burst
- [ ] No compilation errors
