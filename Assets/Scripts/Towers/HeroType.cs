namespace MuhDefense
{
    public enum HeroKind
    {
        Heinz,
        Gertrud,
        Berta,
    }

    /// <summary>Die Helden aus dem Spielkonzept (Kapitel 9). Werte stehen in <see cref="Hero"/>.</summary>
    public class HeroType
    {
        public HeroKind Kind;
        public string Name;
        public string Role;
        public string Description;
        public string ModelName;
        public string Ability1Name;
        public string Ability2Name;
        public float Ability1Cooldown;
        public float Ability2Cooldown;
        /// <summary>Wird automatisch freigeschaltet, sobald man so viele Sterne gesammelt hat (0 = von Anfang an).</summary>
        public int UnlockStars;
        /// <summary>Alternativ sofort mit Diamanten freischaltbar.</summary>
        public int UnlockGems;

        public string Id => Kind.ToString();

        public static readonly HeroType Heinz = new HeroType
        {
            Kind = HeroKind.Heinz,
            Name = "Bauer Heinz",
            Role = "Allrounder",
            Description = "Grummelig, aber mutig. Schrotflinte mit mehreren Kugeln, trifft auch Ufos.",
            ModelName = "BauerHeinz",
            Ability1Name = "Ladung Schrot",
            Ability2Name = "Traktor-Raserei",
            Ability1Cooldown = 25f,
            Ability2Cooldown = 45f,
        };

        public static readonly HeroType Gertrud = new HeroType
        {
            Kind = HeroKind.Gertrud,
            Name = "Oma Gertrud",
            Role = "Unterstützerin",
            Description = "Wirft Kekse, die Aliens verlangsamen. Türme in ihrer Nähe schießen schneller („Kaffee und Kuchen“).",
            ModelName = "OmaGertrud",
            Ability1Name = "Apfelkuchen-Wurf",
            Ability2Name = "Sonntagsbraten-Duft",
            Ability1Cooldown = 20f,
            Ability2Cooldown = 50f,
            UnlockStars = 12,
            UnlockGems = 100,
        };

        public static readonly HeroType Berta = new HeroType
        {
            Kind = HeroKind.Berta,
            Name = "Berta",
            Role = "Kampfkuh",
            Description = "Laser aus den Augen. In ihrer Nähe kann kein Alien eine Kuh schnappen.",
            ModelName = "Berta",
            Ability1Name = "Hörner-Ramme",
            Ability2Name = "Muh-Schallwelle",
            Ability1Cooldown = 22f,
            Ability2Cooldown = 50f,
            UnlockStars = 20,
            UnlockGems = 150,
        };

        public static readonly HeroType[] All = { Heinz, Gertrud, Berta };

        public static HeroType Get(string id)
        {
            foreach (var h in All)
            {
                if (h.Id == id) return h;
            }
            return Heinz;
        }
    }
}
