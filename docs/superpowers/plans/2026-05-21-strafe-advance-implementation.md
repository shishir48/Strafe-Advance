# Strafe Advance — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build Strafe Advance — a third-person corridor auto-fire strafe shooter for Android using Unity URP with level-based progression and IAP monetization.

**Architecture:** Single Unity scene (GameScene) with UI panel swapping. The world scrolls toward a stationary player on a fixed Z axis; player strafes on X only. Auto-fire targets the nearest enemy in a 180° forward arc. WaveSpawner reads LevelConfig ScriptableObjects; boss fight triggers on final wave clear. IAPManager wraps Unity Purchasing; UnlockRegistry persists purchase state to PlayerPrefs.

**Tech Stack:** Unity 2022.3 LTS, Universal Render Pipeline 14.x, Unity IAP 4.x, Cinemachine 2.9, TextMeshPro 3.x, Unity Test Framework 1.3.x, Android Build Support (IL2CPP / ARM64)

---

## File Map

```
Assets/_Game/
  Scripts/
    Core/
      GameState.cs          — enum: Menu, Playing, BossFight, LevelComplete, GameOver
      IDamageable.cs        — interface: TakeDamage(int)
      IPoolable.cs          — interface: OnGetFromPool(), OnReturnToPool()
      ObjectPool.cs         — generic MonoBehaviour pool constrained to IPoolable
      GameManager.cs        — singleton state machine; broadcasts OnStateChanged
    Player/
      PlayerConfig.cs       — ScriptableObject: hp, speed, fireRate, damage, strafeLimit
      PlayerHealth.cs       — IDamageable; fires OnDeath / OnHealthChanged events
      PlayerController.cs   — drag-to-strafe input; moves player on X only
      PlayerBuffs.cs        — applies / removes timed power-up effects
      AutoShooter.cs        — fires bullets at nearest enemy every fireRate seconds
    Combat/
      Bullet.cs             — IPoolable projectile with optional homing
      DamageSystem.cs       — static helper: Calculate(base, multiplier)
      PowerUp.cs            — trigger-based temp buff spawned by EliteEnemy
    Level/
      CorridorTile.cs       — IPoolable tile; no logic beyond pool callbacks
      CorridorScroller.cs   — spawns/recycles tiles; world-scroll illusion
      WaveConfig.cs         — ScriptableObject: enemyType, count, spawnInterval
      LevelConfig.cs        — ScriptableObject: waves[], bossRef, speed, iapProductId
      WaveSpawner.cs        — reads LevelConfig; spawns waves; tracks kills; fires events
    Enemies/
      EnemyConfig.cs        — ScriptableObject: hp, speed, fireRate, damage
      EnemyBase.cs          — IDamageable; fires OnDeath; virtual Die()
      GruntEnemy.cs         — walks forward, fires at player on timer
      FlankerEnemy.cs       — curves toward player X while advancing
      EliteEnemy.cs         — slow advance, telegraphed charge, drops PowerUp on death
      BossController.cs     — extends EnemyBase; 2-phase HP with OnPhaseChanged event
    IAP/
      UnlockRegistry.cs     — plain C# class; reads/writes PlayerPrefs unlock state
      IAPManager.cs         — MonoBehaviour; wraps UnityEngine.Purchasing; calls registry
    UI/
      ScoreCalculator.cs    — static: Calculate(kills), CalculateStars(boss,noDeath,time)
      HUDController.cs      — updates HP bar, wave label, boss HP bar
      MainMenuController.cs — shows/hides main menu panel; wires Play / Shop buttons
      LevelSelectController.cs — populates grid; locks levels per UnlockRegistry
      LevelSelectItem.cs    — per-item UI: level name, lock icon, price button
      LevelCompleteController.cs — shows score, stars; wires Next / Shop buttons
      GameOverController.cs — wires Retry / Menu / Shop buttons
      ShopController.cs     — tabs: Skins / Levels / Power-ups; calls IAPManager
      ShopItemView.cs       — per-item UI: name, preview, buy / equipped button
    Audio/
      SoundID.cs            — enum: Shoot, EnemyHit, PlayerHit, Death, LevelComplete, BossRoar
      AudioManager.cs       — singleton; plays SFX from pool; streams music
  Tests/
    EditMode/
      Tests.EditMode.asmdef
      ObjectPoolTests.cs
      PlayerHealthTests.cs
      DamageSystemTests.cs
      UnlockRegistryTests.cs
      WaveSpawnerTests.cs
      BossControllerTests.cs
      ScoreCalculatorTests.cs
  Scripts/_Game.Scripts.asmdef
```

---

## Task 1: Unity Project Setup

**Files:**
- Create: `Packages/manifest.json` (add packages)
- Create: `Assets/_Game/Scripts/_Game.Scripts.asmdef`
- Create: `Assets/_Game/Tests/EditMode/Tests.EditMode.asmdef`

- [ ] **Step 1: Create Unity project**

Open Unity Hub → New Project → Template: **3D (URP)** → Name: `Strafe-Advance` → Location: `/Users/shishirsingh/` → Create.
Unity will populate `/Users/shishirsingh/Strafe-Advance/` with `Assets/`, `Packages/`, `ProjectSettings/`.

- [ ] **Step 2: Add required packages**

Open `Packages/manifest.json` and add to the `"dependencies"` block:
```json
"com.unity.purchasing": "4.8.0",
"com.unity.cinemachine": "2.9.7",
"com.unity.textmeshpro": "3.0.6"
```
Save. Unity will auto-import packages on next editor focus.

- [ ] **Step 3: Configure Android build target**

In Unity: **Edit → Project Settings → Player**
- Company Name: your studio name
- Product Name: `Strafe Advance`
- Default Orientation: Portrait
- **Other Settings:**
  - Scripting Backend: `IL2CPP`
  - Target Architectures: `ARM64` only
  - Minimum API Level: `26 (Android 8.0)`
  - Target API Level: `33`

Switch build platform: **File → Build Settings → Android → Switch Platform**

- [ ] **Step 4: Create folder structure**

In the Unity Project window, create the following folders under `Assets/`:
```
_Game/Scripts/Core
_Game/Scripts/Player
_Game/Scripts/Combat
_Game/Scripts/Level
_Game/Scripts/Enemies
_Game/Scripts/IAP
_Game/Scripts/UI
_Game/Scripts/Audio
_Game/Tests/EditMode
_Game/Prefabs/Enemies
_Game/Prefabs/Combat
_Game/Prefabs/Level
_Game/Prefabs/UI
_Game/ScriptableObjects
_Game/Scenes
_Game/Art/Materials
_Game/Art/VFX
_Game/Audio/Music
_Game/Audio/SFX
```

- [ ] **Step 5: Create main scripts assembly definition**

