"""Builds the GunPowdered bucket: wood staves, two bronze hoops, a leather rope handle.
Two staves (opposite sides) are extended above the rim into rounded ears with a hole; the ear is part of the
stave (same wood, same wall thickness), and the rope loops through the hole.
Run inside Blender (or: blender -b GunPowdered.blend --python scripts/make_bucket.py).
Units: 1 Blender unit = 1 m. Objects are named snake_case; exports are PascalCase.
Creates collection "Model" objects: bucket_wood, bucket_bronze, bucket_leather, bucket_water.
"""
import math
import bmesh
import bpy

H = 0.28          # body height (m)
R_BOT = 0.125     # outer radius at the bottom
R_TOP = 0.160     # outer radius at the rim
STAVES = 14
THICK = 0.014     # stave thickness
GAP = 0.0035      # visible groove between staves
HOOPS = (0.055, 0.215)   # hoop heights above the bottom
EAR_UT = 0.017    # half width of the ear at its rounded top (the base is a full stave wide)
EAR_ARC_Z = 0.028 # height above the rim where the round top starts
EAR_HOLE_R = 0.0065
LOOP_R = 0.016    # radius of the rope loop through each ear hole
HOLE_Z = H + EAR_ARC_Z


def radius_at(z):
    return R_BOT + (R_TOP - R_BOT) * (z / H)


def hole_x():
    """Radial position of the ear hole: the middle of the wall."""
    return radius_at(HOLE_Z) - THICK / 2


