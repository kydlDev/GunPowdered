"""Shared helpers of the GunPowdered weapon model scripts (musket, mobile cannon, explosive arrow, dynamite).
Run a model script with: blender -b --factory-startup --python make_<name>.py -- <exports dir> <photos dir>
Units: 1 Blender unit = 1 m. Weapons point along -Y (becomes +Z in Unity with the export contract of exports/README.md), up = +Z, object origin
as the script says. Rendering is Cycles on the CPU on purpose: a TestLab run may be using the GPU (PROJECT_RULES 31) and must not be disturbed.
"""
import math
import os
import sys
import time
import bmesh
import bpy
from mathutils import Matrix, Vector


def script_args():
    """The two arguments after '--': exports dir, photos dir."""
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    exports = a[0] if len(a) > 0 else os.path.join(os.path.dirname(__file__), "..", "exports")
    photos = a[1] if len(a) > 1 else os.path.join(os.path.dirname(__file__), "..", "preview")
    os.makedirs(photos, exist_ok=True)
    return exports, photos


def mat(name, rgb, rough=0.8, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


def put(name, bm, material, matrix=Matrix.Identity(4), smooth=True):
    """Turns a bmesh into an object (normals fixed, one material), placed with `matrix`."""
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = smooth
    box_uv(me, matrix)
    o.matrix_world = matrix
    return o


def box_uv(me, matrix=Matrix.Identity(4), tile_cap=0.5):
    """Cube-projection UVs (D-029): one texture tile = min(biggest size of the part, 0.5 m), so the small procedural textures repeat evenly."""
    co = [matrix @ v.co for v in me.vertices]
    if not co:
        return
    size = max(max(c[i] for c in co) - min(c[i] for c in co) for i in range(3))
    tile = max(min(size, tile_cap), 0.03)
    uv = me.uv_layers.new(name="UVMap")
    m3 = matrix.to_3x3()
    for p in me.polygons:
        n = m3 @ p.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        ui, vi = ((1, 2), (0, 2), (0, 1))[ax]
        for li in p.loop_indices:
            c = co[me.loops[li].vertex_index]
            uv.data[li].uv = (c[ui] / tile, c[vi] / tile)


def lathe(bm, profile, matrix=Matrix.Identity(4), segs=32, caps=True):
    """Solid of revolution around +Z from a (radius, z) profile; the ends close with a cap (caps=False for a profile that is a closed loop, like a ring)."""
    rings = []
    closed = (not caps) and len(profile) > 2 and tuple(profile[0]) == tuple(profile[-1])
    if closed:
        profile = profile[:-1]                                # the last point equals the first: join round instead of leaving a seam
    for r, z in profile:
        r = max(r, 1e-4)
        rings.append([bm.verts.new(matrix @ Vector((r * math.cos(2 * math.pi * i / segs), r * math.sin(2 * math.pi * i / segs), z))) for i in range(segs)])
    for k in range(len(profile) - (0 if closed else 1)):
        a, b = rings[k], rings[(k + 1) % len(rings)]
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if caps:
        for rg in (rings[0], rings[-1]):                    # caps
            bm.faces.new(rg)


def box(bm, size, centre, matrix=Matrix.Identity(4)):
    bmesh.ops.create_cube(bm, size=1.0, matrix=matrix @ Matrix.Translation(centre) @ Matrix.Diagonal((size[0], size[1], size[2], 1.0)))


def cyl(bm, r, depth, matrix=Matrix.Identity(4), segs=16, r2=None):
    """Cylinder / cone along local Z, centred."""
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r, radius2=r if r2 is None else r2, depth=depth, matrix=matrix)


def ring(bm, r_out, r_in, width, matrix=Matrix.Identity(4), segs=32):
    """Flat band (a hoop / tyre): outer radius, inner radius, width along local Z."""
    lathe(bm, [(r_in, -width / 2), (r_out, -width / 2), (r_out, width / 2), (r_in, width / 2), (r_in, -width / 2)], matrix, segs, caps=False)   # closed loop: NO caps (they were solid discs plugging the ring)


def poly_extrude(bm, pts, width, matrix=Matrix.Identity(4)):
    """A side profile (list of (y, z) points, counter-clockwise seen from +X) extruded `width` along X (centred)."""
    front = [bm.verts.new(matrix @ Vector((-width / 2, y, z))) for y, z in pts]
    back = [bm.verts.new(matrix @ Vector((width / 2, y, z))) for y, z in pts]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((front[i], front[j], back[j], back[i]))


