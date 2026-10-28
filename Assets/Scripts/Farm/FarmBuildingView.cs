using UnityEngine;

namespace MuhDefense
{
    /// <summary>Darstellung eines Hof-Gebäudes (Platzhalter aus Grundkörpern), inklusive Baugerüst und Stufen-Anzeige.</summary>
    public class FarmBuildingView : MonoBehaviour
    {
        public BuildingData Data { get; private set; }

        int shownLevel = -1;
        bool shownUpgrading;
        Transform model;
        LineRenderer selection;

        public int Size => Data.TypeDef.Size;
        public Vector3 Center => new Vector3(Data.X + Size / 2f, 0f, Data.Y + Size / 2f);
        /// <summary>Punkt über dem Gebäude für Anzeigen (Blase, Baufortschritt).</summary>
        public Vector3 TopPoint => Center + Vector3.up * (Size == 3 ? 2.4f : 1.9f);

        public void Init(BuildingData data)
        {
            Data = data;
            name = data.TypeDef.Name;
            selection = Visuals.Circle(transform, Size * 0.78f, new Color(1f, 1f, 1f, 0.9f), 0.06f);
            selection.enabled = false;
            Refresh();
        }

        public void SetSelected(bool selected) => selection.enabled = selected;

        /// <summary>Position und Modell an die Daten anpassen (nach Bau, Upgrade oder Verschieben).</summary>
        public void Refresh()
        {
            transform.position = Center;
            if (shownLevel == Data.Level && shownUpgrading == Data.IsUpgrading) return;
            shownLevel = Data.Level;
            shownUpgrading = Data.IsUpgrading;

            if (model != null) Destroy(model.gameObject);
            model = new GameObject("Modell").transform;
            model.SetParent(transform, false);

            if (Data.Level == 0)
            {
                BuildConstructionSite(model, Size);
            }
            else
            {
                FarmModels.Build(Data.TypeDef, Data.Level, model);
                AddLevelPips(model, Data.Level, Size);
            }
            if (Data.IsUpgrading) AddScaffold(model, Size);
        }

