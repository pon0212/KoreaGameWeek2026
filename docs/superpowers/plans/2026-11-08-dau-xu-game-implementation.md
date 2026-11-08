# Dấu Xứ Việt Hàn — implementation plan

## Baseline

- Preserve the current Unity 6 / URP 2D project and the existing dirty working tree.
- Baseline `dotnet build TastePassport_KGW2026.sln -c Debug --no-restore`: passed after `dotnet restore` (0 warnings, 0 errors).
- Baseline Unity headless smoke: passed profile creation, movement, both destinations, six current mission records, crafts, festival, saves, ending and free exploration. Headless RenderGraph warnings mean visual rendering is not verified.
- Primary runtime is `PassportApp`; world session, save, story and mini-game logic are already separate services/classes. Avoid a broad rewrite.
- Source specifications currently live in the sibling checkout `D:\NCKH\GameVNKorea\KoreaGameWeek2026\TastePassport_Main`, not this project. Read the master prompt, story, draw.io/SVG use cases and all nine art references there.

## Phases

1. **Audit and traceability:** document every use case, current entry point, implementation, saved state, test evidence and status. Record architecture and assumptions.
2. **Playable Hoi An slice:** retain the verified New Game → walk → Nhung/Lan → interactive lantern craft → saved item/imprint → lantern shown on a reloaded world route. Close any gaps found in the save/display path.
3. **Two cultural worlds:** keep both walkable destinations and local NPC/story entry points; make Chùa Cầu and Hanok interactions launch their respective genuine puzzle activities.
4. **Six activities and journal:** make all six activities culturally distinct and result-driven; add persistent photo capture, gallery/journal and page decoration from unlocked memories/items. Replay must not grant first-completion rewards again.
5. **Festival and postgame:** persist owned-item display choices, unlock only after requirements, reflect choices in the ending and retain both-world exploration/replay afterward.
6. **Support and release checks:** validate migration, backup recovery, localization, independently saved audio, narration/TTS fallback, pause/repeated input, PC controls and Android build availability.

## Work rules

- Implement incrementally in the current checkout; keep unrelated user changes intact.
- For each functional change, add a failing check first, implement it, then run the focused check and project build/smoke as appropriate.
- Do not label a use case DONE until the entry point, behavior, saved state, test and regressions are verified.
- Update `docs/TEST_REPORT.md` with exact commands and observed results after each verified phase.
