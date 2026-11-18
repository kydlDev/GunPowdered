"""Leaching Station (saltpeter): a stone-filled leaching barrel on a stand, a trough, and a fire-heated cauldron.
Run inside Blender. Units: 1 Blender unit = 1 m. Station origin = ground centre (placed at x=4 next to the bucket).
Parts (each its own object so the mod can switch states): leach_barrel, leach_hoops, leach_stones, leach_stand,
leach_trough, hearth_stones, cauldron, cauldron_legs, cauldron_liquid, cauldron_fire, cauldron_salt.
Layout: barrel at x=-0.75 (feed 1: water + guano at the top), hearth with cauldron at x=+0.6,
the hearth itself takes the wood (feed 2: wood for the fire, no separate wood pile).
States: idle (no liquid, fire, salt) / burning (liquid + fire) / done (salt only).
"""
import math
import random
import bmesh
import bpy
from mathutils import Matrix, Vector

ORIGIN = Vector((4.0, 0.0, 0.0))
BARREL_X, HEARTH_X = -0.75, 0.60
STAND_H = 0.72            # height of the barrel bottom
BARREL_H = 0.95
CAULDRON_Z = 0.42
HOLE_Z = 0.13            # height of the outlet hole above the barrel bottom (the inner floor is at 0.10)
HOLE_R = 0.02


def mat(name, rgb, rough=0.8, metal=0.0, emit=None, alpha=1.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    b.inputs["Alpha"].default_value = alpha
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1.0)
        b.inputs["Emission Strength"].default_value = 1.2
    return m


def put(name, bm, material):
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    me = bpy.data.meshes.new(name)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    coll = bpy.data.collections.get("Station") or bpy.data.collections.new("Station")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    obj.location = ORIGIN
    me.materials.append(material)
    return obj


def lathe(bm, profile, cx=0.0, cy=0.0, cz=0.0, segs=32):
    """Revolves a closed (r, z) profile around the vertical axis through (cx, cy)."""
    rings = []
    for r, z in profile:
        rings.append([bm.verts.new((cx + max(r, 1e-4) * math.cos(2 * math.pi * i / segs),
                                    cy + max(r, 1e-4) * math.sin(2 * math.pi * i / segs), cz + z))
                      for i in range(segs)])
    for k in range(len(profile)):
        a, b = rings[k], rings[(k + 1) % len(profile)]
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((a[i], a[j], b[j], b[i]))


def box(bm, c, size, rot_y=0.0, rot_z=0.0):
    m = Matrix.Translation(c) @ Matrix.Rotation(rot_z, 4, "Z") @ Matrix.Rotation(rot_y, 4, "Y")
    bmesh.ops.create_cube(bm, size=1.0, matrix=m @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))


def log(bm, c, length, radius, rot_z, tilt=0.0):
    m = (Matrix.Translation(c) @ Matrix.Rotation(rot_z, 4, "Z") @ Matrix.Rotation(tilt, 4, "Y")
         @ Matrix.Rotation(math.pi / 2, 4, "Y"))
    bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=radius, radius2=radius, depth=length, matrix=m)


def blob(bm, c, r, sq=(1.0, 1.0, 0.7)):
    m = Matrix.Translation(c) @ Matrix.Diagonal((r * sq[0], r * sq[1], r * sq[2], 1.0))
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0, matrix=m)


BARREL_OUT = [(0.37, 0.0), (0.40, 0.04), (0.45, 0.30), (0.46, 0.50), (0.44, 0.75), (0.41, BARREL_H)]


