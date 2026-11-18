"""Iron musket (D-045 item 3): iron bullets, gunpowder to refill, a load of 5 bullets, arrow-like damage. A Plains / Black Forest-era long gun.
Run: blender -b --factory-startup --python make_musket.py -- <exports dir> <photos dir>
Design (about 1.05 m, chunky like the other models): dark-wood stock with a pistol grip and a brass butt plate, a long round iron barrel held by three brass
bands, a muzzle swell with a front-sight bead, a matchlock-style lock (lock plate, S-shaped serpentine with a glowing-match clamp, pan), a trigger and
a guard, an iron ramrod under the barrel with brass pipes and a sling swivel. The musket points along -Y, the barrel axis is the origin height (z = 0),
origin y = 0 at the wrist. Empties: Muzzle (bore end), Pan (where the flash comes from), Grip (right hand).
Objects: musket_stock, musket_barrel, musket_bands, musket_lock, musket_trigger, musket_ramrod, musket_fittings.
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

MUZZLE_Y = -0.80
BREECH_Y = 0.04
ALONG_NEG_Y = Matrix.Rotation(math.radians(90), 4, "X")       # local +Z -> -Y


def build():
    wood = g.mat("musket_wood", (0.20, 0.11, 0.055), 0.8, 0.0)
    iron = g.mat("musket_iron", (0.06, 0.06, 0.065), 0.55, 0.85)
    brass = g.mat("musket_brass", (0.55, 0.38, 0.12), 0.4, 0.9)
    steel = g.mat("musket_steel", (0.30, 0.30, 0.32), 0.4, 0.9)
    names = []

    bm = bmesh.new()                                                     # stock: one side profile, extruded
    prof = [(0.42, 0.030), (0.30, 0.015), (0.14, -0.004), (0.04, -0.008), (-0.62, -0.010), (-0.62, -0.046), (-0.10, -0.050), (0.02, -0.056),
            (0.07, -0.100), (0.115, -0.125), (0.165, -0.105), (0.22, -0.075), (0.30, -0.085), (0.42, -0.150)]
    g.poly_extrude(bm, prof, 0.040)
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.004, segments=2, affect="EDGES")
    names.append(g.put("musket_stock", bm, wood).name)

    bm = bmesh.new()                                                     # barrel: breech swell, long taper, muzzle swell, bore
    g.lathe(bm, [(0.0, 0.0), (0.024, 0.0), (0.027, 0.02), (0.026, 0.09), (0.022, 0.20), (0.0205, 0.50), (0.0215, 0.70), (0.0238, 0.75), (0.0320, 0.80), (0.0392, 0.83), (0.0405, 0.83), (0.0405, 0.84), (0.0290, 0.84), (0.0200, 0.80), (0.0160, 0.76), (0.0160, 0.68), (0.0, 0.68)], Matrix.Translation((0, BREECH_Y, 0)) @ ALONG_NEG_Y, 28)
    g.cyl(bm, 0.0045, 0.014, Matrix.Translation((0, MUZZLE_Y + 0.05, 0.0295)), 8, r2=0.002)          # front-sight bead
    g.box(bm, (0.012, 0.030, 0.008), Vector((0, BREECH_Y + 0.035, 0.030)))                            # rear sight block
    names.append(g.put("musket_barrel", bm, iron).name)

    bm = bmesh.new()                                                     # three brass barrel bands with a lug each
    for y in (-0.12, -0.34, -0.56):
        g.ring(bm, 0.0285, 0.0245, 0.016, Matrix.Translation((0, y, 0)) @ ALONG_NEG_Y, 28)
        g.box(bm, (0.010, 0.012, 0.014), Vector((0, y, -0.0275)))
    names.append(g.put("musket_bands", bm, brass).name)

    bm = bmesh.new()                                                     # the matchlock on the right side (+X)
    xs = 0.0225
    g.box(bm, (0.006, 0.100, 0.034), Vector((xs, 0.075, -0.004)))                                      # lock plate
    g.box(bm, (0.010, 0.020, 0.012), Vector((xs + 0.002, 0.040, 0.018)))                               # pan with a cover
    g.cyl(bm, 0.0075, 0.012, Matrix.Translation((xs + 0.002, 0.040, 0.026)), 10)                       # priming cup
    pts = [Vector((xs + 0.004, 0.098, -0.010)), Vector((xs + 0.008, 0.092, 0.006)), Vector((xs + 0.008, 0.078, 0.026)), Vector((xs + 0.008, 0.058, 0.044)),
           Vector((xs + 0.008, 0.046, 0.044))]
    g.tube(bm, pts, 0.0048, 8)                                                                          # S-shaped serpentine
    g.box(bm, (0.010, 0.016, 0.010), Vector((xs + 0.008, 0.044, 0.040)))                               # match clamp
    g.cyl(bm, 0.0035, 0.020, Matrix.Translation((xs + 0.008, 0.038, 0.034)) @ Matrix.Rotation(math.radians(70), 4, "X"), 8)   # the slow match
    names.append(g.put("musket_lock", bm, steel).name)

    bm = bmesh.new()                                                     # trigger guard: a big oval bow (user reference photo, 2026-11-18), tangs, trigger blade
    F, R = (-0.080, -0.052), (0.098, -0.112)                              # front end (under the stock belly) and rear end (inside the grip slope), (y, z)
    bow = [Vector((0, y, z)) for y, z in (F, (-0.074, -0.082), (-0.050, -0.116), (-0.012, -0.141), (0.030, -0.148), (0.066, -0.136), (0.090, -0.122), R)]
    g.strap(bm, g.spline(bow, 6), 0.012, 0.0085, Vector((1, 0, 0)))                                        # the bow, a flat band 12 mm wide and 8.5 mm thick in profile
    g.box(bm, (0.013, 0.075, 0.0045), Vector((0, -0.043, -0.0535)))                                       # front tang / trigger plate along the stock belly
    g.cyl(bm, 0.0065, 0.0050, Matrix.Translation((0, -0.082, -0.0535)), 12)                               # front finial disc
    g.strap(bm, g.spline([Vector((0, y, z)) for y, z in ((0.085, -0.1085), (0.115, -0.1275), (0.150, -0.1160))], 4), 0.012, 0.0085, Vector((1, 0, 0)))   # rear tang on the grip
    blade = [Vector((0, y, z)) for y, z in ((0.004, -0.054), (0.007, -0.078), (0.014, -0.100), (0.027, -0.114), (0.040, -0.117))]
    g.strap(bm, g.spline(blade, 4), 0.008, 0.012, Vector((1, 0, 0)))                                      # curved trigger blade, tip to the rear
    names.append(g.put("musket_trigger", bm, iron).name)

    bm = bmesh.new()                                                     # ramrod: a long thin rod below the fore-end, two pipes and a brass tip
    g.cyl(bm, 0.0055, 0.58, Matrix.Translation((0, -0.32, -0.060)) @ Matrix.Rotation(math.radians(90), 4, "X"), 8)
    for y in (-0.20, -0.46):
        g.ring(bm, 0.011, 0.0055, 0.014, Matrix.Translation((0, y, -0.060)) @ ALONG_NEG_Y, 14)
    names.append(g.put("musket_ramrod", bm, iron).name)

    bm = bmesh.new()                                                     # fittings: butt plate, tip cap, ramrod tip, sling swivels
    g.box(bm, (0.044, 0.008, 0.190), Vector((0, 0.423, -0.060)), Matrix.Identity(4))                  # butt plate
    g.cyl(bm, 0.0075, 0.014, Matrix.Translation((0, -0.625, -0.060)) @ Matrix.Rotation(math.radians(90), 4, "X"), 10)    # ramrod head
    g.box(bm, (0.040, 0.020, 0.040), Vector((0, -0.612, -0.028)))                                      # fore-end cap
    for y, z in ((-0.36, -0.052), (0.27, -0.082)):
        g.ring(bm, 0.011, 0.007, 0.006, Matrix.Translation((0, y, z)) @ Matrix.Rotation(math.radians(90), 4, "Y"), 12)
    names.append(g.put("musket_fittings", bm, brass).name)
    return names


exports, photos = g.script_args()
sc, cam = g.stage((3.0, 2.4))
NAMES = build()
EM = [g.empty("Muzzle", (0, MUZZLE_Y, 0)).name, g.empty("Pan", (0.0225, 0.040, 0.034)).name, g.empty("Grip", (0, 0.115, -0.090)).name]
print("MUSKET TRIANGLES", g.tris(NAMES))
g.export(os.path.join(exports, "Musket.fbx"), NAMES + EM)
g.shift(NAMES + EM, 0.45)
g.views(photos, "musket", (0.0, -0.10, 0.45), 1.7, 0.1)
g.views(photos, "musket-lock", (0.0, 0.10, 0.45), 0.55, 0.05)
g.views(photos, "musket-trigger", (0.0, 0.02, 0.36), 0.42, 0.02)
