"""Probeszene im glatten Cartoon-3D-Stil (Richtung Bloons TD6).

Aufruf: blender -b -P btd_scene.py -- <ausgabe.png> [samples]
"""
import bpy, sys, math, random
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out = args[0]
samples = int(args[1]) if len(args) > 1 else 160
random.seed(5)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------- Materialien

def new_mat(name):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    p = nt.nodes["Principled BSDF"]
    return m, nt, p


def basic(color, rough=0.42, coat=0.35, emit=0.0, name="m"):
    m, nt, p = new_mat(name)
    p.inputs["Base Color"].default_value = color + (1,)
    p.inputs["Roughness"].default_value = rough
    p.inputs["Coat Weight"].default_value = coat
    p.inputs["Coat Roughness"].default_value = 0.25
    if emit:
        p.inputs["Emission Color"].default_value = color + (1,)
        p.inputs["Emission Strength"].default_value = emit
    return m


def textured(c1, c2, scale, bump=0.25, rough=0.8, detail=6.0, ramp=(0.35, 0.65), name="t"):
    """Zwei Farben über Rauschen gemischt, mit leichter Oberflächenstruktur."""
    m, nt, p = new_mat(name)
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = detail
    cr = nt.nodes.new("ShaderNodeValToRGB")
    cr.color_ramp.elements[0].position = ramp[0]
    cr.color_ramp.elements[0].color = c1 + (1,)
    cr.color_ramp.elements[1].position = ramp[1]
    cr.color_ramp.elements[1].color = c2 + (1,)
    nt.links.new(noise.outputs["Fac"], cr.inputs["Fac"])
    nt.links.new(cr.outputs["Color"], p.inputs["Base Color"])
    b = nt.nodes.new("ShaderNodeBump")
    b.inputs["Strength"].default_value = bump
    nt.links.new(noise.outputs["Fac"], b.inputs["Height"])
    nt.links.new(b.outputs["Normal"], p.inputs["Normal"])
    p.inputs["Roughness"].default_value = rough
    return m


def cow_skin():
    """Weiß mit schwarzen Flecken (Flecken kleben am Objekt)."""
    m, nt, p = new_mat("Kuhfell")
    tc = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.2
    noise.inputs["Detail"].default_value = 1.0
    noise.inputs["Distortion"].default_value = 0.4
    nt.links.new(tc.outputs["Object"], noise.inputs["Vector"])
    cr = nt.nodes.new("ShaderNodeValToRGB")
    cr.color_ramp.interpolation = "CONSTANT"
    cr.color_ramp.elements[0].color = (0.97, 0.96, 0.93, 1)
    cr.color_ramp.elements[1].position = 0.56
    cr.color_ramp.elements[1].color = (0.05, 0.05, 0.06, 1)
    nt.links.new(noise.outputs["Fac"], cr.inputs["Fac"])
    nt.links.new(cr.outputs["Color"], p.inputs["Base Color"])
    p.inputs["Roughness"].default_value = 0.5
    p.inputs["Coat Weight"].default_value = 0.2
    return m


