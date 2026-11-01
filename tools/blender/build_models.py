"""Baut die Spielfiguren von Muh Defense im Grafikstil 7 und exportiert sie als FBX für Unity.

Aufruf:
    blender -b -P tools/blender/build_models.py -- <Zielordner> [vorschau.png]

Jede Figur wird aus Grundformen zusammengesetzt, die Farben werden als Vertexfarben
eingebacken (Attribut "Col"). In Unity zeichnet der Shader "MuhDefense/Toon" daraus
Cel-Shading und Umrisslinien. Figuren schauen in Blender nach -Y (in Unity nach +Z),
der Ursprung liegt zwischen den Füßen.
"""
import bpy, sys, math, os, random

args = sys.argv[sys.argv.index("--") + 1:]
out_dir = os.path.abspath(args[0])
preview = os.path.abspath(args[1]) if len(args) > 1 else None
os.makedirs(out_dir, exist_ok=True)
random.seed(4)

# ---------------------------------------------------------------- Farben
WHITE = (0.97, 0.96, 0.93)
BLACK = (0.04, 0.04, 0.05)
PINK = (1.0, 0.58, 0.64)
HORN = (0.95, 0.88, 0.7)
HOOF = (0.25, 0.18, 0.14)
GOLD = (0.95, 0.75, 0.25)
SKIN = (1.0, 0.76, 0.6)
CHEEK = (1.0, 0.55, 0.5)
DENIM = (0.18, 0.36, 0.75)
SHIRT = (0.82, 0.2, 0.14)
STRAW = (0.96, 0.76, 0.34)
HATBAND = (0.75, 0.15, 0.12)
BEARD = (0.42, 0.27, 0.13)
BOOT = (0.24, 0.15, 0.08)
WOOD = (0.55, 0.34, 0.17)
DARK_WOOD = (0.38, 0.23, 0.11)
METAL = (0.6, 0.63, 0.7)
DARK_METAL = (0.3, 0.31, 0.36)
STONE = (0.62, 0.6, 0.57)
GLOW_RED = (1.0, 0.3, 0.12)
ARMOR = (0.42, 0.22, 0.55)
TEETH = (0.95, 0.95, 0.85)
MILK_BLUE = (0.35, 0.6, 1.0)
ICE = (0.7, 0.92, 1.0)
LEAF = (0.25, 0.6, 0.18)

# ---------------------------------------------------------------- Bausteine

parts = []


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    parts.clear()


def finish(o, color, subsurf=1, bevel=0.0):
    if bevel > 0:
        b = o.modifiers.new("Bevel", "BEVEL")
        b.width = bevel
        b.segments = 2
    if subsurf:
        s = o.modifiers.new("Sub", "SUBSURF")
        s.levels = subsurf
        s.render_levels = subsurf
    for poly in o.data.polygons:
        poly.use_smooth = True
    parts.append((o, color))
    return o


