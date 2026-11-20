"""Spiked Grenade (D-045 item 1, D-047): a round clay pot full of gunpowder with short spikes all over and a fuse in the neck,
after the user's picture of Song-era spiked ceramic grenades. Run inside Blender. Units: 1 Blender unit = 1 m.
Parts: grenade_body (pot + spikes, dark grey clay), grenade_fuse (twisted cord standing out of the neck).
Size: about 12 cm across the belly, 14 cm high with the neck; the spikes stand out about 2.5 cm. Object origin = bottom centre.
Placed at x=23 (next to the items of make_items.py). build() makes it, export() writes exports/Grenade.fbx, icon() renders the inventory icon.
"""
import math
import random
import runpy
import bmesh
import bpy
from mathutils import Matrix, Vector
import os
# paths from this script's own folder (D-134, release gate G18: no PC paths in tracked files): scripts\ -> GunPowdered-Blender\ -> GunPowdered\ -> the Valheim folder
_HERE = os.path.dirname(os.path.abspath(__file__))
_VALHEIM = os.path.normpath(os.path.join(_HERE, "..", "..", ".."))

H = runpy.run_path(os.path.join(_HERE, "make_leaching_station.py"), run_name="helpers")
mat, lathe = H["mat"], H["lathe"]
ITEMS = runpy.run_path(os.path.join(_HERE, "make_items.py"), run_name="helpers")
OUT_DIR = os.path.normpath(os.path.join(_HERE, "..", "exports"))
SPOT = Vector((23.0, 0.0, 0.0))
CLAY = (0.16, 0.15, 0.14)       # warm dark grey fired clay (the picture), not a cold metal grey
FUSE = (0.45, 0.36, 0.22)
BELLY_Z = 0.062           # height of the widest ring, the centre the spikes point away from
SPIKES = 12

# outer profile of the pot, (radius, height), closed at the axis: foot, belly, shoulder, neck, lip, and the mouth plugged a little below the lip
POT = [(0.0, 0.0), (0.030, 0.0), (0.040, 0.008), (0.052, 0.024), (0.060, 0.045), (0.062, BELLY_Z), (0.059, 0.080), (0.049, 0.099),
       (0.032, 0.112), (0.021, 0.118), (0.020, 0.128), (0.025, 0.131), (0.025, 0.138), (0.015, 0.138), (0.015, 0.127), (0.0, 0.127)]


def put(name, bm, material):
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    box_uv(bm)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    coll = bpy.data.collections.get("Weapons") or bpy.data.collections.new("Weapons")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    obj.location = SPOT
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = True
    return obj


def box_uv(bm, tile=0.12):
    """Cube projection (D-029: every model is box-mapped, one texture tile = min(size, 0.5 m), here the pot's 12 cm): each face takes the
    two axes across its main normal direction."""
    uv = bm.loops.layers.uv.new("UVMap")
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        u_i, v_i = [(1, 2), (0, 2), (0, 1)][ax]
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co[u_i] / tile, loop.vert.co[v_i] / tile)


def spike_dirs(rng):
    """SPIKES directions spread evenly over the pot (a Fibonacci spiral), kept off the neck and the foot."""
    dirs = []
    golden = math.pi * (3.0 - math.sqrt(5.0))
    k = 0
    while len(dirs) < SPIKES:
        y = 0.62 - 1.35 * (k + 0.5) / (SPIKES + 3)       # from upper shoulder down to the lower belly
        r = math.sqrt(max(0.0, 1.0 - y * y))
        a = golden * k + rng.uniform(-0.15, 0.15)
        dirs.append(Vector((r * math.cos(a), r * math.sin(a), y)).normalized())
        k += 1
    return dirs


def build_body(rng):
    bm = bmesh.new()
    lathe(bm, POT, segs=28)
    # hand-made: every vertex of the pot pushed in or out up to 2 mm, so the belly is not a perfect lathe
    for v in bm.verts:
        flat = Vector((v.co.x, v.co.y, 0.0))
        if flat.length > 0.018:
            v.co += flat.normalized() * rng.uniform(-0.002, 0.002)
    centre = Vector((0.0, 0.0, BELLY_Z))
    for d in spike_dirs(rng):
        length = rng.uniform(0.020, 0.028)
        base_r = rng.uniform(0.015, 0.019)              # stubby, wide-based clay spikes like the picture, not needles
        # the cone's base sits a little inside the clay; its axis points away from the belly centre, slightly crooked like hand-made clay
        d = (d + Vector((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), rng.uniform(-0.05, 0.05)))).normalized()
        surface = centre + d * 0.058
        mid = surface + d * (length * 0.5 - 0.004)
        rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=True, segments=7, radius1=base_r, radius2=0.003, depth=length,
                              matrix=Matrix.Translation(mid) @ rot)
    return put("grenade_body", bm, mat("grenade_clay_mat", CLAY, rough=0.92))


def build_fuse():
    """A cord out of the plug in the neck that leans over to one side: a chain of short tubes along a gentle curve."""
    bm = bmesh.new()
    pts = [Vector((0.0, 0.0, 0.125)), Vector((0.002, 0.0, 0.142)), Vector((0.008, 0.002, 0.156)), Vector((0.018, 0.004, 0.166)), Vector((0.030, 0.005, 0.170))]
    for a, b in zip(pts, pts[1:]):
        d = b - a
        rot = d.normalized().to_track_quat("Z", "Y").to_matrix().to_4x4()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=0.0035, radius2=0.0032, depth=d.length + 0.002,
                              matrix=Matrix.Translation((a + b) / 2) @ rot)
    return put("grenade_fuse", bm, mat("grenade_fuse_mat", FUSE, rough=0.95))


def build(seed=7):
    rng = random.Random(seed)
    body = build_body(rng)
    fuse = build_fuse()
    tris = sum(len(p.vertices) - 2 for o in (body, fuse) for p in o.data.polygons)
    return {"objects": [body.name, fuse.name], "triangles": tris}


def export():
    bpy.ops.object.select_all(action="DESELECT")
    names = ["grenade_body", "grenade_fuse"]
    for n in names:
        o = bpy.data.objects[n]
        o.location = (0.0, 0.0, 0.0)
        o.select_set(True)
    path = OUT_DIR + "\\Grenade.fbx"
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE")
    for n in names:
        bpy.data.objects[n].location = SPOT
    return path


def icon(path):
    ITEMS["icon"](["grenade_body", "grenade_fuse"], path)
    return path
