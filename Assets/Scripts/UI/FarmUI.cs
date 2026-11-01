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
        Vector2 questScroll;
        bool questAchievements;

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
            EnsureStyles();
            // Vor dem Zeichnen: "blockers" enthält noch die Bedienelemente des letzten Durchlaufs
            HandleDrag(rawMouse);
            blockers.Clear();

            DrawWorldOverlays();
            DrawTopBar();
            DrawSideBar();
            DrawZoomButtons();
            DrawTutorial();
            if (farm.ShopOpen) DrawShop();
            if (farm.MapOpen) DrawWorldMap();
            if (farm.TalentsOpen) DrawTalents();
            if (farm.GemShopOpen) DrawGemShop();
            if (farm.HeroesOpen) DrawHeroes();
            if (farm.QuestsOpen) DrawQuests();

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
            // Bauernstufe mit Erfahrungsbalken
            var lvlRect = new Rect(968f, 10f, 190f, 36f);
            if (lvlRect.xMax < vw - 10f)
            {
                GUI.Box(lvlRect, GUIContent.none, UiKit.Pill);
                UiKit.OutlinedText(new Rect(lvlRect.x + 10f, lvlRect.y, 90f, 22f), $"Stufe {State.FarmLevel}", nameStyle, UiKit.Gold, 1.2f);
                UiKit.DrawBar(new Rect(lvlRect.x + 10f, lvlRect.y + 23f, lvlRect.width - 20f, 8f), State.FarmXpProgress, new Color(0.6f, 0.95f, 0.4f));
                string xp = State.FarmLevel >= FarmState.MaxFarmLevel ? "max" : $"{State.Data.FarmXp}/{FarmState.XpToNext(State.FarmLevel)} EP";
                GUI.Label(new Rect(lvlRect.x + 96f, lvlRect.y + 1f, 90f, 22f), xp, tinyStyle);
            }
        }

        /// <summary>Hof-Tutorial: Hinweis-Fenster unten links, bis alle Schritte erledigt sind.</summary>
        void DrawTutorial()
        {
            if (Event.current.type == EventType.Layout) FarmTutorial.Update(State);
            var step = FarmTutorial.Current(State);
            if (step == null || farm.AnyWindowOpen || farm.Mode != FarmMode.Normal) return;
            var r = new Rect(16f, UiKit.RefHeight - 80f - 190f, 440f, 180f);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Dialog);
            GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 10f, r.width - 32f, r.height - 20f));
            UiKit.Heading($"{step.Title}  ({State.Data.TutorialStep + 1}/{FarmTutorial.Steps.Length})", UiKit.Title, GUILayout.Height(30f));
            GUILayout.Label(step.Text, UiKit.Small);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (UiKit.Btn("Überspringen", UiKit.Button, GUILayout.Height(36f), GUILayout.Width(150f))) FarmTutorial.Skip(State);
            GUILayout.FlexibleSpace();
            if (step.Done == null && UiKit.Btn("Weiter", UiKit.YellowButton, GUILayout.Height(36f), GUILayout.Width(150f))) FarmTutorial.Next(State);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
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
            else if (farm.SelectedObstacle != null)
            {
                DrawObstaclePanel(farm.SelectedObstacle);
            }
            else
            {
                UiKit.Heading("Dein Hof", UiKit.Title);
                GUILayout.Label("Tippe auf Gebäude zum Verbessern, auf Steine und Büsche zum Wegräumen.", UiKit.Small);
                GUILayout.Space(6f);
                if (AnythingToCollect() && UiKit.IconBtn("Alles einsammeln", UiKit.Icon("Maiskolben"), UiKit.ActiveButton, GUILayout.Height(44f)))
                {
                    farm.CollectAll();
                }
                GUILayout.Space(4f);
                if (UiKit.Btn("Bauen", UiKit.YellowButton, GUILayout.Height(56f))) farm.ShopOpen = true;
                GUILayout.Space(4f);
                if (State.SchoolLevel > 0)
                {
                    if (UiKit.IconBtn($"Bauernwissen ({State.AvailableStars} frei)", UiKit.Icon("Bauernschule"), UiKit.BlueCard, GUILayout.Height(48f))) farm.TalentsOpen = true;
                }
                else
                {
                    GUILayout.Label("Bau eine Bauernschule, um Sterne gegen Bauernwissen zu tauschen.", UiKit.Small);
                }
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                if (UiKit.IconBtn("Shop", UiKit.Icon("Diamant"), UiKit.Button, GUILayout.Height(48f))) farm.GemShopOpen = true;
                int claimable = State.ClaimableCount;
                if (UiKit.Btn(claimable > 0 ? $"Aufgaben ({claimable}!)" : "Aufgaben", claimable > 0 ? UiKit.YellowButton : UiKit.Button, GUILayout.Height(48f)))
                {
                    farm.QuestsOpen = true;
                }
                GUILayout.EndHorizontal();
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

        bool AnythingToCollect()
        {
            int n = 0;
            foreach (var b in State.Data.Buildings)
            {
                if (b.TypeDef != null && b.TypeDef.IsProducer && b.Level > 0 && b.Stored >= 1) n++;
            }
            return n >= 2;
        }

        void DrawObstaclePanel(ObstacleData o)
        {
            var t = o.TypeDef;
            UiKit.Heading(t.Name, UiKit.Title);
            GUILayout.Label("Räum den Hof auf! Ein Knecht entfernt das Hindernis und findet dabei oft Diamanten. " +
                            "Mit der Zeit wächst neues Zeug nach.", UiKit.Small);
            GUILayout.Space(6f);
            GUILayout.BeginVertical(UiKit.BlueCard);
            GUILayout.Label("Belohnung:", headStyle);
            GUILayout.Label(t.MaxGems > 0 ? $"{t.MinGems}–{t.MaxGems} Diamanten und {t.Xp} Erfahrung" : $"{t.Xp} Erfahrung", smallStyle);
            GUILayout.EndVertical();
            GUILayout.Space(6f);
            if (o.IsClearing)
            {
                GUILayout.Label($"Wird weggeräumt: noch {UiKit.FormatTime(State.ClearRemaining(o))}", UiKit.Small);
                Rect bar = GUILayoutUtility.GetRect(10f, 14f);
                UiKit.DrawBar(bar, State.ClearProgress(o), new Color(0.4f, 0.8f, 0.3f));
            }
            else
            {
                string reason = State.WhyCannotClear(o);
                GUI.enabled = reason == null;
                string label = $"Wegräumen\n{t.Cost} {FarmBuildingType.ResourceName(t.CostResource)} · {UiKit.FormatTime(t.Seconds)}";
                if (UiKit.Btn(label, upgradeStyle, GUILayout.Height(60f))) farm.ClearSelectedObstacle();
                GUI.enabled = true;
                if (reason != null) GUILayout.Label(reason, UiKit.Small);
            }
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Height(36f))) farm.DeselectObstacle();
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
                string preview = NextLevelPreview(type, b.Level + 1);
                if (preview.Length > 0)
                {
                    GUILayout.BeginVertical(UiKit.BlueCard);
                    GUILayout.Label($"Stufe {b.Level + 1} bringt:", headStyle ?? UiKit.Small);
                    GUILayout.Label(preview, smallStyle ?? UiKit.Small);
                    GUILayout.EndVertical();
                }
                string reason = State.WhyCannotUpgrade(b);
                int cost = type.CostForLevel(b.Level + 1);
                GUI.enabled = reason == null;
                string label = $"Verbessern auf Stufe {b.Level + 1}\n{cost} {FarmBuildingType.ResourceName(type.CostResource)} · {UiKit.FormatTime(type.SecondsForLevel(b.Level + 1))}";
                if (UiKit.Btn(label, upgradeStyle, GUILayout.Height(60f))) farm.UpgradeSelected();
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
                if (State.IsBoosted(b))
                {
                    GUILayout.Label($"Ernte-Turbo aktiv: doppelte Produktion, noch {UiKit.FormatTime(b.BoostEnd - FarmState.Now)}", UiKit.Small);
                }
                else
                {
                    GUI.enabled = State.Gems >= FarmState.BoostGems;
                    if (UiKit.IconBtn($"Ernte-Turbo 1 h: {FarmState.BoostGems}", UiKit.Icon("Diamant"), UiKit.BlueCard, GUILayout.Height(40f)) && State.BoostProducer(b))
                    {
                        State.Save();
                        GameAudio.Play(SoundId.LevelUp);
                    }
                    GUI.enabled = true;
                }
            }

            if (type == FarmBuildingType.Bauernschule && b.Level > 0)
            {
                if (UiKit.IconBtn($"Bauernwissen ({State.AvailableStars} Sterne frei)", UiKit.Icon("Bauernschule"), UiKit.YellowButton, GUILayout.Height(52f))) farm.TalentsOpen = true;
            }
            if (type == FarmBuildingType.Heldenhuette && b.Level > 0)
            {
                if (UiKit.IconBtn("Helden und Training", UiKit.Icon(State.SelectedHero.ModelName), UiKit.YellowButton, GUILayout.Height(52f))) farm.HeroesOpen = true;
            }
            if (UiKit.Btn("Verschieben", UiKit.Button, GUILayout.Height(40f))) farm.BeginMoving();
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Height(36f))) farm.Select(null);
        }

        /// <summary>Was die nächste Stufe eines Gebäudes bringt (vor dem Verbessern sichtbar).</summary>
        string NextLevelPreview(FarmBuildingType type, int next)
        {
            int cur = next - 1;
            var lines = new List<string>();
            if (type == FarmBuildingType.Bauernhaus)
            {
                lines.Add($"Lager im Bauernhaus: {FarmState.FarmhouseCapacity(cur)} → {FarmState.FarmhouseCapacity(next)}");
                lines.Add("Mehr und höhere Gebäude erlaubt");
                foreach (var t in FarmBuildingType.All)
                {
                    if (t != FarmBuildingType.Bauernhaus && t.UnlockFarmhouseLevel == next) lines.Add($"Neu: {t.Name}");
                }
            }
            if (type.IsProducer)
            {
                string res = FarmBuildingType.ResourceName(type.ProducedResource);
                lines.Add($"Produktion: {type.RatePerMinute[cur - 1]:0} → {type.RatePerMinute[next - 1]:0} {res}/min");
                lines.Add($"Speicher: {type.ProducerCapacity[cur - 1]} → {type.ProducerCapacity[next - 1]}");
            }
            if (type.IsStorage)
            {
                lines.Add($"Lager: {type.StorageCapacity[cur - 1]} → {type.StorageCapacity[next - 1]} {FarmBuildingType.ResourceName(type.StoredResource)}");
            }
            foreach (var unlock in type.Unlocks)
            {
                int a = FarmState.TowerLevelForWorkshop(cur, unlock.FromLevel);
                int b2 = FarmState.TowerLevelForWorkshop(next, unlock.FromLevel);
                if (a == 0 && b2 > 0) lines.Add($"Neuer Turm: {unlock.Tower.Name} (bis Stufe {b2})");
                else if (b2 > a) lines.Add($"{unlock.Tower.ShortName}: Upgrades bis Stufe {a} → {b2}");
            }
            if (type == FarmBuildingType.Heldenhuette)
            {
                lines.Add($"Held startet im Level auf Stufe {cur} → {next}");
                lines.Add($"Helden-Training bis Stufe {next}");
            }
            if (type == FarmBuildingType.Bauernschule)
            {
                var names = new List<string>();
                for (int c = 0; c < Talents.ColumnNames.Length; c++)
                {
                    var t = Talents.At(c, next - 1);
                    if (t != null) names.Add(t.Name);
                }
                lines.Add($"Neue Talent-Reihe {next}: {string.Join(", ", names)}");
            }
            return string.Join("\n", lines);
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
                lines.Add($"Produktion: {State.ProductionPerMinute(b) * (State.IsBoosted(b) ? 2 : 1):0} {res} pro Minute" + (State.IsBoosted(b) ? " (Turbo)" : ""));
                lines.Add($"Gesammelt: {(int)b.Stored} / {(int)State.ProducerCapacity(b)}");
            }
            if (type == FarmBuildingType.Heldenhuette)
            {
                lines.Add($"Held startet im Level auf Stufe {b.Level}. Gewählt: {State.SelectedHero.Name}");
            }
            if (type == FarmBuildingType.Bauernschule)
            {
                lines.Add($"Talent-Reihen 1–{b.Level} freigeschaltet, {State.AvailableStars} Sterne frei");
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

        /// <summary>Heldenhütte: Helden wählen, freischalten, trainieren und einkleiden.</summary>
        void DrawHeroes()
        {
            EnsureStyles();
            Rect box = UiKit.DrawOverlay(blockers, 1010f, 680f);
            float x0 = box.x + 20f, y0 = box.y + 14f, w = box.width - 40f;
            UiKit.Title.alignment = TextAnchor.MiddleLeft;
            UiKit.OutlinedText(new Rect(x0, y0, 400f, 40f), "Heldenhütte", UiKit.Title, Color.white);
            var gem = UiKit.Icon("Diamant");
            var milk = UiKit.Icon("Milchflasche");
            var pill = new Rect(x0 + w - 470f, y0 + 2f, 150f, 36f);
            UiKit.PillLabel(pill, gem, State.Gems.ToString());
            UiKit.PillLabel(new Rect(pill.xMax + 10f, pill.y, 150f, 36f), milk, ((int)State.Amount(Resource.Milch)).ToString());
            if (UiKit.BtnRect(new Rect(x0 + w - 150f, y0, 150f, 40f), "Schließen", UiKit.Button, null)) farm.HeroesOpen = false;
            GUI.Label(new Rect(x0, y0 + 42f, w, 22f),
                $"Der gewählte Held kommt mit ins Level und startet dort auf Stufe {Mathf.Max(1, State.HeroHutLevel)}. Training gilt dauerhaft.", smallStyle);

            float gap = 14f;
            float cw = (w - gap * 2f) / 3f;
            float top = y0 + 70f;
            float ch = box.yMax - 16f - top;
            for (int i = 0; i < HeroType.All.Length; i++)
            {
                DrawHeroCard(HeroType.All[i], new Rect(x0 + i * (cw + gap), top, cw, ch), gem, milk);
            }
        }

        void DrawHeroCard(HeroType h, Rect r, Texture2D gem, Texture2D milk)
        {
            bool unlocked = State.IsHeroUnlocked(h);
            bool selected = State.SelectedHero == h;
            GUI.Box(r, GUIContent.none, selected ? UiKit.ActiveButton : UiKit.Card);
            float x = r.x + 10f, w = r.width - 20f, y = r.y + 6f;

            UiKit.OutlinedText(new Rect(x, y, w, 26f), h.Name, nameStyle, Color.white);
            GUI.Label(new Rect(x, y + 24f, w, 18f), h.Role, headStyle);
            y += 44f;

            // Bild (mit gewähltem Aussehen)
            var icon = UiKit.Icon(h.ModelName);
            if (icon != null)
            {
                var skin = State.SelectedHeroSkin(h);
                GUI.color = !unlocked ? new Color(0.3f, 0.3f, 0.35f) : skin != null ? skin.Tint : Color.white;
                GUI.DrawTexture(new Rect(x, y, w, 96f), icon, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
            }
            y += 98f;
            GUI.Label(new Rect(x, y, w, 46f), h.Description, tinyStyle);
            y += 46f;
            GUI.Label(new Rect(x, y, w, 30f), $"Ab Stufe {Hero.Ability1Level}: {h.Ability1Name}\nAb Stufe {Hero.Ability2Level}: {h.Ability2Name}", tinyStyle);
            y += 34f;

            // Training: 5 Kästchen, aktuelle Wirkung und was die nächste Stufe bringt
            int training = State.HeroTrainingLevel(h);
            UiKit.OutlinedText(new Rect(x, y, 90f, 20f), "Training", nameStyle, Color.white, 1.2f);
            for (int k = 0; k < HeroTraining.MaxLevel; k++)
            {
                var pip = new Rect(x + 92f + k * 22f, y + 3f, 18f, 14f);
                UiKit.DrawRect(pip, new Color(0.15f, 0.08f, 0.03f, 0.9f));
                if (k < training) UiKit.DrawRect(new Rect(pip.x + 2f, pip.y + 2f, pip.width - 4f, pip.height - 4f), UiKit.Gold);
            }
            y += 22f;
            string now = training > 0 ? "Jetzt: " + HeroTraining.Describe(training) : "Noch nicht trainiert.";
            string next = training < HeroTraining.MaxLevel ? "Nächste Stufe: " + HeroTraining.Describe(training + 1) : "Voll trainiert!";
            GUI.Label(new Rect(x, y, w, 30f), now, tinyStyle);
            y += 30f;
            GUI.Label(new Rect(x, y, w, 30f), next, tinyStyle);
            y += 32f;
            string why = State.WhyCannotTrain(h);
            GUI.enabled = why == null;
            string label = training >= HeroTraining.MaxLevel ? "Voll trainiert" : why ?? $"Trainieren: {HeroTraining.CostFor(training + 1)}";
            if (UiKit.BtnRect(new Rect(x, y, w, 34f), label, UiKit.YellowButton, why == null ? milk : null) && State.TrainHero(h))
            {
                State.Save();
                GameAudio.Play(SoundId.LevelUp);
            }
            GUI.enabled = true;
            y += 40f;

            // Aussehen: eine Zeile je Skin
            if (unlocked)
            {
                var current = State.SelectedHeroSkin(h);
                if (UiKit.BtnRect(new Rect(x, y, w, 26f), current == null ? "Normal (gewählt)" : "Normal", current == null ? UiKit.ActiveButton : UiKit.Button, null))
                {
                    State.SelectHeroSkin(h, null);
                    State.Save();
                }
                y += 29f;
                foreach (var s in HeroSkin.All)
                {
                    if (s.Hero != h.Kind) continue;
                    var row = new Rect(x, y, w, 26f);
                    if (State.OwnsHeroSkin(s))
                    {
                        if (UiKit.BtnRect(row, current == s ? $"{s.Name} (gewählt)" : s.Name, current == s ? UiKit.ActiveButton : UiKit.Button, null))
                        {
                            State.SelectHeroSkin(h, s);
                            State.Save();
                        }
                    }
                    else
                    {
                        GUI.enabled = State.Gems >= s.Gems;
                        if (UiKit.BtnRect(row, $"{s.Name}: {s.Gems}", UiKit.BlueCard, gem) && State.BuyHeroSkin(s))
                        {
                            State.SelectHeroSkin(h, s);
                            State.Save();
                            GameAudio.Play(SoundId.Coin);
                        }
                        GUI.enabled = true;
                    }
                    y += 29f;
                }
            }

            // Unten: wählen oder freischalten
            var bottom = new Rect(x, r.yMax - 48f, w, 40f);
            if (selected)
            {
                UiKit.OutlinedText(bottom, "Gewählt", UiKit.Title, new Color(1f, 1f, 0.7f));
            }
            else if (unlocked)
            {
                if (UiKit.BtnRect(bottom, "Wählen", UiKit.YellowButton, null))
                {
                    State.SelectHero(h);
                    State.Save();
                    GameAudio.Play(SoundId.Click);
                }
            }
            else
            {
                GUI.Label(new Rect(x, bottom.y - 34f, w, 32f), $"Frei ab {h.UnlockStars} Sternen (du hast {State.TotalStars}) – oder sofort:", tinyStyle);
                GUI.enabled = State.Gems >= h.UnlockGems;
                if (UiKit.BtnRect(bottom, h.UnlockGems.ToString(), UiKit.ActiveButton, gem) && State.UnlockHeroWithGems(h))
                {
                    State.SelectHero(h);
                    State.Save();
                    GameAudio.Play(SoundId.LevelUp);
                }
                GUI.enabled = true;
            }
        }

        /// <summary>Tägliche Aufgaben und Erfolge.</summary>
        void DrawQuests()
        {
            Rect box = UiKit.DrawOverlay(blockers, 820f, 600f);
            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 16f, box.width - 40f, box.height - 32f));
            GUILayout.BeginHorizontal();
            UiKit.Heading(questAchievements ? "Erfolge" : "Tägliche Aufgaben", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Täglich", questAchievements ? UiKit.Button : UiKit.ActiveButton, GUILayout.Width(120f), GUILayout.Height(40f)))
            {
                questAchievements = false;
                questScroll = Vector2.zero;
            }
            if (UiKit.Btn("Erfolge", questAchievements ? UiKit.ActiveButton : UiKit.Button, GUILayout.Width(120f), GUILayout.Height(40f)))
            {
                questAchievements = true;
                questScroll = Vector2.zero;
            }
            if (UiKit.Btn("Schließen", UiKit.Button, GUILayout.Width(140f), GUILayout.Height(40f))) farm.QuestsOpen = false;
            GUILayout.EndHorizontal();

            questScroll = GUILayout.BeginScrollView(questScroll);
            if (!questAchievements)
            {
                GUILayout.Label("Jeden Tag gibt es drei neue Aufgaben. Um Mitternacht kommen frische.", UiKit.Small);
                foreach (var q in State.DailyQuests)
                {
                    var def = Quests.GetDaily(q.Id);
                    if (def == null) continue;
                    QuestRow(def.Text, "Belohnung: " + Quests.RewardText(def.Reward, def.Amount), q.Progress, def.Target, q.Claimed,
                        def.Reward == RewardKind.Gems ? "Diamant" : def.Reward == RewardKind.Corn ? "Maiskolben" : "Milchflasche",
                        () => State.ClaimDaily(q));
                }
            }
            else
            {
                int done = 0;
                foreach (var a in Quests.Achievements)
                {
                    if (State.IsAchievementClaimed(a)) done++;
                }
                GUILayout.Label($"Abzeichen: {done} von {Quests.Achievements.Length}", UiKit.Small);
                foreach (var a in Quests.Achievements)
                {
                    QuestRow(a.Name, $"{a.Description}  •  {a.Gems} Diamanten", State.AchievementProgress(a), a.Target,
                        State.IsAchievementClaimed(a), a.Icon, () => State.ClaimAchievement(a));
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void QuestRow(string title, string detail, int progress, int target, bool claimed, string icon, System.Func<bool> claim)
        {
            GUILayout.BeginHorizontal(claimed ? UiKit.BlueCard : UiKit.Card, GUILayout.Height(78f));
            var tex = UiKit.Icon(icon);
            if (tex != null) GUILayout.Label(tex, GUILayout.Width(60f), GUILayout.Height(60f));
            GUILayout.BeginVertical();
            UiKit.Heading(title, UiKit.Title, GUILayout.Height(28f));
            GUILayout.Label(detail, UiKit.Small);
            Rect bar = GUILayoutUtility.GetRect(200f, 12f, GUILayout.Height(12f));
            UiKit.DrawBar(bar, target > 0 ? progress / (float)target : 1f, new Color(0.4f, 0.85f, 0.3f));
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(150f));
            GUILayout.Label($"{progress} / {target}", UiKit.Center);
            if (claimed)
            {
                GUILayout.Label("Abgeholt", UiKit.Center);
            }
            else
            {
                GUI.enabled = progress >= target;
                if (UiKit.Btn("Abholen", UiKit.YellowButton, GUILayout.Height(36f)) && claim())
                {
                    State.Save();
                    GameAudio.Play(SoundId.LevelUp);
                }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
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
        // Eigene Schriftstile für die schickeren Fenster (kleiner, damit alles reinpasst)
        GUIStyle headStyle, smallStyle, tinyStyle, nameStyle, upgradeStyle;

        void EnsureStyles()
        {
            if (nameStyle != null) return;
            nameStyle = new GUIStyle(UiKit.ButtonText) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
            headStyle = new GUIStyle(UiKit.Small) { fontSize = 14, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(UiKit.Small) { fontSize = 13 };
            tinyStyle = new GUIStyle(UiKit.Small) { fontSize = 12, alignment = TextAnchor.UpperLeft };
            upgradeStyle = new GUIStyle(UiKit.ActiveButton) { fontSize = 15 };
        }

        static readonly Color[] ColumnColors =
        {
            new Color(1f, 0.78f, 0.25f), new Color(0.95f, 0.45f, 0.3f), new Color(0.45f, 0.8f, 1f),
        };

        /// <summary>Talentbaum der Bauernschule: drei Spalten, fünf Reihen, Verbindungslinien wie Spezial-Upgrades.</summary>
        void DrawTalents()
        {
            EnsureStyles();
            Rect box = UiKit.DrawOverlay(blockers, 1010f, 670f);
            float x0 = box.x + 20f, y0 = box.y + 14f, w = box.width - 40f;

            // Kopfzeile
            UiKit.Title.alignment = TextAnchor.MiddleLeft;
            UiKit.OutlinedText(new Rect(x0, y0, 520f, 40f), "Bauernschule – Bauernwissen", UiKit.Title, Color.white);
            var starPill = new Rect(x0 + w - 330f, y0 + 2f, 170f, 36f);
            GUI.Box(starPill, GUIContent.none, UiKit.Pill);
            GUI.color = new Color(1f, 0.82f, 0.1f);
            GUI.DrawTexture(new Rect(starPill.x + 8f, starPill.y + 4f, 28f, 28f), UiKit.Star);
            GUI.color = Color.white;
            UiKit.OutlinedText(new Rect(starPill.x + 40f, starPill.y, 126f, 36f), $"{State.AvailableStars} frei", nameStyle, Color.white);
            if (UiKit.BtnRect(new Rect(x0 + w - 150f, y0, 150f, 40f), "Schließen", UiKit.Button, null)) farm.TalentsOpen = false;
            GUI.Label(new Rect(x0, y0 + 42f, w, 22f),
                $"Bauernschule Stufe {State.SchoolLevel}: Reihe 1–{Mathf.Max(0, State.SchoolLevel)} freigeschaltet. Verbessere die Schule für weitere Reihen. Jedes Talent gilt in allen Levels.",
                smallStyle);

            float colGap = 14f;
            float colW = (w - colGap * 2f) / 3f;
            float top = y0 + 70f;
            const float cardH = 90f, gap = 14f;
            for (int c = 0; c < Talents.ColumnNames.Length; c++)
            {
                float cx = x0 + c * (colW + colGap);
                // Spaltenkopf mit Farbbalken
                var head = new Rect(cx, top, colW, 34f);
                GUI.Box(head, GUIContent.none, UiKit.Card);
                UiKit.DrawRect(new Rect(head.x + 6f, head.yMax - 6f, head.width - 12f, 3f), ColumnColors[c]);
                UiKit.OutlinedText(new Rect(head.x + 10f, head.y, head.width - 20f, 30f), Talents.ColumnNames[c], nameStyle, ColumnColors[c]);

                for (int row = 0; row < Talents.Rows; row++)
                {
                    var t = Talents.At(c, row);
                    if (t == null) continue;
                    float cy = top + 42f + row * (cardH + gap);
                    var card = new Rect(cx, cy, colW, cardH);
                    if (row > 0)
                    {
                        // Verbindungslinie zum Talent darüber
                        bool prevOwned = State.HasTalent(Talents.At(c, row - 1).Id);
                        UiKit.DrawRect(new Rect(cx + 40f, cy - gap, 6f, gap), prevOwned ? ColumnColors[c] : new Color(0.2f, 0.12f, 0.06f, 0.8f));
                    }
                    DrawTalentNode(t, card, ColumnColors[c]);
                }
            }
        }

        void DrawTalentNode(Talent t, Rect r, Color accent)
        {
            bool owned = State.HasTalent(t.Id);
            string reason = State.WhyCannotBuyTalent(t);
            bool available = !owned && reason == null;
            bool locked = !owned && reason != null && reason != "Zu wenig Sterne";

            GUI.Box(r, GUIContent.none, owned ? UiKit.ActiveButton : available ? UiKit.BlueCard : UiKit.Card);
            if (locked) UiKit.DrawRect(r, new Color(0f, 0f, 0f, 0.28f));

            // Symbol im runden Rahmen
            var badge = new Rect(r.x + 8f, r.y + 8f, 66f, 66f);
            GUI.Box(badge, GUIContent.none, UiKit.Pill);
            var icon = UiKit.Icon(Talents.IconFor(t));
            if (icon != null)
            {
                if (locked) GUI.color = new Color(0.45f, 0.45f, 0.5f);
                GUI.DrawTexture(new Rect(badge.x + 6f, badge.y + 6f, 54f, 54f), icon, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
            }
            // Sterne-Kosten als kleines Abzeichen unten am Symbol
            var cost = new Rect(badge.x + 12f, badge.yMax - 12f, 42f, 20f);
            UiKit.DrawRect(cost, new Color(0.15f, 0.08f, 0.03f, 0.9f));
            GUI.color = new Color(1f, 0.82f, 0.1f);
            GUI.DrawTexture(new Rect(cost.x + 2f, cost.y + 2f, 16f, 16f), UiKit.Star);
            GUI.color = Color.white;
            GUI.Label(new Rect(cost.x + 19f, cost.y, 22f, 20f), t.Cost.ToString(), headStyle);

            float tx = r.x + 84f, tw = r.width - 92f;
            UiKit.OutlinedText(new Rect(tx, r.y + 4f, tw, 24f), t.Name, nameStyle, locked ? new Color(0.75f, 0.75f, 0.75f) : Color.white, 1.4f);
            GUI.Label(new Rect(tx, r.y + 27f, tw, 36f), t.Description, tinyStyle);

            var action = new Rect(tx, r.yMax - 26f, tw, 22f);
            if (owned)
            {
                UiKit.OutlinedText(action, "Gelernt", nameStyle, new Color(1f, 1f, 0.7f), 1.2f);
            }
            else if (available)
            {
                if (UiKit.BtnRect(new Rect(action.x, action.y - 2f, Mathf.Min(120f, tw), 26f), "Lernen", UiKit.YellowButton, null) && State.BuyTalent(t))
                {
                    State.Save();
                    GameAudio.Play(SoundId.LevelUp);
                    farm.AddFloatingText(new Vector3(FarmState.Width / 2f, 1f, FarmState.Height / 2f), $"{t.Name} gelernt!", UiKit.Gold);
                }
            }
            else
            {
                GUI.Label(action, reason, headStyle);
            }
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
                var diff = GameFlow.CurrentDifficulty;
                bool diffOpen = State.IsDifficultyUnlocked(def.Id, diff);
                float rf = DifficultyRules.RewardFactor(diff);
                string info;
                if (!unlocked) info = regionOpen ? "Gesperrt – schaffe zuerst das vorherige Level." : "Gesperrt – zu wenige Sterne.";
                else if (!diffOpen) info = diff == Difficulty.Mittel ? "„Mittel“ gesperrt – schaffe das Level zuerst auf „Leicht“." : "„Schwer“ gesperrt – schaffe das Level zuerst auf „Mittel“.";
                else info = $"Belohnung bei 3 Sternen: {Mathf.RoundToInt(def.RewardCorn * rf)} Mais, {Mathf.RoundToInt(def.RewardMilk * rf)} Milch";
                GUILayout.Label(info, UiKit.Small);
                if (unlocked)
                {
                    GUILayout.Label($"Leicht: {StarText(def.Id, Difficulty.Leicht)}   •   Mittel: {StarText(def.Id, Difficulty.Mittel)}   •   " +
                                    $"Schwer: {StarText(def.Id, Difficulty.Schwer)}", UiKit.Small);
                }
                GUILayout.EndVertical();
                // Sterne für den gewählten Schwierigkeitsgrad
                Rect stars = GUILayoutUtility.GetRect(130f, 60f, GUILayout.Width(130f));
                UiKit.DrawStars(stars, State.Stars(def.Id, diff), 36f);
                GUI.enabled = unlocked && diffOpen;
                if (UiKit.Btn(unlocked && !diffOpen ? "Gesperrt" : "Spielen", UiKit.BigButton, GUILayout.Width(140f), GUILayout.Height(60f)))
                {
                    farm.MapOpen = false;
                    farm.PlayLevel(def, diff);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Tipp: Werkstätten auf dem Hof schalten neue Türme frei und erlauben höhere Turm-Stufen im Level.", UiKit.Small);
            GUILayout.EndArea();
        }

        string StarText(string levelId, Difficulty d)
        {
            if (!State.IsDifficultyUnlocked(levelId, d)) return "gesperrt";
            int n = State.Stars(levelId, d);
            return n == 1 ? "1 Stern" : $"{n} Sterne";
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

            foreach (var o in State.Data.Obstacles)
            {
                if (!o.IsClearing || !UiKit.WorldToGui(cam, farm.ObstacleTop(o), out var op)) continue;
                UiKit.DrawBar(new Rect(op.x - 30f, op.y, 60f, 8f), State.ClearProgress(o), new Color(0.4f, 0.8f, 0.3f));
                UiKit.ShadowLabel(new Vector2(op.x, op.y - 11f), UiKit.FormatTime(State.ClearRemaining(o)), Color.white, UiKit.Small);
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