WHITE = basic((0.97, 0.97, 0.95), rough=0.3)
BLACK = basic((0.02, 0.02, 0.025), rough=0.25, coat=0.8)
PINK = basic((1.0, 0.55, 0.62), rough=0.45)
HORN = basic((0.95, 0.88, 0.7), rough=0.4)
HOOF = basic((0.25, 0.18, 0.14), rough=0.5)
ALIEN = basic((0.3, 0.68, 0.22), rough=0.35, coat=0.5)
ALIEN_DARK = basic((0.12, 0.3, 0.12), rough=0.4)
IRIS = basic((0.15, 0.55, 0.95), rough=0.2, coat=0.8)
SHINE = basic((1, 1, 1), emit=3.0)
YELLOW_GLOW = basic((1.0, 0.85, 0.15), emit=2.5)
SKIN = basic((1.0, 0.76, 0.6), rough=0.5, coat=0.1)
CHEEK = basic((1.0, 0.55, 0.5), rough=0.6, coat=0.0)
DENIM = textured((0.15, 0.3, 0.68), (0.22, 0.4, 0.8), 40, bump=0.15, rough=0.7, name="Jeans")
SHIRT = textured((0.78, 0.18, 0.12), (0.9, 0.28, 0.18), 30, bump=0.1, rough=0.8, name="Hemd")
STRAW = textured((0.92, 0.7, 0.28), (1.0, 0.82, 0.4), 60, bump=0.4, rough=0.8, name="Stroh")
HATBAND = basic((0.75, 0.15, 0.12), rough=0.5)
MUSTACHE = basic((0.45, 0.28, 0.15), rough=0.7, coat=0.0)
WOOD = textured((0.42, 0.25, 0.12), (0.58, 0.38, 0.2), 12, bump=0.3, rough=0.7, name="Holz")
METAL = basic((0.45, 0.48, 0.55), rough=0.25, coat=0.9)
GLASS = basic((0.45, 0.85, 1.0), rough=0.05, coat=1.0)
STONE = textured((0.5, 0.5, 0.52), (0.7, 0.68, 0.64), 4, bump=0.2, rough=0.8, detail=3.0, name="Stein")
GRASS = textured((0.22, 0.5, 0.08), (0.45, 0.68, 0.12), 2.5, bump=0.15, rough=0.85, detail=3.0, ramp=(0.3, 0.75), name="Gras")
PATH = textured((0.55, 0.4, 0.24), (0.72, 0.56, 0.36), 2.5, bump=0.15, rough=0.9, detail=3.0, name="Weg")
SOIL = textured((0.35, 0.22, 0.12), (0.5, 0.33, 0.18), 6, bump=0.6, rough=0.9, name="Erde")
LEAF = textured((0.1, 0.32, 0.07), (0.24, 0.5, 0.11), 3, bump=0.12, rough=0.6, detail=2.0, name="Blätter")
FLOWER_COLORS = [basic(c, rough=0.4) for c in [(1, 0.95, 0.3), (1, 0.45, 0.6), (1, 1, 1), (0.6, 0.5, 1.0)]]

# ---------------------------------------------------------------- Formen

def smooth_obj(o, mat, subsurf=2, bevel=0.0):
    if bevel > 0:
        b = o.modifiers.new("Bevel", "BEVEL")
        b.width = bevel
        b.segments = 3
    if subsurf:
        s = o.modifiers.new("Sub", "SUBSURF")
        s.levels = subsurf
        s.render_levels = subsurf
    o.data.materials.append(mat)
    for poly in o.data.polygons:
        poly.use_smooth = True
    return o


def parent_to(o, parent):
    if parent is not None:
        o.parent = parent
    return o


def group(name, loc, rot_z=0.0, scale=1.0):
    e = bpy.data.objects.new(name, None)
    scene.collection.objects.link(e)
    e.location = loc
    e.rotation_euler = (0, 0, rot_z)
    e.scale = (scale, scale, scale)
    return e


def ball(loc, size, mat, parent=None, rot=(0, 0, 0), subsurf=1):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    return parent_to(smooth_obj(o, mat, subsurf), parent)


def soft_box(loc, size, mat, parent=None, round_=0.35, rot=(0, 0, 0)):
    """Würfel mit stark abgerundeten Kanten – ergibt weiche, organische Körper."""
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(scale=True)
    return parent_to(smooth_obj(o, mat, 2, bevel=min(size) * round_), parent)


def tube(loc, r, h, mat, parent=None, rot=(0, 0, 0), r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=r, depth=h, location=loc, rotation=rot)
        o = bpy.context.object
        return parent_to(smooth_obj(o, mat, 1, bevel=min(r * 0.4, h * 0.2)), parent)
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=r, radius2=r2, depth=h, location=loc, rotation=rot)
    return parent_to(smooth_obj(bpy.context.object, mat, 1), parent)