def tube(bm, points, r, segs=8):
    """A bent tube (cord, fuse) along a list of Vector points."""
    prev = None
    for k, p in enumerate(points):
        d = (points[min(k + 1, len(points) - 1)] - points[max(k - 1, 0)]).normalized()
        side = d.cross(Vector((0, 0, 1)))
        if side.length < 1e-3:
            side = d.cross(Vector((1, 0, 0)))
        side.normalize()
        up = side.cross(d).normalized()
        cur = [bm.verts.new(p + (side * math.cos(2 * math.pi * i / segs) + up * math.sin(2 * math.pi * i / segs)) * r) for i in range(segs)]
        if prev:
            for i in range(segs):
                j = (i + 1) % segs
                bm.faces.new((prev[i], prev[j], cur[j], cur[i]))
        prev = cur
        if k == 0:
            bm.faces.new(cur)
    bm.faces.new(prev)


def strap(bm, pts, width, thick, wdir):
    """A flat strap (belt, trace) along a list of Vector points; `wdir` is the direction its width lies in (made square to the strap)."""
    prev = None
    for k, p in enumerate(pts):
        d = (pts[min(k + 1, len(pts) - 1)] - pts[max(k - 1, 0)]).normalized()
        w = (wdir - d * wdir.dot(d))
        if w.length < 1e-3:
            w = Vector((1, 0, 0)) - d * d.x
        w.normalize()
        n = d.cross(w).normalized()
        cur = [bm.verts.new(p + w * sx * width / 2 + n * sy * thick / 2) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        if prev:
            for i in range(4):
                j = (i + 1) % 4
                bm.faces.new((prev[i], prev[j], cur[j], cur[i]))
        else:
            bm.faces.new(cur)
        prev = cur
    bm.faces.new(prev)


def spline(pts, per=6):
    """Catmull-Rom curve through the points (smooth belts and traces); returns a denser list of Vectors."""
    P = [pts[0]] + list(pts) + [pts[-1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for s in range(per):
            t = s / per
            out.append(0.5 * ((2 * p1) + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (3 * p1 - p0 - 3 * p2 + p3) * t ** 3))
    out.append(P[-2])
    return out


def empty(name, loc, rot=(0, 0, 0)):
    o = bpy.data.objects.new(name, None)
    o.empty_display_type = "PLAIN_AXES"
    o.empty_display_size = 0.05
    o.location = loc
    o.rotation_euler = rot
    bpy.context.scene.collection.objects.link(o)
    return o


def tris(names):
    return sum(sum(len(p.vertices) - 2 for p in bpy.data.objects[n].data.polygons) for n in names if bpy.data.objects[n].type == "MESH")


def export(path, names):
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        bpy.data.objects[n].select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z",
                             axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", object_types={"MESH", "EMPTY"})
    return path


def stage(ground_size=(6.0, 5.0)):
    """Empty scene: sun, sky, grass ground, CPU Cycles."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 40
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = 1400, 900
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.58, 0.64, 0.72, 1.0)
    sc.world = world
    g = bmesh.new()
    box(g, (ground_size[0], ground_size[1], 0.02), Vector((0, 0, -0.01)))
    put("ground", g, mat("ground", (0.36, 0.46, 0.25), 0.95), smooth=False)
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 3.5
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = (math.radians(55), math.radians(10), math.radians(-30))
    sc.collection.objects.link(so)
    cam = bpy.data.cameras.new("cam")
    co = bpy.data.objects.new("cam", cam)
    sc.collection.objects.link(co)
    sc.camera = co
    return sc, co


def shoot(sc, co, path, cam_loc, target, lens=50):
    co.data.type = "PERSP"
    co.data.lens = lens
    co.location = Vector(cam_loc)
    co.rotation_euler = (Vector(target) - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


def stamp():
    return time.strftime("%Y%m%d-%H%M")


def views(photos, label, centre, dist, height=0.0):
    """Side (+X side looking at the model) and three-quarter renders; returns the two paths."""
    sc, co = bpy.context.scene, bpy.context.scene.camera
    c = Vector(centre)
    p1 = shoot(sc, co, os.path.join(photos, "%s_%s-side.png" % (stamp(), label)), c + Vector((dist, 0.0, height)), c)
    p2 = shoot(sc, co, os.path.join(photos, "%s_%s-3q.png" % (stamp(), label)), c + Vector((dist * 0.7, -dist * 0.8, height + dist * 0.35)), c)
    return p1, p2


def shift(names, dz):
    """Moves objects up / down (a preview lifts the model off the ground plane; the export lowers it again)."""
    for n in names:
        bpy.data.objects[n].location.z += dz
