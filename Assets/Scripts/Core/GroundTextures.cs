using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Gezeichnete Boden-Texturen im Comic-Stil (Rasen mit Halmen, Erde mit Kieseln).
    /// Werden einmal beim Start per Code erzeugt und sind nahtlos kachelbar. Die Farbe kommt über _Color dazu,
    /// die Textur liefert nur helle/dunkle Abstufungen (Werte um 1).
    /// </summary>
    public static class GroundTextures
    {
        public enum Kind
        {
            Grass,
            Dirt,
            Field,
        }

        const int Size = 128;
        static readonly Dictionary<Kind, Texture2D> textures = new Dictionary<Kind, Texture2D>();
        static readonly Dictionary<(Kind, Color, float), Material> materials = new Dictionary<(Kind, Color, float), Material>();

        public static Texture2D Get(Kind kind)
        {
            if (!textures.TryGetValue(kind, out var tex) || tex == null)
            {
                tex = kind == Kind.Dirt ? MakeDirt(31) : MakeGrass(kind == Kind.Field ? 57 : 17);
                textures[kind] = tex;
            }
            return tex;
        }

        /// <summary>Toon-Material mit Boden-Textur. <paramref name="tiling"/> = wie oft die Textur auf die Fläche passt.</summary>
        public static Material Material(Kind kind, Color color, float tiling = 1f)
        {
            if (materials.TryGetValue((kind, color, tiling), out var mat) && mat != null) return mat;
            mat = new Material(Visuals.Mat(color, false));
            mat.mainTexture = Get(kind);
            mat.mainTextureScale = new Vector2(tiling, tiling);
            materials[(kind, color, tiling)] = mat;
            return mat;
        }

        // ---------------------------------------------------------------- Rasen

        static Texture2D MakeGrass(int seed)
        {
            var rng = new System.Random(seed);
            var px = new Color[Size * Size];
            var noiseA = new TileNoise(rng, 4);
            var noiseB = new TileNoise(rng, 9);
            var dark = new Color(0.80f, 0.88f, 0.76f);
            var light = new Color(1f, 1f, 0.9f);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float n = noiseA.Sample(u, v) * 0.65f + noiseB.Sample(u, v) * 0.35f;
                    // weiche Flecken in Comic-Abstufungen statt Verlauf
                    n = Mathf.Round(n * 4f) / 4f * 0.6f + n * 0.4f;
                    px[y * Size + x] = Color.Lerp(dark, light, n);
                }
            }

            // Grashalme: kleine, leicht schräge Striche, dunkel mit heller Spitze
            for (int i = 0; i < 260; i++)
            {
                int bx = rng.Next(Size), by = rng.Next(Size);
                int len = 4 + rng.Next(5);
                float lean = (float)(rng.NextDouble() - 0.5) * 0.8f;
                bool lightBlade = rng.NextDouble() < 0.4;
                var col = lightBlade ? new Color(1f, 1f, 0.82f) : new Color(0.62f, 0.76f, 0.56f);
                for (int k = 0; k < len; k++)
                {
                    int x = bx + Mathf.RoundToInt(lean * k);
                    int y = by + k;
                    float tip = k / (float)len;
                    Blend(px, x, y, col, 0.75f - tip * 0.35f);
                    if (k < len / 2) Blend(px, x + 1, y, col, 0.4f);
                }
            }

            return Build(px, "Rasen");
        }

        // ---------------------------------------------------------------- Erde

        static Texture2D MakeDirt(int seed)
        {
            var rng = new System.Random(seed);
            var px = new Color[Size * Size];
            var noiseA = new TileNoise(rng, 5);
            var noiseB = new TileNoise(rng, 12);
            var dark = new Color(0.82f, 0.78f, 0.74f);
            var light = new Color(1f, 0.97f, 0.92f);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float n = noiseA.Sample(u, v) * 0.6f + noiseB.Sample(u, v) * 0.4f;
                    px[y * Size + x] = Color.Lerp(dark, light, n);
                }
            }
            // Kiesel: dunkler Rand, helle Mitte
            for (int i = 0; i < 22; i++)
            {
                int cx = rng.Next(Size), cy = rng.Next(Size);
                int r = 1 + rng.Next(3);
                Dot(px, cx, cy, r + 1, new Color(0.66f, 0.6f, 0.56f));
                Dot(px, cx, cy, r, new Color(1.08f, 1.04f, 1f));
            }
            return Build(px, "Erde");
        }

        // ---------------------------------------------------------------- Deko: Blumen und Grasbüschel

        /// <summary>
        /// Verstreut kleine Blumen und Grasbüschel (ohne Umriss, damit sie den Boden nur beleben).
        /// <paramref name="isFree"/> entscheidet, ob an einer Stelle Platz ist.
        /// </summary>
        public static void ScatterDecor(Transform parent, Rect area, int count, int seed, System.Func<Vector2, bool> isFree)
        {
            var rng = new System.Random(seed);
            var holder = new GameObject("Blumen und Gras").transform;
            holder.SetParent(parent, false);
            Color[] petals =
            {
                new Color(1f, 1f, 0.97f), new Color(1f, 0.9f, 0.3f), new Color(1f, 0.55f, 0.7f), new Color(0.65f, 0.6f, 1f),
            };
            var tuft = new Color(0.32f, 0.62f, 0.2f);
            for (int i = 0; i < count; i++)
            {
                var p = new Vector2(area.x + (float)rng.NextDouble() * area.width, area.y + (float)rng.NextDouble() * area.height);
                if (!isFree(p)) continue;
                var pos = new Vector3(p.x, 0f, p.y);
                if (rng.NextDouble() < 0.45)
                {
                    // Blume: Stängel, 4 Blütenblätter, gelbe Mitte
                    var c = petals[rng.Next(petals.Length)];
                    Visuals.Prim(PrimitiveType.Cylinder, holder, pos + new Vector3(0f, 0.05f, 0f), new Vector3(0.015f, 0.05f, 0.015f), tuft, "Stängel", outline: false);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI / 2f + (float)rng.NextDouble();
                        Visuals.Prim(PrimitiveType.Sphere, holder, pos + new Vector3(Mathf.Cos(a) * 0.035f, 0.1f, Mathf.Sin(a) * 0.035f),
                            new Vector3(0.05f, 0.02f, 0.05f), c, "Blüte", outline: false);
                    }
                    Visuals.Prim(PrimitiveType.Sphere, holder, pos + new Vector3(0f, 0.11f, 0f), Vector3.one * 0.035f, new Color(1f, 0.75f, 0.15f), "Mitte", outline: false);
                }
                else
                {
                    // Grasbüschel: drei schräge Halme
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * 2.1f + (float)rng.NextDouble();
                        var blade = Visuals.Prim(PrimitiveType.Cube, holder, pos + new Vector3(Mathf.Cos(a) * 0.03f, 0.06f, Mathf.Sin(a) * 0.03f),
                            new Vector3(0.02f, 0.12f + 0.05f * (float)rng.NextDouble(), 0.02f),
                            Color.Lerp(tuft, new Color(0.5f, 0.8f, 0.3f), (float)rng.NextDouble()), "Halm", outline: false);
                        blade.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 25f, 0f, -Mathf.Cos(a) * 25f);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- Hilfen

        static void Blend(Color[] px, int x, int y, Color c, float a)
        {
            x = ((x % Size) + Size) % Size;
            y = ((y % Size) + Size) % Size;
            int i = y * Size + x;
            px[i] = Color.Lerp(px[i], px[i] * c, a);
        }

        static void Dot(Color[] px, int cx, int cy, int r, Color c)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r + r) continue;
                    int x = ((cx + dx) % Size + Size) % Size;
                    int y = ((cy + dy) % Size + Size) % Size;
                    px[y * Size + x] = c * 0.75f;
                }
            }
        }

        static Texture2D Build(Color[] px, string name)
        {
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                px[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = name };
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 2;
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Kachelbares Wertrauschen (Gitter wird an den Rändern fortgesetzt, daher keine Nähte).</summary>
        class TileNoise
        {
            readonly float[,] grid;
            readonly int cells;

            public TileNoise(System.Random rng, int cells)
            {
                this.cells = cells;
                grid = new float[cells, cells];
                for (int y = 0; y < cells; y++)
                {
                    for (int x = 0; x < cells; x++) grid[x, y] = (float)rng.NextDouble();
                }
            }

            public float Sample(float u, float v)
            {
                float fx = u * cells, fy = v * cells;
                int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                float tx = Smooth(fx - x0), ty = Smooth(fy - y0);
                float a = At(x0, y0), b = At(x0 + 1, y0), c = At(x0, y0 + 1), d = At(x0 + 1, y0 + 1);
                return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
            }

            float At(int x, int y) => grid[((x % cells) + cells) % cells, ((y % cells) + cells) % cells];

            static float Smooth(float t) => t * t * (3f - 2f * t);
        }
    }
}
