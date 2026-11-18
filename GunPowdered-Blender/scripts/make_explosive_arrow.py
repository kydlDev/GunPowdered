"""Explosive arrow (D-045 item 5): a black-metal arrow with a small gunpowder charge tied behind the head; it explodes on impact (a Plains item).
Run: blender -b --factory-startup --python make_explosive_arrow.py -- <exports dir> <photos dir>
Design (a little chunkier than life like the other models): 0.80 m ash shaft, three dark-red fletching vanes and a horn nock at the back; a black-metal
leaf-shaped broadhead; right behind the head a fist-sized (3.5 cm) bundle of black leather bound with tan twine that holds the powder, with a red wax seal
and a little black-powder dust on the head. The arrow points along -Y (like the weapons), origin = the middle of the shaft.
Objects: arrow_shaft, arrow_fletching, arrow_nock, arrow_head, arrow_charge, arrow_twine, arrow_seal, arrow_powder.
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

LEN = 0.80
HALF = LEN / 2
TO_NEG_Y = Matrix.Rotation(math.radians(90), 4, "X")           # local +Z -> -Y (the arrow points forward along -Y)


def at(y):
    return Matrix.Translation((0.0, y, 0.0)) @ TO_NEG_Y


BUNDLE = [(0.0, 0.0), (0.004, 0.012), (0.015, 0.021), (0.038, 0.0235), (0.060, 0.019), (0.072, 0.011), (0.075, 0.0)]       # (distance behind the head, radius)


def bundle_radius(d):
    for (d0, r0), (d1, r1) in zip(BUNDLE, BUNDLE[1:]):
        if d0 <= d <= d1:
            return r0 + (r1 - r0) * (d - d0) / (d1 - d0)
    return 0.01


def build():
    ash = g.mat("arrow_ash", (0.55, 0.42, 0.26), 0.85, 0.0)
    red = g.mat("arrow_feather", (0.45, 0.07, 0.05), 0.9, 0.0)
    horn = g.mat("arrow_horn", (0.85, 0.80, 0.68), 0.5, 0.0)
    black = g.mat("arrow_blackmetal", (0.03, 0.03, 0.035), 0.5, 0.9)
    leather = g.mat("arrow_leather", (0.06, 0.05, 0.045), 0.8, 0.0)
    twine = g.mat("arrow_twine", (0.60, 0.50, 0.32), 0.95, 0.0)
    wax = g.mat("arrow_wax", (0.55, 0.05, 0.04), 0.4, 0.0)
    dust = g.mat("arrow_powder", (0.01, 0.01, 0.01), 1.0, 0.0)
    names = []
    tip_y = -HALF                                                # the point is at -Y
    head_len = 0.085
    charge_len = 0.075
    head_back = tip_y + head_len                                 # where the head meets the charge (y grows backwards)
    charge_back = head_back + charge_len

    bm = bmesh.new()                                             # the shaft runs from the charge to the nock
    g.cyl(bm, 0.0075, (HALF - 0.004) - charge_back, Matrix.Translation((0.0, (charge_back + HALF - 0.004) / 2, 0.0)) @ Matrix.Rotation(math.radians(90), 4, "X"), 10)
    names.append(g.put("arrow_shaft", bm, ash).name)

    bm = bmesh.new()                                             # three vanes at 120 degrees, thin tapered boxes
    for k in range(3):
        a = math.radians(90 + 120 * k)
        m = Matrix.Rotation(a, 4, "Y")
        pts = [(HALF - 0.095, 0.007), (HALF - 0.095, 0.034), (HALF - 0.060, 0.038), (HALF - 0.012, 0.022), (HALF - 0.012, 0.007)]
        g.poly_extrude(bm, [(y, z) for y, z in pts], 0.003, m)
    names.append(g.put("arrow_fletching", bm, red, smooth=False).name)

    bm = bmesh.new()                                             # horn nock: a small barrel with a slot
    g.lathe(bm, [(0.0, 0.0), (0.0085, 0.001), (0.011, 0.008), (0.009, 0.020), (0.0, 0.021)], at(HALF + 0.0))
    names.append(g.put("arrow_nock", bm, horn).name)

    bm = bmesh.new()                                             # broadhead: a flat leaf plus a socket
    leaf = [(tip_y, 0.0), (tip_y + 0.030, 0.026), (tip_y + 0.060, 0.030), (head_back, 0.011), (head_back, -0.011), (tip_y + 0.060, -0.030), (tip_y + 0.030, -0.026)]
    g.poly_extrude(bm, leaf, 0.006, Matrix.Rotation(math.radians(90), 4, "Y"))                  # leaf lies in the Y-X plane
    g.lathe(bm, [(0.0, 0.0), (0.011, 0.0), (0.0105, 0.020), (0.0085, 0.030), (0.0, 0.030)], Matrix.Translation((0.0, head_back - 0.004, 0.0)) @ TO_NEG_Y, 14)
    names.append(g.put("arrow_head", bm, black).name)

    bm = bmesh.new()                                             # the powder charge: a leather bundle, fat in the middle, gathered at both ends
    g.lathe(bm, [(0.0, 0.0), (0.012, 0.004), (0.021, 0.015), (0.0235, 0.038), (0.019, 0.060), (0.011, 0.072), (0.0, 0.075)],
            Matrix.Translation((0.0, head_back, 0.0)) @ Matrix.Rotation(math.radians(-90), 4, "X"), 18)
    names.append(g.put("arrow_charge", bm, leather).name)

    bm = bmesh.new()                                             # twine: three turns near the head end and two at the shaft end
    for d in (0.012, 0.019, 0.026, 0.056, 0.064):
        r = bundle_radius(d)
        g.ring(bm, r + 0.0022, r - 0.0004, 0.0042, Matrix.Translation((0.0, head_back + d, 0.0)) @ TO_NEG_Y, 20)
    names.append(g.put("arrow_twine", bm, twine).name)

    bm = bmesh.new()                                             # red wax seal on the top of the bundle
    g.cyl(bm, 0.0085, 0.006, Matrix.Translation((0.0, head_back + 0.036, 0.0222)), 12)
    names.append(g.put("arrow_seal", bm, wax).name)

    bm = bmesh.new()                                             # a pinch of black powder spilled on the leaf
    for k, (dy, dx) in enumerate([(0.020, 0.012), (0.028, -0.010), (0.040, 0.008), (0.050, -0.006)]):
        g.cyl(bm, 0.004 - 0.0004 * k, 0.003, Matrix.Translation((dx, tip_y + dy, 0.0035)), 8)
    names.append(g.put("arrow_powder", bm, dust).name)
    return names


exports, photos = g.script_args()
sc, cam = g.stage((2.0, 1.6))
NAMES = build()
for n in NAMES:                                                  # lift the arrow so it lies on the ground in the preview only (origin stays the shaft middle)
    bpy.data.objects[n].location.z += 0.03
g.empty("Tip", (0.0, -HALF, 0.0))
g.empty("Charge", (0.0, -HALF + 0.085 + 0.0375, 0.0))
print("ARROW TRIANGLES", g.tris(NAMES))
for n in NAMES:
    bpy.data.objects[n].location.z -= 0.03
g.export(os.path.join(exports, "ExplosiveArrow.fbx"), NAMES + ["Tip", "Charge"])
for n in NAMES:
    bpy.data.objects[n].location.z += 0.03
g.views(photos, "explosive-arrow", (0.0, -0.10, 0.04), 1.0, 0.25)
g.views(photos, "explosive-arrow-head", (0.0, -0.30, 0.04), 0.38, 0.10)
