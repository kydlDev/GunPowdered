"""Hand cannon (D-051, D-052, D-053): three assets after the user's Loshult picture, model B (dark iron).
Run: blender -b --factory-startup --python make_cannon.py -- <preview.png>   Units: 1 Blender unit = 1 m. The muzzle points along -Y in Blender (= +Z in Unity: the FBX export maps Blender -Y to Unity +Z).
Cannon.fbx      cannon_barrel (iron: rounded breech, slim waist, flared muzzle ring, 3 hoops, 2 side lugs, raised touch hole) + empties Muzzle, TouchHole.
                Origin = the middle of the barrel on its axis (the pivot), so it sits on the stock's MountPoint without an offset.
CannonStock.fbx cannon_stock (wood: base plank, two trestles, the REAR cradle (+Y side) 4 cm lower than the front one (-Y side)) + empty MountPoint (at the barrel pivot,
                pitched up by about 9.5 degrees, muzzle up). Origin = bottom centre of the base plank.
CannonBall.fbx  cannon_ball (cast iron, 10 cm: seam ring + sprue nub). Origin = bottom centre.
Icons: Cannon.png (barrel), CannonStock.png (stock + barrel), CannonBall.png go to GunPowdered-Unity/Assets/GunPowdered_Art/Icons (256 px, transparent).
Game scale: the real gun is 31 cm long; the piece is built 2.5 times bigger (barrel 1.0 m, bore 12 cm) so that it can be used from the stock;
the ball is 10 cm (fits the 12 cm bore). All three are box-projected (D-029, one texture tile = min(size, 0.5 m)). Concept preview PNG is rendered at the end.
"""
import math
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector

OUT_DIR = r"E:\Projects\Valheim\GunPowdered\GunPowdered-Blender\exports"
PREVIEW = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else OUT_DIR + r"\cannon-preview.png"
SCALE = 2.5                  # the piece is 2.5 times the real size
BALL_RADIUS = 0.05           # the ball: 10 cm across
AXIS_Z = 0.235               # barrel axis above the ground at the pivot, real size
SPAN = 0.24                  # distance between the two trestles, real size
DROP = 0.04                  # rear cradle lower than the front one, real size
PITCH = math.atan2(DROP, SPAN)
SPOTS = {"stock": Vector((0.0, 0.0, 0.0)), "ball": Vector((1.15, -0.2, 0.0))}


def mat(name, rgb, rough, metal):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


def lathe(bm, profile, matrix, segs=40, close=False):
    """Revolves a (radius, height) profile around Z, then moves the new vertices by matrix. close=True joins the last ring to the first."""
    rings, made = [], []
    for r, z in profile:
        ring = [bm.verts.new((max(r, 1e-4) * math.cos(2 * math.pi * i / segs), max(r, 1e-4) * math.sin(2 * math.pi * i / segs), z)) for i in range(segs)]
        rings.append(ring)
        made += ring
    for k in range(len(profile) if close else len(profile) - 1):
        a, b = rings[k], rings[(k + 1) % len(profile)]
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.transform(bm, matrix=matrix, verts=made)


def box(bm, size, centre):
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(centre) @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))


def box_uv(bm, tile):
    uv = bm.loops.layers.uv.new("UVMap")
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        u_i, v_i = [(1, 2), (0, 2), (0, 1)][ax]
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co[u_i] / tile, loop.vert.co[v_i] / tile)


