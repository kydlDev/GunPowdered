"""Items and deposits: how the mod's materials look in the world and as dropped items.
Run inside Blender. Units: 1 Blender unit = 1 m. Each model is placed on its own spot along x (y=0) so they can be viewed together;
export() moves a model to the origin for its FBX and puts it back. Reuses the helpers of make_leaching_station.py.
Deposits (placed in the world, picked with a pickaxe): guano_deposit, sulfur_mud (+ sulfur_crystals).
Items (what a dropped stack looks like, also the source of the inventory icons): guano_item, saltpeter_item, gunpowder_item, water_item.
Object origin = bottom centre of each model. icon() renders a transparent PNG of one model for the inventory icon.
Shapes: guano and mud are soft jittered lumps (smooth shaded), saltpeter and gunpowder are powder mounds, water is a drop.
"""
import math
import random
import runpy
import bmesh
import bpy
from mathutils import Matrix, Vector

H = runpy.run_path(r"E:\Projects\Valheim\GunPowdered\GunPowdered-Blender\scripts\make_leaching_station.py", run_name="helpers")
mat, lathe = H["mat"], H["lathe"]
OUT_DIR = r"E:\Projects\Valheim\GunPowdered\GunPowdered-Blender\exports"
SPOTS = {"guano_deposit": 16.0, "sulfur_mud": 18.0, "sulfur_crystals": 18.0, "guano_item": 20.0, "saltpeter_item": 20.5,
         "gunpowder_item": 21.0, "water_item": 21.5}

GUANO = (0.07, 0.045, 0.03)
GUANO_PALE = (0.15, 0.13, 0.10)
MUD = (0.36, 0.29, 0.05)
CRYSTAL = (0.98, 0.88, 0.12)
SALT = (0.93, 0.94, 0.96)
POWDER = (0.025, 0.025, 0.028)
WATER = (0.25, 0.55, 0.85)


def put(name, bm, materials, smooth=True):
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    coll = bpy.data.collections.get("Items") or bpy.data.collections.new("Items")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    obj.location = (SPOTS[name], 0.0, 0.0)
    for m in (materials if isinstance(materials, (list, tuple)) else [materials]):
        me.materials.append(m)
    for p in me.polygons:
        p.use_smooth = smooth
    return obj


def lump(bm, rng, c, r, sq, mat_index=0, jitter=0.09):
    """One soft lump: a twice-subdivided icosphere, squashed, with every vertex pushed in or out a little so it is not a perfect ball."""
    m = Matrix.Translation(c) @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z") @ Matrix.Diagonal((r * sq[0], r * sq[1], r * sq[2], 1.0))
    res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0, matrix=m)
    verts = res["verts"]
    for v in verts:
        k = 1.0 + rng.uniform(-jitter, jitter)
        v.co = c + (v.co - c) * k
    faces = {f for v in verts for f in v.link_faces}
    for f in faces:
        f.material_index = mat_index


def heap(bm, rng, count, rad, height, size, sq=(1.0, 1.0, 0.7), pale_above=None):
    """A low heap of soft lumps inside a disc, higher in the middle; lumps above pale_above (a fraction of the height) get material 1."""
    for i in range(count):
        a = rng.uniform(0, 2 * math.pi)
        d = rad * math.sqrt(rng.uniform(0.0, 1.0)) * (0.8 if i else 0.0)
        z = height * (1.0 - (d / rad) ** 2) * 0.75 + size * 0.3
        r = size * rng.uniform(0.75, 1.3)
        pale = 1 if pale_above is not None and z > height * pale_above and rng.random() < 0.3 else 0
        lump(bm, rng, Vector((d * math.cos(a), d * math.sin(a), z)), r, sq, pale)


def mound(bm, rng, rad, height, segs=16):
    """A soft powder mound (a small dune): a lathed profile whose rim is pushed in and out a little."""
    prof = [(0.0, 0.0), (rad, 0.0), (rad * 0.93, height * 0.18), (rad * 0.72, height * 0.5), (rad * 0.42, height * 0.8), (rad * 0.14, height * 0.97), (0.0, height)]
    lathe(bm, prof, 0.0, 0.0, 0.0, segs)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    for v in bm.verts:
        if v.co.z > 0.0001:
            ang = math.atan2(v.co.y, v.co.x)
            k = 1.0 + 0.09 * math.sin(3 * ang + rng.uniform(0, 1)) + rng.uniform(-0.04, 0.04)
            v.co.x *= k
            v.co.y *= k


def build_guano_deposit():
    bm = bmesh.new()
    heap(bm, random.Random(11), 15, 0.24, 0.17, 0.085, (1.0, 0.9, 0.7), pale_above=0.6)
    return put("guano_deposit", bm, [mat("guano_mat", GUANO, 0.9), mat("guano_pale_mat", GUANO_PALE, 0.85)])


def build_sulfur_mud():
    bm = bmesh.new()
    heap(bm, random.Random(5), 12, 0.25, 0.18, 0.1, (1.1, 1.05, 0.7))
    return put("sulfur_mud", bm, mat("sulfur_mud_mat", MUD, 0.3))