Create `Assets/_Game/Scripts/_Game.Scripts.asmdef`:
```json
{
    "name": "_Game.Scripts",
    "rootNamespace": "StrafAdvance",
    "references": ["com.unity.purchasing"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Create test assembly definition**

Create `Assets/_Game/Tests/EditMode/Tests.EditMode.asmdef`:
```json
{
    "name": "Tests.EditMode",
    "rootNamespace": "StrafAdvance.Tests",
    "references": [
        "_Game.Scripts",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 7: Create Bootstrap and GameScene**

In Unity, create two scenes in `Assets/_Game/Scenes/`:
- `Bootstrap.unity`
- `GameScene.unity`

Add both to **File → Build Settings** scene list (Bootstrap index 0, GameScene index 1).

- [ ] **Step 8: Configure URP for mobile**

In `Assets/Settings/`, find the `UniversalRenderPipelineAsset`. Set:
- Rendering Path: Forward
- HDR: Off
- MSAA: 2x
- Shadow Distance: 30
- Cascade Count: 1

- [ ] **Step 9: Commit**

```bash
cd /Users/shishirsingh/Strafe-Advance
git add Assets/ Packages/manifest.json ProjectSettings/
git commit -m "feat: Unity project setup with URP, Android target, and package references"
```

---

## Task 2: Core Interfaces & Game Manager

**Files:**
- Create: `Assets/_Game/Scripts/Core/GameState.cs`
- Create: `Assets/_Game/Scripts/Core/IDamageable.cs`
- Create: `Assets/_Game/Scripts/Core/IPoolable.cs`
- Create: `Assets/_Game/Scripts/Core/GameManager.cs`

- [ ] **Step 1: Create GameState enum**

`Assets/_Game/Scripts/Core/GameState.cs`:
```csharp
namespace StrafAdvance
{
    public enum GameState
    {
        Menu,
        Playing,
        BossFight,
        LevelComplete,
        GameOver
    }
}
```

- [ ] **Step 2: Create IDamageable interface**

`Assets/_Game/Scripts/Core/IDamageable.cs`:
```csharp
namespace StrafAdvance
{
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}
```

- [ ] **Step 3: Create IPoolable interface**

`Assets/_Game/Scripts/Core/IPoolable.cs`:
```csharp
namespace StrafAdvance
{
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }
}
```

- [ ] **Step 4: Create GameManager**

`Assets/_Game/Scripts/Core/GameManager.cs`:
```csharp
using System;
using UnityEngine;

namespace StrafAdvance
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;

        public event Action<GameState> OnStateChanged;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
```

- [ ] **Step 5: Verify compilation**

In Unity, check Console for errors. No errors expected.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Core/
git commit -m "feat: core interfaces, GameState enum, and GameManager singleton"
```

---

## Task 3: ObjectPool

**Files:**
- Create: `Assets/_Game/Scripts/Core/ObjectPool.cs`
- Create: `Assets/_Game/Tests/EditMode/ObjectPoolTests.cs`

- [ ] **Step 1: Write failing test**

`Assets/_Game/Tests/EditMode/ObjectPoolTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class TestPoolable : MonoBehaviour, IPoolable
    {
        public bool WasActivated { get; private set; }
        public bool WasReturned { get; private set; }
        public void OnGetFromPool() => WasActivated = true;
        public void OnReturnToPool() => WasReturned = true;
    }

    public class ObjectPoolTests
    {
        private GameObject _prefabGo;
        private TestPoolable _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefabGo = new GameObject("Prefab");
            _prefab = _prefabGo.AddComponent<TestPoolable>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_prefabGo);

        [Test]
        public void Get_ReturnsActiveObject()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 2);
            var obj = pool.Get();
            Assert.IsTrue(obj.gameObject.activeSelf);
        }

        [Test]
        public void Get_CallsOnGetFromPool()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 2);
            var obj = pool.Get();
            Assert.IsTrue(obj.WasActivated);
        }

        [Test]
        public void Return_DeactivatesObject()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 2);
            var obj = pool.Get();
            pool.Return(obj);
            Assert.IsFalse(obj.gameObject.activeSelf);
        }

        [Test]
        public void Get_AfterReturn_ReusesSameObject()
        {
            var pool = new ObjectPool<TestPoolable>(_prefab, 1);
            var first = pool.Get();
            pool.Return(first);
            var second = pool.Get();
            Assert.AreEqual(first, second);
        }
    }
}
```

- [ ] **Step 2: Run test — verify FAIL**

Open **Window → General → Test Runner** in Unity. Run `ObjectPoolTests`. Expected: compile error — `ObjectPool<T>` not found.

- [ ] **Step 3: Implement ObjectPool**

`Assets/_Game/Scripts/Core/ObjectPool.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class ObjectPool<T> where T : MonoBehaviour, IPoolable
    {
        private readonly T _prefab;
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly Transform _parent;

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialSize; i++)
            {
                T obj = Object.Instantiate(_prefab, _parent);
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj = _pool.Count > 0 ? _pool.Dequeue() : Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(true);
            obj.OnGetFromPool();
            return obj;
        }

        public void Return(T obj)
        {
            obj.OnReturnToPool();
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
```

- [ ] **Step 4: Run tests — verify PASS**

Run `ObjectPoolTests` in Test Runner. All 4 tests must pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Core/ObjectPool.cs Assets/_Game/Tests/
git commit -m "feat: generic ObjectPool with IPoolable constraint"
```

---

## Task 4: Player Config & Health

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerConfig.cs`
- Create: `Assets/_Game/Scripts/Player/PlayerHealth.cs`
- Create: `Assets/_Game/Tests/EditMode/PlayerHealthTests.cs`

- [ ] **Step 1: Create PlayerConfig ScriptableObject**

`Assets/_Game/Scripts/Player/PlayerConfig.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "StrafAdvance/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        public int maxHp = 100;
        public float strafeSpeed = 8f;
        public float strafeLimit = 3f;
        public float fireRate = 0.3f;
        public int bulletDamage = 10;
        public float homingStrength = 5f;
    }
}
```

- [ ] **Step 2: Write failing test**

`Assets/_Game/Tests/EditMode/PlayerHealthTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class PlayerHealthTests
    {
        private PlayerHealth _health;
        private PlayerConfig _config;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _health = _go.AddComponent<PlayerHealth>();
            _config = ScriptableObject.CreateInstance<PlayerConfig>();
            _config.maxHp = 100;
            _health.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsCurrentHpToMax()
        {
            Assert.AreEqual(100, _health.CurrentHp);
            Assert.AreEqual(100, _health.MaxHp);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _health.TakeDamage(30);
            Assert.AreEqual(70, _health.CurrentHp);
        }

        [Test]
        public void TakeDamage_ClampsAtZero()
        {
            _health.TakeDamage(999);
            Assert.AreEqual(0, _health.CurrentHp);
        }

        [Test]
        public void TakeDamage_FiresDeathEvent_WhenHpReachesZero()
        {
            bool fired = false;
            _health.OnDeath += () => fired = true;
            _health.TakeDamage(100);
            Assert.IsTrue(fired);
        }

        [Test]
        public void TakeDamage_DoesNotFireDeathEvent_WhenHpAboveZero()
        {
            bool fired = false;
            _health.OnDeath += () => fired = true;
            _health.TakeDamage(50);
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnHealthChanged_Fires_WithCorrectValues()
        {
            int reportedCurrent = -1, reportedMax = -1;
            _health.OnHealthChanged += (cur, max) => { reportedCurrent = cur; reportedMax = max; };
            _health.TakeDamage(25);
            Assert.AreEqual(75, reportedCurrent);
            Assert.AreEqual(100, reportedMax);
        }
    }
}
```

- [ ] **Step 3: Run test — verify FAIL**

Run `PlayerHealthTests` in Test Runner. Expected: compile error — `PlayerHealth` not found.

- [ ] **Step 4: Implement PlayerHealth**

`Assets/_Game/Scripts/Player/PlayerHealth.cs`:
```csharp
using System;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }

        public event Action OnDeath;
        public event Action<int, int> OnHealthChanged;

        public void Initialize(PlayerConfig config)
        {
            MaxHp = config.maxHp;
            CurrentHp = MaxHp;
        }

        private bool _invincible;

        public void TakeDamage(int amount)
        {
            if (CurrentHp <= 0 || _invincible) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp == 0)
                OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void SetInvincible(bool on) => _invincible = on;
    }
}
```

- [ ] **Step 5: Run tests — verify PASS**

Run `PlayerHealthTests`. All 6 tests must pass.

- [ ] **Step 6: Create PlayerConfig asset**

In Unity Project window: right-click `Assets/_Game/ScriptableObjects` → **Create → StrafAdvance → PlayerConfig**. Name it `DefaultPlayerConfig`. Leave default values.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Scripts/Player/ Assets/_Game/Tests/EditMode/PlayerHealthTests.cs Assets/_Game/ScriptableObjects/
git commit -m "feat: PlayerConfig ScriptableObject and PlayerHealth with damage/death events"
```

---

