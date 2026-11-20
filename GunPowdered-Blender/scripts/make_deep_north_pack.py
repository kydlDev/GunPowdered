"""Deep North Dynamite Pack (D-108, user sketch 2026-11-20): four sticks of dynamite standing in the corners, two Spiked Grenades stacked in the middle,
four SealHide straps round the bundle, each tied off with a bone toggle.
Run: blender -b --factory-startup --python make_deep_north_pack.py -- <exports dir> <photos dir> [<icon path>]
The sticks and grenades are the real Dynamite.fbx and Grenade.fbx (imported, so they stay identical to the single items and keep their material names
for Unity's remap); only the straps and toggles are new. Object origin = bottom centre, the pack stands up (+Z), about 0.27 m tall, 0.26 m across.
Objects: pack_dynamite_1..4 (+ their parts), pack_grenade_low, pack_grenade_high (+ fuse), pack_straps, pack_toggles.
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

R = 0.022            # dynamite casing radius (make_dynamite.py)
L = 0.20             # casing length
SPIKE_R = 0.075      # grenade radius over its spikes (measured from Grenade.fbx)
GRENADE_H = 0.138    # grenade body height with spikes
DIAG = SPIKE_R + R + 0.004            # centre of the pack to the centre of a stick (corner)
D = DIAG / math.sqrt(2)               # the sticks stand at (+-D, +-D)
STICK_Z = 0.035                       # sticks stand a little up so the straps frame the grenade stack
STRAP_W, STRAP_T = 0.020, 0.004
STRAP_ON_TUBE = R + 0.0075 + STRAP_T / 2     # strap centre line round a stick: clears the band rivets (R + 0.0073)
STRAP_Z = [STICK_Z + z for z in (0.030, 0.080, 0.130, 0.180)]     # four straps, evenly up the sticks


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y")
    return [o for o in bpy.data.objects if o not in before]


def group(objs, name, loc, rot_z):
    """Puts imported parts under one empty so they move together; returns the empty."""
    root = g.empty(name, loc, (0, 0, rot_z))
    for o in objs:
        if o.parent is None:
            o.parent = root
    return root


def loop_points(z, segs_per_arc=10):
    """The strap's centre line at height z: the convex hull of the four sticks grown by the strap (a rounded square, tight on the sticks)."""
    pts = []
    corners = [(D, D), (-D, D), (-D, -D), (D, -D)]
    for i, (cx, cy) in enumerate(corners):
        a0 = math.pi / 4 + i * math.pi / 2 - math.pi / 4         # each corner wraps a quarter turn, facing out
        for s in range(segs_per_arc + 1):
            a = a0 + (math.pi / 2) * s / segs_per_arc
            pts.append(Vector((cx + math.cos(a) * STRAP_ON_TUBE, cy + math.sin(a) * STRAP_ON_TUBE, z)))
    pts.append(pts[0].copy())
    return pts


def build(exports):
    hide = g.mat("pack_sealhide", (0.17, 0.13, 0.10), 0.95, 0.0)       # dark brown-grey seal hide (first try 0.30 grey read as metal)
    bone = g.mat("pack_toggle", (0.78, 0.73, 0.62), 0.6, 0.0)          # bone toggles
    names = []

    for i, (sx, sy) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        parts = import_fbx(os.path.join(exports, "Dynamite.fbx"))
        out = math.atan2(sy, sx)                                         # fuse (local +X) leans out of the corner
        root = group(parts, "pack_dynamite_%d" % (i + 1), (sx * D, sy * D, STICK_Z), out)
        for o in parts:
            o.name = "pack_dyn%d_%s" % (i + 1, o.name.split(".")[0].replace("dynamite_", ""))
            names.append(o.name)
        names.append(root.name)

    low = import_fbx(os.path.join(exports, "Grenade.fbx"))
    for o in low:
        if o.name.startswith("grenade_fuse"):
            bpy.data.objects.remove(o, do_unlink=True)                    # the lower grenade's fuse would stick into the upper one
    low = [o for o in bpy.data.objects if o.name.startswith("grenade_") and o.parent is None and o.type == "MESH"]
    root = group(low, "pack_grenade_low", (0, 0, 0.0), math.radians(20))
    for o in low:
        o.name = "pack_grenade_low_" + o.name.split(".")[0].replace("grenade_", "")
        names.append(o.name)
    names.append(root.name)
    high = import_fbx(os.path.join(exports, "Grenade.fbx"))
    root = group(high, "pack_grenade_high", (0, 0, GRENADE_H - 0.010), math.radians(65))
    for o in high:
        o.name = "pack_grenade_high_" + o.name.split(".")[0].replace("grenade_", "")
        names.append(o.name)
    names.append(root.name)

    bm = bmesh.new()
    for z in STRAP_Z:
        g.strap(bm, loop_points(z), STRAP_W, STRAP_T, Vector((0, 0, 1)))
    names.append(g.put("pack_straps", bm, hide, smooth=False).name)      # flat: a smooth-shaded thin strap looked like a pipe

    bm = bmesh.new()                                                     # a bone toggle where each strap is tied, on the +X side between two sticks
    r_side = D + STRAP_ON_TUBE + STRAP_T
    for k, z in enumerate(STRAP_Z):
        y = (-0.012 if k % 2 else 0.012)
        g.cyl(bm, 0.0045, 0.034, Matrix.Translation((r_side + 0.002, y, z)) @ Matrix.Rotation(math.radians(80 if k % 2 else 100), 4, "X"), 8)
        g.box(bm, (0.006, 0.014, STRAP_W * 1.1), Vector((r_side, y * 0.3, z)))     # the tied knot under it
    names.append(g.put("pack_toggles", bm, bone).name)
    return names


def icon(names, path, size=256):
    """Transparent 256 px inventory icon, front and a little above (the make_items.py way)."""
    sc = bpy.context.scene
    objs = [bpy.data.objects[n] for n in names if bpy.data.objects[n].type == "MESH"]
    for o in bpy.data.objects:
        o.hide_render = o.type == "MESH" and o not in objs
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (lo + hi) / 2
    cam = bpy.data.objects.new("icon_cam", bpy.data.cameras.new("icon_cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(hi - lo) * 1.45
    sc.collection.objects.link(cam)
    cam.location = c + Vector((0.55, -1.0, 0.8))
    cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
    old = sc.camera
    sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.film_transparent = True
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    sc.camera = old
    for o in bpy.data.objects:
        o.hide_render = False


exports, photos = g.script_args()
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
icon_path = argv[2] if len(argv) > 2 else None
sc, cam = g.stage((1.2, 1.0))
NAMES = build(exports)
mesh_names = [n for n in NAMES if bpy.data.objects[n].type == "MESH"]
print("DEEP NORTH PACK TRIANGLES", g.tris(mesh_names), "sticks at +-%.4f, strap loop on stick %.4f" % (D, STRAP_ON_TUBE))
g.export(os.path.join(exports, "DeepNorthPack.fbx"), NAMES)
g.views(photos, "deep-north-pack", (0.0, 0.0, 0.14), 0.95, 0.15)
sc.render.resolution_x, sc.render.resolution_y = 1400, 900
g.shoot(sc, cam, os.path.join(photos, "%s_deep-north-pack-top.png" % g.stamp()), (0.0, -0.05, 1.0), (0.0, 0.0, 0.14))
if icon_path:
    icon(mesh_names, icon_path)
