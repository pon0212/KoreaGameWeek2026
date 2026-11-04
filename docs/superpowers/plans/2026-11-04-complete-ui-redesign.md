# Complete UI/UX Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the existing Dấu Xứ runtime UI into a readable, mobile-first storybook adventure interface while preserving the current game flow, save data, and interactions.

**Architecture:** Keep UGUI, `PassportApp`, `WalkableWorldSession`, and the existing 3D diorama renderers. Add shared visual tokens and a small button-motion component, then replace the layout of high-traffic screens and improve the shared page primitives so every remaining page follows the same visual hierarchy. Keep current game/save services as the source of truth.

**Tech Stack:** Unity 6000.6.7f1, C#, UGUI, Input System, existing Editor smoke/capture drivers.

**Spec:** `docs/superpowers/specs/2026-11-04-complete-ui-redesign-design.md`

## Global Constraints

- Keep Unity version 6000.6.7f1 and the current UGUI/Input System implementation.
- Keep the 720 x 1280 logical canvas, portrait Android orientation, safe-area fitting, and touch targets of at least 64 reference units.
- Keep the current profile/save schema, quest routes, world session, mini-game scoring, localization, and free exploration.
- Add Vietnamese and Korean strings for all new visible text.
- Preserve the 3D scene and 3D diorama backdrops; keep the walkable world visible behind transient UI.
- Do not stage or rewrite unrelated dirty user files.
- Do not report a physical-phone test unless a device is connected and the APK is actually launched there.

## Review Focus

- Long Vietnamese or Korean labels: each layout keeps text inside safe bounds without overlap or truncation.
- 720 x 1280 and narrow/tall safe areas: primary controls remain visible and reachable.
- No saved profile, valid saved profile, and corrupted/unusable save: Start/Continue state is truthful.
- Dialogue, journal, and guide open/close: movement resumes and a pointer release cannot remain captured by the joystick.
- Locked Jeonju and unfinished activities: visuals indicate disabled state and never invoke the underlying route.

---

### Task 1: Shared design system and interaction polish

**Files:**
- Modify: `Assets/_Game/Scripts/UI/GameUiTheme.cs`
- Create: `Assets/_Game/Scripts/UI/GameUiButtonMotion.cs`
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/World/WalkableWorldSession.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs` (add layout/motion assertions if the current harness can exercise them)

**Interfaces:**
- `GameUiTheme` provides palette roles, reference dimensions, spacing, and minimum control sizes to both runtime UI builders.
- `GameUiButtonMotion` attaches to a UGUI `Button` and animates only its visual scale on press/highlight; it does not own click actions or focus.

- [ ] Write failing smoke assertions for the shared minimum touch size, button visual-state response, and screen-space bounds of the new UI.
- [ ] Run the targeted Unity smoke command and confirm the assertions fail for the current implementation.
- [ ] Add the shared palette/spacing tokens and button-motion component; keep `UiFeedbackSound` callbacks and existing click listeners intact.
- [ ] Update `PassportApp.Card`, `Button`, and `Label`, plus `WalkableWorldSession.CreateButton`, to use the shared roles and restrained paper treatment.
- [ ] Run the targeted smoke command and confirm the new assertions pass.

### Task 2: Main menu, profile entry, settings, and route map

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs` (only if needed for new paired labels)
- Test: `Assets/_Game/Scripts/Core/TastePassportSmokeDriver.cs`

**Interfaces:**
- Continue using `Navigate`, `CreateProfile`, `OpenWorld`, `RunMapObjective`, `MapObjectiveService.GetCurrent`, and existing settings/audio services.
- Preserve the existing `Page` flow and click labels used by the smoke driver where they still describe real actions.

- [ ] Add failing assertions for the single main-menu CTA, Save-backed Continue state, one active map objective, and locked Jeonju route state.
- [ ] Run the focused Unity editor smoke command and confirm the new assertions fail against the current screen hierarchy.
- [ ] Recompose `DrawMenu` as a diorama-led hero with localized game title/subtitle, exactly one Start/Continue CTA, and a restrained secondary row. Show Continue and Collection only when `profile.created` is valid; keep `start` routing through language and profile creation, and keep continue routing to a valid saved world or map.
- [ ] Recompose `DrawCreate` as a short profile step with a name field, 3 illustrated avatar choices, one save/enter action, and a real back action. Preserve existing `InputField`, character validation, and `CreateProfile` save behavior.
- [ ] Recompose `DrawMap` with `MapObjectiveService.GetCurrent` as the first actionable waypoint, Hội An/Jeonju progress cards, collapsed optional activities, and the existing finale/free-exploration route. Keep Jeonju locked until its current prerequisite is met.
- [ ] Recompose `DrawSettings` as distinct language/audio groups with a clear return action; preserve current mixer values, mute state, and persistence.
- [ ] Update stale smoke labels to click the actual localized `journey_enter_hoian` action instead of the unrelated `create` label. Run the full smoke command and verify profile creation, continuation, language, locked routes, map objectives, and existing story progression.

### Task 3: World HUD, contextual help, dialogue, and quest journal

