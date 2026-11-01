using System;
using System.Collections.Generic;

namespace MuhDefense
{
    /// <summary>Spielstand, wird als JSON gespeichert (JsonUtility).</summary>
    [Serializable]
    public class SaveData
    {
        public int Version = 1;
        public double Corn;
        public double Milk;
        public int NextBuildingId = 1;
        public List<BuildingData> Buildings = new List<BuildingData>();
        public List<LevelProgress> Levels = new List<LevelProgress>();
        /// <summary>Alien-Arten, die schon vorgestellt wurden (Fenster „Neuer Gegner“).</summary>
        public List<string> SeenEnemies = new List<string>();
        /// <summary>Gekaufte Talente aus dem Talentbaum „Bauernwissen“.</summary>
        public List<string> Talents = new List<string>();
        /// <summary>Premium-Währung (im Prototyp nur im Spiel verdient).</summary>
        public int Gems;
        /// <summary>Mit Diamanten angeheuerte zusätzliche Knechte.</summary>
        public int ExtraBuilders;
        public string CowSkin = "standard";
        public List<string> OwnedSkins = new List<string> { "standard" };
    }

    [Serializable]
    public class BuildingData
    {
        public int Id;
        public string Type;
        public int X;
        public int Y;
        /// <summary>0 = wird gerade neu gebaut.</summary>
        public int Level;
        /// <summary>Zeitpunkt (Sekunden seit 1970, UTC), an dem der Bau/das Upgrade fertig ist. 0 = kein Bau.</summary>
        public double UpgradeEnd;
        /// <summary>Bei Produktionsgebäuden: angesammelte, noch nicht eingesammelte Menge.</summary>
        public double Stored;
        /// <summary>Letzter Zeitpunkt, bis zu dem die Produktion berechnet wurde.</summary>
        public double LastTick;

        public bool IsUpgrading => UpgradeEnd > 0;
        public FarmBuildingType TypeDef => FarmBuildingType.Get(Type);
    }

    [Serializable]
    public class LevelProgress
    {
        public string Id;
        public int BestStars;
    }
}
