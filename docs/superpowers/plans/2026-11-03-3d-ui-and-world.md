# Full 3D Game UI and World Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Present every part of the game in 3D, including all UI pages, the walkable towns, NPCs, and activity scenes, while retaining the existing playable story and Android touch flow.

**Architecture:** Use the owned `GameUiDioramaStage` renderer as a reusable 3D scene layer. Put page UI on a camera-facing World Space Canvas with a shared fitter and GraphicRaycaster; render town characters, architecture, and activity props as generated low-poly meshes. Keep current story/progression/gameplay state as the source of truth.

**Tech Stack:** Unity 6000.6.7f1, C#, URP, world-space uGUI, Input System, existing `Resources` sprites for printed art/portraits only.

**Spec:** `docs/superpowers/specs/2026-11-03-3d-ui-and-world-design.md`

## Global Constraints

- Preserve all story keys, mission IDs, save fields, unlock conditions, rewards, and activity callbacks.
- Convert every `PassportApp` page and the walkable-world HUD to World Space Canvas while retaining GraphicRaycaster and mobile touch handling.
- In-world player and NPC visuals use 3D low-poly models; no 2D character sprite may remain as the visible in-world avatar.
- All activity screens receive a 3D stage/props; existing activity rules and data remain unchanged.
- Add no Unity package and download no external models.
- Target mid-range Android at a 30 FPS floor; use shared materials and avoid real-time shadow maps.
- Preserve unrelated dirty files in the primary checkout.

## Review Focus

- World-space Canvas projection, scaler, safe area, and GraphicRaycaster work at narrow portrait aspect ratios and accept touch.
- Returning from a world restores the original camera and does not leave an extra UI camera/render texture active.
- Existing XY movement, NPC range checks, dialogue, and activity callbacks still point at the visible 3D models.
- Generated geometry/material count remains bounded and resources are destroyed with their owning page/world.
- Vietnamese/Korean text, passport content, and mini-game instructions remain readable in perspective.

---

### Task 1: Reusable 3D diorama stage

**Files:**
- Create `Assets/_Game/Scripts/UI/GameUiDioramaStage.cs` and its Unity `.meta`.

**Interfaces:**
- `GameUiDioramaStage.Ensure(Transform owner)`, `StageTexture`, `SetDestination(bool korean)`, and `SetStageVisible(bool visible)`.
- The stage owns its renderer, portrait RenderTexture, generated meshes/materials, and cleanup.

- [x] Create the isolated, shared-material 3D stage with Vietnam/Korea variants.
- [x] Defer resource creation until activation and synchronize camera lifetime/visibility.
- [ ] Recheck the component during final integration/build review.

### Task 2: World-space interface across every page

**Files:**
- Create `Assets/_Game/Scripts/UI/WorldSpaceCanvasFitter.cs` and `.meta`.
- Modify `Assets/_Game/Scripts/Core/PassportApp.cs`.

**Interfaces:**
- `WorldSpaceCanvasFitter.Configure(Canvas canvas, Camera camera)` sizes, positions, and orients the existing page Canvas for the current portrait camera/screen.
- `PassportApp.EnsureCanvas` retains its `Canvas`, `CanvasScaler`, `GraphicRaycaster`, safe-area root, and all current page callbacks.
- `GameUiDioramaStage` remains the 3D backdrop provider; UI text/buttons are planes in the scene's world-space Canvas.

- [ ] Convert all `PassportApp` pages, including menu, map, passport, dialogue, activities, results, and settings, to the fitted World Space Canvas.
- [ ] Keep touch raycasts and safe-area layout usable on narrow Android screens.
- [ ] Redesign menu, map, and passport around one primary action and physical-looking 3D book/map surfaces, removing oversized duplicate hero copy.
- [ ] Hide/restore the stage around world entry and return; preserve all callbacks and progression.

### Task 3: 3D walkable towns, player, NPCs, and HUD

**Files:**
- Create `Assets/_Game/Scripts/World/WorldDioramaBuilder.cs` and `.meta`.
- Create `Assets/_Game/Scripts/World/WorldCharacterModelBuilder.cs` and `.meta`.
- Modify `Assets/_Game/Scripts/World/WalkableWorldSession.cs`, `WorldPlayerController.cs`, and `WorldInteractable.cs`.

**Interfaces:**
- `WorldDioramaBuilder.Build(Transform parent, WorldDefinition world)` adds the destination's low-poly buildings/street/props on existing interaction coordinates.
- `WorldCharacterModelBuilder.Create(Transform parent, string characterId, bool isPlayer)` returns an owned `WorldCharacterModel` component; `SetWalking(bool walking)` drives the player pose from existing movement state and NPCs use a bounded idle pose.
- `WalkableWorldSession` retains `WorldDefinition` coordinates, `InteractNearest`, profile persistence, and open/close/activity callbacks.
- The HUD uses `WorldSpaceCanvasFitter` with the existing world camera and touch controls.

- [ ] Replace the visible player/NPC sprite renders with low-poly 3D models and simple idle/walk motion.
- [ ] Add destination-specific 3D architecture, landmarks, craft displays, and one low-cost light setup.
- [ ] Put the HUD, journal, dialogue, controls guide, and stick in a fitted World Space Canvas.
- [ ] Preserve interaction radius, checkpoint, camera restore, and runtime cleanup.

### Task 4: 3D presentation for passport and every activity

**Files:**
- Create `Assets/_Game/Scripts/UI/GameUiActivityDiorama.cs` and `.meta`.
- Modify `Assets/_Game/Scripts/Core/PassportApp.cs` after Task 2.

**Interfaces:**
- `GameUiActivityDiorama.SetActivity(string activityId, bool korean)` selects a small generated 3D prop set for the current page.
- It does not change the `ExplorationRun`, `RecipeRun`, `MatchRun`, craft runs, score, reward, or completion callback.

- [ ] Show the passport as an open 3D book with existing page content on its surfaces.
- [ ] Add 3D boards/props for architecture exploration, food assembly, matching, lantern/Hanji craft, and festival preparation/results.
- [ ] Keep buttons, instructions, language switching, and existing game rules functional in the world-space Canvas.
- [ ] Release/reuse page props without duplicating cameras, meshes, or materials during `Show()` rebuilds.

### Task 5: Integration and build review

**Files:**
- Review the complete changed set and generated Unity build artifacts.

- [ ] Build Android with the existing Unity project settings and inspect portrait presentation.
- [ ] Check World Space UI touch routing and camera restore with a manual menu → world → NPC → activity → map flow in an available player/editor.
- [ ] Record any device-specific performance or touch behavior that cannot be verified because no Android device is attached.

Automated tests are not part of this plan. Use static diff checks, Unity build, and visual/manual inspection only.
