"""Static binding check; Unity Play Mode still verifies reach and tap behavior."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
source = (root / 'Assets/Scripts/Interactables/SupplyCounter.cs').read_text()
tags = (root / 'ProjectSettings/TagManager.asset').read_text()
scene = (root / 'Assets/Scenes/Office_Level1.unity').read_text()
layers = tags.split('  layers:\n', 1)[1].split('  m_SortingLayers:', 1)[0]
names = re.findall(r'^  - ?(.*)$', layers, re.M)
layer = names.index('Interactable')
interactor = next(block for block in scene.split('--- !u!')
                  if 'Assembly-CSharp::Interactor\n' in block)
mask = int(re.search(r'm_Bits: (\d+)', interactor).group(1))
assert mask & (1 << layer), 'Level 1 player does not detect Interactable'
assert 'root.layer = LayerMask.NameToLayer("Interactable");' in source, \
    'Supply Counter root collider is left on Default, outside the player interaction mask'
assert 'root.AddComponent<BoxCollider>()' in source
assert 'root.AddComponent<SupplyCounter>()' in source
print('PASS: Supply Counter collider and player interaction layer agree (static check).')
