"""Dynamite (D-045 item 6): a cylindrical metal casing filled with gunpowder, lit with a torch. For gathering: it digs round holes, hardly hurts creatures.
Run: blender -b --factory-startup --python make_dynamite.py -- <exports dir> <photos dir>
Design: stout dark-iron tube (about 4 cm across, 20 cm long, a little bigger than life like the other models), rounded ends, two riveted bands, a red-painted
warning band, a screw-in cap with a short twisted fuse (a bent tan cord with a charred tip). Object origin = bottom centre, the tube stands up (+Z).
Objects: dynamite_casing, dynamite_bands, dynamite_marking, dynamite_cap, dynamite_fuse.
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

L = 0.20            # casing length
R = 0.022           # casing radius


def build():
    iron = g.mat("dyn_iron", (0.09, 0.09, 0.10), 0.55, 0.8)
    band = g.mat("dyn_band", (0.22, 0.17, 0.10), 0.5, 0.9)             # dull bronze bands
    red = g.mat("dyn_red", (0.55, 0.06, 0.04), 0.7, 0.0)
    cap = g.mat("dyn_cap", (0.18, 0.13, 0.08), 0.6, 0.7)
    cord = g.mat("dyn_fuse", (0.62, 0.52, 0.34), 0.95, 0.0)
    names = []

    bm = bmesh.new()                                                     # casing: tube with rounded bottom and a slightly narrower shoulder at the top
    g.lathe(bm, [(0.0, 0.0), (R * 0.55, 0.002), (R * 0.88, 0.008), (R, 0.02), (R, L - 0.03), (R * 0.93, L - 0.012), (R * 0.72, L - 0.002), (R * 0.70, L)], segs=32)
    names.append(g.put("dynamite_casing", bm, iron).name)

    bm = bmesh.new()                                                     # two riveted bands, rivets as small domes
    for z in (0.045, L - 0.06):
        g.ring(bm, R + 0.0035, R - 0.001, 0.014, Matrix.Translation((0, 0, z)), 32)
        for i in range(8):
            a = 2 * math.pi * i / 8
            g.cyl(bm, 0.0028, 0.004, Matrix.Translation((math.cos(a) * (R + 0.0045), math.sin(a) * (R + 0.0045), z)) @ Matrix.Rotation(a + math.pi / 2, 4, "Z") @ Matrix.Rotation(math.pi / 2, 4, "X"), 8)
    names.append(g.put("dynamite_bands", bm, band).name)

    bm = bmesh.new()                                                     # the red warning band between the bands, a bit proud of the casing
    g.ring(bm, R + 0.0012, R - 0.001, 0.05, Matrix.Translation((0, 0, L * 0.5)), 32)
    names.append(g.put("dynamite_marking", bm, red).name)

    bm = bmesh.new()                                                     # screw-in cap with a collar and a fuse hole
    g.lathe(bm, [(0.0, L - 0.004), (R * 0.72, L - 0.004), (R * 0.72, L + 0.004), (R * 0.55, L + 0.010), (R * 0.28, L + 0.014), (R * 0.28, L + 0.026), (0.0, L + 0.026)], segs=20)
    names.append(g.put("dynamite_cap", bm, cap).name)

    bm = bmesh.new()                                                     # twisted fuse leaning out of the cap: bent tube + a thicker charred tip
    pts = [Vector((0.0, 0.0, L + 0.020)), Vector((0.004, 0.0, L + 0.05)), Vector((0.016, 0.004, L + 0.082)), Vector((0.034, 0.012, L + 0.098)), Vector((0.050, 0.020, L + 0.100))]
    g.tube(bm, pts, 0.0042, 8)
    names.append(g.put("dynamite_fuse", bm, cord).name)
    tip = bmesh.new()
    g.cyl(tip, 0.0058, 0.012, Matrix.Translation((0.054, 0.021, L + 0.100)) @ Matrix.Rotation(math.radians(80), 4, "Y"), 8, r2=0.003)
    names.append(g.put("dynamite_fuse_tip", tip, g.mat("dyn_tip", (0.05, 0.03, 0.02), 0.9, 0.0)).name)
    return names


exports, photos = g.script_args()
sc, cam = g.stage((1.2, 1.0))
NAMES = build()
print("DYNAMITE TRIANGLES", g.tris(NAMES))
g.export(os.path.join(exports, "Dynamite.fbx"), NAMES)
g.views(photos, "dynamite", (0.02, 0.0, 0.16), 0.85, 0.0)
