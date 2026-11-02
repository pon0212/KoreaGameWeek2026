# Walkable World and Story Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Vietnamese-first, persistent, walkable Hội An/Jeonju exploration slice to the existing DẤU XỨ project.

**Architecture:** Keep the current entry scene, responsive uGUI and six mission loops. Migrate the existing local profile to schema 2, add focused story-state data, and create a runtime 2D exploration layer opened from the current map. The layer owns world visuals, movement, collision, camera follow, nearby interactions, dialogue and save-on-exit/pause.

**Tech Stack:** Unity 6.6.7f1, C#, URP 2D, uGUI, the installed Input System, `Resources` sprites, existing `ProfileStore` JSON saves.

**Spec:** `docs/superpowers/specs/2026-11-02-dau-xu-rebuild-design.md`

## Global Constraints

- Không tạo Unity project mới.
- Không xóa source đang hoạt động, Scene, Prefab hoặc tài nguyên người dùng.
- Không tự ý thay đổi Unity version hoặc nâng package.
- Giữ nguyên `Assets/_Game/Scenes/TastePassport_MVP.unity` làm entry scene.
- Android là nền tảng chính; Windows hỗ trợ chạy thử và trình diễn.
- Tiếng Việt là ngôn ngữ mặc định; phải giữ khả năng chọn tiếng Hàn.
- Tên nhân vật chính thức là Nhung và Ga Bo (가보); người chơi là nhân vật điều khiển.
- Giữ nguyên các sửa đổi chưa commit của người dùng; chỉ stage file thuộc phase này.
- Không tuyên bố Android build/device QA khi chưa có Unity Android Build Support và SDK.

## Review Focus

- Hồ sơ schema 1, schema 2, bị hỏng hoặc thiếu field phải giữ được các stamp cũ và fallback backup; kiểm tra trong Task 1 migration smoke.
- Profile chưa có world ID, world ID lạ, hoặc tọa độ ngoài bản đồ phải trở về checkpoint hợp lệ; kiểm tra trong Task 1 và Task 3.
- Thiết bị chỉ có bàn phím hoặc chỉ có cảm ứng phải di chuyển được và không vượt biên; kiểm tra clamp trong Task 3 và controller bằng Unity smoke khi Editor khả dụng.
- Tương tác NPC lặp lại không được cộng dấu vô hạn; kiểm tra cờ/dấu idempotent trong Task 1 và Task 4.
- Thoát, pause hoặc resume khi đang đi trong thế giới phải giữ checkpoint và không để UI/world camera mắc kẹt; kiểm tra lưu phiên trong Task 4 smoke.

---

### Task 1: Profile migration and persistent story marks

**Files:**
- Create: `Assets/_Game/Scripts/Story/StoryProgressService.cs`
- Modify: `Assets/_Game/Scripts/Save/PlayerProfile.cs`
- Modify: `Assets/_Game/Scripts/Save/ProfileStore.cs`
- Create: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- `PlayerProfile` adds schema-2 story chapter/completion, unique story flags, unique world imprints, last world ID, and last world X/Y while retaining all schema-1 fields.
- `StoryProgressService.HasFlag(PlayerProfile, string) -> bool`
- `StoryProgressService.AddFlag(PlayerProfile, string) -> bool`
- `StoryProgressService.HasImprint(PlayerProfile, string) -> bool`
- `StoryProgressService.RecordInteraction(PlayerProfile, string, string) -> bool` records an optional story flag and optional imprint once.
- `ProfileStore.Load()` accepts schema 1 and schema 2; schema-1 records normalize to schema 2 without losing existing fields.

