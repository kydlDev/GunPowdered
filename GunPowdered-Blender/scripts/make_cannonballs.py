"""Hand cannon ammunition previews (D-052): four shot variants that fit the 4.8 cm bore of the Loshult barrel (make_cannon_b.py).
1 smooth iron ball, 2 cast iron ball with a seam and a sprue nub, 3 rough stone ball (the original Loshult shot), 4 iron-tipped bolt with fins (the original gun also fired bolts).
Run: blender -b --factory-startup --python make_cannonballs.py -- <out.png>. Units: 1 Blender unit = 1 m. Concept render only.
"""
import math
import random
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector

OUT = sys.argv[sys.argv.index("--") + 1]
R = 0.021               # ball radius: a 4.2 cm ball in a 4.8 cm bore


def mat(name, rgb, rough, metal):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


def lathe(bm, profile, segs=36):
    rings = [[bm.verts.new((max(r, 1e-4) * math.cos(2 * math.pi * i / segs), max(r, 1e-4) * math.sin(2 * math.pi * i / segs), z)) for i in range(segs)]
             for r, z in profile]
    for k in range(len(profile)):
        a, b = rings[k], rings[(k + 1) % len(profile)]
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((a[i], a[j], b[j], b[i]))


def sphere_profile(r, rings=18):
    return [(r * math.sin(math.pi * k / rings), -r * math.cos(math.pi * k / rings)) for k in range(rings + 1)]


def put(name, bm, material, loc, rot=Matrix.Identity(4), smooth=True):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = smooth
    o.matrix_world = Matrix.Translation(loc) @ rot
    return o


def box(bm, size, centre, rot=Matrix.Identity(4)):
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(centre) @ rot @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))


def ball_smooth(x, iron):
    bm = bmesh.new()
    lathe(bm, sphere_profile(R))
    put("ball_smooth", bm, iron, Vector((x, 0, R)))


def ball_cast(x, cast):
    bm = bmesh.new()
    lathe(bm, sphere_profile(R))
    put("ball_cast", bm, cast, Vector((x, 0, R)))
    seam = bmesh.new()
    lathe(seam, [(R + 0.0012, -0.0016), (R + 0.0012, 0.0016), (R - 0.004, 0.0016), (R - 0.004, -0.0016)], 36)
    put("seam", seam, cast, Vector((x, 0, R)), Matrix.Rotation(math.radians(90), 4, "Y"))
    nub = bmesh.new()
    lathe(nub, [(0.0, 0.0), (0.005, 0.0), (0.0035, 0.004), (0.0, 0.004)], 14)
    put("sprue", nub, cast, Vector((x + 0.0, 0.0, 2 * R - 0.0008)))


def ball_stone(x, stone):
    rnd = random.Random(7)
    bm = bmesh.new()
    lathe(bm, sphere_profile(R, 20), 30)
    for v in bm.verts:
        v.co = v.co * (1.0 + (rnd.random() - 0.5) * 0.07)
    put("ball_stone", bm, stone, Vector((x, 0, R)), smooth=True)


def bolt(x, iron, wood):
    bm = bmesh.new()
    lathe(bm, [(0.0, 0.0), (0.004, 0.012), (0.010, 0.030), (0.0105, 0.042), (0.0075, 0.046), (0.0075, 0.17), (0.0, 0.17)], 20)
    put("bolt_head", bm, iron, Vector((x - 0.085, 0, 0.0105)), Matrix.Rotation(math.radians(90), 4, "Y"))
    fins = bmesh.new()
    for a in (0, 120, 240):
        box(fins, (0.03, 0.002, 0.05), Vector((0.0, 0.0, 0.0)), Matrix.Rotation(math.radians(a), 4, "Z") @ Matrix.Translation((0.016, 0, 0.145)))
    put("bolt_fins", fins, wood, Vector((x - 0.085, 0, 0.0105)), Matrix.Rotation(math.radians(90), 4, "Y"), smooth=False)


def scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 64
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = 1800, 800
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.62, 0.7, 1.0)
    sc.world = world
    g = bmesh.new()
    box(g, (2.0, 2.0, 0.02), Vector((0.0, 0.0, -0.01)))
    put("ground", g, mat("ground", (0.30, 0.22, 0.14), 0.95, 0.0), Vector((0, 0, 0)))
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 4.0
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
    sc.collection.objects.link(so)
    iron = mat("iron", (0.05, 0.05, 0.055), 0.45, 0.9)
    cast = mat("cast", (0.07, 0.065, 0.06), 0.7, 0.8)
    stone = mat("stone", (0.16, 0.155, 0.145), 0.95, 0.0)
    wood = mat("wood", (0.30, 0.19, 0.10), 0.85, 0.0)
    ball_smooth(-0.19, iron)
    ball_cast(-0.07, cast)
    ball_stone(0.05, stone)
    bolt(0.23, iron, wood)
    cam = bpy.data.cameras.new("cam")
    cam.lens = 35
    co = bpy.data.objects.new("cam", cam)
    sc.collection.objects.link(co)
    sc.camera = co
    co.location = Vector((0.055, -0.78, 0.34))
    co.rotation_euler = (Vector((0.055, 0.0, 0.02)) - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = OUT
    bpy.ops.render.render(write_still=True)


scene()