def eye(parent, loc, size, iris=IRIS, lid=None, lid_tilt=0.0, lid_cover=0.45, brow=None, brow_tilt=0.0, glow=False):
    """Cartoon-Auge mit Lid (für entschlossene oder fiese Blicke) und Augenbraue. Blickrichtung -y.
    lid_tilt > 0 neigt das Lid zur Nase hin nach unten (böse/entschlossen)."""
    x, y, z = loc
    ball((x, y, z), (size, size * 0.8, size * 1.0), WHITE if not glow else iris, parent)
    if not glow:
        ball((x, y - size * 0.36, z - size * 0.05), (size * 0.62, size * 0.2, size * 0.66), iris, parent)
        ball((x, y - size * 0.44, z - size * 0.05), (size * 0.32, size * 0.12, size * 0.36), BLACK, parent)
        ball((x + size * 0.12, y - size * 0.5, z + size * 0.1), (size * 0.12, size * 0.05, size * 0.12), SHINE, parent)
    if lid is not None:
        ball((x, y - size * 0.04, z + size * (0.62 - lid_cover)), (size * 1.18, size * 0.95, size * 0.62), lid, parent,
             rot=(0, lid_tilt, 0))
    if brow is not None:
        soft_box((x, y - size * 0.3, z + size * 0.62), (size * 1.25, size * 0.25, size * 0.24), brow, parent, round_=0.45,
                 rot=(0, brow_tilt, 0))


# ---------------------------------------------------------------- Boden

bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=1, depth=1)
island = bpy.context.object
island.scale = (4.4, 3.0, 0.45)
island.location = (0, 0, -0.45)
smooth_obj(island, SOIL, 2, bevel=0.15)

bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=1, depth=1)
top = bpy.context.object
top.scale = (4.38, 2.98, 0.08)
top.location = (0, 0, 0.0)
smooth_obj(top, GRASS, 2, bevel=0.05)

# Weg: Erdband mit einzelnen Steinplatten (wie in Bloons TD6)
DIRT = textured((0.36, 0.24, 0.14), (0.46, 0.32, 0.19), 3, bump=0.1, rough=0.95, detail=2.0, name="Wegerde")
TILE_MATS = [basic(c, rough=0.7, coat=0.0) for c in [(0.85, 0.5, 0.3), (0.8, 0.45, 0.26), (0.9, 0.58, 0.35)]]
def path_y(x):
    return -0.35 + math.sin((x + 4.2) / 8.4 * math.pi * 1.4) * 0.45
for i in range(30):
    x = -4.3 + 8.6 * i / 29
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.6, depth=0.04, location=(x, path_y(x), 0.055))
    smooth_obj(bpy.context.object, DIRT, 1)
x = -4.1
while x < 4.1:
    for side in (-1, 1):
        w = random.uniform(0.32, 0.46)
        yy = path_y(x) + side * random.uniform(0.17, 0.27)
        slope = math.atan2(path_y(x + 0.05) - path_y(x - 0.05), 0.1)
        soft_box((x, yy, 0.1), (w, random.uniform(0.22, 0.3), 0.07), random.choice(TILE_MATS), round_=0.45,
                 rot=(0, 0, slope + random.uniform(-0.25, 0.25)))
    x += random.uniform(0.38, 0.5)
for i in range(14):
    x = random.uniform(-3.8, 3.8)
    s_ = random.uniform(0.05, 0.09)
    ball((x, path_y(x) + random.uniform(-0.08, 0.08), 0.09), (s_ * 1.4, s_, s_ * 0.6), STONE)

# Blumen und Grasbüschel
for i in range(60):
    while True:
        x, y = random.uniform(-4.0, 4.0), random.uniform(-2.7, 2.7)
        if (x / 4.2) ** 2 + (y / 2.85) ** 2 > 0.92:
            continue
        path_y = -0.35 + math.sin((x + 4.2) / 8.4 * math.pi * 1.4) * 0.45
        if abs(y - path_y) > 0.8:
            break
    if i % 3 == 0:
        mat = random.choice(FLOWER_COLORS)
        tube((x, y, 0.14), 0.012, 0.16, LEAF)
        ball((x, y, 0.24), (0.09, 0.09, 0.05), mat)
        ball((x, y, 0.26), (0.035, 0.035, 0.03), YELLOW_GLOW if mat is not FLOWER_COLORS[0] else WHITE)
    elif i % 7 == 1:
        bs = random.uniform(0.28, 0.42)
        ball((x, y, bs * 0.3), (bs, bs, bs * 0.75), LEAF, subsurf=2)
        ball((x + bs * 0.35, y + 0.05, bs * 0.25), (bs * 0.7, bs * 0.7, bs * 0.55), LEAF, subsurf=2)