- [x] **Step 1: Add migration and idempotency smoke assertions** in `TastePassportWorldSmoke.RunProfileAndStoryChecks`: load a hand-written schema-1 JSON profile containing name, language and one completed level; assert schema-2 migration retains them; record the same interaction twice; assert one flag and one imprint; save/reload and assert both remain.
- [x] **Step 2: Run the editor assembly compile** and confirm it fails only because the new story API/profile fields have not been implemented yet.
- [x] **Step 3: Implement schema-2 normalization and migration** in `PlayerProfile`/`ProfileStore`; enforce bounded list sizes, valid world IDs, finite coordinates, and unique non-empty flag/imprint IDs.
- [x] **Step 4: Implement the story-state service** with duplicate-safe add and record operations.
- [x] **Step 5: Re-run the editor assembly compile**; expected: 0 compiler errors.

### Task 2: Original character sprites for the walkable slice

**Files:**
- Create: `Assets/_Game/Resources/Art/Characters/Traveler.png`
- Create: `Assets/_Game/Resources/Art/Characters/Nhung.png`
- Create: `Assets/_Game/Resources/Art/Characters/GaBo.png`
- Create: Unity `.meta` files for the new folder and sprites.
- Modify: `Assets/_Game/Resources/Art/ASSET_PROVENANCE.md`

**Interfaces:**
- Load as `Sprite` resources at `Art/Characters/Traveler`, `Art/Characters/Nhung`, and `Art/Characters/GaBo`.
- Characters use the same premium cozy storybook treatment, transparent background, full-body front/three-quarter view, no text, no watermark, and remain visually distinct from each other.

- [x] **Step 1: Generate three project-bound original transparent sprites** with the built-in image generation tool, keeping consistent style and respecting the official character descriptions.
- [x] **Step 2: Inspect each output** for transparency, legibility at mobile size, cultural details and separation from the player identity.
- [x] **Step 3: Save each selected image and unique Unity metadata** to the exact paths above without replacing existing artwork.
- [x] **Step 4: Record the generation method and limitations** in `ASSET_PROVENANCE.md`.

### Task 3: Walkable world movement and camera

**Files:**
- Create: `Assets/_Game/Scripts/World/WorldCatalog.cs`
- Create: `Assets/_Game/Scripts/World/WorldPlayerController.cs`
- Create: `Assets/_Game/Scripts/World/WorldTouchStick.cs`
- Create: `Assets/_Game/Scripts/World/WorldInteractable.cs`
- Create: Unity `.meta` files for the new scripts and folder.
- Modify: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- `WorldCatalog.Get(string worldId) -> WorldDefinition` supports `hoian` and `jeonju`, with distinct existing background resource paths and bounded walk areas.
- `WorldPlayerController.ClampToBounds(Vector2 position, Vector2 min, Vector2 max) -> Vector2` is public static and deterministic.
- `WorldPlayerController` accepts `Vector2` movement from keyboard or touch, moves a 2D Rigidbody without passing world bounds, and updates an orthographic camera follow target.
- `WorldInteractable` carries a stable ID, speaker key, first/repeat dialogue keys, optional story flag and optional imprint ID.

- [x] **Step 1: Add boundary assertions** to the editor smoke: input beyond each world edge clamps to the edge and an in-bounds point remains unchanged.
- [x] **Step 2: Implement the two-world catalog** using separate Hội An and Jeonju backgrounds, spawn points, limits, interactable positions and labels.
- [x] **Step 3: Implement controller movement and camera follow** with keyboard input on Windows/Editor and a uGUI pointer-driven virtual stick for touch.
- [x] **Step 4: Implement collision-safe bounds** and make the static clamp method the same code path used by runtime movement.
- [x] **Step 5: Compile the runtime assembly**; expected: 0 compiler errors.

### Task 4: World session, NPC dialogue and saved interaction state

