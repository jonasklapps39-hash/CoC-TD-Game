namespace MuhDefense
{
    /// <summary>Kurzes Hof-Tutorial für neue Spieler. Die Schritte gehen von selbst weiter, sobald man das Verlangte getan hat.</summary>
    public static class FarmTutorial
    {
        public class Step
        {
            public string Title;
            public string Text;
            /// <summary>null = weiter mit dem Knopf „Weiter“.</summary>
            public System.Func<FarmState, bool> Done;
        }

        public static readonly Step[] Steps =
        {
            new Step
            {
                Title = "Willkommen auf Heinz' Hof!",
                Text = "Aliens wollen deine Kühe! Hier auf dem Hof baust du auf, im Level verteidigst du die Weide. " +
                       "Ziehe die Wiese, um dich umzusehen, und zoome mit zwei Fingern oder mit + und -.",
            },
            new Step
            {
                Title = "Erstes Gebäude",
                Text = "Tippe rechts auf „Bauen“ und stelle ein Gebäude auf, zum Beispiel einen Maissilo. " +
                       "Ein Knecht baut es für dich – das dauert ein paar Sekunden.",
                Done = f => f.CounterValue(Counter.FarmBuilds) > 0,
            },
            new Step
            {
                Title = "Ab in den Kampf",
                Text = "Während gebaut wird, kannst du schon kämpfen: Tippe auf „Weltkarte“ und spiele das erste Level im Heimattal.",
                Done = f => f.BestStars(Levels.All[0].Id) > 0,
            },
            new Step
            {
                Title = "Ernte einsammeln",
                Text = "Maisfeld und Kuhstall produzieren ständig – auch wenn das Spiel zu ist. " +
                       "Tippe auf die Blase über einem Feld oder Stall, um einzusammeln.",
                Done = f => f.CounterValue(Counter.Collected) > 0,
            },
            new Step
            {
                Title = "Bauernhaus verbessern",
                Text = "Kein Gebäude darf höher sein als das Bauernhaus. Tippe aufs Bauernhaus und verbessere es – " +
                       "danach kannst du auch die Schmiede verbessern, die neue Türme und höhere Upgrades freischaltet.",
                Done = f => FarmhouseImproved(f) || SmithyImproved(f),
            },
            new Step
            {
                Title = "Gut gemacht!",
                Text = "Noch ein paar Tipps: Baue eine Bauernschule – dort tauschst du Sterne gegen Boni. Diamanten gibt es im Shop auszugeben, " +
                       "und unter „Aufgaben“ warten jeden Tag neue Belohnungen. Viel Spaß!",
            },
        };

        static bool FarmhouseImproved(FarmState f)
        {
            foreach (var b in f.Data.Buildings)
            {
                if (b.TypeDef == FarmBuildingType.Bauernhaus && (b.Level >= 2 || b.IsUpgrading)) return true;
            }
            return false;
        }

        static bool SmithyImproved(FarmState f)
        {
            foreach (var b in f.Data.Buildings)
            {
                if (b.TypeDef == FarmBuildingType.Schmiede && (b.Level >= 2 || b.IsUpgrading)) return true;
            }
            return false;
        }

        public static Step Current(FarmState f)
        {
            int i = f.Data.TutorialStep;
            return i >= 0 && i < Steps.Length ? Steps[i] : null;
        }

        /// <summary>Geht automatisch weiter, wenn die Aufgabe des aktuellen Schritts erledigt ist.</summary>
        public static void Update(FarmState f)
        {
            var step = Current(f);
            if (step != null && step.Done != null && step.Done(f)) Next(f);
        }

        public static void Next(FarmState f)
        {
            f.Data.TutorialStep++;
            if (f.Data.TutorialStep >= Steps.Length) f.Data.TutorialStep = -1;
            f.Save();
        }

        public static void Skip(FarmState f)
        {
            f.Data.TutorialStep = -1;
            f.Save();
        }
    }
}
