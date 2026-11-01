using UnityEngine;

namespace MuhDefense
{
    /// <summary>Kosmetisches Aussehen der Kühe im Level (mit Diamanten kaufbar).</summary>
    public class CowSkin
    {
        public string Id;
        public string Name;
        public int Cost;
        /// <summary>Farbe, mit der das Kuh-Modell eingefärbt wird (Weiß = unverändert).</summary>
        public Color Tint;

        public static readonly CowSkin[] All =
        {
            new CowSkin { Id = "standard", Name = "Klassisch", Cost = 0, Tint = Color.white },
            new CowSkin { Id = "schoko", Name = "Schoko-Kühe", Cost = 40, Tint = new Color(0.78f, 0.55f, 0.38f) },
            new CowSkin { Id = "mitternacht", Name = "Mitternachts-Kühe", Cost = 60, Tint = new Color(0.6f, 0.65f, 1f) },
            new CowSkin { Id = "gold", Name = "Goldene Kühe", Cost = 120, Tint = new Color(1f, 0.84f, 0.35f) },
        };

        public static CowSkin Get(string id)
        {
            foreach (var s in All)
            {
                if (s.Id == id) return s;
            }
            return All[0];
        }
    }
}
