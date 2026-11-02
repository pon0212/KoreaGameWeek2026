# Original location illustrations

`HoiAn.png` and `Jeonju.png` were generated for Taste Passport on 29 October 2026 with Codex's built-in image generation tool. They are original outputs for this project; no third-party image was downloaded or traced. They contain no embedded text or logos.

- `HoiAn.png`: Prompt requested a warm 2D storybook view of Hội An old town, ochre houses, wooden shutters, clay roofs, lanterns and a glimpse of the Hoài River; excluded Korean motifs and fantasy architecture.
- `Jeonju.png`: Prompt requested a calm 2D storybook view of Jeonju Hanok Village, curved tiled eaves, timber, hanji paper doors and stone walls; excluded Vietnamese lanterns and fantasy architecture.

These are atmospheric illustrations, not documentary reconstructions. A local cultural and art review remains necessary before contest submission. The passport cover, stamps, friendship graphics and fallback bowls are code-generated UI illustrations in `TravelArtwork.cs` and remain temporary art.

- `CaoLau.png`: original cutout made for this project with Codex's built-in image generation tool on 02 November 2026. Prompt: "Create a production game UI illustration asset: a single appetizing bowl of Hội An Cao Lầu, Vietnam. Three-quarter overhead view, thick golden chewy Cao Lầu noodles, slices of browned pork, fresh local herbs and greens, crunchy golden rice crackers, a little dark savory sauce, ceramic bowl with subtle blue-white traditional motif. Cozy classical travel-journal painting with fine hand-painted texture, warm lantern light, rich natural colors, semi-realistic food detail, crisp readable silhouette at phone size. Center the entire bowl with generous clear margin, no people, no map, no flags, no typography, no lettering, no watermark. Transparent background cutout, clean alpha edges. This is an original food illustration for Taste Passport game and should not resemble phở." It is a gameplay illustration, not a full traditional recipe reference. Cultural and food presentation review remains necessary before release.

- VietnamJourney.png: supplied by the project owner from Downloads/Hội An trong sắc màu Việt Nam.png; copied without editing. Used as the Vietnam destination banner. Rights/provenance are user-supplied; retain attribution/permission confirmation from the owner if the image was created by another artist.

- KoreaJourney.png: supplied by the project owner from Downloads/Hàn Quốc_ Hương vị Jeonju.png; copied without editing. Used as the Korea/Jeonju destination banner. Rights/provenance are user-supplied; retain attribution/permission confirmation from the owner if the image was created by another artist.

## Walkable character sprites (02 November 2026)

`Characters/Traveler.png`, `Characters/Nhung.png`, `Characters/GaBo.png`, and `Characters/Lan.png` were generated as original transparent full-body Unity sprites with Codex's built-in image generation tool. The prompts requested a consistent cozy, hand-painted storybook style, readable refined chibi proportions, no text, no flags, no watermark, and a transparent background.

- `Traveler.png`: gender-neutral young traveler in a cream jacket with teal/lantern-orange accents and a journal satchel; intended as the controllable player, separate from companion NPCs.
- `Nhung.png`: friendly, energetic young Vietnamese companion in a contemporary yellow-and-jade outfit with a small woven bag and lantern charm.
- `GaBo.png`: curious, playful young Korean companion in contemporary ivory/navy/coral travel clothes with a camera.
- `Lan.png`: middle-aged Vietnamese lantern artisan in indigo work clothes and apron, holding a bamboo frame and finished lantern; designed as Cô Lan, the Hội An craft guide.

The portraits are single idle poses, not animated spritesheets. The backgrounds are illustrative and the character clothing is a stylized game design; review cultural details locally before submission. Unity import was checked in Unity 6000.6.7f1; the sprites load as 2:3 single-sprite assets at 200 pixels per unit. No device-scale review has been performed.

## Hội An lantern activity (02 November 2026)

- `Props/HoiAnLantern.png`: original transparent storybook illustration generated with Codex's built-in image generation tool. The prompt requested a single unlit Hội An lantern, bamboo/wood frame, golden fabric with plum blossoms and hanging tassel, centered as a clean 2:3 cutout. It is a display and preview illustration, not a documentary record of a specific artisan's design.
- `Characters/Lan.png`: Cô Lan's companion sprite was generated in the same visual style as the other walkable characters. She is a fictional guide and is not modeled on a named living artisan.

The craft activity stores the selected frame and fabric color in the inventory variant ID and applies a light color tint to the shared preview/display sprite. It does not isolate the silk for color changes or redraw the frame geometry for each selected shape.

## Jeonju Hanji activity (03 November 2026)

- `Props/HanjiFan.png`, `Props/HanjiBookmark.png`, and `Props/HanjiFlower.png`: original transparent product illustrations generated for this project with Codex's built-in image generation tool. The prompts requested three clearly distinct 2:3 Hanji craft objects—a folding fan, a bookmark, and a paper flower—in a warm, hand-painted storybook style, centered with clear margins, no text, and transparent backgrounds.
- The workshop colors tint the whole illustration lightly; they do not recolor an isolated paper layer. The selected motif is recorded in the product variant and shown in the label, but is not drawn as a separate overlay. The model represents a short paper-craft activity, not the complete traditional Hanji paper-making process.
- Unity imports were checked as single 2:3 sprites at 200 pixels per unit. The focused smoke verified that all three shapes load and that the most recently saved product appears in Jeonju's display. No Android device-scale review has been performed.

## Jeonju character sprite (03 November 2026)

- `Characters/BKim.png`: original 1024×1536 transparent sprite generated with Codex's built-in image generation tool. Prompt: “Create Bác Kim, a warm Korean hanji artisan and mentor, as a single standing character asset. Friendly Korean man in his late 50s, gently smiling, holding a small handmade paper sheet; modest indigo work vest over a cream traditional Korean hanbok top and dark trousers; premium cozy storybook illustration, refined chibi proportions, painterly watercolor-like texture, soft dimensional shading, 2:3 full-body portrait, transparent background, no text or watermark.”
- The character is fictional and is not modeled on a specific artisan. The paper prop represents the craft theme, not a claim that the pictured paper reproduces a full traditional production stage.
- Unity import is configured as a single 2:3 sprite at 200 pixels per unit. The sprite was checked in the editor smoke; no Android device-scale review has been performed.
