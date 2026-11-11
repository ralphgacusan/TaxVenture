#!/usr/bin/env python3
"""Check the supplied MP3 frames, cue timing, catalogue text and Unity GUIDs without starting Unity."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def mp3_seconds(path):
    data = path.read_bytes()
    offset = 0
    if data[:3] == b"ID3":
        offset = 10 + sum((data[6 + i] & 127) << (21 - 7 * i) for i in range(4))
        if data[3] == 4 and data[5] & 16:
            offset += 10
    samples = 0
    rate = None
    # Supplied exports are MPEG-1 Layer III. Reject a different encoding rather
    # than treating file size/bitrate (or metadata duration) as an exact length.
    bitrates = (0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320)
    while offset + 4 <= len(data):
        if data[offset:offset + 3] == b"TAG":
            break
        header = int.from_bytes(data[offset:offset + 4], "big")
        assert header >> 21 == 0x7FF, f"{path.name}: unexpected data at {offset}"
        assert (header >> 19) & 3 == 3 and (header >> 17) & 3 == 1, f"{path.name}: expected MPEG-1 Layer III"
        bitrate = (header >> 12) & 15
        sample_rate = (header >> 10) & 3
        assert 0 < bitrate < 15 and sample_rate < 3, f"{path.name}: invalid MP3 header"
        current_rate = (44100, 48000, 32000)[sample_rate]
        assert rate is None or current_rate == rate, f"{path.name}: mixed sample rates"
        rate = current_rate
        size = 144000 * bitrates[bitrate] // rate + ((header >> 9) & 1)
        assert offset + size <= len(data), f"{path.name}: truncated MP3 frame"
        samples += 1152
        offset += size
    assert samples > 0, f"{path.name}: no MP3 audio frames"
    # Conservatively includes decoder padding and any Xing frame.
    return samples / rate


def main():
    seen = set()
    folder = ROOT / "Assets/AudioManager/Cutscene_Dialogues"
    for path in sorted((ROOT / "Assets/Cutscenes").glob("Chapter*/*_script.json")):
        script = json.loads(path.read_text())
        catalogue_path = ROOT / "Assets/Resources/Cutscenes/Voice" / (script["id"].replace(".", "_") + ".asset")
        catalogue = catalogue_path.read_text()
        entries = {}
        for block in catalogue.split("  - id: ")[1:]:
            entry_id, body = block.split("\n", 1)
            entries[entry_id] = body
        assert len(entries) == len(script["cues"]), f"{path}: catalogue/cue count differs"
        for cue in script["cues"]:
            voice = cue["voice"]
            assert voice not in seen, f"duplicate ID {voice}"
            seen.add(voice)
            clip = folder / (voice + ".mp3")
            seconds = mp3_seconds(clip)
            tail = cue["end"] - cue["start"] - seconds
            assert tail >= .25, f"{voice}: {seconds:.3f}s exceeds its budget"
            boundary = min([t for t in script["shotStarts"] if t > cue["start"]] + [script["duration"] - script["fadeOutSeconds"]])
            assert cue["end"] <= boundary, f"{voice}: cue crosses a cut/fade"
            body = entries[voice]
            for field in ("text", "speaker", "variant"):
                value = re.search(r"^    " + field + r": (.*)$", body, re.M)[1]
                assert json.loads(value) == cue[field], f"{voice}: stale {field}"
            guid = re.search(r"guid: ([0-9a-f]{32})", Path(str(clip) + ".meta").read_text())[1]
            assert f"fileID: 8300000, guid: {guid}, type: 3" in body, f"{voice}: wrong clip reference"
            metadata = Path(str(clip) + ".meta").read_text()
            assert "loadType: 0" in metadata and "preloadAudioData: 1" in metadata and "loadInBackground: 0" in metadata, f"{voice}: speech must preload/decompress"
            print(f"PASS {voice}: {seconds:.3f}s, {tail:.3f}s before subtitle end; shot boundary {boundary:.2f}s")
    assert len(seen) == 14, "expected all fourteen supplied recordings"
    assert {p.stem for p in folder.glob("*.mp3")} == seen, "unmapped or missing recordings"
    print("14/14 recordings fit their subtitle windows and camera shots; catalogue references verified.")


if __name__ == "__main__":
    main()
