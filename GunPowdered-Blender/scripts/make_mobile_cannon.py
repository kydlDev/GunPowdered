"""Ashlands mobile cannon (D-045 item 4): a flametal field gun on an ashwood carriage with two spoked wheels that turn, pulled by an asksvin with a harness.
Run: blender -b --factory-startup --python make_mobile_cannon.py -- <exports dir> <photos dir>
Design (chunky, 1.25 m barrel; about 2.4 m long with the trail, wheels 1.03 m high): a dark flametal barrel with a cascabel knob, reinforcing bands with a
glowing flame-orange seam, trunnions resting in two ashwood cheeks that step down into a long trail; an elevating screw under the breech turned by a side crank on the left cheek (D-064); a twelve-spoke wheel
each side with a flametal tyre; an axle beam with linchpins; a tow ring at the end of the trail for the asksvin harness; a rammer / sponge staff on the left cheek.
The cannon points along -Y, up = +Z, the ground is z = 0, origin = on the ground below the axle. Parts that move have their origin at their pivot:
cannon_barrel and cannon_bands (the trunnions, for elevation and recoil: rotate round X, a NEGATIVE angle raises the muzzle), wheel_left / wheel_right (the axle centre,
they spin round X; their tyres are children), elevation_screw (moves along Z: goes DOWN by about 0.43 m x sin(elevation) when the muzzle goes up), elevation_crank (rotates
round X on the left cheek, its wooden grip elevation_crank_grip is a child).
Empties: Muzzle, TouchHole, HarnessPoint (where the harness chain hooks on), AxleCentre, ElevationPivot (the trunnion axis), CrankAxis,
HandleGripA / HandleGripB (hands on the push handle, D-107), PushPoint (where the player stands while moving it by hand).
Objects: cannon_barrel, cannon_bands (glow), carriage (cheeks, trail, axle beam, screw housing, staff), carriage_iron (tow ring, linchpins, bolts), wheel_left, wheel_right,
wheel_left_tyre, wheel_right_tyre, elevation_screw, elevation_crank, elevation_crank_grip.
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

AXLE_Z = 0.515            # wheel centre height (wheel radius 0.515 touches the ground)
BARREL_Z = 0.74           # barrel axis height
BREECH_Y = 0.62           # the breech end of the barrel body, the muzzle is 1.24 m further along -Y
TRUNNION_Y = 0.07
WHEEL_X = 0.66
TO_NEG_Y = Matrix.Rotation(math.radians(90), 4, "X")           # local +Z -> -Y
TO_X = Matrix.Rotation(math.radians(90), 4, "Y")               # local +Z -> +X


def put_at(name, bm, material, pivot):
    """Object whose origin is `pivot` (the mesh is built in world coordinates and moved so the origin sits on the pivot)."""
    bmesh.ops.translate(bm, vec=-Vector(pivot), verts=bm.verts)
    return g.put(name, bm, material, Matrix.Translation(pivot))


def build_barrel(flametal):
    bm = bmesh.new()
    prof = [(0.0, -0.17), (0.04, -0.16), (0.055, -0.135), (0.04, -0.11), (0.034, -0.08), (0.075, -0.05), (0.112, 0.0), (0.114, 0.10), (0.108, 0.12), (0.104, 0.30),
            (0.098, 0.80), (0.092, 1.0), (0.088, 1.15), (0.098, 1.19), (0.108, 1.215), (0.108, 1.24), (0.082, 1.24), (0.074, 1.23), (0.074, 1.10), (0.0, 1.10)]
    g.lathe(bm, prof, Matrix.Translation((0, BREECH_Y, BARREL_Z)) @ TO_NEG_Y, 40)
    g.cyl(bm, 0.046, 0.46, Matrix.Translation((0, BREECH_Y - 0.55, BARREL_Z)) @ TO_X, 20)                 # trunnions
    g.cyl(bm, 0.014, 0.050, Matrix.Translation((0, BREECH_Y - 0.06, BARREL_Z + 0.118)), 10, r2=0.010)    # touch hole spout
    return put_at("cannon_barrel", bm, flametal, (0.0, TRUNNION_Y, BARREL_Z))


def build_bands(glow):
    bm = bmesh.new()
    for z, r in ((0.13, 0.116), (0.45, 0.108), (0.85, 0.100), (1.12, 0.094)):
        g.ring(bm, r + 0.004, r - 0.002, 0.030, Matrix.Translation((0, BREECH_Y, BARREL_Z)) @ TO_NEG_Y @ Matrix.Translation((0, 0, z)), 36)
    return put_at("cannon_bands", bm, glow, (0.0, TRUNNION_Y, BARREL_Z))


def build_carriage(ash):
    bm = bmesh.new()
    cheek = [(-0.42, 0.86), (0.22, 0.86), (0.62, 0.60), (1.66, 0.47), (1.66, 0.32), (0.62, 0.34), (0.22, 0.50), (-0.42, 0.50)]
    for x in (-0.23, 0.23):                                                                                    # two cheeks
        g.poly_extrude(bm, cheek, 0.075, Matrix.Translation((x, 0, 0)))
    g.box(bm, (0.56, 0.34, 0.075), Vector((0.0, 1.50, 0.39)))                                                  # trail plate joining the cheeks at the rear
    g.box(bm, (1.02, 0.20, 0.17), Vector((0.0, 0.0, AXLE_Z)))                                                  # axle beam
    g.box(bm, (0.15, 0.15, 0.12), Vector((0.0, 0.50, 0.44)))                                                  # nut housing of the elevating screw, between the cheeks
    for x in (-0.135, 0.135):
        g.box(bm, (0.12, 0.10, 0.10), Vector((x, 0.50, 0.42)))                                                 # arms joining the housing to the cheeks
    g.box(bm, (0.05, 0.11, 0.11), Vector((-0.292, 0.50, 0.40)))                                                # outer bearing block of the crank shaft (left cheek)
    g.box(bm, (0.44, 0.07, 0.07), Vector((0.0, 0.95, 0.58)))                                                   # cross bar (transom)
    g.box(bm, (0.44, 0.07, 0.07), Vector((0.0, -0.30, 0.70)))                                                  # front transom
    g.cyl(bm, 0.017, 1.35, Matrix.Translation((-0.31, 0.32, 0.64)) @ Matrix.Rotation(math.radians(90), 4, "X"), 8)   # rammer staff on the left cheek
    g.cyl(bm, 0.036, 0.16, Matrix.Translation((-0.31, -0.37, 0.64)) @ Matrix.Rotation(math.radians(90), 4, "X"), 12)  # sponge head
    # D-107 push handle: an inverted U of ashwood rising back from the cheek ends; the player grabs the cross bar to push or pull by hand
    handle = [Vector((-0.23, 1.60, 0.47)), Vector((-0.23, 1.80, 0.84)), Vector((-0.22, 1.93, 1.12)), Vector((-0.17, 1.97, 1.16)),
              Vector((0.17, 1.97, 1.16)), Vector((0.22, 1.93, 1.12)), Vector((0.23, 1.80, 0.84)), Vector((0.23, 1.60, 0.47))]
    g.tube(bm, handle, 0.028, 10)
    g.box(bm, (0.42, 0.05, 0.05), Vector((0.0, 1.80, 0.84)))                                                  # brace between the handle legs
    return g.put("carriage", bm, ash)


def build_iron(iron):
    bm = bmesh.new()
    g.cyl(bm, 0.032, 1.44, Matrix.Translation((0.0, 0.0, AXLE_Z)) @ TO_X, 12)                                  # axle
    for x in (-0.60, 0.60):
        g.cyl(bm, 0.012, 0.10, Matrix.Translation((x, 0.0, AXLE_Z + 0.04)), 8)                                 # linchpin
    # tow clevis (user 2026-11-19 16:35): TWO eye plates stand apart on the trail end and the harness's last ring runs BETWEEN them; one iron pin goes through
    # all three eyes (the harness ring is 0.030 wide, centred on x 0: the plates touch it, no gap). Everything overlaps its neighbour so it reads as one welded piece.
    CLEVIS_Y, CLEVIS_Z = 1.78, 0.395
    for s in (-1, 1):
        g.ring(bm, 0.058, 0.0175, 0.026, Matrix.Translation((s * 0.0285, CLEVIS_Y, CLEVIS_Z)) @ TO_X, 20)           # eye plate (inner face at x +-0.0155, touching the harness ring)
        g.box(bm, (0.026, 0.150, 0.056), Vector((s * 0.0285, 1.72, CLEVIS_Z)))                                    # its web down to the trail
    g.box(bm, (0.110, 0.060, 0.070), Vector((0.0, 1.665, CLEVIS_Z)))                                              # cross block joining both plates and the trail end
    g.cyl(bm, 0.0125, 0.130, Matrix.Translation((0.0, CLEVIS_Y, CLEVIS_Z)) @ TO_X, 12)                           # the pin through all three eyes
    for s in (-1, 1):
        g.cyl(bm, 0.022, 0.014, Matrix.Translation((s * 0.067, CLEVIS_Y, CLEVIS_Z)) @ TO_X, 12)                   # pin head / retaining cap, flush on the plate side
    for y in (-0.20, 0.45, 1.20, 1.55):                                                                        # bolt heads along the cheeks
        for x in (-0.275, 0.275):
            g.cyl(bm, 0.016, 0.012, Matrix.Translation((x, y, 0.62 if y < 1.0 else 0.43)) @ TO_X, 8)
    g.ring(bm, 0.060, 0.047, 0.035, Matrix.Translation((0.0, TRUNNION_Y, BARREL_Z)) @ TO_X @ Matrix.Translation((0, 0, 0.27)), 20)    # trunnion caps
    g.ring(bm, 0.060, 0.047, 0.035, Matrix.Translation((0.0, TRUNNION_Y, BARREL_Z)) @ TO_X @ Matrix.Translation((0, 0, -0.27)), 20)
    return g.put("carriage_iron", bm, iron)


def build_wheel(side, ash, flametal):
    x = side * WHEEL_X
    axis = Matrix.Translation((x, 0.0, AXLE_Z)) @ TO_X
    wood = bmesh.new()
    g.lathe(wood, [(0.0, -0.11), (0.060, -0.11), (0.090, -0.07), (0.095, -0.02), (0.095, 0.02), (0.090, 0.07), (0.060, 0.11), (0.0, 0.11)], axis, 20)   # hub
    g.ring(wood, 0.498, 0.425, 0.085, axis, 40)                                                                                              # felloes
    for k in range(12):                                                                                                                     # twelve spokes
        a = 2 * math.pi * k / 12
        mid = 0.26
        g.box(wood, (0.040 if k % 2 == 0 else 0.034, 0.075, 0.37), Vector((0.0, 0.0, 0.0)),
              Matrix.Translation((x, 0.0, AXLE_Z)) @ Matrix.Rotation(a, 4, "X") @ Matrix.Translation((0.0, 0.0, mid)))
    wheel = put_at("wheel_left" if side < 0 else "wheel_right", wood, ash, (x, 0.0, AXLE_Z))
    tyre = bmesh.new()
    g.ring(tyre, 0.515, 0.494, 0.100, axis, 40)
    for k in range(8):                                                                                                                      # tyre nails
        a = 2 * math.pi * k / 8
        g.cyl(tyre, 0.011, 0.010, Matrix.Translation((x, 0.0, AXLE_Z)) @ Matrix.Rotation(a, 4, "X") @ Matrix.Translation((0.0, 0.0, 0.518)), 8)
    t = put_at("wheel_left_tyre" if side < 0 else "wheel_right_tyre", tyre, flametal, (x, 0.0, AXLE_Z))
    t.parent = wheel
    t.matrix_parent_inverse = wheel.matrix_world.inverted()
    return wheel, t


def build_elevation(iron, ash):
    """Elevating screw under the breech (moves up / down along Z) and the side crank that turns it (rotates round X, the grip is its child).
    Raising the muzzle = the screw goes DOWN (the breech sinks), the barrel turns on the trunnions."""
    pivot = (0.0, 0.50, 0.44)
    bm = bmesh.new()
    g.cyl(bm, 0.020, 0.22, Matrix.Translation((0.0, 0.50, 0.53)), 12)                                          # screw rod
    for k in range(10):
        g.cyl(bm, 0.027, 0.008, Matrix.Translation((0.0, 0.50, 0.435 + k * 0.021)), 12)                        # thread rings
    g.box(bm, (0.13, 0.11, 0.030), Vector((0.0, 0.50, 0.628)))                                                 # cradle under the barrel
    for y in (0.445, 0.555):
        g.box(bm, (0.13, 0.014, 0.05), Vector((0.0, y, 0.605)))                                                # cradle lips
    screw = put_at("elevation_screw", bm, iron, pivot)
    axis = (-0.41, 0.50, 0.40)
    base = Matrix.Translation(axis) @ Matrix.Rotation(math.radians(35), 4, "X")                                # crank arm hangs down and a little to the rear
    cb = bmesh.new()
    g.cyl(cb, 0.014, 0.42, Matrix.Translation((-0.20, 0.50, 0.40)) @ TO_X, 10)                                 # shaft through the cheek to the housing
    g.cyl(cb, 0.030, 0.035, Matrix.Translation(axis) @ TO_X, 14)                                               # hub
    g.box(cb, (0.024, 0.032, 0.17), Vector((0.0, 0.0, -0.085)), base)                                          # arm
    crank = put_at("elevation_crank", cb, iron, axis)
    gb = bmesh.new()
    g.cyl(gb, 0.017, 0.12, base @ Matrix.Translation((-0.05, 0.0, -0.17)) @ TO_X, 12)                         # wooden grip, points outwards
    grip = put_at("elevation_crank_grip", gb, ash, axis)
    grip.parent = crank
    grip.matrix_parent_inverse = crank.matrix_world.inverted()
    return screw, crank, grip


def build():
    flametal = g.mat("flametal", (0.09, 0.085, 0.09), 0.45, 0.9)
    glow = g.mat("flametal_glow", (0.95, 0.30, 0.05), 0.5, 0.0)
    glow.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1.0, 0.28, 0.04, 1.0)
    glow.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 2.0
    ash = g.mat("ashwood", (0.13, 0.105, 0.09), 0.85, 0.0)
    iron = g.mat("carriage_blackiron", (0.05, 0.05, 0.055), 0.6, 0.85)
    names = []
    names.append(build_barrel(flametal).name)
    names.append(build_bands(glow).name)
    names.append(build_carriage(ash).name)
    names.append(build_iron(iron).name)
    for side in (-1, 1):
        w, t = build_wheel(side, ash, flametal)
        names += [w.name, t.name]
    names += [o.name for o in build_elevation(iron, ash)]
    return names


exports, photos = g.script_args()
sc, cam = g.stage((5.0, 4.0))
NAMES = build()
EM = [g.empty("Muzzle", (0.0, BREECH_Y - 1.24, BARREL_Z)).name, g.empty("TouchHole", (0.0, BREECH_Y - 0.06, BARREL_Z + 0.14)).name,
      g.empty("HarnessPoint", (0.0, 1.78, 0.395)).name, g.empty("AxleCentre", (0.0, 0.0, AXLE_Z)).name,
      g.empty("ElevationPivot", (0.0, TRUNNION_Y, BARREL_Z)).name, g.empty("CrankAxis", (-0.41, 0.50, 0.40)).name,
      g.empty("HandleGripA", (0.16, 1.97, 1.16)).name, g.empty("HandleGripB", (-0.16, 1.97, 1.16)).name,   # D-107: where the hands hold the push handle
      g.empty("PushPoint", (0.0, 2.33, 0.80)).name]                                                       # where the player stands (hips) while pushing / pulling
print("MOBILE CANNON TRIANGLES", g.tris(NAMES))
g.export(os.path.join(exports, "MobileCannon.fbx"), NAMES + EM)
g.views(photos, "mobile-cannon", (0.0, 0.55, 0.55), 3.4, 0.4)
g.views(photos, "mobile-cannon-barrel", (0.0, 0.0, 0.72), 1.7, 0.2)
g.shoot(sc, cam, os.path.join(photos, "%s_mobile-cannon-crank.png" % g.stamp()), (-1.15, 1.35, 0.95), (-0.15, 0.5, 0.45), 50)
g.shoot(sc, cam, os.path.join(photos, "%s_mobile-cannon-handle.png" % g.stamp()), (1.6, 3.4, 1.5), (0.0, 1.6, 0.7), 50)
# demo pose, AFTER the export: muzzle raised 20 degrees (barrel turns on the trunnions, the screw sinks by the breech drop, the crank turns)
rise = math.radians(20)
bpy.data.objects["cannon_barrel"].rotation_euler.x = -rise
bpy.data.objects["cannon_bands"].rotation_euler.x = -rise
bpy.data.objects["elevation_screw"].location.z -= 0.43 * math.sin(rise)
bpy.data.objects["elevation_crank"].rotation_euler.x = math.radians(120)
g.shoot(sc, cam, os.path.join(photos, "%s_mobile-cannon-elev20.png" % g.stamp()), (2.6, -1.2, 1.1), (0.0, 0.3, 0.65), 50)