## Task 5: Player Controller & Buffs

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerController.cs`
- Create: `Assets/_Game/Scripts/Player/PlayerBuffs.cs`

- [ ] **Step 1: Implement PlayerController**

`Assets/_Game/Scripts/Player/PlayerController.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerHealth health;

        private float _targetX;
        private bool _isDragging;
        private float _dragStartX;
        private float _playerStartX;

        void Start() => health.Initialize(config);

        void Update()
        {
            HandleInput();
            float newX = Mathf.MoveTowards(
                transform.position.x, _targetX,
                config.strafeSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }

        void HandleInput()
        {
#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _isDragging = true;
                _dragStartX = touch.position.x;
                _playerStartX = transform.position.x;
            }
            else if (touch.phase == TouchPhase.Moved && _isDragging)
                UpdateTargetX(touch.position.x);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                _isDragging = false;
        }

        void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _dragStartX = Input.mousePosition.x;
                _playerStartX = transform.position.x;
            }
            else if (Input.GetMouseButton(0) && _isDragging)
                UpdateTargetX(Input.mousePosition.x);
            else if (Input.GetMouseButtonUp(0))
                _isDragging = false;
        }

        void UpdateTargetX(float screenX)
        {
            float delta = (screenX - _dragStartX) / Screen.width * config.strafeLimit * 2f;
            _targetX = Mathf.Clamp(_playerStartX + delta, -config.strafeLimit, config.strafeLimit);
        }
    }
}
```

- [ ] **Step 2: Create PowerUpType enum (needed by PlayerBuffs)**

Add to the top of `Assets/_Game/Scripts/Combat/PowerUp.cs` (create the file):
```csharp
namespace StrafAdvance
{
    public enum PowerUpType { RapidFire, Shield, Multishot }
}
```

- [ ] **Step 3: Implement PlayerBuffs**

`Assets/_Game/Scripts/Player/PlayerBuffs.cs`:
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class PlayerBuffs : MonoBehaviour
    {
        [SerializeField] private AutoShooter autoShooter;
        [SerializeField] private PlayerHealth health;

        private readonly Dictionary<PowerUpType, Coroutine> _active = new Dictionary<PowerUpType, Coroutine>();

        public void ApplyBuff(PowerUpType type, float duration)
        {
            if (_active.TryGetValue(type, out Coroutine existing))
                StopCoroutine(existing);
            _active[type] = StartCoroutine(BuffRoutine(type, duration));
        }

        IEnumerator BuffRoutine(PowerUpType type, float duration)
        {
            SetEffect(type, true);
            yield return new WaitForSeconds(duration);
            SetEffect(type, false);
            _active.Remove(type);
        }

        void SetEffect(PowerUpType type, bool on)
        {
            switch (type)
            {
                case PowerUpType.RapidFire:
                    autoShooter.SetFireRateMultiplier(on ? 0.4f : 1f);
                    break;
                case PowerUpType.Shield:
                    health.SetInvincible(on);
                    break;
                case PowerUpType.Multishot:
                    autoShooter.SetMultishot(on);
                    break;
            }
        }
    }
}
```

- [ ] **Step 4: Verify compilation**

Check Unity Console. `AutoShooter.SetFireRateMultiplier`, `PlayerHealth.SetInvincible`, and `AutoShooter.SetMultishot` will be added in later tasks — ignore "missing member" errors until then.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Player/PlayerController.cs Assets/_Game/Scripts/Player/PlayerBuffs.cs Assets/_Game/Scripts/Combat/PowerUp.cs
git commit -m "feat: PlayerController drag-to-strafe input and PlayerBuffs power-up system"
```

---

## Task 6: Bullet & Damage System

**Files:**
- Create: `Assets/_Game/Scripts/Combat/Bullet.cs` (expand the PowerUp.cs file already has enum)
- Create: `Assets/_Game/Scripts/Combat/DamageSystem.cs`
- Create: `Assets/_Game/Tests/EditMode/DamageSystemTests.cs`

- [ ] **Step 1: Write failing test**

`Assets/_Game/Tests/EditMode/DamageSystemTests.cs`:
```csharp
using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class DamageSystemTests
    {
        [Test]
        public void Calculate_ReturnsBaseDamage_WithDefaultMultiplier()
        {
            Assert.AreEqual(10, DamageSystem.Calculate(10));
        }

        [Test]
        public void Calculate_AppliesMultiplier()
        {
            Assert.AreEqual(20, DamageSystem.Calculate(10, 2f));
        }

        [Test]
        public void Calculate_RoundsToNearestInt()
        {
            Assert.AreEqual(15, DamageSystem.Calculate(10, 1.5f));
        }
    }
}
```

- [ ] **Step 2: Run test — verify FAIL**

Run `DamageSystemTests`. Expected: compile error.

- [ ] **Step 3: Implement DamageSystem**

`Assets/_Game/Scripts/Combat/DamageSystem.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public static class DamageSystem
    {
        public static int Calculate(int baseDamage, float multiplier = 1f)
            => Mathf.RoundToInt(baseDamage * multiplier);
    }
}
```

- [ ] **Step 4: Implement Bullet**

`Assets/_Game/Scripts/Combat/Bullet.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class Bullet : MonoBehaviour, IPoolable
    {
        private int _damage;
        private float _speed = 18f;
        private float _homingStrength;
        private Transform _target;
        private ObjectPool<Bullet> _pool;
        private bool _isPlayerBullet;

        public void Setup(Transform target, int damage, float homingStrength,
                          ObjectPool<Bullet> pool, bool isPlayerBullet)
        {
            _target = target;
            _damage = damage;
            _homingStrength = homingStrength;
            _pool = pool;
            _isPlayerBullet = isPlayerBullet;
        }

        void Update()
        {
            if (_target != null && _homingStrength > 0f)
            {
                Vector3 dir = (_target.position - transform.position).normalized;
                transform.forward = Vector3.RotateTowards(
                    transform.forward, dir,
                    _homingStrength * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }
            transform.Translate(Vector3.forward * _speed * Time.deltaTime);

            if (transform.position.z > 60f || transform.position.z < -10f)
                _pool?.Return(this);
        }

        void OnTriggerEnter(Collider other)
        {
            string targetTag = _isPlayerBullet ? "Enemy" : "Player";
            if (!other.CompareTag(targetTag)) return;
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                _pool?.Return(this);
            }
        }

        public void OnGetFromPool() { }
        public void OnReturnToPool() { _target = null; }
    }
}
```

- [ ] **Step 5: Run tests — verify PASS**

Run `DamageSystemTests`. All 3 tests must pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Combat/Bullet.cs Assets/_Game/Scripts/Combat/DamageSystem.cs Assets/_Game/Tests/EditMode/DamageSystemTests.cs
git commit -m "feat: Bullet poolable projectile and DamageSystem static calculator"
```

---

## Task 7: AutoShooter

**Files:**
- Create: `Assets/_Game/Scripts/Player/AutoShooter.cs`

- [ ] **Step 1: Implement AutoShooter**

`Assets/_Game/Scripts/Player/AutoShooter.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class AutoShooter : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Bullet bulletPrefab;

        private ObjectPool<Bullet> _pool;
        private float _fireTimer;
        private float _fireRateMultiplier = 1f;
        private bool _multishot;

        void Start() => _pool = new ObjectPool<Bullet>(bulletPrefab, 20, transform);

        void Update()
        {
            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = config.fireRate * _fireRateMultiplier;
                Fire();
            }
        }

        void Fire()
        {
            Transform target = FindNearestEnemy();
            SpawnBullet(firePoint.position, firePoint.rotation, target);

            if (_multishot)
            {
                SpawnBullet(
                    firePoint.position + firePoint.right * 0.3f,
                    Quaternion.Euler(0, 10f, 0) * firePoint.rotation, target);
                SpawnBullet(
                    firePoint.position - firePoint.right * 0.3f,
                    Quaternion.Euler(0, -10f, 0) * firePoint.rotation, target);
            }
        }

        void SpawnBullet(Vector3 pos, Quaternion rot, Transform target)
        {
            Bullet b = _pool.Get();
            b.transform.SetPositionAndRotation(pos, rot);
            b.Setup(target, config.bulletDamage, config.homingStrength, _pool, isPlayerBullet: true);
        }

        Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 50f,
                LayerMask.GetMask("Enemy"));
            Transform nearest = null;
            float minSqDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                Vector3 dir = hit.transform.position - transform.position;
                if (dir.z <= 0f) continue;
                if (Vector3.Angle(transform.forward, dir) > 90f) continue;
                float sqDist = dir.sqrMagnitude;
                if (sqDist < minSqDist) { minSqDist = sqDist; nearest = hit.transform; }
            }
            return nearest;
        }

        public void SetFireRateMultiplier(float multiplier) => _fireRateMultiplier = multiplier;
        public void SetMultishot(bool enabled) => _multishot = enabled;
    }
}
```