# Holzzaun hinten
for i in range(8):
    x = -3.2 + i * 0.85
    y = 1.85 + 0.25 * math.sin(i * 0.7)
    soft_box((x, y, 0.38), (0.16, 0.16, 0.7), WOOD, round_=0.25)
for z in (0.28, 0.52):
    for i in range(7):
        x0 = -3.2 + i * 0.85
        x1 = x0 + 0.85
        y0 = 1.85 + 0.25 * math.sin(i * 0.7)
        y1 = 1.85 + 0.25 * math.sin((i + 1) * 0.7)
        ang = math.atan2(y1 - y0, x1 - x0)
        soft_box(((x0 + x1) / 2, (y0 + y1) / 2, z), (0.9, 0.07, 0.1), WOOD, round_=0.3, rot=(0, 0, ang))

# Baum
tube((3.0, 1.5, 0.55), 0.17, 1.1, WOOD)
for (dx, dy, dz, s) in [(0, 0, 1.35, 1.1), (-0.35, -0.15, 1.15, 0.75), (0.35, 0.1, 1.2, 0.8), (0.05, 0.3, 1.7, 0.7)]:
    ball((3.0 + dx, 1.5 + dy, dz), (s, s, s * 0.92), LEAF, subsurf=2)

# Heuballen
for (x, y) in [(-2.9, 1.0), (-2.35, 1.15)]:
    tube((x, y, 0.3), 0.3, 0.48, STRAW, rot=(math.radians(90), 0, math.radians(15)))


# ---------------------------------------------------------------- Kuh: gelassen, mit Nasenring
cow = group("Kuh", (1.35, 0.95, 0.0), rot_z=math.radians(-25))
soft_box((0, 0.15, 0.7), (0.66, 1.08, 0.6), cow_skin(), cow, round_=0.4)
soft_box((0, -0.55, 0.9), (0.46, 0.46, 0.44), WHITE, cow, round_=0.42)
soft_box((0, -0.8, 0.76), (0.42, 0.2, 0.26), PINK, cow, round_=0.45)
ball((-0.09, -0.9, 0.78), (0.06, 0.03, 0.07), BLACK, cow)
ball((0.09, -0.9, 0.78), (0.06, 0.03, 0.07), BLACK, cow)
bpy.ops.mesh.primitive_torus_add(major_radius=0.07, minor_radius=0.014, location=(0, -0.92, 0.7), rotation=(math.radians(90), 0, 0))
parent_to(smooth_obj(bpy.context.object, basic((0.85, 0.7, 0.3), rough=0.2, coat=1.0), 1), cow)
eye(cow, (-0.12, -0.76, 1.01), 0.16, iris=basic((0.3, 0.2, 0.12), rough=0.2, coat=0.8), lid=WHITE, lid_tilt=math.radians(-12), lid_cover=0.45)
eye(cow, (0.12, -0.76, 1.01), 0.16, iris=basic((0.3, 0.2, 0.12), rough=0.2, coat=0.8), lid=WHITE, lid_tilt=0.0, lid_cover=0.5)
ball((-0.3, -0.5, 1.02), (0.2, 0.08, 0.1), WHITE, cow, rot=(0, math.radians(-20), 0))
ball((0.3, -0.5, 1.02), (0.2, 0.08, 0.1), WHITE, cow, rot=(0, math.radians(20), 0))
tube((-0.17, -0.5, 1.16), 0.05, 0.22, HORN, cow, rot=(0, math.radians(-35), 0), r2=0.01)
tube((0.17, -0.5, 1.16), 0.05, 0.22, HORN, cow, rot=(0, math.radians(35), 0), r2=0.01)
for lx in (-0.2, 0.2):
    for ly in (-0.24, 0.54):
        tube((lx, ly, 0.27), 0.085, 0.44, WHITE, cow)
        tube((lx, ly, 0.06), 0.09, 0.1, HOOF, cow)
ball((0, 0.38, 0.38), (0.24, 0.22, 0.12), PINK, cow)
tube((0, 0.72, 0.74), 0.025, 0.42, WHITE, cow, rot=(math.radians(-35), 0, 0))
ball((0, 0.85, 0.55), (0.08, 0.08, 0.12), BLACK, cow)

