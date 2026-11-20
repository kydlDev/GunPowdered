"""Rain Barrel: an upright open-top wooden barrel with black iron hoops.
Run inside Blender. Units: 1 Blender unit = 1 m. Origin = ground centre (placed at x=12).
Parts: rain_barrel (wood), rain_barrel_hoops, rain_barrel_water. No tap: the player interacts with the barrel and takes all the water at once.
rain_barrel_water is a disc drawn at the FULL level (z=0.86). The mod moves it down toward z=EMPTY_Z as the barrel
empties (fill / capacity); at 0 it is hidden. Reuses the helper functions of make_leaching_station.py.
"""
import math
import runpy
import bmesh
import bpy
from mathutils import Matrix, Vector
import os
# paths from this script's own folder (D-134, release gate G18: no PC paths in tracked files): scripts\ -> GunPowdered-Blender\ -> GunPowdered\ -> the Valheim folder
_HERE = os.path.dirname(os.path.abspath(__file__))
_VALHEIM = os.path.normpath(os.path.join(_HERE, "..", "..", ".."))

H = runpy.run_path(os.path.join(_HERE, "make_leaching_station.py"),
                   run_name="helpers")
mat, lathe, box = H["mat"], H["lathe"], H["box"]
ORIGIN = Vector((12.0, 0.0, 0.0))
HEIGHT = 1.0
FULL_Z, EMPTY_Z = 0.86, 0.13
OUT = [(0.36, 0.0), (0.38, 0.04), (0.43, 0.30), (0.44, 0.50), (0.42, 0.75), (0.39, HEIGHT)]
WALL = 0.035
WOOD = (0.30, 0.17, 0.08)
IRON = (0.035, 0.032, 0.03)


def put(name, bm, material):
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    coll = bpy.data.collections.get("RainBarrel") or bpy.data.collections.new("RainBarrel")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    obj.location = ORIGIN
    me.materials.append(material)
    return obj


def radius_at(z):
    for (r0, z0), (r1, z1) in zip(OUT, OUT[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    return OUT[-1][0]


def build_barrel():
    bm = bmesh.new()
    inner = [(r - WALL, z) for r, z in reversed(OUT[1:])]
    lathe(bm, [(0.0, 0.0)] + OUT + inner + [(0.0, 0.10)], 0.0, 0.0, 0.0, 36)
    return put("rain_barrel", bm, mat("rain_wood_mat", WOOD, 0.8))


def build_hoops():
    bm = bmesh.new()
    for z in (0.18, 0.50, 0.88):
        r = radius_at(z)
        lathe(bm, [(r - 0.004, z - 0.024), (r + 0.014, z - 0.024), (r + 0.014, z + 0.024), (r - 0.004, z + 0.024)],
              0.0, 0.0, 0.0, 40)
    return put("rain_barrel_hoops", bm, mat("rain_iron_mat", IRON, 0.45, 0.7))


def build_water():
    bm = bmesh.new()
    bmesh.ops.create_circle(bm, cap_ends=True, radius=radius_at(FULL_Z) - WALL - 0.004, segments=36,
                            matrix=Matrix.Translation(Vector((0, 0, FULL_Z))))
    return put("rain_barrel_water", bm, mat("rain_water_mat", (0.20, 0.42, 0.62), 0.1, 0.0, None, 0.75))


PARTS = ["rain_barrel", "rain_barrel_hoops", "rain_barrel_water"]


def build():
    return [build_barrel(), build_hoops(), build_water()]


def export(path, names=PARTS):
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        bpy.data.objects[n].select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE")


if __name__ == "__main__":
    build()