def barrel_r(z):
    for (r0, z0), (r1, z1) in zip(BARREL_OUT, BARREL_OUT[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    return BARREL_OUT[-1][0]


def cut_hole(obj):
    """A small round hole low in the barrel wall: the only way the liquid leaves the barrel (it runs into the trough)."""
    cb = bmesh.new()
    m = Matrix.Translation(Vector((BARREL_X + 0.40, 0.0, STAND_H + HOLE_Z))) @ Matrix.Rotation(math.pi / 2, 4, "Y")
    bmesh.ops.create_cone(cb, cap_ends=True, segments=12, radius1=HOLE_R, radius2=HOLE_R, depth=0.16, matrix=m)
    me = bpy.data.meshes.new("hole_cut_mesh")
    cb.to_mesh(me)
    cb.free()
    cut = bpy.data.objects.new("hole_cut", me)
    cut.location = ORIGIN
    bpy.context.scene.collection.objects.link(cut)
    mod = obj.modifiers.new("hole", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cut
    mod.solver = "EXACT"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut, do_unlink=True)


def build_barrel():
    bm = bmesh.new()
    inner = [(r - 0.035, z) for r, z in reversed(BARREL_OUT[1:])]
    profile = [(0.0, 0.0)] + BARREL_OUT + inner + [(0.0, 0.10)]
    lathe(bm, [(r, z + (0.10 if i == len(profile) - 1 else 0.0) * 0) for i, (r, z) in enumerate(profile)],
          BARREL_X, 0.0, STAND_H)
    obj = put("leach_barrel", bm, mat("leach_wood_mat", (0.30, 0.17, 0.08), 0.8))
    cut_hole(obj)
    return obj


def build_hoops():
    bm = bmesh.new()
    for z in (0.27, 0.57, 0.87):
        r = barrel_r(z)
        lathe(bm, [(r - 0.004, z - 0.022), (r + 0.014, z - 0.022), (r + 0.014, z + 0.022), (r - 0.004, z + 0.022)],
              BARREL_X, 0.0, STAND_H, 36)
    return put("leach_hoops", bm, mat("leach_iron_mat", (0.035, 0.032, 0.03), 0.45, 0.7))


def build_filter_stones():
    rnd = random.Random(7)
    bm = bmesh.new()
    top = STAND_H + BARREL_H - 0.10
    for _ in range(70):
        a, d = rnd.uniform(0, 2 * math.pi), 0.34 * math.sqrt(rnd.random())
        blob(bm, Vector((BARREL_X + d * math.cos(a), d * math.sin(a), top + rnd.uniform(-0.02, 0.03))),
             rnd.uniform(0.035, 0.065), (1.0, rnd.uniform(0.8, 1.1), 0.7))
    return put("leach_stones", bm, mat("leach_stone_mat", (0.42, 0.41, 0.39), 0.95))


def build_stand():
    bm = bmesh.new()
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, Vector((BARREL_X + sx * 0.36, sy * 0.36, STAND_H / 2)), (0.09, 0.09, STAND_H))
    for z in (0.28, STAND_H - 0.03):
        box(bm, Vector((BARREL_X, 0.36, z)), (0.80, 0.07, 0.07))
        box(bm, Vector((BARREL_X, -0.36, z)), (0.80, 0.07, 0.07))
        box(bm, Vector((BARREL_X + 0.36, 0, z)), (0.07, 0.80, 0.07))
        box(bm, Vector((BARREL_X - 0.36, 0, z)), (0.07, 0.80, 0.07))
    return put("leach_stand", bm, mat("leach_wood_mat", (0.30, 0.17, 0.08), 0.8))


def trough_line():
    xa, za, xb, zb = BARREL_X + 0.40, STAND_H + HOLE_Z - 0.075, HEARTH_X - 0.10, CAULDRON_Z + 0.28
    return xa, za, xb, zb, math.hypot(xb - xa, za - zb), math.atan2(za - zb, xb - xa)


def build_trough():
    bm = bmesh.new()
    xa, za, xb, zb, length, tilt = trough_line()
    c = Vector(((xa + xb) / 2, 0.0, (za + zb) / 2))
    box(bm, c, (length, 0.14, 0.025), tilt)
    box(bm, Vector((c.x, 0.0, c.z / 2 - 0.01)), (0.07, 0.07, c.z - 0.04))   # post that carries the trough
    for sy in (-1, 1):
        box(bm, c + Vector((0, sy * 0.07, 0.0225)), (length, 0.025, 0.045), tilt)
    return put("leach_trough", bm, mat("leach_wood_mat", (0.30, 0.17, 0.08), 0.8))


def build_flow():
    """A thin trickle: a short stream out of the hole and a narrow ribbon along the trough (the flow is limited)."""
    bm = bmesh.new()
    xa, za, xb, zb, length, tilt = trough_line()
    c = Vector(((xa + xb) / 2, 0.0, (za + zb) / 2 + 0.016))
    box(bm, c, (length * 0.92, 0.026, 0.006), tilt)
    strut(bm, Vector((BARREL_X + 0.42, 0.0, STAND_H + HOLE_Z - 0.012)), Vector((BARREL_X + 0.44, 0.0, za + 0.014)), 0.007)
    return put("leach_flow", bm, mat("leach_liquid_mat", (0.30, 0.38, 0.45), 0.1, 0.0, None, 0.75))

def build_hearth():
    rnd = random.Random(11)
    bm = bmesh.new()
    for k in range(13):
        a = 2 * math.pi * k / 13
        blob(bm, Vector((HEARTH_X + 0.64 * math.cos(a), 0.64 * math.sin(a), 0.13)), rnd.uniform(0.13, 0.16), (1, 1, 0.9))
    for k in range(8):
        a = 2 * math.pi * (k + 0.5) / 8
        blob(bm, Vector((HEARTH_X + 0.62 * math.cos(a), 0.62 * math.sin(a), 0.25)), rnd.uniform(0.10, 0.13), (1, 1, 0.8))
    blob(bm, Vector((HEARTH_X, 0, 0.02)), 0.6, (1, 1, 0.08))
    return put("hearth_stones", bm, mat("leach_stone_mat", (0.42, 0.41, 0.39), 0.95))


def build_cauldron():
    bm = bmesh.new()
    prof = [(0.0, 0.0), (0.20, 0.01), (0.35, 0.08), (0.43, 0.20), (0.46, 0.25),
            (0.43, 0.25), (0.41, 0.21), (0.33, 0.10), (0.19, 0.04), (0.0, 0.03)]
    lathe(bm, prof, HEARTH_X, 0.0, CAULDRON_Z)
    return put("cauldron", bm, mat("leach_iron_mat", (0.035, 0.032, 0.03), 0.45, 0.7))


def build_liquid():
    bm = bmesh.new()
    bmesh.ops.create_circle(bm, cap_ends=True, radius=0.39, segments=32,
                            matrix=Matrix.Translation(Vector((HEARTH_X, 0, CAULDRON_Z + 0.17))))
    return put("cauldron_liquid", bm, mat("leach_liquid_mat", (0.30, 0.38, 0.45), 0.1, 0.0, None, 0.75))


def build_fire():
    bm = bmesh.new()
    for k, (dx, dy, h) in enumerate(((0, 0, 0.40), (0.2, 0.05, 0.30), (-0.18, 0.1, 0.32), (0.05, -0.2, 0.28), (-0.05, 0.22, 0.28))):
        m = Matrix.Translation(Vector((HEARTH_X + dx, dy, 0.04 + h / 2)))
        bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.09, radius2=0.0, depth=h, matrix=m)
    for k in range(3):
        log(bm, Vector((HEARTH_X, 0, 0.07)), 0.7, 0.045, k * math.pi / 3 + 0.4)
    return put("cauldron_fire", bm, mat("leach_fire_mat", (0.85, 0.22, 0.02), 0.5, 0.0, (1.0, 0.28, 0.02)))


