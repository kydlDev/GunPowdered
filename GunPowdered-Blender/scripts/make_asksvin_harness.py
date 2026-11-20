"""Asksvin tow harness for the Ashlands mobile cannon (D-063): a saddle-draft rig. It hangs on the in-game asksvin saddle, so the player stays seated and rides
while the cannon follows.
Run: blender -b --factory-startup --python make_asksvin_harness.py -- <exports dir> <photos dir> [ref dir]
Space: ASKSVIN SPACE, origin = the asksvin root on the ground, the animal looks along -Y (= +Z in Unity after the export contract), up = +Z. Unity (x, y, z) is
(x, -z, y) here. Numbers come from the AssetRipper project (Asksvin.prefab, SaddleAsksvin.prefab): saddle origin (0, 1.514, -0.591), seat attach (0, 1.515, -0.477),
back height at the saddle about 1.49, torso at the saddle about 1.0 m wide, tail end at z = -1.87. The saddle mesh is 0.86 wide, 1.26 long, 0.65 high.
Parts: a girth belt under the belly with two short straps up to brass D-rings at the saddle sides, two rear D-rings at the saddle's back corners, two leather traces
that run along the flanks and meet a spreader bar behind the tail, a brass centre ring and a tow chain that ends at the CannonHook (the cannon tow ring).
Objects: harness_leather, harness_brass, harness_iron (spreader bar), harness_chain. Empties: SaddleOrigin, SeatAttach, TraceAnchorL, TraceAnchorR, SpreaderCentre, CannonHook.
The reference body, saddle and cannon are only ghosts in the preview pictures (read from the ref dir, never exported).
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

TO_X = Matrix.Rotation(math.radians(90), 4, "Y")        # local +Z -> +X
TO_Y = Matrix.Rotation(math.radians(90), 4, "X")        # local +Z -> -Y
SADDLE = (0.0, 1.514, -0.591)                           # Unity coordinates
HOOK = (0.0, 0.395, -3.50)                              # where the cannon tow ring ends up (Unity coordinates)


def U(x, y, z):
    """Unity asksvin coordinates -> Blender asksvin space."""
    return Vector((x, -z, y))


def side(sign, pts):
    return [U(sign * x, y, z) for x, y, z in pts]


def build_leather(leather):
    bm = bmesh.new()
    up = Vector((0, 0, 1))
    along = Vector((0, 1, 0))
    girth = [(-0.46, 1.46, -0.33), (-0.48, 1.28, -0.33), (-0.40, 1.06, -0.33), (-0.20, 0.92, -0.33), (0.0, 0.89, -0.33),
             (0.20, 0.92, -0.33), (0.40, 1.06, -0.33), (0.48, 1.28, -0.33), (0.46, 1.46, -0.33)]
    g.strap(bm, g.spline([U(*p) for p in girth], 5), 0.11, 0.022, along)                                     # girth belt under the belly
    for s in (-1, 1):
        # both short straps END INSIDE the saddle flap (the flap side is at x 0.41-0.43 at the girth and 0.22 at the rear, measured on saddle_item.obj), so nothing
        # sticks out or hangs loose; a brass plate (build_brass) covers each joint
        g.strap(bm, g.spline(side(s, [(0.46, 1.46, -0.33), (0.445, 1.55, -0.33), (0.425, 1.60, -0.33), (0.385, 1.61, -0.33)]), 4), 0.08, 0.022, along)
        # user 2026-11-20: the traces hung away from the body from two D-rings on the saddle's back corners. They now start at a D-ring on the GIRTH (low on the
        # flank, where the belly band comes up) and run back low along the flank, close to the hide (flank half-widths from the ripped body: 0.43 at the girth,
        # 0.50 over the hind legs at y 1.05-1.2, 0.3 behind z -1.2), to the spreader bar; the rear saddle straps are gone, nothing hangs loose.
        trace = [(0.47, 1.18, -0.33), (0.54, 1.17, -0.62), (0.58, 1.15, -0.95), (0.50, 1.12, -1.35), (0.46, 1.08, -1.85), (0.48, 1.05, -2.30), (0.50, 1.05, -2.55)]
        g.strap(bm, g.spline(side(s, trace), 6), 0.07, 0.022, Vector((1, 0, 0)))                              # trace from the girth to the spreader bar
    return g.put("harness_leather", bm, leather)


def build_brass(brass):
    bm = bmesh.new()
    for s in (-1, 1):
        g.ring(bm, 0.058, 0.038, 0.022, Matrix.Translation(U(s * 0.47, 1.46, -0.33)) @ TO_X, 20)             # girth D-rings
        g.ring(bm, 0.058, 0.038, 0.022, Matrix.Translation(U(s * 0.475, 1.18, -0.33)) @ TO_X, 20)            # trace D-rings on the girth, low on the flank
        g.box(bm, (0.024, 0.09, 0.06), U(s * 0.432, 1.595, -0.33))                                            # plate over the girth strap / saddle flap joint
        g.ring(bm, 0.050, 0.030, 0.026, Matrix.Translation(U(s * 0.50, 1.05, -2.55)) @ TO_Y, 20)             # spreader bar end rings
    g.ring(bm, 0.068, 0.044, 0.030, Matrix.Translation(U(0.0, 1.05, -2.55)) @ TO_X, 20)                       # centre ring
    return g.put("harness_brass", bm, brass)


def build_iron(iron):
    bm = bmesh.new()
    g.cyl(bm, 0.028, 1.04, Matrix.Translation(U(0.0, 1.05, -2.55)) @ TO_X, 12)                               # spreader bar
    for s in (-1, 1):
        g.cyl(bm, 0.040, 0.05, Matrix.Translation(U(s * 0.50, 1.05, -2.55)) @ TO_X, 12)                       # bar end caps
    return g.put("harness_iron", bm, iron)


def chain_frame(d, ax):
    x = d.normalized()
    z = (ax - x * ax.dot(x)).normalized()
    y = z.cross(x)
    return Matrix(((x.x, y.x, z.x, 0), (x.y, y.y, z.y, 0), (x.z, y.z, z.z, 0), (0, 0, 0, 1)))


def build_chain(iron, n=11):
    """Links along a sagging line from the centre ring to the cannon hook; alternate links stand square to each other."""
    a, b = U(0.0, 1.05, -2.62), U(*HOOK)
    pts = []
    for i in range(n + 1):
        t = i / n
        p = a.lerp(b, t)
        p.z -= 0.12 * math.sin(math.pi * t)
        pts.append(p)
    bm = bmesh.new()
    for i in range(n):
        c = (pts[i] + pts[i + 1]) / 2
        d = pts[i + 1] - pts[i]
        d_n = d.normalized()
        sideways = Vector((1, 0, 0))
        ax = sideways if i % 2 == 0 else d_n.cross(sideways)
        g.ring(bm, 0.036, 0.019, 0.020, Matrix.Translation(c) @ chain_frame(d, ax) @ Matrix.Diagonal((1.6, 1.0, 1.0, 1.0)), 14)
    g.ring(bm, 0.060, 0.036, 0.030, Matrix.Translation(U(*HOOK)) @ TO_X, 20)                                   # the hook ring round the cannon tow ring
    return g.put("harness_chain", bm, iron)


def ghost_import(ref, filename, material, scale=1.0):
    path = os.path.join(ref, filename)
    if not os.path.exists(path):
        print("NO REFERENCE", path)
        return None
    bpy.ops.wm.obj_import(filepath=path, forward_axis="NEGATIVE_Z", up_axis="Y")
    objs = list(bpy.context.selected_objects)
    for o in objs:
        o.scale = (scale, scale, scale)
        o.data.materials.append(material)
    return objs


def ghost_cannon(exports):
    """The mobile cannon hitched behind: its trail towards the asksvin, the tow ring on the CannonHook."""
    path = os.path.join(exports, "MobileCannon.fbx")
    if not os.path.exists(path):
        return
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    root = bpy.data.objects.new("cannon_root", None)
    bpy.context.scene.collection.objects.link(root)
    for o in new:
        if o.parent is None:
            o.parent = root
    root.rotation_euler.z = math.pi
    root.location = Vector((0.0, U(*HOOK).y + 1.78, 0.0))              # tow ring (0, 1.78, 0.395 in the cannon file) lands on the hook


exports, photos = g.script_args()
_a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ref = _a[2] if len(_a) > 2 else r"E:\Projects\Valheim\Valheim-Unity-AssetRipper\_export"
sc, cam = g.stage((16.0, 16.0))
leather = g.mat("harness_leather", (0.30, 0.13, 0.055), 0.65)
brass = g.mat("harness_brass", (0.78, 0.55, 0.20), 0.35, 0.9)
iron = g.mat("harness_iron", (0.05, 0.05, 0.055), 0.6, 0.85)
NAMES = [build_leather(leather).name, build_brass(brass).name, build_iron(iron).name, build_chain(iron).name]
EM = [g.empty("SaddleOrigin", U(*SADDLE)).name, g.empty("SeatAttach", U(0.0, 1.515, -0.477)).name,
      g.empty("TraceAnchorL", U(-0.475, 1.18, -0.33)).name, g.empty("TraceAnchorR", U(0.475, 1.18, -0.33)).name,
      g.empty("SpreaderCentre", U(0.0, 1.05, -2.55)).name, g.empty("CannonHook", U(*HOOK)).name]
print("HARNESS TRIANGLES", g.tris(NAMES))
g.export(os.path.join(exports, "Harness.fbx"), NAMES + EM)

# preview only: the real asksvin body and saddle (ghosts) and the cannon behind
body_mat = g.mat("ghost_body", (0.52, 0.50, 0.44), 0.9)
saddle_mat = g.mat("ghost_saddle", (0.14, 0.12, 0.11), 0.8)
ghost_import(ref, "asksvin_body.obj", body_mat, 0.01)
sad = ghost_import(ref, "saddle_item.obj", saddle_mat, 1.0)
for o in sad or []:
    o.location = U(*SADDLE)
ghost_cannon(exports)
c = Vector((0.0, 2.1, 1.0))
stamp = g.stamp()
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-side.png" % stamp), c + Vector((11.0, 0.0, 1.0)), c, 40)
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-3q.png" % stamp), Vector((-6.0, 6.5, 3.4)), Vector((0.0, 1.0, 1.1)), 40)
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-saddle.png" % stamp), Vector((3.4, -0.6, 2.7)), Vector((0.0, 0.7, 1.3)), 50)
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-rear.png" % stamp), Vector((2.4, 4.9, 1.9)), Vector((0.0, 2.3, 0.9)), 50)
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-joint-girth.png" % stamp), Vector((1.7, -0.1, 2.0)), U(0.43, 1.57, -0.33), 50)     # close-ups of the two saddle joints
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-joint-trace.png" % stamp), Vector((2.2, 0.6, 1.6)), U(0.47, 1.18, -0.6), 50)
g.shoot(sc, cam, os.path.join(photos, "%s_asksvin-harness-clevis.png" % stamp), U(*HOOK) + Vector((0.32, -0.42, 0.30)), U(*HOOK), 60)       # the tow clevis: harness ring between the two eye plates, one pin