def material(name, rgb, rough=0.7, metal=0.0, alpha=1.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Alpha"].default_value = alpha
    return mat


def new_object(name, mesh):
    for old in [o for o in bpy.data.objects if o.name == name]:
        bpy.data.objects.remove(old, do_unlink=True)
    obj = bpy.data.objects.new(name, mesh)
    coll = bpy.data.collections.get("Model") or bpy.data.collections.new("Model")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    coll.objects.link(obj)
    return obj


def wall_point(bm, centre_angle, u, zz, inner):
    """A point on the (outer or inner) wall: u = distance along the wall from the stave centre, zz = height above the rim."""
    ro = radius_at(H + zz) - (THICK if inner else 0.0)
    a = centre_angle + u / R_TOP
    return bm.verts.new((ro * math.cos(a), ro * math.sin(a), H + zz))


def ear_outline(ub):
    """(u, zz) points of the ear, from the left base corner over the round top to the right base corner."""
    pts = [(-ub, 0.0), (-ub, 0.010), (-EAR_UT, EAR_ARC_Z)]
    arc = 9
    for k in range(1, arc):
        t = math.pi * (1 - k / arc)
        pts.append((EAR_UT * math.cos(t), EAR_ARC_Z + EAR_UT * math.sin(t)))
    pts += [(EAR_UT, EAR_ARC_Z), (ub, 0.010), (ub, 0.0)]
    return pts


def add_ear(bm, centre_angle, base_out, base_in, ub):
    """Extends a stave upward: outer and inner surface (triangle-filled around the hole) and the edge faces."""
    pts = ear_outline(ub)
    outs, ins = [base_out[0]], [base_in[0]]
    for u, zz in pts[1:-1]:
        outs.append(wall_point(bm, centre_angle, u, zz, False))
        ins.append(wall_point(bm, centre_angle, u, zz, True))
    outs.append(base_out[1])
    ins.append(base_in[1])
    n = len(outs)
    holes = []
    for inner in (False, True):
        ring = [wall_point(bm, centre_angle, EAR_HOLE_R * math.cos(2 * math.pi * k / 12),
                           EAR_ARC_Z + EAR_HOLE_R * math.sin(2 * math.pi * k / 12), inner) for k in range(12)]
        holes.append(ring)
    for k in range(n - 1):      # rim of the ear (the edge faces); the base edge stays joined to the stave
        bm.faces.new((outs[k], outs[k + 1], ins[k + 1], ins[k]))
    for k in range(12):         # the wall of the hole
        j = (k + 1) % 12
        bm.faces.new((holes[0][k], holes[0][j], holes[1][j], holes[1][k]))
    for loop, ring in ((outs, holes[0]), (ins, holes[1])):
        edges = []
        for k in range(len(loop)):
            a, b = loop[k], loop[(k + 1) % len(loop)]
            edges.append(bm.edges.get((a, b)) or bm.edges.new((a, b)))
        for k in range(12):
            a, b = ring[k], ring[(k + 1) % 12]
            edges.append(bm.edges.get((a, b)) or bm.edges.new((a, b)))
        bmesh.ops.triangle_fill(bm, edges=edges, use_beauty=True)


def build_staves():
    bm = bmesh.new()
    step = 2 * math.pi / STAVES
    half = step / 2 - GAP / R_TOP
    ub = half * R_TOP
    for i in range(STAVES):
        c = i * step
        a0, a1 = c - half, c + half
        verts = []
        for z in (0.0, H):
            for ro, a in ((radius_at(z), a0), (radius_at(z), a1),
                          (radius_at(z) - THICK, a1), (radius_at(z) - THICK, a0)):
                verts.append(bm.verts.new((ro * math.cos(a), ro * math.sin(a), z)))
        has_ear = i in (0, STAVES // 2)
        faces = [(0, 1, 2, 3), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
        if not has_ear:
            faces.append((7, 6, 5, 4))
        for f in faces:
            bm.faces.new([verts[k] for k in f])
        if has_ear:
            add_ear(bm, c, (verts[4], verts[5]), (verts[7], verts[6]), ub)
    bottom = bmesh.ops.create_circle(bm, cap_ends=True, radius=R_BOT - THICK, segments=STAVES * 2)
    for v in bottom["verts"]:
        v.co.z += 0.012
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("bucket_wood")
    bm.to_mesh(mesh)
    bm.free()
    return mesh


def ring(bm, z, r_out, width, thick, segs=48):
    """A flat band (hoop) around the body at height z."""
    top, bot = [], []
    for zz, store in ((z + width / 2, top), (z - width / 2, bot)):
        for ro in (r_out, r_out - thick):
            row = []
            for i in range(segs):
                a = 2 * math.pi * i / segs
                row.append(bm.verts.new((ro * math.cos(a), ro * math.sin(a), zz)))
            store.append(row)
    for i in range(segs):
        j = (i + 1) % segs
        bm.faces.new((top[0][i], top[0][j], bot[0][j], bot[0][i]))      # outside
        bm.faces.new((top[1][j], top[1][i], bot[1][i], bot[1][j]))      # inside
        bm.faces.new((top[0][j], top[0][i], top[1][i], top[1][j]))      # upper edge
        bm.faces.new((bot[0][i], bot[0][j], bot[1][j], bot[1][i]))      # lower edge


def build_hoops():
    bm = bmesh.new()
    for z in HOOPS:
        ring(bm, z, radius_at(z) + 0.006, 0.022, 0.008)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("bucket_bronze")
    bm.to_mesh(mesh)
    bm.free()
    return mesh


def build_handle():
    """Rope arch between the two ears, plus a small loop through each ear hole (the hole axis is X)."""
    curve = bpy.data.curves.new("bucket_leather_curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.006
    curve.bevel_resolution = 3
    curve.use_fill_caps = True
    hx = hole_x()
    top = HOLE_Z + 2 * LOOP_R
    spline = curve.splines.new("POLY")
    pts = 25
    spline.points.add(pts - 1)
    for i in range(pts):
        a = math.pi * i / (pts - 1)
        spline.points[i].co = (hx * math.cos(a), 0.0, top + 0.11 * math.sin(a), 1.0)
    for sign in (1, -1):
        loop = curve.splines.new("POLY")
        loop.use_cyclic_u = True
        segs = 16
        loop.points.add(segs - 1)
        for i in range(segs):
            t = 2 * math.pi * i / segs
            loop.points[i].co = (sign * hx + LOOP_R * math.sin(t), 0.0, HOLE_Z + LOOP_R - LOOP_R * math.cos(t), 1.0)
    return new_object("bucket_leather", curve)


def build_water():
    bm = bmesh.new()
    bmesh.ops.create_circle(bm, cap_ends=True, radius=radius_at(H * 0.86) - THICK - 0.002, segments=STAVES * 2)
    for v in bm.verts:
        v.co.z = H * 0.86
    mesh = bpy.data.meshes.new("bucket_water")
    bm.to_mesh(mesh)
    bm.free()
    return mesh


def build():
    wood = new_object("bucket_wood", build_staves())
    wood.data.materials.append(material("bucket_wood_mat", (0.30, 0.17, 0.08), 0.8))
    bronze = new_object("bucket_bronze", build_hoops())
    bronze.data.materials.append(material("bucket_bronze_mat", (0.035, 0.032, 0.03), 0.45, 0.7))
    rope = build_handle()
    rope.data.materials.append(material("bucket_leather_mat", (0.10, 0.05, 0.025), 0.9))
    water = new_object("bucket_water", build_water())
    water.data.materials.append(material("bucket_water_mat", (0.10, 0.35, 0.60), 0.1, 0.0, 0.7))
    return wood, bronze, rope, water


def export(path, names):
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        bpy.data.objects[n].select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE")


if __name__ == "__main__":
    build()