def build_salt():
    rnd = random.Random(3)
    bm = bmesh.new()
    for _ in range(26):
        a, d = rnd.uniform(0, 2 * math.pi), 0.24 * math.sqrt(rnd.random())
        blob(bm, Vector((HEARTH_X + d * math.cos(a), d * math.sin(a), CAULDRON_Z + 0.06 + 0.04 * (1 - d / 0.24))),
             rnd.uniform(0.03, 0.055), (1, 1, 0.9))
    return put("cauldron_salt", bm, mat("leach_salt_mat", (0.93, 0.93, 0.90), 0.5))


def strut(bm, p0, p1, radius):
    d = p1 - p0
    m = Matrix.Translation((p0 + p1) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=radius, radius2=radius, depth=d.length, matrix=m)


def build_legs():
    """Three splayed iron legs that carry the cauldron above the fire, joined by a ring brace."""
    bm = bmesh.new()
    for k in range(3):
        a = math.pi / 2 + 2 * math.pi * k / 3 + math.pi / 6
        top = Vector((HEARTH_X + 0.36 * math.cos(a), 0.36 * math.sin(a), CAULDRON_Z + 0.10))
        foot = Vector((HEARTH_X + 0.50 * math.cos(a), 0.50 * math.sin(a), 0.0))
        strut(bm, top, foot, 0.03)
        blob(bm, foot + Vector((0, 0, 0.01)), 0.045, (1, 1, 0.5))
    ring_z = 0.20
    lathe(bm, [(0.425, ring_z - 0.015), (0.445, ring_z - 0.015), (0.445, ring_z + 0.015), (0.425, ring_z + 0.015)],
          HEARTH_X, 0.0, 0.0, 36)
    return put("cauldron_legs", bm, mat("leach_iron_mat", (0.035, 0.032, 0.03), 0.45, 0.7))

PARTS = ["leach_barrel", "leach_hoops", "leach_stones", "leach_stand", "leach_trough", "leach_flow", "hearth_stones",
         "cauldron", "cauldron_legs", "cauldron_liquid", "cauldron_fire", "cauldron_salt"]


def build():
    return [build_barrel(), build_hoops(), build_filter_stones(), build_stand(), build_trough(), build_flow(), build_hearth(),
            build_cauldron(), build_legs(), build_liquid(), build_fire(), build_salt()]


def export(path, names=PARTS):
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        bpy.data.objects[n].select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE")


if __name__ == "__main__":
    build()
