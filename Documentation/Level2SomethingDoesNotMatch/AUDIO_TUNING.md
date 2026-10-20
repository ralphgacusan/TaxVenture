# Paper/card sounds and ambience — step by step

Implemented 2026-10-21; internally verified, user Unity listening/feel test pending. No manual Inspector wiring is required.

## What changed

1. **Document icons:** Notes, Case Folder, Taxpayer Profiles, Tax Code Book and Annotated Papers use paper sounds. AudioManager.PlayDocumentSFX rotates paper pickup/place, rustle, page turn and folder-opening/closing recordings. Shared Button fallback checks the document identity before generic UI words such as open or close. Explicit action sounds override generic fallback so one action does not double its click.
2. **Every value transfer:** ValueTransferManager plays exactly sfx_card_pickup_02 for selection and sfx_card_place_01 for successful placement. ValueDragSource uses pickup 02 for another drag of an already-selected value. Deselect uses return 02; invalid placements keep unavailable feedback. This is shared across case-folder fields, book values, annotated amounts and other IDataValueSource/IDataValueDestination users.
3. **Side Quest 2:** IncomeToolkit uses sfx_card_place_01 when a card actually lands on the paper, including Arthur's demonstration. Physical highlight sweeping and the completion flourish use exactly sfx_card_return_02. Sweep coverage changes do not trigger the recording on every frame.
4. **Music/ambience:** AudioManager.MusicVolume is 0.035 and AmbienceVolume is 0.45. The original office and environment files measure approximately -42.3 and -49.7 dBFS RMS respectively, so gain on the AudioSource alone left them quiet. Playback copies in Assets/AudioManager/MixedAmbience target -26 dBFS RMS with peak below 85%; the originals remain intact. Resources/TaxVentureAudioLibrary.asset references the louder copies in officeAmbience/outdoorAmbience. AudioEnvironment recognises OfficeBuilding (office), Outside and Env (outdoors), based on the nearest matching ground hit.

## Unity test sequence

1. Stop the old Play session, let Unity finish importing the edited scripts and two new WAV copies, then start Office_Level2. In Settings enable SFX (this also controls ambience) and Music. No scene/Inspector changes are needed.
2. Temporarily turn Music OFF and leave SFX ON. Listen inside the office, then walk outside. Confirm office and environment ambience play and change with the location. Turn Music ON and confirm it remains quieter underneath.
3. Open/close Notes, retrieve the Case Folder before using its icon, collect/open Taxpayer Profiles where available, and open/close the book and annotated papers. Repeat each icon several times: only paper/folder/page recordings should vary; Continue and Settings should retain their own click sounds.
4. At a folder/book/annotated-paper value, test tap-select then tap-place and a physical drag/drop. Pickup must be card_pickup_02, a successful drop card_place_01. Select a value and drag it again, deselect it, and try an incompatible destination; existing selection/validation behavior should remain intact.
5. At Rosa's Side Quest 2 desk, drop Type/Treatment/Evidence cards onto the paper and listen at landing. Swipe a highlighted line with the pen and tap a line for the traced animation: use card_return_02, without a rapid stack of keyboard/UI ticks.
6. Repeat the shared document/value interactions in Level 1 and a training module. Check SFX OFF silences paper/card sounds and ambience, while Music keeps its own switch.

For later tuning, change the two volume constants in Assets/Scripts/AudioManager/AudioManager.cs; keep the playback-copy gain stable. The generated folder's README records the source-to-copy gain. Internal build/regression checks cannot establish loudness on the user's speakers or touch feel.
