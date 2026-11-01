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
        int mapRegion;
        Vector2 mapScroll;
        Vector2 gemScroll;

        FarmState State => farm.Farm;

        // Drag & Drop: Gebäude festhalten und ziehen
        const float DragThreshold = 12f;
        BuildingData pressedBuilding;
        Vector2 pressRaw;
        bool draggingGhost;
        bool moveByDrag;

        // Kamera: zoomen (Finger spreizen, Mausrad, +/−) und verschieben (freie Wiese ziehen)
        const float CameraAngle = 52f;
        Vector3 camCenter = new Vector3(FarmState.Width / 2f, 0f, FarmState.Height / 2f);
        float camSize = -1f;
        bool panning;
        bool pressOnGround;
        Vector3 panGrab;
        bool pinching;
        float pinchStartDistance;
        float pinchStartSize;

        float MaxCamSize => UiKit.FitSize(cam, FarmState.Width, FarmState.Height, CameraAngle);
        float MinCamSize => 3.5f;

        void ApplyCamera()
        {
            if (camSize < 0f) camSize = MaxCamSize * 0.62f;
            camSize = Mathf.Clamp(camSize, MinCamSize, MaxCamSize);
            camCenter.x = Mathf.Clamp(camCenter.x, 0f, FarmState.Width);
            camCenter.z = Mathf.Clamp(camCenter.z, -1f, FarmState.Height);
            UiKit.SetCameraView(cam, camCenter, camSize, CameraAngle);
        }

        void Zoom(float factor)
        {
            camSize *= factor;
            ApplyCamera();
        }

        /// <summary>Zwei Finger: zoomen (nur mit dem alten Input-System verfügbar, sonst Mausrad und +/− Knöpfe).</summary>
        void UpdatePinch()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount >= 2)
            {
                float d = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                if (!pinching)
                {
                    pinching = true;
                    panning = false;
                    pinchStartDistance = Mathf.Max(1f, d);
                    pinchStartSize = camSize;
                }
                else
                {
                    camSize = pinchStartSize * pinchStartDistance / Mathf.Max(1f, d);
                    ApplyCamera();
                }
                return;
            }
