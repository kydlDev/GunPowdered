"""Powder Mill (gunpowder station): a stone pan with two heavy edge-runner stones turned by a wooden sweep arm, on a
plank platform, with two powder barrels beside it. Run inside Blender. Units: 1 Blender unit = 1 m.
Station origin = ground centre (placed at x=8). Reuses the helper functions of make_leaching_station.py.
Parts: mill_base, mill_pan, mill_frame (fixed), mill_arm + mill_runners (rotating together, pivot at the shaft: the object origin is the pivot), mill_barrels,
mill_barrel_hoops, mill_powder (state: black powder in the pan).
"""
import math
import runpy
import bmesh
import bpy
from mathutils import Matrix, Vector

H = runpy.run_path(r"E:\Projects\Valheim\GunPowdered\GunPowdered-Blender\scripts\make_leaching_station.py",
                   run_name="helpers")
mat, lathe, box, blob, strut = H["mat"], H["lathe"], H["box"], H["blob"], H["strut"]
ORIGIN = Vector((8.0, 0.0, 0.0))
PAN_Z = 0.18              # the pan sits on the platform
PIVOT = Vector((0.0, 0.0, 0.49))
WOOD = (0.30, 0.17, 0.08)
IRON = (0.035, 0.032, 0.03)
BLACK_STONE = (0.045, 0.045, 0.05)   # the grinding stones are polished black stone
GREY_STONE = (0.42, 0.41, 0.39)      # the pan is plain grey stone


def put(name, bm, material, pivot=None):
    """Mesh in station coordinates; with a pivot the object origin is the pivot (so Unity can rotate it)."""
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    if pivot is not None:
        bmesh.ops.translate(bm, vec=-pivot, verts=bm.verts)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    coll = bpy.data.collections.get("Mill") or bpy.data.collections.new("Mill")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    obj.location = ORIGIN + (pivot if pivot is not None else Vector((0, 0, 0)))
    me.materials.append(material)
    return obj


def build_base():
    bm = bmesh.new()
    box(bm, Vector((0, 0, 0.09)), (2.8, 1.5, 0.18))
    for k in range(6):                       # plank lines on the platform top
        box(bm, Vector((0, -0.62 + k * 0.248, 0.182)), (2.78, 0.012, 0.004))
    return put("mill_base", bm, mat("mill_wood_mat", WOOD, 0.8))


def build_pan():
    bm = bmesh.new()
    prof = [(0.0, 0.0), (0.66, 0.0), (0.66, 0.17), (0.58, 0.17), (0.55, 0.06), (0.0, 0.05)]
    lathe(bm, prof, 0.0, 0.0, PAN_Z, 40)
    return put("mill_pan", bm, mat("mill_pan_stone_mat", GREY_STONE, 0.95))


def cog(bm, z):
    """A small toothed wheel on top of the shaft (drive: the mill turns by itself, nobody pushes the arm)."""
    bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=0.13, radius2=0.13, depth=0.05,
                          matrix=Matrix.Translation(Vector((0, 0, z))))
    for k in range(14):
        a = 2 * math.pi * k / 14
        box(bm, Vector((0.15 * math.cos(a), 0.15 * math.sin(a), z)), (0.07, 0.04, 0.05), 0.0, a)


def build_frame():
    """Fixed frame: two uprights and a crossbeam that holds the shaft at the top."""
    bm = bmesh.new()
    for sy in (-1, 1):
        box(bm, Vector((0, sy * 0.74, 0.69)), (0.09, 0.09, 1.02))
    for sy in (-1, 1):                     # the crossbeam stops at a wooden ring; it does not touch the shaft
        box(bm, Vector((0, sy * 0.485, 1.22)), (0.10, 0.67, 0.10))
    # bearing ring: the shaft (radius 0.06) runs through it with a 0.025 m gap all around, so nothing rubs
    lathe(bm, [(0.085, 1.17), (0.15, 1.17), (0.15, 1.27), (0.085, 1.27)], 0.0, 0.0, 0.0, 32)
    return put("mill_frame", bm, mat("mill_wood_mat", WOOD, 0.8))


def build_arm():
    """Wooden shaft, sweep beam, handle crossbar and collar (+ the two stone runners as their own object).
    Both objects use the shaft axis at z=0.49 as pivot, so Unity rotates them together."""
    bm, rn = bmesh.new(), bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.06, radius2=0.06, depth=1.17,
                          matrix=Matrix.Translation(Vector((0, 0, 0.815))))    # shaft z 0.23 .. 1.40
    box(bm, Vector((0, 0, 0.49)), (1.24, 0.10, 0.10))                           # sweep beam along x
    cog(bm, 1.40)                                                                # drive cog on top: the mill turns itself
    box(bm, Vector((0, 0, 0.30)), (0.20, 0.20, 0.10))                           # collar
    for sx in (-1, 1):
        mm = Matrix.Translation(Vector((sx * 0.34, 0, 0.49))) @ Matrix.Rotation(math.pi / 2, 4, "Y")
        bmesh.ops.create_cone(rn, cap_ends=True, segments=24, radius1=0.26, radius2=0.26, depth=0.14, matrix=mm)
    put("mill_runners", rn, mat("mill_polished_stone_mat", BLACK_STONE, 0.08, 0.1), PIVOT)
    return put("mill_arm", bm, mat("mill_wood_mat", WOOD, 0.8), PIVOT)

def build_barrels():
    bm, hb = bmesh.new(), bmesh.new()
    prof = [(0.0, 0.0), (0.17, 0.0), (0.20, 0.22), (0.17, 0.44), (0.15, 0.44), (0.18, 0.22), (0.15, 0.04), (0.0, 0.04)]
    for sy in (-1, 1):
        lathe(bm, prof, 1.15, sy * 0.52, 0.18, 24)
        for z in (0.10, 0.34):
            r = 0.17 + 0.03 * (1 - abs(z - 0.22) / 0.22)
            lathe(hb, [(r - 0.004, z - 0.016), (r + 0.010, z - 0.016), (r + 0.010, z + 0.016), (r - 0.004, z + 0.016)],
                  1.15, sy * 0.52, 0.18, 24)
    put("mill_barrel_hoops", hb, mat("mill_iron_mat", IRON, 0.45, 0.7))
    return put("mill_barrels", bm, mat("mill_wood_mat", WOOD, 0.8))


def build_powder():
    bm = bmesh.new()
    for k in range(16):
        a, d = 2 * math.pi * k / 16, 0.22 + 0.08 * (k % 3)
        blob(bm, Vector((0.9 * d * math.cos(a), 0.9 * d * math.sin(a), PAN_Z + 0.09)), 0.075, (1, 1, 0.5))
    blob(bm, Vector((0, 0, PAN_Z + 0.07)), 0.20, (1, 1, 0.45))
    return put("mill_powder", bm, mat("mill_powder_mat", (0.02, 0.02, 0.022), 0.95))


PARTS = ["mill_base", "mill_pan", "mill_frame", "mill_arm", "mill_runners", "mill_barrels", "mill_barrel_hoops", "mill_powder"]


def build():
    return [build_base(), build_pan(), build_frame(), build_arm(), build_barrels(), build_powder()]


def export(path, names=PARTS):
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        bpy.data.objects[n].select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE")


if __name__ == "__main__":
    build()
