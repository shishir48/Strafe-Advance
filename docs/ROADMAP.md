# Strafe Advance — Roadmap to Paid Game

**Status:** Demo → polished mobile shooter, indie scope, 6 months
**Target:** Premium mobile shooter (Android/iOS), $4.99 with IAP cosmetic shop
**KPIs to hit before global launch:** D1 ≥ 35%, D7 ≥ 12%, D30 ≥ 5%, ARPDAU ≥ $0.10

> **Session checkpoint (last touched 2026-05-25):** Phase 1 ✅, Phase 2 ✅ 22/24, Phase 4 ✅ 6/8, Phase 5 ✅ event-routing (clips TODO), Phase 6 ✅ currency + shop + daily login + achievements + toasts + **battle pass**, Phase 7 ✅ CI tests + Android build + crash reporter + **PlayMode smoke tests**. **105 EditMode + 2 PlayMode = 107 tests green** + CI matrix runs both on every PR (pending Unity secrets). Full session log + file map in `PROGRESS.md`.

---

## Current Demo Audit

| Area | Demo Signal |
|------|-------------|
| Wave system | Single enemy type per wave, no mix |
| Enemy variety | 3 types (Grunt, Flanker, Elite) + Boss, no behavior depth |
| Animation | Static FBX, no rigs, no death rolls, no aim IK |
| Combat juice | None — no screen shake, hitstop, ragdoll, damage numbers, kill cam |
| Code architecture | `FindAnyObjectByType`, `SetField` reflection, no DI, no event bus |
| Asset loading | `Resources.Load` everywhere (anti-pattern, blocks ship size) |
| Save system | None — runs forget everything |
| Input | Mixed legacy + new Input System, no rebinding |
| Audio | Stub AudioManager, no SFX library, no adaptive music |
| Monetization | IAPManager placeholder, no products, no ads, no currency |
| Telemetry | None |
| UI | Basic HUD + tap-to-start, no main menu polish, no settings |
| Tutorial | None |
| Localization | EN only, hardcoded strings |
| Performance | No profiling, no LOD, no occlusion culling |
| CI/CD | Manual builds via menu item |

---

## Phase 1 — Foundation Refactor ✅ COMPLETE (2026-05-23)

Goal: stop fighting the codebase. Lay senior-grade plumbing.

- [x] **Addressables migration** — `AssetLoader` facade with Addressables → Resources fallback, 9 keys registered via `StrafAdvance/11. Bootstrap Addressables`
- [x] **DI container** (VContainer 1.18.0) — `GameLifetimeScope` registers GameManager + WaveSpawner + CorridorScroller + AudioManager + IAPManager + `SaveSystemFacade`
- [x] **State machine** — hand-rolled `StateMachine<TState>` with validated transitions, enter/exit callbacks; `GameManager` now FSM-driven
- [x] **Event bus** — hand-rolled `EventBus<T>` typed pub/sub, zero-alloc dispatch, exception-safe; standard messages `GameStateChanged`, `EnemyKilled`, `WaveStarted`, `PlayerDamaged`
- [x] **Save system** — `SaveSystem.cs` JSON + AES-256-CBC + atomic write (temp+rename) + .bak rotation + schema versioning + migration scaffolding
- [x] **New Input System** — `GameInput` facade, all `UnityEngine.Input` calls gone from runtime
- [x] **Test harness** — fixed 4 pre-existing failures + added EventBus/StateMachine/SaveSystem suites; 36 → **52 EditMode tests, 0 failing**

---

## Phase 2 — Gameplay Depth (4 weeks)

- [x] **Mixed-type waves** — `WaveConfig.entries[]` shipped P1.3; L1_W4/W6/W7/W9 converted to mixed (P2.5/P2.6)
- [x] **Enemy variety** — Charger (P2.6), Sniper (P2.10), Shielded (P2.12), Splitter (P2.13), Drone swarm boids (P2.15), Mini-Boss (P2.19) — 8 enemy types total
- [x] **Enemy juice** — universal HitReact flash+pop+knock (P2.16), Charger telegraphed lunge (P2.18), aim-leading + accuracy jitter (P2.20), per-level difficulty scaling (P2.20)
- [ ] **Enemy AI** — proper behavior trees (NodeCanvas free), not Update timers. Charger/MiniBoss have local FSMs; full unified brain skipped this batch.
- [x] **Weapons** — 5 weapons in catalog (P2.11): Standard Blaster, Rapid SMG, Heavy Cannon, Scatter Gun (5-bullet spread), Tracker Pistol (homing). AutoShooter reads equipped. Alt-fire/reload still pending.
- [x] **Player progression (foundation)** — `PlayerProgression` service (P2.8): XP per kill, quadratic level curve, auto-unlock perks, 5-perk catalog with stat multipliers, persisted in SaveData
- [x] **Perk equip UI** — `PerkEquipPanel` (P2.14): runtime canvas auto-opens on level-up, lists unlocked perks, tap-toggle equip (max 3), persists via SaveSystem, live-refreshes AutoShooter
- [x] **Combat juice** — hitstop (P2.3, 0.04s grunt / 0.10s elite / 0.06s player hit), screen shake (P2.2, Perlin trauma model), damage numbers (P2.1, pooled TMP), slow-mo via hitstop chain
- [x] **Combat juice (extended)** — ragdoll-lite (P2.22) + KillCam slow-mo+zoom on boss death (P2.23). Cinemachine impulse upgrade still pending.
- [x] **Movement** — dodge roll (P2.9), sprint+stamina (P2.21); slide/aim-assist still pending
- [x] **Power-ups** — pickup prefab existed; `PowerUpDropper` (P2.7) drops on kill with type-based chance (Grunt 5% / Flanker 10% / Elite 40% / Charger 10%); types: Shield, RapidFire, Multishot
- [x] **Combo + score multiplier** — `ComboTracker` (P2.4): ×1→×2 at 5, ×4 at 10, ×8 at 20; resets on miss-timeout (2s) or PlayerDamaged

