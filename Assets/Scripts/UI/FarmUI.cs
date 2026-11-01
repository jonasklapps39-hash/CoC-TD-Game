using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Bedienoberfläche des Hofs (IMGUI): Rohstoffe, Gebäude-Infos, Baumenü und Weltkarte.</summary>
    public class FarmUI : MonoBehaviour
    {
        FarmManager farm;
        Camera cam;
        readonly List<Rect> blockers = new List<Rect>();
        Vector2 shopScroll;
        Vector2 mapScroll;
        Vector2 gemScroll;

        FarmState State => farm.Farm;

        // Drag & Drop: Gebäude festhalten und ziehen
        const float DragThreshold = 12f;
        BuildingData pressedBuilding;
        Vector2 pressRaw;
        bool draggingGhost;
        bool moveByDrag;

        /// <summary>
        /// Gebäude festhalten und ziehen verschiebt es (Loslassen auf freiem Platz bestätigt, sonst springt es zurück).
        /// Beim Bauen und Verschieben folgt die Vorschau dem Finger.
        /// </summary>
        void HandleDrag(Vector2 rawMouse)
        {
            var e = Event.current;
            bool blocked = farm.AnyWindowOpen || UiKit.IsOverUi(rawMouse, blockers);

            if (e.type == EventType.MouseDown && e.button == 0 && !blocked && UiKit.ScreenToGround(rawMouse, cam, out var down))
            {
                if (farm.Mode != FarmMode.Normal)
                {
                    draggingGhost = true;
                }
                else
                {
                    pressedBuilding = State.BuildingAt(new Vector2Int(Mathf.FloorToInt(down.x), Mathf.FloorToInt(down.z)));
                    pressRaw = rawMouse;
                }
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (pressedBuilding != null && !draggingGhost && (rawMouse - pressRaw).magnitude > DragThreshold * UiKit.Scale)
                {
                    farm.Select(pressedBuilding);
                    farm.BeginMoving();
                    draggingGhost = true;
                    moveByDrag = true;
                }
                if (draggingGhost && UiKit.ScreenToGround(rawMouse, cam, out var world))
                {
                    farm.DragGhostTo(world);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (draggingGhost && moveByDrag)
                {
                    if (farm.GhostValid) farm.ConfirmPlacement();
                    else farm.CancelMode();
                    e.Use();
                }
                draggingGhost = false;
                moveByDrag = false;
                pressedBuilding = null;
            }
        }

        public void Init(FarmManager farm)
        {
            this.farm = farm;
            cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) UiKit.FitCamera(cam, FarmState.Width, FarmState.Height, 52f);
        }

        void OnGUI()
        {
            if (farm == null || cam == null) return;

            Vector2 rawMouse = UiKit.BeginGui();
            // Vor dem Zeichnen: "blockers" enthält noch die Bedienelemente des letzten Durchlaufs
            HandleDrag(rawMouse);
            blockers.Clear();

            DrawWorldOverlays();
            DrawTopBar();
            DrawSideBar();
            if (farm.ShopOpen) DrawShop();
            if (farm.MapOpen) DrawWorldMap();
            if (farm.TalentsOpen) DrawTalents();
            if (farm.GemShopOpen) DrawGemShop();

            if (UiKit.EscapePressed())
            {
                if (farm.AnyWindowOpen) farm.CloseWindows();
                else if (farm.Mode != FarmMode.Normal) farm.CancelMode();
                else farm.Select(null);
            }
            else if (UiKit.TryGetWorldClick(rawMouse, blockers, cam, out var world))
            {
                farm.HandleClick(world);
            }
        }

        // ---------------------------------------------------------------- Kopfzeile und Seitenleiste

        void DrawTopBar()
        {
            float vw = UiKit.VirtualWidth;
            var r = new Rect(-4f, -4f, vw + 8f, UiKit.TopBarHeight + 4f);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            UiKit.PillLabel(new Rect(18f, 10f, 230f, 36f), UiKit.Icon("Maiskolben"),
                $"{(int)State.Amount(Resource.Mais)} / {(int)State.Capacity(Resource.Mais)}");
            UiKit.PillLabel(new Rect(266f, 10f, 230f, 36f), UiKit.Icon("Milchflasche"),
                $"{(int)State.Amount(Resource.Milch)} / {(int)State.Capacity(Resource.Milch)}");
            var starRect = new Rect(514f, 10f, 110f, 36f);
            GUI.Box(starRect, GUIContent.none, UiKit.Pill);
            GUI.color = new Color(1f, 0.82f, 0.1f);
            GUI.DrawTexture(new Rect(starRect.x + 4f, starRect.y + 2f, 32f, 32f), UiKit.Star);
            GUI.color = Color.white;
            UiKit.Title.alignment = TextAnchor.MiddleLeft;
            UiKit.OutlinedText(new Rect(starRect.x + 42f, starRect.y, 66f, 36f), State.TotalStars.ToString(), UiKit.Title, Color.white);
            UiKit.PillLabel(new Rect(642f, 10f, 140f, 36f), UiKit.Icon("MistgabelBauer"), $"{State.BuildersFree}/{State.BuildersTotal}");
            var gemRect = new Rect(800f, 10f, 150f, 36f);
            if (gemRect.xMax < vw - 10f)
            {
                UiKit.PillLabel(gemRect, UiKit.Icon("Diamant"), State.Gems.ToString());
                if (GUI.Button(gemRect, GUIContent.none, GUIStyle.none)) farm.GemShopOpen = true;
            }
        }

        void DrawSideBar()
        {
            float vw = UiKit.VirtualWidth;
            var r = new Rect(vw - UiKit.SideBarWidth, UiKit.TopBarHeight, UiKit.SideBarWidth, UiKit.RefHeight - UiKit.TopBarHeight);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, r.width - 24f, r.height - 20f));

            if (farm.Mode != FarmMode.Normal)
            {
                DrawPlacementPanel();
            }
            else if (farm.Selected != null)
            {
                DrawBuildingPanel(farm.Selected);
            }
            else
            {
                UiKit.Heading("Dein Hof", UiKit.Title);
                GUILayout.Label("Tippe auf ein Gebäude, um es zu verbessern oder Mais und Milch einzusammeln.", UiKit.Small);
                GUILayout.Space(10f);
                if (UiKit.Btn("Bauen", UiKit.YellowButton, GUILayout.Height(56f))) farm.ShopOpen = true;
                GUILayout.Space(4f);
                if (UiKit.Btn($"Bauernwissen ({State.AvailableStars} Sterne)", UiKit.BlueCard, GUILayout.Height(48f))) farm.TalentsOpen = true;
                GUILayout.Space(4f);
                if (UiKit.IconBtn("Shop", UiKit.Icon("Diamant"), UiKit.Button, GUILayout.Height(48f))) farm.GemShopOpen = true;
                GUILayout.Space(6f);
                GUILayout.BeginHorizontal();
                if (UiKit.Btn(GameAudio.MusicOn ? "Musik: an" : "Musik: aus", UiKit.Button, GUILayout.Height(36f))) GameAudio.MusicOn = !GameAudio.MusicOn;
                if (UiKit.Btn(GameAudio.SoundOn ? "Ton: an" : "Ton: aus", UiKit.Button, GUILayout.Height(36f))) GameAudio.SoundOn = !GameAudio.SoundOn;
                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            if (farm.Mode == FarmMode.Normal)
            {
                if (UiKit.IconBtn("Weltkarte", UiKit.Icon("Ufo"), UiKit.BigButton, GUILayout.Height(66f))) farm.MapOpen = true;
            }
            GUILayout.EndArea();
        }

        void DrawPlacementPanel()
        {
            var type = farm.Mode == FarmMode.Placing ? farm.PlacingType : farm.Selected.TypeDef;
            UiKit.Heading(farm.Mode == FarmMode.Placing ? $"{type.Name} bauen" : $"{type.Name} verschieben", UiKit.Title);
            GUILayout.Label("Zieh das Gebäude an den gewünschten Platz oder tippe auf den Hof. Grün = frei, rot = belegt.", UiKit.Small);
            if (farm.Mode == FarmMode.Placing)
            {
                GUILayout.Label($"Kosten: {type.CostForLevel(1)} {FarmBuildingType.ResourceName(type.CostResource)}\n" +
                                $"Bauzeit: {UiKit.FormatTime(type.SecondsForLevel(1))}", UiKit.Small);
            }
            GUILayout.Space(10f);
            GUI.enabled = farm.GhostValid;
            if (UiKit.Btn("Bestätigen", UiKit.ActiveButton, GUILayout.Height(52f))) farm.ConfirmPlacement();
            GUI.enabled = true;
            if (UiKit.Btn("Abbrechen", UiKit.RedButton, GUILayout.Height(44f))) farm.CancelMode();
        }

        void DrawBuildingPanel(BuildingData b)
        {
            var type = b.TypeDef;
            var buildingIcon = UiKit.Icon(type.Id);
            if (buildingIcon != null)
            {
                Rect ir = GUILayoutUtility.GetRect(10f, 90f, GUILayout.Height(90f));
                GUI.DrawTexture(ir, buildingIcon, ScaleMode.ScaleToFit);
            }
            UiKit.Heading(b.Level == 0 ? $"{type.Name} (im Bau)" : $"{type.Name} – Stufe {b.Level}", UiKit.Title);
            GUILayout.Label(type.Description, UiKit.Small);
            GUILayout.Space(6f);

            string stats = BuildingStats(b);
            if (stats.Length > 0) GUILayout.Label(stats, UiKit.Small);

            if (b.IsUpgrading)
            {
                GUILayout.Label($"{(b.Level == 0 ? "Wird gebaut" : $"Wird auf Stufe {b.Level + 1} verbessert")}: noch {UiKit.FormatTime(State.RemainingSeconds(b))}", UiKit.Small);
                Rect bar = GUILayoutUtility.GetRect(10f, 14f);
                UiKit.DrawBar(bar, State.UpgradeProgress(b), new Color(0.4f, 0.8f, 0.3f));
                int gems = State.InstantFinishCost(b);
                GUI.enabled = State.Gems >= gems;
                if (UiKit.IconBtn($"Sofort fertig: {gems}", UiKit.Icon("Diamant"), UiKit.BlueCard, GUILayout.Height(46f))) farm.InstantFinishSelected();
                GUI.enabled = true;
            }
            else if (b.Level < type.MaxLevel)
            {
                string reason = State.WhyCannotUpgrade(b);
                int cost = type.CostForLevel(b.Level + 1);
                GUI.enabled = reason == null;
                string label = $"Verbessern auf Stufe {b.Level + 1}\n{cost} {FarmBuildingType.ResourceName(type.CostResource)} · {UiKit.FormatTime(type.SecondsForLevel(b.Level + 1))}";
                if (UiKit.Btn(label, UiKit.ActiveButton, GUILayout.Height(60f))) farm.UpgradeSelected();
                GUI.enabled = true;
                if (reason != null) GUILayout.Label(reason, UiKit.Small);
            }
            else
            {
                GUILayout.Label("Maximale Stufe erreicht", UiKit.Small);
            }

            if (type.IsProducer && b.Level > 0)
            {
                GUI.enabled = b.Stored >= 1;
                var resIcon = UiKit.Icon(type.ProducedResource == Resource.Mais ? "Maiskolben" : "Milchflasche");
                if (UiKit.IconBtn($"Einsammeln {(int)b.Stored}", resIcon, UiKit.YellowButton, GUILayout.Height(46f))) farm.Collect(b);
                GUI.enabled = true;
            }

            if (UiKit.Btn("Verschieben", UiKit.Button, GUILayout.Height(40f))) farm.BeginMoving();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Height(36f))) farm.Select(null);
        }

        string BuildingStats(BuildingData b)
        {
            var type = b.TypeDef;
            if (b.Level == 0) return "";
            var lines = new List<string>();

            if (type == FarmBuildingType.Bauernhaus)
            {
                lines.Add($"Lager im Bauernhaus: {FarmState.FarmhouseCapacity(b.Level)} Mais und Milch");
            }
            if (type.IsProducer)
            {
                string res = FarmBuildingType.ResourceName(type.ProducedResource);
                lines.Add($"Produktion: {State.ProductionPerMinute(b):0} {res} pro Minute");
                lines.Add($"Gesammelt: {(int)b.Stored} / {(int)State.ProducerCapacity(b)}");
            }
            if (type.IsStorage)
            {
                lines.Add($"Lagert: {type.StorageCapacity[b.Level - 1]} {FarmBuildingType.ResourceName(type.StoredResource)}");
            }
            foreach (var unlock in type.Unlocks)
            {
                int tier = FarmState.TowerLevelForWorkshop(b.Level, unlock.FromLevel);
                lines.Add(tier > 0
                    ? $"{unlock.Tower.Name}: Upgrades im Level bis Stufe {tier}"
                    : $"{unlock.Tower.Name}: ab Werkstatt-Stufe {unlock.FromLevel}");
            }
            return string.Join("\n", lines);
        }

        // ---------------------------------------------------------------- Fenster

        void DrawShop()
        {
            Rect box = UiKit.DrawOverlay(blockers, 760f, 600f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Bauen", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.ShopOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            shopScroll = GUILayout.BeginScrollView(shopScroll);
            foreach (var type in FarmBuildingType.All)
            {
                if (type == FarmBuildingType.Bauernhaus) continue;
                string reason = State.WhyCannotBuild(type);

                GUILayout.BeginHorizontal(UiKit.Card);
                var icon = UiKit.Icon(type.Id);
                if (icon != null) GUILayout.Label(icon, GUILayout.Width(84f), GUILayout.Height(84f));
                GUILayout.BeginVertical();
                UiKit.Heading($"{type.Name}   {State.Count(type)}/{State.MaxCount(type)}", UiKit.Title, GUILayout.Height(28f));
                GUILayout.Label(type.Description, UiKit.Small);
                GUILayout.Label($"{type.CostForLevel(1)} {FarmBuildingType.ResourceName(type.CostResource)} · Bauzeit {UiKit.FormatTime(type.SecondsForLevel(1))}" +
                                (reason != null ? $"   –   {reason}" : ""), UiKit.Small);
                GUILayout.EndVertical();
                GUI.enabled = reason == null;
                if (UiKit.Btn("Bauen", UiKit.ActiveButton, GUILayout.Width(120f), GUILayout.Height(60f))) farm.BeginPlacing(type);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Shop: Diamanten ausgeben (Knechte, Rohstoffe, Kuh-Skins), Echtgeld-Pakete als Vorschau.</summary>
        void DrawGemShop()
        {
            Rect box = UiKit.DrawOverlay(blockers, 860f, 620f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Shop", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            var gem = UiKit.Icon("Diamant");
            if (gem != null) GUILayout.Label(gem, GUILayout.Width(40f), GUILayout.Height(40f));
            UiKit.Heading(State.Gems.ToString(), UiKit.Title, GUILayout.Width(90f), GUILayout.Height(40f));
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.GemShopOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Diamanten gibt es für jeden neuen Stern ({FarmState.GemsPerNewStar}) und beim ersten Schaffen eines Levels ({FarmState.GemsFirstClear}).", UiKit.Small);

            gemScroll = GUILayout.BeginScrollView(gemScroll);
            GUILayout.BeginHorizontal();
            // Knecht
            GUILayout.BeginVertical(UiKit.Card, GUILayout.Width(250f));
            UiKit.Heading("Zusätzlicher Knecht", UiKit.Title, GUILayout.Height(30f));
            var worker = UiKit.Icon("MistgabelBauer");
            if (worker != null) GUILayout.Label(worker, GUILayout.Height(80f));
            GUILayout.Label($"Ein Knecht mehr zum Bauen. Angeheuert: {State.Data.ExtraBuilders}/{FarmState.MaxExtraBuilders}", UiKit.Small);
            GUI.enabled = State.Data.ExtraBuilders < FarmState.MaxExtraBuilders && State.Gems >= State.ExtraBuilderCost;
            if (UiKit.IconBtn(State.Data.ExtraBuilders < FarmState.MaxExtraBuilders ? State.ExtraBuilderCost.ToString() : "Maximum", gem, UiKit.ActiveButton, GUILayout.Height(44f))
                && State.BuyExtraBuilder())
            {
                State.Save();
                GameAudio.Play(SoundId.LevelUp);
            }
            GUI.enabled = true;
            GUILayout.EndVertical();

            // Rohstoffe
            foreach (var res in new[] { Resource.Mais, Resource.Milch })
            {
                GUILayout.BeginVertical(UiKit.Card, GUILayout.Width(250f));
                UiKit.Heading($"{FarmState.ResourcePackAmount} {FarmBuildingType.ResourceName(res)}", UiKit.Title, GUILayout.Height(30f));
                var icon = UiKit.Icon(res == Resource.Mais ? "Maiskolben" : "Milchflasche");
                if (icon != null) GUILayout.Label(icon, GUILayout.Height(80f));
                GUILayout.Label("Sofort ins Lager (so viel hineinpasst).", UiKit.Small);
                GUI.enabled = State.Gems >= FarmState.ResourcePackGems;
                if (UiKit.IconBtn(FarmState.ResourcePackGems.ToString(), gem, UiKit.ActiveButton, GUILayout.Height(44f)))
                {
                    double added = State.BuyResourcePack(res);
                    if (added > 0)
                    {
                        State.Save();
                        GameAudio.Play(SoundId.Coin);
                    }
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            UiKit.Heading("Kuh-Skins", UiKit.Title, GUILayout.Height(32f));
            GUILayout.BeginHorizontal();
            var cowIcon = UiKit.Icon("Kuh");
            foreach (var skin in CowSkin.All)
            {
                bool owned = State.OwnsSkin(skin.Id);
                bool active = State.Data.CowSkin == skin.Id;
                GUILayout.BeginVertical(active ? UiKit.ActiveButton : UiKit.Card, GUILayout.Width(190f));
                UiKit.Heading(skin.Name, UiKit.Title, GUILayout.Height(28f));
                if (cowIcon != null)
                {
                    Rect ir = GUILayoutUtility.GetRect(10f, 80f, GUILayout.Height(80f));
                    GUI.color = skin.Tint;
                    GUI.DrawTexture(ir, cowIcon, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }
                if (active)
                {
                    GUILayout.Label("Aktiv", UiKit.Small);
                }
                else if (owned)
                {
                    if (UiKit.Btn("Auswählen", UiKit.YellowButton, GUILayout.Height(40f)))
                    {
                        State.SelectSkin(skin);
                        State.Save();
                    }
                }
                else
                {
                    GUI.enabled = State.Gems >= skin.Cost;
                    if (UiKit.IconBtn(skin.Cost.ToString(), gem, UiKit.ActiveButton, GUILayout.Height(40f)) && State.BuySkin(skin))
                    {
                        State.Save();
                        GameAudio.Play(SoundId.Moo);
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            UiKit.Heading("Diamanten kaufen", UiKit.Title, GUILayout.Height(32f));
            GUILayout.BeginHorizontal();
            foreach (var (amount, price) in new[] { (100, "1,99 €"), (550, "9,99 €"), (1200, "19,99 €") })
            {
                GUILayout.BeginVertical(UiKit.Card, GUILayout.Width(190f));
                UiKit.Heading($"{amount} Diamanten", UiKit.Title, GUILayout.Height(28f));
                GUI.enabled = false;
                UiKit.Btn(price, UiKit.Button, GUILayout.Height(40f));
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Echtgeld-Käufe kommen erst mit dem Start im Play Store.", UiKit.Small);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Talentbaum „Bauernwissen“: drei Spalten, Talente bauen aufeinander auf.</summary>
        void DrawTalents()
        {
            Rect box = UiKit.DrawOverlay(blockers, 900f, 620f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Bauernwissen", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            Rect starRect = GUILayoutUtility.GetRect(150f, 40f, GUILayout.Width(150f));
            GUI.color = new Color(1f, 0.82f, 0.1f);
            GUI.DrawTexture(new Rect(starRect.x, starRect.y + 2f, 36f, 36f), UiKit.Star);
            GUI.color = Color.white;
            UiKit.Title.alignment = TextAnchor.MiddleLeft;
            UiKit.OutlinedText(new Rect(starRect.x + 42f, starRect.y, 108f, 40f), $"{State.AvailableStars} frei", UiKit.Title, Color.white);
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.TalentsOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Label("Sterne aus den Levels gegen dauerhafte Boni tauschen. Jedes Talent gilt in allen Levels.", UiKit.Small);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            for (int c = 0; c < Talents.ColumnNames.Length; c++)
            {
                GUILayout.BeginVertical(UiKit.Card, GUILayout.Width((box.width - 64f) / 3f));
                UiKit.Heading(Talents.ColumnNames[c], UiKit.Title, GUILayout.Height(32f));
                foreach (var t in Talents.All)
                {
                    if (t.Column != c) continue;
                    DrawTalentCard(t);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawTalentCard(Talent t)
        {
            bool owned = State.HasTalent(t.Id);
            string reason = State.WhyCannotBuyTalent(t);
            GUILayout.BeginVertical(owned ? UiKit.ActiveButton : UiKit.Panel);
            UiKit.Heading($"{t.Name}  ({t.Cost} Stern{(t.Cost == 1 ? "" : "e")})", UiKit.Title, GUILayout.Height(28f));
            GUILayout.Label(t.Description, UiKit.Small, GUILayout.Height(40f));
            if (owned)
            {
                GUILayout.Label("Gelernt", UiKit.Small);
            }
            else
            {
                GUI.enabled = reason == null;
                if (UiKit.Btn(reason ?? "Lernen", UiKit.YellowButton, GUILayout.Height(34f)) && State.BuyTalent(t))
                {
                    State.Save();
                    GameAudio.Play(SoundId.LevelUp);
                }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        void DrawWorldMap()
        {
            Rect box = UiKit.DrawOverlay(blockers, 720f, 560f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Weltkarte – Heimattal", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.MapOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            mapScroll = GUILayout.BeginScrollView(mapScroll);
            for (int i = 0; i < Levels.All.Length; i++)
            {
                var def = Levels.All[i];
                bool unlocked = GameFlow.IsUnlocked(i);
                GUILayout.BeginHorizontal(UiKit.Card, GUILayout.Height(96f));
                var levelIcon = UiKit.Icon(unlocked ? (i == Levels.All.Length - 1 ? "Boss" : "KuhUfo") : "Schleicher");
                if (levelIcon != null) GUILayout.Label(levelIcon, GUILayout.Width(72f), GUILayout.Height(72f));
                GUILayout.BeginVertical();
                UiKit.Heading(def.Name, UiKit.Title, GUILayout.Height(30f));
                GUILayout.Label(unlocked
                    ? $"Belohnung bei 3 Sternen: {def.RewardCorn} Mais, {def.RewardMilk} Milch"
                    : "Gesperrt – schaffe zuerst das vorherige Level.", UiKit.Small);
                GUILayout.EndVertical();
                Rect stars = GUILayoutUtility.GetRect(130f, 60f, GUILayout.Width(130f));
                UiKit.DrawStars(stars, State.BestStars(def.Id), 36f);
                GUI.enabled = unlocked;
                if (UiKit.Btn("Spielen", UiKit.BigButton, GUILayout.Width(140f), GUILayout.Height(60f)))
                {
                    farm.MapOpen = false;
                    farm.PlayLevel(def);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Tipp: Werkstätten auf dem Hof schalten neue Türme frei und erlauben höhere Turm-Stufen im Level.", UiKit.Small);
            GUILayout.EndArea();
        }

        // ---------------------------------------------------------------- Anzeigen über den Gebäuden

        void DrawWorldOverlays()
        {
            foreach (var view in farm.Views.Values)
            {
                var b = view.Data;
                if (!UiKit.WorldToGui(cam, view.TopPoint, out var p)) continue;

                if (b.IsUpgrading)
                {
                    UiKit.DrawBar(new Rect(p.x - 40f, p.y, 80f, 10f), State.UpgradeProgress(b), new Color(0.4f, 0.8f, 0.3f));
                    UiKit.ShadowLabel(new Vector2(p.x, p.y - 12f), UiKit.FormatTime(State.RemainingSeconds(b)), Color.white, UiKit.Small);
                }
                else if (b.TypeDef.IsProducer && b.Level > 0 && b.Stored >= Mathf.Max(1f, (float)State.ProducerCapacity(b) * 0.1f))
                {
                    string res = FarmBuildingType.ResourceName(b.TypeDef.ProducedResource);
                    var bubble = new Rect(p.x - 45f, p.y - 14f, 90f, 26f);
                    string text = $"{(int)b.Stored} {res}";
                    bool clickable = farm.Mode == FarmMode.Normal && !farm.AnyWindowOpen;
                    if (!clickable) GUI.Box(bubble, text, UiKit.Bubble);
                    else if (GUI.Button(bubble, text, UiKit.Bubble)) farm.Collect(b);
                }
            }

            foreach (var ft in farm.FloatingTexts)
            {
                float k = ft.Age / LevelManager.FloatingTextLifetime;
                if (!UiKit.WorldToGui(cam, ft.Position, out var p)) continue;
                p.y -= 20f + k * 40f;
                var c = ft.Color;
                c.a = 1f - k;
                UiKit.ShadowLabel(p, ft.Text, c);
            }
        }
    }
}