**Files:**
- Modify: `Assets/_Game/Scripts/World/WalkableWorldSession.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs` (only if needed)
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- Keep `WorldObjectiveService.GetCurrent`, `WorldInteractable`, `InteractNearest`, `DismissDialogue`, `ToggleQuestJournal`, `OpenControlsGuide`, and `RefreshMovementAvailability` behavior.
- The world renderer remains 3D; HUD panels stay camera-fitted and inside the existing safe area.

- [ ] Add failing assertions that the HUD offers one concise current objective, sufficient touch targets, and a contextual NPC name only while in range.
- [ ] Run the world smoke command and confirm the layout assertions fail on the present large top bar/guide.
- [ ] Replace the four-wide navigation strip with compact, named map/book/help controls; keep keyboard shortcuts operational.
- [ ] Reduce the top objective surface, keep the touch stick clear at the lower left, and make the action affordance a named button at the lower right.
- [ ] Turn the long first-visit guide into a short, dismissible contextual step card; show the world behind help/journal.
- [ ] Recompose dialogue and journal cards around portrait, speaker, current objective/progress, and the actions currently available.
- [ ] Run the world smoke command and verify controls, NPC dialogue, story mutations, help/journal toggles, touch/keyboard movement, safe area, and world close/reopen.

### Task 4: Dấu Xứ passport, journal collections, and postcard pages

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs` (only if needed)
- Test: `Assets/_Game/Scripts/Core/TastePassportSmokeDriver.cs`

**Interfaces:**
- Read progress only from `PlayerProfile`, `ProgressService`, `StoryProgressService`, `InventoryService`, `PostcardCatalog`, and `OutfitCatalog`.
- Do not add save keys or write reward state from display-only pages.

- [ ] Add failing assertions for the four passport sections, earned/locked stamp state, and current save-backed inventory/postcard counts.
- [ ] Run the targeted smoke flow and confirm the passport assertions fail against the one long stacked page.
- [ ] Recompose passport as an illustrated identity page, separate Hội An/Jeonju stamp pages, and a collections page using existing data.
- [ ] Preserve navigation to inventory, culture cards, wardrobe, postcards, and the map; expose only one page of collection detail at a time.
- [ ] Add subtle book/page transition state without blocking keyboard or touch input.
- [ ] Run the end-to-end smoke command and verify profile, stamps, inventory, cards, wardrobe, postcards, and map navigation.

### Task 5: Dialogue, exploration, and all cultural activity screens

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/UI/GameUiActivityDiorama.cs` (only if layout-safe positioning is needed)
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs` (only if needed)
- Test: `Assets/_Game/Scripts/Core/TastePassportSmokeDriver.cs`

**Interfaces:**
- Keep current `ExplorationRun`, `RecipeRun`, `MatchRun`, `LanternCraftRun`, `HanjiCraftRun`, and `FestivalPreparationRun` actions, score calculations, and completion transactions.
- Keep `FragmentDrag`, `LanternCraftDrag`, and `FragmentDropSlot` input ownership.

- [ ] Add failing smoke assertions for a single instruction, visible step progress, activity-specific visual focal point, and completion state across architecture, cooking, matching, lantern, hanji, festival, and result screens.
- [ ] Run the end-to-end activity smoke command and confirm the visual assertions fail against the current repeated-header/button stack.
- [ ] Recompose activity start/dialogue pages as compact character cards with localized, skippable or listenable narrative.
- [ ] Recompose exploration/assembly, recipe, matching, lantern, hanji, and festival screens around their actual artwork, current step, and existing touch/drag controls.
- [ ] Keep success/retry states explicit, show the earned stamp/reward, and return to world exploration or the map through a real action.
- [ ] Run the full existing gameplay smoke command; verify each activity produces the same saved stamp, reward, and story flag as before.

### Task 6: Responsive visual capture, regression review, and Android build

**Files:**
- Modify: `Assets/_Game/Editor/TastePassportUiCapture.cs` (only if capture coverage is missing)
- Modify: `Assets/_Game/Scripts/Core/TastePassportUiCaptureDriver.cs` (only if capture coverage is missing)
- Test: existing `TastePassportSmoke`, `TastePassportWorldSmoke`, and `TastePassportUiCapture` commands

**Interfaces:**
- Use the current Unity editor version, project scene, profile isolation variables, and Android build method `TastePassport.EditorTools.TastePassportBuild.BuildAndroid`.

- [ ] Capture the menu, profile, map, world HUD, dialogue, journal, passport, and representative activity screens at portrait reference resolution.
- [ ] Inspect captures for cropped text, overlapping controls, poor contrast, incorrect Vietnamese/Hangul, and controls outside safe bounds; fix any blocker.
- [ ] Run `TastePassportWorldSmoke.Run` and the full `TastePassportSmoke.Run` under isolated temporary profiles; record actual pass/fail logs.
- [ ] Build Android using the existing build method and confirm APK package metadata and file timestamp/hash.
- [ ] Check `git diff --check`, review the exact staged file list, and leave unrelated user changes unstaged.
- [ ] If a physical Android device is attached, install and launch the APK; otherwise report the phone layout as not verified on device.
