# 2D Card Adventure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a new player complete the full Dấu Xứ story using simple 2D location, character, dialogue, activity, and reward cards, without requiring joystick movement.

**Architecture:** Keep `PassportApp` as the page owner and `MapObjectiveService` as the story objective selector. Extract world interaction side effects into a reusable service so card interactions and optional 3D free roam share the exact flags and rewards. Route only required campaign interactions through the card flow; retain existing activity runs and the optional 3D stroll.

**Tech Stack:** Unity 6000.6.7f1, C#, UGUI, Input System, existing PlayerProfile/ProfileStore, Unity Editor smoke and UI capture drivers.

**Spec:** `docs/superpowers/specs/2026-11-04-2d-card-adventure-design.md`

## Global Constraints

- Keep the existing Unity scene, UGUI, Input System, save format, localization service, activity run models, and story order.
- Every required story step must work by touch without entering `WalkableWorldSession` or using a keyboard shortcut.
- Use the existing 720 x 1280 logical reference, safe-area fitting, and at least 64 logical units for tappable controls.
- Add paired Vietnamese and Korean copy for every new player-facing string.
- Save interaction changes from a candidate profile before changing the visible story state.
- Preserve optional 3D free roam as a secondary route; it must not gate campaign progress or the ending.
- Do not stage or commit unrelated dirty workspace files.

## Review Focus

- **Legacy saves partway through the story:** objective selection must resume from existing flags and stamps. Pin with `RunCardObjectiveChecks` (Task 2).
- **Repeated NPC/landmark taps:** repeat dialogue must not grant duplicate rewards. Pin with `RunCardInteractionChecks` (Task 1).
- **Save failure during a card interaction:** do not advance the profile or hide the retry path. Pin with `RunCardAdventureChecks` (Task 3).
- **Locked Jeonju and post-ending state:** keep Jeonju locked until its existing prerequisites and make free roam optional after completion. Pin with `RunCardObjectiveChecks` (Task 2).
- **VI/KO copy and portrait safe areas:** cards remain readable and tappable at both portrait capture sizes. Pin with `RunCardAdventureUiChecks` (Task 4).

---

### Task 1: Share NPC and landmark interaction state changes

**Files:**
- Create: `Assets/_Game/Scripts/World/WorldInteractionService.cs` and `.meta`
- Modify: `Assets/_Game/Scripts/World/WorldInteractable.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- Produces: `WorldInteractionService.Interact(PlayerProfile profile, WorldInteractableDefinition definition) -> WorldInteractionResult`
- `WorldInteractable.Interact(PlayerProfile)` delegates to the service; card and 3D routes use the same implementation.

- [ ] **Step 1: Write the failing interaction test**

Add `RunCardInteractionChecks` to resolve the service contract through reflection (so the old project still compiles before the service exists), then assert Nhung's first interaction returns `prologue_nhung_first`, records `nhung-intro-seen` and `dau-xu-book-discovered`, and advances to chapter 2. Assert a repeat returns `nhung_repeat` without duplicate flags. Assert a Jeonju landmark interaction adds its existing culture card once, and `hanji-display` remains unavailable until `hanji-display-ready` is recorded.

- [ ] **Step 2: Run the test and verify the expected failure**

Run Unity batch mode with `-executeMethod TastePassport.EditorTools.TastePassportWorldSmoke.RunCardInteractionChecks`.
Expected: FAIL at the explicit missing-service assertion, not at compilation.

- [ ] **Step 3: Extract the existing interaction rules**

Move the state mutations from `WorldInteractable.Interact` into `WorldInteractionService.Interact`; retain the current dialogue-key, first-visit, chapter, discovery, collection-card, and `RequiredImprintId` gate rules. Make the component delegate to it.

- [ ] **Step 4: Run focused tests**

Run `RunCardInteractionChecks` and `RunPhaseFiveStoryChecks`.
Expected: both PASS; the old world route and new service return the same first/repeat dialogue and saved effects.

- [ ] **Step 5: Commit the interaction service**

Stage only the service, component, and smoke file; commit as `refactor(world): share story interaction service`.

### Task 2: Select card targets from current story state

**Files:**
- Modify: `Assets/_Game/Scripts/World/MapObjectiveService.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- Add `MapObjectiveAction.StoryCardTarget` while retaining `ExploreWorld` for optional free roam.
- Add nullable `TargetId` to `MapObjectiveDefinition`; card objectives provide both `WorldId` and `TargetId`.

