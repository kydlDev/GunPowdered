"""Flametal Cannon Ball (D-145, user 2026-11-21): the Mobile Cannon's first ball. Same 10 cm cast ball as the Iron Cannon Ball (make_cannon.py build_ball:
seam ring + sprue nub, origin = bottom centre) in flametal: dark metal with thin glowing-orange veins, like the game's flametal and the mod's
Flametal Heater / Drying Oven (Unity material ext_flametal).
Run: blender -b --factory-startup --python make_flametal_cannon_ball.py -- <preview.png>   Units: 1 Blender unit = 1 m.
Writes exports/FlametalCannonBall.fbx (mesh flametal_cannon_ball, material ext_flametal) and the icon FlametalCannonBall.png (256 px, transparent)
into GunPowdered-Unity/Assets/GunPowdered_Art/Icons; the preview PNG goes where the argument says (Photos\\GunPowdered, PROJECT_RULES 20).
"""
import math
import os
import sys
import bmesh
import bpy
from mathutils import Matrix, Vector

_HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.normpath(os.path.join(_HERE, "..", "exports"))
ICON_DIR = os.path.normpath(os.path.join(_HERE, "..", "..", "GunPowdered-Unity", "Assets", "GunPowdered_Art", "Icons"))
PREVIEW = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.join(OUT_DIR, "flametal-cannon-ball-preview.png")
SCALE = 2.5                  # as make_cannon.py: built in real-size numbers, scaled up 2.5 times
BALL_RADIUS = 0.05           # 10 cm across in game, like the iron ball


def flametal_material(name):
    """Dark grey metal; a noise texture cut into thin bands gives orange glowing veins (emission), as the Unity ext_flametal texture does."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.09, 0.085, 0.085, 1.0)
    b.inputs["Roughness"].default_value = 0.45
    b.inputs["Metallic"].default_value = 0.9
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 9.0
    noise.inputs["Detail"].default_value = 6.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")          # only a thin band of the noise glows: the veins
    ramp.color_ramp.elements[0].position = 0.47
    ramp.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
    ramp.color_ramp.elements[1].position = 0.50
    ramp.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
    e = ramp.color_ramp.elements.new(0.53)
    e.color = (0.0, 0.0, 0.0, 1.0)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    b.inputs["Emission Color"].default_value = (1.0, 0.42, 0.08, 1.0)
    nt.links.new(ramp.outputs["Color"], b.inputs["Emission Strength"])
    return m


def lathe(bm, profile, matrix, segs=40, close=False):
    """Revolves a (radius, height) profile around Z, then moves the new vertices by matrix (make_cannon.py)."""
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


def box_uv(bm, tile):
    uv = bm.loops.layers.uv.new("UVMap")
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        u_i, v_i = [(1, 2), (0, 2), (0, 1)][ax]
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co[u_i] / tile, loop.vert.co[v_i] / tile)


def build_ball(material):
    """The iron ball's shape (make_cannon.py build_ball), origin at the bottom."""
    r = BALL_RADIUS / SCALE
    bm = bmesh.new()
    arc = [(r * math.sin(math.pi * k / 18), -r * math.cos(math.pi * k / 18)) for k in range(19)]
    lathe(bm, arc, Matrix.Translation((0.0, 0.0, r)))
    lathe(bm, [(r + 0.0012, -0.0016), (r + 0.0012, 0.0016), (r - 0.004, 0.0016), (r - 0.004, -0.0016)],
          Matrix.Translation((0.0, 0.0, r)) @ Matrix.Rotation(math.radians(90), 4, "Y"), 36, close=True)                         # the casting seam
    lathe(bm, [(0.0, 0.0), (0.005, 0.0), (0.0035, 0.004), (0.0, 0.004)], Matrix.Translation((0.0, 0.0, 2 * r - 0.0008)), 14)   # the sprue nub
    bmesh.ops.scale(bm, vec=(SCALE, SCALE, SCALE), verts=bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    box_uv(bm, 2 * BALL_RADIUS)
    me = bpy.data.meshes.new("flametal_cannon_ball")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("flametal_cannon_ball", me)
    bpy.context.scene.collection.objects.link(obj)
    me.materials.append(material)
    for p in me.polygons:
        p.use_smooth = True
    return obj


def export(path, obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z",
                             axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE", object_types={"MESH"})
    return path


def light_and_world(strength=3.5):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 64
    sc.cycles.use_denoising = False
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.6, 0.65, 0.7, 1.0)
    sc.world = world
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = strength
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    sc.collection.objects.link(so)


def render_view(obj, path, view, size, transparent):
    """Orthographic render of the ball seen from 'view' relative to its middle (make_cannon.py icon)."""
    sc = bpy.context.scene
    bpy.context.view_layer.update()
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    centre = (lo + hi) / 2
    cd = bpy.data.cameras.new("cam")
    cd.type = "ORTHO"
    cd.ortho_scale = max(hi - lo) * 1.25
    cam = bpy.data.objects.new("cam", cd)
    sc.collection.objects.link(cam)
    cam.location = centre + Vector(view)
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    sc.render.resolution_x = size
    sc.render.resolution_y = size
    sc.render.film_transparent = transparent
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
ball = build_ball(flametal_material("ext_flametal"))
print("FLAMETAL BALL TRIANGLES", sum(len(p.vertices) - 2 for p in ball.data.polygons))
print("FLAMETAL BALL EXPORT", export(os.path.join(OUT_DIR, "FlametalCannonBall.fbx"), ball))
light_and_world()
render_view(ball, os.path.join(ICON_DIR, "FlametalCannonBall.png"), (0.0, -1.0, 0.8), 256, True)
render_view(ball, PREVIEW, (0.0, -1.0, 0.8), 768, False)
print("FLAMETAL BALL ICON + PREVIEW DONE")