- [ ] **Step 2: Set up Enemy layer in Unity**

**Edit → Project Settings → Tags and Layers** → add layer `Enemy` (e.g., layer 6).
Tag `Player` already exists by default. Add tag `Enemy` for enemy GameObjects.

- [ ] **Step 3: Verify compilation**

Check Unity Console. No errors expected.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/Player/AutoShooter.cs
git commit -m "feat: AutoShooter with nearest-enemy targeting, multishot, and fire-rate buff support"
```

---

## Task 8: Corridor Scroller

**Files:**
- Create: `Assets/_Game/Scripts/Level/CorridorTile.cs`
- Create: `Assets/_Game/Scripts/Level/CorridorScroller.cs`

- [ ] **Step 1: Implement CorridorTile**

`Assets/_Game/Scripts/Level/CorridorTile.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class CorridorTile : MonoBehaviour, IPoolable
    {
        public void OnGetFromPool() { }
        public void OnReturnToPool() { }
    }
}
```

- [ ] **Step 2: Implement CorridorScroller**

`Assets/_Game/Scripts/Level/CorridorScroller.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class CorridorScroller : MonoBehaviour
    {
        [SerializeField] private CorridorTile tilePrefab;
        [SerializeField] private int visibleTileCount = 5;
        [SerializeField] private float tileLength = 12f;

        private ObjectPool<CorridorTile> _pool;
        private readonly Queue<CorridorTile> _active = new Queue<CorridorTile>();
        private float _speed;
        private float _nextTileZ;

        public void Initialize(float scrollSpeed)
        {
            _speed = scrollSpeed;
            _pool = new ObjectPool<CorridorTile>(tilePrefab, visibleTileCount + 2);
            for (int i = 0; i < visibleTileCount; i++)
                SpawnTile();
        }

        void Update()
        {
            if (_speed <= 0f) return;
            float move = _speed * Time.deltaTime;
            foreach (CorridorTile tile in _active)
                tile.transform.Translate(Vector3.back * move);

            if (_active.Peek().transform.position.z < -tileLength)
            {
                _pool.Return(_active.Dequeue());
                SpawnTile();
            }
        }

        void SpawnTile()
        {
            CorridorTile tile = _pool.Get();
            tile.transform.position = new Vector3(0f, 0f, _nextTileZ);
            _active.Enqueue(tile);
            _nextTileZ += tileLength;
        }

        public void SetSpeed(float speed) => _speed = speed;
        public void Stop() => _speed = 0f;
    }
}
```

- [ ] **Step 3: Create corridor tile prefab**

In Unity, create a Cube: name it `CorridorTile`, scale to `(7, 0.1, 12)` (wide flat floor). Add `CorridorTile` component. Drag to `Assets/_Game/Prefabs/Level/CorridorTile.prefab`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Game/Scripts/Level/CorridorTile.cs Assets/_Game/Scripts/Level/CorridorScroller.cs Assets/_Game/Prefabs/Level/
git commit -m "feat: CorridorTile and CorridorScroller for infinite scroll illusion"
```

---

## Task 9: Enemy Config & Base

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/EnemyConfig.cs`
- Create: `Assets/_Game/Scripts/Enemies/EnemyBase.cs`
- Create: `Assets/_Game/Tests/EditMode/EnemyBaseTests.cs` (via PlayerHealthTests pattern)

- [ ] **Step 1: Create EnemyConfig ScriptableObject**

`Assets/_Game/Scripts/Enemies/EnemyConfig.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "StrafAdvance/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public int maxHp = 30;
        public int contactDamage = 10;
        public float moveSpeed = 3f;
        public float fireRate = 2f;
        public int bulletDamage = 8;
    }
}
```

- [ ] **Step 2: Write failing test**

`Assets/_Game/Tests/EditMode/EnemyBaseTests.cs`:
```csharp
using System;
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class ConcreteEnemy : EnemyBase { }

    public class EnemyBaseTests
    {
        private GameObject _go;
        private ConcreteEnemy _enemy;
        private EnemyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _enemy = _go.AddComponent<ConcreteEnemy>();
            _config = ScriptableObject.CreateInstance<EnemyConfig>();
            _config.maxHp = 50;
            _enemy.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsCurrentHp()
        {
            Assert.AreEqual(50, _enemy.CurrentHp);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _enemy.TakeDamage(20);
            Assert.AreEqual(30, _enemy.CurrentHp);
        }

        [Test]
        public void TakeDamage_FiresOnDeath_AtZeroHp()
        {
            bool fired = false;
            _enemy.OnDeath += _ => fired = true;
            _enemy.TakeDamage(50);
            Assert.IsTrue(fired);
        }

        [Test]
        public void TakeDamage_PassesSelfToDeathEvent()
        {
            EnemyBase reported = null;
            _enemy.OnDeath += e => reported = e;
            _enemy.TakeDamage(50);
            Assert.AreEqual(_enemy, reported);
        }
    }
}
```

- [ ] **Step 3: Run test — verify FAIL**

Run `EnemyBaseTests`. Expected: compile error.

- [ ] **Step 4: Implement EnemyBase**

`Assets/_Game/Scripts/Enemies/EnemyBase.cs`:
```csharp
using System;
using UnityEngine;

namespace StrafAdvance
{
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        protected EnemyConfig Config { get; private set; }
        public int CurrentHp { get; private set; }

        public event Action<EnemyBase> OnDeath;

        public void Initialize(EnemyConfig config)
        {
            Config = config;
            CurrentHp = config.maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (CurrentHp <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnDamageTaken();
            if (CurrentHp == 0)
            {
                OnDeath?.Invoke(this);
                Die();
            }
        }

        protected virtual void OnDamageTaken() { }
        protected virtual void Die() => Destroy(gameObject);
    }
}
```

- [ ] **Step 5: Run tests — verify PASS**

Run `EnemyBaseTests`. All 4 tests must pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Enemies/EnemyConfig.cs Assets/_Game/Scripts/Enemies/EnemyBase.cs Assets/_Game/Tests/EditMode/EnemyBaseTests.cs
git commit -m "feat: EnemyConfig ScriptableObject and EnemyBase with IDamageable and OnDeath event"
```

---