def ball(loc, size, color, rot=(0, 0, 0), subsurf=1, segs=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=max(6, segs // 2), radius=1, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    return finish(o, color, subsurf)


def soft_box(loc, size, color, round_=0.35, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(scale=True)
    return finish(o, color, 1, bevel=min(size) * round_)


def tube(loc, r, h, color, rot=(0, 0, 0), r2=None, segs=12):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=segs, radius=r, depth=h, location=loc, rotation=rot)
        return finish(bpy.context.object, color, 1, bevel=min(r * 0.4, h * 0.2))
    bpy.ops.mesh.primitive_cone_add(vertices=segs, radius1=r, radius2=r2, depth=h, location=loc, rotation=rot)
    return finish(bpy.context.object, color, 1)


def ring(loc, major, minor, color, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=24, minor_segments=8, location=loc, rotation=rot)
    return finish(bpy.context.object, color, 0)


def eye(loc, size, iris, lid=None, lid_tilt=0.0, lid_cover=0.45, brow=None, brow_tilt=0.0, glow=False):
    """Cartoon-Auge mit Lid (entschlossener/fieser Blick) und Augenbraue, Blickrichtung -y."""
    x, y, z = loc
    ball((x, y, z), (size, size * 0.8, size), WHITE if not glow else iris)
    if not glow:
        ball((x, y - size * 0.36, z - size * 0.05), (size * 0.62, size * 0.2, size * 0.66), iris)
        ball((x, y - size * 0.44, z - size * 0.05), (size * 0.32, size * 0.12, size * 0.36), BLACK)
        ball((x + size * 0.12, y - size * 0.5, z + size * 0.1), (size * 0.12, size * 0.05, size * 0.12), WHITE, subsurf=0, segs=8)
    if lid is not None:
        ball((x, y - size * 0.04, z + size * (0.62 - lid_cover)), (size * 1.18, size * 0.95, size * 0.62), lid, rot=(0, lid_tilt, 0))
    if brow is not None:
        soft_box((x, y - size * 0.3, z + size * 0.62), (size * 1.25, size * 0.25, size * 0.24), brow, round_=0.45, rot=(0, brow_tilt, 0))


def export(name, max_tris=6000):
    """Modifikatoren anwenden, Farben als Vertexfarben einbacken, zu einem Objekt verbinden, FBX exportieren."""
    bpy.ops.object.select_all(action="DESELECT")
    meshes = []
    for o, color in parts:
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        bpy.ops.object.convert(target="MESH")
        o.select_set(False)
        attr = o.data.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        rgba = (color[0], color[1], color[2], 1.0)
        for d in attr.data:
            d.color_srgb = rgba
        meshes.append(o)
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    if tris > max_tris:
        # Für das Handy vereinfachen (Formen bleiben, Dreiecke werden weniger)
        dec = obj.modifiers.new("Vereinfachen", "DECIMATE")
        dec.ratio = max_tris / tris
        bpy.ops.object.modifier_apply(modifier=dec.name)
        tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(out_dir, name + ".fbx"),
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        colors_type="SRGB",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    print(f"EXPORT {name}: {tris} Dreiecke")
    return obj


# ---------------------------------------------------------------- Figuren

def build_cow():
    soft_box((0, 0.15, 0.7), (0.66, 1.08, 0.6), WHITE, round_=0.4)
    # Flecken als flache Beulen auf dem Fell
    for (x, y, z, sx, sy, sz) in [(0.3, 0.1, 0.78, 0.12, 0.42, 0.32), (-0.3, 0.35, 0.72, 0.12, 0.36, 0.3),
                                  (0.0, 0.5, 1.0, 0.36, 0.3, 0.1), (-0.12, -0.12, 1.0, 0.3, 0.26, 0.1)]:
        ball((x, y, z), (sx, sy, sz), BLACK)
    soft_box((0, -0.55, 0.9), (0.46, 0.46, 0.44), WHITE, round_=0.42)
    soft_box((0, -0.8, 0.76), (0.42, 0.2, 0.26), PINK, round_=0.45)
    ball((-0.09, -0.9, 0.78), (0.06, 0.03, 0.07), BLACK, subsurf=0, segs=8)
    ball((0.09, -0.9, 0.78), (0.06, 0.03, 0.07), BLACK, subsurf=0, segs=8)
    ring((0, -0.92, 0.7), 0.07, 0.016, GOLD, rot=(math.radians(90), 0, 0))
    brown = (0.32, 0.2, 0.12)
    eye((-0.12, -0.76, 1.01), 0.16, brown, lid=WHITE, lid_tilt=math.radians(12), lid_cover=0.45)
    eye((0.12, -0.76, 1.01), 0.16, brown, lid=WHITE, lid_tilt=math.radians(-12), lid_cover=0.45)
    ball((-0.3, -0.5, 1.02), (0.2, 0.08, 0.1), WHITE, rot=(0, math.radians(-20), 0))
    ball((0.3, -0.5, 1.02), (0.2, 0.08, 0.1), WHITE, rot=(0, math.radians(20), 0))
    tube((-0.17, -0.5, 1.16), 0.05, 0.22, HORN, rot=(0, math.radians(-35), 0), r2=0.01)
    tube((0.17, -0.5, 1.16), 0.05, 0.22, HORN, rot=(0, math.radians(35), 0), r2=0.01)
    for lx in (-0.2, 0.2):
        for ly in (-0.24, 0.54):
            tube((lx, ly, 0.27), 0.085, 0.44, WHITE)
            tube((lx, ly, 0.06), 0.09, 0.1, HOOF)
    ball((0, 0.38, 0.38), (0.24, 0.22, 0.12), PINK)
    tube((0, 0.72, 0.74), 0.025, 0.42, WHITE, rot=(math.radians(-35), 0, 0))
    ball((0, 0.85, 0.55), (0.08, 0.08, 0.12), BLACK)


def build_alien(body, dark, scale=1.0, armor=None, crown=False, jet=False, bulky=False, gun=True):
    """Fieses Alien. Varianten über Farbe, Panzer, Krone (Boss), Düse (Flitzer) und kräftigen Körper (Brocken)."""
    s = scale
    w = 1.3 if bulky else 1.0
    ball((0, 0, 0.38 * s), (0.4 * s * w, 0.34 * s * w, 0.62 * s), body)
    if armor:
        soft_box((0, -0.06 * s, 0.48 * s), (0.42 * s * w, 0.3 * s * w, 0.36 * s), armor, round_=0.4)
        ball((0, -0.2 * s * w, 0.52 * s), (0.08 * s, 0.04 * s, 0.08 * s), GLOW_RED)
    ball((0, 0.02 * s, 0.92 * s), (0.58 * s, 0.5 * s, 0.56 * s), body, subsurf=2)
    ball((0, 0.06 * s, 1.1 * s), (0.5 * s, 0.44 * s, 0.34 * s), body, subsurf=2)
    eye((-0.13 * s, -0.2 * s, 0.95 * s), 0.2 * s, GLOW_RED, glow=True, lid=body, lid_tilt=math.radians(28), lid_cover=0.55)
    eye((0.13 * s, -0.2 * s, 0.95 * s), 0.2 * s, GLOW_RED, glow=True, lid=body, lid_tilt=math.radians(-28), lid_cover=0.55)
    # breites, fieses Grinsen mit Zähnen
    ball((0, -0.24 * s, 0.78 * s), (0.24 * s, 0.07 * s, 0.08 * s), BLACK)
    for dx in (-0.06, 0.0, 0.06):
        tube((dx * s, -0.27 * s, 0.81 * s), 0.016 * s, 0.05 * s, TEETH, rot=(0, math.radians(180), 0), r2=0.002 * s, segs=6)
    for side in (-1, 1):
        tube((0.1 * side * s, 0.05 * s, 1.33 * s), 0.018 * s, 0.26 * s, dark, rot=(math.radians(-15), math.radians(25 * side), 0))
        ball((0.16 * side * s, 0.02 * s, 1.46 * s), (0.08 * s, 0.08 * s, 0.08 * s), GLOW_RED)
        ball((0.1 * side * s * w, -0.04 * s, 0.07 * s), (0.16 * s * w, 0.24 * s, 0.1 * s), dark)
    ball((-0.24 * s * w, -0.04 * s, 0.44 * s), (0.11 * s * w, 0.11 * s * w, 0.3 * s), body, rot=(0, math.radians(25), 0))
    ball((0.26 * s * w, -0.12 * s, 0.5 * s), (0.11 * s * w, 0.3 * s, 0.11 * s * w), body, rot=(math.radians(-10), 0, 0))
    if gun:
        gx, gy, gz = 0.28 * s * w, -0.32 * s, 0.52 * s
        soft_box((gx, gy, gz), (0.1 * s, 0.24 * s, 0.1 * s), armor or ARMOR, round_=0.4)
        tube((gx, gy - 0.16 * s, gz), 0.035 * s, 0.1 * s, METAL, rot=(math.radians(90), 0, 0))
        ball((gx, gy - 0.22 * s, gz), (0.06 * s, 0.06 * s, 0.06 * s), GLOW_RED)
    if crown:
        tube((0, 0.06 * s, 1.3 * s), 0.17 * s, 0.1 * s, GOLD)
        for i in range(5):
            a = i * 2 * math.pi / 5
            tube((math.cos(a) * 0.14 * s, 0.06 * s + math.sin(a) * 0.14 * s, 1.4 * s), 0.035 * s, 0.12 * s, GOLD, r2=0.005 * s, segs=6)
        soft_box((0, 0.22 * s, 0.6 * s), (0.5 * s, 0.06 * s, 0.7 * s), (0.6, 0.1, 0.12), round_=0.3, rot=(math.radians(10), 0, 0))
    if jet:
        for side in (-1, 1):
            tube((0.1 * side * s, 0.22 * s, 0.55 * s), 0.07 * s, 0.32 * s, DARK_METAL)
            tube((0.1 * side * s, 0.22 * s, 0.36 * s), 0.07 * s, 0.1 * s, GLOW_RED, r2=0.02 * s)


def build_sneaker():
    """Schleicher: getarntes Alien mit Kapuze und Messer."""
    body = (0.42, 0.38, 0.5)
    dark = (0.2, 0.18, 0.26)
    build_alien(body, dark, scale=0.95, gun=False)
    s = 0.95
    # Kapuze über dem Kopf
    ball((0, 0.08 * s, 1.0 * s), (0.66 * s, 0.6 * s, 0.62 * s), dark, subsurf=2)
    tube((0, 0.22 * s, 1.28 * s), 0.12 * s, 0.3 * s, dark, rot=(math.radians(-50), 0, 0), r2=0.01 * s)
    # Halstuch über dem Mund
    ball((0, -0.18 * s, 0.76 * s), (0.42 * s, 0.22 * s, 0.16 * s), (0.3, 0.12, 0.35))
    # Messer in der rechten Hand
    tube((0.28 * s, -0.22 * s, 0.42 * s), 0.02 * s, 0.1 * s, DARK_WOOD)
    tube((0.28 * s, -0.22 * s, 0.56 * s), 0.035 * s, 0.22 * s, METAL, r2=0.004 * s, segs=6)


def build_medic():
    """Sanitäter: weißes Alien mit Kreuz auf dem Brustpanzer und Heiltrank auf dem Rücken."""
    body = (0.88, 0.94, 0.9)
    dark = (0.45, 0.6, 0.55)
    teal = (0.2, 0.65, 0.6)
    build_alien(body, dark, scale=1.0, armor=teal, gun=False)
    red = (0.9, 0.15, 0.15)
    soft_box((0, -0.23, 0.5), (0.18, 0.03, 0.06), red, round_=0.3)
    soft_box((0, -0.23, 0.5), (0.06, 0.03, 0.18), red, round_=0.3)
    tube((0, 0.22, 0.6), 0.1, 0.26, (0.3, 1.0, 0.45))
    tube((0, 0.22, 0.78), 0.04, 0.1, (0.55, 0.4, 0.25))
    # Spritze in der Hand
    tube((0.28, -0.24, 0.48), 0.035, 0.18, (0.85, 0.95, 1.0), rot=(math.radians(-60), 0, 0))
    tube((0.28, -0.34, 0.44), 0.008, 0.1, METAL, rot=(math.radians(-60), 0, 0))


def build_cow_ufo():
    """Kuhfänger-Ufo: kleines Ufo mit Alien-Pilot unter der Glaskuppel."""
    ball((0, 0, 0.35), (1.3, 1.3, 0.3), METAL, subsurf=2, segs=24)
    ring((0, 0, 0.35), 0.62, 0.045, DARK_METAL)
    ball((0, 0, 0.47), (0.56, 0.56, 0.42), (0.55, 0.95, 0.7), subsurf=2)
    # Pilot schaut aus der Kuppel
    ball((0, -0.02, 0.62), (0.3, 0.28, 0.28), (0.35, 0.72, 0.22), subsurf=1)
    eye((-0.07, -0.13, 0.65), 0.1, GLOW_RED, glow=True, lid=(0.35, 0.72, 0.22), lid_tilt=math.radians(28), lid_cover=0.55)
    eye((0.07, -0.13, 0.65), 0.1, GLOW_RED, glow=True, lid=(0.35, 0.72, 0.22), lid_tilt=math.radians(-28), lid_cover=0.55)
    for i in range(6):
        a = i * math.pi / 3
        ball((math.cos(a) * 0.52, math.sin(a) * 0.52, 0.28), (0.09, 0.09, 0.08), GLOW_RED)
    # Greifarm-Strahler unten
    tube((0, 0, 0.16), 0.12, 0.14, DARK_METAL, r2=0.18)
    ball((0, 0, 0.08), (0.2, 0.2, 0.08), (0.5, 1.0, 0.6))


def build_farmer():
    """Mistgabel-Bauer mit Steinsockel (Turm)."""
    tube((0, 0, 0.12), 0.5, 0.24, STONE, segs=16)
    for s_ in (-1, 1):
        tube((0.1 * s_, 0, 0.42), 0.075, 0.36, DENIM)
        soft_box((0.1 * s_, -0.04, 0.3), (0.15, 0.22, 0.12), BOOT, round_=0.4)
    soft_box((0, 0, 0.82), (0.42, 0.3, 0.56), DENIM, round_=0.35)
    soft_box((0, 0.01, 1.06), (0.5, 0.32, 0.24), SHIRT, round_=0.4)
    for s_ in (-1, 1):
        soft_box((0.11 * s_, -0.15, 0.98), (0.06, 0.03, 0.32), DENIM, round_=0.3)
        ball((0.11 * s_, -0.17, 0.86), (0.04, 0.025, 0.04), GOLD, subsurf=0, segs=8)
    ball((0, 0, 1.36), (0.34, 0.32, 0.38), SKIN, subsurf=2)
    ball((0, -0.17, 1.33), (0.09, 0.08, 0.1), CHEEK)
    ball((0, -0.05, 1.22), (0.34, 0.28, 0.24), BEARD, subsurf=2)
    ball((0, -0.15, 1.28), (0.26, 0.1, 0.06), BEARD)
    green = (0.3, 0.5, 0.25)
    eye((-0.075, -0.14, 1.43), 0.12, green, lid=SKIN, lid_tilt=math.radians(18), lid_cover=0.42, brow=BEARD, brow_tilt=math.radians(22))
    eye((0.075, -0.14, 1.43), 0.12, green, lid=SKIN, lid_tilt=math.radians(-18), lid_cover=0.42, brow=BEARD, brow_tilt=math.radians(-22))
    hat_rot = (math.radians(-22), 0, 0)
    tube((0, 0.06, 1.55), 0.34, 0.035, STRAW, rot=hat_rot, segs=20)
    tube((0, 0.1, 1.64), 0.16, 0.18, STRAW, rot=hat_rot, r2=0.13, segs=16)
    tube((0, 0.07, 1.58), 0.165, 0.045, HATBAND, rot=hat_rot, segs=16)
    for s_ in (-1, 1):
        ball((0.26 * s_, -0.04, 0.98), (0.13, 0.13, 0.36), SHIRT, rot=(math.radians(-25), math.radians(-12 * s_), 0))
        ball((0.28 * s_, -0.14, 0.8), (0.11, 0.11, 0.11), SKIN)
    tube((0.28, -0.16, 1.1), 0.025, 1.45, WOOD, rot=(math.radians(-6), 0, 0))
    soft_box((0.28, -0.08, 1.85), (0.26, 0.04, 0.05), METAL, round_=0.3)
    for d in (-0.11, 0.0, 0.11):
        tube((0.28 + d, -0.08, 1.98), 0.016, 0.26, METAL, r2=0.004, segs=6)


def build_heinz():
    """Held Bauer Heinz: grimmiger Bauer mit Schrotflinte und Halstuch, ohne Sockel."""
    for s_ in (-1, 1):
        tube((0.1 * s_, 0, 0.2), 0.075, 0.36, DENIM)
        soft_box((0.1 * s_, -0.04, 0.07), (0.15, 0.24, 0.13), BOOT, round_=0.4)
    soft_box((0, 0, 0.6), (0.44, 0.32, 0.56), DENIM, round_=0.35)
    soft_box((0, 0.01, 0.84), (0.52, 0.34, 0.24), (0.25, 0.45, 0.25), round_=0.4)
    for s_ in (-1, 1):
        soft_box((0.11 * s_, -0.16, 0.76), (0.06, 0.03, 0.32), DENIM, round_=0.3)
        ball((0.11 * s_, -0.18, 0.64), (0.04, 0.025, 0.04), GOLD, subsurf=0, segs=8)
    ball((0, -0.06, 0.98), (0.36, 0.3, 0.12), SHIRT)
    ball((0, 0, 1.14), (0.36, 0.34, 0.4), SKIN, subsurf=2)
    ball((0, -0.18, 1.11), (0.1, 0.09, 0.11), CHEEK)
    ball((0, -0.05, 1.0), (0.36, 0.3, 0.26), BEARD, subsurf=2)
    ball((0, -0.16, 1.06), (0.28, 0.1, 0.07), BEARD)
    green = (0.3, 0.5, 0.25)
    eye((-0.08, -0.15, 1.21), 0.12, green, lid=SKIN, lid_tilt=math.radians(20), lid_cover=0.45, brow=BEARD, brow_tilt=math.radians(24))
    eye((0.08, -0.15, 1.21), 0.12, green, lid=SKIN, lid_tilt=math.radians(-20), lid_cover=0.45, brow=BEARD, brow_tilt=math.radians(-24))
    hat_rot = (math.radians(-18), 0, 0)
    tube((0, 0.05, 1.33), 0.36, 0.035, STRAW, rot=hat_rot, segs=20)
    tube((0, 0.09, 1.42), 0.17, 0.18, STRAW, rot=hat_rot, r2=0.14, segs=16)
    tube((0, 0.06, 1.36), 0.175, 0.045, HATBAND, rot=hat_rot, segs=16)
    # Arme halten die Schrotflinte quer vor dem Körper
    ball((-0.24, -0.12, 0.74), (0.13, 0.3, 0.13), (0.25, 0.45, 0.25), rot=(math.radians(-60), 0, math.radians(-20)))
    ball((0.24, -0.12, 0.74), (0.13, 0.3, 0.13), (0.25, 0.45, 0.25), rot=(math.radians(-60), 0, math.radians(20)))
    ball((-0.14, -0.3, 0.7), (0.11, 0.11, 0.11), SKIN)
    ball((0.16, -0.32, 0.72), (0.11, 0.11, 0.11), SKIN)
    gun_rot = (math.radians(90), 0, math.radians(-25))
    soft_box((-0.05, -0.28, 0.69), (0.08, 0.34, 0.1), DARK_WOOD, round_=0.3, rot=(0, 0, math.radians(-25)))
    for d in (-0.02, 0.02):
        tube((0.12 + d, -0.5, 0.74), 0.022, 0.5, DARK_METAL, rot=gun_rot)


def build_tractor():
    """Traktor für die Traktor-Raserei: rote Karosserie, große Hinterräder."""
    red = (0.82, 0.16, 0.12)
    soft_box((0, -0.1, 0.42), (0.5, 0.9, 0.36), red, round_=0.3)
    soft_box((0, -0.45, 0.5), (0.42, 0.3, 0.3), red, round_=0.35)
    soft_box((0, -0.62, 0.48), (0.36, 0.04, 0.22), DARK_METAL, round_=0.3)
    soft_box((0, 0.22, 0.68), (0.52, 0.38, 0.1), DARK_METAL, round_=0.3)
    soft_box((0, 0.28, 0.82), (0.2, 0.2, 0.24), (0.2, 0.2, 0.22), round_=0.3)
    tube((0.12, -0.42, 0.8), 0.04, 0.4, DARK_METAL)
    ball((0.12, -0.42, 1.02), (0.1, 0.1, 0.06), (0.3, 0.3, 0.3))
    for s_ in (-1, 1):
        tube((0.34 * s_, 0.25, 0.36), 0.36, 0.2, (0.12, 0.12, 0.13), rot=(0, math.radians(90), 0), segs=20)
        tube((0.35 * s_, 0.25, 0.36), 0.18, 0.21, GOLD, rot=(0, math.radians(90), 0), segs=16)
        tube((0.28 * s_, -0.5, 0.2), 0.2, 0.14, (0.12, 0.12, 0.13), rot=(0, math.radians(90), 0), segs=16)
        tube((0.29 * s_, -0.5, 0.2), 0.1, 0.15, GOLD, rot=(0, math.radians(90), 0), segs=12)
        ball((0.16 * s_, -0.62, 0.56), (0.1, 0.04, 0.1), (1.0, 0.95, 0.6))


def build_catapult():
    """Heuballen-Katapult: Holzgestell, Wurfarm, Heuballen."""
    tube((0, 0, 0.1), 0.48, 0.2, STONE, segs=16)
    soft_box((0, 0.05, 0.3), (0.62, 0.7, 0.16), WOOD, round_=0.3)
    for s_ in (-1, 1):
        soft_box((0.24 * s_, 0.1, 0.55), (0.08, 0.1, 0.5), DARK_WOOD, round_=0.3)
        tube((0.32 * s_, -0.25, 0.22), 0.13, 0.06, DARK_WOOD, rot=(0, math.radians(90), 0), segs=14)
        tube((0.32 * s_, 0.35, 0.22), 0.13, 0.06, DARK_WOOD, rot=(0, math.radians(90), 0), segs=14)
    tube((0, 0.1, 0.78), 0.05, 0.56, METAL, rot=(0, math.radians(90), 0))
    soft_box((0, -0.15, 0.85), (0.1, 0.65, 0.08), WOOD, round_=0.3, rot=(math.radians(-25), 0, 0))
    soft_box((0, -0.42, 0.98), (0.3, 0.26, 0.06), DARK_WOOD, round_=0.3, rot=(math.radians(-25), 0, 0))
    tube((0, -0.42, 1.14), 0.17, 0.26, STRAW, rot=(0, math.radians(90), 0), segs=16)
    ring((0, -0.42, 1.14), 0.172, 0.012, (0.75, 0.55, 0.2), rot=(0, math.radians(90), 0))
    ball((0, 0.4, 0.5), (0.24, 0.24, 0.24), (0.55, 0.55, 0.58))


def build_sprayer():
    """Eismilch-Sprüher: Milchkanne mit Kühlschlangen und Sprühdüse."""
    tube((0, 0, 0.1), 0.48, 0.2, STONE, segs=16)
    tube((0, 0, 0.55), 0.3, 0.7, WHITE, segs=20)
    tube((0, 0, 0.94), 0.22, 0.12, WHITE, segs=20)
    tube((0, 0, 0.5), 0.305, 0.08, MILK_BLUE, segs=20)
    tube((0, 0, 0.74), 0.305, 0.05, MILK_BLUE, segs=20)
    for i in range(4):
        a = i * math.pi / 2 + math.pi / 4
        ball((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.62), (0.12, 0.12, 0.12), ICE)
    tube((0, -0.1, 1.08), 0.06, 0.2, DARK_METAL)
    tube((0, -0.28, 1.16), 0.05, 0.36, DARK_METAL, rot=(math.radians(70), 0, 0))
    tube((0, -0.46, 1.22), 0.08, 0.1, METAL, rot=(math.radians(70), 0, 0), r2=0.04)
    ball((0, -0.52, 1.24), (0.1, 0.1, 0.1), ICE)
    for s_ in (-1, 1):
        ring((0.32 * s_, 0, 0.8), 0.08, 0.018, METAL, rot=(0, math.radians(90), 0))


def build_beehive():
    """Bienenstock: geflochtener Bienenkorb auf einem Holzbock, Bienen drumherum."""
    tube((0, 0, 0.1), 0.48, 0.2, STONE, segs=16)
    for s_ in (-1, 1):
        for t in (-1, 1):
            tube((0.18 * s_, 0.18 * t, 0.36), 0.035, 0.34, DARK_WOOD)
    soft_box((0, 0, 0.55), (0.56, 0.56, 0.06), WOOD, round_=0.3)
    straw_dark = (0.78, 0.55, 0.2)
    for i, (r, z) in enumerate([(0.26, 0.66), (0.25, 0.78), (0.22, 0.9), (0.17, 1.0), (0.1, 1.08)]):
        ring((0, 0, z), r, 0.06, STRAW if i % 2 == 0 else straw_dark)
    ball((0, 0, 0.84), (0.48, 0.48, 0.56), STRAW)
    ball((0, -0.24, 0.68), (0.14, 0.06, 0.1), (0.15, 0.1, 0.05))
    for (x, y, z) in [(0.35, -0.2, 1.0), (-0.32, -0.25, 0.85), (0.1, -0.4, 1.15), (-0.2, 0.3, 1.2)]:
        ball((x, y, z), (0.1, 0.08, 0.08), (1.0, 0.8, 0.1))
        ball((x, y + 0.01, z + 0.04), (0.08, 0.03, 0.05), WHITE, subsurf=0, segs=8)


def build_windmill():
    """Windmühle: kleiner Turm mit Dach und vier Flügeln vorne."""
    tube((0, 0, 0.1), 0.48, 0.2, STONE, segs=16)
    tube((0, 0, 0.6), 0.3, 0.8, (0.92, 0.88, 0.78), r2=0.22, segs=16)
    tube((0, 0, 1.1), 0.27, 0.32, (0.72, 0.2, 0.15), r2=0.02, segs=16)
    soft_box((0, -0.22, 0.35), (0.16, 0.04, 0.26), DARK_WOOD, round_=0.3)
    tube((0, -0.26, 0.95), 0.05, 0.12, DARK_WOOD, rot=(math.radians(90), 0, 0))
    for i in range(4):
        a = math.radians(45 + i * 90)
        cx, cz = math.cos(a) * 0.36, 0.95 + math.sin(a) * 0.36
        soft_box((cx, -0.32, cz), (0.16, 0.03, 0.62), WOOD, round_=0.2, rot=(0, -a + math.radians(90), 0))
        soft_box((math.cos(a) * 0.42, -0.34, 0.95 + math.sin(a) * 0.42), (0.14, 0.015, 0.42), WHITE, round_=0.2,
                 rot=(0, -a + math.radians(90), 0))
    ball((0, -0.33, 0.95), (0.1, 0.06, 0.1), DARK_METAL)


def build_weathercock():
    """Wetterhahn: Holzpfahl mit Windrose und einem Hahn obendrauf."""
    tube((0, 0, 0.1), 0.48, 0.2, STONE, segs=16)
    tube((0, 0, 0.75), 0.05, 1.1, DARK_WOOD)
    for (dx, dy, letter_c) in [(0.22, 0, GOLD), (-0.22, 0, GOLD), (0, 0.22, GOLD), (0, -0.22, GOLD)]:
        tube((dx / 2, dy / 2, 1.0), 0.015, 0.22, DARK_METAL, rot=(math.radians(90) if dy else 0, math.radians(90) if dx else 0, 0))
        ball((dx, dy, 1.0), (0.06, 0.06, 0.06), letter_c)
    # Hahn im Profil (von vorne gut zu erkennen)
    orange = (0.95, 0.45, 0.15)
    red = (0.9, 0.1, 0.1)
    ball((0.02, 0, 1.45), (0.5, 0.26, 0.34), orange)
    ball((-0.24, 0, 1.66), (0.22, 0.2, 0.24), orange)
    tube((-0.38, 0, 1.65), 0.05, 0.1, GOLD, rot=(0, math.radians(-90), 0), r2=0.003)
    for i, x in enumerate((-0.3, -0.24, -0.18)):
        ball((x, 0, 1.8 + (0.02 if i == 1 else 0)), (0.07, 0.06, 0.1), red)
    ball((-0.3, 0, 1.55), (0.06, 0.05, 0.1), red)
    for s_ in (-1, 1):
        ball((-0.3, 0.09 * s_, 1.69), (0.05, 0.03, 0.05), BLACK, subsurf=0, segs=8)
    for i, c in enumerate([(0.15, 0.45, 0.2), (0.1, 0.25, 0.65), (0.95, 0.3, 0.1), (0.15, 0.45, 0.2)]):
        a = math.radians(-20 + 22 * i)
        ball((0.3 + math.cos(a) * 0.12, 0, 1.6 + math.sin(a) * 0.18), (0.12, 0.08, 0.36), c, rot=(0, math.radians(-30 + 20 * i), 0))
    tube((0.06, 0, 1.29), 0.02, 0.1, GOLD)
    for s_ in (-1, 1):
        tube((0.04 * s_, 0, 1.3), 0.015, 0.08, GOLD)


def build_farm_shop():
    """Hofladen: Marktstand mit gestreifter Markise und Kisten voller Gemüse."""
    tube((0, 0, 0.1), 0.48, 0.2, STONE, segs=16)
    soft_box((0, 0.05, 0.42), (0.7, 0.45, 0.42), WOOD, round_=0.2)
    for s_ in (-1, 1):
        tube((0.32 * s_, -0.15, 0.75), 0.03, 0.7, DARK_WOOD)
        tube((0.32 * s_, 0.25, 0.75), 0.03, 0.7, DARK_WOOD)
    for i in range(5):
        soft_box((-0.3 + 0.15 * i, 0.05, 1.12), (0.15, 0.6, 0.04), (0.85, 0.15, 0.12) if i % 2 == 0 else WHITE, round_=0.2,
                 rot=(math.radians(-12), 0, 0))
    for (x, c) in [(-0.2, (0.95, 0.5, 0.1)), (0.0, (0.3, 0.7, 0.2)), (0.2, (0.95, 0.15, 0.15))]:
        soft_box((x, -0.08, 0.68), (0.18, 0.18, 0.08), DARK_WOOD, round_=0.2)
        for j in range(3):
            ball((x - 0.05 + 0.05 * j, -0.08, 0.76), (0.08, 0.08, 0.08), c)
    soft_box((0, -0.2, 0.92), (0.3, 0.03, 0.12), (0.95, 0.9, 0.7), round_=0.2)


def build_coin():
    """Münze (nur für Symbole)."""
    tube((0, 0, 0.5), 0.45, 0.12, GOLD, rot=(math.radians(90), 0, 0), segs=24)
    tube((0, -0.065, 0.5), 0.33, 0.02, (1.0, 0.88, 0.4), rot=(math.radians(90), 0, 0), segs=24)
    soft_box((0, -0.08, 0.5), (0.08, 0.02, 0.36), (0.75, 0.55, 0.12), round_=0.3)


def build_corn():
    """Maiskolben (nur für Symbole)."""
    ball((0, 0, 0.55), (0.32, 0.32, 0.8), (1.0, 0.82, 0.2), subsurf=2)
    for s_ in (-1, 1):
        ball((0.12 * s_, -0.02, 0.4), (0.12, 0.2, 0.7), LEAF, rot=(0, math.radians(18 * s_), 0))


def build_milk():
    """Milchflasche (nur für Symbole)."""
    tube((0, 0, 0.42), 0.24, 0.62, WHITE, segs=20)
    tube((0, 0, 0.84), 0.12, 0.24, WHITE, r2=0.1, segs=16)
    tube((0, 0, 0.99), 0.12, 0.06, MILK_BLUE, segs=16)
    tube((0, 0, 0.42), 0.245, 0.18, MILK_BLUE, segs=20)


def build_ufo():
    ball((0, 0, 0), (1.7, 1.7, 0.34), METAL, subsurf=2, segs=24)
    ring((0, 0, 0), 0.82, 0.05, DARK_METAL)
    ball((0, 0, 0.14), (0.7, 0.7, 0.5), (0.35, 0.9, 0.55), subsurf=2)
    for i in range(8):
        a = i * math.pi / 4
        ball((math.cos(a) * 0.68, math.sin(a) * 0.68, -0.07), (0.11, 0.11, 0.09), GLOW_RED)


def build_tree():
    tube((0, 0, 0.55), 0.17, 1.1, WOOD)
    for (dx, dy, dz, s_) in [(0, 0, 1.35, 1.1), (-0.35, -0.15, 1.15, 0.75), (0.35, 0.1, 1.2, 0.8), (0.05, 0.3, 1.7, 0.7)]:
        ball((dx, dy, dz), (s_, s_, s_ * 0.92), LEAF, subsurf=1)


# ---------------------------------------------------------------- Hof-Gebäude (Vorderseite nach -Y, in Unity zur Kamera gedreht)

BARN_RED = (0.75, 0.18, 0.13)
ROOF_RED = (0.62, 0.16, 0.12)
ROOF_DARK = (0.32, 0.24, 0.22)
CREAM = (0.95, 0.9, 0.78)
GLASS = (0.55, 0.85, 1.0)
GRASS = (0.4, 0.68, 0.22)
SOIL = (0.45, 0.29, 0.15)


def roof(center, width, depth, color, overhang=0.12):
    """Satteldach mit First entlang X (um 45° gedrehter, abgerundeter Würfel)."""
    side = (depth + overhang * 2) / math.sqrt(2)
    soft_box(center, (width + overhang * 2, side, side), color, round_=0.12, rot=(math.radians(45), 0, 0))


def window(x, y, z, w=0.22, h=0.24):
    soft_box((x, y, z), (w + 0.06, 0.04, h + 0.06), WHITE, round_=0.3)
    soft_box((x, y - 0.015, z), (w, 0.04, h), GLASS, round_=0.3)
    soft_box((x, y - 0.03, z), (0.025, 0.02, h), WHITE, round_=0.3)


def build_farmhouse():
    soft_box((0, 0, 0.1), (2.7, 2.7, 0.2), (0.55, 0.62, 0.35), round_=0.2)
    soft_box((0, 0.1, 0.32), (2.1, 1.7, 0.24), STONE, round_=0.25)
    soft_box((0, 0.1, 0.95), (2.0, 1.6, 1.1), CREAM, round_=0.12)
    roof((0, 0.1, 1.5), 2.0, 1.6, ROOF_RED, 0.18)
    tube((0.6, 0.45, 2.15), 0.13, 0.7, STONE, segs=10)
    ball((0.6, 0.45, 2.55), (0.28, 0.28, 0.18), (0.85, 0.85, 0.85))
    soft_box((0, -0.71, 0.75), (0.42, 0.06, 0.7), DARK_WOOD, round_=0.25)
    ball((0.12, -0.75, 0.75), (0.05, 0.03, 0.05), GOLD, subsurf=0, segs=8)
    for x in (-0.65, 0.65):
        window(x, -0.71, 1.0)
        soft_box((x, -0.75, 0.82), (0.32, 0.12, 0.08), DARK_WOOD, round_=0.3)
        for k in (-0.08, 0.0, 0.08):
            ball((x + k, -0.77, 0.88), (0.08, 0.08, 0.08), (1.0, 0.4, 0.55) if k == 0 else (1.0, 0.85, 0.2))
    # Vordach mit Pfosten
    soft_box((0, -0.95, 1.36), (1.0, 0.5, 0.06), ROOF_DARK, round_=0.3, rot=(math.radians(-12), 0, 0))
    for x in (-0.42, 0.42):
        tube((x, -1.12, 0.82), 0.04, 1.05, WOOD)
    soft_box((0, -0.95, 0.47), (1.1, 0.55, 0.06), WOOD, round_=0.3)
    window(0, -0.71, 1.62, 0.18, 0.18)


def build_cornfield():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), SOIL, round_=0.3)
    for i in range(4):
        for j in range(4):
            x, y = -0.66 + 0.44 * i, -0.66 + 0.44 * j
            h = 0.55 + 0.08 * ((i * 3 + j) % 3)
            tube((x, y, 0.12 + h / 2), 0.03, h, (0.35, 0.62, 0.18), segs=6)
            for s_ in (-1, 1):
                ball((x + 0.08 * s_, y, 0.12 + h * 0.6), (0.18, 0.05, 0.06), LEAF, rot=(0, math.radians(30 * s_), 0))
            ball((x + 0.04, y - 0.03, 0.12 + h * 0.75), (0.08, 0.08, 0.18), (1.0, 0.82, 0.2))
    for (x, y) in [(-0.92, -0.92), (0.92, -0.92), (-0.92, 0.92), (0.92, 0.92)]:
        tube((x, y, 0.25), 0.04, 0.4, WOOD, segs=6)