# ---------------------------------------------------------------- Alien: fies, mit Panzer und Strahlenpistole
GLOW_RED = basic((1.0, 0.25, 0.1), emit=4.0)
ARMOR = basic((0.32, 0.18, 0.42), rough=0.2, coat=1.0)
TEETH = basic((0.95, 0.95, 0.85), rough=0.3)
al = group("Alien", (-0.35, -0.45, 0.07), rot_z=math.radians(-20))
ball((0, 0, 0.38), (0.4, 0.34, 0.62), ALIEN, al)
soft_box((0, -0.06, 0.48), (0.42, 0.3, 0.36), ARMOR, al, round_=0.4)
ball((0, -0.2, 0.52), (0.08, 0.04, 0.08), GLOW_RED, al)
ball((0, 0.02, 0.92), (0.58, 0.5, 0.56), ALIEN, al, subsurf=2)
ball((0, 0.06, 1.1), (0.5, 0.44, 0.34), ALIEN, al, subsurf=2)
eye(al, (-0.13, -0.2, 0.95), 0.2, iris=GLOW_RED, glow=True, lid=ALIEN, lid_tilt=math.radians(28), lid_cover=0.55)
eye(al, (0.13, -0.2, 0.95), 0.2, iris=GLOW_RED, glow=True, lid=ALIEN, lid_tilt=math.radians(-28), lid_cover=0.55)
ball((0, -0.24, 0.78), (0.22, 0.06, 0.07), BLACK, al, rot=(0, 0, 0))
soft_box((0, -0.27, 0.79), (0.18, 0.02, 0.03), TEETH, al, round_=0.3)
for s in (-1, 1):
    tube((0.1 * s, 0.05, 1.33), 0.018, 0.26, ALIEN_DARK, al, rot=(math.radians(-15), math.radians(25 * s), 0))
    ball((0.16 * s, 0.02, 1.46), (0.08, 0.08, 0.08), GLOW_RED, al)
    ball((0.1 * s, -0.04, 0.07), (0.16, 0.24, 0.1), ALIEN_DARK, al)
ball((-0.24, -0.04, 0.44), (0.11, 0.11, 0.3), ALIEN, al, rot=(0, math.radians(25), 0))
ball((0.26, -0.12, 0.5), (0.11, 0.3, 0.11), ALIEN, al, rot=(math.radians(-10), 0, 0))
gun = group("Strahlenpistole", (0.28, -0.32, 0.52))
gun.parent = al
soft_box((0, 0, 0), (0.1, 0.24, 0.1), ARMOR, gun, round_=0.4)
tube((0, -0.16, 0.0), 0.035, 0.1, METAL, gun, rot=(math.radians(90), 0, 0))
ball((0, -0.22, 0), (0.06, 0.06, 0.06), GLOW_RED, gun)

# ---------------------------------------------------------------- Ufo: dunkles Metall, rote Lichter
ufo = group("Ufo", (2.2, -1.4, 2.0))
ball((0, 0, 0), (1.7, 1.7, 0.34), METAL, ufo, subsurf=2)
bpy.ops.mesh.primitive_torus_add(major_radius=0.82, minor_radius=0.05, location=(0, 0, 0))
parent_to(smooth_obj(bpy.context.object, basic((0.2, 0.2, 0.24), rough=0.3, coat=0.8), 1), ufo)
ball((0, 0, 0.14), (0.7, 0.7, 0.5), basic((0.3, 0.9, 0.5), rough=0.05, coat=1.0), ufo, subsurf=2)
for i in range(8):
    a = i * math.pi / 4
    ball((math.cos(a) * 0.68, math.sin(a) * 0.68, -0.07), (0.11, 0.11, 0.09), GLOW_RED, ufo)
beam_mat = bpy.data.materials.new("Strahl")
beam_mat.use_nodes = True
bn = beam_mat.node_tree
bn.nodes.clear()
o_ = bn.nodes.new("ShaderNodeOutputMaterial")
mix = bn.nodes.new("ShaderNodeMixShader")
tr = bn.nodes.new("ShaderNodeBsdfTransparent")
em = bn.nodes.new("ShaderNodeEmission")
em.inputs["Color"].default_value = (0.4, 1.0, 0.5, 1)
em.inputs["Strength"].default_value = 1.5
mix.inputs["Fac"].default_value = 0.25
bn.links.new(tr.outputs[0], mix.inputs[1])
bn.links.new(em.outputs[0], mix.inputs[2])
bn.links.new(mix.outputs[0], o_.inputs["Surface"])
bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=0.65, radius2=0.18, depth=1.9, location=(2.2, -1.4, 1.0))
beam = bpy.context.object
beam.data.materials.append(beam_mat)
for poly in beam.data.polygons:
    poly.use_smooth = True