## Task 10: Grunt, Flanker, Elite & PowerUp

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/GruntEnemy.cs`
- Create: `Assets/_Game/Scripts/Enemies/FlankerEnemy.cs`
- Create: `Assets/_Game/Scripts/Enemies/EliteEnemy.cs`
- Expand: `Assets/_Game/Scripts/Combat/PowerUp.cs`

- [ ] **Step 1: Implement GruntEnemy**

`Assets/_Game/Scripts/Enemies/GruntEnemy.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class GruntEnemy : EnemyBase
    {
        [SerializeField] private Bullet bulletPrefab;

        private Transform _player;
        private ObjectPool<Bullet> _bulletPool;
        private float _fireTimer;

        public void InitGrunt(Transform player, ObjectPool<Bullet> sharedBulletPool)
        {
            _player = player;
            _bulletPool = sharedBulletPool;
            _fireTimer = Config.fireRate;
        }

        void Update()
        {
            transform.Translate(Vector3.back * Config.moveSpeed * Time.deltaTime);

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = Config.fireRate;
                FireAtPlayer();
            }

            if (transform.position.z < -8f)
                Destroy(gameObject);
        }

        void FireAtPlayer()
        {
            if (_player == null || _bulletPool == null) return;
            Bullet b = _bulletPool.Get();
            b.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
            b.Setup(_player, Config.bulletDamage, 0f, _bulletPool, isPlayerBullet: false);
        }
    }
}
```

- [ ] **Step 2: Implement FlankerEnemy**

`Assets/_Game/Scripts/Enemies/FlankerEnemy.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class FlankerEnemy : EnemyBase
    {
        private Transform _player;
        private const float CurveStrength = 2.5f;

        public void InitFlanker(Transform player) => _player = player;

        void Update()
        {
            Vector3 forwardMove = Vector3.back * Config.moveSpeed;
            Vector3 curve = Vector3.zero;

            if (_player != null)
            {
                float xDiff = _player.position.x - transform.position.x;
                curve = Vector3.right * (Mathf.Sign(xDiff) * CurveStrength);
            }

            transform.Translate((forwardMove + curve).normalized * Config.moveSpeed * Time.deltaTime,
                Space.World);

            if (transform.position.z < -8f)
                Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 3: Implement EliteEnemy**

`Assets/_Game/Scripts/Enemies/EliteEnemy.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class EliteEnemy : EnemyBase
    {
        [SerializeField] private PowerUp powerUpPrefab;

        private Transform _player;
        private float _chargeTimer = 3f;
        private bool _charging;
        private Vector3 _chargeDir;

        public void InitElite(Transform player) => _player = player;

        void Update()
        {
            if (_charging)
            {
                transform.Translate(_chargeDir * 9f * Time.deltaTime, Space.World);
                if (transform.position.z < -8f) Destroy(gameObject);
            }
            else
            {
                transform.Translate(Vector3.back * Config.moveSpeed * 0.4f * Time.deltaTime);
                _chargeTimer -= Time.deltaTime;
                if (_chargeTimer <= 0f)
                {
                    _charging = true;
                    _chargeDir = _player != null
                        ? (_player.position - transform.position).normalized
                        : Vector3.back;
                }
            }
        }

        protected override void Die()
        {
            if (powerUpPrefab != null)
                Instantiate(powerUpPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            base.Die();
        }
    }
}
```

- [ ] **Step 4: Complete PowerUp script**

Replace contents of `Assets/_Game/Scripts/Combat/PowerUp.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public enum PowerUpType { RapidFire, Shield, Multishot }

    public class PowerUp : MonoBehaviour
    {
        [SerializeField] private PowerUpType type;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float moveSpeed = 4f;

        void Update() => transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (other.TryGetComponent<PlayerBuffs>(out var buffs))
                buffs.ApplyBuff(type, duration);
            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 5: Verify compilation**

Check Unity Console. No errors expected.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/Enemies/ Assets/_Game/Scripts/Combat/PowerUp.cs
git commit -m "feat: Grunt, Flanker, Elite enemy variants and PowerUp drop"
```

---

## Task 11: Wave & Level System

**Files:**
- Create: `Assets/_Game/Scripts/Level/WaveConfig.cs`
- Create: `Assets/_Game/Scripts/Level/LevelConfig.cs`
- Create: `Assets/_Game/Scripts/Level/WaveSpawner.cs`
- Create: `Assets/_Game/Tests/EditMode/WaveSpawnerTests.cs`

- [ ] **Step 1: Create WaveConfig ScriptableObject**

`Assets/_Game/Scripts/Level/WaveConfig.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public enum EnemyType { Grunt, Flanker, Elite }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "StrafAdvance/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public EnemyType enemyType;
        public int count = 5;
        public float spawnInterval = 1.5f;
    }
}
```

- [ ] **Step 2: Create LevelConfig ScriptableObject**

`Assets/_Game/Scripts/Level/LevelConfig.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "StrafAdvance/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public string levelName = "Level 1";
        public WaveConfig[] waves;
        public GameObject bossPrefab;
        public float worldScrollSpeed = 4f;
        public string iapProductId = "free";
        public float parTimeSeconds = 120f;
    }
}
```

- [ ] **Step 3: Write failing test**

`Assets/_Game/Tests/EditMode/WaveSpawnerTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class WaveSpawnerTests
    {
        private WaveSpawner _spawner;
        private LevelConfig _level;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _spawner = _go.AddComponent<WaveSpawner>();

            WaveConfig w1 = ScriptableObject.CreateInstance<WaveConfig>();
            w1.count = 3; w1.spawnInterval = 0f;
            WaveConfig w2 = ScriptableObject.CreateInstance<WaveConfig>();
            w2.count = 2; w2.spawnInterval = 0f;

            _level = ScriptableObject.CreateInstance<LevelConfig>();
            _level.waves = new[] { w1, w2 };
            _spawner.LoadLevel(_level);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void LoadLevel_SetsWaveCountToZero()
        {
            Assert.AreEqual(0, _spawner.CurrentWaveIndex);
        }

        [Test]
        public void ReportKill_AdvancesWave_WhenAllEnemiesDead()
        {
            _spawner.ReportEnemySpawned(3);
            for (int i = 0; i < 3; i++) _spawner.ReportKill();
            Assert.AreEqual(1, _spawner.CurrentWaveIndex);
        }

        [Test]
        public void ReportKill_FiresAllWavesComplete_AfterLastWave()
        {
            bool fired = false;
            _spawner.OnAllWavesComplete += () => fired = true;
            _spawner.ReportEnemySpawned(3);
            for (int i = 0; i < 3; i++) _spawner.ReportKill();
            _spawner.ReportEnemySpawned(2);
            for (int i = 0; i < 2; i++) _spawner.ReportKill();
            Assert.IsTrue(fired);
        }
    }
}
```

- [ ] **Step 4: Run test — verify FAIL**

Run `WaveSpawnerTests`. Expected: compile error.

- [ ] **Step 5: Implement WaveSpawner**

`Assets/_Game/Scripts/Level/WaveSpawner.cs`:
```csharp
using System;
using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private EnemyConfig gruntConfig;
        [SerializeField] private EnemyConfig flankerConfig;
        [SerializeField] private EnemyConfig eliteConfig;
        [SerializeField] private GruntEnemy gruntPrefab;
        [SerializeField] private FlankerEnemy flankerPrefab;
        [SerializeField] private EliteEnemy elitePrefab;
        [SerializeField] private Bullet enemyBulletPrefab;

        private ObjectPool<Bullet> _enemyBulletPool;
        private LevelConfig _level;
        private int _enemiesAlive;
        private bool _spawning;

        void Awake() => _enemyBulletPool = new ObjectPool<Bullet>(enemyBulletPrefab, 30, spawnParent);

        public int CurrentWaveIndex { get; private set; }
        public event Action OnAllWavesComplete;
        public event Action<int> OnWaveStarted;

        public void LoadLevel(LevelConfig level)
        {
            _level = level;
            CurrentWaveIndex = 0;
            _enemiesAlive = 0;
        }

        public void StartSpawning() => StartCoroutine(SpawnWave(CurrentWaveIndex));

        IEnumerator SpawnWave(int index)
        {
            _spawning = true;
            WaveConfig wave = _level.waves[index];
            OnWaveStarted?.Invoke(index);

            for (int i = 0; i < wave.count; i++)
            {
                SpawnEnemy(wave.enemyType);
                ReportEnemySpawned(1);
                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }
            _spawning = false;
        }

        void SpawnEnemy(EnemyType type)
        {
            float spawnX = UnityEngine.Random.Range(-2.5f, 2.5f);
            Vector3 spawnPos = new Vector3(spawnX, 0f, 25f);

            switch (type)
            {
                case EnemyType.Grunt:
                    GruntEnemy grunt = Instantiate(gruntPrefab, spawnPos, Quaternion.identity, spawnParent);
                    grunt.Initialize(gruntConfig);
                    grunt.InitGrunt(playerTransform, _enemyBulletPool);
                    grunt.OnDeath += _ => ReportKill();
                    break;
                case EnemyType.Flanker:
                    FlankerEnemy flanker = Instantiate(flankerPrefab, spawnPos, Quaternion.identity, spawnParent);
                    flanker.Initialize(flankerConfig);
                    flanker.InitFlanker(playerTransform);
                    flanker.OnDeath += _ => ReportKill();
                    break;
                case EnemyType.Elite:
                    EliteEnemy elite = Instantiate(elitePrefab, spawnPos, Quaternion.identity, spawnParent);
                    elite.Initialize(eliteConfig);
                    elite.InitElite(playerTransform);
                    elite.OnDeath += _ => ReportKill();
                    break;
            }
        }

        public void ReportEnemySpawned(int count) => _enemiesAlive += count;

        public void ReportKill()
        {
            _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
            if (_enemiesAlive > 0 || _spawning) return;

            CurrentWaveIndex++;
            if (CurrentWaveIndex >= _level.waves.Length)
                OnAllWavesComplete?.Invoke();
            else
                StartCoroutine(SpawnWave(CurrentWaveIndex));
        }
    }
}
```

- [ ] **Step 6: Run tests — verify PASS**

Run `WaveSpawnerTests`. All 3 tests must pass.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Game/Scripts/Level/ Assets/_Game/Tests/EditMode/WaveSpawnerTests.cs
git commit -m "feat: WaveConfig/LevelConfig ScriptableObjects and WaveSpawner with kill tracking"
```

---

## Task 12: Boss Controller

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/BossController.cs`
- Create: `Assets/_Game/Tests/EditMode/BossControllerTests.cs`

- [ ] **Step 1: Write failing test**

`Assets/_Game/Tests/EditMode/BossControllerTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class BossControllerTests
    {
        private GameObject _go;
        private BossController _boss;
        private EnemyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
            _boss = _go.AddComponent<BossController>();
            _config = ScriptableObject.CreateInstance<EnemyConfig>();
            _config.maxHp = 200;
            _boss.Initialize(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void StartsInPhase1()
        {
            Assert.AreEqual(1, _boss.Phase);
        }

        [Test]
        public void TransitionsToPhase2_AtHalfHp()
        {
            _boss.TakeDamage(100);
            Assert.AreEqual(2, _boss.Phase);
        }

        [Test]
        public void DoesNotTransitionToPhase2_AboveHalfHp()
        {
            _boss.TakeDamage(99);
            Assert.AreEqual(1, _boss.Phase);
        }

        [Test]
        public void OnPhaseChanged_FiresWithPhase2()
        {
            int reportedPhase = 0;
            _boss.OnPhaseChanged += p => reportedPhase = p;
            _boss.TakeDamage(100);
            Assert.AreEqual(2, reportedPhase);
        }

        [Test]
        public void Phase2_DoesNotFireAgain_OnFurtherDamage()
        {
            int count = 0;
            _boss.OnPhaseChanged += _ => count++;
            _boss.TakeDamage(100);
            _boss.TakeDamage(50);
            Assert.AreEqual(1, count);
        }
    }
}
```

- [ ] **Step 2: Run test — verify FAIL**

Run `BossControllerTests`. Expected: compile error.

- [ ] **Step 3: Implement BossController**

`Assets/_Game/Scripts/Enemies/BossController.cs`:
```csharp
using System;
using UnityEngine;

namespace StrafAdvance
{
    public class BossController : EnemyBase
    {
        public int Phase { get; private set; } = 1;
        public event Action<int> OnPhaseChanged;

        protected override void OnDamageTaken()
        {
            if (Phase != 1) return;
            if (CurrentHp <= Config.maxHp / 2)
            {
                Phase = 2;
                OnPhaseChanged?.Invoke(2);
            }
        }
    }
}
```

- [ ] **Step 4: Run tests — verify PASS**

Run `BossControllerTests`. All 5 tests must pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Game/Scripts/Enemies/BossController.cs Assets/_Game/Tests/EditMode/BossControllerTests.cs
git commit -m "feat: BossController with two-phase HP transition"
```

---

## Task 13: IAP & Unlock Registry

**Files:**
- Create: `Assets/_Game/Scripts/IAP/UnlockRegistry.cs`
- Create: `Assets/_Game/Scripts/IAP/IAPManager.cs`
- Create: `Assets/_Game/Tests/EditMode/UnlockRegistryTests.cs`

- [ ] **Step 1: Write failing test**

`Assets/_Game/Tests/EditMode/UnlockRegistryTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class UnlockRegistryTests
    {
        private UnlockRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            _registry = new UnlockRegistry();
        }

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        [Test]
        public void IsUnlocked_ReturnsFalse_ForUnknownProduct()
        {
            Assert.IsFalse(_registry.IsUnlocked("level_pack_2"));
        }

        [Test]
        public void Unlock_MakesProductUnlocked()
        {
            _registry.Unlock("level_pack_2");
            Assert.IsTrue(_registry.IsUnlocked("level_pack_2"));
        }

        [Test]
        public void Unlock_Persists_AcrossInstances()
        {
            _registry.Unlock("level_pack_2");
            var registry2 = new UnlockRegistry();
            Assert.IsTrue(registry2.IsUnlocked("level_pack_2"));
        }

        [Test]
        public void IsUnlocked_ReturnsFalse_WhenOnlyOtherProductUnlocked()
        {
            _registry.Unlock("level_pack_2");
            Assert.IsFalse(_registry.IsUnlocked("level_pack_3"));
        }
    }
}
```

- [ ] **Step 2: Run test — verify FAIL**

Run `UnlockRegistryTests`. Expected: compile error.

- [ ] **Step 3: Implement UnlockRegistry**

`Assets/_Game/Scripts/IAP/UnlockRegistry.cs`:
```csharp
using System;
using UnityEngine;

