# Dấu Xứ Việt Hàn — 2D Card Adventure Redesign

## Intent

Make the campaign easy to play on a phone by presenting the story as a sequence of clear, tappable 2D cards. The selected approach removes walking, proximity checks, and the virtual joystick from required story progress while keeping the existing missions, dialogue, rewards, saves, localization, and mini-games.

## Current project findings

- `PassportApp` owns the runtime page flow and already has map, dialogue, activities, passport, save, and localized content.
- `MapObjectiveService` selects the next campaign objective, but objectives such as meeting Nhung or Lan currently route into `WalkableWorldSession`.
- `WorldCatalog` contains NPC and landmark definitions, while `WorldInteractable.Interact` applies persistent story flags, imprints, chapter advancement, and collection rewards.
- `WalkableWorldSession` adds the movement controller, touch stick, proximity interaction, dialogue panel, and free-roam world. Reusing this session for required story interactions would retain the main source of control difficulty.
- Existing activities already have individual touch UIs and save/reward paths; the card redesign should route into those screens rather than duplicate their run logic.
- `PlayerProfile`, `ProfileStore`, `StoryProgressService`, `GameText`, and the existing Unity smoke/capture drivers remain the progression, persistence, localization, and verification sources of truth.

## Goals

- Use a 2D card flow for all required story progression from new game through the ending.
- Put the current task and one obvious primary action first on the journey screen.
- Let players open the relevant NPC or landmark directly from its card, without walking to a coordinate or remembering keyboard keys.
- Present short NPC dialogue cards with portrait, name, Continue, and optional Ask More when available.
- Route activities into their existing interactive mini-games, then show a clear stamp/reward card and the next story objective.
- Keep Hội An and Jeonju distinct through their illustrations, palette accents, characters, and cultural cards.
- Support Vietnamese and Korean with the existing save and profile format.
- Keep touch targets at least 64 logical units and keep all content inside Android safe areas.

## Non-goals and anti-goals

- Do not change the six-mission story, character relationships, unlock order, activity scoring, rewards, or save schema.
- Do not turn the activities into passive read-only cards; their existing touch interactions remain playable.
- Do not require a joystick, keyboard shortcut, or proximity trigger for any campaign milestone.
- Do not replace the Unity project, scene, UGUI, Input System, or working profile services.
- Do not make a collectible-card battle game; cards are the navigation and story presentation metaphor.
- Keep optional 3D free-roam separate from the main campaign and never use it as a required step. The player may finish the whole story without entering it.

## Approaches considered

1. **Full 2D card adventure — selected by the user.** Use location, NPC/landmark, dialogue, activity-entry, and reward cards to drive the existing story state. This removes movement friction from the critical path while retaining the game's narrative and mini-games.
2. **Keep walking 3D and add card overlays.** This would improve dialogue and quest readability but leave the joystick and NPC range requirement in the main path, so it would not solve the stated difficulty.
3. **Card-combat game.** This would redefine the cultural adventure as a battle/collection genre and discard the current mission and activity design, so it is out of scope.

## Card flow

1. **Journey screen:** show the active story card, destination, concise objective, progress, and one primary `Tiếp tục` action. Secondary routes are visually quieter.
2. **Location card:** show Hội An or Jeonju as a 2D illustrated destination with a small set of relevant character/landmark cards. Locked destinations show their unlock requirement.
3. **NPC/landmark card:** tapping the objective target opens its card directly. Entering an NPC card records the same interaction flags/imprints and collection rewards used by the current world interaction path, then opens dialogue.
4. **Dialogue card:** show character portrait and speaker, short localized copy, one Continue action, and optional Listen/Ask More only when the current dialogue provides those actions. The player never needs to choose through a long stack of Next prompts.
5. **Activity card:** explain the next action in one short instruction and open the existing interactive activity. Completion, failure, retry, scoring, and reward logic remain unchanged.
6. **Reward card:** reveal the earned stamp, item, or culture card, persist via existing save services, and present one primary route to the next objective.
7. **Post-story:** show ending/reward cards and a 2D collection/free-exploration hub. Any existing 3D stroll is an optional secondary route and can be completed or skipped without affecting the ending or saved collection.

## Architecture calls

- Keep `PassportApp` as the page/navigation owner. Add card-specific pages or focused card drawing methods there instead of creating a parallel screen manager.
- Extend objective metadata with the target identity needed for direct card routing; `MapObjectiveService` remains responsible for selecting the next objective.
- Extract the state-changing part of `WorldInteractable.Interact` into a reusable interaction service that accepts `PlayerProfile` and `WorldInteractableDefinition`. The 3D component delegates to it, and the card path calls the same service. This prevents divergent save/reward rules.
- Persist a cloned candidate profile before advancing the card flow, following the current safe-save behavior. A failed save leaves the objective repeatable and displays the existing localized save error.
- Add paired Vietnamese/Korean labels for any new card action. Continue using `StoryProgressService`, `InventoryService`, `ProfileStore`, and existing activity run objects.
- Use existing 2D location and character images as card art; use flat card silhouettes, clear type hierarchy, and subtle transitions. Do not add new 3D cameras or render targets for these campaign pages.
- Keep `WalkableWorldSession` available for optional 3D free roam, but remove its joystick/help requirement from the new-game-to-ending route.

## Constraints

- Unity remains 6000.6.7f1, Android portrait, UGUI, and the current Input System.
- Preserve the 720 x 1280 logical design size, safe-area layout, and minimum 64-unit touch targets.
- Preserve all existing VI/KO save data and let current saves resume at their current objective.
- Continue using existing UI capture and smoke infrastructure. Capture the full card route at 720x1280 and 1080x2400.
- Keep unrelated dirty files out of commits.

## Acceptance checks

- A new profile can complete the story from Nhung's first appearance through the ending without opening the 3D free-roam world.
- The current objective has one visible primary action and can open the target card directly.
- Completing a card interaction updates the same flags, stamps, inventory, and chapter state as before; reopening or resuming the profile does not repeat completed rewards.
- Activity cards launch the existing minigames, and retry/return routes preserve current scoring and save rules.
- Vietnamese and Korean card copy, portraits, lock state, reward state, and safe-area anchors render at both capture sizes.
- Every visible campaign card button works; no decorative control looks actionable.
- Full story smoke and card navigation smoke pass, and a fresh Android APK builds. Physical Android touch testing is reported separately if no device is connected.

## Builder decisions left open

- Choose the smallest card page set that expresses the flow without duplicating existing activity pages.
- Reuse existing profile save semantics and exact localized copy wherever possible.
- Preserve optional 3D exploration as a secondary post-story route unless navigation or save evidence shows it would confuse new players.
