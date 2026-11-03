# Game UI & NPC Interaction Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Redesign all player-facing screens of *Dấu Xứ Việt Hàn* and make NPC conversations clear and actionable while preserving the existing linear story and saves.

**Architecture:** Keep the existing Unity scene, runtime uGUI, `PassportApp` page flow, and `WalkableWorldSession`. Add shared UI tokens, restyle screens in cohesive groups, and extend transient NPC dialogue data with optional localized follow-up copy. Do not alter gameplay models, story flags, rewards, unlock conditions, or save schema.

**Tech Stack:** Unity 6000.6.7f1, C#, uGUI/EventSystem, existing Input System, `GameText`, existing art and Noto fonts.

**Spec:** `docs/superpowers/specs/2026-11-03-game-ui-npc-redesign-design.md`

## Global Constraints

- Android portrait is primary; Windows keyboard/mouse remains supported.
- Keep the current scene and runtime uGUI; add no packages or network services.
- Keep Vietnamese default/fallback and add equivalent Korean for every new UI string.
- Preserve six mission loops, rewards, IDs, unlock conditions, offline saves, and existing profile data.
- Do not add relationship meters, branching outcomes, or save fields.
- Preserve unrelated edits already present in the working tree; only stage files listed in this plan if a commit is later requested.
- Automated tests are not part of this plan. Compile checks and the requested playable build are the planned verification outputs.

## Review Focus

- **Safe area / narrow portrait:** controls and primary actions remain visible and separated from cutouts and system bars.
- **Long Korean/Vietnamese copy:** text wraps without hiding speaker names, quest actions, or close buttons.
- **No nearby target vs. target in range:** the interaction prompt communicates what to do and names the correct character/object.
- **NPC with an activity vs. NPC without one:** starting an activity is explicit and optional; ordinary conversation can close safely.
- **Returning profiles and locked destinations:** existing progress and unlock explanations remain legible and unchanged.

## File Map

- Create `Assets/_Game/Scripts/UI/GameUiTheme.cs`: shared palette, spacing, type hierarchy, and minimum control sizes.
- Modify `Assets/_Game/Scripts/Core/PassportApp.cs`: onboarding, menu, map, passport, activity, result, collection, wardrobe, and settings layouts.
- Modify `Assets/_Game/Scripts/World/WalkableWorldSession.cs`: in-world HUD, first-time controls guide, target prompt, NPC dialogue and activity actions.
- Modify `Assets/_Game/Scripts/World/WorldInteractable.cs` and `WorldCatalog.cs`: optional dialogue key on immutable target definitions; keep story mutation behavior unchanged.
- Modify `Assets/_Game/Scripts/Localization/GameText.cs`: Vietnamese and Korean labels and optional NPC dialogue copy.

---

### Task 1: Shared visual system

**Files:**
- Create: `Assets/_Game/Scripts/UI/GameUiTheme.cs`
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/World/WalkableWorldSession.cs`

**Interfaces:**
- `GameUiTheme` exposes shared `Color` values `Cream=#F6E9D4`, `Paper=#FFF9EE`, `Ink=#352820`, `Jade=#357E84`, `Lantern=#E6A14D`, `Vermilion=#A94A42`, `Border=#D9C6A7`, and `Muted=#51463D`, plus `const float MinimumTapTarget = 64f` in the existing 720 × 1280 reference-space units.
- Both runtime UI builders consume the same values; existing private layout helpers remain the construction mechanism.

- [ ] Create `GameUiTheme` with the approved existing palette and minimum control size.
- [ ] Replace duplicate palette declarations in `PassportApp` and `WalkableWorldSession` with the shared values without changing page behavior.

### Task 2: Start flow, journey map, and progress navigation

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`

**Interfaces:**
- Keep the existing `Page` enum and `Navigate(Page)` routing.
- `MapObjectiveService.GetCurrent(...)`, mission callbacks, and existing unlock checks remain the source of truth for the map's next-step card.
- All added labels use `GameText` keys for `vi` and `ko`.

- [ ] Redesign `DrawMenu`, `DrawLanguage`, and `DrawCreate` around one prominent next action, clearer first-run setup, and compact access to profile/passport/settings.
- [ ] Redesign `DrawMap` with one focused next-step card, two destination sections, grouped activity actions, and readable locked-state explanations; retain the existing mission callbacks and requirements.
- [ ] Add or revise localized keys for navigation, destination sections, progress, and locked-state helper copy in both languages.

### Task 3: Passport, collections, and settings

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`