namespace StrafAdvance
{
    public class UnlockRegistry
    {
        private const string Prefix = "unlock_";

        public event Action<string> OnUnlocked;

        public void Unlock(string productId)
        {
            PlayerPrefs.SetInt(Prefix + productId, 1);
            PlayerPrefs.Save();
            OnUnlocked?.Invoke(productId);
        }

        public bool IsUnlocked(string productId)
            => PlayerPrefs.GetInt(Prefix + productId, 0) == 1;
    }
}
```

- [ ] **Step 4: Run tests — verify PASS**

Run `UnlockRegistryTests`. All 4 tests must pass.

- [ ] **Step 5: Implement IAPManager**

`Assets/_Game/Scripts/IAP/IAPManager.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace StrafAdvance
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        public static IAPManager Instance { get; private set; }

        public UnlockRegistry Registry { get; } = new UnlockRegistry();

        private IStoreController _store;
        private IExtensionProvider _extensions;

        private static readonly string[] ProductIds =
        {
            "level_pack_2", "level_pack_3", "level_pack_4",
            "skin_bundle_1", "skin_bundle_2"
        };
        private static readonly string[] ConsumableIds = { "powerup_pack" };

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePurchasing();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (string id in ProductIds)
                builder.AddProduct(id, ProductType.NonConsumable);
            foreach (string id in ConsumableIds)
                builder.AddProduct(id, ProductType.Consumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyProduct(string productId)
        {
            if (_store == null) { Debug.LogWarning("Store not initialized"); return; }
            _store.InitiatePurchase(productId);
        }

        public void RestorePurchases()
        {
            if (_extensions == null) return;
            _extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(
                (success, error) => Debug.Log(success ? "Restore OK" : $"Restore failed: {error}"));
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _store = controller;
            _extensions = extensions;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Registry.Unlock(args.purchasedProduct.definition.id);
            return PurchaseProcessingResult.Complete;
        }

        public void OnInitializeFailed(InitializationFailureReason error) =>
            Debug.LogError($"IAP init failed: {error}");

        public void OnInitializeFailed(InitializationFailureReason error, string message) =>
            Debug.LogError($"IAP init failed: {error} — {message}");

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription desc) =>
            Debug.LogWarning($"Purchase failed: {product.definition.id} — {desc.reason}");

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) =>
            Debug.LogWarning($"Purchase failed: {product.definition.id} — {reason}");
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/IAP/ Assets/_Game/Tests/EditMode/UnlockRegistryTests.cs
git commit -m "feat: UnlockRegistry with PlayerPrefs persistence and IAPManager wrapping Unity Purchasing"
```

---

## Task 14: Score Calculator & HUD

**Files:**
- Create: `Assets/_Game/Scripts/UI/ScoreCalculator.cs`
- Create: `Assets/_Game/Scripts/UI/HUDController.cs`
- Create: `Assets/_Game/Tests/EditMode/ScoreCalculatorTests.cs`

- [ ] **Step 1: Write failing test**

`Assets/_Game/Tests/EditMode/ScoreCalculatorTests.cs`:
```csharp
using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class ScoreCalculatorTests
    {
        [Test]
        public void Calculate_Returns100PerKill()
        {
            Assert.AreEqual(500, ScoreCalculator.Calculate(5));
        }

        [Test]
        public void CalculateStars_Returns0_WhenBossNotKilled()
        {
            Assert.AreEqual(0, ScoreCalculator.CalculateStars(false, true, true));
        }

        [Test]
        public void CalculateStars_Returns1_WhenOnlyBossKilled()
        {
            Assert.AreEqual(1, ScoreCalculator.CalculateStars(true, false, false));
        }

        [Test]
        public void CalculateStars_Returns2_WhenBossKilledAndNoDeath()
        {
            Assert.AreEqual(2, ScoreCalculator.CalculateStars(true, true, false));
        }

        [Test]
        public void CalculateStars_Returns3_WhenAllConditionsMet()
        {
            Assert.AreEqual(3, ScoreCalculator.CalculateStars(true, true, true));
        }
    }
}
```

- [ ] **Step 2: Run test — verify FAIL**

Run `ScoreCalculatorTests`. Expected: compile error.

- [ ] **Step 3: Implement ScoreCalculator**

`Assets/_Game/Scripts/UI/ScoreCalculator.cs`:
```csharp
namespace StrafAdvance
{
    public static class ScoreCalculator
    {
        private const int PointsPerKill = 100;