def build_cowshed():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), (0.5, 0.42, 0.3), round_=0.3)
    soft_box((0, 0.1, 0.6), (1.5, 1.3, 0.95), BARN_RED, round_=0.12)
    roof((0, 0.1, 1.07), 1.5, 1.3, ROOF_DARK, 0.12)
    soft_box((0, -0.56, 0.45), (0.62, 0.06, 0.66), WHITE, round_=0.2)
    soft_box((0, -0.6, 0.45), (0.52, 0.04, 0.56), (0.55, 0.12, 0.1), round_=0.2)
    for a in (35, -35):
        soft_box((0, -0.63, 0.45), (0.06, 0.03, 0.7), WHITE, round_=0.3, rot=(0, math.radians(a), 0))
    window(0, -0.56, 0.98, 0.2, 0.16)
    # Kuh schaut aus dem Tor
    soft_box((0.42, -0.68, 0.45), (0.24, 0.22, 0.22), WHITE, round_=0.4)
    soft_box((0.42, -0.8, 0.4), (0.22, 0.08, 0.12), PINK, round_=0.4)
    ball((0.36, -0.79, 0.52), (0.06, 0.04, 0.06), BLACK, subsurf=0, segs=8)
    ball((0.48, -0.79, 0.52), (0.06, 0.04, 0.06), BLACK, subsurf=0, segs=8)
    tube((-0.6, -0.72, 0.2), 0.18, 0.3, STRAW, rot=(0, math.radians(90), 0))


