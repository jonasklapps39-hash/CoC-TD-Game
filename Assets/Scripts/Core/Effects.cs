using UnityEngine;

namespace MuhDefense
{
    /// <summary>Kleine Partikel-Effekte aus Grundkörpern (Funken, Splitter, Stroh), passend zum Toon-Look.</summary>
    public static class Effects
    {
        public static void Beam(Transform parent, Vector3 from, Vector3 to, Color color) => BeamEffect.Spawn(parent, from, to, color);

        /// <summary>Wirft <paramref name="count"/> kleine Teilchen in alle Richtungen nach oben.</summary>
        public static void Burst(Transform parent, Vector3 position, Color color, int count, float speed = 2.5f, float size = 0.09f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Visuals.Prim(PrimitiveType.Cube, parent, position, Vector3.one * size * Random.Range(0.7f, 1.3f),
                    Color.Lerp(color, Color.white, Random.Range(0f, 0.25f)), "Teilchen", outline: false);
                go.transform.position = position;
                go.transform.rotation = Random.rotation;
                var dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.6f;
                var p = go.AddComponent<Particle>();
                p.velocity = dir.normalized * speed * Random.Range(0.6f, 1.2f);
                p.lifetime = Random.Range(0.4f, 0.75f);
            }
        }
    }

    /// <summary>Kurzer Lichtstrahl (Bertas Laseraugen).</summary>
    public class BeamEffect : MonoBehaviour
    {
        LineRenderer line;
        float age;
        const float Lifetime = 0.12f;
        static Material material;
        Color color;

        public static void Spawn(Transform parent, Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("Strahl");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<BeamEffect>();
            b.color = color;
            b.line = go.AddComponent<LineRenderer>();
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            b.line.sharedMaterial = material;
            b.line.positionCount = 2;
            b.line.SetPosition(0, from);
            b.line.SetPosition(1, to);
            b.line.widthMultiplier = 0.08f;
            b.line.startColor = color;
            b.line.endColor = color;
            b.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            age += Time.deltaTime;
            var c = color;
            c.a = 1f - age / Lifetime;
            line.startColor = c;
            line.endColor = c;
            if (age >= Lifetime) Destroy(gameObject);
        }
    }

    public class Particle : MonoBehaviour
    {
        public Vector3 velocity;
        public float lifetime = 0.6f;
        float age;
        Vector3 startScale;

        void Start() => startScale = transform.localScale;

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            velocity += Vector3.down * 9f * dt;
            transform.position += velocity * dt;
            if (transform.position.y < 0.02f)
            {
                transform.position = new Vector3(transform.position.x, 0.02f, transform.position.z);
                velocity = new Vector3(velocity.x * 0.5f, -velocity.y * 0.3f, velocity.z * 0.5f);
            }
            transform.Rotate(400f * dt, 300f * dt, 0f);
            float k = Mathf.Clamp01(age / lifetime);
            transform.localScale = startScale * (1f - k);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
