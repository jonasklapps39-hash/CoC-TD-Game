using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Grafik-Hilfen: Materialien im Grafikstil 7 (Shader "MuhDefense/Toon" mit Cel-Shading und Umrisslinien)
    /// und Grundkörper für Kleinteile. Figuren und Türme kommen als Modelle aus <see cref="ModelLibrary"/>.
    /// </summary>
    public static class Visuals
    {
        const string ToonShaderName = "MuhDefense/Toon";

        static Shader toonShader;
        static bool toonShaderSearched;
        static Material fallbackMaterial;
        static Material lineMaterial;
        static Material vertexColorMaterial;
        static readonly Dictionary<(Color, bool), Material> cache = new Dictionary<(Color, bool), Material>();

        /// <summary>Der Toon-Shader, oder null, falls er (noch) nicht vorhanden ist.</summary>
        static Shader ToonShader
        {
            get
            {
                if (!toonShaderSearched)
                {
                    toonShader = Shader.Find(ToonShaderName);
                    toonShaderSearched = true;
                    if (toonShader == null) Debug.LogWarning("Shader " + ToonShaderName + " nicht gefunden – nutze Standard-Material.");
                }
                return toonShader;
            }
        }

        /// <summary>Ersatz, falls der Toon-Shader fehlt: Kopie des Standard-Materials eines Grundkörpers.</summary>
        static Material FallbackMaterial
        {
            get
            {
                if (fallbackMaterial == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fallbackMaterial = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                    Object.DestroyImmediate(tmp);
                }
                return fallbackMaterial;
            }
        }

        /// <param name="outline">Mit dunkler Umrisslinie (aus für Bodenplatten, damit kein Gitter entsteht).</param>
        public static Material Mat(Color color, bool outline = true)
        {
            if (!cache.TryGetValue((color, outline), out var mat) || mat == null)
            {
                if (ToonShader != null)
                {
                    mat = new Material(ToonShader);
                    mat.SetColor("_Color", color);
                    mat.SetFloat("_OutlineWidth", outline ? 2.2f : 0f);
                }
                else
                {
                    mat = new Material(FallbackMaterial) { color = color };
                }
                cache[(color, outline)] = mat;
            }
            return mat;
        }

        /// <summary>Material für die Blender-Modelle: Farbe kommt aus den Vertexfarben.</summary>
        public static Material VertexColorMaterial
        {
            get
            {
                if (vertexColorMaterial == null)
                {
                    if (ToonShader != null)
                    {
                        vertexColorMaterial = new Material(ToonShader);
                        vertexColorMaterial.SetFloat("_UseVertexColor", 1f);
                        vertexColorMaterial.SetFloat("_OutlineWidth", 2.4f);
                    }
                    else
                    {
                        vertexColorMaterial = new Material(FallbackMaterial);
                    }
                }
                return vertexColorMaterial;
            }
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, string name = null, bool outline = true)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color, outline);
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