def build_silo():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), STONE, round_=0.3)
    tube((0, 0.1, 0.95), 0.55, 1.7, (0.86, 0.8, 0.6), segs=20)
    for z in (0.45, 0.95, 1.45):
        ring((0, 0.1, z), 0.56, 0.03, (0.6, 0.5, 0.3))
    ball((0, 0.1, 1.82), (1.14, 1.14, 0.6), (0.62, 0.64, 0.7), subsurf=2)
    for z in range(6):
        soft_box((0, -0.47, 0.3 + z * 0.25), (0.3, 0.04, 0.03), DARK_METAL, round_=0.3)
    for s_ in (-1, 1):
        tube((0.15 * s_, -0.47, 1.0), 0.02, 1.6, DARK_METAL, segs=6)
    soft_box((0.62, -0.42, 0.28), (0.3, 0.3, 0.3), DARK_WOOD, round_=0.3)
    ball((0.62, -0.42, 0.48), (0.22, 0.22, 0.14), (1.0, 0.82, 0.2))


def build_milk_tank():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), STONE, round_=0.3)
    for x in (-0.35, 0.35):
        for y in (-0.25, 0.45):
            tube((x, y, 0.35), 0.05, 0.6, DARK_METAL, segs=8)
    tube((0, 0.1, 0.95), 0.5, 1.2, WHITE, rot=(0, math.radians(90), 0), segs=20)
    for x in (-0.6, 0.6):
        ball((x, 0.1, 0.95), (0.3, 1.0, 1.0), WHITE)
    for x in (-0.3, 0.3):
        ring((x, 0.1, 0.95), 0.505, 0.04, MILK_BLUE, rot=(0, math.radians(90), 0))
    tube((0, 0.1, 1.5), 0.12, 0.16, METAL, segs=10)
    tube((0.55, -0.45, 0.25), 0.15, 0.36, METAL, segs=12)
    tube((0.55, -0.45, 0.47), 0.1, 0.08, METAL, segs=12)