---

## Phase 3 — Visual Production (4 weeks)

- [ ] **Mixamo rig** astronautA — idle, run forward/strafe, aim, shoot, hit, death (~10 anims)
- [ ] **Animator** with blend trees + IK constraints (weapon-arm to target)
- [ ] **Custom shader** (Shader Graph) — fresnel rim, dissolve death, hit flash, energy shield
- [ ] **VFX Graph** rewrite — smoke, plasma, sparks, impact decals, env particles
- [ ] **Cinemachine** — gameplay vcam, kill cam, boss intro cam, scripted shake on impulse
- [ ] **Lighting pass** — directional + point lights on corridor strips, fog, baked light probes
- [ ] **Skybox** — sci-fi nebula HDRI (Polyhaven CC0)
- [ ] **Environment variants** — 4+ corridors: industrial / cargo / boss arena / abandoned + prop kit

---

## Phase 4 — UI / UX (3 weeks)

- [x] **Main menu** (P4.3) — runtime-built `MainHubController`: animated title pulse, currency chip, Play/Loadout/Shop/Settings/Quit, auto-shown when `GameState=Menu`
- [x] **Loadout screen** (P4.3) — `LoadoutPanel`: weapon picker from unlocked catalog + equipped perks display + Start Run; persists `equippedWeaponId` and live-refreshes AutoShooter
- [x] **In-run HUD** (P4.1) — HP + stamina + dodge pip + wave + combo + rolling score (`ModernHUD`)
- [x] **Pause menu** (P4.2) — Esc/Start toggle, Resume/Perks/Restart/Quit, freezes time
- [x] **Settings** (P4.5) — `SettingsPanel`: Music/SFX/UI sliders, aim sensitivity slider, Vibration/InvertY/Colorblind toggles, Low/Med/High Quality dropdown, Reset Profile. Persists to `SaveData.settings` and applies live to AudioManager + QualitySettings
- [x] **Tutorial** (P4.6) — `TutorialController`: 4 FSM-driven steps (Strafe → Sprint → Dodge → Combo) that advance on action detection (player x-delta, `IsSprinting`, `DodgePerformed` event, `ComboChanged.Multiplier ≥ 2`). Skip button + Settings → Reset Tutorial. Persists `profile.tutorialCompleted` so it only fires on first run
- [ ] **Localization** — SmartLocalization, ship EN + ES + JP + ZH-CN
- [ ] **UI Toolkit (UXML)** for menus — faster iteration than uGUI for complex layouts
- [x] **Post-run summary** (P6.1, lives in UI) — score, kills, XP, currency, best, restart/menu buttons

---

## Phase 5 — Audio (2 weeks)

- [ ] **Adaptive music** — FMOD or Unity Audio Mixer snapshots: chill → tension → boss
- [ ] **SFX library** — 60+ clips: weapons per-type, impacts per-surface, enemy vocals, UI clicks, ambient. SoundID enum and `SfxRouter` already in place (P5.1) — only AudioClip assets needed
- [x] **Event routing** (P5.1) — `SfxRouter` bridges `EnemyKilled`/`EnemyDamaged`/`PlayerDamaged`/`DodgePerformed`/`ShieldHit`/`ComboChanged`/`PerkUnlocked`/`BossPhaseChanged` to `AudioManager.PlaySFX`
- [ ] **VO barks** — short player + boss lines (Eleven Labs or asset store)
- [ ] **3D positional audio** — distance falloff, occlusion on corridors
- [ ] **Mixer routing** — Music / SFX / VO / UI buses with per-bus volume controls in Settings

---

## Phase 6 — Monetization + Live Ops (3 weeks)

