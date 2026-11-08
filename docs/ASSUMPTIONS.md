# Assumptions and open product decisions

- Target remains portrait Android first with PC/editor controls for development and QA, as stated in the current README and project configuration.
- Existing project content and uncommitted user changes are intentional and must be preserved.
- The master prompt and reference files in the sibling `TastePassport_Main` checkout are the source of truth for use-case names, narrative and visual direction until they are copied into this checkout.
- Supplied art references indicate character/prop roles but are not production-ready transparent assets; current project art remains in use.
- Korean copy needs review by a fluent speaker. Until then the build can be functionally tested, but language quality is not signed off.
- Photos are expected to remain local to the device and may be removed by the user through the gallery; no cloud upload is implied.
- Platform-specific Android TTS availability varies. A missing voice/locale must leave visible subtitles and a usable silent fallback.
