# Taste Passport UI font

`Assets/_Game/Resources/TastePassportFont.ttf` is a static weight-500 instance of `NotoSansKR-VariableSource.ttf`, generated on 02 November 2026 with fontTools `instantiateVariableFont` from the variable font already present in this project. The original variable font is retained here for provenance; it is outside `Resources` and is not used by the runtime UI.

Noto Sans KR is distributed under the SIL Open Font License. The bundled license is `Assets/_Game/Resources/OFL-NotoSansKR.txt`. Official family source: https://github.com/google/fonts/tree/main/ofl/notosanskr

The static instance keeps the existing Vietnamese Latin and Korean Hangul coverage. Glyph coverage was checked locally after generation; phone rendering still needs Play Mode and Android review.

## Classic display face

`Assets/_Game/Resources/TastePassportSerif.ttf` is a static weight-600 subset of the official Noto Serif KR variable font. It is used for titles and passport-style headings while the sans face above remains in buttons, instructions, and small labels.

Original source: https://github.com/google/fonts/tree/main/ofl/notoserifkr (`NotoSerifKR[wght].ttf`). The project includes its SIL Open Font License at `Assets/_Game/Resources/OFL-NotoSerifKR.txt`. The subset retains Latin/Vietnamese, Hangul syllables and Jamo, punctuation and symbols used by localization. It does not include the full Hanja collection; player names made of Hanja need a future fallback-font review.
