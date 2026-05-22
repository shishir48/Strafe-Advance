using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace StrafAdvance.Editor
{
    public static class GameSetup
    {
        private const string SOPath    = "Assets/_Game/ScriptableObjects";
        private const string PrefabPath = "Assets/_Game/Prefabs";
        private const string ScenePath  = "Assets/_Game/Scenes";

        // ─── 10. Apply Sci-Fi Upgrade ────────────────────────────────────────────────
        [MenuItem("StrafAdvance/10. Apply Sci-Fi Upgrade", priority = 100)]
        public static void ApplySciFiUpgrade()
        {
            ApplySciFiMaterials();
            ApplyPostProcessing();
            ApplyVFX();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SciFiUpgrade] All passes complete. Check console for warnings.");
        }

        static void ApplySciFiMaterials()
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Color navyBase  = new Color(0.05f, 0.10f, 0.18f, 1f);
            Color blueGlow  = new Color(0.31f, 0.76f, 0.97f, 1f);
            Color redBase   = new Color(0.10f, 0.02f, 0.02f, 1f);
            Color redGlow   = new Color(1.00f, 0.27f, 0.27f, 1f);
            Color tealBase  = new Color(0.02f, 0.10f, 0.09f, 1f);
            Color tealGlow  = new Color(0.00f, 1.00f, 0.80f, 1f);
            Color blackBase = new Color(0.04f, 0.03f, 0.00f, 1f);
            Color goldGlow  = new Color(1.00f, 0.85f, 0.00f, 1f);
            Color wallBase  = new Color(0.03f, 0.05f, 0.09f, 1f);
            Color wallGlow  = new Color(0.31f, 0.76f, 0.97f, 1f);

            UpdateSciFiMat("Player",  navyBase,  blueGlow,  0.15f, 0.7f, 0.5f, urp);
            UpdateSciFiMat("Grunt",   redBase,   redGlow,   0.15f, 0.6f, 0.4f, urp);
            UpdateSciFiMat("Flanker", tealBase,  tealGlow,  0.15f, 0.7f, 0.5f, urp);
            UpdateSciFiMat("Elite",   tealBase,  tealGlow,  0.15f, 0.7f, 0.5f, urp);
            UpdateSciFiMat("Boss",    blackBase, goldGlow,  0.3f,  0.9f, 0.6f, urp);
            UpdateSciFiMat("Bullet",  Color.black, blueGlow, 5.0f, 0.0f, 0.0f, urp);
            UpdateSciFiMat("Tile",    navyBase,  blueGlow,  0.15f, 0.5f, 0.3f, urp);
            UpdateSciFiMat("Wall",    wallBase,  wallGlow,  0.12f, 0.5f, 0.3f, urp);

            // Remap ALL renderer material slots in FBX-based prefabs
            Material playerMat2  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Player.mat");
            Material gruntMat2   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Grunt.mat");
            Material flankerMat2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Flanker.mat");
            Material eliteMat2   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Elite.mat");
            Material bossMat2    = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Boss.mat");
            Material tileMat2    = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Tile.mat");

            RemapPrefabRenderers("Player",               playerMat2);
            RemapPrefabRenderers("Enemies/GruntEnemy",   gruntMat2);
            RemapPrefabRenderers("Enemies/FlankerEnemy",  flankerMat2);
            RemapPrefabRenderers("Enemies/EliteEnemy",   eliteMat2);
            RemapPrefabRenderers("Enemies/Boss",         bossMat2);
            RebuildCorridorTile(tileMat2, urp);

            Debug.Log("[SciFiUpgrade] Materials updated.");
        }

        static void RemapPrefabRenderers(string prefabSubPath, Material mat)
        {
            if (mat == null) return;
            string path = $"{PrefabPath}/{prefabSubPath}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            foreach (var r in scope.prefabContentsRoot.GetComponentsInChildren<Renderer>())
            {
                var slots = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < slots.Length; i++) slots[i] = mat;
                r.sharedMaterials = slots;
            }
        }

        static void RebuildCorridorTile(Material tileMat, Shader urp)
        {
            string path = $"{PrefabPath}/Level/CorridorTile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

            // Bright edge-strip material — neon blue glow line
            string edgePath = "Assets/_Game/Art/Materials/EdgeGlow.mat";
            var edgeMat = AssetDatabase.LoadAssetAtPath<Material>(edgePath) ?? new Material(urp ?? Shader.Find("Standard"));
            edgeMat.color = new Color(0.02f, 0.06f, 0.12f);
            edgeMat.SetColor("_BaseColor", new Color(0.02f, 0.06f, 0.12f));
            edgeMat.SetColor("_EmissionColor", new Color(0.31f, 0.76f, 0.97f) * 4f);
            edgeMat.EnableKeyword("_EMISSION");
            edgeMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (AssetDatabase.LoadAssetAtPath<Material>(edgePath) == null)
                AssetDatabase.CreateAsset(edgeMat, edgePath);
            else
                EditorUtility.SetDirty(edgeMat);

            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var root = scope.prefabContentsRoot;

            // Remove FBX mesh children, keep functional children (CorridorTile component stays on root)
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            // Floor — wide flat panel
            CreatePart(root, "Floor", PrimitiveType.Cube, Vector3.zero, new Vector3(8f, 0.08f, 12f), tileMat);
            // Side walls — thin vertical panels
            CreatePart(root, "WallL", PrimitiveType.Cube, new Vector3(-4f, 1.5f, 0), new Vector3(0.08f, 3f, 12f), tileMat);
            CreatePart(root, "WallR", PrimitiveType.Cube, new Vector3( 4f, 1.5f, 0), new Vector3(0.08f, 3f, 12f), tileMat);
            // Glowing floor-edge strips — neon Tron lines
            CreatePart(root, "EdgeL", PrimitiveType.Cube, new Vector3(-3.85f, 0.05f, 0), new Vector3(0.12f, 0.06f, 12f), edgeMat);
            CreatePart(root, "EdgeR", PrimitiveType.Cube, new Vector3( 3.85f, 0.05f, 0), new Vector3(0.12f, 0.06f, 12f), edgeMat);
            // Wall-base accent strips
            CreatePart(root, "BaseL", PrimitiveType.Cube, new Vector3(-4f, 0.08f, 0), new Vector3(0.08f, 0.15f, 12f), edgeMat);
            CreatePart(root, "BaseR", PrimitiveType.Cube, new Vector3( 4f, 0.08f, 0), new Vector3(0.08f, 0.15f, 12f), edgeMat);
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

        static void ApplyVFX()
        {
            EnsureDir("Assets/Resources");
            EnsureDir("Assets/Resources/VFX");

            var urp = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            UpdateBulletTrail("Combat/PlayerBullet", new Color(0.31f, 0.76f, 0.97f), urp);

            CreateVFXPrefab("HitSpark",
                new Color(0.31f, 0.76f, 0.97f, 1f),
                new Color(1.00f, 1.00f, 1.00f, 0f),
                lifetime: 0.15f, speed: 3.5f, size: 0.08f, burst: 12, urp: urp);

            CreateVFXPrefab("EnemyDeath",
                new Color(0.31f, 0.76f, 0.97f, 1f),
                new Color(0.50f, 0.90f, 1.00f, 0f),
                lifetime: 0.40f, speed: 5f, size: 0.15f, burst: 20, urp: urp);

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
            emission.enabled      = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, burst) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;

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

        static void ApplyPostProcessing()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (profile == null)
            {
                Debug.LogError("[SciFiUpgrade] DefaultVolumeProfile not found at Assets/Settings/DefaultVolumeProfile.asset");
                return;
            }

            if (!profile.TryGet<Bloom>(out var bloom))
                bloom = profile.Add<Bloom>(false);
            bloom.active = true;
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.2f);
            bloom.scatter.Override(0.5f);

            if (!profile.TryGet<Vignette>(out var vignette))
                vignette = profile.Add<Vignette>(false);
            vignette.active = true;
            vignette.intensity.Override(0.35f);
            vignette.rounded.Override(true);

            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
                colorAdj = profile.Add<ColorAdjustments>(false);
            colorAdj.active = true;
            colorAdj.postExposure.Override(0.1f);
            colorAdj.saturation.Override(10f);

            if (!profile.TryGet<Tonemapping>(out var tonemap))
                tonemap = profile.Add<Tonemapping>(false);
            tonemap.active = true;
            tonemap.mode.Override(TonemappingMode.ACES);

            if (!profile.TryGet<WhiteBalance>(out var wb))
                wb = profile.Add<WhiteBalance>(false);
            wb.active = true;
            wb.temperature.Override(-10f);

            EditorUtility.SetDirty(profile);
            Debug.Log("[SciFiUpgrade] Post-processing configured.");
        }

        // ─── Rewire Player Prefab ────────────────────────────────────────────────
        [MenuItem("StrafAdvance/Rewire Player Prefab", priority = 3)]
        public static void RewirePlayerPrefab()
        {
            string path = $"{PrefabPath}/Player.prefab";
            var playerGO = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (playerGO == null) { Debug.LogError("[GameSetup] Player.prefab not found."); return; }

            var bulletPref = LoadPrefab<Bullet>("Combat/PlayerBullet");
            if (bulletPref == null) { Debug.LogError("[GameSetup] PlayerBullet.prefab not found."); return; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                var shooter = root.GetComponent<AutoShooter>();
                var health  = root.GetComponent<PlayerHealth>();
                var ctrl    = root.GetComponent<PlayerController>();
                var buffs   = root.GetComponent<PlayerBuffs>();
                var config  = LoadSO<PlayerConfig>("DefaultPlayerConfig");
                var fp      = root.transform.Find("FirePoint");

                if (shooter != null)
                {
                    var so = new SerializedObject(shooter);
                    so.FindProperty("bulletPrefab").objectReferenceValue = bulletPref;
                    if (config != null) so.FindProperty("config").objectReferenceValue = config;
                    if (fp != null) so.FindProperty("firePoint").objectReferenceValue = fp;
                    so.ApplyModifiedProperties();
                }
                if (ctrl != null && config != null && health != null)
                {
                    var so = new SerializedObject(ctrl);
                    so.FindProperty("config").objectReferenceValue = config;
                    so.FindProperty("health").objectReferenceValue = health;
                    so.ApplyModifiedProperties();
                }
                if (buffs != null && shooter != null && health != null)
                {
                    var so = new SerializedObject(buffs);
                    so.FindProperty("autoShooter").objectReferenceValue = shooter;
                    so.FindProperty("health").objectReferenceValue = health;
                    so.ApplyModifiedProperties();
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[GameSetup] Player prefab rewired. Press Play.");
        }

        // ─── Apply Kenney Models ─────────────────────────────────────────────────
        [MenuItem("StrafAdvance/9. Apply Kenney 3D Models", priority = 90)]
        public static void ApplyKenneyModels()
        {
            const string modelsPath = "Assets/_Game/Art/Models";

            // Load materials for applying to FBX meshes
            var playerMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Player.mat");
            var gruntMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Grunt.mat");
            var flankerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Flanker.mat");
            var eliteMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Elite.mat");
            var bossMat    = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Boss.mat");
            var tileMat    = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Tile.mat");

            // Helper: load mesh from FBX and apply to prefab replacing primitive renderers
            void SwapMesh(string prefabSubPath, string fbxName, Vector3 scale, Vector3 rotation, Material applyMat)
            {
                string fbxPath = $"{modelsPath}/{fbxName}";
                var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbxAsset == null) { Debug.LogWarning($"[GameSetup] FBX not found: {fbxPath}"); return; }

                string prefabPath = $"{PrefabPath}/{prefabSubPath}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;

                using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
                var root = scope.prefabContentsRoot;

                // Remove old visual children but KEEP named functional ones (FirePoint, etc.)
                var keepNames = new System.Collections.Generic.HashSet<string> { "FirePoint" };
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    var child = root.transform.GetChild(i);
                    if (!keepNames.Contains(child.name))
                        Object.DestroyImmediate(child.gameObject);
                }

                // Instantiate FBX mesh as child (use Object.Instantiate to break prefab link)
                var meshObj = Object.Instantiate(fbxAsset);
                meshObj.name = "Mesh";
                meshObj.transform.SetParent(root.transform, false);
                meshObj.transform.localScale    = scale;
                meshObj.transform.localRotation = Quaternion.Euler(rotation);

                // Remove colliders from mesh children
                foreach (var col in meshObj.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);

                // Apply material to all renderers
                if (applyMat != null)
                    foreach (var r in meshObj.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = applyMat;
            }

            SwapMesh("Player",              "astronautA.fbx",    Vector3.one * 0.015f, new Vector3(0, 180, 0), playerMat);
            SwapMesh("Enemies/GruntEnemy",  "astronautA.fbx",    Vector3.one * 0.015f, new Vector3(0, 180, 0), gruntMat);
            SwapMesh("Enemies/FlankerEnemy","craft_speederA.fbx", Vector3.one * 0.007f, new Vector3(0, 180, 0), flankerMat);
            SwapMesh("Enemies/EliteEnemy",  "craft_speederC.fbx", Vector3.one * 0.008f, new Vector3(0, 180, 0), eliteMat);
            SwapMesh("Enemies/Boss",        "turret_double.fbx",  Vector3.one * 0.015f, new Vector3(0, 0, 0),   bossMat);
            SwapMesh("Level/CorridorTile",  "corridor.fbx", new Vector3(0.015f, 0.015f, 0.015f), Vector3.zero, tileMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] Kenney 3D models applied to all prefabs.");
        }

        // ─── Upgrade Graphics ────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/8. Upgrade Graphics (Sci-Fi Neon)", priority = 80)]
        public static void UpgradeGraphics()
        {
            EnsureDir("Assets/_Game/Art/Materials");
            EnsureDir("Assets/_Game/Prefabs/Combat");
            EnsureDir("Assets/_Game/Prefabs/Enemies");
            EnsureDir("Assets/_Game/Prefabs/Level");

            var urp = Shader.Find("Universal Render Pipeline/Lit")
                   ?? Shader.Find("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader")
                   ?? Shader.Find("Standard");
            if (urp == null) { Debug.LogError("[GameSetup] Could not find URP/Standard shader."); return; }

            // ── Neon emissive materials ──────────────────────────────────────────
            Material playerMat  = CreateNeonMat("Player",   new Color(0.1f, 0.4f, 1f),    new Color(0f,   0.5f, 2f),   urp);
            Material gruntMat   = CreateNeonMat("Grunt",    new Color(1f,   0.15f, 0.05f), new Color(2f,   0.1f, 0f),   urp);
            Material flankerMat = CreateNeonMat("Flanker",  new Color(1f,   0.5f,  0f),    new Color(2f,   0.8f, 0f),   urp);
            Material eliteMat   = CreateNeonMat("Elite",    new Color(0.6f, 0f,    1f),    new Color(1.5f, 0f,   3f),   urp);
            Material bossMat    = CreateNeonMat("Boss",     new Color(1f,   0f,    0.3f),  new Color(3f,   0f,   0.5f), urp);
            Material bulletMat  = CreateNeonMat("Bullet",   new Color(0.9f, 1f,    0.2f),  new Color(2f,   3f,   0f),   urp);
            Material tileMat    = CreateNeonMat("Tile",     new Color(0.06f,0.06f, 0.09f), new Color(0.1f, 0.3f, 0.5f), urp);
            Material wallMat    = CreateNeonMat("Wall",     new Color(0.04f,0.04f, 0.07f), new Color(0f,   0.2f, 0.8f), urp);
            Material powerUpMat = CreateNeonMat("PowerUp",  new Color(0f,   1f,    0.5f),  new Color(0f,   3f,   1f),   urp);

            // ── Player: capsule body + sphere head + glow ────────────────────────
            UpgradePrefab("Player", go =>
            {
                ClearChildren(go);
                var body = CreatePart(go, "Body",   PrimitiveType.Capsule, new Vector3(0, 0, 0), new Vector3(0.5f, 0.7f, 0.5f), playerMat);
                var cockpit = CreatePart(go, "Cockpit", PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one * 0.35f, playerMat);
                AddGlow(go, new Color(0.1f, 0.5f, 1f), 1.5f);
            });

            // ── Grunt: sphere (simple threat) ────────────────────────────────────
            UpgradePrefab("Enemies/GruntEnemy", go =>
            {
                ClearChildren(go);
                CreatePart(go, "Body", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.7f, gruntMat);
                CreatePart(go, "Eye",  PrimitiveType.Sphere, new Vector3(0, 0.1f, 0.3f), Vector3.one * 0.2f, CreateNeonMat("GruntEye", Color.white, new Color(3f,0.5f,0f), urp));
                AddGlow(go, new Color(1f, 0.2f, 0f), 1f);
            });

            // ── Flanker: elongated diamond shape ────────────────────────────────
            UpgradePrefab("Enemies/FlankerEnemy", go =>
            {
                ClearChildren(go);
                CreatePart(go, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.5f, 0.9f), flankerMat);
                CreatePart(go, "Wing1", PrimitiveType.Cube, new Vector3( 0.5f, 0, 0), new Vector3(0.6f, 0.12f, 0.5f), flankerMat);
                CreatePart(go, "Wing2", PrimitiveType.Cube, new Vector3(-0.5f, 0, 0), new Vector3(0.6f, 0.12f, 0.5f), flankerMat);
                AddGlow(go, new Color(1f, 0.6f, 0f), 1f);
            });

            // ── Elite: armoured cube with spikes ─────────────────────────────────
            UpgradePrefab("Enemies/EliteEnemy", go =>
            {
                ClearChildren(go);
                CreatePart(go, "Body",   PrimitiveType.Cube,    Vector3.zero,          new Vector3(0.8f, 0.8f, 0.8f), eliteMat);
                CreatePart(go, "SpikeF", PrimitiveType.Cylinder, new Vector3(0, 0,  0.7f), new Vector3(0.15f, 0.4f, 0.15f), eliteMat);
                CreatePart(go, "SpikeT", PrimitiveType.Cylinder, new Vector3(0, 0.7f, 0),  new Vector3(0.15f, 0.4f, 0.15f), eliteMat);
                AddGlow(go, new Color(0.8f, 0f, 1f), 1.5f);
            });

            // ── Boss: large imposing shape ────────────────────────────────────────
            UpgradePrefab("Enemies/Boss", go =>
            {
                ClearChildren(go);
                CreatePart(go, "Core",  PrimitiveType.Cylinder, Vector3.zero,          new Vector3(1.2f, 0.6f, 1.2f), bossMat);
                CreatePart(go, "Dome",  PrimitiveType.Sphere,   new Vector3(0, 0.5f, 0), Vector3.one * 1.0f, bossMat);
                CreatePart(go, "Ring1", PrimitiveType.Cylinder, Vector3.zero,          new Vector3(1.8f, 0.08f, 1.8f), bossMat);
                AddGlow(go, new Color(1f, 0f, 0.3f), 3f);
            });

            // ── Bullets: tiny glowing sphere ─────────────────────────────────────
            UpgradeBulletPrefab("Combat/PlayerBullet", bulletMat, new Color(1f, 1f, 0.2f));
            UpgradeBulletPrefab("Combat/EnemyBullet",  gruntMat,  new Color(1f, 0.2f, 0f));

            // ── Corridor tile: dark floor with glowing edges ─────────────────────
            UpgradePrefab("Level/CorridorTile", go =>
            {
                ClearChildren(go);
                // Floor
                CreatePart(go, "Floor", PrimitiveType.Cube, Vector3.zero, new Vector3(8f, 0.05f, 12f), tileMat);
                // Left wall
                CreatePart(go, "WallL", PrimitiveType.Cube, new Vector3(-4f, 1.5f, 0), new Vector3(0.1f, 3f, 12f), wallMat);
                // Right wall
                CreatePart(go, "WallR", PrimitiveType.Cube, new Vector3( 4f, 1.5f, 0), new Vector3(0.1f, 3f, 12f), wallMat);
                // Edge strips (glowing)
                CreatePart(go, "EdgeL", PrimitiveType.Cube, new Vector3(-3.9f, 0.06f, 0), new Vector3(0.15f, 0.05f, 12f), wallMat);
                CreatePart(go, "EdgeR", PrimitiveType.Cube, new Vector3( 3.9f, 0.06f, 0), new Vector3(0.15f, 0.05f, 12f), wallMat);
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] Graphics upgraded to sci-fi neon style.");
        }

        static Material CreateNeonMat(string name, Color baseColor, Color emissiveColor, Shader shader)
        {
            if (shader == null) shader = Shader.Find("Standard");
            string path = $"Assets/_Game/Art/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path) ?? new Material(shader) { name = name };
            mat.color = baseColor;
            mat.SetColor("_EmissionColor", emissiveColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                AssetDatabase.CreateAsset(mat, path);
            else
                EditorUtility.SetDirty(mat);
            return mat;
        }

        static void UpgradePrefab(string subPath, System.Action<GameObject> modify)
        {
            string path = $"{PrefabPath}/{subPath}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            modify(scope.prefabContentsRoot);
        }

        static void UpgradeBulletPrefab(string subPath, Material mat, Color glowColor)
        {
            UpgradePrefab(subPath, go =>
            {
                ClearChildren(go);
                CreatePart(go, "Body", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.18f, mat);
                AddGlow(go, glowColor, 0.8f);
                try
                {
                    // Remove old trail first to avoid serialization issues
                    var oldTrail = go.GetComponent<TrailRenderer>();
                    if (oldTrail != null) Object.DestroyImmediate(oldTrail);
                    var trail = go.AddComponent<TrailRenderer>();
                    trail.time = 0.12f;
                    trail.startWidth = 0.08f;
                    trail.endWidth   = 0f;
                    trail.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, 1f);
                    trail.endColor   = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
                    if (mat != null) trail.material = mat;
                }
                catch (System.Exception e) { Debug.LogWarning($"[GameSetup] Trail setup: {e.Message}"); }
            });
        }

        static void ClearChildren(GameObject go)
        {
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            foreach (var r in go.GetComponents<Renderer>())
                r.sharedMaterial = null;
        }

        static GameObject CreatePart(GameObject parent, string name, PrimitiveType pType,
                                      Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(pType);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            // Remove colliders from sub-parts (parent has the real one)
            foreach (var col in go.GetComponents<Collider>())
                Object.DestroyImmediate(col);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void AddGlow(GameObject go, Color color, float intensity)
        {
            // Glow comes from emissive materials + URP bloom — no Light component needed in prefab
        }

        // ─── Create Materials ────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/6. Create Materials & Apply to Prefabs", priority = 60)]
        public static void CreateMaterials()
        {
            EnsureDir("Assets/_Game/Art/Materials");
            var shader = Shader.Find("Universal Render Pipeline/Lit");

            Material playerMat  = CreateMat("Player",   new Color(0.2f, 0.5f, 1f),    shader);
            Material gruntMat   = CreateMat("Grunt",    new Color(1f, 0.25f, 0.1f),   shader);
            Material flankerMat = CreateMat("Flanker",  new Color(1f, 0.5f, 0f),      shader);
            Material eliteMat   = CreateMat("Elite",    new Color(0.7f, 0f, 0.9f),    shader);
            Material bossMat    = CreateMat("Boss",     new Color(1f, 0f, 0.2f),      shader);
            Material bulletMat  = CreateMat("Bullet",   new Color(0.8f, 1f, 0.2f),   shader);
            Material tileMat    = CreateMat("Tile",     new Color(0.12f, 0.12f, 0.15f), shader);
            Material powerUpMat = CreateMat("PowerUp",  new Color(0f, 1f, 0.6f),      shader);

            ApplyMatToPrefab("Player",          playerMat);
            ApplyMatToPrefab("Enemies/GruntEnemy",   gruntMat);
            ApplyMatToPrefab("Enemies/FlankerEnemy", flankerMat);
            ApplyMatToPrefab("Enemies/EliteEnemy",   eliteMat);
            ApplyMatToPrefab("Enemies/Boss",         bossMat);
            ApplyMatToPrefab("Combat/PlayerBullet",  bulletMat);
            ApplyMatToPrefab("Level/CorridorTile",   tileMat);
            ApplyMatToPrefab("Combat/PowerUp",       powerUpMat);

            AssetDatabase.SaveAssets();
            Debug.Log("[GameSetup] Materials created and applied.");
        }

        static Material CreateMat(string name, Color color, Shader shader)
        {
            string path = $"Assets/_Game/Art/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { existing.color = color; EditorUtility.SetDirty(existing); return existing; }
            var mat = new Material(shader) { name = name };
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void ApplyMatToPrefab(string prefabSubPath, Material mat)
        {
            string path = $"{PrefabPath}/{prefabSubPath}.prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) return;
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var renderers = scope.prefabContentsRoot.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.sharedMaterial = mat;
        }

        // ─── Wire HUD ────────────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/7. Wire HUD in Scene", priority = 70)]
        public static void WireHUD()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[GameSetup] No Canvas in scene."); return; }

            var hud = Object.FindAnyObjectByType<HUDController>();
            if (hud == null) { Debug.LogError("[GameSetup] HUDController not found."); return; }

            // Find or create HUDPanel
            Transform hudPanel = canvas.transform.Find("HUDPanel");
            if (hudPanel == null)
            {
                var go = new GameObject("HUDPanel");
                go.transform.SetParent(canvas.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                hudPanel = go.transform;
            }

            // HP Slider
            Slider hpSlider = CreateUISlider(hudPanel, "HPBar",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10, -10), new Vector2(200, -40));
            hpSlider.maxValue = 100; hpSlider.value = 100;

            // Wave Label
            var waveGO = new GameObject("WaveLabel");
            waveGO.transform.SetParent(hudPanel, false);
            var waveTMP = waveGO.AddComponent<TMPro.TextMeshProUGUI>();
            waveTMP.text = "Wave 1/6";
            waveTMP.fontSize = 24;
            waveTMP.color = Color.white;
            var waveRT = waveGO.GetComponent<RectTransform>();
            waveRT.anchorMin = new Vector2(0.5f, 1f); waveRT.anchorMax = new Vector2(0.5f, 1f);
            waveRT.pivot = new Vector2(0.5f, 1f);
            waveRT.anchoredPosition = new Vector2(0, -10);
            waveRT.sizeDelta = new Vector2(200, 40);

            // Boss HP Group (hidden by default)
            var bossGroup = new GameObject("BossHPGroup");
            bossGroup.transform.SetParent(hudPanel, false);
            var bossRT = bossGroup.AddComponent<RectTransform>();
            bossRT.anchorMin = new Vector2(0f, 0f); bossRT.anchorMax = new Vector2(1f, 0f);
            bossRT.pivot = new Vector2(0.5f, 0f);
            bossRT.anchoredPosition = new Vector2(0, 10);
            bossRT.sizeDelta = new Vector2(-40, 30);
            Slider bossSlider = CreateUISlider(bossGroup.transform, "BossHPBar",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bossGroup.SetActive(false);

            // Wire HUDController
            var so = new SerializedObject(hud);
            so.FindProperty("playerHpSlider").objectReferenceValue = hpSlider;
            so.FindProperty("waveLabel").objectReferenceValue = waveTMP;
            so.FindProperty("bossHpGroup").objectReferenceValue = bossGroup;
            so.FindProperty("bossHpSlider").objectReferenceValue = bossSlider;
            so.ApplyModifiedProperties();

            // Wire HUDController to PlayerHealth events
            var playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
                SetField(playerHealth, "hud", hud); // only if PlayerHealth has this field

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameSetup] HUD wired successfully.");
        }

        static Slider CreateUISlider(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = name.Contains("Boss") ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 0.8f, 0.2f);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;

            slider.fillRect = fillRT;
            return slider;
        }

        // ─── Build Android APK ──────────────────────────────────────────────────
        [MenuItem("StrafAdvance/Build Android APK", priority = 5)]
        public static void BuildAndroidAPK()
        {
            // Ensure Android build target
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[GameSetup] Switching to Android build target...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Build settings
            PlayerSettings.productName        = "Strafe Advance";
            PlayerSettings.applicationIdentifier = "com.strafegame.advance";
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var scenes = new[]
            {
                "Assets/_Game/Scenes/Bootstrap.unity",
                "Assets/_Game/Scenes/GameScene.unity"
            };

            string outputPath = System.IO.Path.Combine(
                System.IO.Path.GetFullPath(".."),
                "StrafeAdvance.apk");

            var options = new BuildPlayerOptions
            {
                scenes      = scenes,
                locationPathName = outputPath,
                target      = BuildTarget.Android,
                options     = BuildOptions.None
            };

            Debug.Log($"[GameSetup] Building APK to {outputPath}...");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[GameSetup] APK build SUCCESS: {outputPath} ({summary.totalSize / 1024 / 1024} MB)");
            else
                Debug.LogError($"[GameSetup] APK build FAILED: {summary.result} — {summary.totalErrors} errors");
        }

        // ─── Play Game ───────────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/Play Game %F5", priority = 0)]
        public static void PlayGame()
        {
            if (!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();
            else
                EditorApplication.ExitPlaymode();
        }

        // ─── Fix Input System ────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/Fix Input System (Both)", priority = 1)]
        public static void FixInputSystem()
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null) { prop.intValue = 2; so.ApplyModifiedProperties(); }
            AssetDatabase.SaveAssets();
            Debug.Log("[GameSetup] activeInputHandler set to Both (2). Restart Unity to apply.");
        }

        // ─── Run All ────────────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/Run Full Setup (All Steps)", priority = 0)]
        public static void RunAll()
        {
            EnsureEnemyLayer();
            CreateScriptableObjects();
            CreatePrefabs();
            SetupGameScene();
            SetupBootstrapScene();
            Debug.Log("[GameSetup] Full setup complete.");
        }

        // ─── Step 1: Enemy Layer ─────────────────────────────────────────────────
        [MenuItem("StrafAdvance/1. Add Enemy Layer", priority = 10)]
        public static void EnsureEnemyLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            // Add "Enemy" tag
            var tags = tagManager.FindProperty("tags");
            bool hasEnemyTag = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == "Enemy") { hasEnemyTag = true; break; }
            if (!hasEnemyTag)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Enemy";
                Debug.Log("[GameSetup] Enemy tag added.");
            }

            // Add "Enemy" layer
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == "Enemy") break;
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = "Enemy";
                    Debug.Log($"[GameSetup] Enemy layer added at index {i}.");
                    break;
                }
            }
            tagManager.ApplyModifiedProperties();
            Debug.Log("[GameSetup] TagManager updated.");
        }

        // ─── Step 2: ScriptableObjects ───────────────────────────────────────────
        [MenuItem("StrafAdvance/2. Create ScriptableObject Assets", priority = 20)]
        public static void CreateScriptableObjects()
        {
            EnsureDir(SOPath);

            CreateSO<PlayerConfig>("DefaultPlayerConfig");

            CreateEnemyConfig("GruntConfig",   maxHp: 30,  contact: 10, speed: 3f, fireRate: 2f,  bullet: 8);
            CreateEnemyConfig("FlankerConfig",  maxHp: 20,  contact: 10, speed: 4f, fireRate: 0f,  bullet: 0);
            CreateEnemyConfig("EliteConfig",    maxHp: 80,  contact: 15, speed: 2f, fireRate: 0f,  bullet: 0);
            CreateEnemyConfig("BossConfig",     maxHp: 200, contact: 20, speed: 1.5f, fireRate: 3f, bullet: 15);

            // Level 1 waves
            var l1w = new[]
            {
                CreateWaveConfig("L1_Wave1", EnemyType.Grunt,   5, 1.5f),
                CreateWaveConfig("L1_Wave2", EnemyType.Grunt,   4, 1.2f),
                CreateWaveConfig("L1_Wave3", EnemyType.Grunt,   3, 1.0f),
                CreateWaveConfig("L1_Wave4", EnemyType.Flanker, 4, 1.5f),
                CreateWaveConfig("L1_Wave5", EnemyType.Flanker, 3, 1.2f),
                CreateWaveConfig("L1_Wave6", EnemyType.Elite,   2, 2.0f),
            };
            CreateLevelConfig("Level1", "Level 1", l1w, 4f,   "free",  120f);

            // Level 2 waves
            var l2w = new[]
            {
                CreateWaveConfig("L2_Wave1", EnemyType.Grunt,   6, 1.2f),
                CreateWaveConfig("L2_Wave2", EnemyType.Flanker, 5, 1.0f),
                CreateWaveConfig("L2_Wave3", EnemyType.Grunt,   5, 1.0f),
                CreateWaveConfig("L2_Wave4", EnemyType.Flanker, 4, 1.0f),
                CreateWaveConfig("L2_Wave5", EnemyType.Elite,   3, 2.0f),
                CreateWaveConfig("L2_Wave6", EnemyType.Elite,   2, 1.5f),
            };
            CreateLevelConfig("Level2", "Level 2", l2w, 4.5f, "free",  110f);

            // Level 3 waves
            var l3w = new[]
            {
                CreateWaveConfig("L3_Wave1", EnemyType.Flanker, 6, 1.0f),
                CreateWaveConfig("L3_Wave2", EnemyType.Grunt,   8, 0.8f),
                CreateWaveConfig("L3_Wave3", EnemyType.Flanker, 5, 1.0f),
                CreateWaveConfig("L3_Wave4", EnemyType.Elite,   4, 2.0f),
                CreateWaveConfig("L3_Wave5", EnemyType.Flanker, 6, 0.8f),
                CreateWaveConfig("L3_Wave6", EnemyType.Elite,   3, 1.5f),
            };
            CreateLevelConfig("Level3", "Level 3", l3w, 5f,   "free",  100f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] ScriptableObjects created.");
        }

        // ─── Step 3: Prefabs ─────────────────────────────────────────────────────
        [MenuItem("StrafAdvance/3. Create Prefabs", priority = 30)]
        public static void CreatePrefabs()
        {
            EnsureDir(PrefabPath + "/Combat");
            EnsureDir(PrefabPath + "/Enemies");
            EnsureDir(PrefabPath + "/Level");
            EnsureDir(PrefabPath + "/UI");

            CreateBulletPrefab("PlayerBullet", isEnemy: false);
            CreateBulletPrefab("EnemyBullet",  isEnemy: true);
            CreateCorridorTilePrefab();
            CreatePowerUpPrefab();
            CreatePlayerPrefab();
            CreateEnemyPrefab<GruntEnemy>("GruntEnemy",     "Enemies/GruntEnemy");
            CreateEnemyPrefab<FlankerEnemy>("FlankerEnemy", "Enemies/FlankerEnemy");
            CreateEnemyPrefab<EliteEnemy>("EliteEnemy",     "Enemies/EliteEnemy");
            CreateBossPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] Prefabs created. Assign custom meshes/materials in Inspector.");
        }

        // ─── Step 4: GameScene ───────────────────────────────────────────────────
        [MenuItem("StrafAdvance/4. Setup GameScene", priority = 40)]
        public static void SetupGameScene()
        {
            EnsureDir(ScenePath);
            string path = ScenePath + "/GameScene.unity";

            var scene = File.Exists(path)
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Clear existing roots
            foreach (var r in scene.GetRootGameObjects())
                Object.DestroyImmediate(r);

            // Singletons
            MakeGO<GameManager>("GameManager");
            MakeGO<IAPManager>("IAPManager");
            var audioGO = MakeGO<AudioManager>("AudioManager");
            var musicSrc = audioGO.AddComponent<AudioSource>();
            musicSrc.playOnAwake = false;
            SetField(audioGO.GetComponent<AudioManager>(), "musicSource", musicSrc);

            // Spawn parent
            var spawnParent = new GameObject("SpawnParent");

            // WaveSpawner
            var wsGO = MakeGO<WaveSpawner>("WaveSpawner");
            var ws   = wsGO.GetComponent<WaveSpawner>();
            SetField(ws, "spawnParent",    spawnParent.transform);
            SetField(ws, "gruntConfig",    LoadSO<EnemyConfig>("GruntConfig"));
            SetField(ws, "flankerConfig",  LoadSO<EnemyConfig>("FlankerConfig"));
            SetField(ws, "eliteConfig",    LoadSO<EnemyConfig>("EliteConfig"));
            SetField(ws, "gruntPrefab",    LoadPrefab<GruntEnemy>("Enemies/GruntEnemy"));
            SetField(ws, "flankerPrefab",  LoadPrefab<FlankerEnemy>("Enemies/FlankerEnemy"));
            SetField(ws, "elitePrefab",    LoadPrefab<EliteEnemy>("Enemies/EliteEnemy"));
            SetField(ws, "enemyBulletPrefab", LoadPrefab<Bullet>("Combat/EnemyBullet"));

            // CorridorScroller
            var csGO = MakeGO<CorridorScroller>("CorridorScroller");
            SetField(csGO.GetComponent<CorridorScroller>(), "tilePrefab", LoadPrefab<CorridorTile>("Level/CorridorTile"));

            // Player (instantiate from prefab if available, else raw GO)
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Player.prefab");
            GameObject playerGO;
            if (playerPrefab != null)
                playerGO = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            else
            {
                playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerGO.name = "Player";
            }
            playerGO.tag = "Player";
            playerGO.transform.position = Vector3.zero;
            SetField(ws, "playerTransform", playerGO.transform);

            // Camera
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags   = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            camGO.transform.SetPositionAndRotation(new Vector3(0, 2, -6), Quaternion.Euler(8, 0, 0));
            camGO.AddComponent<AudioListener>();

            // Directional Light
            var lightGO = new GameObject("Directional Light");
            var lt = lightGO.AddComponent<Light>();
            lt.type      = LightType.Directional;
            lt.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Canvas (root UI)
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler  = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();

            // UI panel placeholders
            CreateUIPanelGO(canvasGO.transform, "MainMenuPanel",       out var mainMenuPanel);
            CreateUIPanelGO(canvasGO.transform, "LevelSelectPanel",    out var levelSelectPanel);
            CreateUIPanelGO(canvasGO.transform, "HUDPanel",            out var hudPanel);
            CreateUIPanelGO(canvasGO.transform, "LevelCompletePanel",  out var levelCompletePanel);
            CreateUIPanelGO(canvasGO.transform, "GameOverPanel",       out var gameOverPanel);
            CreateUIPanelGO(canvasGO.transform, "ShopPanel",           out var shopPanel);

            // Attach controllers to canvas
            var mainMenu = canvasGO.AddComponent<MainMenuController>();
            SetField(mainMenu, "menuPanel",   mainMenuPanel);

            var levelSelectCtrl = canvasGO.AddComponent<LevelSelectController>();
            SetField(levelSelectCtrl, "panel",       levelSelectPanel);
            SetField(mainMenu, "levelSelect", levelSelectCtrl);

            // Load level configs into LevelSelectController
            var allLevels = new LevelConfig[]
            {
                LoadSO<LevelConfig>("Level1"),
                LoadSO<LevelConfig>("Level2"),
                LoadSO<LevelConfig>("Level3"),
            };
            var lcSO = new SerializedObject(levelSelectCtrl);
            var levelsProp = lcSO.FindProperty("allLevels");
            levelsProp.arraySize = allLevels.Length;
            for (int i = 0; i < allLevels.Length; i++)
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = allLevels[i];
            lcSO.ApplyModifiedProperties();

            var hudCtrl = canvasGO.AddComponent<HUDController>();
            SetField(hudCtrl, "bossHpGroup", gameOverPanel); // placeholder until proper HP bar built

            var levelCompleteCtrl = canvasGO.AddComponent<LevelCompleteController>();
            SetField(levelCompleteCtrl, "panel", levelCompletePanel);

            var gameOverCtrl = canvasGO.AddComponent<GameOverController>();
            SetField(gameOverCtrl, "panel", gameOverPanel);

            var shopCtrl = canvasGO.AddComponent<ShopController>();
            SetField(shopCtrl, "panel", shopPanel);

            // EventSystem (required for UI input)
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[GameSetup] GameScene saved to {path}. Wire Slider/TMP references in Inspector.");
        }

        // ─── Step 5: Bootstrap Scene ─────────────────────────────────────────────
        [MenuItem("StrafAdvance/5. Setup Bootstrap Scene", priority = 50)]
        public static void SetupBootstrapScene()
        {
            EnsureDir(ScenePath);
            string path = ScenePath + "/Bootstrap.unity";
            var scene = File.Exists(path)
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            foreach (var r in scene.GetRootGameObjects())
                Object.DestroyImmediate(r);

            // Bootstrap loader
            var loaderGO = new GameObject("SceneLoader");
            loaderGO.AddComponent<BootstrapLoader>();

            EditorSceneManager.SaveScene(scene, path);

            // Add both scenes to build settings
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(path, true),
                new EditorBuildSettingsScene(ScenePath + "/GameScene.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[GameSetup] Bootstrap scene saved. Build settings updated.");
        }

        // ─── Prefab helpers ─────────────────────────────────────────────────────

        static void CreateBulletPrefab(string name, bool isEnemy)
        {
            string path = $"{PrefabPath}/Combat/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = new Vector3(0.15f, 0.15f, 0.25f);
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
            var col    = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius    = 0.08f;
            col.height    = 0.25f;
            col.direction = 2;
            go.AddComponent<Bullet>();
            if (!isEnemy)
            {
                var trail          = go.AddComponent<TrailRenderer>();
                trail.time         = 0.1f;
                trail.startWidth   = 0.05f;
                trail.endWidth     = 0f;
                trail.material     = new Material(Shader.Find("Sprites/Default"));
                trail.startColor   = Color.cyan;
                trail.endColor     = new Color(0, 1, 1, 0);
            }
            go.tag = isEnemy ? "EnemyBullet" : "PlayerBullet";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateCorridorTilePrefab()
        {
            string path = $"{PrefabPath}/Level/CorridorTile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CorridorTile";
            go.transform.localScale = new Vector3(8f, 0.1f, 12f);
            go.AddComponent<CorridorTile>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreatePowerUpPrefab()
        {
            string path = $"{PrefabPath}/Combat/PowerUp.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PowerUp";
            go.transform.localScale = Vector3.one * 0.4f;
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
            var col       = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius    = 0.5f;
            go.AddComponent<PowerUp>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreatePlayerPrefab()
        {
            string path = $"{PrefabPath}/Player.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag  = "Player";

            var health     = go.AddComponent<PlayerHealth>();
            var controller = go.AddComponent<PlayerController>();
            var autoShoot  = go.AddComponent<AutoShooter>();
            var buffs      = go.AddComponent<PlayerBuffs>();

            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(go.transform);
            firePoint.transform.localPosition = new Vector3(0f, 0.5f, 0.8f);

            var config     = LoadSO<PlayerConfig>("DefaultPlayerConfig");
            var bulletPref = LoadPrefab<Bullet>("Combat/PlayerBullet");

            SetField(controller, "config", config);
            SetField(controller, "health", health);
            SetField(autoShoot,  "config", config);
            SetField(autoShoot,  "firePoint", firePoint.transform);
            SetField(autoShoot,  "bulletPrefab", bulletPref);
            SetField(buffs,      "autoShooter", autoShoot);
            SetField(buffs,      "health", health);

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateEnemyPrefab<T>(string goName, string prefabSubPath) where T : EnemyBase
        {
            string path = $"{PrefabPath}/{prefabSubPath}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name  = goName;
            go.tag   = "Enemy";
            go.layer = LayerMask.NameToLayer("Enemy");
            go.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateBossPrefab()
        {
            string path = $"{PrefabPath}/Enemies/Boss.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name  = "Boss";
            go.tag   = "Enemy";
            go.layer = LayerMask.NameToLayer("Enemy");
            go.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            go.AddComponent<BossController>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ─── Utility ────────────────────────────────────────────────────────────

        static GameObject MakeGO<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            return go;
        }

        static void SetField(Object target, string fieldName, Object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[GameSetup] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        static T LoadSO<T>(string name) where T : ScriptableObject
            => AssetDatabase.LoadAssetAtPath<T>($"{SOPath}/{name}.asset");

        static T LoadPrefab<T>(string subPath) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/{subPath}.prefab");
            return go != null ? go.GetComponent<T>() : null;
        }

        static T CreateSO<T>(string name) where T : ScriptableObject
        {
            string path = $"{SOPath}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static EnemyConfig CreateEnemyConfig(string name, int maxHp, int contact, float speed, float fireRate, int bullet)
        {
            string path = $"{SOPath}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (existing != null) return existing;
            var cfg = ScriptableObject.CreateInstance<EnemyConfig>();
            cfg.maxHp        = maxHp;
            cfg.contactDamage = contact;
            cfg.moveSpeed    = speed;
            cfg.fireRate     = fireRate;
            cfg.bulletDamage = bullet;
            AssetDatabase.CreateAsset(cfg, path);
            return cfg;
        }

        static WaveConfig CreateWaveConfig(string name, EnemyType type, int count, float interval)
        {
            string path = $"{SOPath}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (existing != null) return existing;
            var cfg = ScriptableObject.CreateInstance<WaveConfig>();
            cfg.enemyType     = type;
            cfg.count         = count;
            cfg.spawnInterval = interval;
            AssetDatabase.CreateAsset(cfg, path);
            return cfg;
        }

        static void CreateLevelConfig(string name, string levelName, WaveConfig[] waves, float speed, string iapId, float parTime)
        {
            string path = $"{SOPath}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<LevelConfig>(path) != null) return;
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            cfg.levelName       = levelName;
            cfg.waves           = waves;
            cfg.worldScrollSpeed = speed;
            cfg.iapProductId    = iapId;
            cfg.parTimeSeconds  = parTime;
            AssetDatabase.CreateAsset(cfg, path);
        }

        static void CreateUIPanelGO(Transform parent, string name, out GameObject panel)
        {
            panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt       = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image    = panel.AddComponent<Image>();
            image.color  = new Color(0, 0, 0, 0.85f);
            panel.SetActive(false);
        }

        static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts  = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