def put(name, bm, material, scale, smooth=True):
    """Scales the mesh, welds, maps and links it into the 'Weapons' collection. Returns the object."""
    bmesh.ops.scale(bm, vec=(scale, scale, scale), verts=bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ext = max(max(v.co[i] for v in bm.verts) - min(v.co[i] for v in bm.verts) for i in range(3))
    box_uv(bm, min(ext, 0.5))
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    coll = bpy.data.collections.get("Weapons") or bpy.data.collections.new("Weapons")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = smooth
    return obj


def empty(name, loc, rot=(0.0, 0.0, 0.0)):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "ARROWS"
    e.empty_display_size = 0.05
    e.location = loc
    e.rotation_euler = rot
    bpy.data.collections["Weapons"].objects.link(e)
    return e


# ---------------------------------------------------------------- the barrel (real-size numbers, along +Y, centred on the axis pivot)
BARREL = [(0.0, 0.0), (0.012, 0.002), (0.024, 0.010), (0.032, 0.026), (0.034, 0.050), (0.033, 0.090), (0.030, 0.150), (0.028, 0.220), (0.029, 0.290),
          (0.034, 0.330), (0.040, 0.345), (0.041, 0.365), (0.041, 0.400), (0.026, 0.400), (0.024, 0.390), (0.024, 0.320), (0.0, 0.320)]
ALONG_Y = Matrix.Rotation(math.radians(90), 4, "X")         # lathe axis Z -> -Y (the muzzle points along -Y = +Z in Unity)


def build_barrel(iron):
    bm = bmesh.new()
    lathe(bm, BARREL, Matrix.Translation((0.0, 0.20, 0.0)) @ ALONG_Y)
    lathe(bm, [(0.0, 0.0), (0.011, 0.0), (0.007, 0.016), (0.0, 0.016)], Matrix.Translation((0.0, 0.125, 0.030)), 12)         # the touch hole spout
    for z in (0.08, 0.17, 0.26):                                                                                             # reinforcing hoops
        r = 0.0345 if z < 0.2 else 0.0305
        lathe(bm, [(r + 0.004, -0.006), (r + 0.004, 0.006), (r - 0.002, 0.006), (r - 0.002, -0.006)],
              Matrix.Translation((0.0, 0.20 - z, 0.0)) @ ALONG_Y, 40, close=True)
    for side in (-1, 1):                                                                                                     # trunnion lugs
        lathe(bm, [(0.0, 0.0), (0.012, 0.0), (0.012, 0.03), (0.0, 0.03)],
              Matrix.Translation((side * 0.026, 0.03, 0.0)) @ Matrix.Rotation(math.radians(90 * side), 4, "Y"), 20)
    barrel = put("cannon_barrel", bm, iron, SCALE)
    empty("Muzzle", (0.0, -0.20 * SCALE, 0.0))
    empty("TouchHole", (0.0, 0.125 * SCALE, 0.046 * SCALE))
    return barrel


# ---------------------------------------------------------------- the stock (rear cradle lower than the front one)
def floor_at(y):
    return AXIS_Z - y * math.tan(PITCH) - 0.04


def add_trestle(bm, y):
    f = floor_at(y)
    top = f - 0.03
    for x in (-0.07, 0.07):
        box(bm, (0.045, 0.045, top - 0.035), Vector((x, y, 0.035 + (top - 0.035) / 2)))
    box(bm, (0.20, 0.06, 0.03), Vector((0.0, y, f - 0.015)))
    for x in (-0.045, 0.045):
        box(bm, (0.035, 0.06, 0.05), Vector((x, y, f + 0.02)))


def build_stock(wood):
    bm = bmesh.new()
    box(bm, (0.20, 0.50, 0.035), Vector((0.0, 0.0, 0.0175)))
    add_trestle(bm, -SPAN / 2)
    add_trestle(bm, SPAN / 2)
    stock = put("cannon_stock", bm, wood, SCALE, smooth=False)
    empty("MountPoint", (0.0, 0.0, AXIS_Z * SCALE), (-PITCH, 0.0, 0.0))
    return stock


# ---------------------------------------------------------------- the cast iron ball (origin at the bottom)
def build_ball(cast):
    r = BALL_RADIUS / SCALE            # build in real-size numbers, put() scales them up by SCALE
    bm = bmesh.new()
    arc = [(r * math.sin(math.pi * k / 18), -r * math.cos(math.pi * k / 18)) for k in range(19)]
    lathe(bm, arc, Matrix.Translation((0.0, 0.0, r)))
    lathe(bm, [(r + 0.0012, -0.0016), (r + 0.0012, 0.0016), (r - 0.004, 0.0016), (r - 0.004, -0.0016)],
          Matrix.Translation((0.0, 0.0, r)) @ Matrix.Rotation(math.radians(90), 4, "Y"), 36, close=True)                         # the casting seam
    lathe(bm, [(0.0, 0.0), (0.005, 0.0), (0.0035, 0.004), (0.0, 0.004)], Matrix.Translation((0.0, 0.0, 2 * r - 0.0008)), 14)   # the sprue nub
    return put("cannon_ball", bm, cast, SCALE)


# ---------------------------------------------------------------- build, export, preview
def export(path, names):
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        bpy.data.objects[n].select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z",
                             axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", object_types={"MESH", "EMPTY"})
    return path


def build_all():
    iron = mat("cannon_iron", (0.05, 0.05, 0.055), 0.6, 0.85)
    wood = mat("cannon_wood", (0.30, 0.19, 0.10), 0.85, 0.0)
    cast = mat("cast_iron", (0.07, 0.065, 0.06), 0.7, 0.8)
    barrel, stock, ball = build_barrel(iron), build_stock(wood), build_ball(cast)
    tris = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in (barrel, stock, ball)}
    for o in (barrel, stock, ball):
        o.location = (0.0, 0.0, 0.0)
        o.rotation_euler = (0.0, 0.0, 0.0)
    out = [export(OUT_DIR + r"\Cannon.fbx", ["cannon_barrel", "Muzzle", "TouchHole"]),
           export(OUT_DIR + r"\CannonStock.fbx", ["cannon_stock", "MountPoint"]),
           export(OUT_DIR + r"\CannonBall.fbx", ["cannon_ball"])]
    return tris, out


