# Main Story Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete and persist the four-chapter story, seven-corner festival, ending, and post-story free exploration.

**Architecture:** Extend the existing `PassportApp`, `StoryProgressService`, and schema 3 profile. A focused festival model validates mark selection and station placement; story flags and world imprints persist each correct placement so the activity resumes after navigation or reload.

**Tech Stack:** Unity 6000.6.7f1, C#, uGUI, Unity Editor smoke methods, .NET build.

**Spec:** `docs/superpowers/specs/2026-11-03-main-story-design.md`; user master prompt, Part III, IV, VII, IX, XII Phase 5, and XIII.

## Global Constraints

- Use the currently open Unity project and existing scenes; do not add a new Scene or package.
- Keep offline save schema 3 and migrate without losing existing progress.
- Keep Vietnamese as default and preserve Korean localization.
- Preserve Hoi An and Jeonju free exploration after the main story.

## Review Focus

- Missing mission/craft prerequisites keep the festival locked.
- Wrong or repeated station placement does not create saved progress.
- Leaving and reopening the festival restores every correct placement.
- A completed story cannot move back to an earlier chapter.
- A finished ending returns to a playable map and preserves the completed flag.

---

### Task 1: Persistent story progression

**Files:**
- Modify `Assets/_Game/Scripts/Story/StoryProgressService.cs`.
- Modify `Assets/_Game/Scripts/World/WorldInteractable.cs`.
- Modify `Assets/_Game/Scripts/Core/PassportApp.cs`.
- Test `Assets/_Game/Editor/TastePassportWorldSmoke.cs`.

**Interfaces:**
- Add `StoryProgressService.CompleteMainStory(PlayerProfile profile) -> bool`; it stores chapter 4, `mainStoryCompleted`, `main-story-completed`, and `festival-completed` idempotently.
- Meeting Nhung advances to chapter 2; the Hoi An milestone continues to unlock Jeonju and chapter 3.
- `PassportApp.FirstJourneyComplete()` requires six mission stamps plus `lantern-display-ready` and `hanji-display-ready` imprints.

- [x] Write assertions for monotonic chapters, idempotent completion, save/reload, and the complete-journey prerequisite.
- [x] Run the focused Unity story checks and record the expected missing-completion failure.
- [x] Implement story transitions without changing the save schema.
- [x] Run focused story checks and confirm saved chapter/completion state.

### Task 2: Festival placement model

**Files:**
- Create `Assets/_Game/Scripts/Gameplay/FestivalPreparationRun.cs` and Unity metadata.
- Test `Assets/_Game/Editor/TastePassportWorldSmoke.cs`.

**Interfaces:**
- Add `FestivalPlacementResult` values for missing selection, invalid mark, wrong station, already placed, and placed.
- Add a seven-station run with validated mark selection, station placement, one of three chosen decorations per station, three cross-cultural links, and profile-backed restoration.
- Persist placement as `festival-corner-{stationId}-ready`, decoration as `festival-decor-{stationId}-{decorationId}`, and links as `festival-link-{pairId}` imprints.
- Station IDs: `hoian`, `jeonju`, `food`, `craft`, `arts`, `exchange`, `memory`.
- Mark mapping by station: `{0, 4, 1, 3, 2, 5, 6}`.
- Link pairs: `chuacau` ↔ `hanok`, `caolau` ↔ `bibimbap`, and `lantern` ↔ `hanji`.

- [x] Add assertions for invalid marks, wrong stations, duplicate placements, all seven correct placements, and reloading partial progress.
- [x] Run the focused Unity model check and record its expected missing-model failure.
- [x] Implement the small placement model and profile restoration.
- [x] Run focused checks and confirm each guard and completion result.

### Task 3: Map, festival, and ending UI

**Files:**
- Modify `Assets/_Game/Scripts/Core/PassportApp.cs`.
- Modify `Assets/_Game/Scripts/Localization/GameText.cs`.
- Test `Assets/_Game/Editor/TastePassportWorldSmoke.cs`.

- [x] Add checks for map gating, Vietnamese/Korean labels, persisted placement/decor/links, festival friend sprites, and the post-story map.
- [x] Add the locked/ready festival map card, seven item choices and station buttons, per-station decoration choices, and a three-pair activity connection board.
- [x] Save every placement, decoration, and valid link transactionally and resume from saved imprints.
- [x] Replace the random timer puzzle with a localized, sequentially animated ending page, four returning NPCs, story narration, and a free-exploration button.
- [x] Add visible completed-story status to the passport/map and run focused Unity checks.

### Task 4: Full-story integration and report

**Files:**
- Modify `Assets/_Game/Scripts/Core/TastePassportSmokeDriver.cs`.
- Modify `Assets/_Game/Editor/TastePassportWorldSmoke.cs`.
- Create `Assets/_Game/PHASE5.md`.

- [x] Extend the integrated smoke through the seven stations, persisted completion, ending, and both free-exploration entries.
- [x] Run .NET build, focused story/festival checks, and the integrated Unity smoke; require explicit PASS output.
- [x] Record the remaining visual/platform limits in the Phase 5 report.
- [ ] Stage only Phase 5 files, commit in Vietnamese, and push without force.
