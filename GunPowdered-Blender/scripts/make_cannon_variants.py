"""Hand cannon concept variants (D-051): three cast-metal barrels after the user's picture of the Loshult gun (a 14th century bronze hand gun:
a blunt rounded breech, a barrel that slims towards the middle and a thick flared muzzle ring, a touch hole on top), each lying on a simple wooden
trestle stock. Run: blender -b --factory-startup --python make_cannon_variants.py -- <output.png>. Units: 1 Blender unit = 1 m.
A = faithful bronze Loshult, B = the same cast in dark iron (Swamp tier) with hoops and trunnion lugs, C = longer and slimmer with a rear tang that
pushes into the stock and a bell muzzle. Concept render only: nothing here is exported to Unity yet.
"""
import math
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else r"E:\Projects\Valheim\Photos\GunPowdered\hand-cannon-variants.png"
BARREL_Z = 0.235     # height of the barrel axis above the ground


def mat(name, rgb, rough=0.8, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


def lathe(bm, profile, segs=40):
    """Revolves a closed (r, z) profile around the Z axis."""
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


def to_x():
    """Turns a barrel built along +Z so that it lies along +X (muzzle to the right)."""
    return Matrix.Rotation(math.radians(90), 4, "Y")


def box(bm, size, centre, rot_y=0.0):
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(centre) @ Matrix.Rotation(rot_y, 4, "Y") @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))


def barrel_a():
    """Faithful Loshult: blunt rounded breech, slim waist, thick muzzle ring with a step, bore, touch hole on top."""
    return [(0.0, 0.0), (0.012, 0.002), (0.024, 0.010), (0.032, 0.026), (0.034, 0.050), (0.033, 0.090), (0.030, 0.150), (0.028, 0.220), (0.029, 0.290),
            (0.034, 0.330), (0.040, 0.345), (0.041, 0.365), (0.041, 0.400), (0.026, 0.400), (0.024, 0.390), (0.024, 0.320), (0.0, 0.320)]


def barrel_c():
    """Longer and slimmer, breech tang, smooth bell muzzle."""
    return [(0.0, 0.0), (0.006, 0.000), (0.014, 0.004), (0.026, 0.020), (0.030, 0.045), (0.029, 0.120), (0.026, 0.220), (0.025, 0.320), (0.027, 0.400),
            (0.033, 0.435), (0.040, 0.452), (0.043, 0.460), (0.027, 0.460), (0.024, 0.450), (0.022, 0.380), (0.0, 0.380)]


def add_cannon(variant, cx):
    prof = barrel_c() if variant == "C" else barrel_a()
    bronze = mat("bronze" + variant, (0.10, 0.085, 0.05), rough=0.55, metal=0.7)
    iron = mat("iron" + variant, (0.05, 0.05, 0.055), rough=0.6, metal=0.85)
    m = iron if variant == "B" else bronze
    bm_spout_z = 0.075 if variant != "C" else 0.09
    y = 0.0
    bm = bmesh.new()
    lathe(bm, prof)
    barrel = obj_from("barrel" + variant, bm, m, Vector((cx - 0.20, y, BARREL_Z)), to_x())
    # touch hole spout as its own small object standing on the top of the barrel
    sp = bmesh.new()
    bmesh.ops.create_cone(sp, cap_ends=True, segments=10, radius1=0.011, radius2=0.007, depth=0.016, matrix=Matrix.Translation((0, 0, 0)))
    obj_from("spout" + variant, sp, m, Vector((cx - 0.20 + bm_spout_z, y, BARREL_Z + 0.033)))
    if variant == "B":
        for z in (0.08, 0.17, 0.26):                     # reinforcing hoops
            h = bmesh.new()
            r = 0.0345 if z < 0.2 else 0.0305
            lathe(h, [(r + 0.004, -0.006), (r + 0.004, 0.006), (r - 0.002, 0.006), (r - 0.002, -0.006)], 40)
            obj_from("hoop%s%.2f" % (variant, z), h, m, Vector((cx - 0.20 + z, y, BARREL_Z)), to_x())
        for side in (-1, 1):                              # trunnion lugs
            t = bmesh.new()
            lathe(t, [(0.0, 0.0), (0.012, 0.0), (0.012, 0.03), (0.0, 0.03)], 20)
            obj_from("lug%s%d" % (variant, side), t, m, Vector((cx - 0.20 + 0.17, y + side * 0.03, BARREL_Z)), Matrix.Rotation(math.radians(-90 * side), 4, "X"))
    if variant == "C":                                    # the tang at the breech
        tg = bmesh.new()
        bmesh.ops.create_cone(tg, cap_ends=True, segments=4, radius1=0.016, radius2=0.006, depth=0.08, matrix=Matrix.Translation((0, 0, 0)))
        obj_from("tang", tg, m, Vector((cx - 0.20 - 0.038, y, BARREL_Z)), to_x().inverted() @ Matrix.Rotation(math.radians(180), 4, "Y"))
    return barrel


def add_stock(cx):
    """A plain wooden trestle: a base plank, two sloped legs and a saddle with a V cradle for the barrel."""
    wood = mat("wood", (0.30, 0.19, 0.10), rough=0.85)
    bm = bmesh.new()
    box(bm, (0.50, 0.20, 0.035), Vector((0.0, 0.0, 0.0175)))                                    # base
    for x in (-0.12, 0.12):
        box(bm, (0.045, 0.045, 0.19), Vector((x, 0.07, 0.115)), 0.0)
        box(bm, (0.045, 0.045, 0.19), Vector((x, -0.07, 0.115)), 0.0)                           # legs
        box(bm, (0.06, 0.20, 0.03), Vector((x, 0.0, 0.18)))                                      # cross saddle
        box(bm, (0.06, 0.035, 0.05), Vector((x, 0.045, 0.215)))
        box(bm, (0.06, 0.035, 0.05), Vector((x, -0.045, 0.215)))                                 # the two cradle cheeks
    obj_from("stock", bm, wood, Vector((cx, 0.0, 0.0)))


def scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 48
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = 2400, 1100
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.62, 0.7, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    sc.world = world
    ground = bmesh.new()
    box(ground, (6.0, 3.0, 0.02), Vector((0.0, 0.0, -0.01)))
    obj_from("ground", ground, mat("ground", (0.38, 0.5, 0.26), rough=0.95), Vector((0, 0, 0)))
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 3.5
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = (math.radians(55), math.radians(10), math.radians(-30))
    sc.collection.objects.link(so)
    cam = bpy.data.cameras.new("cam")
    cam.lens = 42
    co = bpy.data.objects.new("cam", cam)
    sc.collection.objects.link(co)
    co.location = Vector((0.0, -2.35, 1.15))
    co.rotation_euler = (math.radians(64), 0, 0)
    sc.camera = co
    for i, v in enumerate(("A", "B", "C")):
        cx = (i - 1) * 0.72
        add_stock(cx)
        add_cannon(v, cx)
    sc.render.filepath = OUT
    bpy.ops.render.render(write_still=True)


scene()