# ---------------------------------------------------------------- Bauer: kräftig, mit Bart, Stiefeln und entschlossenem Blick
BEARD = textured((0.32, 0.2, 0.1), (0.45, 0.3, 0.16), 50, bump=0.3, rough=0.9, name="Bart")
BOOT = basic((0.22, 0.14, 0.08), rough=0.5, coat=0.3)
fa = group("Bauer", (-2.35, -1.6, 0.0), rot_z=math.radians(-12))
tube((0, 0, 0.12), 0.5, 0.24, STONE, fa)
for s in (-1, 1):
    tube((0.1 * s, 0, 0.42), 0.075, 0.36, DENIM, fa)
    soft_box((0.1 * s, -0.04, 0.3), (0.15, 0.22, 0.12), BOOT, fa, round_=0.4)
soft_box((0, 0, 0.82), (0.42, 0.3, 0.56), DENIM, fa, round_=0.35)
soft_box((0, 0.01, 1.06), (0.5, 0.32, 0.24), SHIRT, fa, round_=0.4)
for s in (-1, 1):
    soft_box((0.11 * s, -0.15, 0.98), (0.06, 0.03, 0.32), DENIM, fa, round_=0.3)
    ball((0.11 * s, -0.17, 0.86), (0.04, 0.025, 0.04), basic((0.85, 0.7, 0.3), rough=0.2, coat=1.0), fa)
ball((0, 0, 1.36), (0.34, 0.32, 0.38), SKIN, fa, subsurf=2)
ball((0, -0.17, 1.33), (0.09, 0.08, 0.1), CHEEK, fa)
ball((0, -0.05, 1.22), (0.34, 0.28, 0.24), BEARD, fa, subsurf=2)
ball((0, -0.15, 1.28), (0.26, 0.1, 0.06), BEARD, fa)
eye(fa, (-0.075, -0.14, 1.43), 0.12, iris=basic((0.3, 0.45, 0.25), rough=0.2, coat=0.8), lid=SKIN, lid_tilt=math.radians(18), lid_cover=0.42,
    brow=BEARD, brow_tilt=math.radians(22))
eye(fa, (0.075, -0.14, 1.43), 0.12, iris=basic((0.3, 0.45, 0.25), rough=0.2, coat=0.8), lid=SKIN, lid_tilt=math.radians(-18), lid_cover=0.42,
    brow=BEARD, brow_tilt=math.radians(-22))
hat = group("Hut", (0, 0.06, 1.55), scale=0.9)
hat.parent = fa
hat.rotation_euler = (math.radians(-22), 0, 0)
tube((0, 0, 0), 0.38, 0.035, STRAW, hat)
tube((0, 0, 0.1), 0.18, 0.2, STRAW, hat, r2=0.15)
tube((0, 0, 0.03), 0.185, 0.05, HATBAND, hat)
for s in (-1, 1):
    ball((0.26 * s, -0.04, 0.98), (0.13, 0.13, 0.36), SHIRT, fa, rot=(math.radians(-25), math.radians(-12 * s), 0))
    ball((0.28 * s, -0.14, 0.8), (0.11, 0.11, 0.11), SKIN, fa)
tube((0.28, -0.16, 1.1), 0.025, 1.45, WOOD, fa, rot=(math.radians(-6), 0, 0))
fork = group("Gabel", (0.28, -0.08, 1.85))
fork.parent = fa
soft_box((0, 0, 0), (0.26, 0.04, 0.05), METAL, fork, round_=0.3)
for d in (-0.11, 0.0, 0.11):
    tube((d, 0, 0.13), 0.016, 0.26, METAL, fork, r2=0.004)

