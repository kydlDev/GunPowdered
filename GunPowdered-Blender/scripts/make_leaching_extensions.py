"""The five Leaching Station extensions (D-129), one per biome after the Black Forest. Each is built next to a Leaching Station.
Run: blender -b --factory-startup --python make_leaching_extensions.py -- <exports dir> <photos dir>
Units: 1 Blender unit = 1 m; up = +Z; object origin = ground centre of each model. Every model is its own FBX (LeachBarrel.fbx, SettlingTank.fbx,
BlackMetalBoiler.fbx, DvergrDripPipes.fbx, FlametalHeater.fbx). Material names start with "ext_" and match GunPowderedArtBuilder's material table.
- LeachBarrel (Swamp): a stone-filled barrel like the station's own, iron hoops, a wooden spout.
- SettlingTank (Mountains): a stone trough with brine, silver corner bands, an obsidian lip.
- BlackMetalBoiler (Plains): an upright fine wood boiler with black metal bands, lid and chimney.
- DvergrDripPipes (Mistlands): a yggdrasil wood frame on a black marble base carrying copper pipes with drip nozzles.
- FlametalHeater (Mistlands -> Ashlands): an ashwood-framed iron fire box with a glowing flametal core.
"""
import math
import os
import random
import sys
import bmesh
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g

T = Matrix.Translation
R = Matrix.Rotation


def barrel_profile(r_bot, r_mid, h):
    return [(r_bot, 0.0), (r_mid * 0.97, h * 0.25), (r_mid, h * 0.5), (r_mid * 0.97, h * 0.75), (r_bot, h)]


def leach_barrel():
    wood = g.mat("ext_wood", (0.20, 0.11, 0.05), 0.85)
    iron = g.mat("ext_iron", (0.035, 0.032, 0.03), 0.45, 0.7)
    stone = g.mat("ext_stone", (0.30, 0.30, 0.29), 0.95)
    names = []
    h = 1.0
    prof = barrel_profile(0.36, 0.42, h)
    bm = bmesh.new()
    inner = [(r - 0.035, z) for r, z in reversed(prof[1:])]
    g.lathe(bm, [(0.0, 0.0)] + prof + inner + [(0.0, 0.10)], segs=28)
    names.append(g.put("ext_barrel_body", bm, wood).name)
    bm = bmesh.new()
    for z in (0.12, 0.5, 0.9):
        r = 0.42 if z == 0.5 else 0.38
        g.ring(bm, r + 0.015, r - 0.01, 0.045, T((0, 0, z)), 32)
    names.append(g.put("ext_barrel_hoops", bm, iron).name)
    bm = bmesh.new()                                                     # the stones that fill it, peeking over the rim
    rnd = random.Random(7)
    for _ in range(14):
        a, d = rnd.uniform(0, 2 * math.pi), rnd.uniform(0, 0.27)
        s = rnd.uniform(0.07, 0.12)
        bmesh.ops.create_icosphere(bm, subdivisions=1, radius=s, matrix=T((math.cos(a) * d, math.sin(a) * d, h - 0.06 + rnd.uniform(-0.03, 0.04))))
    names.append(g.put("ext_barrel_stones", bm, stone, smooth=False).name)
    bm = bmesh.new()                                                     # spout at the bottom, pointing to the station
    g.box(bm, (0.08, 0.30, 0.06), Vector((0.0, -0.50, 0.16)))
    names.append(g.put("ext_barrel_spout", bm, wood).name)
    return "LeachBarrel", names, (0, 0, 0.5), 3.0


