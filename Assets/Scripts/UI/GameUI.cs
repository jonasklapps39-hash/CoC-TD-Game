using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Bedienoberfläche eines Levels im Bloons-TD6-Look (Unity IMGUI mit eigenen Stilen aus <see cref="UiKit"/>).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        LevelManager level;
        Camera cam;
        readonly List<Rect> blockers = new List<Rect>();
        bool confirmLeave;

        // Drag & Drop von Turm- oder Helden-Karte auf die Karte
        const float DragThreshold = 12f;
        TowerType pressedTower;
        bool pressedHero;
        Vector2 pressRaw;
        Vector2 currentRaw;
        bool dragging;

        public void Init(LevelManager level)
        {
            this.level = level;
            cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) UiKit.FitCamera(cam, level.Def.Width, level.Def.Height, 62f);
        }

        void OnGUI()
        {
            if (level == null || cam == null) return;

            Vector2 rawMouse = UiKit.BeginGui();
            currentRaw = rawMouse;
            // Vor dem Zeichnen: "blockers" enthält noch die Bedienelemente des letzten Durchlaufs
            HandleDrag(rawMouse);
            blockers.Clear();

            DrawWorldOverlays();
            DrawTopBar();
            DrawSideBar();
            if (level.SelectedTower != null && !level.Paused) DrawUpgradePanel(level.SelectedTower);
            else if (level.HeroSelected && level.Hero != null && !level.Paused) DrawHeroPanel(level.Hero);

            switch (level.State)
            {
                case LevelState.Intro: DrawIntro(); break;
                case LevelState.Won: DrawEndScreen(true); break;
                case LevelState.Lost: DrawEndScreen(false); break;
            }
            if (level.Paused) DrawPauseMenu();
            else if (level.NewEnemyIntro != null) DrawEnemyIntro();
            else if (level.GiftedTower != null) DrawGift();

            if (UiKit.EscapePressed())
            {
                if (level.Paused) ResumeGame();
                else if (level.PlacingType != null || level.PlacingHero || level.SelectedTower != null || level.HeroSelected)
                {
                    level.CancelPlacing();
                    level.Select(null);
                }
                else level.SetPaused(true);
            }
            else if (UiKit.TryGetWorldClick(rawMouse, blockers, cam, out var world))
            {
                level.HandleClick(world);
            }
        }

        bool Playing => level.State == LevelState.Building || level.State == LevelState.WaveRunning;

        // ---------------------------------------------------------------- Drag & Drop

        /// <summary>Karte festhalten und auf die Karte ziehen: Vorschau folgt dem Finger, Loslassen baut den Turm.</summary>
        void HandleDrag(Vector2 rawMouse)
        {
            var e = Event.current;
            if (pressedTower == null && !pressedHero) return;

            if (e.type == EventType.MouseDrag)
            {
                if (!dragging && (rawMouse - pressRaw).magnitude > DragThreshold * UiKit.Scale)
                {
                    dragging = true;
                    GUIUtility.hotControl = 0; // Kartenknopf soll beim Loslassen nicht zusätzlich auslösen
                    if (pressedHero) level.BeginPlacingHero();
                    else level.BeginPlacing(pressedTower);
                }
                if (dragging)
                {
                    bool overUi = UiKit.IsOverUi(rawMouse, blockers);
                    if (!overUi && UiKit.ScreenToGround(rawMouse, cam, out var world)) level.SetPlacementPreview(world);
                    else level.SetPlacementPreview(null);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (dragging)
                {
                    bool placing = level.PlacingType != null || level.PlacingHero;
                    if (placing && !UiKit.IsOverUi(rawMouse, blockers) && UiKit.ScreenToGround(rawMouse, cam, out var world))
                    {
                        level.HandleClick(world);
                    }
                    // Nicht gebaut (belegt, zu teuer, über der Leiste losgelassen) → abbrechen
                    if (level.PlacingType != null || level.PlacingHero) level.CancelPlacing();
                    e.Use();
                }
                dragging = false;
                pressedTower = null;
                pressedHero = false;
            }
        }

        /// <summary>Merkt sich, dass eine Karte gedrückt wurde (für Drag & Drop).</summary>
        void RememberPress(Rect r, TowerType tower, bool hero)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !GUI.enabled || !r.Contains(e.mousePosition)) return;
            pressedTower = tower;
            pressedHero = hero;
            pressRaw = currentRaw;
        }

        // ---------------------------------------------------------------- Kopfzeile

        void DrawTopBar()
        {
            float vw = UiKit.VirtualWidth;
            var r = new Rect(-4f, -4f, vw + 8f, UiKit.TopBarHeight + 4f);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            UiKit.PillLabel(new Rect(18f, 10f, 140f, 36f), UiKit.Icon("Kuh"), $"{level.CowsRemaining}/{level.Def.CowCount}");
            UiKit.PillLabel(new Rect(176f, 10f, 170f, 36f), UiKit.Icon("Muenze"), level.Money.ToString());
            UiKit.PillLabel(new Rect(364f, 10f, 190f, 36f), UiKit.Icon("Ufo"), $"Welle {level.WavesStarted}/{level.Def.Waves.Length}");

            var nameRect = new Rect(570f, 8f, vw - UiKit.SideBarWidth - 590f, 40f);
            if (nameRect.width > 80f)
            {
                UiKit.Title.alignment = TextAnchor.MiddleLeft;
                UiKit.OutlinedText(nameRect, level.Def.Name, UiKit.Title, new Color(1f, 0.9f, 0.55f));
            }
            // Menü und Tempo oben rechts
            float x = vw - UiKit.SideBarWidth + 12f;
            GUI.enabled = Playing;
            if (UiKit.BtnRect(new Rect(x, 8f, 110f, 40f), "Menü", UiKit.Button, null)) level.SetPaused(true);
            bool fast = LevelManager.GameSpeed > 1f;
            if (UiKit.BtnRect(new Rect(x + 122f, 8f, 112f, 40f), fast ? "Tempo 2x" : "Tempo 1x", fast ? UiKit.YellowButton : UiKit.Button, null)) level.ToggleSpeed();
            GUI.enabled = true;
        }

        // ---------------------------------------------------------------- Seitenleiste

        void DrawSideBar()
        {
            float vw = UiKit.VirtualWidth;
            var r = new Rect(vw - UiKit.SideBarWidth, UiKit.TopBarHeight - 4f, UiKit.SideBarWidth + 4f, UiKit.RefHeight - UiKit.TopBarHeight + 8f);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 12f, UiKit.SideBarWidth - 24f, r.height - 24f));

            if (level.Hero == null)
            {
                GUI.enabled = Playing;
                Rect heroRect = GUILayoutUtility.GetRect(10f, 56f, GUILayout.Height(56f));
                RememberPress(heroRect, null, true);
                if (UiKit.BtnRect(heroRect, level.PlacingHero ? "Platz wählen …" : "Held: Bauer Heinz",
                        level.PlacingHero ? UiKit.ActiveButton : UiKit.YellowButton, UiKit.Icon("BauerHeinz")))
                {
                    if (level.PlacingHero) level.CancelPlacing();
                    else level.BeginPlacingHero();
                }
                GUI.enabled = true;
                GUILayout.Space(4f);
            }

            UiKit.Heading("Türme", UiKit.Title, GUILayout.Height(30f));
            for (int i = 0; i < level.AvailableTowers.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int k = i; k < Mathf.Min(i + 2, level.AvailableTowers.Count); k++) TowerCard(level.AvailableTowers[k]);
                GUILayout.EndHorizontal();
            }
            if (level.AvailableTowers.Count < TowerType.All.Length)
            {
                GUILayout.Label("Weitere Türme gibt es über Werkstätten auf dem Hof.", UiKit.Small);
            }

            if (level.PlacingHero)
            {
                GUILayout.Label("Tippe auf ein hell markiertes Feld – oder zieh die Karte direkt dorthin.", UiKit.Small);
                if (UiKit.Btn("Abbrechen", UiKit.RedButton, GUILayout.Height(40f))) level.CancelPlacing();
            }
            else if (level.PlacingType != null)
            {
                GUILayout.Label(level.PlacingType.Description, UiKit.Small);
                if (UiKit.Btn("Abbrechen", UiKit.RedButton, GUILayout.Height(40f))) level.CancelPlacing();
            }

            GUILayout.FlexibleSpace();
            DrawAbilities();

            if (level.State == LevelState.Building && level.HasMoreWaves)
            {
                bool lastWave = level.WavesStarted == level.Def.Waves.Length - 1;
                string label = lastWave ? "Letzte Welle!" : $"Welle {level.WavesStarted + 1} starten";
                if (UiKit.Btn(label, UiKit.BigButton, GUILayout.Height(66f))) level.StartNextWave();
            }
            else if (level.State == LevelState.WaveRunning)
            {
                Rect wr = GUILayoutUtility.GetRect(10f, 66f, GUILayout.Height(66f));
                GUI.Box(wr, GUIContent.none, UiKit.Card);
                UiKit.OutlinedText(wr, $"Welle {level.WavesStarted} läuft", UiKit.ButtonText, new Color(1f, 0.9f, 0.55f));
            }

            GUILayout.EndArea();
        }

        /// <summary>Blaue Turm-Karte mit Bild, Name und Preis (wie in Bloons TD6).</summary>
        void TowerCard(TowerType type)
        {
            bool active = level.PlacingType == type;
            bool affordable = level.Money >= type.Cost;
            Rect r = GUILayoutUtility.GetRect(112f, 78f, GUILayout.Width(112f), GUILayout.Height(78f));
            GUI.enabled = Playing && (active || affordable);
            RememberPress(r, type, false);
            if (GUI.Button(r, GUIContent.none, active ? UiKit.ActiveButton : UiKit.BlueCard))
            {
                GameAudio.Play(SoundId.Click);
                if (active) level.CancelPlacing();
                else level.BeginPlacing(type);
            }
            var icon = UiKit.TowerIcon(type);
            if (icon != null) GUI.DrawTexture(new Rect(r.x + 28f, r.y + 3f, 56f, 48f), icon, ScaleMode.ScaleToFit);
            UiKit.ButtonText.fontSize = 13;
            UiKit.OutlinedText(new Rect(r.x + 2f, r.y + 44f, r.width - 4f, 18f), type.ShortName, UiKit.ButtonText, Color.white, 1.4f);
            UiKit.ButtonText.fontSize = 16;
            UiKit.OutlinedText(new Rect(r.x + 2f, r.y + 59f, r.width - 4f, 18f), type.Cost.ToString(), UiKit.ButtonText,
                affordable ? new Color(1f, 0.88f, 0.3f) : new Color(1f, 0.45f, 0.4f), 1.4f);
            GUI.enabled = true;
        }

        /// <summary>Knöpfe für Fähigkeiten (Held und Stufe-5-Türme).</summary>
        void DrawAbilities()
        {
            int shown = 0;
            var hero = level.Hero;
            if (hero != null && hero.HasBlast)
            {
                AbilityButton("Ladung Schrot", UiKit.Icon("BauerHeinz"), hero.BlastReady, hero.BlastCooldownLeft, level.UseHeroBlast);
                if (hero.HasTractor) AbilityButton("Traktor", UiKit.Icon("Traktor"), hero.TractorReady, hero.TractorCooldownLeft, level.UseHeroTractor);
            }
            foreach (var tower in level.Towers)
            {
                if (!tower.HasAbility || shown >= 2) continue;
                shown++;
                var t = tower;
                AbilityButton(TowerType.AbilityName(tower.Stats.Ability), UiKit.TowerIcon(tower.Type), tower.AbilityReady, tower.AbilityCooldownLeft,
                    () => level.UseAbility(t));
            }
        }

        void AbilityButton(string name, Texture2D icon, bool ready, float cooldownLeft, System.Action use)
        {
            string label = ready ? name : $"{name} {Mathf.CeilToInt(cooldownLeft)}s";
            GUI.enabled = ready && level.State == LevelState.WaveRunning;
            if (UiKit.IconBtn(label, icon, ready ? UiKit.YellowButton : UiKit.Button, GUILayout.Height(40f))) use();
            GUI.enabled = true;
        }

        // ---------------------------------------------------------------- Upgrade-Fenster und Held

        Rect BottomPanel(float height)
        {
            float width = UiKit.VirtualWidth - UiKit.SideBarWidth - 16f;
            var r = new Rect(8f, UiKit.RefHeight - height - 8f, width, height);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Dialog);
            return r;
        }

        /// <summary>Upgrade-Fenster wie in Bloons TD6: links der Turm, daneben drei Pfade als Karten.</summary>
        void DrawUpgradePanel(Tower tower)
        {
            const float height = 214f;
            var r = BottomPanel(height);

            const float leftWidth = 196f;
            var icon = UiKit.TowerIcon(tower.Type);
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, leftWidth, height - 20f));
            GUILayout.BeginHorizontal();
            if (icon != null) GUILayout.Label(icon, GUILayout.Width(56f), GUILayout.Height(56f));
            UiKit.Heading(tower.Type.ShortName, UiKit.Title, GUILayout.Height(56f));
            GUILayout.EndHorizontal();
            if (UiKit.Btn($"Ziel: {TowerType.TargetModeName(tower.TargetMode)}", UiKit.Button, GUILayout.Height(38f))) level.CycleTargetMode();
            if (UiKit.Btn($"Verkaufen +{tower.SellValue}", UiKit.RedButton, GUILayout.Height(38f))) level.SellSelected();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Height(34f))) level.Select(null);
            GUILayout.EndArea();

            float colWidth = (r.width - leftWidth - 36f) / TowerType.PathCount;
            for (int p = 0; p < TowerType.PathCount; p++)
            {
                var path = tower.Type.Paths[p];
                var col = new Rect(r.x + leftWidth + 24f + p * colWidth, r.y + 10f, colWidth - 8f, height - 20f);
                GUI.Box(col, GUIContent.none, UiKit.Card);

                // Pfadname und Stufen-Kästchen
                UiKit.Title.alignment = TextAnchor.MiddleLeft;
                UiKit.OutlinedText(new Rect(col.x + 10f, col.y + 4f, col.width - 100f, 28f), path.Name, UiKit.Title, path.Color);
                for (int t = 0; t < TowerType.MaxTier; t++)
                {
                    var pip = new Rect(col.xMax - 90f + t * 17f, col.y + 11f, 13f, 13f);
                    UiKit.DrawRect(new Rect(pip.x - 1f, pip.y - 1f, pip.width + 2f, pip.height + 2f), UiKit.OutlineColor);
                    UiKit.DrawRect(pip, t < tower.Tiers[p] ? path.Color : new Color(1f, 1f, 1f, 0.18f));
                }

                GUILayout.BeginArea(new Rect(col.x + 10f, col.y + 36f, col.width - 20f, col.height - 42f));
                var next = tower.NextTier(p);
                if (next == null)
                {
                    GUILayout.Label("Alle Stufen gekauft!", UiKit.Label);
                }
                else
                {
                    GUILayout.Label($"Stufe {tower.Tiers[p] + 1}: {next.Name}", UiKit.Label);
                    GUILayout.Label(next.Description, UiKit.Small, GUILayout.Height(58f));
                    GUILayout.FlexibleSpace();
                    string reason = tower.WhyCannotUpgrade(p);
                    if (reason != null)
                    {
                        GUILayout.Label(reason, UiKit.Small);
                    }
                    else
                    {
                        int cost = level.UpgradeCost(tower, p);
                        GUI.enabled = level.Money >= cost;
                        string label = cost < next.Cost ? $"{cost} (statt {next.Cost})" : cost.ToString();
                        if (UiKit.IconBtn(label, UiKit.Icon("Muenze"), UiKit.ActiveButton, GUILayout.Height(44f))) level.UpgradeSelected(p);
                        GUI.enabled = true;
                    }
                }
                GUILayout.EndArea();
            }
        }

        /// <summary>Infos zum Helden: Stufe, Erfahrung, Fähigkeiten, Zielmodus.</summary>
        void DrawHeroPanel(Hero hero)
        {
            const float height = 180f;
            var r = BottomPanel(height);
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 12f, r.width - 28f, r.height - 24f));
            GUILayout.BeginHorizontal();
            var icon = UiKit.Icon("BauerHeinz");
            if (icon != null) GUILayout.Label(icon, GUILayout.Width(120f), GUILayout.Height(150f));
            GUILayout.BeginVertical(GUILayout.Width(280f));
            UiKit.Heading($"{Hero.HeroName} – Stufe {hero.Level}", UiKit.Title, GUILayout.Height(34f));
            Rect bar = GUILayoutUtility.GetRect(260f, 18f);
            UiKit.DrawBar(bar, hero.XpProgress, new Color(1f, 0.8f, 0.25f));
            GUILayout.Label(hero.Level >= Hero.MaxLevel ? "Höchste Stufe erreicht" : "Erfahrung durch besiegte Aliens und geschaffte Wellen", UiKit.Small);
            if (UiKit.Btn($"Ziel: {TowerType.TargetModeName(hero.TargetMode)}", UiKit.Button, GUILayout.Height(38f))) hero.CycleTargetMode();
            GUILayout.EndVertical();
            GUILayout.Space(16f);
            GUILayout.BeginVertical();
            var st = hero.Stats;
            GUILayout.Label($"Schrotflinte: {st.Projectiles} Kugeln à {st.Damage:0} Schaden, {st.FireRate:0.0} Schüsse/s, Reichweite {st.Range:0.0}" +
                            (st.SeesCamo ? ", sieht Getarnte" : "") + (st.ArmorPiercing ? ", durchschlägt Panzer" : ""), UiKit.Small);
            GUILayout.Label(hero.HasBlast ? $"Ladung Schrot: {hero.BlastDamage:0} Schaden an allen Aliens in Reichweite" : $"Ab Stufe {Hero.BlastLevel}: Ladung Schrot", UiKit.Small);
            GUILayout.Label(hero.HasTractor ? $"Traktor-Raserei: überrollt alle Aliens am Boden ({hero.TractorDamage:0} Schaden)" : $"Ab Stufe {Hero.TractorLevel}: Traktor-Raserei", UiKit.Small);
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Height(36f), GUILayout.Width(150f))) level.Select(null);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ---------------------------------------------------------------- Anzeigen über der Spielwelt

        void DrawWorldOverlays()
        {
            foreach (var enemy in level.Enemies)
            {
                if (!UiKit.WorldToGui(cam, enemy.HealthBarPoint, out var p)) continue;
                float w = 36f * Mathf.Clamp(enemy.Type.Scale, 1f, 1.8f);
                float frac = Mathf.Clamp01(enemy.Hp / enemy.MaxHp);
                UiKit.DrawBar(new Rect(p.x - w / 2f, p.y, w, 7f), frac, Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(0.3f, 0.9f, 0.25f), frac));
            }

            foreach (var ft in level.FloatingTexts)
            {
                float k = ft.Age / LevelManager.FloatingTextLifetime;
                if (!UiKit.WorldToGui(cam, ft.Position, out var p)) continue;
                p.y -= k * 40f;
                var c = ft.Color;
                c.a = 1f - k * k;
                UiKit.ShadowLabel(p, ft.Text, c);
            }
        }

        // ---------------------------------------------------------------- Fenster

        void ResumeGame()
        {
            confirmLeave = false;
            level.SetPaused(false);
        }

        void DrawPauseMenu()
        {
            Rect box = UiKit.DrawOverlay(blockers, 460f, confirmLeave ? 320f : 470f);
            GUILayout.BeginArea(new Rect(box.x + 28f, box.y + 22f, box.width - 56f, box.height - 44f));

            if (confirmLeave)
            {
                UiKit.Heading("Level verlassen?", UiKit.Big, GUILayout.Height(60f));
                GUILayout.Label("Dein Fortschritt in diesem Level geht verloren und du bekommst keine Belohnung.", UiKit.Center);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (UiKit.Btn("Abbrechen", UiKit.Button, GUILayout.Height(56f))) confirmLeave = false;
                if (UiKit.Btn("Verlassen", UiKit.RedButton, GUILayout.Height(56f))) level.BackToFarm();
                GUILayout.EndHorizontal();
            }
            else
            {
                UiKit.Heading("Pause", UiKit.Big, GUILayout.Height(60f));
                GUILayout.Space(8f);
                if (UiKit.Btn("Weiterspielen", UiKit.BigButton, GUILayout.Height(62f))) ResumeGame();
                GUILayout.Space(6f);
                if (UiKit.Btn("Neu starten", UiKit.YellowButton, GUILayout.Height(52f))) level.Restart();
                GUILayout.Space(6f);
                if (UiKit.Btn("Level verlassen", UiKit.RedButton, GUILayout.Height(52f))) confirmLeave = true;
                GUILayout.Space(10f);
                GUILayout.BeginHorizontal();
                if (UiKit.Btn(GameAudio.MusicOn ? "Musik an" : "Musik aus", UiKit.Button, GUILayout.Height(42f))) GameAudio.MusicOn = !GameAudio.MusicOn;
                if (UiKit.Btn(GameAudio.SoundOn ? "Ton an" : "Ton aus", UiKit.Button, GUILayout.Height(42f))) GameAudio.SoundOn = !GameAudio.SoundOn;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        /// <summary>Fenster mit großem Bild links, Titel, Name und Text rechts.</summary>
        bool PictureDialog(string title, string name, string text, Texture2D icon, string button, float height = 340f)
        {
            Rect box = UiKit.DrawOverlay(blockers, 640f, height);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 18f, box.width - 48f, box.height - 36f));
            UiKit.Heading(title, UiKit.Big, GUILayout.Height(54f));
            GUILayout.BeginHorizontal();
            if (icon != null) GUILayout.Label(icon, GUILayout.Width(150f), GUILayout.Height(150f));
            GUILayout.BeginVertical();
            UiKit.Heading(name, UiKit.Title, GUILayout.Height(32f));
            GUILayout.Label(text, UiKit.Label);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            bool clicked = UiKit.Btn(button, UiKit.BigButton, GUILayout.Height(56f));
            GUILayout.EndArea();
            return clicked;
        }

        void DrawEnemyIntro()
        {
            var type = level.NewEnemyIntro;
            if (PictureDialog("Neuer Gegner!", type.Name, type.Description, UiKit.Icon(type.ModelName), "Verstanden!", 360f)) level.DismissEnemyIntro();
        }

        void DrawGift()
        {
            var tower = level.GiftedTower;
            string text = tower.Description + $"\n\nKosten: {tower.Cost} Münzen. Du findest ihn rechts bei den Türmen.";
            if (PictureDialog("Neuer Turm!", tower.Name, text, UiKit.TowerIcon(tower), "Super!")) level.DismissGift();
        }

        void DrawIntro()
        {
            Rect box = UiKit.DrawOverlay(blockers, 700f, 470f);
            GUILayout.BeginArea(new Rect(box.x + 28f, box.y + 20f, box.width - 56f, box.height - 40f));
            UiKit.Heading(level.Def.Name, UiKit.Big, GUILayout.Height(60f));

            string story = level.Def.Id == Levels.All[0].Id
                ? "Ein Alien-Schiff vom Planeten Zorb ist auf Bauer Heinz' Weide bruchgelandet. " +
                  "Die Aliens haben Bertas Milch probiert – und jetzt wollen sie ALLE Kühe!\n\n" +
                  "Baue Türme neben den Weg, damit die Aliens die Weide nicht erreichen. " +
                  "Schnappt sich ein Alien eine Kuh, besiege es, bevor es zurück zum Ufo kommt."
                : "Die Aliens kommen wieder – diesmal stärker und auf einem neuen Weg. Beschütze die Kühe!";
            GUILayout.BeginHorizontal();
            var cow = UiKit.Icon("Kuh");
            if (cow != null) GUILayout.Label(cow, GUILayout.Width(130f), GUILayout.Height(130f));
            GUILayout.Label(story, UiKit.Label);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            Rect stars = GUILayoutUtility.GetRect(10f, 40f);
            UiKit.DrawStars(new Rect(stars.x, stars.y, 140f, 40f), 3, 34f);
            GUILayout.Label("3 Sterne: keine Kuh verloren  •  2 Sterne: höchstens 2  •  1 Stern: geschafft\n" +
                            $"Belohnung bei 3 Sternen: {level.Def.RewardCorn} Mais und {level.Def.RewardMilk} Milch", UiKit.Small);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (UiKit.Btn("Zurück zum Hof", UiKit.Button, GUILayout.Height(60f), GUILayout.Width(220f))) level.BackToFarm();
            if (UiKit.Btn("Los geht's!", UiKit.BigButton, GUILayout.Height(60f))) level.BeginLevel();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawEndScreen(bool won)
        {
            Rect box = UiKit.DrawOverlay(blockers, 600f, 430f);
            GUILayout.BeginArea(new Rect(box.x + 28f, box.y + 20f, box.width - 56f, box.height - 40f));
            UiKit.Heading(won ? "Gewonnen!" : "Alle Kühe weg!", UiKit.Big, GUILayout.Height(60f));

            if (won)
            {
                Rect starsRect = GUILayoutUtility.GetRect(box.width - 56f, 96f);
                UiKit.DrawStars(starsRect, level.Stars, 86f);
                GUILayout.Label($"Kühe gerettet: {level.CowsRemaining}/{level.Def.CowCount}   •   Bestes Ergebnis: {level.BestStars} Sterne", UiKit.Center);

                var reward = level.Reward;
                if (reward != null)
                {
                    GUILayout.Space(6f);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    var corn = UiKit.Icon("Maiskolben");
                    var milk = UiKit.Icon("Milchflasche");
                    if (corn != null) GUILayout.Label(corn, GUILayout.Width(44f), GUILayout.Height(44f));
                    UiKit.Heading($"+{reward.Corn}", UiKit.Title, GUILayout.Width(110f), GUILayout.Height(44f));
                    if (milk != null) GUILayout.Label(milk, GUILayout.Width(44f), GUILayout.Height(44f));
                    UiKit.Heading($"+{reward.Milk}", UiKit.Title, GUILayout.Width(110f), GUILayout.Height(44f));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    if (reward.CornLost > 0 || reward.MilkLost > 0) GUILayout.Label("Lager voll – baue Silos und Tanks!", UiKit.Center);
                }
            }
            else
            {
                GUILayout.Label("Die Aliens haben alle Kühe entführt. Versuch es mit einer anderen Turm-Aufstellung " +
                                "oder verbessere deine Werkstätten auf dem Hof!", UiKit.Label);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (UiKit.Btn("Nochmal", UiKit.YellowButton, GUILayout.Height(60f), GUILayout.Width(200f))) level.Restart();
            if (UiKit.Btn("Zurück zum Hof", UiKit.BigButton, GUILayout.Height(60f))) level.BackToFarm();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