#endif
            pinching = false;
        }

        /// <summary>
        /// Gebäude festhalten und ziehen verschiebt es (Loslassen auf freiem Platz bestätigt, sonst springt es zurück).
        /// Beim Bauen und Verschieben folgt die Vorschau dem Finger.
        /// </summary>
        void HandleDrag(Vector2 rawMouse)
        {
            var e = Event.current;
            bool blocked = farm.AnyWindowOpen || UiKit.IsOverUi(rawMouse, blockers);

            if (e.type == EventType.ScrollWheel && !blocked)
            {
                Zoom(e.delta.y > 0f ? 1.1f : 1f / 1.1f);
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !blocked && UiKit.ScreenToGround(rawMouse, cam, out var down))
            {
                pressRaw = rawMouse;
                if (farm.Mode != FarmMode.Normal)
                {
                    draggingGhost = true;
                }
                else
                {
                    pressedBuilding = State.BuildingAt(new Vector2Int(Mathf.FloorToInt(down.x), Mathf.FloorToInt(down.z)));
                    pressOnGround = pressedBuilding == null;
                    panGrab = down;
                }
            }
            else if (e.type == EventType.MouseDrag && pinching)
            {
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && pressOnGround && farm.Mode == FarmMode.Normal)
            {
                // Freie Wiese ziehen: Hof verschieben (der Punkt unter dem Finger bleibt unter dem Finger)
                if (!panning && (rawMouse - pressRaw).magnitude > DragThreshold * UiKit.Scale) panning = true;
                if (panning && UiKit.ScreenToGround(rawMouse, cam, out var now))
                {
                    camCenter += panGrab - now;
                    ApplyCamera();
                    e.Use();
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
                pressOnGround = false;
                panning = false;
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
            if (cam == null) return;
            UpdatePinch();
            ApplyCamera();
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
            DrawZoomButtons();
            if (farm.ShopOpen) DrawShop();
            if (farm.MapOpen) DrawWorldMap();
            if (farm.TalentsOpen) DrawTalents();
            if (farm.GemShopOpen) DrawGemShop();
            if (farm.HeroesOpen) DrawHeroes();

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

        /// <summary>Knöpfe zum Zoomen unten links (funktionieren überall, auch ohne zwei Finger).</summary>
        void DrawZoomButtons()
        {
            if (farm.AnyWindowOpen) return;
            float y = UiKit.RefHeight - 70f;
            var plus = new Rect(16f, y, 56f, 56f);
            var minus = new Rect(80f, y, 56f, 56f);
            blockers.Add(plus);
            blockers.Add(minus);
            if (UiKit.BtnRect(plus, "+", UiKit.Button, null)) Zoom(1f / 1.25f);
            if (UiKit.BtnRect(minus, "-", UiKit.Button, null)) Zoom(1.25f);
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
                if (UiKit.IconBtn("Weltkarte", UiKit.Icon("Ufo"), UiKit.BigButton, GUILayout.Height(66f)))
                {
                    farm.MapOpen = true;
                    mapRegion = 0;
                    for (int reg = 0; reg < Levels.Regions.Length; reg++)
                    {
                        if (GameFlow.IsRegionUnlocked(reg)) mapRegion = reg;
                    }
                }
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

            if (type == FarmBuildingType.Heldenhuette && b.Level > 0)
            {
                if (UiKit.IconBtn("Helden wählen", UiKit.Icon(State.SelectedHero.ModelName), UiKit.YellowButton, GUILayout.Height(52f))) farm.HeroesOpen = true;
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
            if (type == FarmBuildingType.Heldenhuette)
            {
                lines.Add($"Held startet im Level auf Stufe {b.Level}. Gewählt: {State.SelectedHero.Name}");
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

        /// <summary>Heldenhütte: Helden wählen und freischalten (mit Sternen oder Diamanten).</summary>
        void DrawHeroes()
        {
            Rect box = UiKit.DrawOverlay(blockers, 900f, 560f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Helden", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            var gem = UiKit.Icon("Diamant");
            if (gem != null) GUILayout.Label(gem, GUILayout.Width(40f), GUILayout.Height(40f));
            UiKit.Heading(State.Gems.ToString(), UiKit.Title, GUILayout.Width(90f), GUILayout.Height(40f));
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.HeroesOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Der gewählte Held kommt mit ins Level. Er startet dort auf Stufe {Mathf.Max(1, State.HeroHutLevel)} (Stufe der Heldenhütte).", UiKit.Small);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            foreach (var h in HeroType.All)
            {
                bool unlocked = State.IsHeroUnlocked(h);
                bool selected = State.SelectedHero == h;
                GUILayout.BeginVertical(selected ? UiKit.ActiveButton : UiKit.Card, GUILayout.Width((box.width - 64f) / 3f));
                UiKit.Heading(h.Name, UiKit.Title, GUILayout.Height(30f));
                GUILayout.Label(h.Role, UiKit.Small);
                var icon = UiKit.Icon(h.ModelName);
                if (icon != null)
                {
                    Rect ir = GUILayoutUtility.GetRect(10f, 150f, GUILayout.Height(150f));
                    if (!unlocked) GUI.color = new Color(0.35f, 0.35f, 0.4f);
                    GUI.DrawTexture(ir, icon, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }
                GUILayout.Label(h.Description, UiKit.Small, GUILayout.Height(54f));
                GUILayout.Label($"Fähigkeiten: {h.Ability1Name} (ab Stufe {Hero.Ability1Level}), {h.Ability2Name} (ab Stufe {Hero.Ability2Level})", UiKit.Small, GUILayout.Height(40f));
                GUILayout.FlexibleSpace();
                if (selected)
                {
                    UiKit.Heading("Gewählt", UiKit.Title, GUILayout.Height(40f));
                }
                else if (unlocked)
                {
                    if (UiKit.Btn("Wählen", UiKit.YellowButton, GUILayout.Height(44f)))
                    {
                        State.SelectHero(h);
                        State.Save();
                        GameAudio.Play(SoundId.Click);
                    }
                }
                else
                {
                    GUILayout.Label($"Frei ab {h.UnlockStars} Sternen (du hast {State.TotalStars}) – oder sofort:", UiKit.Small);
                    GUI.enabled = State.Gems >= h.UnlockGems;
                    if (UiKit.IconBtn(h.UnlockGems.ToString(), gem, UiKit.ActiveButton, GUILayout.Height(44f)) && State.UnlockHeroWithGems(h))
                    {
                        State.SelectHero(h);
                        State.Save();
                        GameAudio.Play(SoundId.LevelUp);
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
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
            Rect box = UiKit.DrawOverlay(blockers, 760f, 580f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Weltkarte – " + Levels.Regions[mapRegion].Name, UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.MapOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            // Reiter für die Gebiete
            GUILayout.BeginHorizontal();
            for (int r = 0; r < Levels.Regions.Length; r++)
            {
                var region = Levels.Regions[r];
                bool open = GameFlow.IsRegionUnlocked(r);
                string label = open ? region.Name : $"{region.Name} ({region.UnlockStars} Sterne)";
                if (UiKit.IconBtn(label, UiKit.Icon(region.Icon), r == mapRegion ? UiKit.ActiveButton : UiKit.Button, GUILayout.Height(48f)))
                {
                    mapRegion = r;
                    mapScroll = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            // Schwierigkeitsgrad
            GUILayout.BeginHorizontal();
            foreach (Difficulty d in System.Enum.GetValues(typeof(Difficulty)))
            {
                var style = d == GameFlow.CurrentDifficulty ? (d == Difficulty.Schwer ? UiKit.RedButton : UiKit.ActiveButton) : UiKit.Button;
                if (UiKit.Btn(DifficultyRules.Name(d), style, GUILayout.Height(38f), GUILayout.Width(130f))) GameFlow.CurrentDifficulty = d;
            }
            GUILayout.Label(DifficultyRules.Description(GameFlow.CurrentDifficulty), UiKit.Small);
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);

            bool regionOpen = GameFlow.IsRegionUnlocked(mapRegion);
            if (!regionOpen)
            {
                GUILayout.Label($"Gesperrt – sammle insgesamt {Levels.Regions[mapRegion].UnlockStars} Sterne, um dieses Gebiet zu betreten " +
                                $"(du hast {State.TotalStars}).", UiKit.Label);
            }

            mapScroll = GUILayout.BeginScrollView(mapScroll);
            for (int i = 0; i < Levels.All.Length; i++)
            {
                var def = Levels.All[i];
                if (def.Region != mapRegion) continue;
                var region = Levels.Regions[def.Region];
                bool unlocked = GameFlow.IsUnlocked(i);
                GUILayout.BeginHorizontal(UiKit.Card, GUILayout.Height(96f));
                var levelIcon = UiKit.Icon(unlocked ? (def.BossLevel ? region.BossIcon : region.Icon) : "Schleicher");
                if (levelIcon != null) GUILayout.Label(levelIcon, GUILayout.Width(72f), GUILayout.Height(72f));
                GUILayout.BeginVertical();
                UiKit.Heading(def.Name, UiKit.Title, GUILayout.Height(30f));
                float rf = DifficultyRules.RewardFactor(GameFlow.CurrentDifficulty);
                GUILayout.Label(unlocked
                    ? $"Belohnung bei 3 Sternen: {Mathf.RoundToInt(def.RewardCorn * rf)} Mais, {Mathf.RoundToInt(def.RewardMilk * rf)} Milch" +
                      (State.HardCleared(def.Id) ? "   •   Krone (Schwer geschafft)" : "")
                    : regionOpen ? "Gesperrt – schaffe zuerst das vorherige Level." : "Gesperrt – zu wenige Sterne.", UiKit.Small);
                GUILayout.EndVertical();
                Rect stars = GUILayoutUtility.GetRect(130f, 60f, GUILayout.Width(130f));
                UiKit.DrawStars(stars, State.BestStars(def.Id), 36f);
                GUI.enabled = unlocked;
                if (UiKit.Btn("Spielen", UiKit.BigButton, GUILayout.Width(140f), GUILayout.Height(60f)))
                {
                    farm.MapOpen = false;
                    farm.PlayLevel(def, GameFlow.CurrentDifficulty);
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
