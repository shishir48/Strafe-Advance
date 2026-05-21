using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace StrafAdvance.Editor
{
    public static class GameSetup
    {
        private const string SOPath    = "Assets/_Game/ScriptableObjects";
        private const string PrefabPath = "Assets/_Game/Prefabs";
        private const string ScenePath  = "Assets/_Game/Scenes";

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
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == "Enemy") return;
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = "Enemy";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[GameSetup] Enemy layer added at index {i}.");
                    return;
                }
            }
            Debug.LogWarning("[GameSetup] Could not add Enemy layer — no free layer slots.");
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
            camGO.transform.SetPositionAndRotation(new Vector3(0, 4, -7), Quaternion.Euler(18, 0, 0));
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
