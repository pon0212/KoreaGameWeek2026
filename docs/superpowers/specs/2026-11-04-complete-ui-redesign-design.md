# Dấu Xứ Việt Hàn — Complete UI/UX Redesign

## Intent

Make the existing game feel like a calm, polished cultural adventure. Players should see and explore Hội An or Jeonju first, understand one current objective, meet Nhung or Ga Bo in the world, and use the Dấu Xứ book to review progress and rewards. The portrait Android build is the primary layout target; keyboard and mouse remain supported.

## Audit findings

- `PassportApp.Show` builds every screen at runtime inside the same full-screen vertical `ScrollRect`. The shared 270-unit header, repeated paper cards, long button stacks, and large gaps make the menu, map, activities, and collection pages feel like one long web form.
- The main menu places the welcome card, primary action, profile/passport/settings links, destination pair, narration, and language link in the same scroll hierarchy. The player has no single visual starting point.
- The map mixes the next story objective with expandable activity lists and several secondary routes. Its current objective card does not establish a clear adventure-first path.
- `WalkableWorldSession.CreateHud` uses a 204-unit objective/navigation card, four full-width labels, a large action button, and a first-visit guide that can cover most of the world view. The current guide is a long text block.
- Dialogue, journal, passport, and mini-game pages reuse the same tall header, paper card, and button primitives. Save, localization, quest, world, and activity services already work and must remain the source of truth.
- UI is generated in `PassportApp` and `WalkableWorldSession`; the authored scene has one runtime app root and an EventSystem. `WorldSpaceCanvasFitter` already sizes the canvases to the camera, and both app and world HUD already apply safe areas.
- `GameUiDioramaStage` and `GameUiActivityDiorama` already render isolated 3D backdrops. Existing Vietnamese and Korean localization, custom fonts, page fades, sound feedback, and smoke-test drivers are present.

## Goals

- Establish a consistent storybook design system for color roles, spacing, typography, cards, buttons, focus states, and short transitions.
- Give the main menu one primary action and a clear continuation state; only expose Continue for a usable saved profile.
- Make the walkable world the dominant play surface. Show a compact location and a one- or two-line active objective, then reveal a named interaction action only near a target.
- Teach controls in short, contextual steps; keep the world visible while help is open.
- Make NPC dialogue readable and compact, with portrait/name, concise copy, optional listen/ask-more actions, and an obvious continue action.
- Reorganize the passport and quest journal as illustrated, progress-led book pages sourced from the current save.
- Give each cultural activity a clear focal object, short instruction, visible progress, and touch-sized controls. Keep its existing run and reward logic.
- Preserve the 3D Hội An/Jeonju world, dioramas, current save format, localization, keyboard controls, touch movement, audio, missions, and free exploration.
- Verify Vietnamese and Korean text, phone safe areas, PC navigation, and the end-to-end story flow using the existing Unity smoke infrastructure and new UI assertions where needed.

## Non-goals and anti-goals

- Do not replace the Unity project, scene, or current data/progression systems.
- Do not migrate the project to UI Toolkit or replace its working input modules just for styling.
- Do not use flat administration dashboards, dense quest tables, repeated competing CTAs, or decorative controls that do nothing.
- Do not add a new save schema or break existing profiles.
- Do not describe editor-only screenshots as Android-device verification.

## Constraints

- Unity version remains 6000.6.7f1; retain the current UGUI/Input System implementation and portrait Android orientation.
- Keep the existing 720 x 1280 logical reference size, camera-fitted UI, safe-area handling, and a minimum 64-unit touch target.
- Reuse localization keys when possible; every new player-facing label must exist in Vietnamese and Korean.
- Preserve unrelated dirty user files and only stage files belonging to this redesign.
- The project already has runtime scene/UI smoke tools. Use them and report any device-only checks that cannot run because no device is attached.

## Approaches considered

1. **Replace the runtime UI with new scene/prefab screens.** This offers authoring convenience but duplicates the existing navigation and creates high risk for save, dialogue, and activity routes.
2. **Replace UGUI with UI Toolkit.** This could make layout authoring cleaner but conflicts with the current touch stick, pointer-based mini-games, and runtime screen creation, and would expand the migration beyond the requested experience work.
3. **Refactor the current runtime UI with shared design tokens and page-specific composition.** This preserves existing gameplay services and 3D stages while allowing the major screens and their hierarchy to change. This is the selected approach because the user explicitly requested implementation on the current project and prioritized easy play and stable gameplay.

## Design direction

- Use warm cream paper and ink for readable surfaces; deep teal for the one primary action; terracotta for story seals and earned rewards; muted jade and lantern gold for secondary states. Keep one display face and one body face, with the existing font assets and Vietnamese/Korean support.
- Keep the 3D stage visible behind all menus and book pages. Use subtle paper framing, smaller hierarchy-driven cards, restrained shadows, and no large opaque panel over the walkable scene.
- Main menu: title and subtitle over the dual-land diorama, one primary Start/Continue action, then a quiet secondary row for Collection (when a valid profile exists), Settings, and Language.
- Map: active story waypoint first, two destination cards with earned-stamp progress, and expandable optional activity routes. The route CTA should enter the corresponding world or activity directly.
- World HUD: compact location/objective chip at the top, small book/map/help affordances, safe-area joystick at lower left, and a named contextual action at lower right. Help and journal pause movement but leave the world visible around a centered paper card.
- Dialogue: use a shorter lower-screen paper card with an identifiable portrait and speaker, limited readable copy, and only the actions available for that character/line.
- Passport: use a cover/identity page, Hội An page, Jeonju page, and collections page. Each page reads real stamps, inventory, cultural cards, postcards, and rewards from `PlayerProfile`.
- Activities: retain each activity's input and scoring rules while grouping controls around the featured lantern, food, hanok, hanji, or Chùa Cầu artwork and surfacing only the current step.
- Motion: brief button response, page fade, conversation entrance, and earned-stamp reveal. Motion is low-amplitude and does not gate input.

## Implementation calls left to the builder

- Reuse existing screen methods and extract only the shared pieces needed to keep the change reviewable.
- Prefer existing localized keys; add paired `vi`/`ko` strings when a shorter contextual label is required.
- Use the existing smoke drivers to verify real navigation and save behavior, then build a fresh Android APK. Run visual capture at portrait reference size where the Unity editor permits it.
- Do not claim physical Android verification unless ADB lists a connected device and the build is launched there.
