using UnityEngine;

namespace MuhDefense
{
    /// <summary>Kleine Partikel-Effekte aus Grundkörpern (Funken, Splitter, Stroh), passend zum Toon-Look.</summary>
    public static class Effects
    {
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
