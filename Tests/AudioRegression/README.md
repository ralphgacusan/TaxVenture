# Audio regression

From the project root, run `dotnet run --project Tests/AudioRegression/AudioRegression.csproj`.

The harness runs the production AudioManager, AudioLibrary, and SettingsManager against a fake Unity audio surface. It checks that all supplied SFX and ambience clips plus the approved gameplay music are cataloged, every non-voice SFX family has a contextual call site and every numbered variant is reachable, settings independently control music and SFX ambience, context changes select ambience and footsteps, dialogue/gibberish playback is blocked through both legacy and generic APIs, matching music continues across scenes, and contextual button/world feedback coalesces duplicate action hooks without being silenced by footsteps or notifications. Music uses 3.5% gain and ambience uses 45% gain. Ambience categories reference constant-gain playback copies in Assets/AudioManager/MixedAmbience (original source WAVs are unchanged), targeting -26 dBFS RMS and peaks below 85%.

Compile the actual game scripts with `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`.

In Unity Play Mode, check the menu, Level 1, and Level 2: buttons/settings, footsteps, paper/book pages, stamp pickup/impact/return, drawers, folder submission, audit outcomes, objectives, and rewards. Check Continue/Arthur guide taps, NPC and item taps, and opening/closing/returning papers in both levels and training modules. Confirm there are no gibberish voices. Toggle SFX off during an effect, change scenes and restart, then turn it back on. Confirm the saved switch controls every effect while music keeps its own setting. Check the Level 2 deadline cues as well.

The Resources catalog references clips in the original supplied audio folder, so those assets must stay with the project. No Inspector assignment is required. Playback quality, volume balance, and real scene/device behavior require the Play Mode check.

## Paper / card sound mapping — 2026-10-21

Document icons use PlayDocumentSFX and the document_open/document_close fallback pools: paper pickup/place, rustle, page turn and folder opening/closing; no UI panel beep. Pools advance without immediate repeats. Continue/settings/navigation retain their non-document UI feedback. ValueTransferManager owns the exact card pickup 02 / place 01 mapping for every tap/drag transfer; ValueDragSource also plays pickup 02 when beginning another drag of an already-selected value. Deselect uses card return 02. PlayExactEffect preserves the named recording and retains shared family duplicate suppression. IncomeToolkit selects/lifts cards with pickup 02, plays place 01 at the SettleInto landing, and plays return 02 on the first physical highlight-sweep progress and completion flourish. Coverage ticks do not replay continuously.

Manual check: see Documentation/Level2SomethingDoesNotMatch/AUDIO_TUNING.md. Generated ambience copies have the same channels, rate, duration and importer settings as their source; no compression, trimming or denoising was applied. The Env outdoor hierarchy now participates in floor classification.