**Interfaces:**
- Keep existing save-backed data sources: `PlayerProfile`, `ProgressService`, `InventoryService`, and `PostcardCatalog`.
- Keep the existing page callbacks for wardrobe, inventory, culture cards, postcards, settings, and return navigation.

- [ ] Redesign `DrawPassport`, `DrawPostcard`, `DrawInventory`, `DrawCultureCards`, `DrawWardrobe`, and `DrawSettings` to share page headers, status labels, card spacing, action hierarchy, and clear return actions.
- [ ] Preserve all current stamp, coin, item, culture-card, outfit, postcard, and settings values and callbacks.
- [ ] Add or revise equivalent Vietnamese/Korean labels for collection states, settings groups, and navigation.

### Task 4: World HUD and NPC conversation flow

**Files:**
- Modify: `Assets/_Game/Scripts/World/WorldInteractable.cs`
- Modify: `Assets/_Game/Scripts/World/WorldCatalog.cs`
- Modify: `Assets/_Game/Scripts/World/WalkableWorldSession.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`

**Interfaces:**
- Extend the constructor to `WorldInteractableDefinition(..., string activityId = null, string requiredImprintId = null, string optionalDialogueKey = null)` and expose `readonly string OptionalDialogueKey`; existing landmark definitions continue to use the defaults.
- Add private view actions in `WalkableWorldSession`: `ShowOptionalDialogue()`, `RestorePrimaryDialogue()`, and `StartDialogueActivity()`; these do not mutate `PlayerProfile`.
- Retain `InteractNearest()`, `ContinueDialogue()`, `DismissDialogue()`, `RefreshCurrentObjective()`, and the current callback for activity launch.
- Story completion remains controlled by existing story flags and imprints; optional dialogue is session-only.

- [ ] Add optional dialogue keys to NPC definitions and localized character/lore copy in Vietnamese and Korean; do not add new factual claims about culture.
- [ ] Redesign `CreateHud`, `CreateQuestJournal`, and `CreateControlsGuide` into a compact objective card, named contextual action, clearly accessible help/passport/journal buttons, and safe-area-aware touch controls.
- [ ] Redesign the dialogue panel to show speaker portrait/name/copy, a continue/close action, an optional “Hỏi thêm” action when a key exists, and a distinct activity CTA only for NPCs with `ActivityId`.
- [ ] Keep movement paused during dialogue, resume on close, and route keyboard input to the same visible actions as touch.

### Task 5: Activities, instructions, and result screens

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`

**Interfaces:**
- Keep existing gameplay models and result commits: `ExplorationRun`, `RecipeRun`, `MatchRun`, `LanternCraftRun`, `HanjiCraftRun`, and `FestivalPreparationRun`.
- Keep all current navigation callbacks and transactional save behavior.

- [ ] Apply the shared screen hierarchy to `DrawDialogue`, `DrawExplore`, `DrawAssembly`, recipe, match, craft, finale, and their result views: concise instruction, visible step/progress, one primary action, and readable feedback.
- [ ] Preserve drag/tap alternatives, timers, answer evaluation, score/stars, rewards, and replay behavior.
- [ ] Keep result screens explicit about earned items/stamps and the next available action.

### Task 6: Compile and produce the refreshed playable build

**Files:**
- No new product files. Build outputs: `Builds/Windows/` and `Builds/Android/`.

**Interfaces:**
- Use the existing scene and `TastePassportBuild` editor entry points; no save migration or project package change.

- [ ] Fast-forward the reviewed feature branch into the original project checkout, preserving its unrelated local edits.
- [ ] Run `dotnet build Assembly-CSharp.csproj -v:q` and `dotnet build Assembly-CSharp-Editor.csproj -v:q` in the original project checkout, where Unity's generated project references and Library cache are available; expected: both complete without C# errors.
- [ ] Build Windows and Android players using the existing Unity build entry points; expected: new build outputs include the UI changes.
- [ ] Launch the Windows player for the visible preview previously requested; report separately if Unity license/module availability prevents either platform build.

## Plan Self-Review

- Spec coverage: global tokens (Task 1); first run/menu/map (Task 2); passport/collections/settings (Task 3); HUD/tutorial/NPC conversations (Task 4); all six activities and results (Task 5); platform outputs and visible Windows preview (Task 6).
- State and input risks: Task 4 preserves save/story mutation paths, adds no persistent conversation choice, and keeps touch/keyboard actions aligned. Tasks 1–5 preserve current page callbacks and gameplay models.
- Scope: all work is runtime UI/localization in the current project; no package, scene, content system, save schema, or gameplay-loop rewrite is included.