        public static int Calculate(int enemiesKilled) => enemiesKilled * PointsPerKill;

        public static int CalculateStars(bool bossKilled, bool noDeath, bool underParTime)
        {
            if (!bossKilled) return 0;
            if (!noDeath) return 1;
            if (!underParTime) return 2;
            return 3;
        }
    }
}
```

- [ ] **Step 4: Run tests — verify PASS**

Run `ScoreCalculatorTests`. All 5 tests must pass.

- [ ] **Step 5: Implement HUDController**

`Assets/_Game/Scripts/UI/HUDController.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private GameObject bossHpGroup;
        [SerializeField] private Slider bossHpSlider;

        public void SetPlayerHp(int current, int max)
        {
            playerHpSlider.maxValue = max;
            playerHpSlider.value = current;
        }

        public void SetWave(int waveIndex, int totalWaves)
            => waveLabel.text = $"Wave {waveIndex + 1}/{totalWaves}";

        public void ShowBossHp(int maxHp)
        {
            bossHpGroup.SetActive(true);
            bossHpSlider.maxValue = maxHp;
            bossHpSlider.value = maxHp;
        }

        public void UpdateBossHp(int current)
            => bossHpSlider.value = current;

        public void HideBossHp()
            => bossHpGroup.SetActive(false);
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add Assets/_Game/Scripts/UI/ScoreCalculator.cs Assets/_Game/Scripts/UI/HUDController.cs Assets/_Game/Tests/EditMode/ScoreCalculatorTests.cs
git commit -m "feat: ScoreCalculator with star logic and HUDController for HP/wave/boss display"
```

---

## Task 15: Remaining UI Screens

**Files:**
- Create: `Assets/_Game/Scripts/UI/MainMenuController.cs`
- Create: `Assets/_Game/Scripts/UI/LevelSelectController.cs`
- Create: `Assets/_Game/Scripts/UI/LevelSelectItem.cs`
- Create: `Assets/_Game/Scripts/UI/LevelCompleteController.cs`
- Create: `Assets/_Game/Scripts/UI/GameOverController.cs`
- Create: `Assets/_Game/Scripts/UI/ShopController.cs`
- Create: `Assets/_Game/Scripts/UI/ShopItemView.cs`

- [ ] **Step 1: Implement MainMenuController**

`Assets/_Game/Scripts/UI/MainMenuController.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private LevelSelectController levelSelect;

        void Start()
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            menuPanel.SetActive(GameManager.Instance.State == GameState.Menu);
        }

        void OnDestroy() => GameManager.Instance.OnStateChanged -= OnStateChanged;

        void OnStateChanged(GameState state)
            => menuPanel.SetActive(state == GameState.Menu);

        public void OnPlayPressed()
            => levelSelect.Show();

        public void OnShopPressed()
            => FindObjectOfType<ShopController>().Show();
    }
}
```

- [ ] **Step 2: Implement LevelSelectItem**

`Assets/_Game/Scripts/UI/LevelSelectItem.cs`:
```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class LevelSelectItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TMP_Text priceLabel;

        private Action _onPlay;

        public void Setup(LevelConfig config, bool unlocked, string price, Action onPlay)
        {
            _onPlay = onPlay;
            nameLabel.text = config.levelName;
            lockOverlay.SetActive(!unlocked);
            priceLabel.text = unlocked ? "Play" : price;
            playButton.interactable = true;
        }

        public void OnPlayButtonPressed()
        {
            _onPlay?.Invoke();
        }
    }
}
```

- [ ] **Step 3: Implement LevelSelectController**

`Assets/_Game/Scripts/UI/LevelSelectController.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform gridParent;
        [SerializeField] private LevelSelectItem itemPrefab;
        [SerializeField] private LevelConfig[] allLevels;

        void Start() => panel.SetActive(false);

        public void Show()
        {
            panel.SetActive(true);
            PopulateGrid();
        }

        public void Hide() => panel.SetActive(false);

        void PopulateGrid()
        {
            foreach (Transform child in gridParent)
                Destroy(child.gameObject);

            UnlockRegistry registry = IAPManager.Instance.Registry;
            foreach (LevelConfig level in allLevels)
            {
                bool unlocked = level.iapProductId == "free" || registry.IsUnlocked(level.iapProductId);
                LevelSelectItem item = Instantiate(itemPrefab, gridParent);
                LevelConfig captured = level;
                item.Setup(level, unlocked, "$1.99", () => OnLevelSelected(captured, unlocked));
            }
        }

        void OnLevelSelected(LevelConfig level, bool unlocked)
        {
            if (!unlocked)
            {
                IAPManager.Instance.BuyProduct(level.iapProductId);
                return;
            }
            Hide();
            FindObjectOfType<WaveSpawner>().LoadLevel(level);
            FindObjectOfType<CorridorScroller>().Initialize(level.worldScrollSpeed);
            GameManager.Instance.SetState(GameState.Playing);
            FindObjectOfType<WaveSpawner>().StartSpawning();
        }
    }
}
```

- [ ] **Step 4: Implement LevelCompleteController**

`Assets/_Game/Scripts/UI/LevelCompleteController.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class LevelCompleteController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;

        void Start()
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            panel.SetActive(false);
        }

        void OnDestroy() => GameManager.Instance.OnStateChanged -= OnStateChanged;

        void OnStateChanged(GameState state)
        {
            if (state != GameState.LevelComplete) { panel.SetActive(false); return; }
            panel.SetActive(true);
        }

        public void Show(int score, int stars)
        {
            panel.SetActive(true);
            scoreLabel.text = $"Score: {score}";
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = i < stars ? starFilled : starEmpty;
        }

        public void OnNextPressed() => GameManager.Instance.SetState(GameState.Menu);
        public void OnShopPressed() => FindObjectOfType<ShopController>().Show();
    }
}
```

- [ ] **Step 5: Implement GameOverController**

`Assets/_Game/Scripts/UI/GameOverController.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        void Start()
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            panel.SetActive(false);
        }

        void OnDestroy() => GameManager.Instance.OnStateChanged -= OnStateChanged;

        void OnStateChanged(GameState state)
            => panel.SetActive(state == GameState.GameOver);

        public void OnRetryPressed() => GameManager.Instance.SetState(GameState.Playing);
        public void OnMenuPressed() => GameManager.Instance.SetState(GameState.Menu);
        public void OnShopPressed() => FindObjectOfType<ShopController>().Show();
    }
}
```

- [ ] **Step 6: Implement ShopItemView**

`Assets/_Game/Scripts/UI/ShopItemView.cs`:
```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text buttonLabel;

        public void Setup(string itemName, string price, bool owned, Action onBuy)
        {
            nameLabel.text = itemName;
            priceLabel.text = owned ? "Owned" : price;
            buttonLabel.text = owned ? "Equip" : "Buy";
            actionButton.onClick.RemoveAllListeners();
            if (!owned) actionButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }
}
```

- [ ] **Step 7: Implement ShopController**

`Assets/_Game/Scripts/UI/ShopController.cs`:
```csharp
using UnityEngine;