- [x] **Soft currency** (P6.1) — `CurrencyService` awards per-enemy drop, persists in SaveData, exposes EarnedThisRun for summary. `CurrencyEarned` event for HUD popups
- [x] **Shop w/ soft currency** (P6.2) — `ShopController` tabbed (Weapons / Cosmetics). Weapons tab spends `CurrencyService.TrySpend(price)` to unlock from `WeaponCatalog`. Equip/Buy/Locked states reactive to balance + ownership. Persists `unlockedWeaponIds` and `equippedWeaponId`
- [ ] **Hard currency** — IAP gems (Phase 6 actual)
- [ ] **Store cosmetics** — cosmetic skins (player, blaster, corridor theme), weekly featured rotation (current Cosmetics tab is IAP bundles only)
- [ ] **IAP products** — gem packs ($1.99/$4.99/$9.99/$19.99), starter bundle, season pass, no-ads removal
- [ ] **Rewarded ads** — IronSource/AppLovin: revive on death, 2x reward on run end
- [x] **Battle Pass** (P6.5) — `BattlePassService` + 10-tier `BattlePassCatalog` (Season 1, linear XP curve 500 per tier, free+premium lanes; tier 3/5/7/9 premium grants weapon unlocks). `BattlePassPanel` runtime UI: scrollable tier list, XP-to-next progress bar, per-lane claim buttons, Unlock Premium CTA (500 credits placeholder until IAP wired). MainHub gains a `BATTLE PASS` button + a `BP Tier N/10` chip top-right. ToastNotifier announces tier-ups
- [x] **Daily login** (P6.3) — `DailyLoginService`: UTC-day streak, escalating reward curve (50→500 capped at day 7), gap resets to 1, idempotent same-day, emits `DailyLoginCheckedIn`
- [x] **Achievements** (P6.4) — `AchievementService` + 8-entry `AchievementCatalog` (kill counts, levels, first win, wave 10, 7-day streak); predicate-based, grants currency on unlock, retroactive eligibility, emits `AchievementUnlocked`. `ToastNotifier` renders both daily-login and achievement events as queued bottom-center cards
- [ ] **Leaderboards** — Lootlocker free tier (not yet)
- [ ] **Remote config** (Firebase) — tune wave difficulty + IAP prices + drop rates without app update
- [ ] **Analytics** — GameAnalytics or Firebase: funnel, D1/D7/D30 retention, ARPDAU, churn cohorts

---

## Phase 7 — Quality + Ship (4 weeks)

- [ ] **Performance** target: 60 FPS iPhone 12 / Pixel 6, 30 FPS low-end. SRP Batcher, GPU Instancing, LOD groups, occlusion culling
- [ ] **Memory** — ASTC/ETC2 textures, audio compression, asset bundle splits
- [x] **Crash reporting (in-process)** (P7.2) — `CrashReporter` hooks `Application.logMessageReceivedThreaded`, keeps a 50-entry breadcrumb ring, persists crash report atomically to disk on Exception/Error, detects "previous session crashed" on next boot. Pluggable `ICrashUploader` (default no-op `LocalFileUploader`); to enable Sentry/Crashlytics, install the SDK and call `CrashReporter.SetUploader(new SentryAdapter())` before `Awake` (e.g. from Bootstrap scene). Native NDK crashes still need a real SDK
- [x] **CI/CD** (P7.1+P7.4) — GitHub Actions: `tests.yml` matrix runs **EditMode + PlayMode** in parallel on every PR / push to main; `build-android.yml` builds Android APK on `v*` tag via `game-ci/unity-builder@v4` and attaches to GitHub release. Requires `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` secrets in repo settings. iOS pipeline still TODO
- [ ] **Bug bash** — controller test, accessibility audit, age rating, store metadata + screenshots
- [ ] **Soft launch** — Philippines/Vietnam 4 weeks, optimize KPIs
- [ ] **Global launch** — Apple/Google editorial pitch, launch trailer, press kit

---

## Team Estimate (typical mid-tier indie)

| Role | Count | Notes |
|------|-------|-------|
| Senior generalist Unity dev | 1 | Lead |
| Gameplay/combat programmer | 1 | |
| UI/tools programmer | 1 | |
| 3D artist / animator | 1 | |
| VFX + tech artist | 1 | |
| Audio designer | 1 | Contract from Phase 5 |
| Game designer | 1 | |
| Producer / live ops | 1 | |
| QA | 1 | Contract from Phase 5 |

## Budget (6 months indie scope)

- Team salaries: **$250k–$400k**
- Asset licenses (FMOD/Wwise, Mixamo Pro, IronSource setup): **$5k–$15k**
- Marketing/UA budget: **$50k–$200k** at launch
- **Total: ~$400k–$700k**

---

## Phase 1 Execution Order (this session + follow-ups)

1. Fix 4 pre-existing test failures (baseline green)
2. Add `SaveSystem` — JSON + AES, atomic writes, version migration
3. Mixed-type `WaveConfig.entries[]` + spawner support
4. New Input System wiring + replace legacy `Input` calls
5. VContainer DI scope (replaces `FindAnyObjectByType`, `SetField`)
6. Addressables migration (kills `Resources.Load`)
7. State machine refactor (Stateless package)
8. Event bus (MessagePipe)
