using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Hilfsfunktionen, um Platzhalter-Grafik aus Unity-Grundkörpern zu bauen.
    /// Wird später durch richtige 3D-Modelle (Prefabs) ersetzt.
    /// </summary>
    public static class Visuals
    {
        static Material baseMaterial;
        static Material lineMaterial;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        /// <summary>
        /// Kopie des Standard-Materials eines Grundkörpers. Funktioniert so mit der
        /// Built-in-Pipeline und mit URP, ohne einen Shader per Namen suchen zu müssen.
        /// </summary>
        static Material BaseMaterial
        {
            get
            {
                if (baseMaterial == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    baseMaterial = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                    Object.DestroyImmediate(tmp);
                }
                return baseMaterial;
            }
        }

        public static Material Mat(Color color)
        {
            if (!cache.TryGetValue(color, out var mat) || mat == null)
            {
                mat = new Material(BaseMaterial) { color = color };
                cache[color] = mat;
            }
            return mat;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        /// <summary>Flacher Kreis aus einer Linie, z. B. für die Reichweite eines Turms.</summary>
        public static LineRenderer Circle(Transform parent, float radius, Color color, float width = 0.05f)
        {
            var go = new GameObject("Kreis");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var lr = go.AddComponent<LineRenderer>();
            if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lr.sharedMaterial = lineMaterial;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = width;
            lr.startColor = color;
            lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            SetCircleRadius(lr, radius);
            return lr;
        }

        public static void SetCircleRadius(LineRenderer lr, float radius)
        {
            const int segments = 48;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }
    }

    /// <summary>Ein Kreis, der sich ausdehnt und verblasst (Treffer, Eismilch-Welle).</summary>
    public class RingEffect : MonoBehaviour
    {
        LineRenderer line;
        float maxRadius;
        float duration;
        float age;
        Color color;

        public static void Spawn(Transform parent, Vector3 position, float radius, Color color, float duration = 0.4f)
        {
            var go = new GameObject("Ring-Effekt");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var fx = go.AddComponent<RingEffect>();
            fx.maxRadius = radius;
            fx.duration = duration;
            fx.color = color;
            fx.line = Visuals.Circle(go.transform, 0.05f, color, 0.08f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / duration);
            Visuals.SetCircleRadius(line, Mathf.Lerp(0.1f, maxRadius, k));
            var c = color;
            c.a = color.a * (1f - k);
            line.startColor = c;
            line.endColor = c;
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
