using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Baut das Spielfeld (Raster, Weg, Weide, Ufo am Eingang) und kennt, welche Felder Weg sind.</summary>
    public class MapView : MonoBehaviour
    {
        // Rasen: nur noch ein leichtes Mäh-Schachbrett, die Textur bringt Halme und Flecken
        static readonly Color GrassA = new Color(0.45f, 0.78f, 0.25f);
        static readonly Color GrassB = new Color(0.42f, 0.75f, 0.235f);
        static readonly Color PathColor = new Color(0.5f, 0.35f, 0.21f);
        static readonly Color StoneA = new Color(0.86f, 0.52f, 0.32f);
        static readonly Color StoneB = new Color(0.93f, 0.64f, 0.4f);
        static readonly Color PastureColor = new Color(0.6f, 0.88f, 0.42f);
        static readonly Color BuildableColor = new Color(0.68f, 1f, 0.5f);
        static readonly Color Wood = new Color(0.55f, 0.36f, 0.20f);
        static readonly Color FieldA = new Color(0.67f, 0.76f, 0.26f);
        static readonly Color FieldB = new Color(0.645f, 0.735f, 0.25f);
        static readonly Color Soil = new Color(0.5f, 0.35f, 0.19f);

        static readonly Color CraterA = new Color(0.52f, 0.55f, 0.45f);
        static readonly Color CraterB = new Color(0.5f, 0.53f, 0.435f);
        static readonly Color CraterSoil = new Color(0.36f, 0.31f, 0.4f);

        bool IsMaisfeld => def.Theme == MapTheme.Maisfeld;
        bool IsCrash => def.Theme == MapTheme.Absturzstelle;
        string ObstacleModel => IsCrash ? "Kristall" : "Maisstaude";
        bool IsHof => def.Theme == MapTheme.Hof;
        static readonly string[] HofModels = { "Kuhstall", "Maissilo", "Milchtank", "Knechthuette", "Schmiede", "Maisfeld" };

        LevelDefinition def;
        readonly HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();
        Renderer[,] tiles;
        Color[,] tileColors;
        GroundTextures.Kind[,] tileKinds;
        Transform ufo;

        public void Build(LevelDefinition definition)
        {
            def = definition;
            ComputePathCells();

            var ground = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(def.Width / 2f, -0.45f, def.Height / 2f),
                new Vector3(def.Width + 12f, 0.5f, def.Height + 12f), Color.white, "Untergrund", outline: false);
            var groundColor = IsCrash ? new Color(0.4f, 0.4f, 0.37f) : IsMaisfeld ? new Color(0.55f, 0.62f, 0.2f) : new Color(0.33f, 0.6f, 0.2f);
            ground.GetComponent<Renderer>().sharedMaterial = GroundTextures.Material(GroundTextures.Kind.Grass, groundColor, def.Width + 12f);
            BuildTrees();

            tiles = new Renderer[def.Width, def.Height];
            tileColors = new Color[def.Width, def.Height];
            tileKinds = new GroundTextures.Kind[def.Width, def.Height];
            for (int x = 0; x < def.Width; x++)
            {
                for (int y = 0; y < def.Height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    Color color;
                    if (pathCells.Contains(cell)) color = PathColor;
                    else if (def.Pasture.Contains(cell)) color = PastureColor;
                    else if (def.IsCorn(cell) && !IsHof) color = IsCrash ? CraterSoil : Soil;
                    else if (IsCrash) color = (x + y) % 2 == 0 ? CraterA : CraterB;
                    else if (IsMaisfeld) color = (x + y) % 2 == 0 ? FieldA : FieldB;
                    else color = (x + y) % 2 == 0 ? GrassA : GrassB;

                    var kind = pathCells.Contains(cell) || (def.IsCorn(cell) && !IsHof) ? GroundTextures.Kind.Dirt
                        : IsMaisfeld && !def.Pasture.Contains(cell) ? GroundTextures.Kind.Field : GroundTextures.Kind.Grass;
                    var tile = CreateTile(cell, color);
                    tile.sharedMaterial = GroundTextures.Material(kind, color);
                    tiles[x, y] = tile;
                    tileColors[x, y] = color;
                    tileKinds[x, y] = kind;
                }
            }

            // Wegstücke außerhalb des Rasters (Eingang der Aliens)
            foreach (var cell in pathCells)
            {
                if (!InBounds(cell)) CreateTile(cell, PathColor).sharedMaterial = GroundTextures.Material(GroundTextures.Kind.Dirt, PathColor);
            }

            // Blumen und Grasbüschel auf freien Rasenfeldern und rund um das Spielfeld
            if (!IsCrash)
            {
                GroundTextures.ScatterDecor(transform, new Rect(0f, 0f, def.Width, def.Height), 70, (def.Id ?? "").GetHashCode(), p =>
                {
                    var c = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                    return InBounds(c) && !pathCells.Contains(c) && !def.IsCorn(c);
                });
                GroundTextures.ScatterDecor(transform, new Rect(-5f, -5f, def.Width + 10f, def.Height + 10f), 120, 99, p =>
                    p.x < -0.3f || p.y < -0.3f || p.x > def.Width + 0.3f || p.y > def.Height + 0.3f);
            }

            BuildCornWalls();
            BuildFence();
            BuildUfo();
        }

        Renderer CreateTile(Vector2Int cell, Color color)
        {
            bool isPath = pathCells.Contains(cell);
            float y = isPath ? -0.13f : -0.1f;
            var go = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(cell.x + 0.5f, y, cell.y + 0.5f),
                new Vector3(1f, 0.2f, 1f), color, $"Feld {cell.x},{cell.y}", outline: false);
            if (isPath) AddPathStones(cell);
            return go.GetComponent<Renderer>();
        }

        /// <summary>Steinplatten auf dem Weg wie in Bloons TD6 (mit Umrisslinie).</summary>
        void AddPathStones(Vector2Int cell)
        {
            var rng = new System.Random(cell.x * 73856093 ^ cell.y * 19349663);
            for (int i = 0; i < 2; i++)
            {
                float t = (float)rng.NextDouble();
                var color = Color.Lerp(StoneA, StoneB, t);
                float ox = (i == 0 ? -0.2f : 0.2f) + ((float)rng.NextDouble() - 0.5f) * 0.12f;
                float oz = ((float)rng.NextDouble() - 0.5f) * 0.4f;
                var stone = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(cell.x + 0.5f + ox, -0.01f, cell.y + 0.5f + oz),
                    new Vector3(0.34f + 0.08f * t, 0.06f, 0.28f), color, "Steinplatte");
                stone.transform.localRotation = Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 40f, 0f);
            }
        }

        void ComputePathCells()
        {
            var wps = def.Waypoints;
            for (int i = 0; i < wps.Length - 1; i++)
            {
                Vector2Int a = wps[i];
                Vector2Int b = wps[i + 1];
                var step = new Vector2Int(System.Math.Sign(b.x - a.x), System.Math.Sign(b.y - a.y));
                var c = a;
                pathCells.Add(c);
                while (c != b)
                {
                    c += step;
                    pathCells.Add(c);
                }
            }
            // Felder der Weide zählen nicht als Weg (dort stehen die Kühe).
            pathCells.RemoveWhere(c => def.Pasture.Contains(c));
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < def.Width && c.y < def.Height;

        public bool IsPath(Vector2Int c) => pathCells.Contains(c);

        /// <summary>Hebt beim Platzieren eines Turms alle bebaubaren Felder hervor.</summary>
        public void ShowBuildable(bool show, System.Func<Vector2Int, bool> canBuild)
        {
            for (int x = 0; x < def.Width; x++)
            {
                for (int y = 0; y < def.Height; y++)
                {
                    bool highlight = show && canBuild(new Vector2Int(x, y));
                    tiles[x, y].sharedMaterial = GroundTextures.Material(highlight ? GroundTextures.Kind.Grass : tileKinds[x, y],
                        highlight ? BuildableColor : tileColors[x, y]);
                }
            }
        }

        void BuildFence()
        {
            var p = def.Pasture;
            var fence = new GameObject("Zaun").transform;
            fence.SetParent(transform, false);

            // Lücke im Zaun, wo der Weg auf die Weide führt
            Vector2Int last = def.Waypoints[def.Waypoints.Length - 1];
            Vector2Int prev = def.Waypoints[def.Waypoints.Length - 2];
            var dir = new Vector2Int(System.Math.Sign(last.x - prev.x), System.Math.Sign(last.y - prev.y));
            Vector2 gap = new Vector2(last.x + 0.5f - dir.x * 0.5f, last.y + 0.5f - dir.y * 0.5f);

            for (int x = p.xMin; x < p.xMax; x++)
            {
                AddRail(fence, new Vector2(x + 0.5f, p.yMin), true, gap);
                AddRail(fence, new Vector2(x + 0.5f, p.yMax), true, gap);
            }
            for (int y = p.yMin; y < p.yMax; y++)
            {
                AddRail(fence, new Vector2(p.xMin, y + 0.5f), false, gap);
                AddRail(fence, new Vector2(p.xMax, y + 0.5f), false, gap);
            }
        }

        static void AddRail(Transform parent, Vector2 mid, bool alongX, Vector2 gap)
        {
            if ((mid - gap).sqrMagnitude < 0.01f) return;
            Vector3 scale = alongX ? new Vector3(1f, 0.06f, 0.05f) : new Vector3(0.05f, 0.06f, 1f);
            Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(mid.x, 0.18f, mid.y), scale, Wood, "Latte");
            Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(mid.x, 0.36f, mid.y), scale, Wood, "Latte");
            Vector2 offset = alongX ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            foreach (var end in new[] { mid - offset, mid + offset })
            {
                Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(end.x, 0.22f, end.y), new Vector3(0.09f, 0.44f, 0.09f), Wood, "Pfosten");
            }
        }

        /// <summary>Maisstauden auf den Labyrinth-Feldern (dort kann man nicht bauen).</summary>
        void BuildCornWalls()
        {
            var rng = new System.Random(7);
            if (IsHof)
            {
                // Überfall auf den Hof: auf jeder gesperrten Fläche steht ein Hof-Gebäude
                for (int i = 0; i < def.Corn.Length; i++)
                {
                    var r = def.Corn[i];
                    var holder = new GameObject("Hof-Gebäude").transform;
                    holder.SetParent(transform, false);
                    holder.localPosition = new Vector3(r.x + r.width / 2f, 0f, r.y + r.height / 2f);
                    ModelLibrary.Spawn(HofModels[i % HofModels.Length], holder, Mathf.Min(r.width, r.height) * 0.95f, ModelLibrary.Fit.Footprint, 180f);
                }
                return;
            }
            for (int x = 0; x < def.Width; x++)
            {
                for (int y = 0; y < def.Height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!def.IsCorn(cell) || pathCells.Contains(cell) || def.Pasture.Contains(cell)) continue;
                    var stalk = new GameObject(ObstacleModel).transform;
                    stalk.SetParent(transform, false);
                    stalk.localPosition = new Vector3(x + 0.5f, -0.02f, y + 0.5f);
                    stalk.localRotation = Quaternion.Euler(0f, rng.Next(0, 4) * 90f + (float)rng.NextDouble() * 20f, 0f);
                    if (ModelLibrary.Spawn(ObstacleModel, stalk, (IsCrash ? 0.8f : 1.05f) + (float)rng.NextDouble() * 0.2f) == null)
                    {
                        Visuals.Prim(PrimitiveType.Cylinder, stalk, new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 0.5f, 0.2f), new Color(0.42f, 0.68f, 0.2f), "Stängel");
                        Visuals.Prim(PrimitiveType.Capsule, stalk, new Vector3(0.1f, 0.6f, 0f), new Vector3(0.12f, 0.2f, 0.12f), new Color(1f, 0.82f, 0.2f), "Kolben");
                    }
                }
            }
        }

        /// <summary>Ein paar Bäume (im Maisfeld: Maisstauden) rund um das Spielfeld.</summary>
        void BuildTrees()
        {
            var rng = new System.Random(def.Id != null ? def.Id.GetHashCode() : 1);
            for (int i = 0; i < (IsMaisfeld ? 40 : IsCrash ? 26 : 18); i++)
            {
                float x, z;
                do
                {
                    x = (float)rng.NextDouble() * (def.Width + 8f) - 4f;
                    z = (float)rng.NextDouble() * (def.Height + 8f) - 4f;
                } while (x > -1.3f && x < def.Width + 1.3f && z > -1.3f && z < def.Height + 1.3f);
                var tree = new GameObject("Baum").transform;
                tree.SetParent(transform, false);
                tree.localPosition = new Vector3(x, -0.2f, z);
                tree.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                if (IsCrash)
                {
                    // Kristalle und Wrackteile des Mutterschiffs
                    if (i % 6 == 0)
                    {
                        tree.localRotation = Quaternion.Euler(25f, (float)rng.NextDouble() * 360f, 10f);
                        ModelLibrary.Spawn("Ufo", tree, 2.2f, ModelLibrary.Fit.Footprint);
                    }
                    else ModelLibrary.Spawn("Kristall", tree, 0.9f + (float)rng.NextDouble() * 0.8f);
                }
                else if (IsMaisfeld && i % 5 != 0) ModelLibrary.Spawn("Maisstaude", tree, 1.2f + (float)rng.NextDouble() * 0.4f);
                else ModelLibrary.Spawn("Baum", tree, 1.8f + (float)rng.NextDouble() * 0.8f);
            }
        }

        void BuildUfo()
        {
            Vector2Int entry = def.Waypoints[0];
            ufo = new GameObject("Ufo").transform;
            ufo.SetParent(transform, false);
            ufo.localPosition = new Vector3(entry.x + 0.5f, 2.2f, entry.y + 0.5f);
            if (ModelLibrary.Spawn("Ufo", ufo, 1.8f, ModelLibrary.Fit.Footprint) != null) return;
            Visuals.Prim(PrimitiveType.Sphere, ufo, Vector3.zero, new Vector3(1.6f, 0.3f, 1.6f), new Color(0.6f, 0.65f, 0.7f), "Untertasse");
            Visuals.Prim(PrimitiveType.Sphere, ufo, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.5f, 0.7f), new Color(0.5f, 0.9f, 1f), "Kuppel");
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Visuals.Prim(PrimitiveType.Sphere, ufo, new Vector3(Mathf.Cos(a) * 0.65f, -0.05f, Mathf.Sin(a) * 0.65f),
                    Vector3.one * 0.12f, new Color(1f, 0.9f, 0.2f), "Lampe");
            }
        }

        void Update()
        {
            if (ufo == null) return;
            ufo.Rotate(0f, 40f * Time.deltaTime, 0f);
            var pos = ufo.localPosition;
            pos.y = 2.2f + Mathf.Sin(Time.time * 1.5f) * 0.15f;
            ufo.localPosition = pos;
        }
    }
}
