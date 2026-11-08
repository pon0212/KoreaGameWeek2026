# Jeonju Cultural Activities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the Jeonju chapter’s Hanok, Hanji, and Bibimbap activities with localized cultural cards and saved world displays.

**Architecture:** Extend the existing `WorldCatalog`, `InventoryService`, `PlayerProfile`, `PassportApp`, and editor smoke harness. Keep craft rules in small deterministic gameplay models; store completed products and cultural cards in the existing schema 3 collections and use world imprints to control persistent displays.

**Tech Stack:** Unity 6000.6.7f1, C#, uGUI/EventSystem, Unity Editor smoke checks, .NET build.

**Spec:** User-pasted master prompt, Sections II–IV, VII–VIII, X, and XII Phase 4 — `C:\Users\Tran Gia Bao\.codex\attachments\a5f79fd0-9e5a-42a2-a56d-80402910ec48\Văn bản đã dán.txt`.

## Global Constraints

- Keep Unity version 6000.6.7f1 and do not change packages or create another Unity project.
- Android is primary; preserve touch input and support Windows keyboard/mouse testing.
- Preserve offline saves and Vietnamese/Korean localization; Vietnamese remains the default.
- Use the existing scene, save schema, and gameplay patterns; do not remove or overwrite user-owned changes.
- Cultural statements must be source-backed; describe the 2026 Hanji UNESCO nomination as pending, not inscribed.
- Every completed activity must save its collection reward and world imprint so it remains after reload.

## Review Focus

- **Legacy schema 1/2/3 saves:** migration keeps prior progress and initializes or preserves collections (Task 1 tests).
- **Repeated and full-inventory Hanji grants:** no duplicates or partial story rewards when capacity is exhausted (Task 1 tests).
- **Out-of-order Hanji actions and invalid variants:** reject them without saving a product (Task 3 tests).
- **Korean language and long card copy:** names, dialogue, instructions, and source labels resolve in Korean (Task 2/4 tests).
- **Reloaded Jeonju displays:** Hanji display is hidden before completion and visible after a saved craft (Task 2/3 tests).

---

### Task 1: Jeonju collection records

**Files:**
- Modify: `Assets/_Game/Scripts/Save/InventoryService.cs`
- Test: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- Add IDs `culture-jeonju-hanok`, `culture-jeonju-hanji`, `culture-jeonju-bibimbap`, `culture-jeonju-gyeonggijeon`, and `culture-jeonju-pansori`.
- Add `RecordHanjiCraft(PlayerProfile profile, string shapeId, string colorId, string motifId) -> bool`, `GetLatestCraftedHanji(PlayerProfile profile) -> string`, and validated Hanji variant readers.
- Hanji products use `crafted-hanji-{shapeId}-{colorId}-{motifId}`; successful creation grants the Hanji card and `hanji-display-ready` imprint.
- Valid shapes are `fan`, `bookmark`, and `flower`; colors are `indigo`, `vermilion`, `gold`, and `jade`; motifs are `flower`, `cloud`, and `stripe`.

- [x] **Step 1: Write failing editor smoke assertions** in `RunJeonjuInventoryChecks` for valid Hanji rewards, invalid variants, duplicate grants, full inventory, five culture card IDs, and reload persistence.
- [x] **Step 2: Run `TastePassport.EditorTools.TastePassportWorldSmoke.RunJeonjuInventoryChecks`** and confirm it fails on the missing Jeonju records.
- [x] **Step 3: Implement bounded, idempotent Jeonju record methods** in `InventoryService` using the existing 128-item collection limit.
- [x] **Step 4: Run `RunJeonjuInventoryChecks`** and confirm each assertion passes.

### Task 2: Jeonju world and sourced content

