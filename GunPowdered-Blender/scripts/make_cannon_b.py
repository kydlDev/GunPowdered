"""Hand cannon, model B (dark iron Loshult) on its stock, with the rear support lower than the front so the barrel tilts up a little (D-052 concept).
Run: blender -b --factory-startup --python make_cannon_b.py -- <side.png> <three-quarter.png>. Units: 1 Blender unit = 1 m. Concept render only.
"""
import math
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector

ARGS = sys.argv[sys.argv.index("--") + 1:]
SIDE = ARGS[0]
QUARTER = ARGS[1]
AXIS_Z = 0.235          # barrel axis height above the ground at the pivot (middle of the barrel)
SPAN = 0.24             # distance between the two supports
DROP = 0.04             # how much lower the rear cradle sits than the front one
PITCH = math.atan2(DROP, SPAN)


def mat(name, rgb, rough=0.8, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


def lathe(bm, profile, segs=40):
    rings = [[bm.verts.new((max(r, 1e-4) * math.cos(2 * math.pi * i / segs), max(r, 1e-4) * math.sin(2 * math.pi * i / segs), z)) for i in range(segs)]
             for r, z in profile]
    for k in range(len(profile)):
        a, b = rings[k], rings[(k + 1) % len(profile)]
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((a[i], a[j], b[j], b[i]))


def obj_from(name, bm, material, loc, rot=Matrix.Identity(4)):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = True
    o.matrix_world = Matrix.Translation(loc) @ rot
    return o


def box(bm, size, centre):
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(centre) @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))


def to_x():
    return Matrix.Rotation(math.radians(90), 4, "Y")


def barrel_profile():
    return [(0.0, 0.0), (0.012, 0.002), (0.024, 0.010), (0.032, 0.026), (0.034, 0.050), (0.033, 0.090), (0.030, 0.150), (0.028, 0.220), (0.029, 0.290),
            (0.034, 0.330), (0.040, 0.345), (0.041, 0.365), (0.041, 0.400), (0.026, 0.400), (0.024, 0.390), (0.024, 0.320), (0.0, 0.320)]


def add_barrel():
    iron = mat("iron", (0.05, 0.05, 0.055), rough=0.6, metal=0.85)
    parts = []
    bm = bmesh.new()
    lathe(bm, barrel_profile())
    parts.append(obj_from("barrel", bm, iron, Vector((-0.20, 0.0, AXIS_Z)), to_x()))
    sp = bmesh.new()
    bmesh.ops.create_cone(sp, cap_ends=True, segments=10, radius1=0.011, radius2=0.007, depth=0.016, matrix=Matrix.Identity(4))
    parts.append(obj_from("touchhole", sp, iron, Vector((-0.20 + 0.075, 0.0, AXIS_Z + 0.033))))
    for z in (0.08, 0.17, 0.26):                                       # reinforcing hoops
        h = bmesh.new()
        r = 0.0345 if z < 0.2 else 0.0305
        lathe(h, [(r + 0.004, -0.006), (r + 0.004, 0.006), (r - 0.002, 0.006), (r - 0.002, -0.006)], 40)
        parts.append(obj_from("hoop%.2f" % z, h, iron, Vector((-0.20 + z, 0.0, AXIS_Z)), to_x()))
    for side in (-1, 1):                                               # trunnion lugs
        t = bmesh.new()
        lathe(t, [(0.0, 0.0), (0.012, 0.0), (0.012, 0.03), (0.0, 0.03)], 20)
        parts.append(obj_from("lug%d" % side, t, iron, Vector((-0.20 + 0.17, side * 0.03, AXIS_Z)), Matrix.Rotation(math.radians(-90 * side), 4, "X")))
    pivot = Vector((0.0, 0.0, AXIS_Z))
    tilt = Matrix.Translation(pivot) @ Matrix.Rotation(-PITCH, 4, "Y") @ Matrix.Translation(-pivot)     # muzzle (+X) goes up
    for o in parts:
        o.matrix_world = tilt @ o.matrix_world


def floor_at(x):
    """Height of the cradle floor under the barrel at x, 0.04 below the barrel axis."""
    return AXIS_Z + x * math.tan(PITCH) - 0.04


def add_trestle(wood, x):
    f = floor_at(x)
    bm = bmesh.new()
    leg_top = f - 0.03
    for y in (-0.07, 0.07):
        box(bm, (0.045, 0.045, leg_top - 0.035), Vector((x, y, 0.035 + (leg_top - 0.035) / 2)))
    box(bm, (0.06, 0.20, 0.03), Vector((x, 0.0, f - 0.015)))
    for y in (-0.045, 0.045):
        box(bm, (0.06, 0.035, 0.05), Vector((x, y, f + 0.02)))
    obj_from("trestle%.2f" % x, bm, wood, Vector((0, 0, 0)))


def add_stock():
    wood = mat("wood", (0.30, 0.19, 0.10), rough=0.85)
    bm = bmesh.new()
    box(bm, (0.50, 0.20, 0.035), Vector((0.0, 0.0, 0.0175)))
    obj_from("base", bm, wood, Vector((0, 0, 0)))
    add_trestle(wood, -SPAN / 2)       # rear, lower
    add_trestle(wood, SPAN / 2)        # front, higher


def look_at(cam, target):
    d = Vector(target) - cam.location
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()


def scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 48
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = 1600, 1000
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.62, 0.7, 1.0)
    sc.world = world
    g = bmesh.new()
    box(g, (4.0, 3.0, 0.02), Vector((0.0, 0.0, -0.01)))
    obj_from("ground", g, mat("ground", (0.38, 0.5, 0.26), rough=0.95), Vector((0, 0, 0)))
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 3.5
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = (math.radians(55), math.radians(10), math.radians(-30))
    sc.collection.objects.link(so)
    add_stock()
    add_barrel()
    cam = bpy.data.cameras.new("cam")
    co = bpy.data.objects.new("cam", cam)
    sc.collection.objects.link(co)
    sc.camera = co
    cam.type = "ORTHO"                                                  # pure side view, shows the tilt
    cam.ortho_scale = 0.9
    co.location = Vector((0.0, -2.0, 0.24))
    look_at(co, (0.0, 0.0, 0.24))
    sc.render.filepath = SIDE
    bpy.ops.render.render(write_still=True)
    cam.type = "PERSP"                                                  # three-quarter view from the front left
    cam.lens = 45
    co.location = Vector((0.75, -1.0, 0.55))
    look_at(co, (0.0, 0.0, 0.2))
    sc.render.filepath = QUARTER
    bpy.ops.render.render(write_still=True)


scene()