def build_worker_hut():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), (0.5, 0.42, 0.3), round_=0.3)
    soft_box((0, 0.15, 0.48), (1.0, 0.95, 0.72), WOOD, round_=0.15)
    roof((0, 0.15, 0.84), 1.0, 0.95, STRAW, 0.14)
    soft_box((0, -0.34, 0.38), (0.3, 0.05, 0.52), DARK_WOOD, round_=0.3)
    window(0.3, -0.33, 0.58, 0.16, 0.16)
    tube((-0.55, -0.55, 0.5), 0.025, 0.85, WOOD, rot=(0, math.radians(12), 0))
    soft_box((-0.6, -0.55, 0.95), (0.18, 0.03, 0.04), METAL, round_=0.3)
    # Schubkarre
    soft_box((0.5, -0.6, 0.3), (0.34, 0.26, 0.14), (0.3, 0.55, 0.3), round_=0.3)
    tube((0.5, -0.78, 0.18), 0.09, 0.05, BLACK, rot=(0, math.radians(90), 0), segs=10)
    ball((0.5, -0.6, 0.38), (0.24, 0.18, 0.08), STRAW)


def build_smithy():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), STONE, round_=0.3)
    soft_box((0, 0.2, 0.5), (1.4, 1.0, 0.8), (0.58, 0.56, 0.53), round_=0.15)
    roof((0, 0.2, 0.9), 1.4, 1.0, ROOF_DARK, 0.12)
    soft_box((0.42, 0.4, 1.35), (0.3, 0.3, 0.8), (0.45, 0.42, 0.4), round_=0.2)
    ball((0.42, 0.4, 1.8), (0.24, 0.24, 0.2), (1.0, 0.55, 0.15))
    soft_box((0, -0.31, 0.42), (0.5, 0.05, 0.5), (0.2, 0.15, 0.12), round_=0.3)
    ball((0, -0.32, 0.36), (0.36, 0.08, 0.24), (1.0, 0.5, 0.1))
    soft_box((-0.45, -0.6, 0.32), (0.34, 0.16, 0.12), (0.18, 0.18, 0.2), round_=0.3)
    soft_box((-0.45, -0.6, 0.18), (0.14, 0.12, 0.16), (0.18, 0.18, 0.2), round_=0.3)
    ring((0.45, -0.34, 0.95), 0.12, 0.03, GOLD, rot=(math.radians(90), 0, 0))