# ---------------------------------------------------------------- Licht, Kamera, Render
world = bpy.data.worlds.new("Himmel")
scene.world = world
world.use_nodes = True
wn = world.node_tree
bg = wn.nodes["Background"]
bg.inputs["Color"].default_value = (0.62, 0.74, 0.88, 1)
bg.inputs["Strength"].default_value = 0.6

bpy.ops.object.light_add(type="SUN", rotation=(math.radians(58), math.radians(12), math.radians(-50)))
sun = bpy.context.object
sun.data.energy = 4.2
sun.data.color = (1.0, 0.86, 0.68)
sun.data.angle = math.radians(8)

# weiches Aufhelllicht von vorne, damit Gesichter nicht im Schatten liegen
bpy.ops.object.light_add(type="AREA", location=(-3, -7, 6))
fill = bpy.context.object
fill.data.energy = 300
fill.data.size = 8
fill.data.color = (0.85, 0.92, 1.0)
fill.rotation_euler = (Vector((0, 0, 0)) - fill.location).to_track_quat("-Z", "Y").to_euler()

bpy.ops.object.camera_add()
cam = bpy.context.object
cam.data.lens = 70
if len(args) > 2 and args[2] == "nah":
    target = Vector((-1.35, -1.0, 0.95))
    direction = Vector((-0.3, -1.0, 0.55)).normalized()
    cam.location = target + direction * 7.5
else:
    target = Vector((0.0, -0.25, 0.35))
    direction = Vector((-0.2, -0.75, 1.3)).normalized()
    cam.location = target + direction * 18.5
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = cam


# ---------------------------------------------------------------- Cel-Shading wie in Bloons TD6
def to_toon(m):
    nt = m.node_tree
    if nt is None:
        return
    p = nt.nodes.get("Principled BSDF")
    if p is None or p.inputs["Emission Strength"].default_value > 0 or p.inputs["Roughness"].default_value < 0.1:
        return
    outn = [n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"][0]
    base = p.inputs["Base Color"]
    def color_into(sock):
        if base.is_linked:
            nt.links.new(base.links[0].from_socket, sock)
        else:
            sock.default_value = base.default_value
    toon = nt.nodes.new("ShaderNodeBsdfToon")
    toon.component = "DIFFUSE"
    toon.inputs["Size"].default_value = 0.8
    toon.inputs["Smooth"].default_value = 0.22
    color_into(toon.inputs["Color"])
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = 0.42
    color_into(em.inputs["Color"])
    add = nt.nodes.new("ShaderNodeAddShader")
    nt.links.new(toon.outputs[0], add.inputs[0])
    nt.links.new(em.outputs[0], add.inputs[1])
    final = add.outputs[0]
    if p.inputs["Coat Weight"].default_value >= 0.3:
        gl = nt.nodes.new("ShaderNodeBsdfToon")
        gl.component = "GLOSSY"
        gl.inputs["Size"].default_value = 0.12
        gl.inputs["Smooth"].default_value = 0.08
        gl.inputs["Color"].default_value = (1, 1, 1, 1)
        add2 = nt.nodes.new("ShaderNodeAddShader")
        mix = nt.nodes.new("ShaderNodeMixShader")
        mix.inputs["Fac"].default_value = 0.25
        nt.links.new(final, mix.inputs[1])
        nt.links.new(gl.outputs[0], mix.inputs[2])
        final = mix.outputs[0]
    nt.links.new(final, outn.inputs["Surface"])

for m in bpy.data.materials:
    to_toon(m)

scene.render.use_freestyle = True
scene.render.line_thickness_mode = "ABSOLUTE"
scene.render.line_thickness = 1.0
fs = scene.view_layers[0].freestyle_settings
fs.crease_angle = math.radians(120)
ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("Linien")
if ls.linestyle is None:
    ls.linestyle = bpy.data.linestyles.new("Linienstil")
ls.linestyle.color = (0.16, 0.08, 0.04)
ls.linestyle.thickness = 2.6 if not (len(args) > 2 and args[2] == "nah") else 4.0
ls.select_by_visibility = True
ls.select_silhouette = True
ls.select_border = True
ls.select_crease = False

scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = samples
scene.cycles.use_denoising = False
scene.cycles.max_bounces = 6
scene.render.resolution_x = 1200
scene.render.resolution_y = 740
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = -0.35
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("FERTIG", out)
