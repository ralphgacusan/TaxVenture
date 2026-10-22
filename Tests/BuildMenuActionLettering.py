"""Run with Blender --background --factory-startup --python Tests/BuildMenuActionLettering.py.
Derive menu labels from the existing Settings font/geometry; never edit Settings.
No Unity, rendering or player-save access. Rebuilding verifies the recovered style
against every original Settings vertex before exporting the additional words.
"""
from pathlib import Path
import bpy
from mathutils import Vector, kdtree

ROOT = Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT / "Assets/3D Models/Buttons/settings_button.fbx"))
source = bpy.data.objects["Text.002"]
font = bpy.data.fonts.load(str(ROOT / "Assets/fonts/Minnie.ttf"))


def bounds(mesh):
    return tuple(Vector(tuple(f(v.co[i] for v in mesh.vertices) for i in range(3)))
                 for f in (min, max))


low, high = bounds(source.data)
center = (low + high) / 2
size = high - low


def lettering(word):
    curve = bpy.data.curves.new(word, "FONT")
    curve.font = font
    curve.body = word
    # Recovered from Settings: these reproduce all 35,412 original vertices.
    curve.size = 0.904
    curve.resolution_u = 12
    curve.bevel_depth = 0.018
    curve.bevel_resolution = 4
    obj = bpy.data.objects.new(word, curve)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    a, b = bounds(obj.data)
    shift = center - (a + b) / 2
    for vertex in obj.data.vertices:
        vertex.co += shift
    obj.matrix_world = source.matrix_world.copy()
    return obj


reference = lettering("Settings")
assert len(reference.data.vertices) == len(source.data.vertices) == 35412
kd = kdtree.KDTree(len(source.data.vertices))
for i, vertex in enumerate(source.data.vertices):
    kd.insert(vertex.co, i)
kd.balance()
error = max(kd.find(v.co)[2] for v in reference.data.vertices)
assert error < 0.00001, f"Recovered Settings font/style differs from original: {error}"
print(f"PASS: Settings font, size and bevel reproduce original mesh; maximum vertex error {error:.9f}")
bpy.data.objects.remove(reference, do_unlink=True)

# Reuse the existing white lettering region of the existing button texture.
# Pick an original UV with an opaque-white 5x5 pixel neighbourhood, away from seams.
image = bpy.data.images.load(str(ROOT / "Assets/3D Models/Buttons/settings_button.fbm/buttons_texture.png"))
pixels = list(image.pixels)
w, h = image.size
white_uv = None
for loop in source.data.uv_layers.active.data:
    x, y = int(loop.uv.x * w), int(loop.uv.y * h)
    if 2 <= x < w - 2 and 2 <= y < h - 2 and all(
        min(pixels[((y + dy) * w + x + dx) * 4:((y + dy) * w + x + dx) * 4 + 4]) > 0.99
        for dy in range(-2, 3) for dx in range(-2, 3)
    ):
        white_uv = ((x + .5) / w, (y + .5) / h)
        break
assert white_uv is not None, "Original Settings texture has no interior white lettering sample"

labels = []
for word in ("Play", "New Game", "Continue"):
    obj = lettering(word)
    obj.name = obj.data.name = word.replace(" ", "")
    a, b = bounds(obj.data)
    extent = b - a
    # Only scale down longer words; keep original proportions, bevel and geometry.
    fit = min(1.0, size.x / extent.x, size.y / extent.y)
    for vertex in obj.data.vertices:
        vertex.co = center + (vertex.co - center) * fit
    # Converted Blender text already has UV0. Replace it rather than appending
    # UV1: Unity's existing Settings material samples TEXCOORD0.
    for layer in list(obj.data.uv_layers):
        obj.data.uv_layers.remove(layer)
    uv = obj.data.uv_layers.new(name="UVMap")
    for loop in uv.data:
        loop.uv = white_uv
    assert len(obj.data.uv_layers) == 1
    assert all(tuple(loop.uv) == tuple(uv.data[0].uv) for loop in obj.data.uv_layers[0].data)
    # The runtime keeps the original Settings renderer/materials; no new material.
    obj.data.materials.clear()
    labels.append(obj)
    a, b = bounds(obj.data)
    assert (b - a).x <= size.x + 1e-5 and (b - a).y <= size.y + 1e-5
    assert len(obj.data.vertices) > 1000 and (b - a).z > 0.02
    print(f"PASS: {word}: {len(obj.data.vertices)} vertices; uniform fit {fit:.6f}")

bpy.ops.object.select_all(action="DESELECT")
for obj in labels:
    obj.select_set(True)
bpy.context.view_layer.objects.active = labels[0]
output = ROOT / "Assets/Resources/MenuActionLettering.fbx"
bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, object_types={"MESH"},
                         axis_forward="-Z", axis_up="Y", bake_anim=False,
                         add_leaf_bones=False, use_mesh_modifiers=False)
print("Exported", output)
# Verify the exported FBX, not just Blender's working mesh. UV order is part
# of the asset/material contract and was missed by the earlier geometry check.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(output))
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    assert len(obj.data.uv_layers) == 1, f"{obj.name}: Unity must receive the white UVs as UV0"
    assert all(abs(loop.uv.x - white_uv[0]) < 1e-6 and abs(loop.uv.y - white_uv[1]) < 1e-6
               for loop in obj.data.uv_layers[0].data), f"{obj.name}: exported UV0 differs"
    print(f"PASS: exported {obj.name} has opaque-white UV0 and no extra UV layer")