**Files:**
- Modify: `Assets/_Game/Scripts/World/WorldCatalog.cs`
- Modify: `Assets/_Game/Scripts/World/WorldInteractable.cs`
- Modify: `Assets/_Game/Scripts/Gameplay/LevelDefinition.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`
- Modify: `Assets/_Game/Resources/Art/ASSET_PROVENANCE.md`
- Create: `Assets/_Game/Resources/Art/Characters/BKim.png` and Unity metadata
- Modify: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`
- Create: `docs/jeonju-cultural-sources.md`

**Interfaces:**
- Add Bác Kim (`ActivityId="hanji-craft"`), Gyeonggijeon, Pansori, and an imprint-gated Hanji display to Jeonju interactables.
- Replace generic Hanok puzzle labels with roof tiles, wooden doors, Hanji windows, courtyard, eaves, and stone wall details.
- Unlock the Hanok card on world discovery or successful architecture mission; unlock Gyeonggijeon and Pansori cards on their respective first discovery.

- [x] **Step 1: Add failing world smoke assertions** in `RunJeonjuWorldChecks` for NPC/site IDs, activity routing, imprint-gated display, VI/KO dialogue, and Hanok object keys.
- [x] **Step 2: Run `TastePassport.EditorTools.TastePassportWorldSmoke.RunJeonjuWorldChecks`** and confirm the assertions fail for absent Jeonju content.
- [x] **Step 3: Add Jeonju interactables, Hanok puzzle copy, and VI/KO strings**; keep Bác Kim’s NPC distinct from the player character.
- [x] **Step 4: Generate and import a transparent, original Bác Kim sprite** consistent with the existing character art.
- [x] **Step 5: Add official sources** for Jeonju Hanok/Gyeonggijeon/Bibimbap, UNESCO Pansori, and UNESCO’s pending Hanji nomination.
- [x] **Step 6: Run `RunJeonjuWorldChecks`** and verify sprite import and localized interactions.

### Task 3: Hanji workshop and persistent product

**Files:**
- Create: `Assets/_Game/Scripts/Gameplay/HanjiCraftRun.cs` and Unity metadata
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Create: `Assets/_Game/Resources/Art/Props/HanjiFan.png` and Unity metadata
- Create: `Assets/_Game/Resources/Art/Props/HanjiBookmark.png` and Unity metadata
- Create: `Assets/_Game/Resources/Art/Props/HanjiFlower.png` and Unity metadata
- Modify: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`

**Interfaces:**
- `HanjiCraftRun.SelectPaperColor(string colorId)`, `SelectShape(string shapeId)`, `SelectMotif(string motifId)`, and `ApplyAction(string actionId) -> HanjiCraftActionResult` enforce color + shape + motif selection followed by `cut`, `fold`, and `decorate` in order.
- `HanjiCraftActionResult` is `MissingDesign`, `OutOfOrder`, `AlreadyApplied`, `Applied`, or `Completed`.
- The UI offers touch/mouse buttons for color, shape, motif, and every ordered action. Three generated product sprites make `fan`, `bookmark`, and `flower` visibly distinct; color adds a subtle tint and the selected motif appears in the variant label.
- Save the product transactionally through `InventoryService.RecordHanjiCraft`; show the selected shape in Inventory and Jeonju’s imprint-gated display after reload.

- [x] **Step 1: Write model smoke assertions** in `RunHanjiCraftChecks` for action order, invalid selections, valid completion, repeated actions, and saved display state.
- [x] **Step 2: Run `TastePassport.EditorTools.TastePassportWorldSmoke.RunHanjiCraftChecks`**; it first failed on the missing model, then on the unwired saved display as expected.
- [x] **Step 3: Implement the small craft state model** and validate the paper variant against the inventory service.
- [x] **Step 4: Build the localized, scroll-safe workshop/result pages** and persist the completed product with a candidate profile before replacing live state.
- [x] **Step 5: Generate/import one original transparent sprite for each Hanji shape**; use the selected shape in the workshop, inventory, and world display.
- [x] **Step 6: Run `RunHanjiCraftChecks`** and confirm the product and display survive save/reload.

### Task 4: Cultural card rewards and integration

**Files:**
- Modify: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Modify: `Assets/_Game/Scripts/Localization/GameText.cs`
- Modify: `Assets/_Game/Scripts/Core/TastePassportSmokeDriver.cs`
- Modify: `Assets/_Game/Editor/TastePassportWorldSmoke.cs`
- Create: `Assets/_Game/PHASE4.md`
- Modify: `Assets/_Game/Resources/Art/ASSET_PROVENANCE.md`

**Interfaces:**
- Completing the Jeonju Hanok mission and Bibimbap game grants their cards; the Hanji game grants its card and display imprint.
- Culture Cards lists all five Jeonju discoveries as locked/unlocked entries, with VI/KO copy and credited sources.
- Integrated smoke creates a profile, completes Jeonju missions and Hanji crafting, reloads it, and asserts rewards and display state.

- [x] **Step 1: Add integrated assertions** for Hanok/Bibimbap cards after mission completion and Hanji item/card/imprint after the workshop.
- [x] **Step 2: Run `TastePassport.EditorTools.TastePassportSmoke.Run`** and confirm the complete reward, collection, localization, and world-display flow passes.
- [x] **Step 3: Wire reward grants into mission completion and render all five Jeonju cards** in the passport collection.
- [x] **Step 4: Run `dotnet build TastePassport_KGW2026.sln -c Debug`, `RunPhaseFourChecks`, and `TastePassportSmoke.Run`**; build has 0 warnings/errors and smoke output contains PASS markers.
- [x] **Step 5: Write the Phase 4 report and asset provenance**, noting visual and device limitations.
- [ ] **Step 6: Commit only Phase 4 files with a Vietnamese message and push the current branch without force.**
