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
