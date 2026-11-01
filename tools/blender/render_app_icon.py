"""Rendert das App-Icon (1024 × 1024): Kuh im grünen Traktorstrahl unter einem Ufo, auf Himmel-Hintergrund.

Aufruf:
    blender -b -P tools/blender/render_app_icon.py -- <Modellordner> <Zieldatei.png>
"""
import bpy, sys, os, math
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
model_dir = os.path.abspath(args[0])
out = os.path.abspath(args[1])
os.makedirs(os.path.dirname(out), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

def toon_material(name, emission=0.55):
    mat = bpy.data.materials.new(name)
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
    em.inputs["Strength"].default_value = emission
    add = nt.nodes.new("ShaderNodeAddShader")
    nt.links.new(vc.outputs["Color"], toon.inputs["Color"])
    nt.links.new(vc.outputs["Color"], em.inputs["Color"])
    nt.links.new(toon.outputs[0], add.inputs[0])
    nt.links.new(em.outputs[0], add.inputs[1])
    nt.links.new(add.outputs[0], outn.inputs["Surface"])
    return mat

mat = toon_material("Toon")

outline = bpy.data.collections.new("Umriss")
scene.collection.children.link(outline)

def load(name, loc, scale, rot_z=0.0, tilt=(0, 0)):
    """Modell laden und über ein Elternobjekt drehen (die FBX-Drehung bleibt erhalten)."""
    bpy.ops.import_scene.fbx(filepath=os.path.join(model_dir, name + ".fbx"))
    objs = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    pivot = bpy.data.objects.new(name + "_Pivot", None)
    scene.collection.objects.link(pivot)
    for o in objs:
        o.data.materials.clear()
        o.data.materials.append(mat)
        for c in o.users_collection:
            c.objects.unlink(o)
        outline.objects.link(o)
        o.parent = pivot
    pivot.scale = (scale, scale, scale)
    pivot.rotation_euler = (math.radians(tilt[0]), math.radians(tilt[1]), math.radians(rot_z))
    pivot.location = loc
    return objs

# Ufo oben, Kuh im Strahl darunter
load("Ufo", (0, 0, 2.45), 1.35, 0, (0, 0))
load("Kuh", (0, 0, -0.15), 1.5, 25, (12, -10))

# Grüner, leuchtender Traktorstrahl
bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=1.25, radius2=0.5, depth=2.0, location=(0, 0, 1.05))
beam = bpy.context.object
bm = bpy.data.materials.new("Strahl")
bm.use_nodes = True
nt = bm.node_tree
nt.nodes.clear()
o = nt.nodes.new("ShaderNodeOutputMaterial")
e = nt.nodes.new("ShaderNodeEmission")
e.inputs["Color"].default_value = (0.55, 1.0, 0.45, 1)
e.inputs["Strength"].default_value = 1.2
t = nt.nodes.new("ShaderNodeBsdfTransparent")
mix = nt.nodes.new("ShaderNodeMixShader")
mix.inputs["Fac"].default_value = 0.3
nt.links.new(t.outputs[0], mix.inputs[1])
nt.links.new(e.outputs[0], mix.inputs[2])
nt.links.new(mix.outputs[0], o.inputs["Surface"])
beam.data.materials.append(bm)

# Hintergrund: Himmel-Verlauf über einer Wiese
world = bpy.data.worlds.new("w")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.35, 0.65, 1.0, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
bpy.ops.object.light_add(type="SUN", rotation=(math.radians(45), math.radians(15), math.radians(-35)))
bpy.context.object.data.energy = 3.2
bpy.ops.object.camera_add()
cam = bpy.context.object
cam.data.type = "ORTHO"
cam.data.ortho_scale = 4.2
target = Vector((0, 0, 1.05))
cam.location = target + Vector((-0.35, -1.0, 0.45)).normalized() * 20
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = cam

scene.render.engine = "CYCLES"
scene.cycles.samples = 48
scene.cycles.use_denoising = False
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.image_settings.file_format = "PNG"
scene.view_settings.view_transform = "Standard"
scene.render.use_freestyle = True
scene.render.line_thickness_mode = "ABSOLUTE"
fs = scene.view_layers[0].freestyle_settings
ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("Linien")
if ls.linestyle is None:
    ls.linestyle = bpy.data.linestyles.new("Linienstil")
ls.linestyle.color = (0.14, 0.07, 0.03)
ls.linestyle.thickness = 6.0
ls.select_silhouette = True
ls.select_border = True
ls.select_crease = False
# Umrisslinien nur für die Figuren, nicht für den Strahl
ls.select_by_collection = True
ls.collection = outline
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("APPICON", out)