def settling_tank():
    stone = g.mat("ext_stone", (0.30, 0.30, 0.29), 0.95)
    silver = g.mat("ext_silver", (0.62, 0.64, 0.66), 0.3, 1.0)
    obsidian = g.mat("ext_obsidian", (0.02, 0.015, 0.03), 0.15)
    brine = g.mat("ext_brine", (0.42, 0.40, 0.25), 0.1)
    names = []
    w, d, h, wall = 1.3, 0.85, 0.6, 0.12
    bm = bmesh.new()
    g.box(bm, (w, d, 0.10), Vector((0, 0, 0.05)))
    names.append(g.put("ext_tank_floor", bm, stone, smooth=False).name)
    bm = bmesh.new()
    g.box(bm, (w, wall, h - 0.10), Vector((0, d / 2 - wall / 2, 0.10 + (h - 0.10) / 2)))
    g.box(bm, (w, wall, h - 0.10), Vector((0, -d / 2 + wall / 2, 0.10 + (h - 0.10) / 2)))
    names.append(g.put("ext_tank_walls", bm, stone, smooth=False).name)
    bm = bmesh.new()                                                     # end walls as their own part: overlapping boxes in one mesh flip the shading
    g.box(bm, (wall, d - 2 * wall, h - 0.10), Vector((w / 2 - wall / 2, 0, 0.10 + (h - 0.10) / 2)))
    g.box(bm, (wall, d - 2 * wall, h - 0.10), Vector((-w / 2 + wall / 2, 0, 0.10 + (h - 0.10) / 2)))
    names.append(g.put("ext_tank_ends", bm, stone, smooth=False).name)
    bm = bmesh.new()
    g.box(bm, (w + 0.04, 0.06, 0.05), Vector((0, d / 2 - 0.02, h + 0.025)))
    g.box(bm, (w + 0.04, 0.06, 0.05), Vector((0, -d / 2 + 0.02, h + 0.025)))
    names.append(g.put("ext_tank_lip", bm, obsidian, smooth=False).name)
    bm = bmesh.new()
    for sx in (-1, 1):
        for sy in (-1, 1):
            for z in (0.15, 0.45):
                g.box(bm, (0.05, 0.05, 0.06), Vector((sx * (w / 2 + 0.005), sy * (d / 2 + 0.005), z)))
    names.append(g.put("ext_tank_bands", bm, silver, smooth=False).name)
    bm = bmesh.new()
    g.box(bm, (w - 2 * wall, d - 2 * wall, 0.01), Vector((0, 0, h - 0.10)))
    names.append(g.put("ext_tank_brine", bm, brine, smooth=False).name)
    return "SettlingTank", names, (0, 0, 0.3), 3.2


def black_metal_boiler():
    fine = g.mat("ext_finewood", (0.42, 0.25, 0.12), 0.7)
    black = g.mat("ext_blackmetal", (0.03, 0.035, 0.03), 0.4, 0.9)
    names = []
    h = 1.3
    prof = barrel_profile(0.40, 0.44, h)
    bm = bmesh.new()
    g.lathe(bm, prof, segs=24)
    names.append(g.put("ext_boiler_body", bm, fine).name)
    bm = bmesh.new()
    for z in (0.08, 0.45, 0.85, 1.22):
        r = 0.43 if 0.3 < z < 1.0 else 0.41
        g.ring(bm, r + 0.02, r - 0.01, 0.07, T((0, 0, z)), 32)
    g.lathe(bm, [(0.0, h), (0.44, h), (0.40, h + 0.06), (0.10, h + 0.10), (0.0, h + 0.10)], segs=24)   # domed lid
    g.cyl(bm, 0.07, 0.55, T((0.18, 0.0, h + 0.33)), 16)                                                   # chimney
    g.cyl(bm, 0.10, 0.04, T((0.18, 0.0, h + 0.62)), 16)
    g.cyl(bm, 0.035, 0.45, T((0.0, -0.55, 0.30)) @ R(math.pi / 2, 4, "X"), 12)                            # pipe to the station
    names.append(g.put("ext_boiler_metal", bm, black).name)
    return "BlackMetalBoiler", names, (0, 0, 0.75), 3.6


