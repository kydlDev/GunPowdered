"""Musket ammunition models (D-080 / D-081): GP_Paper, GP_IronBullet, GP_PaperCartridge.
Run: blender -b --factory-startup --python make_musket_ammo.py -- <exports dir> <photos dir>
Sizes are a little bigger than life like the other items (a dropped stack must be readable). Object origin = bottom centre, items stand up (+Z).
Paper.fbx: paper_sheets (three loose sheets, plain flat, no curl). IronBullet.fbx: bullet_ball (dark iron ball with a small casting nub).
Cartridge.fbx: cartridge_paper (tube: ball end at the bottom, folded and rounded top), cartridge_thread (two tied bindings).
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gp_common as g


def sheets():
    paper = g.mat("ammo_paper", (0.86, 0.80, 0.64), 0.95, 0.0)
    bm = bmesh.new()
    for i, (dx, dy, rot, z) in enumerate(((0.0, 0.0, 0.0, 0.0), (0.006, -0.004, 14.0, 0.004), (-0.004, 0.005, -9.0, 0.008))):
        g.box(bm, (0.15, 0.105, 0.003), Vector((dx, dy, z + 0.0015)), Matrix.Rotation(math.radians(rot), 4, "Z"))
    top = Matrix.Translation((0.0, 0.0, 0.0))
    return [g.put("paper_sheets", bm, paper, top, smooth=False).name]


def bullet():
    iron = g.mat("ammo_iron", (0.10, 0.10, 0.11), 0.45, 0.85)
    R = 0.02
    prof = [(0.0, 0.0)]
    for k in range(1, 12):
        a = math.pi * k / 12
        prof.append((R * math.sin(a), R - R * math.cos(a)))
    prof.append((0.0, 2 * R))
    bm = bmesh.new()
    g.lathe(bm, prof, segs=20)
    g.cyl(bm, 0.0028, 0.003, Matrix.Translation((0.0, 0.0, 2 * R + 0.0005)), 8, r2=0.002)       # casting nub
    return [g.put("bullet_ball", bm, iron).name]


def cartridge():
    paper = g.mat("ammo_cartridge", (0.80, 0.70, 0.50), 0.95, 0.0)
    thread = g.mat("ammo_thread", (0.28, 0.20, 0.12), 0.9, 0.0)
    R, rb = 0.018, 0.021
    prof = [(0.0, 0.0), (rb * 0.6, 0.003), (rb, 0.014), (rb, 0.030), (rb * 0.8, 0.044), (R * 0.62, 0.052),      # ball end, pinched at the neck
            (R * 0.95, 0.062), (R, 0.075), (R, 0.092), (R * 0.88, 0.102), (R * 0.55, 0.108), (0.0, 0.110)]       # short powder tube, closed rounded top (no twisted tip)
    bm = bmesh.new()
    g.lathe(bm, prof, segs=18)
    names = [g.put("cartridge_paper", bm, paper).name]
    bm = bmesh.new()
    for z, r in ((0.050, R * 0.66),):
        g.ring(bm, r + 0.0022, r - 0.0005, 0.005, Matrix.Translation((0, 0, z)), 18)
    names.append(g.put("cartridge_thread", bm, thread).name)
    return names


def icon(names, path, size=256):
    sc = bpy.context.scene
    old = (sc.render.resolution_x, sc.render.resolution_y, sc.render.film_transparent, sc.render.filepath, sc.camera)
    objs = [bpy.data.objects[n] for n in names]
    bpy.context.view_layer.update()
    hidden = {o.name: o.hide_render for o in bpy.data.objects}
    for o in bpy.data.objects:
        o.hide_render = o.type == "MESH" and o not in objs
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    center = (lo + hi) / 2
    cd = bpy.data.cameras.new("icon_cam")
    cd.type = "ORTHO"
    cd.ortho_scale = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z) * 1.4
    cam = bpy.data.objects.new("icon_cam", cd)
    sc.collection.objects.link(cam)
    cam.location = center + Vector((0.0, -1.0, 0.8))
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.film_transparent = True
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.cameras.remove(cd)
    for o in bpy.data.objects:
        if o.name in hidden:
            o.hide_render = hidden[o.name]
    sc.render.resolution_x, sc.render.resolution_y, sc.render.film_transparent, sc.render.filepath, sc.camera = old


exports, photos = g.script_args()
sc, cam = g.stage((0.8, 0.8))
PAPER, BULLET, CART = sheets(), bullet(), cartridge()
# placed side by side for one picture: paper left, bullet middle, cartridge right
for n, dx in (("paper_sheets", -0.14), ("bullet_ball", 0.0), ("cartridge_paper", 0.12), ("cartridge_thread", 0.12)):
    bpy.data.objects[n].location.x += dx
print("TRIANGLES paper", g.tris(PAPER), "bullet", g.tris(BULLET), "cartridge", g.tris(CART))
g.views(photos, "musket-ammo", (0.0, 0.0, 0.07), 0.55, 0.05)
for n, dx in (("paper_sheets", -0.14), ("bullet_ball", 0.0), ("cartridge_paper", 0.12), ("cartridge_thread", 0.12)):
    bpy.data.objects[n].location.x -= dx
g.export(os.path.join(exports, "Paper.fbx"), PAPER)
g.export(os.path.join(exports, "IronBullet.fbx"), BULLET)
g.export(os.path.join(exports, "Cartridge.fbx"), CART)
for names, label in ((PAPER, "Paper"), (BULLET, "IronBullet"), (CART, "Cartridge")):
    icon(names, os.path.join(photos, "%s_icon-%s.png" % (g.stamp(), label)))
