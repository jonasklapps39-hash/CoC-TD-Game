using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    public enum FarmMode
    {
        Normal,
        /// <summary>Ein neues Gebäude wird platziert.</summary>
        Placing,
        /// <summary>Ein vorhandenes Gebäude wird verschoben.</summary>
        Moving,
    }

    /// <summary>Steuert die Hof-Ansicht: Gebäude anzeigen, auswählen, bauen, verbessern, verschieben und Rohstoffe einsammeln.</summary>
    public class FarmManager : MonoBehaviour
    {
        const float AutoSaveInterval = 10f;

        public FarmState Farm { get; private set; }
        public FarmMode Mode { get; private set; } = FarmMode.Normal;
        public FarmBuildingType PlacingType { get; private set; }
        public BuildingData Selected { get; private set; }
        /// <summary>Ausgewähltes Hindernis (Stein, Busch …), oder null.</summary>
        public ObstacleData SelectedObstacle { get; private set; }
        readonly Dictionary<int, GameObject> obstacleViews = new Dictionary<int, GameObject>();
        float obstacleSyncTimer;
        public bool GhostValid { get; private set; }
        public bool ShopOpen;
        public bool MapOpen;
        public bool TalentsOpen;
        public bool GemShopOpen;
        public bool HeroesOpen;
        public bool QuestsOpen;
        /// <summary>Irgendein Fenster (Bauen, Weltkarte, Talente, Shop …) ist offen.</summary>
        public bool AnyWindowOpen => ShopOpen || MapOpen || TalentsOpen || GemShopOpen || HeroesOpen || QuestsOpen;

        public void CloseWindows()
        {
            ShopOpen = false;
            MapOpen = false;
            TalentsOpen = false;
            GemShopOpen = false;
            HeroesOpen = false;
            QuestsOpen = false;
        }

        public void InstantFinishSelected()
        {
            if (Selected == null || !Farm.InstantFinish(Selected)) return;
            GameAudio.Play(SoundId.Coin);
            Farm.Save();
        }

        public readonly Dictionary<int, FarmBuildingView> Views = new Dictionary<int, FarmBuildingView>();
        public readonly List<FloatingText> FloatingTexts = new List<FloatingText>();

        Vector2Int ghostCell;
        Transform ghost;
        GameObject ghostMarker;
        float saveTimer;

        public void Init(FarmState farm)
        {
            Farm = farm;
            Farm.Tick();
            BuildGround();
            foreach (var b in Farm.Buildings) CreateView(b);
            SyncObstacles();
            gameObject.AddComponent<FarmAnimals>().Init(Farm);
            gameObject.AddComponent<FarmUI>().Init(this);
            GameAudio.PlayMusic(MusicId.Farm);
        }

        void Update()
        {
            foreach (var b in Farm.Tick())
            {
                if (Views.TryGetValue(b.Id, out var view))
                {
                    view.Refresh();
                    AddFloatingText(view.TopPoint, $"{b.TypeDef.Name} fertig!", UiKit.Gold);
                    GameAudio.Play(SoundId.LevelUp);
                    Effects.Burst(transform, view.TopPoint, UiKit.Gold, 12, 3f);
                }
                Farm.Save();
            }

            // Ereignisse aus dem Spielstand anzeigen (Hindernis weg, Erfahrung, Bauernstufe)
            if (Farm.Events.Count > 0)
            {
                foreach (var e in Farm.Events)
                {
                    AddFloatingText(e.Position, e.Text, e.Color);
                    Effects.Burst(transform, e.Position, e.Color, 6, 2.5f);
                }
                Farm.Events.Clear();
                Farm.Save();
            }
            if (Farm.LevelUpPending)
            {
                Farm.LevelUpPending = false;
                GameAudio.Play(SoundId.Win);
                Effects.Burst(transform, new Vector3(FarmState.Width / 2f, 2f, FarmState.Height / 2f), UiKit.Gold, 40, 5f, 0.12f);
            }

            obstacleSyncTimer -= Time.deltaTime;
            if (obstacleSyncTimer <= 0f)
            {
                obstacleSyncTimer = 0.5f;
                SyncObstacles();
            }
            AnimateClearing();

            for (int i = FloatingTexts.Count - 1; i >= 0; i--)
            {
                FloatingTexts[i].Age += Time.deltaTime;
                if (FloatingTexts[i].Age > LevelManager.FloatingTextLifetime) FloatingTexts.RemoveAt(i);
            }

            saveTimer += Time.deltaTime;
            if (saveTimer >= AutoSaveInterval)
            {
                saveTimer = 0f;
                Farm.Save();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Farm.Save();
        }

        void OnApplicationQuit()
        {
            Farm.Save();
        }

        // ---------------------------------------------------------------- Eingabe

        public void HandleClick(Vector3 world)
        {
            if (AnyWindowOpen) return;
            var cell = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.z));

            if (Mode != FarmMode.Normal)
            {
                DragGhostTo(world);
                return;
            }

            var b = Farm.BuildingAt(cell);
            Select(b);
            if (b != null && b.TypeDef.IsProducer && b.Stored >= 1) Collect(b);
            if (b == null)
            {
                SelectedObstacle = Farm.ObstacleAt(cell);
                if (SelectedObstacle != null) GameAudio.Play(SoundId.Click);
            }
        }

        // ---------------------------------------------------------------- Hindernisse

        public void ClearSelectedObstacle()
        {
            if (SelectedObstacle == null || !Farm.StartClearing(SelectedObstacle)) return;
            GameAudio.Play(SoundId.Build);
            Farm.Save();
        }

        public void DeselectObstacle() => SelectedObstacle = null;

        void SyncObstacles()
        {
            var alive = new HashSet<int>();
            foreach (var o in Farm.Data.Obstacles)
            {
                alive.Add(o.Id);
                if (!obstacleViews.ContainsKey(o.Id)) obstacleViews[o.Id] = ObstacleModels.Build(o, transform);
            }
            var gone = new List<int>();
            foreach (var kv in obstacleViews)
            {
                if (alive.Contains(kv.Key)) continue;
                Effects.Burst(transform, kv.Value.transform.position + Vector3.up * 0.3f, new Color(0.6f, 0.45f, 0.3f), 10, 2.5f);
                Destroy(kv.Value);
                gone.Add(kv.Key);
            }
            foreach (var id in gone)
            {
                obstacleViews.Remove(id);
                if (SelectedObstacle != null && SelectedObstacle.Id == id) SelectedObstacle = null;
            }
        }

        /// <summary>Hindernisse, die gerade weggeräumt werden, wackeln.</summary>
        void AnimateClearing()
        {
            foreach (var o in Farm.Data.Obstacles)
            {
                if (!obstacleViews.TryGetValue(o.Id, out var view)) continue;
                float wobble = o.IsClearing ? Mathf.Sin(Time.time * 18f + o.Id) * 4f : 0f;
                var e = view.transform.localEulerAngles;
                view.transform.localRotation = Quaternion.Euler(wobble, e.y, 0f);
            }
        }

        public Vector3 ObstacleTop(ObstacleData o) => new Vector3(o.X + 0.5f, 0.8f, o.Y + 0.5f);

        /// <summary>Sammelt alle Felder und Ställe auf einmal ein.</summary>
        public void CollectAll()
        {
            foreach (var b in Farm.Data.Buildings)
            {
                if (b.TypeDef != null && b.TypeDef.IsProducer && b.Level > 0 && b.Stored >= 1) Collect(b);
            }
        }

        /// <summary>Vorschau beim Platzieren oder Verschieben unter den Finger setzen (auch beim Ziehen).</summary>
        public void DragGhostTo(Vector3 world)
        {
            if (Mode == FarmMode.Normal) return;
            var cell = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.z));
            int size = Mode == FarmMode.Placing ? PlacingType.Size : Selected.TypeDef.Size;
            int offset = (size - 1) / 2;
            var target = new Vector2Int(
                Mathf.Clamp(cell.x - offset, 0, FarmState.Width - size),
                Mathf.Clamp(cell.y - offset, 0, FarmState.Height - size));
            if (target != ghostCell) MoveGhost(target);
        }

        public void Select(BuildingData b)
        {
            SelectedObstacle = null;
            if (Selected != null && Views.TryGetValue(Selected.Id, out var oldView)) oldView.SetSelected(false);
            Selected = b;
            if (Selected != null && Views.TryGetValue(Selected.Id, out var newView)) newView.SetSelected(true);
        }

        // ---------------------------------------------------------------- Aktionen

        public void Collect(BuildingData b)
        {
            var type = b.TypeDef;
            double collected = Farm.Collect(b);
            Farm.Count(Counter.Collected, (int)collected);
            var view = Views[b.Id];
            if (collected >= 1)
            {
                AddFloatingText(view.TopPoint, $"+{(int)collected} {FarmBuildingType.ResourceName(type.ProducedResource)}",
                    type.ProducedResource == Resource.Mais ? UiKit.CornColor : UiKit.MilkColor);
                GameAudio.Play(type.ProducedResource == Resource.Milch ? SoundId.Moo : SoundId.Coin);
                Effects.Burst(transform, view.TopPoint, type.ProducedResource == Resource.Mais ? UiKit.CornColor : UiKit.MilkColor, 8, 2.5f);
                Farm.Save();
            }
            else
            {
                AddFloatingText(view.TopPoint, "Lager voll!", UiKit.BadColor);
            }
        }

        public void UpgradeSelected()
        {
            if (Selected == null || !Farm.StartUpgrade(Selected)) return;
            GameAudio.Play(SoundId.Build);
            Views[Selected.Id].Refresh();
            Farm.Save();
        }

        public void BeginPlacing(FarmBuildingType type)
        {
            if (Farm.WhyCannotBuild(type) != null) return;
            ShopOpen = false;
            Select(null);
            Mode = FarmMode.Placing;
            PlacingType = type;
            CreateGhost(type);
            MoveGhost(FindFreeSpot(type.Size));
        }

        public void BeginMoving()
        {
            if (Selected == null) return;
            Mode = FarmMode.Moving;
            CreateGhost(Selected.TypeDef);
            MoveGhost(new Vector2Int(Selected.X, Selected.Y));
        }

        public void ConfirmPlacement()
        {
            if (!GhostValid) return;
            if (Mode == FarmMode.Placing)
            {
                var b = Farm.Build(PlacingType, ghostCell.x, ghostCell.y);
                if (b != null)
                {
                    CreateView(b);
                    GameAudio.Play(SoundId.Build);
                    CancelMode();
                    Select(b);
                    Farm.Save();
                    return;
                }
            }
            else if (Mode == FarmMode.Moving && Selected != null)
            {
                if (Farm.Move(Selected, ghostCell.x, ghostCell.y))
                {
                    Views[Selected.Id].Refresh();
                    Farm.Save();
                }
            }
            CancelMode();
        }

        public void CancelMode()
        {
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            Mode = FarmMode.Normal;
            PlacingType = null;
        }

        public void PlayLevel(LevelDefinition def, Difficulty difficulty = Difficulty.Mittel)
        {
            Farm.Save();
            GameFlow.StartLevel(def, difficulty);
        }

        public void AddFloatingText(Vector3 position, string text, Color color)
        {
            FloatingTexts.Add(new FloatingText { Position = position, Text = text, Color = color });
        }

        // ---------------------------------------------------------------- Vorschau beim Platzieren

        void CreateGhost(FarmBuildingType type)
        {
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = new GameObject("Vorschau").transform;
            ghost.SetParent(transform, false);
            FarmModels.Build(type, 1, ghost);
        }

        void MoveGhost(Vector2Int cell)
        {
            ghostCell = cell;
            int size = Mode == FarmMode.Placing ? PlacingType.Size : Selected.TypeDef.Size;
            int ignore = Mode == FarmMode.Moving ? Selected.Id : -1;
            GhostValid = Farm.IsAreaFree(cell.x, cell.y, size, ignore);

            ghost.position = new Vector3(cell.x + size / 2f, 0.05f, cell.y + size / 2f);
            if (ghostMarker != null) Destroy(ghostMarker);
            ghostMarker = Visuals.Prim(PrimitiveType.Cube, ghost, new Vector3(0f, 0.01f, 0f), new Vector3(size, 0.04f, size),
                GhostValid ? new Color(0.3f, 0.95f, 0.3f) : new Color(0.95f, 0.25f, 0.2f), "Markierung");
        }

        Vector2Int FindFreeSpot(int size)
        {
            var center = new Vector2Int(FarmState.Width / 2, FarmState.Height / 2);
            for (int radius = 0; radius < FarmState.Width; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        int x = center.x + dx, y = center.y + dy;
                        if (Farm.IsAreaFree(x, y, size, -1)) return new Vector2Int(x, y);
                    }
                }
            }
            return new Vector2Int(0, 0);
        }

        // ---------------------------------------------------------------- Aufbau

        void CreateView(BuildingData b)
        {
            var view = new GameObject("Gebäude").AddComponent<FarmBuildingView>();
            view.transform.SetParent(transform, false);
            view.Init(b);
            Views[b.Id] = view;
        }

        /// <summary>Schachbrett-Wiese als ein einziges Mesh (viel schneller als ein Objekt pro Feld).</summary>
        void BuildMeadow(int w, int h)
        {
            // nur noch ein leichtes Mäh-Schachbrett, die Rasen-Textur bringt Halme und Flecken
            var grassA = new Color(0.5f, 0.8f, 0.34f);
            var grassB = new Color(0.475f, 0.77f, 0.325f);
            var vertices = new List<Vector3>(w * h * 4);
            var uvs = new List<Vector2>(w * h * 4);
            var colors = new List<Color>(w * h * 4);
            var normals = new List<Vector3>(w * h * 4);
            var triangles = new List<int>(w * h * 6);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int i = vertices.Count;
                    vertices.Add(new Vector3(x, 0f, y));
                    vertices.Add(new Vector3(x, 0f, y + 1));
                    vertices.Add(new Vector3(x + 1, 0f, y + 1));
                    vertices.Add(new Vector3(x + 1, 0f, y));
                    uvs.Add(new Vector2(x, y));
                    uvs.Add(new Vector2(x, y + 1));
                    uvs.Add(new Vector2(x + 1, y + 1));
                    uvs.Add(new Vector2(x + 1, y));
                    var c = (x + y) % 2 == 0 ? grassA : grassB;
                    for (int k = 0; k < 4; k++)
                    {
                        colors.Add(c);
                        normals.Add(Vector3.up);
                    }
                    triangles.Add(i);
                    triangles.Add(i + 1);
                    triangles.Add(i + 2);
                    triangles.Add(i);
                    triangles.Add(i + 2);
                    triangles.Add(i + 3);
                }
            }
            var mesh = new Mesh { name = "Wiese" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            var go = new GameObject("Wiese");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(Visuals.VertexColorMaterial);
            mat.SetFloat("_OutlineWidth", 0f);
            mat.mainTexture = GroundTextures.Get(GroundTextures.Kind.Grass);
            mr.sharedMaterial = mat;

            // Blumen und Grasbüschel rund um den Hof (innen bleibt Platz für Gebäude)
            GroundTextures.ScatterDecor(transform, new Rect(-6f, -6f, w + 12f, h + 12f), 160, 5, p =>
                p.x < -0.4f || p.y < -0.4f || p.x > w + 0.4f || p.y > h + 0.4f);
        }

        void BuildGround()
        {
            int w = FarmState.Width, h = FarmState.Height;
            var ground = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(w / 2f, -0.45f, h / 2f), new Vector3(w + 14f, 0.5f, h + 14f),
                Color.white, "Untergrund", outline: false);
            ground.GetComponent<Renderer>().sharedMaterial = GroundTextures.Material(GroundTextures.Kind.Grass, new Color(0.34f, 0.62f, 0.21f), w + 14f);

            BuildMeadow(w, h);

            // Zaun rund um den Hof
            var wood = new Color(0.55f, 0.36f, 0.20f);
            var fence = new GameObject("Zaun").transform;
            fence.SetParent(transform, false);
            for (int x = 0; x <= w; x++)
            {
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(x, 0.2f, -0.15f), new Vector3(0.08f, 0.4f, 0.08f), wood, "Pfosten");
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(x, 0.2f, h + 0.15f), new Vector3(0.08f, 0.4f, 0.08f), wood, "Pfosten");
            }
            for (int y = 0; y <= h; y++)
            {
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(-0.15f, 0.2f, y), new Vector3(0.08f, 0.4f, 0.08f), wood, "Pfosten");
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(w + 0.15f, 0.2f, y), new Vector3(0.08f, 0.4f, 0.08f), wood, "Pfosten");
            }
            foreach (float yy in new[] { 0.15f, 0.3f })
            {
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(w / 2f, yy, -0.15f), new Vector3(w, 0.05f, 0.04f), wood, "Latte");
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(w / 2f, yy, h + 0.15f), new Vector3(w, 0.05f, 0.04f), wood, "Latte");
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(-0.15f, yy, h / 2f), new Vector3(0.04f, 0.05f, h), wood, "Latte");
                Visuals.Prim(PrimitiveType.Cube, fence, new Vector3(w + 0.15f, yy, h / 2f), new Vector3(0.04f, 0.05f, h), wood, "Latte");
            }

            // Ein paar Bäume außerhalb des Zauns
            var rng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                float x, z;
                do
                {
                    x = (float)rng.NextDouble() * (w + 10f) - 5f;
                    z = (float)rng.NextDouble() * (h + 10f) - 5f;
                } while (x > -1.2f && x < w + 1.2f && z > -1.2f && z < h + 1.2f);
                var tree = new GameObject("Baum").transform;
                tree.SetParent(transform, false);
                tree.localPosition = new Vector3(x, 0f, z);
                float scale = 0.8f + (float)rng.NextDouble() * 0.6f;
                if (ModelLibrary.Spawn("Baum", tree, 1.9f * scale) != null) continue;
                tree.localScale = Vector3.one * scale;
                Visuals.Prim(PrimitiveType.Cylinder, tree, new Vector3(0f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.18f), new Color(0.45f, 0.3f, 0.18f), "Stamm");
                Visuals.Prim(PrimitiveType.Sphere, tree, new Vector3(0f, 1.1f, 0f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.22f, 0.5f, 0.2f), "Krone");
            }
        }
    }
}
