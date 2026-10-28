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
        public bool GhostValid { get; private set; }
        public bool ShopOpen;
        public bool MapOpen;

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
            gameObject.AddComponent<FarmUI>().Init(this);
        }

        void Update()
        {
            foreach (var b in Farm.Tick())
            {
                if (Views.TryGetValue(b.Id, out var view))
                {
                    view.Refresh();
                    AddFloatingText(view.TopPoint, $"{b.TypeDef.Name} fertig!", UiKit.Gold);
                }
                Farm.Save();
            }

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
            if (ShopOpen || MapOpen) return;
            var cell = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.z));

            if (Mode != FarmMode.Normal)
            {
                int size = Mode == FarmMode.Placing ? PlacingType.Size : Selected.TypeDef.Size;
                int offset = (size - 1) / 2;
                MoveGhost(new Vector2Int(
                    Mathf.Clamp(cell.x - offset, 0, FarmState.Width - size),
                    Mathf.Clamp(cell.y - offset, 0, FarmState.Height - size)));
                return;
            }

            var b = Farm.BuildingAt(cell);
            Select(b);
            if (b != null && b.TypeDef.IsProducer && b.Stored >= 1) Collect(b);
        }

        public void Select(BuildingData b)
        {
            if (Selected != null && Views.TryGetValue(Selected.Id, out var oldView)) oldView.SetSelected(false);
            Selected = b;
            if (Selected != null && Views.TryGetValue(Selected.Id, out var newView)) newView.SetSelected(true);
        }

        // ---------------------------------------------------------------- Aktionen

        public void Collect(BuildingData b)
        {
            var type = b.TypeDef;
            double collected = Farm.Collect(b);
            var view = Views[b.Id];
            if (collected >= 1)
            {
                AddFloatingText(view.TopPoint, $"+{(int)collected} {FarmBuildingType.ResourceName(type.ProducedResource)}",
                    type.ProducedResource == Resource.Mais ? UiKit.CornColor : UiKit.MilkColor);
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

        public void PlayLevel(LevelDefinition def)
        {
            Farm.Save();
            GameFlow.StartLevel(def);
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

        void BuildGround()
        {
            int w = FarmState.Width, h = FarmState.Height;
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(w / 2f, -0.45f, h / 2f), new Vector3(w + 14f, 0.5f, h + 14f),
                new Color(0.30f, 0.52f, 0.22f), "Untergrund");

            var grassA = new Color(0.45f, 0.72f, 0.32f);
            var grassB = new Color(0.42f, 0.68f, 0.30f);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(x + 0.5f, -0.1f, y + 0.5f), new Vector3(1f, 0.2f, 1f),
                        (x + y) % 2 == 0 ? grassA : grassB, "Wiese");
                }
            }

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
                tree.localScale = Vector3.one * scale;
                Visuals.Prim(PrimitiveType.Cylinder, tree, new Vector3(0f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.18f), new Color(0.45f, 0.3f, 0.18f), "Stamm");
                Visuals.Prim(PrimitiveType.Sphere, tree, new Vector3(0f, 1.1f, 0f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.22f, 0.5f, 0.2f), "Krone");
            }
        }
    }
}
