"""The three Powder Mill extensions (D-137), one per biome: Mountains, Mistlands, Ashlands. Each is built next to a Powder Mill.
Run: blender -b --factory-startup --python make_mill_extensions.py -- <exports dir> <photos dir>
Units: 1 Blender unit = 1 m; up = +Z; object origin = ground centre of each model. Every model is its own FBX (MillGrindstone.fbx, MillSieve.fbx,
MillOven.fbx). Material names reuse the ext_* materials of the Leaching Station extensions (GunPowderedArtBuilder already has them).
- MillGrindstone (Mountains): a stone trough with an upright obsidian edge-runner wheel on a silver axle in two stone cheeks.
- MillSieve (Mistlands): a yggdrasil wood frame on a black marble slab holding a tilted copper sieve tray and a copper chute.
- MillOven (Ashlands): a flametal drying oven in an ashwood frame, glowing door slits, a flue and a drying shelf on top.
"""
import math
import os
import sys
import bmesh
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

T = Matrix.Translation
R = Matrix.Rotation


def grindstone():
    stone = g.mat("ext_stone", (0.30, 0.30, 0.29), 0.95)
    obsidian = g.mat("ext_obsidian", (0.02, 0.015, 0.03), 0.15)
    silver = g.mat("ext_silver", (0.62, 0.64, 0.66), 0.3, 1.0)
    names = []
    bm = bmesh.new()                                                     # trough: floor + four walls as separate boxes that do not overlap
    g.box(bm, (1.2, 0.7, 0.12), Vector((0, 0, 0.06)))
    names.append(g.put("mill_gs_floor", bm, stone, smooth=False).name)
    bm = bmesh.new()
    g.box(bm, (1.2, 0.10, 0.22), Vector((0, 0.30, 0.23)))
    g.box(bm, (1.2, 0.10, 0.22), Vector((0, -0.30, 0.23)))
    names.append(g.put("mill_gs_sides", bm, stone, smooth=False).name)
    bm = bmesh.new()
    g.box(bm, (0.10, 0.50, 0.22), Vector((0.55, 0, 0.23)))
    g.box(bm, (0.10, 0.50, 0.22), Vector((-0.55, 0, 0.23)))
    names.append(g.put("mill_gs_ends", bm, stone, smooth=False).name)
    bm = bmesh.new()                                                     # two cheeks that carry the axle
    for y in (0.42, -0.42):
        g.box(bm, (0.22, 0.12, 0.75), Vector((0, y, 0.50)))
    names.append(g.put("mill_gs_cheeks", bm, stone, smooth=False).name)
    bm = bmesh.new()                                                     # upright edge-runner wheel, axis along Y
    g.cyl(bm, 0.36, 0.20, T((0, 0, 0.62)) @ R(math.pi / 2, 4, "X"), 28)
    names.append(g.put("mill_gs_wheel", bm, obsidian).name)
    bm = bmesh.new()                                                     # silver axle, rim bands and hub caps
    g.cyl(bm, 0.035, 1.05, T((0, 0, 0.62)) @ R(math.pi / 2, 4, "X"), 12)
    for y in (0.105, -0.105):
        g.ring(bm, 0.375, 0.33, 0.025, T((0, y, 0.62)) @ R(math.pi / 2, 4, "X"), 28)
        g.cyl(bm, 0.07, 0.03, T((0, y * 1.2, 0.62)) @ R(math.pi / 2, 4, "X"), 12)
    names.append(g.put("mill_gs_silver", bm, silver).name)
    return "MillGrindstone", names, (0, 0, 0.5), 3.0


