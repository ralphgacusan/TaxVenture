"""Read actual shipped font metrics and serialized T03 text geometry; never launch Unity.
This checks glyphs, fixed-size line fit and the anchor/height fault seen in T02.
It does not certify rendered contrast, font/shader appearance or phone readability.
"""
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
FONT = ROOT / 'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset'
font = FONT.read_text()
glyphs = {int(i): float(a) for i, a in re.findall(r'- m_Index: (\d+).*?m_HorizontalAdvance: ([\d.]+)', font, re.S)}
chars = {int(u): int(i) for u, i in re.findall(r'm_Unicode: (\d+)\s+m_GlyphIndex: (\d+)', font)}
point = float(re.search(r'm_PointSize: ([\d.]+)', font)[1])
line_height = float(re.search(r'm_LineHeight: ([\d.]+)', font)[1])
checks = 0


def field(body, name):
    return re.search(r'^  ' + re.escape(name) + r': (.*)$', body, re.M)[1]


def xy(value):
    return [float(re.search(axis + r': ([-+\d.eE]+)', value)[1]) for axis in 'xy']


for path in sorted((ROOT / 'Assets/Cutscenes/Chapter03').glob('*.prefab')):
    docs = {fid: (int(cls), body) for cls, fid, body in re.findall(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- |\Z)', path.read_text(), re.M | re.S)}
    rects = {re.search(r'fileID: (\d+)', field(body, 'm_GameObject'))[1]: body for cls, body in docs.values() if cls == 224}
    for cls, body in docs.values():
        if cls != 114 or '9541d86e2fd84c1d9990edf0852d74ab' not in body:
            continue
        text = re.sub(r'<[^>]+>', '', json.loads(field(body, 'm_text')))
        rect = rects[re.search(r'fileID: (\d+)', field(body, 'm_GameObject'))[1]]
        width, height = xy(field(rect, 'm_SizeDelta'))
        pos, anchor = xy(field(rect, 'm_LocalPosition')), xy(field(rect, 'm_AnchoredPosition'))
        assert pos == anchor and pos[1] > 0, (path.name, text, 'anchor can bury/lift printed text', pos, anchor)
        assert field(body, 'm_enableAutoSizing') == '0', (path.name, text, 'unexpected auto-shrink')
        size = float(field(body, 'm_fontSize'))
        rows = text.split('\n')
        for row in rows:
            missing = [c for c in row if ord(c) not in chars]
            assert not missing, (path.name, text, 'missing glyphs', missing)
            # Same installed TMP perspective multiplier and 4% bold allowance as VerifyLevel3Print.
            advance = sum(glyphs[chars[ord(c)]] for c in row) * size / point * .1 * 1.04
            assert advance <= width, (path.name, row, 'too wide', advance, width)
            checks += 1
        full_height = line_height * size / point * .1 * len(rows)
        assert full_height <= height, (path.name, text, 'too tall', full_height, height)
        checks += 2
        print(f'PASS {path.stem}: {text!r}, fixed {size:g}, height {full_height:.5f}/{height:.5f} m')
print(f'{checks} print geometry checks passed (offline, not rendered acceptance)')