**Files:**
- Create: `Assets/_Game/Scripts/World/WalkableWorldSession.cs`
- Modify: `Assets/_Game/Scripts/World/WorldInteractable.cs`
- Modify: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- `WalkableWorldSession.Open(string worldId, PlayerProfile profile, ProfileStore store, Action onClose)` creates the world background, player, NPCs, camera configuration and HUD without requiring scene edits.
- `WalkableWorldSession.Close()` saves the current world and position and invokes `onClose` once.
- Player can approach a nearby target and use a localized `Tương tác` action; first and repeat dialogue are selected from persisted flags.
- Nhung appears in Hội An; Ga Bo appears in Jeonju. Their interactions set unique story flags. A Chùa Cầu landmark interaction records a persistent cultural imprint.
- Closing/reopening or pausing the app saves and restores the checkpoint and world imprint state.

- [x] **Step 1: Add smoke assertions** for first/repeat NPC interaction, one-time imprint, world/position save and reload.
- [x] **Step 2: Build the world session runtime layer** and preserve/restore the original camera settings when the player exits.
- [x] **Step 3: Add NPC/landmark prompts, Vietnamese-first dialogue, Korean translations, and the touch interaction button.**
- [x] **Step 4: Save on close, app pause and quit** and restore a clamped checkpoint when reopening the world.
- [x] **Step 5: Compile runtime and editor assemblies**; expected: 0 compiler errors.

### Task 5: Connect the story slice to the existing app and passport

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`
- Modify: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`
- Modify: `Assets/_Game/PHASE1.md`

**Interfaces:**
- Existing map exposes a foot-exploration entry for each location; opening Jeonju requires the saved `jeonju-unlocked` flag.
- Closing exploration returns to the existing map UI with its previous profile and progress.
- Main brand, tagline, prologue and NPC labels use DẤU XỨ, Nhung and Ga Bo (가보) in both languages; no player dialogue presents Nhung/Ga Bo as the player.
- Passport displays chapter, unlocked discoveries and world imprints; existing stamps, wardrobe and postcards remain reachable.
- The old time-critical “postcard from tomorrow” finale is removed from entry/navigation for this slice; the four-chapter story replaces it in later phases.

- [x] **Step 1: Add integration smoke assertions** for map entry, Vietnamese default labels, correct NPC names, and returning from world to map.
- [x] **Step 2: Wire the two world buttons and passport progress display** through `PassportApp` without deleting current pages or changing scene/build settings.
- [x] **Step 3: Replace active Vietnamese/Korean story copy and speaker labels** with the fixed project title, tagline, Nhung/Ga Bo names and chapter-one premise.
- [x] **Step 4: Update the phase report** with implemented and deferred features plus how to open and inspect the game.

### Task 6: Verification and phase commit

**Files:**
- Verify: `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`, and existing Unity smoke flow when the editor is available.
- Review: all changed files and `git diff`.

- [x] **Step 1: Build runtime and editor C# assemblies** with the installed Unity API reference assemblies; expected: zero errors.
- [x] **Step 2: Run the project smoke flow** with a temporary profile; expected: existing six missions/save/postcards still pass, schema migration and persistent world marks pass.
- [ ] **Step 3: Inspect Git status and diff**; confirm no user-dirty, `.utmp`, cache, build, or secret files are staged.
- [ ] **Step 4: Commit only this phase’s implementation and documentation** with a concise Vietnamese commit message.
- [ ] **Step 5: Push the phase branch without force** and record the commit hash and push result.

---

## Self-review

- **Coverage:** This plan delivers the first walkable narrative slice, save migration, localized Nhung/Ga Bo interactions, and evidence-based phase reporting. The six named cultural activity mini-games, Chương 4 festival, expanded inventory/cards/postcards, full audio system, production animation, and Windows/Android packaging remain follow-on phases in the design spec.
- **Step scan:** Each implementation step names one behavior or verification outcome; the save/story, movement, interaction, integration and verification boundaries are independent enough to review.
- **Type consistency:** Story methods and session signatures are defined in Task 1/Task 4 and reused by the controller and `PassportApp`.
- **Review focus:** Invalid/old save, bad coordinates, input/bounds, repeat interaction, pause/exit are covered by the smoke checks listed above.
- **Proportion:** The plan is deliberately limited to the first playable vertical slice; it does not claim the full submission game is done.