def sieve():
    ygg = g.mat("ext_yggwood", (0.10, 0.12, 0.20), 0.6)
    copper = g.mat("ext_copper", (0.60, 0.28, 0.12), 0.35, 1.0)
    marble = g.mat("ext_marble", (0.05, 0.05, 0.055), 0.3)
    names = []
    bm = bmesh.new()
    g.box(bm, (1.1, 0.8, 0.12), Vector((0, 0, 0.06)))
    names.append(g.put("mill_sv_base", bm, marble, smooth=False).name)
    bm = bmesh.new()                                                     # four legs, the back pair taller so the tray tilts toward the front (-Y)
    for x in (-0.45, 0.45):
        g.box(bm, (0.08, 0.08, 0.80), Vector((x, -0.30, 0.52)))
        g.box(bm, (0.08, 0.08, 1.05), Vector((x, 0.30, 0.645)))
    g.box(bm, (0.98, 0.08, 0.08), Vector((0, 0.30, 1.12)))              # back cross bar
    names.append(g.put("mill_sv_frame", bm, ygg, smooth=False).name)
    tilt = T((0, 0, 1.05)) @ R(math.radians(22), 4, "X")                 # the tray, tilted ~22 degrees
    bm = bmesh.new()
    g.box(bm, (0.86, 0.04, 0.10), Vector((0, 0.30, 0)), tilt)          # sieve frame (four sides), yggdrasil wood like the legs (D-138)
    g.box(bm, (0.86, 0.04, 0.10), Vector((0, -0.30, 0)), tilt)
    g.box(bm, (0.04, 0.64, 0.10), Vector((0.43, 0, 0)), tilt)
    g.box(bm, (0.04, 0.64, 0.10), Vector((-0.43, 0, 0)), tilt)
    names.append(g.put("mill_sv_rim", bm, ygg, smooth=False).name)
    bm = bmesh.new()
    for k in range(7):                                                   # the mesh: thin copper bars both ways
        g.box(bm, (0.012, 0.60, 0.012), Vector((-0.36 + 0.12 * k, 0, -0.03)), tilt)
    for k in range(5):
        g.box(bm, (0.82, 0.012, 0.012), Vector((0, -0.24 + 0.12 * k, -0.03)), tilt)
    names.append(g.put("mill_sv_tray", bm, copper, smooth=False).name)
    bm = bmesh.new()                                                     # chute from the tray's low edge down and out toward the mill, with two side walls
    slope = T((0, -0.47, 0.76)) @ R(math.radians(45), 4, "X")
    g.box(bm, (0.30, 0.42, 0.02), Vector((0, 0, 0)), slope)
    g.box(bm, (0.02, 0.42, 0.08), Vector((0.15, 0, 0.04)), slope)
    g.box(bm, (0.02, 0.42, 0.08), Vector((-0.15, 0, 0.04)), slope)
    names.append(g.put("mill_sv_chute", bm, copper, smooth=False).name)
    return "MillSieve", names, (0, 0, 0.6), 3.2


def oven():
    grausten = g.mat("ext_grausten", (0.16, 0.16, 0.17), 0.9)
    flametal = g.mat("ext_flametal", (0.09, 0.085, 0.09), 0.45, 0.8)
    glass = g.mat("ext_glass", (0.95, 0.40, 0.08), 0.1)
    flame = g.mat("ext_flame", (1.0, 0.55, 0.12), 0.5)
    names = g.fire_box("mill_ov", (0.9, 0.7, 0.7), Vector((0, 0, 0.47)), 4, flametal, glass, flame)
    bm = bmesh.new()                                                     # short flametal feet and the flue at the back corner
    for sx in (-1, 1):
        for sy in (-1, 1):
            g.box(bm, (0.10, 0.10, 0.12), Vector((sx * 0.38, sy * 0.28, 0.06)))
    g.cyl(bm, 0.07, 0.55, T((-0.30, 0.27, 1.10)), 16)
    g.cyl(bm, 0.10, 0.04, T((-0.30, 0.27, 1.385)), 16)
    names.append(g.put("mill_ov_flue", bm, flametal, smooth=False).name)
    bm = bmesh.new()                                                     # the table and its legs: Grausten (D-138)
    for sx in (-1, 1):
        for sy in (-1, 1):
            g.box(bm, (0.08, 0.08, 1.05), Vector((sx * 0.50, sy * 0.40, 0.525)))
    g.box(bm, (1.08, 0.88, 0.05), Vector((0, 0, 1.06)))
    names.append(g.put("mill_ov_table", bm, grausten, smooth=False).name)
    bm = bmesh.new()                                                     # the two drying squares on the table: flametal (D-138)
    for x in (-0.22, 0.22):
        g.box(bm, (0.36, 0.40, 0.04), Vector((x, -0.12, 1.105)))
    names.append(g.put("mill_ov_trays", bm, flametal, smooth=False).name)
    return "MillOven", names, (0, 0, 0.6), 3.0


exports, photos = g.script_args()
for build in (grindstone, sieve, oven):
    sc, cam = g.stage((3.0, 3.0))
    model, names, centre, dist = build()
    print("EXTENSION", model, "TRIANGLES", g.tris(names))
    g.export(os.path.join(exports, model + ".fbx"), names)
    g.views(photos, "millext-" + model.lower(), centre, dist, 0.6)
print("EXTENSIONS DONE")
