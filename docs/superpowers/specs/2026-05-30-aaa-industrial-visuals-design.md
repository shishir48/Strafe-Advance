# Strafe Advance — AAA Industrial Visual Overhaul (Phase 3)

**Date:** 2026-05-30
**Branch:** `feat/phase3-industrial-visuals`
**Status:** Design — approved shape, pending spec review

## Goal

Lift the in-game visuals from "MVP / programmer-art" to a shippable premium look, **without touching the 12k LOC of working gameplay systems**. The diagnosis: gameplay plumbing, HUD, juice (damage numbers, hitstop, screenshake, killcam, shield VFX), progression and monetization are all senior-grade. Phase 3 "Visual Production" is the only roadmap phase at 0%. That gap *is* the cheap-looking game.

Success = a player/screenshot reads the game as a polished commercial mobile shooter, not a demo.

## Locked Decisions

- **Art direction:** B — Gritty Industrial (sci-fi).
- **Fidelity tier:** 1 — Stylized industrial (low-poly, hand-painted metal). Keeps the existing blocky player; no character remodel.
- **Assets:** Free / CC0. Primary pack = **Quaternius Modular Sci-Fi MegaKit** (CC0, 270+ modular pieces, URP shaders w/ color masks + blinking lights). Backups: Quaternius Sci-Fi Essentials Kit, Kenney Space Kit.
- **Palette:** Dark industrial metal environment + warm practical lights (amber/orange). Player stays cyan/blue (hero read against warm world). Enemies warm red.

## Constraints / Ground Truth (verified via MCP + repo)

- Unity 6, **URP 17.4.0**, **Cinemachine 3.1.3** installed. Post-processing is URP-integrated (no separate package).
- A **`PostProcessVolume` (Volume) GameObject already exists** in GameScene; `DefaultVolumeProfile.asset` + `SampleSceneProfile.asset` exist in `Assets/Settings/`. Stage 1 populates the profile, does not create the system.
- Two RP assets: `Mobile_RPAsset` + `PC_RPAsset` (+ matching Renderers). Visual settings must be applied in a way that holds on the **mobile** asset (the ship target), not just PC.
- Main Camera carries **custom `ScreenShake`, `CameraFeel`, `KillCam`, `UniversalAdditionalCameraData`** — NOT a Cinemachine vcam. These scripts drive camera transform directly.
- Environment is a **`CorridorScroller`** streaming `CorridorTile` prefabs (endless-runner feel), with a `NeonLightStrip` already in scene. Not a static arena.
- **Shader Graph** ships with URP (available). **VFX Graph is NOT installed** — current VFX are Shuriken particle prefabs under `Assets/_Game/Prefabs/VFX/` + `Assets/Resources/VFX/`.
- Target perf: 60 FPS iPhone 12 / Pixel 6, 30 FPS low-end (per roadmap Phase 7). Every visual addition is mobile-budget-aware.

## Non-Goals

- No gameplay/balance/systems changes.
- No character/enemy remodel (tier 1 keeps blocky meshes).
- No new monetization, audio, or UI-layout work (HUD already exists; only restyle if it clashes with new palette).
- No photoreal PBR, no baked-GI-heavy realism (that was tier 2, rejected).

## Architecture / Approach

Six stages, ordered by **visual-payoff-per-hour** so the game visibly transforms after Stage 1, not at the end. Each stage is an isolated unit with a clear deliverable, an in-editor screenshot verification (via MCP `manage_camera` capture in play mode), and its own commit on `feat/phase3-industrial-visuals`. Only my own files staged per commit (sprint-7 WIP in the tree stays untouched).

### Stage 1 — Render Foundation (no assets needed)
Populate the existing Volume profile + RP assets. Establishes mood before any geometry.
- Volume overrides: Tonemapping (ACES/Neutral), Bloom (HDR threshold for emissives), Color Adjustments (post-exposure, contrast, warm tint), Vignette, Shadows/Midtones/Highlights split-tone (warm/cool), subtle Film Grain, optional Depth of Field (mobile-cheap mode).
- Kill the default procedural skybox → dark gradient/flat ambient or HDRI later.
- Fog (URP linear/exponential) for depth, warm-tinted.
- Ambient + environment lighting set dark so practical lights read.
- Apply to **Mobile_RPAsset** (HDR on, post enabled) verifying mobile cost.
- **Verify:** play-mode screenshot shows graded, bloomed, fogged scene vs flat baseline.