def build_carpentry():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), (0.5, 0.42, 0.3), round_=0.3)
    soft_box((0, 0.2, 0.5), (1.4, 1.0, 0.8), WOOD, round_=0.15)
    roof((0, 0.2, 0.9), 1.4, 1.0, ROOF_RED, 0.12)
    soft_box((0, -0.31, 0.42), (0.55, 0.05, 0.58), DARK_WOOD, round_=0.3)
    for i in range(3):
        tube((-0.35 + 0.0 * i, -0.62 + 0.0, 0.14 + 0.2 * i if i < 2 else 0.24), 0.1, 0.7, DARK_WOOD, rot=(0, math.radians(90), 0))
    tube((-0.3, -0.62, 0.14), 0.1, 0.7, DARK_WOOD, rot=(0, math.radians(90), 0))
    tube((-0.3, -0.62, 0.32), 0.1, 0.7, DARK_WOOD, rot=(0, math.radians(90), 0))
    soft_box((0.5, -0.6, 0.3), (0.4, 0.26, 0.3), WOOD, round_=0.3)
    soft_box((0.5, -0.72, 0.5), (0.36, 0.02, 0.16), METAL, round_=0.3, rot=(0, math.radians(15), 0))
    window(0.4, -0.31, 0.62, 0.18, 0.18)


