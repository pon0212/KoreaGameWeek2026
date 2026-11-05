# Hộ Chiếu Vị Giác — Taste Passport

**Taste Passport: A Journey of Two Cultures** is a single-player cozy adventure about discovering food, architecture, and everyday stories in Hội An, Việt Nam, and Jeonju, South Korea. The game is built for portrait Android screens and can also be opened and played in the Unity Editor on Windows.

## Project at a glance

- Unity Editor: `6000.6.7f1`
- Render pipeline: Universal Render Pipeline, 2D template
- Main scene: `Assets/_Game/Scenes/TastePassport_MVP.unity`
- Runtime UI and navigation: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Opening storybook: `Assets/_Game/Scripts/Core/PassportApp.IntroBook.cs`
- Vietnamese and Korean strings: `Assets/_Game/Scripts/Localization/GameText.cs`
- Gameplay and progression: `Assets/_Game/Scripts/Gameplay/`
- Local profile and save data: `Assets/_Game/Scripts/Save/`
- Main artwork and fonts: `Assets/_Game/Resources/`

## Open the project

1. Open the existing repository folder in Unity Hub with Unity `6000.6.7f1`.
2. Open `Assets/_Game/Scenes/TastePassport_MVP.unity`.
3. Set the Game view to a portrait aspect, such as **9:16 Aspect**, or use a phone resolution such as **720×1280**. Do not enter `9 × 16` as a pixel resolution.
4. Press **Play**. The antique storybook opens first; tap a page to turn it. The first journey then continues to language and character setup. Existing profiles can continue their saved journey.

## Game flow and controls

The current build contains six replayable mission loops:

1. Find postcard fragments in Hội An and assemble the picture.
2. Explore Jeonju Hanok details and assemble the picture.
3. Choose ingredients for a simplified Cao Lầu challenge.
4. Arrange ingredients for a simplified Bibimbap challenge.
5. Match visitors' requests with Hội An experiences.
6. Match cultural experiences in Jeonju.

Completing missions saves stamps and unlocks three collectible postcards. Progress and the player profile are stored locally; an account or online service is not required. The opening pages and UI support Vietnamese and Korean. Craft activities, walkable-world content, and later story chapters are described in the phase notes under `Assets/_Game/` and are still being developed.

## Audio and speech

Android builds include a native TextToSpeech bridge. Story and dialogue cards can request Vietnamese (`vi-VN`) or Korean (`ko-KR`) speech. Playback depends on the device's speech engine and installed voice data. The audio setting controls game audio and stops active narration.

Ambient chapter audio is generated at runtime as a lightweight prototype. It is not a recorded cultural-instrument soundtrack.

## Checks and known limits

The Unity batch smoke test has passed for the six mission loops, replay, saved progress, postcard unlock/flip, language switching, and the opening storybook tap-to-turn flow. Automated image capture has rendered the book screens in Vietnamese and Korean at `720×1280` and `1080×2400`.

These checks do not establish an Android APK build or physical-device test. Android layout, touch behavior, text-to-speech voice availability, and performance still need verification on target devices before release.

GitHub Actions runs project-structure and whitespace checks on pushes and pull requests to `main`. Unity compilation and EditMode tests run when the repository has an Actions secret named `UNITY_LICENSE`; otherwise that step is clearly reported as skipped. Setup instructions are in `.github/UNITY_CI.md`.

## Artwork and licenses

Project artwork is kept under `Assets/_Game/Resources/`. Generated storybook artwork and its provenance are documented in `Assets/_Game/Resources/Art/STORYBOOK_SOURCE.md`; other supplied or generated artwork has provenance notes near the assets. Font license notices are included in `Assets/_Game/Resources/`.
