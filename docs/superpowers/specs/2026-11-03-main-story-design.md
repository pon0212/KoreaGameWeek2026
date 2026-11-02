# Phase 5 — Main Story Design

## Intent

Finish the four-chapter DẤU XỨ story in the current Unity project. The player must be able to unlock the cultural festival after completing the six core missions and both craft activities, prepare seven festival corners, see the ending, and return to free exploration. Vietnamese stays the default; Korean remains available.

## Existing state

The project already has Chapter 1–3 dialogue, Hoi An and Jeonju missions, a `chapterIndex`, a `mainStoryCompleted` save field, and an ending page. The ending is currently inaccessible from the map, tests only one randomized stamp, and never persists story completion. Hoi An lantern and Jeonju Hanji products already persist as world imprints.

## Approaches

1. **Extend the current flow and save schema (chosen).** Reuse the current map, `PassportApp`, schema 3, and story/imprint lists. Add a small festival placement model, persist each corner, and complete the existing story flag only after all seven placements. This fits the current product and keeps old saves readable.
2. **Build a separate festival Scene and new story manager.** This could support a larger event world later, but it adds scene routing and save migration before the requested story can be finished.
3. **Keep the single-answer ending page.** This is the smallest change, but it does not implement collecting and arranging festival exhibits or persistent world changes.

## Design

- Chapter 1 begins at chapter index 1. Meeting Nhung records the book discovery and advances to Chapter 2.
- Completing the three Hoi An milestones unlocks Jeonju and advances to Chapter 3. The six core stamps and saved lantern/Hanji products are required before the festival opens.
- The map exposes a locked festival entry with a localized explanation until those prerequisites are complete.
- The festival has seven stations: Hoi An, Jeonju, food, craft, performance, cultural exchange, and shared memories. The player selects one of seven collected marks and places it at its matching station. Wrong placement does not change progress.
- Each station then accepts one player-chosen decoration from a small set of reusable festival decorations. Each completed placement and decoration is saved immediately as a story flag and world imprint, so the player can leave and resume.
- A final connection board pairs the three shared activity types already playable in both lands: architecture (Chùa Cầu ↔ Hanok), food (Cao Lầu ↔ Bibimbap), and craft (lantern ↔ Hanji). A wrong pair is not recorded.
- The placement model restores station choices, decoration, and connections from the profile. Completion requires seven placements, seven decorations, and all three cross-cultural connections.
- Completing the event stores `mainStoryCompleted`, chapter 4, a story flag, and a festival completion imprint. The ending reveals the festival scene, then Nhung, Ga Bo, Cô Lan, and Bác Kim, then the narration with a short UI fade/scale sequence. It offers narration plus a direct return to free exploration.
- No new Scene or save schema is needed. The festival uses the existing UI, localized strings, story flags, and imprints.

## Constraints and limits

- Use Unity 6000.6.7f1 and the existing offline profile format.
- Keep Vietnamese as the default and Korean as an available language.
- Do not add paid progression or a countdown to the festival.
- The festival is a touch-friendly UI staging activity in the current 2D project; this phase does not add a separate festival Scene or video asset. The ending uses an animated UI reveal rather than a pre-rendered cutscene.

## Acceptance

- Story chapters progress monotonically and completion survives save/reload.
- The festival is gated on all six missions and the lantern/Hanji craft imprints.
- All seven station placements are correctable, unique, and individually persistent.
- Completion reveals the ending and keeps both destination exploration buttons available.
- Focused Unity checks and the integrated smoke complete the full story in Vietnamese; Korean story strings resolve.
