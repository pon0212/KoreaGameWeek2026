# Task 4 report — 3D presentation for passport and activities

Status: DONE. Commit: `7570b66` — Add 3D passport and activity dioramas.

## Files changed

- `Assets/_Game/Scripts/UI/GameUiActivityDiorama.cs` and `.meta`: new reusable generated 3D stage with one offscreen camera, one portrait render texture, four shared meshes, shared unlit materials, scene variants, and owned cleanup.
- `Assets/_Game/Scripts/Core/PassportApp.cs`: selects the 3D stage for each applicable page, draws its texture on a non-raycast background plane, and hides/restores the activity stage around world entry and return/failure.

No other product files were included in the commit. A separate pre-existing plan-document edit remains unstaged.

## Behavior

- Passport, inventory, and culture-card pages display an open 3D passport scene. Postcards display a 3D postcard on a tabletop.
- Architecture dialogue/discovery shows a miniature street building and bridge; the assembly page adds three detached building fragments; the architecture result shows a medal.
- Recipe dialogue/game pages show a plated dish with ingredients and chopsticks. Bibimbap mixing shows a spoon, timed-out recipe shows a retry marker, and completed recipes show a medal.
- Matching uses 3D culture cards with lantern, bowl, and bridge tokens; the matching result adds a medal.
- Lantern and Hanji crafting pages show their tabletop props and tools; each result adds a medal. Festival preparation/results show an arch, paired Vietnam/Korea lanterns, and paired culture cards.
- Vietnam/Korea palette choices update shared materials from the existing `koreanBackdrop` state. No gameplay state was added.
- Scene roots and result variants are constructed once and toggled on `SetActivity`; repeated `Show()` rebuilds only reuse the existing component and `RenderTexture`. The activity camera uses a separate remote range on the reserved stage layer, never modifies the gameplay/main camera, and is disabled when hidden. All mesh renderers disable realtime shadows.
- Activity backdrop `RawImage.raycastTarget` remains false; text, buttons, and touch handling remain on the existing fitted World Space Canvas. Existing game handlers, run objects, result flags, score/reward logic, timers, save data, and localized copy were not modified.
- `OpenWorld` disables both diorama cameras. The existing `Show()` path picks the appropriate page scene after world failure/return, including a passport return from an NPC.

## Static self-review

- Reviewed the complete Task 4 diff and checked that every applicable page maps to an activity scene or result variant.
- Verified scene switching changes active roots/variants and shared palette colors only; it does not allocate additional cameras, render textures, meshes, or materials per page.
- Verified one unique Unity script GUID for the new component and ownership under `PassportApp`, allowing `OnDestroy` to release the camera target, texture, stage root, meshes, and materials.
- `git diff --cached --check` passed before commit.

## Validation limits

No automated tests or build were run, as directed by the task. Unity rendering, Android touch, shader inclusion after stripping, and device performance were not exercised here; they remain for the integration/build review.

## Task 4 fix round 1

- Diagnosis: the Task 5 Android build reported `CS0103` at the matching-scene bowl call. The call used `CreateBowlProp`, while the helper defined in this class is named `BuildBowlProp`.
- Fix: changed the call to `BuildBowlProp`; no scene behavior or gameplay logic changed.
- Static validation: `rg` now reports only the matching call and helper definition with the same `BuildBowlProp` name; `git diff --check` passed.
- This fix round did not run tests or another build. The Task 5 build that surfaced the compiler error is the reproducible diagnosis supplied by the parent.