def dvergr_drip_pipes():
    ygg = g.mat("ext_yggwood", (0.10, 0.12, 0.20), 0.6)
    copper = g.mat("ext_copper", (0.60, 0.28, 0.12), 0.35, 1.0)
    marble = g.mat("ext_marble", (0.05, 0.05, 0.055), 0.3)
    names = []
    bm = bmesh.new()
    g.box(bm, (1.4, 0.6, 0.15), Vector((0, 0, 0.075)))
    names.append(g.put("ext_pipes_base", bm, marble, smooth=False).name)
    bm = bmesh.new()
    for x in (-0.6, 0.6):
        g.box(bm, (0.10, 0.10, 1.4), Vector((x, 0, 0.85)))
    g.box(bm, (1.40, 0.12, 0.10), Vector((0, 0, 1.55)))
    names.append(g.put("ext_pipes_frame", bm, ygg, smooth=False).name)
    bm = bmesh.new()
    x0, x1, rx = -0.55, 0.74, 0.74                                       # main pipes run from the left post through the right post into the riser
    for z in (1.40, 1.05):
        g.cyl(bm, 0.045, x1 - x0, T(((x0 + x1) / 2, 0, z)) @ R(math.pi / 2, 4, "Y"), 16)
        bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=0.055, matrix=T((rx, 0, z)))     # elbow joint into the riser
        g.cyl(bm, 0.052, 0.03, T((x0 + 0.015, 0, z)) @ R(math.pi / 2, 4, "Y"), 16)                         # end cap at the left post
        for k in range(5):                                               # drip nozzles under each pipe
            x = -0.45 + 0.24 * k
            g.cyl(bm, 0.018, 0.10, T((x, 0, z - 0.08)), 8, r2=0.008)
    g.cyl(bm, 0.045, 0.35, T((rx, 0, 1.225)), 16)                        # riser joining the two pipes, outside the post
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=0.055, matrix=T((0.0, 0.0, 1.05)))   # tee where the feed pipe leaves the lower pipe
    g.cyl(bm, 0.035, 0.70, T((0.0, -0.35, 1.05)) @ R(math.pi / 2, 4, "X"), 12)    # feed pipe to the station, starting at the lower pipe
    g.cyl(bm, 0.045, 0.04, T((0.0, -0.70, 1.05)) @ R(math.pi / 2, 4, "X"), 12)    # flange at its end
    names.append(g.put("ext_pipes_copper", bm, copper).name)
    return "DvergrDripPipes", names, (0, 0, 0.8), 3.8


def flametal_heater():
    ash = g.mat("ext_ashwood", (0.13, 0.11, 0.10), 0.85)
    iron = g.mat("ext_flametal", (0.09, 0.085, 0.09), 0.45, 0.8)
    glow = g.mat("ext_glow", (0.95, 0.30, 0.05), 0.5)
    names = []
    bm = bmesh.new()
    g.box(bm, (0.9, 0.7, 0.55), Vector((0, 0, 0.40)))
    for sx in (-1, 1):                                                  # short legs
        for sy in (-1, 1):
            g.box(bm, (0.10, 0.10, 0.14), Vector((sx * 0.38, sy * 0.28, 0.07)))
    g.cyl(bm, 0.08, 0.6, T((0.25, 0.15, 0.95)), 16)                     # flue
    names.append(g.put("ext_heater_box", bm, iron, smooth=False).name)
    bm = bmesh.new()
    for sx in (-1, 1):
        g.box(bm, (0.08, 0.8, 0.08), Vector((sx * 0.50, 0, 0.70)))
        g.box(bm, (0.08, 0.08, 0.75), Vector((sx * 0.50, 0.36, 0.38)))
        g.box(bm, (0.08, 0.08, 0.75), Vector((sx * 0.50, -0.36, 0.38)))
    names.append(g.put("ext_heater_frame", bm, ash, smooth=False).name)
    bm = bmesh.new()                                                     # glowing grate in the front and the core seen through it
    for k in range(5):
        g.box(bm, (0.08, 0.02, 0.22), Vector((-0.28 + 0.14 * k, -0.355, 0.40)))
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.12, matrix=T((0, -0.18, 0.40)))
    names.append(g.put("ext_heater_core", bm, glow).name)
    return "FlametalHeater", names, (0, 0, 0.5), 3.0


exports, photos = g.script_args()
for build in (leach_barrel, settling_tank, black_metal_boiler, dvergr_drip_pipes, flametal_heater):
    sc, cam = g.stage((3.0, 3.0))
    model, names, centre, dist = build()
    print("EXTENSION", model, "TRIANGLES", g.tris(names))
    g.export(os.path.join(exports, model + ".fbx"), names)
    g.views(photos, "ext-" + model.lower(), centre, dist, 0.6)
print("EXTENSIONS DONE")