        static void BuildConstructionSite(Transform parent, int size)
        {
            Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(0f, 0.03f, 0f), new Vector3(size * 0.9f, 0.06f, size * 0.9f), new Color(0.55f, 0.42f, 0.28f), "Baugrube");
            var wood = new Color(0.62f, 0.45f, 0.25f);
            for (int i = 0; i < 3; i++)
            {
                Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(-0.3f, 0.1f + i * 0.12f, -0.2f), new Vector3(0.9f, 0.1f, 0.15f), wood, "Bretter");
            }
        }

        static void AddScaffold(Transform parent, int size)
        {
            var wood = new Color(0.75f, 0.58f, 0.32f);
            float h = size == 3 ? 1.8f : 1.3f;
            float d = size * 0.46f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(d * x, h / 2f, d * z), new Vector3(0.06f, h, 0.06f), wood, "Gerüst");
                }
            }
            Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(0f, h * 0.7f, -d), new Vector3(d * 2f, 0.06f, 0.06f), wood, "Gerüst");
            Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(0f, h * 0.7f, d), new Vector3(d * 2f, 0.06f, 0.06f), wood, "Gerüst");
        }

        static void AddLevelPips(Transform parent, int level, int size)
        {
            float z = -size * 0.45f;
            for (int i = 0; i < level; i++)
            {
                Visuals.Prim(PrimitiveType.Sphere, parent, new Vector3(-0.32f + 0.16f * i, 0.12f, z), Vector3.one * 0.12f, new Color(1f, 0.85f, 0.15f), "Stufe");
            }
        }
    }

    /// <summary>Baut die Platzhalter-Modelle der Hof-Gebäude. Ursprung = Mitte des Gebäudes auf dem Boden.</summary>
    public static class FarmModels
    {
        static readonly Color Wood = new Color(0.55f, 0.36f, 0.20f);
        static readonly Color DarkWood = new Color(0.38f, 0.24f, 0.13f);
        static readonly Color BarnRed = new Color(0.72f, 0.20f, 0.16f);
        static readonly Color Stone = new Color(0.58f, 0.56f, 0.53f);
        static readonly Color Roof = new Color(0.35f, 0.25f, 0.22f);
        static readonly Color Straw = new Color(0.92f, 0.80f, 0.40f);

        public static void Build(FarmBuildingType type, int level, Transform p)
        {
            int s = type.Size;
            float grow = 1f + 0.05f * (level - 1);
            var root = new GameObject("Skalierung").transform;
            root.SetParent(p, false);
            root.localScale = new Vector3(grow, grow, grow);

            switch (type.Kind)
            {
                case FarmBuildingKind.Bauernhaus:
                    Plate(root, s, new Color(0.62f, 0.55f, 0.42f));
                    Box(root, new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.1f, 1.8f), new Color(0.93f, 0.88f, 0.78f), "Wände");
                    Box(root, new Vector3(0f, 0.08f, 0f), new Vector3(2.3f, 0.16f, 1.9f), Stone, "Sockel");
                    MakeRoof(root, new Vector3(0f, 1.1f, 0f), 2.5f, 2.0f, BarnRed);
                    Box(root, new Vector3(0f, 0.32f, -0.91f), new Vector3(0.42f, 0.62f, 0.05f), DarkWood, "Tür");
                    Box(root, new Vector3(-0.65f, 0.68f, -0.91f), new Vector3(0.36f, 0.3f, 0.05f), new Color(0.6f, 0.85f, 1f), "Fenster");
                    Box(root, new Vector3(0.65f, 0.68f, -0.91f), new Vector3(0.36f, 0.3f, 0.05f), new Color(0.6f, 0.85f, 1f), "Fenster");
                    Box(root, new Vector3(0.6f, 1.75f, 0.35f), new Vector3(0.25f, 0.6f, 0.25f), Stone, "Schornstein");
                    break;

                case FarmBuildingKind.Maisfeld:
                    Box(root, new Vector3(0f, 0.04f, 0f), new Vector3(1.85f, 0.08f, 1.85f), new Color(0.45f, 0.30f, 0.17f), "Acker");
                    float h = 0.45f + 0.07f * level;
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            var pos = new Vector3(-0.63f + 0.42f * i, 0f, -0.63f + 0.42f * j);
                            Box(root, pos + Vector3.up * (h / 2f), new Vector3(0.07f, h, 0.07f), new Color(0.35f, 0.65f, 0.2f), "Stängel");
                            Box(root, pos + Vector3.up * h, new Vector3(0.12f, 0.2f, 0.12f), new Color(1f, 0.85f, 0.25f), "Kolben");
                        }
                    }
                    break;

                case FarmBuildingKind.Kuhstall:
                    Plate(root, s, new Color(0.5f, 0.42f, 0.3f));
                    Box(root, new Vector3(0f, 0.42f, 0.05f), new Vector3(1.5f, 0.84f, 1.3f), BarnRed, "Stall");
                    MakeRoof(root, new Vector3(0f, 0.84f, 0.05f), 1.75f, 1.45f, Roof);
                    Box(root, new Vector3(0f, 0.32f, -0.61f), new Vector3(0.6f, 0.62f, 0.05f), Color.white, "Tor");
                    Box(root, new Vector3(0f, 0.32f, -0.63f), new Vector3(0.6f, 0.06f, 0.03f), BarnRed, "Torbalken");
                    // Eine Kuh schaut aus dem Tor
                    Box(root, new Vector3(0f, 0.38f, -0.72f), new Vector3(0.2f, 0.2f, 0.18f), Color.white, "Kuhkopf");
                    Box(root, new Vector3(0.04f, 0.42f, -0.74f), new Vector3(0.1f, 0.08f, 0.16f), new Color(0.1f, 0.1f, 0.1f), "Fleck");
                    Box(root, new Vector3(0f, 0.33f, -0.82f), new Vector3(0.15f, 0.09f, 0.04f), new Color(1f, 0.7f, 0.75f), "Maul");
                    break;

                case FarmBuildingKind.Maissilo:
                    Plate(root, s, Stone);
                    Prim(root, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(1.0f, 0.9f, 1.0f), new Color(0.85f, 0.78f, 0.55f), "Silo");
                    Prim(root, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(1.03f, 0.05f, 1.03f), new Color(0.6f, 0.5f, 0.3f), "Ring");
                    Prim(root, PrimitiveType.Sphere, new Vector3(0f, 1.8f, 0f), new Vector3(1.0f, 0.6f, 1.0f), new Color(0.6f, 0.62f, 0.66f), "Kuppel");
                    break;

                case FarmBuildingKind.Milchtank:
                    Plate(root, s, Stone);
                    for (int x = -1; x <= 1; x += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Box(root, new Vector3(0.35f * x, 0.25f, 0.35f * z), new Vector3(0.08f, 0.5f, 0.08f), new Color(0.5f, 0.5f, 0.55f), "Bein");
                        }
                    }
                    Prim(root, PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(1.1f, 0.55f, 1.1f), Color.white, "Tank");
                    Prim(root, PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(1.12f, 0.08f, 1.12f), new Color(0.3f, 0.55f, 0.95f), "Streifen");
                    Prim(root, PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0f), new Vector3(1.1f, 0.35f, 1.1f), Color.white, "Deckel");
                    break;

                case FarmBuildingKind.Knechthuette:
                    Plate(root, s, new Color(0.5f, 0.42f, 0.3f));
                    Box(root, new Vector3(0f, 0.32f, 0.1f), new Vector3(1.0f, 0.64f, 1.0f), Wood, "Hütte");
                    MakeRoof(root, new Vector3(0f, 0.64f, 0.1f), 1.25f, 1.15f, Straw);
                    Box(root, new Vector3(0f, 0.25f, -0.41f), new Vector3(0.3f, 0.5f, 0.04f), DarkWood, "Tür");
                    Box(root, new Vector3(0.6f, 0.15f, -0.55f), new Vector3(0.38f, 0.3f, 0.3f), Straw, "Heuballen");
                    Box(root, new Vector3(-0.6f, 0.45f, -0.5f), new Vector3(0.04f, 0.9f, 0.04f), Wood, "Mistgabel");
                    break;

                case FarmBuildingKind.Schmiede:
                    Plate(root, s, Stone);
                    Box(root, new Vector3(0f, 0.38f, 0.15f), new Vector3(1.4f, 0.76f, 1.1f), Stone, "Schmiede");
                    MakeRoof(root, new Vector3(0f, 0.76f, 0.15f), 1.6f, 1.25f, new Color(0.3f, 0.3f, 0.32f));
                    Box(root, new Vector3(0.45f, 1.3f, 0.35f), new Vector3(0.28f, 0.7f, 0.28f), new Color(0.4f, 0.38f, 0.36f), "Esse");
                    Prim(root, PrimitiveType.Sphere, new Vector3(0f, 0.3f, -0.42f), Vector3.one * 0.3f, new Color(1f, 0.5f, 0.1f), "Glut");
                    Box(root, new Vector3(-0.5f, 0.2f, -0.65f), new Vector3(0.35f, 0.14f, 0.18f), new Color(0.15f, 0.15f, 0.17f), "Amboss");
                    Box(root, new Vector3(-0.5f, 0.07f, -0.65f), new Vector3(0.18f, 0.14f, 0.14f), new Color(0.15f, 0.15f, 0.17f), "Ambossfuß");
                    break;

                case FarmBuildingKind.Zimmerei:
                    Plate(root, s, new Color(0.5f, 0.42f, 0.3f));
                    Box(root, new Vector3(0f, 0.38f, 0.15f), new Vector3(1.4f, 0.76f, 1.1f), Wood, "Zimmerei");
                    MakeRoof(root, new Vector3(0f, 0.76f, 0.15f), 1.6f, 1.25f, BarnRed);
                    for (int i = 0; i < 3; i++)
                    {
                        var log = Prim(root, PrimitiveType.Cylinder, new Vector3(-0.2f + 0.2f * i, 0.1f + (i == 1 ? 0.16f : 0f), -0.62f),
                            new Vector3(0.18f, 0.4f, 0.18f), DarkWood, "Stamm");
                        log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    Box(root, new Vector3(0.6f, 0.25f, -0.6f), new Vector3(0.3f, 0.25f, 0.3f), Straw, "Heuballen");
                    break;

                case FarmBuildingKind.Tueftlerschuppen:
                    Plate(root, s, Stone);
                    Box(root, new Vector3(0f, 0.36f, 0.15f), new Vector3(1.3f, 0.72f, 1.1f), new Color(0.45f, 0.55f, 0.65f), "Schuppen");
                    MakeRoof(root, new Vector3(0f, 0.72f, 0.15f), 1.5f, 1.25f, new Color(0.25f, 0.3f, 0.38f));
                    Prim(root, PrimitiveType.Cylinder, new Vector3(0.55f, 0.6f, -0.55f), new Vector3(0.14f, 0.6f, 0.14f), new Color(0.7f, 0.5f, 0.3f), "Spule");
                    Prim(root, PrimitiveType.Sphere, new Vector3(0.55f, 1.3f, -0.55f), Vector3.one * 0.3f, new Color(0.6f, 0.9f, 1f), "Kugel");
                    Box(root, new Vector3(-0.45f, 0.18f, -0.6f), new Vector3(0.35f, 0.36f, 0.3f), new Color(0.3f, 0.3f, 0.32f), "Kiste");
                    break;
            }
        }

        static void Plate(Transform p, int size, Color c)
        {
            Box(p, new Vector3(0f, 0.03f, 0f), new Vector3(size * 0.92f, 0.06f, size * 0.92f), c, "Platte");
        }

        static GameObject Box(Transform p, Vector3 pos, Vector3 scale, Color c, string name)
            => Visuals.Prim(PrimitiveType.Cube, p, pos, scale, c, name);

        static GameObject Prim(Transform p, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, string name)
            => Visuals.Prim(type, p, pos, scale, c, name);

        /// <summary>Satteldach: ein um 45° gedrehter Würfel, dessen untere Hälfte im Gebäude steckt.</summary>
        static void MakeRoof(Transform p, Vector3 pos, float width, float depth, Color c)
        {
            float side = width / Mathf.Sqrt(2f);
            var roof = Visuals.Prim(PrimitiveType.Cube, p, pos, new Vector3(side, side, depth), c, "Dach");
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }
}