def build_tinker_shed():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), STONE, round_=0.3)
    soft_box((0, 0.2, 0.48), (1.3, 1.0, 0.76), (0.45, 0.56, 0.68), round_=0.15)
    roof((0, 0.2, 0.86), 1.3, 1.0, (0.28, 0.33, 0.42), 0.12)
    soft_box((-0.2, -0.31, 0.4), (0.4, 0.05, 0.54), (0.25, 0.3, 0.38), round_=0.3)
    tube((0.6, -0.55, 0.6), 0.08, 1.0, (0.75, 0.5, 0.3), segs=10)
    for z in (0.35, 0.55, 0.75):
        ring((0.6, -0.55, z), 0.11, 0.025, GOLD)
    ball((0.6, -0.55, 1.18), (0.26, 0.26, 0.26), (0.6, 0.92, 1.0))
    ring((0.25, -0.33, 0.7), 0.13, 0.04, METAL, rot=(math.radians(90), 0, 0))
    soft_box((-0.6, -0.6, 0.2), (0.3, 0.3, 0.28), DARK_METAL, round_=0.3)


def build_shop_office():
    soft_box((0, 0, 0.06), (1.9, 1.9, 0.12), (0.5, 0.42, 0.3), round_=0.3)
    soft_box((0, 0.2, 0.5), (1.4, 1.0, 0.8), CREAM, round_=0.15)
    roof((0, 0.2, 0.9), 1.4, 1.0, ROOF_RED, 0.12)
    for i in range(6):
        soft_box((-0.6 + 0.24 * i, -0.48, 0.88), (0.24, 0.42, 0.04), BARN_RED if i % 2 == 0 else WHITE, round_=0.2,
                 rot=(math.radians(-18), 0, 0))
    soft_box((0, -0.31, 0.4), (0.4, 0.05, 0.56), DARK_WOOD, round_=0.3)
    for (x, c) in [(-0.55, (0.95, 0.5, 0.1)), (0.55, (1.0, 0.82, 0.2))]:
        soft_box((x, -0.62, 0.22), (0.34, 0.3, 0.26), WOOD, round_=0.3)
        for k in (-0.08, 0.08):
            ball((x + k, -0.62, 0.4), (0.13, 0.13, 0.13), c)
    soft_box((0, -0.34, 1.12), (0.5, 0.04, 0.16), (0.95, 0.88, 0.6), round_=0.3)