- [ ] **Step 1: Write the failing objective test**

Add `RunCardObjectiveChecks` using reflection for the new target field so the pre-change assembly still compiles. Assert fresh profile -> `hoian/nhung`; after `nhung-intro-seen` -> `hoian/lan`; before lantern craft readiness -> Lan's craft card; after `lantern-display-ready` -> `hoian/lantern-display`. Assert a profile with existing Hoi An stamps advances to Jeonju without replaying completed objectives, and a completed profile receives `ExploreWorld` only as optional free roam.

- [ ] **Step 2: Run the test and verify the expected failure**

Run Unity batch mode with `-executeMethod TastePassport.EditorTools.TastePassportWorldSmoke.RunCardObjectiveChecks`.
Expected: FAIL at the explicit missing `TargetId` assertion, not at compilation.

- [ ] **Step 3: Add target identity to story objectives**

Add the action and `TargetId`; map the current Meet Nhung, Meet Lan, lantern craft handoff, and lantern display objectives to their `WorldCatalog` target IDs. Keep mission/recipe/match/craft/festival actions and save-based unlock rules intact.

- [ ] **Step 4: Run objective and progression checks**

Run `RunCardObjectiveChecks` and `RunPhaseFiveStoryChecks`.
Expected: PASS for fresh profiles, migrated progress, Jeonju lock/unlock, and completed-story optional free roam.

- [ ] **Step 5: Commit the objective contract**

Stage only `MapObjectiveService.cs` and its smoke assertions; commit as `feat(world): expose story card objective targets`.

### Task 3: Persist direct card interactions safely

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- Add a private `OpenStoryCard(string worldId, string targetId)` route that resolves the `WorldCatalog` definition, interacts through `WorldInteractionService`, and persists via the existing candidate-save method before showing dialogue.
- Expose internal page state only as needed by the editor smoke test; do not add a second profile or story store.

- [ ] **Step 1: Write the failing persistence/navigation test**

Add `RunCardAdventureChecks` to resolve `OpenStoryCard` by reflection and exercise a successful first interaction and a failed-save interaction. Assert the successful path stores flags and opens the first dialogue; assert a save failure leaves the original profile unchanged, keeps the target retryable, and surfaces the localized save error.

- [ ] **Step 2: Run the test and verify the expected failure**

Run Unity batch mode with `-executeMethod TastePassport.EditorTools.TastePassportWorldSmoke.RunCardAdventureChecks`.
Expected: FAIL at the explicit missing card-route assertion, not at compilation.

- [ ] **Step 3: Implement safe direct card routing**

Resolve targets only within the objective's `WorldId`; clone the current profile, call `WorldInteractionService.Interact`, save with `TryPersistCandidate`, and update visible state only after the save succeeds. Preserve the first/repeat dialogue returned by the service.

- [ ] **Step 4: Run the save and interaction checks**

Run `RunCardAdventureChecks`, `RunCardInteractionChecks`, and `RunProfileAndStoryChecks`.
Expected: PASS; failed writes do not consume the first-visit interaction, and retries do not duplicate rewards.

- [ ] **Step 5: Commit safe card routing**

Stage only `PassportApp.cs` and its smoke assertions; commit as `feat(story): route card interactions through saved profiles`.

