"""Rendert Symbole (Icons) aus den FBX-Modellen für die Bedienoberfläche.

Aufruf:
    blender -b -P tools/blender/render_icons.py -- <Modellordner> <Iconordner> [Name ...]

Ohne Namen werden alle FBX-Dateien im Modellordner gerendert. Ergebnis: 256 × 256 PNG mit
transparentem Hintergrund, Cel-Shading und Umrisslinie – passend zum Grafikstil 7.
"""
import bpy, sys, os, math
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
model_dir = os.path.abspath(args[0])
icon_dir = os.path.abspath(args[1])
names = args[2:] or sorted(f[:-4] for f in os.listdir(model_dir) if f.endswith(".fbx"))
os.makedirs(icon_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

mat = bpy.data.materials.new("Icon")
mat.use_nodes = True
nt = mat.node_tree
nt.nodes.clear()
outn = nt.nodes.new("ShaderNodeOutputMaterial")
vc = nt.nodes.new("ShaderNodeVertexColor")
vc.layer_name = "Col"
toon = nt.nodes.new("ShaderNodeBsdfToon")
toon.inputs["Size"].default_value = 0.85
toon.inputs["Smooth"].default_value = 0.15
em = nt.nodes.new("ShaderNodeEmission")
em.inputs["Strength"].default_value = 0.55
add = nt.nodes.new("ShaderNodeAddShader")
nt.links.new(vc.outputs["Color"], toon.inputs["Color"])
nt.links.new(vc.outputs["Color"], em.inputs["Color"])
nt.links.new(toon.outputs[0], add.inputs[0])
nt.links.new(em.outputs[0], add.inputs[1])
nt.links.new(add.outputs[0], outn.inputs["Surface"])

world = bpy.data.worlds.new("w")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.7, 0.8, 0.95, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8

bpy.ops.object.light_add(type="SUN", rotation=(math.radians(45), math.radians(15), math.radians(-35)))
bpy.context.object.data.energy = 3.2
bpy.ops.object.camera_add()
cam = bpy.context.object
cam.data.type = "ORTHO"
scene.camera = cam

scene.render.engine = "CYCLES"
scene.cycles.samples = 32
scene.cycles.use_denoising = False
scene.render.resolution_x = 256
scene.render.resolution_y = 256
scene.render.film_transparent = True
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.view_transform = "Standard"
scene.render.use_freestyle = True
scene.render.line_thickness_mode = "ABSOLUTE"
fs = scene.view_layers[0].freestyle_settings
ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("Linien")
if ls.linestyle is None:
    ls.linestyle = bpy.data.linestyles.new("Linienstil")
ls.linestyle.color = (0.14, 0.07, 0.03)
ls.linestyle.thickness = 3.0
ls.select_silhouette = True
ls.select_border = True
ls.select_crease = False

direction = Vector((-0.45, -1.0, 0.5)).normalized()
for name in names:
    for o in list(scene.objects):
        if o.type == "MESH":
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(model_dir, name + ".fbx"))
    objs = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        o.data.materials.clear()
        o.data.materials.append(mat)
        for corner in o.bound_box:
            p = o.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
    center = (lo + hi) / 2
    size = max((hi - lo).length, 0.01)
    cam.location = center + direction * 20
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = size * 0.92
    scene.render.filepath = os.path.join(icon_dir, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("ICON", name)
