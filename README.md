# Hộ Chiếu Vị Giác — Taste Passport

A cozy, single-player Unity adventure about cultural exchange between Hội An, Việt Nam and Jeonju, South Korea. The project targets Android portrait first and can also be opened in the Windows Unity Editor.

## Project

- Unity: 6.6.7f1
- Render pipeline: Universal Render Pipeline (2D)
- Main scene: `Assets/_Game/Scenes/TastePassport_MVP.unity`
- Runtime UI and flow: `Assets/_Game/Scripts/Core/PassportApp.cs`
- Localized text: `Assets/_Game/Scripts/Localization/GameText.cs`
- Korean/Vietnamese content data: `Assets/_Game/Scripts/Gameplay/`
- Local profile and progress: `Assets/_Game/Scripts/Save/`

## Open and play in Unity

1. Open the existing project in Unity Hub with Unity 6.6.7f1.
2. In the Project window, open `Assets/_Game/Scenes/TastePassport_MVP.unity`.
3. Set the Game view to a portrait aspect ratio (for example, 9:16 Aspect). Do not use a 9 × 16 pixel resolution.
4. Press Play. The first run offers language and character setup; later runs can continue the saved journey.

The game includes six replayable mission loops: exploration and postcard assembly in both locations, Cao Lầu and Bibimbap ingredient challenges, and cultural matching. Completing missions saves stamps and unlocks three collectible postcards. Profiles are stored locally; no account or network service is required.

## Language and narration

The UI supports Vietnamese and Korean. Android builds include a native Android TextToSpeech bridge: the selected UI language requests `vi-VN` or `ko-KR`. On the home screen and story/dialogue cards, use **Nghe lời thoại / 대사 듣기** to speak the story, and **Dừng giọng đọc / 음성 멈추기** to stop. Android must have a compatible speech engine and the requested language voice data installed. Availability and pronunciation depend on the device's TTS engine.

Ambient chapter audio is generated at runtime and changes with the chapter and destination. It is a lightweight prototype, not a recorded cultural-instrument soundtrack. The Settings sound toggle controls game audio and stops active narration.

## Verification status

A Unity batch smoke run reported PASS for the six mission loops, replay, local progress, the three postcards, postcard flip, and switching the UI to Korean. That existing smoke flow plays the missions in Vietnamese before switching the UI language; it does not verify every mission fully in Korean. Runtime and Editor C# assemblies have also been compiled with `dotnet build` against the Unity API profile.

Android APK creation, Android device playback, Korean/Vietnamese TTS voice availability, and visual Play Mode checks are not claimed as verified by this README. Test these in Unity Editor/Android Build Profiles before release.

## Content and assets

Project artwork and fonts are stored under `Assets/_Game/Resources/` and `Assets/_Game/Art/`. Refer to the nearby `*_SOURCE.md`, `ASSET_PROVENANCE.md`, and font license files for source and licensing details.