### Task 4: Build the 2D journey, target, and dialogue cards

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`
- Test: `Assets/_Game/Scripts/Core/TastePassportSmokeDriver.cs`

**Interfaces:**
- Add page states/draw methods for the location target and NPC/landmark dialogue card; activity and reward routes continue to call existing `StartLevel`, `StartRecipe`, `StartMatch`, `StartLanternCraft`, `StartHanjiCraft`, and result pages.
- Required card actions use existing `Button`/`CompactButton` helpers and remain at least 64 logical units tall.

- [ ] **Step 1: Update runtime smoke to expect a card-first new game**

Change the full smoke's profile-creation stage to assert the app opens the journey card, no `WalkableWorldSession` is active, and the first action opens Nhung's target/dialogue card. Assert the new screen has one primary action and no joystick requirement. Run the smoke now; expect it to fail against the old entry route before changing `PassportApp`.

- [ ] **Step 2: Add a failing UI layout/localization check**

Add `RunCardAdventureUiChecks` to inspect the target and dialogue cards at 720x1280, verify paired Vietnamese/Korean labels and portrait visibility, and assert every campaign button is at least 64x64 and inside the safe area. Run it and verify failure before implementation.

- [ ] **Step 3: Implement card pages and copy**

Make the new-game completion route navigate to the journey card instead of `OpenWorld`. Put the active objective and one primary action first, with a direct route to the current target card. Keep Hội An and Jeonju as compact location cards; open a destination's relevant NPC, landmark, and activity cards only after tapping that destination. Add target detail cards from `WorldCatalog`, dialogue cards from `WorldInteractionResult`, Continue/optional Ask More controls, and a direct activity button when the target has an activity. Keep non-current activities under a quieter secondary route, show locked routes as disabled, and add paired VI/KO copy for new controls. Keep the optional 3D free-roam action secondary and post-story.

- [ ] **Step 4: Run card UI and menu/map checks**

Run `RunCardAdventureUiChecks`, `TastePassportSmokeDriver.RunMenuMapChecks`, and `TastePassportWorldSmoke.RunSharedUiChecks`.
Expected: PASS in Vietnamese and Korean, with safe-area targets and a single primary story action.

- [ ] **Step 5: Commit the card screens**

Stage only `PassportApp.cs`, `GameText.cs`, and the related test updates; commit as `feat(ui): add 2d story and npc cards`.

### Task 5: Capture every card state at phone resolutions

**Files:**
- Modify: `Assets/_Game/Scripts/Core/TastePassportUiCaptureDriver.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

- [ ] **Step 1: Add a failing capture coverage assertion**

Extend `RunCardAdventureUiChecks` to require capture states for journey, target, dialogue, activity, reward, passport, and ending. Before the driver is updated, run the check and confirm the missing card-state assertion fails.

- [ ] **Step 2: Update UI capture for card states**

Capture the journey, target, dialogue, activity, reward, passport, and ending cards in Vietnamese and Korean, at 720x1280 and 1080x2400. Ensure captures do not activate the optional world session.

- [ ] **Step 3: Run full story and capture checks**

Run the full `TastePassportSmoke.Run` and `TastePassportUiCapture.Run` entrypoints.
Expected: the story smoke reaches the ending and saves all expected rewards; capture reports every specified card page at both resolutions.

- [ ] **Step 4: Commit the capture updates**

Stage only `TastePassportUiCaptureDriver.cs` and capture assertions; commit as `test(ui): capture 2d card adventure states`.

### Task 6: Build and verify the Android package

**Files:**
- No source file changes unless the build exposes a defect.
- Output: `Builds/Android/TastePassport.apk`

- [ ] **Step 1: Run the relevant complete smoke suite**

Run the new card campaign/UI tests plus existing phase, map, canvas-depth, and optional world HUD checks.
Expected: all relevant Unity checks PASS.

- [ ] **Step 2: Build Android**

Run Unity batch mode with `-executeMethod TastePassport.EditorTools.TastePassportBuild.BuildAndroid`.
Expected: `Taste Passport APK ready` and a fresh APK at `Builds/Android/TastePassport.apk`.

- [ ] **Step 3: Verify repository scope and report device limits**

Run `git diff --check` on the task changes and `adb devices -l`. Report whether a physical device was actually available; never infer touch verification from a successful build.

## Execution handoff

The user selected subagent-driven implementation earlier in this project. After plan approval, execute task-by-task using `superpowers:subagent-driven-development`, committing only files from each task.
