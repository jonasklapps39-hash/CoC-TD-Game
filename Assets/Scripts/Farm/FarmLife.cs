using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Aussehen der Hindernisse (aus Grundkörpern im Toon-Stil).</summary>
    public static class ObstacleModels
    {
        public static GameObject Build(ObstacleData o, Transform parent)
        {
            var go = new GameObject("Hindernis " + o.TypeDef.Name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(o.X + 0.5f, 0f, o.Y + 0.5f);
            var rng = new System.Random(o.Id);
            go.transform.localRotation = Quaternion.Euler(0f, rng.Next(0, 360), 0f);
            var t = go.transform;
            switch (o.Type)
            {
                case "stein":
                    Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(-0.1f, 0.18f, 0f), new Vector3(0.62f, 0.42f, 0.55f), new Color(0.6f, 0.6f, 0.62f), "Stein");
                    Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(0.22f, 0.12f, 0.15f), new Vector3(0.36f, 0.26f, 0.34f), new Color(0.5f, 0.5f, 0.53f), "Stein");
                    Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(-0.05f, 0.34f, -0.05f), new Vector3(0.18f, 0.06f, 0.16f), new Color(0.35f, 0.65f, 0.25f), "Moos");
                    break;
                case "stumpf":
                    var wood = new Color(0.5f, 0.32f, 0.17f);
                    Visuals.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.14f, 0f), new Vector3(0.5f, 0.14f, 0.5f), wood, "Stumpf");
                    Visuals.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.285f, 0f), new Vector3(0.44f, 0.01f, 0.44f), new Color(0.86f, 0.68f, 0.42f), "Schnitt");
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI / 2f + 0.4f;
                        var root = Visuals.Prim(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * 0.28f, 0.05f, Mathf.Sin(a) * 0.28f),
                            new Vector3(0.22f, 0.08f, 0.1f), wood, "Wurzel");
                        root.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                    }
                    break;
                case "pilze":
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 2.1f;
                        var p = new Vector3(Mathf.Cos(a) * 0.2f, 0f, Mathf.Sin(a) * 0.2f);
                        float s = 0.8f + 0.3f * i;
                        Visuals.Prim(PrimitiveType.Cylinder, t, p + new Vector3(0f, 0.08f * s, 0f), new Vector3(0.07f, 0.08f, 0.07f) * s, new Color(0.97f, 0.95f, 0.88f), "Stiel");
                        Visuals.Prim(PrimitiveType.Sphere, t, p + new Vector3(0f, 0.17f * s, 0f), new Vector3(0.22f, 0.12f, 0.22f) * s, new Color(0.88f, 0.15f, 0.12f), "Hut");
                        Visuals.Prim(PrimitiveType.Sphere, t, p + new Vector3(0.04f, 0.22f * s, 0.03f), Vector3.one * 0.04f * s, Color.white, "Punkt", outline: false);
                    }
                    break;
                default: // busch
                    var leaf = new Color(0.25f, 0.58f, 0.2f);
                    Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.45f, 0.55f), leaf, "Busch");
                    Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(0.2f, 0.16f, 0.12f), new Vector3(0.36f, 0.3f, 0.36f), new Color(0.3f, 0.65f, 0.22f), "Busch");
                    Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(-0.18f, 0.15f, -0.1f), new Vector3(0.34f, 0.28f, 0.34f), new Color(0.22f, 0.52f, 0.18f), "Busch");
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.7f;
                        Visuals.Prim(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * 0.22f, 0.3f, Mathf.Sin(a) * 0.22f), Vector3.one * 0.07f,
                            new Color(0.85f, 0.1f, 0.25f), "Beere", outline: false);
                    }
                    break;
            }
            return go;
        }
    }

    /// <summary>Kühe, die rund um die Kuhställe über den Hof laufen und grasen – nur zur Deko.</summary>
    public class FarmAnimals : MonoBehaviour
    {
        class Animal
        {
            public Transform T;
            public Transform Model;
            public Vector3 Home;
            public Vector3 Target;
            public float Wait;
            public float Offset;
        }

        FarmState farm;
        readonly List<Animal> animals = new List<Animal>();
        int signature = -1;
        float checkTimer;

        public void Init(FarmState farm) => this.farm = farm;

        int Signature()
        {
            int s = 17;
            foreach (var b in farm.Buildings)
            {
                if (b.Type == FarmBuildingType.Kuhstall.Id) s = s * 31 + b.X * 1000 + b.Y * 10 + b.Level;
            }
            return s;
        }

        void Rebuild()
        {
            foreach (var a in animals) Destroy(a.T.gameObject);
            animals.Clear();
            int seed = 3;
            foreach (var b in farm.Buildings)
            {
                if (b.Type != FarmBuildingType.Kuhstall.Id || b.Level <= 0) continue;
                int count = Mathf.Min(5, b.Level + 1);
                var home = new Vector3(b.X + 1f, 0f, b.Y - 0.8f);
                for (int i = 0; i < count; i++)
                {
                    var go = new GameObject("Hof-Kuh");
                    go.transform.SetParent(transform, false);
                    var model = new GameObject("Modell").transform;
                    model.SetParent(go.transform, false);
                    if (ModelLibrary.Spawn("Kuh", model, 0.5f) == null)
                    {
                        Visuals.Prim(PrimitiveType.Cube, model, new Vector3(0f, 0.22f, 0f), new Vector3(0.22f, 0.2f, 0.4f), Color.white, "Kuh");
                    }
                    var a = new Animal
                    {
                        T = go.transform, Model = model, Home = home,
                        Wait = Random.Range(0f, 3f), Offset = (seed++) * 1.7f,
                    };
                    a.T.localPosition = PickSpot(a);
                    a.Target = a.T.localPosition;
                    animals.Add(a);
                }
            }
        }

        /// <summary>Freier Platz in der Nähe des Stalls (nicht auf Gebäuden oder Hindernissen).</summary>
        Vector3 PickSpot(Animal a)
        {
            for (int i = 0; i < 12; i++)
            {
                var p = a.Home + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2f, 1f));
                p.x = Mathf.Clamp(p.x, 0.3f, FarmState.Width - 0.3f);
                p.z = Mathf.Clamp(p.z, 0.3f, FarmState.Height - 0.3f);
                var cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
                if (farm.IsAreaFree(cell.x, cell.y, 1, -1)) return p;
            }
            return a.T != null ? a.T.localPosition : a.Home;
        }

        void Update()
        {
            if (farm == null) return;
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = 1f;
                int s = Signature();
                if (s != signature)
                {
                    signature = s;
                    Rebuild();
                }
            }

            float dt = Time.deltaTime;
            foreach (var a in animals)
            {
                float t = Time.time + a.Offset;
                if (a.Wait > 0f)
                {
                    a.Wait -= dt;
                    a.Model.localRotation = Quaternion.Euler(6f + Mathf.Sin(t * 2.5f) * 4f, 0f, 0f);
                    a.Model.localPosition = Vector3.zero;
                    if (a.Wait <= 0f) a.Target = PickSpot(a);
                    continue;
                }
                Vector3 d = a.Target - a.T.localPosition;
                d.y = 0f;
                if (d.magnitude < 0.05f)
                {
                    a.Wait = Random.Range(2f, 6f);
                    continue;
                }
                a.T.localPosition += d.normalized * Mathf.Min(d.magnitude, 0.35f * dt);
                a.T.localRotation = Quaternion.Slerp(a.T.localRotation, Quaternion.LookRotation(d), 6f * dt);
                a.Model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 9f)) * 0.02f, 0f);
                a.Model.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 3f);
            }
        }
    }
}
