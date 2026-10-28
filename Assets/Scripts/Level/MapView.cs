using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Baut das Spielfeld (Raster, Weg, Weide, Ufo am Eingang) und kennt, welche Felder Weg sind.</summary>
    public class MapView : MonoBehaviour
    {
        static readonly Color GrassA = new Color(0.40f, 0.72f, 0.22f);
        static readonly Color GrassB = new Color(0.37f, 0.68f, 0.20f);
        static readonly Color PathColor = new Color(0.42f, 0.29f, 0.18f);
        static readonly Color StoneA = new Color(0.86f, 0.52f, 0.32f);
        static readonly Color StoneB = new Color(0.93f, 0.64f, 0.4f);
        static readonly Color PastureColor = new Color(0.56f, 0.82f, 0.40f);
        static readonly Color BuildableColor = new Color(0.62f, 0.93f, 0.48f);
        static readonly Color Wood = new Color(0.55f, 0.36f, 0.20f);

        LevelDefinition def;
        readonly HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();
        Renderer[,] tiles;
        Color[,] tileColors;
        Transform ufo;

        public void Build(LevelDefinition definition)
        {
            def = definition;
            ComputePathCells();

            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(def.Width / 2f, -0.45f, def.Height / 2f),
                new Vector3(def.Width + 12f, 0.5f, def.Height + 12f), new Color(0.30f, 0.55f, 0.18f), "Untergrund", outline: false);
            BuildTrees();

            tiles = new Renderer[def.Width, def.Height];
            tileColors = new Color[def.Width, def.Height];
            for (int x = 0; x < def.Width; x++)
            {
                for (int y = 0; y < def.Height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    Color color;
                    if (pathCells.Contains(cell)) color = PathColor;
                    else if (def.Pasture.Contains(cell)) color = PastureColor;
                    else color = (x + y) % 2 == 0 ? GrassA : GrassB;

                    var tile = CreateTile(cell, color);
                    tiles[x, y] = tile;
                    tileColors[x, y] = color;
                }
            }

            // Wegstücke außerhalb des Rasters (Eingang der Aliens)
            foreach (var cell in pathCells)
            {
                if (!InBounds(cell)) CreateTile(cell, PathColor);
            }

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
                    tiles[x, y].sharedMaterial = Visuals.Mat(highlight ? BuildableColor : tileColors[x, y], false);
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

        /// <summary>Ein paar Bäume rund um das Spielfeld.</summary>
        void BuildTrees()
        {
            var rng = new System.Random(def.Id != null ? def.Id.GetHashCode() : 1);
            for (int i = 0; i < 18; i++)
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
                ModelLibrary.Spawn("Baum", tree, 1.8f + (float)rng.NextDouble() * 0.8f);
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