# Name, Bau-Funktion, höchstens Dreiecke
MODELS = [
    ("Kuh", build_cow, 6000),
    ("Gruenling", lambda: build_alien((0.35, 0.72, 0.22), (0.15, 0.38, 0.12), armor=None), 5000),
    ("Flitzer", lambda: build_alien((0.62, 0.38, 0.88), (0.32, 0.16, 0.45), scale=0.85, jet=True, gun=False), 5000),
    ("Brocken", lambda: build_alien((0.38, 0.5, 0.32), (0.18, 0.26, 0.15), scale=1.1, armor=(0.5, 0.52, 0.58), bulky=True), 5500),
    ("Boss", lambda: build_alien((0.8, 0.22, 0.2), (0.4, 0.08, 0.08), scale=1.2, armor=(0.25, 0.25, 0.3), crown=True, bulky=True), 8000),
    ("Schleicher", build_sneaker, 5000),
    ("Sanitaeter", build_medic, 5000),
    ("KuhUfo", build_cow_ufo, 4000),
    ("BauerHeinz", build_heinz, 6000),
    ("Traktor", build_tractor, 4000),
    ("MistgabelBauer", build_farmer, 7000),
    ("HeuballenKatapult", build_catapult, 6000),
    ("EismilchSprueher", build_sprayer, 6000),
    ("Bienenstock", build_beehive, 5000),
    ("Windmuehle", build_windmill, 5000),
    ("Wetterhahn", build_weathercock, 4000),
    ("Hofladen", build_farm_shop, 5000),
    ("Ufo", build_ufo, 5000),
    ("Bauernhaus", build_farmhouse, 8000),
    ("Maisfeld", build_cornfield, 6000),
    ("Kuhstall", build_cowshed, 6000),
    ("Maissilo", build_silo, 5000),
    ("Milchtank", build_milk_tank, 5000),
    ("Knechthuette", build_worker_hut, 5000),
    ("Schmiede", build_smithy, 6000),
    ("Zimmerei", build_carpentry, 6000),
    ("Tueftlerschuppen", build_tinker_shed, 6000),
    ("HofladenKontor", build_shop_office, 6000),
    ("Muenze", build_coin, 2000),
    ("Maiskolben", build_corn, 2000),
    ("Milchflasche", build_milk, 2000),
    ("Baum", build_tree, 2500),
]

for name, builder, max_tris in MODELS:
    reset()
    builder()
    export(name, max_tris)

# ---------------------------------------------------------------- Vorschau aller Figuren (Kontrolle)
if preview:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    mat = bpy.data.materials.new("Vorschau")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    outn = nt.nodes.new("ShaderNodeOutputMaterial")
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    toon = nt.nodes.new("ShaderNodeBsdfToon")
    toon.inputs["Size"].default_value = 0.8
    toon.inputs["Smooth"].default_value = 0.2
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = 0.45
    add = nt.nodes.new("ShaderNodeAddShader")
    nt.links.new(attr.outputs["Color"], toon.inputs["Color"])
    nt.links.new(attr.outputs["Color"], em.inputs["Color"])
    nt.links.new(toon.outputs[0], add.inputs[0])
    nt.links.new(em.outputs[0], add.inputs[1])
    nt.links.new(add.outputs[0], outn.inputs["Surface"])

    x = 0.0
    for name, _, _ in MODELS:
        bpy.ops.import_scene.fbx(filepath=os.path.join(out_dir, name + ".fbx"))
        for o in bpy.context.selected_objects:
            o.data.materials.clear()
            o.data.materials.append(mat)
            width = max(o.dimensions.x, 0.8)
            o.location.x = x + width / 2
            if name == "Ufo":
                o.location.z = 0.4
            x += width + 0.35
    bpy.ops.mesh.primitive_plane_add(size=60, location=(x / 2, 0, 0))
    ground = bpy.context.object
    gm = bpy.data.materials.new("Boden")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.35, 0.62, 0.2, 1)
    ground.data.materials.append(gm)

    world = bpy.data.worlds.new("w")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.62, 0.74, 0.88, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.7
    bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), math.radians(10), math.radians(-30)))
    bpy.context.object.data.energy = 3.5
    bpy.ops.object.camera_add()
    cam = bpy.context.object
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = x + 0.6
    from mathutils import Vector
    target = Vector((x / 2, 0, 0.7))
    cam.location = target + Vector((0, -1, 0.45)).normalized() * 30
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = False
    scene.render.resolution_x = 2400
    scene.render.resolution_y = 520
    scene.view_settings.view_transform = "Standard"
    scene.render.use_freestyle = True
    scene.render.line_thickness_mode = "ABSOLUTE"
    fs = scene.view_layers[0].freestyle_settings
    ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("Linien")
    if ls.linestyle is None:
        ls.linestyle = bpy.data.linestyles.new("Linienstil")
    ls.linestyle.color = (0.16, 0.08, 0.04)
    ls.linestyle.thickness = 2.0
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_crease = False
    scene.render.filepath = preview
    bpy.ops.render.render(write_still=True)
    print("VORSCHAU", preview)