def look_at(cam, target):
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def preview(path):
    """The three assets placed as in game: barrel in the stock's cradles, ball next to it."""
    mp = bpy.data.objects["MountPoint"]
    bpy.data.objects["cannon_barrel"].matrix_world = mp.matrix_world
    bpy.data.objects["cannon_ball"].location = SPOTS["ball"]
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 48
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = 1800, 1000
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.62, 0.7, 1.0)
    sc.world = world
    g = bmesh.new()
    box(g, (8.0, 8.0, 0.02), Vector((0.0, 0.0, -0.01)))
    gm = bpy.data.meshes.new("ground")
    g.to_mesh(gm)
    g.free()
    go = bpy.data.objects.new("ground", gm)
    sc.collection.objects.link(go)
    gm.materials.append(mat("ground", (0.38, 0.5, 0.26), 0.95, 0.0))
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 3.5
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = (math.radians(55), math.radians(10), math.radians(-30))
    sc.collection.objects.link(so)
    cam = bpy.data.cameras.new("cam")
    cam.lens = 38
    co = bpy.data.objects.new("cam", cam)
    sc.collection.objects.link(co)
    sc.camera = co
    co.location = Vector((1.9, -2.1, 1.2))
    look_at(co, (0.55, 0.0, 0.35))
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


ICON_DIR = r"E:\Projects\Valheim\GunPowdered\GunPowdered-Unity\Assets\GunPowdered_Art\Icons"


def icon(names, path, view, size=256):
    """Transparent PNG icon of the given objects (like make_items.icon): orthographic, seen from 'view' relative to the middle of the objects."""
    sc = bpy.context.scene
    objs = [bpy.data.objects[n] for n in names]
    bpy.context.view_layer.update()
    hidden = {o.name: o.hide_render for o in bpy.data.objects}
    for o in bpy.data.objects:
        o.hide_render = o.type == "MESH" and o not in objs
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    centre = (lo + hi) / 2
    cd = bpy.data.cameras.new("icon_cam")
    cd.type = "ORTHO"
    cd.ortho_scale = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z) * 1.25
    cam = bpy.data.objects.new("icon_cam", cd)
    sc.collection.objects.link(cam)
    cam.location = centre + Vector(view)
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
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


def icons():
    """The three inventory icons: the barrel alone (muzzle to the right), the stock with the barrel in its cradles, the ball."""
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 48
    sc.cycles.use_denoising = False
    world = bpy.data.worlds.new("iconworld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.6, 0.65, 0.7, 1.0)
    sc.world = world
    sun = bpy.data.lights.new("iconsun", "SUN")
    sun.energy = 3.5
    so = bpy.data.objects.new("iconsun", sun)
    so.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    sc.collection.objects.link(so)
    barrel = bpy.data.objects["cannon_barrel"]
    icon(["cannon_barrel"], ICON_DIR + r"\Cannon.png", (-1.0, -0.25, 0.55))
    barrel.matrix_world = bpy.data.objects["MountPoint"].matrix_world
    icon(["cannon_stock", "cannon_barrel"], ICON_DIR + r"\CannonStock.png", (-1.0, -0.9, 0.7))
    barrel.matrix_world = Matrix.Identity(4)
    icon(["cannon_ball"], ICON_DIR + r"\CannonBall.png", (0.0, -1.0, 0.8))
    bpy.data.objects.remove(so, do_unlink=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
TRIS, FILES = build_all()
print("CANNON TRIANGLES", TRIS)
print("CANNON EXPORTS", FILES)
icons()
preview(PREVIEW)