### Stage 2 — Lighting Pass
- Drop global brightness; convert to dark scene lit by **warm practical point/spot lights** (corridor strips, props).
- Real-time shadows on key light(s) within mobile budget; light layers if needed.
- Tune `Directional Light` to a low cool fill; warm points as the mood.
- Rim/key separation so the cyan player pops against warm bg.
- **Verify:** screenshot — industrial mood, player reads clearly, no blown highlights.

### Stage 3 — Environment (MegaKit)
- **Manual step (user):** download + import Quaternius Modular Sci-Fi MegaKit into `Assets/`.
- Feed MegaKit modules into the **CorridorScroller / CorridorTile** pipeline (rebuild/replace tile prefab content with modular corridor sections; keep the streaming + collision contract intact).
- Recolor via the pack's color-mask shader → dark metal + warm accents palette.
- Add prop dressing (crates, pipes, panels), emissive blinking lights wired to bloom.
- Replace flat gray ground plane with textured industrial floor modules.
- **Verify:** screenshot — corridor streams correctly, no gaps/seams, gameplay unaffected.
- **Fallback if no import:** ProBuilder greybox corridor + Kenney CC0 props, same pipeline.

### Stage 4 — Shaders (Shader Graph, ships with URP)
- Rim-light shader for player/enemies (cheap fresnel) — silhouette pop.
- Hit-flash (already have flash logic; move to a shader property for cleaner look).
- Dissolve-on-death shader for enemy despawn (ties to existing death flow).
- Energy-shield shader (replace current shield bubble with fresnel + scrolling hex).
- Emissive enemy "tell" material channel (charger telegraph, elite glow) hooked to existing tell events.
- **Verify:** screenshot each shader on a live enemy/player.

### Stage 5 — VFX Pass
- **Decision point:** install VFX Graph package OR upgrade existing Shuriken prefabs in place. Default: **upgrade Shuriken** first (zero new deps, mobile-safe); install VFX Graph only if a specific effect needs it.
- Rework muzzle flash, bullet impact, enemy death, powerup pickup, dodge to art-directed style matching palette (warm sparks, smoke, debris).
- Impact decals (URP decal renderer feature) where cheap.
- Restyle bullet/trail materials off programmer-art pink → emissive plasma reading against dark world.
- **Verify:** screenshot combat moment — weighty impacts, cohesive palette.

### Stage 6 — Camera + Framing (highest risk)
- Reframe: pull camera back / adjust angle so the world + action read (current framing too close).
- Introduce **Cinemachine vcam** carefully: must **integrate with or replace** the custom `ScreenShake`/`CameraFeel`/`KillCam` scripts, not double-drive the transform. Preferred path: Cinemachine vcam + **CinemachineImpulse** replacing `ScreenShake`, with `CameraFeel`/`KillCam` either ported to Cinemachine or kept and the vcam disabled during their overrides. Exact integration decided at implementation time after reading those 3 scripts.
- Recoil/impulse shake on fire + impact via impulse sources.
- **Verify:** screenshot + play feel — no camera fighting, killcam still works, shake reads.

## Risks

1. **Camera integration (Stage 6)** — 3 custom camera scripts vs Cinemachine. Highest chance of regression. Mitigation: read all 3 first, prefer impulse-based integration, keep killcam working as the acceptance test.
2. **CorridorScroller + MegaKit (Stage 3)** — streaming tiles must keep collision + scroll contract. Mitigation: replace tile *content* under the existing tile root, don't rewrite the scroller.
3. **Mobile perf** — bloom + real-time shadows + extra lights can blow the frame budget. Mitigation: apply to Mobile_RPAsset and screenshot/perf-check; cap real-time shadow-casting lights; bloom at low res.
4. **Manual asset import** — Stage 3 blocks on user importing the MegaKit. Mitigation: greybox fallback keeps progress unblocked.
5. **Default Volume profile shared** — `DefaultVolumeProfile` may be referenced by the RP global settings; editing it affects all scenes. Mitigation: use the in-scene Volume's own profile, not the global default.

## Verification Strategy

Per stage: enter play mode via MCP, capture Game View screenshot (`manage_camera`, `capture_source=game_view`), compare against the previous baseline, confirm no console errors (`read_console`), confirm gameplay still runs. Commit only on a clean, visibly-improved result. Existing 105 EditMode + 6 PlayMode tests must stay green (these are systems tests; visual work shouldn't touch them, but run as a regression gate).

## Out-of-Scope Follow-ups (noted, not now)

- Mixamo rig + animation (roadmap Phase 3 item — bigger effort, separate spec).
- Skybox HDRI nebula, multiple environment biomes.
- UI Toolkit migration.
