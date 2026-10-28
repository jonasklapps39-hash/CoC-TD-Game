using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Lädt die in Blender gebauten Modelle aus Assets/Resources/Models (siehe tools/blender/build_models.py),
    /// gibt ihnen das Toon-Material und bringt sie auf eine gewünschte Größe.
    /// Fehlt ein Modell, liefert <see cref="Spawn"/> null und der Aufrufer baut einen Platzhalter.
    /// </summary>
    public static class ModelLibrary
    {
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        public enum Fit
        {
            /// <summary>Höhe des Modells = size.</summary>
            Height,
            /// <summary>Größere Seite der Grundfläche = size (für Türme auf einem Feld).</summary>
            Footprint,
        }

        /// <summary>
        /// Erzeugt das Modell als Kind von <paramref name="parent"/>: Mitte der Grundfläche im Ursprung, Unterkante auf y = 0,
        /// Blickrichtung +Z.
        /// </summary>
        public static GameObject Spawn(string name, Transform parent, float size, Fit fit = Fit.Height)
        {
            if (!cache.TryGetValue(name, out var prefab))
            {
                prefab = Resources.Load<GameObject>("Models/" + name);
                cache[name] = prefab;
                if (prefab == null) Debug.LogWarning("Modell nicht gefunden: Models/" + name);
            }
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = Visuals.VertexColorMaterial;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            // Größe und Lage im Raum des Elternobjekts bestimmen
            Bounds b = LocalBounds(go.transform, parent, renderers);
            float current = fit == Fit.Height ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (current > 0.0001f) go.transform.localScale = Vector3.one * (size / current);
            b = LocalBounds(go.transform, parent, renderers);
            go.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            return go;
        }

        static Bounds LocalBounds(Transform root, Transform space, Renderer[] renderers)
        {
            bool first = true;
            var bounds = new Bounds();
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var toSpace = space != null ? space.worldToLocalMatrix * r.transform.localToWorldMatrix : r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = toSpace.MultiplyPoint3x4(corner);
                    if (first)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        first = false;
                    }
                    else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }
    }
}