def build_sulfur_crystals():
    bm = bmesh.new()
    rng = random.Random(8)
    for i in range(9):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(0.0, 0.17)
        h = rng.uniform(0.10, 0.20)
        tilt = rng.uniform(-0.5, 0.5)
        base_z = 0.18 * (1.0 - (d / 0.25) ** 2) * 0.75 + 0.03
        m = (Matrix.Translation(Vector((d * math.cos(a), d * math.sin(a), base_z + h * 0.3))) @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z")
             @ Matrix.Rotation(tilt, 4, "X"))
        bmesh.ops.create_cone(bm, cap_ends=True, segments=5, radius1=rng.uniform(0.022, 0.034), radius2=0.003, depth=h, matrix=m)
    return put("sulfur_crystals", bm, mat("sulfur_crystal_mat", CRYSTAL, 0.25, 0.0, (0.5, 0.42, 0.02)), smooth=False)


def build_guano_item():
    bm = bmesh.new()
    heap(bm, random.Random(21), 5, 0.03, 0.04, 0.026, (1.0, 0.9, 0.7), pale_above=0.85)
    return put("guano_item", bm, [mat("guano_mat", GUANO, 0.9), mat("guano_pale_mat", GUANO_PALE, 0.85)])


def build_saltpeter_item():
    bm = bmesh.new()
    rng = random.Random(31)
    mound(bm, rng, 0.06, 0.05)
    for i in range(4):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(0.0, 0.035)
        z = 0.05 * (1.0 - (d / 0.06) ** 1.6) - 0.002
        m = Matrix.Translation(Vector((d * math.cos(a), d * math.sin(a), z + 0.008))) @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z") @ Matrix.Rotation(rng.uniform(-0.4, 0.4), 4, "X")
        bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=0.007, radius2=0.0015, depth=0.02, matrix=m)
    return put("saltpeter_item", bm, mat("saltpeter_mat", SALT, 0.6))


def build_gunpowder_item():
    bm = bmesh.new()
    mound(bm, random.Random(41), 0.06, 0.045)
    return put("gunpowder_item", bm, mat("gunpowder_mat", POWDER, 0.75, 0.0))


def build_water_item():
    bm = bmesh.new()
    big_r, top = 0.055, 0.16
    prof = [(0.0, 0.0)]
    for k in range(1, 8):
        z = big_r * (1 - math.cos(math.pi / 2 * k / 7))
        prof.append((math.sqrt(max(big_r ** 2 - (big_r - z) ** 2, 0.0)), z))
    for k in range(1, 9):
        t = k / 8
        prof.append((big_r * (1 - t) ** 1.7, big_r + (top - big_r) * t))
    lathe(bm, prof, 0.0, 0.0, 0.0, 20)
    return put("water_item", bm, mat("water_drop_mat", WATER, 0.05, 0.0, None, 0.8))


BUILDERS = [build_guano_deposit, build_sulfur_mud, build_sulfur_crystals, build_guano_item, build_saltpeter_item, build_gunpowder_item, build_water_item]
FILES = {"GuanoDeposit": ["guano_deposit"], "SulfurousMud": ["sulfur_mud", "sulfur_crystals"], "BatGuano": ["guano_item"],
         "Saltpeter": ["saltpeter_item"], "Gunpowder": ["gunpowder_item"], "Water": ["water_item"]}


def build():
    return [b().name for b in BUILDERS]


def export():
    """One FBX per mod item / deposit; the models are moved to the origin for the export and put back."""
    for fbx, names in FILES.items():
        bpy.ops.object.select_all(action="DESELECT")
        saved = {}
        for n in names:
            o = bpy.data.objects[n]
            saved[n] = o.location.copy()
            o.location = (0.0, 0.0, 0.0)
            o.select_set(True)
        bpy.ops.export_scene.fbx(filepath=OUT_DIR + "\\" + fbx + ".fbx", use_selection=True, apply_unit_scale=True,
                                 apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                                 bake_space_transform=True, mesh_smooth_type="FACE")
        for n, loc in saved.items():
            bpy.data.objects[n].location = loc


def frame(names, az=0.0, elev=0.5):
    """Point the scene camera at the given models, far enough to see all of them, and hide the others in the render."""
    objs = [bpy.data.objects[n] for n in names]
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (lo + hi) / 2
    size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
    cam = bpy.data.objects["Camera"]
    d = size / 0.72 * 1.35
    cam.location = c + Vector((d * math.sin(az), -d * math.cos(az) * 0.85, d * elev))
    cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.hide_render = o.name not in names


def icon(names, path, size=256):
    """Transparent PNG of the given models (an inventory icon), seen from the front and a little above, then the scene is put back."""
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
    cam_data = bpy.data.cameras.new("icon_cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z) * 1.4
    cam = bpy.data.objects.new("icon_cam", cam_data)
    sc.collection.objects.link(cam)
    cam.location = center + Vector((0.0, -1.0, 0.8))
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.film_transparent = True
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.cameras.remove(cam_data)
    for o in bpy.data.objects:
        if o.name in hidden:
            o.hide_render = hidden[o.name]
    sc.render.resolution_x, sc.render.resolution_y, sc.render.film_transparent, sc.render.filepath, sc.camera = old


if __name__ == "__main__":
    build()