namespace StrafAdvance
{
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform skinsGrid;
        [SerializeField] private Transform levelsGrid;
        [SerializeField] private ShopItemView itemPrefab;

        private static readonly (string id, string name, string price)[] SkinItems =
        {
            ("skin_bundle_1", "Character Skins Pack", "$2.99"),
            ("skin_bundle_2", "Weapon Skins Pack", "$2.99")
        };

        private static readonly (string id, string name, string price)[] LevelItems =
        {
            ("level_pack_2", "Levels 4-6", "$1.99"),
            ("level_pack_3", "Levels 7-9", "$1.99"),
            ("level_pack_4", "Levels 10-12", "$1.99")
        };

        void Start() => panel.SetActive(false);

        public void Show()
        {
            panel.SetActive(true);
            PopulateGrid(skinsGrid, SkinItems);
            PopulateGrid(levelsGrid, LevelItems);
        }

        public void Hide() => panel.SetActive(false);

        void PopulateGrid(Transform grid, (string id, string name, string price)[] items)
        {
            foreach (Transform child in grid) Destroy(child.gameObject);
            UnlockRegistry reg = IAPManager.Instance.Registry;
            foreach (var (id, name, price) in items)
            {
                bool owned = reg.IsUnlocked(id);
                ShopItemView view = Instantiate(itemPrefab, grid);
                string capturedId = id;
                view.Setup(name, price, owned, () => IAPManager.Instance.BuyProduct(capturedId));
            }
        }
    }
}
```

- [ ] **Step 8: Verify compilation**

Check Unity Console. No errors expected.

- [ ] **Step 9: Commit**

```bash
git add Assets/_Game/Scripts/UI/
git commit -m "feat: all UI screen controllers — main menu, level select, level complete, game over, shop"
```

---

## Task 16: Audio & Visual Polish

**Files:**
- Create: `Assets/_Game/Scripts/Audio/SoundID.cs`
- Create: `Assets/_Game/Scripts/Audio/AudioManager.cs`

- [ ] **Step 1: Create SoundID enum**

`Assets/_Game/Scripts/Audio/SoundID.cs`:
```csharp
namespace StrafAdvance
{
    public enum SoundID
    {
        Shoot,
        EnemyHit,
        PlayerHit,
        EnemyDeath,
        LevelComplete,
        BossRoar,
        BossPhase2,
        PowerUpCollect
    }
}
```

- [ ] **Step 2: Implement AudioManager**

`Assets/_Game/Scripts/Audio/AudioManager.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxPoolSize = 8;

        [System.Serializable]
        public struct SoundEntry
        {
            public SoundID id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private SoundEntry[] sounds;
        [SerializeField] private AudioClip[] musicTracks;

        private Dictionary<SoundID, SoundEntry> _map;
        private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _map = new Dictionary<SoundID, SoundEntry>();
            foreach (var entry in sounds) _map[entry.id] = entry;

            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Enqueue(src);
            }
        }

        public void PlaySFX(SoundID id)
        {
            if (!_map.TryGetValue(id, out SoundEntry entry)) return;
            AudioSource src = _sfxPool.Dequeue();
            src.clip = entry.clip;
            src.volume = entry.volume;
            src.Play();
            _sfxPool.Enqueue(src);
        }

        public void PlayMusic(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= musicTracks.Length) return;
            musicSource.clip = musicTracks[trackIndex];
            musicSource.loop = true;
            musicSource.Play();
        }

        public void SetMusicVolume(float v) => musicSource.volume = v;
        public void SetSFXVolume(float v)
        {
            foreach (var src in _sfxPool)
                src.volume = v;
        }
    }
}
```

- [ ] **Step 3: Add post-process volume in Unity**

In `GameScene`: create empty GameObject → name `PostProcessVolume` → add component **Volume** → set as Global volume → create new **VolumeProfile** in `Assets/_Game/Art/`.
Add overrides:
- **Bloom**: Intensity 0.6, Threshold 0.9
- **Color Adjustments**: Contrast 15, Saturation 10
- **Vignette**: Intensity 0.3

- [ ] **Step 4: Add Cinemachine camera**

Add package **Cinemachine** (already in manifest). In `GameScene`:
- Delete main camera.
- Add `Cinemachine Brain` component to new Camera GameObject.
- Create `CinemachineVirtualCamera` targeting the Player.
- Set offset: `(0, 3, -6)`, Look At: player's head position.
- Add `CinemachineImpulseSource` to player GameObject for boss hit shake.

Call shake from `BossController.OnDamageTaken()`:
```csharp
// In BossController.OnDamageTaken(), add after phase check:
if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
    impulse.GenerateImpulse(0.5f);
```

- [ ] **Step 5: Add trail renderer to bullet prefab**

On the `Bullet` prefab: add **Trail Renderer** component.
- Time: 0.1
- Width: 0.05 start → 0 end
- Material: Sprites/Default with emission color (cyan or white)

- [ ] **Step 6: Wire AudioManager calls**

Add `AudioManager.Instance.PlaySFX(SoundID.X)` calls:
- `AutoShooter.SpawnBullet()` → `PlaySFX(SoundID.Shoot)`
- `EnemyBase.Die()` → `PlaySFX(SoundID.EnemyDeath)`
- `PlayerHealth.TakeDamage()` (when damage > 0) → `PlaySFX(SoundID.PlayerHit)`
- `BossController.OnDamageTaken()` (phase2 transition) → `PlaySFX(SoundID.BossPhase2)`
- `GameManager.SetState(LevelComplete)` → `PlaySFX(SoundID.LevelComplete)`

- [ ] **Step 7: Verify full compilation**

Check Unity Console. Zero errors. Warnings from missing prefab references are OK at this stage.

- [ ] **Step 8: Final commit**

```bash
git add Assets/_Game/Scripts/Audio/ Assets/_Game/Art/ Assets/_Game/Audio/
git commit -m "feat: AudioManager with SFX pool, URP post-process volume, Cinemachine shake, and bullet trail"
```

---

## Post-Implementation Checklist

- [ ] Create and assign all ScriptableObject assets (`LevelConfig`, `WaveConfig`, `EnemyConfig`) for levels 1–3
- [ ] Build all enemy prefabs (`GruntEnemy`, `FlankerEnemy`, `EliteEnemy`) with correct layers (`Enemy`), tags (`Enemy`), and colliders
- [ ] Build Player prefab: `PlayerController` + `PlayerHealth` + `AutoShooter` + `PlayerBuffs`, tag = `Player`
- [ ] Build `BossController` prefab per level pack with `CinemachineImpulseSource`
- [ ] Set up `GameScene` hierarchy: `GameManager`, `WaveSpawner`, `CorridorScroller`, Player, UI Canvas (all panels), `AudioManager`, `PostProcessVolume`, Cinemachine camera
- [ ] Wire all `SerializeField` references in the Inspector
- [ ] Register all IAP products in Google Play Console (match product IDs exactly)
- [ ] Test IAP with Google Play sandbox account on device
- [ ] Android build: **File → Build Settings → Build** → install APK on test device
- [ ] Confirm 60fps on target device with Android Frame Pacing enabled (**Edit → Project Settings → Player → Android → Enable Frame Timing Stats**)
