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

        FarmState State => farm.Farm;

        public void Init(FarmManager farm)
        {
            this.farm = farm;
            cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) UiKit.FitCamera(cam, FarmState.Width, FarmState.Height);
        }

        void OnGUI()
        {
            if (farm == null || cam == null) return;

            Vector2 rawMouse = UiKit.BeginGui();
            blockers.Clear();

            DrawWorldOverlays();
            DrawTopBar();
            DrawSideBar();
            if (farm.ShopOpen) DrawShop();
            if (farm.MapOpen) DrawWorldMap();

            if (UiKit.EscapePressed())
            {
                if (farm.ShopOpen || farm.MapOpen) { farm.ShopOpen = false; farm.MapOpen = false; }
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
            var r = new Rect(0f, 0f, vw, UiKit.TopBarHeight);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            string text =
                $"Mais: {(int)State.Amount(Resource.Mais)} / {(int)State.Capacity(Resource.Mais)}      " +
                $"Milch: {(int)State.Amount(Resource.Milch)} / {(int)State.Capacity(Resource.Milch)}      " +
                $"Sterne: {State.TotalStars}      Knechte: {State.BuildersFree}/{State.BuildersTotal}";
            GUI.Label(new Rect(16f, 0f, vw - 32f, UiKit.TopBarHeight), text, UiKit.Title);
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
                GUILayout.Label("Dein Hof", UiKit.Title);
                GUILayout.Label("Tippe auf ein Gebäude, um es zu verbessern oder Mais und Milch einzusammeln.", UiKit.Small);
                GUILayout.Space(10f);
                if (GUILayout.Button("Bauen", UiKit.Button, GUILayout.Height(56f))) farm.ShopOpen = true;
            }

            GUILayout.FlexibleSpace();
            if (farm.Mode == FarmMode.Normal)
            {
                if (GUILayout.Button("Zur Weltkarte", UiKit.BigButton, GUILayout.Height(64f))) farm.MapOpen = true;
            }
            GUILayout.EndArea();
        }

        void DrawPlacementPanel()
        {
            var type = farm.Mode == FarmMode.Placing ? farm.PlacingType : farm.Selected.TypeDef;
            GUILayout.Label(farm.Mode == FarmMode.Placing ? $"{type.Name} bauen" : $"{type.Name} verschieben", UiKit.Title);
            GUILayout.Label("Tippe auf den Hof, um den Platz auszuwählen. Grün = frei, rot = belegt.", UiKit.Small);
            if (farm.Mode == FarmMode.Placing)
            {
                GUILayout.Label($"Kosten: {type.CostForLevel(1)} {FarmBuildingType.ResourceName(type.CostResource)}\n" +
                                $"Bauzeit: {UiKit.FormatTime(type.SecondsForLevel(1))}", UiKit.Small);
            }
            GUILayout.Space(10f);
            GUI.enabled = farm.GhostValid;
            if (GUILayout.Button("Bestätigen", UiKit.ActiveButton, GUILayout.Height(52f))) farm.ConfirmPlacement();
            GUI.enabled = true;
            if (GUILayout.Button("Abbrechen", UiKit.Button, GUILayout.Height(44f))) farm.CancelMode();
        }

        void DrawBuildingPanel(BuildingData b)
        {
            var type = b.TypeDef;
            GUILayout.Label(b.Level == 0 ? $"{type.Name} (im Bau)" : $"{type.Name} – Stufe {b.Level}", UiKit.Title);
            GUILayout.Label(type.Description, UiKit.Small);
            GUILayout.Space(6f);

            string stats = BuildingStats(b);
            if (stats.Length > 0) GUILayout.Label(stats, UiKit.Small);

            if (b.IsUpgrading)
            {
                GUILayout.Label($"{(b.Level == 0 ? "Wird gebaut" : $"Wird auf Stufe {b.Level + 1} verbessert")}: noch {UiKit.FormatTime(State.RemainingSeconds(b))}", UiKit.Small);
                Rect bar = GUILayoutUtility.GetRect(10f, 14f);
                UiKit.DrawBar(bar, State.UpgradeProgress(b), new Color(0.4f, 0.8f, 0.3f));
            }
            else if (b.Level < type.MaxLevel)
            {
                string reason = State.WhyCannotUpgrade(b);
                int cost = type.CostForLevel(b.Level + 1);
                GUI.enabled = reason == null;
                string label = $"Verbessern auf Stufe {b.Level + 1}\n{cost} {FarmBuildingType.ResourceName(type.CostResource)} · {UiKit.FormatTime(type.SecondsForLevel(b.Level + 1))}";
                if (GUILayout.Button(label, UiKit.Button, GUILayout.Height(56f))) farm.UpgradeSelected();
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
                if (GUILayout.Button($"Einsammeln ({(int)b.Stored})", UiKit.Button, GUILayout.Height(44f))) farm.Collect(b);
                GUI.enabled = true;
            }

            if (GUILayout.Button("Verschieben", UiKit.Button, GUILayout.Height(40f))) farm.BeginMoving();
            if (GUILayout.Button("Schließen", UiKit.Button, GUILayout.Height(36f))) farm.Select(null);
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
            if (type.UnlocksTower != null)
            {
                lines.Add($"Turm: {type.UnlocksTower.Name}, Upgrades im Level bis Stufe {FarmState.TowerLevelForWorkshop(b.Level)}");
            }
            return string.Join("\n", lines);
        }

        // ---------------------------------------------------------------- Fenster

        void DrawShop()
        {
            Rect box = UiKit.DrawOverlay(blockers, 760f, 600f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Bauen", UiKit.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.ShopOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            shopScroll = GUILayout.BeginScrollView(shopScroll);
            foreach (var type in FarmBuildingType.All)
            {
                if (type == FarmBuildingType.Bauernhaus) continue;
                string reason = State.WhyCannotBuild(type);

                GUILayout.BeginHorizontal(UiKit.Panel);
                GUILayout.BeginVertical();
                GUILayout.Label($"{type.Name}   ({State.Count(type)}/{State.MaxCount(type)})", UiKit.Label);
                GUILayout.Label(type.Description, UiKit.Small);
                GUILayout.Label($"{type.CostForLevel(1)} {FarmBuildingType.ResourceName(type.CostResource)} · Bauzeit {UiKit.FormatTime(type.SecondsForLevel(1))}" +
                                (reason != null ? $"   –   {reason}" : ""), UiKit.Small);
                GUILayout.EndVertical();
                GUI.enabled = reason == null;
                if (GUILayout.Button("Bauen", UiKit.ActiveButton, GUILayout.Width(120f), GUILayout.Height(60f))) farm.BeginPlacing(type);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawWorldMap()
        {
            Rect box = UiKit.DrawOverlay(blockers, 720f, 560f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Weltkarte – Heimattal", UiKit.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.MapOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            mapScroll = GUILayout.BeginScrollView(mapScroll);
            for (int i = 0; i < Levels.All.Length; i++)
            {
                var def = Levels.All[i];
                bool unlocked = GameFlow.IsUnlocked(i);
                GUILayout.BeginHorizontal(UiKit.Panel, GUILayout.Height(96f));
                GUILayout.BeginVertical();
                GUILayout.Label(def.Name, UiKit.Label);
                GUILayout.Label(unlocked
                    ? $"Belohnung bei 3 Sternen: {def.RewardCorn} Mais, {def.RewardMilk} Milch"
                    : "Gesperrt – schaffe zuerst das vorherige Level.", UiKit.Small);
                GUILayout.EndVertical();
                Rect stars = GUILayoutUtility.GetRect(130f, 60f, GUILayout.Width(130f));
                UiKit.DrawStars(stars, State.BestStars(def.Id), 36f);
                GUI.enabled = unlocked;
                if (GUILayout.Button("Spielen", UiKit.BigButton, GUILayout.Width(140f), GUILayout.Height(60f)))
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
                    bool clickable = farm.Mode == FarmMode.Normal && !farm.ShopOpen && !farm.MapOpen;
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
