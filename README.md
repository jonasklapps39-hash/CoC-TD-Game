# Muh Defense (Arbeitstitel)

Handyspiel, das Hof-Aufbau im Stil von Clash of Clans mit Tower Defense im Stil von Bloons TD6 verbindet: Baue deinen Bauernhof auf und verteidige deine Kühe gegen milchsüchtige Aliens.

- Engine: Unity 6
- Plattform: Android, Querformat

Das vollständige Spielkonzept steht in [docs/Spielkonzept.md](docs/Spielkonzept.md).

## Aktueller Stand

Der komplette Spielkreislauf ist spielbar: **Level spielen → Hof ausbauen → stärkere Türme → nächstes Level.**

**Hof (Base, wie in Clash of Clans)**
- Gebäude: Bauernhaus, Maisfeld, Kuhstall, Maissilo, Milchtank, Knechthütte und 4 Werkstätten (Schmiede, Zimmerei, Tüftlerschuppen, Hofladen-Kontor)
- Heldenhütte (Held wählen), Talentbaum „Bauernwissen“ (Sterne gegen dauerhafte Boni), Diamanten-Shop
- Mais und Milch werden in Echtzeit produziert, auch während eines Levels oder wenn das Spiel geschlossen ist
- Bauen und Verbessern mit Rohstoffen und Bauzeit, Knechte, Verschieben, automatisches Speichern

**Levels (Tower Defense, wie in Bloons TD6)**
- Weltkarte mit 5 Levels im Heimattal (Level 1 als Tutorial)
- 7 Türme: Mistgabel-Bauer, Heuballen-Katapult, Eismilch-Sprüher, Bienenstock, Windmühle, Wetterhahn, Hofladen
- Upgrade-System wie in Bloons TD6: je 3 Pfade mit 5 Stufen (105 Upgrades), Fähigkeiten auf Stufe 5, Zielmodus
- 3 Helden (Stufe 1–10, je 2 Fähigkeiten): **Bauer Heinz** (Schrotflinte), **Oma Gertrud** (Kekse, macht Türme schneller), **Berta** (Laseraugen, schützt Kühe)
- 7 Aliens: Grünling, Flitzer, Brocken (gepanzert), Schleicher (getarnt), Kuhfänger-Ufo (fliegt), Sanitäter (heilt), Boss Oberbrocken Gorg
- Tempo 1x/2x, Pause-Menü, Sterne-Wertung nach geretteten Kühen

**Verbindung:** Levels bringen Mais, Milch und Sterne. Werkstätten schalten Türme frei und bestimmen die höchste Upgrade-Stufe im Level.

**Grafik und Sound (Stil 7, Bloons-TD6-Look)**
- Eigener Shader `MuhDefense/Toon` mit Cel-Shading und Umrisslinien
- Alle Figuren, Türme und Hof-Gebäude als Blender-Modelle (`Assets/Resources/Models`), erzeugt per Skript:
  `blender -b -P tools/blender/build_models.py -- Assets/Resources/Models vorschau.png`
- Symbole aus den Modellen: `blender -b -P tools/blender/render_icons.py -- Assets/Resources/Models Assets/Resources/Icons`
- Bedienoberfläche mit Comic-Schrift „Luckiest Guy“ (Apache-2.0-Lizenz, siehe `Assets/Resources/Fonts`)
- Partikel-Effekte, per Code erzeugte Geräusche und Musik

Zeiten und Kosten sind für schnelles Testen kurz gehalten.

Noch **nicht** enthalten: weitere Türme aus dem Konzept, echte Käufe von Diamanten (Diamanten gibt es nur im Spiel).

## Starten

1. **Unity Hub** öffnen → **Add** → **Add project from disk** → diesen Ordner auswählen.
2. Mit einer **Unity-6-Version** öffnen (falls Unity Hub nach einer anderen Version fragt, einfach die installierte Unity-6-Version wählen).
3. Beliebige Szene öffnen (z. B. die leere Standardszene) und auf **Play** drücken.

Das Level baut sich komplett per Code auf (`Assets/Scripts/Core/GameBootstrap.cs`), es sind keine Szenen oder Prefabs nötig.
Tipp: Im Game-Fenster ein Querformat wie *1920×1080* einstellen.

**Alternative:** Ein neues Unity-6-Projekt (z. B. Vorlage *Universal 3D*) anlegen und den Ordner `Assets/Scripts` hineinkopieren – das funktioniert auch mit URP.

## Auf dem Android-Handy testen

1. In Unity Hub unter **Installs** bei deiner Unity-Version das Modul **Android Build Support** (mit *OpenJDK* und *Android SDK & NDK Tools*) installieren.
2. Im Unity-Editor oben im Menü **Muh Defense → Android einrichten** klicken. Das stellt App-Name, Paketname, Querformat und die Play-Store-Einstellungen ein, legt die Startszene an und wechselt auf Android.
3. Auf dem Handy die **Entwickleroptionen** aktivieren (Einstellungen → Über das Telefon → 7× auf *Build-Nummer* tippen) und dort **USB-Debugging** einschalten.
4. Handy per USB anschließen und die Nachfrage auf dem Handy erlauben.
5. In Unity **File → Build Profiles** → **Android** → **Build And Run**. Die App wird gebaut und startet auf dem Handy.

## Steuerung

**Hof**
- Gebäude antippen → Infos, verbessern, verschieben, einsammeln
- Gebäude **festhalten und ziehen** → verschieben (loslassen auf freiem Platz)
- „Bauen“ → Gebäude wählen → an den Platz ziehen oder tippen (grün = frei) → „Bestätigen“
- „Zur Weltkarte“ → Level wählen

**Level**
- Turm-Karte **auf die Karte ziehen** und loslassen (Vorschau mit Reichweite, grün = frei) – oder Karte antippen und dann aufs Feld tippen
- Auf einen Turm tippen → Upgrade-Fenster unten (3 Pfade), Zielmodus, verkaufen
- Fähigkeiten der Stufe-5-Türme erscheinen als Knöpfe in der Seitenleiste
- „Welle starten“, wenn du bereit bist

**Zum Testen:** Menü **Muh Defense → Spielstand löschen** setzt den Hof zurück.

## Code-Übersicht

| Ordner | Inhalt |
|---|---|
| `Assets/Scripts/Core` | Start des Spiels, Wechsel zwischen Hof und Level, Kamera, Platzhalter-Grafik |
| `Assets/Scripts/Farm` | Hof: Gebäudetypen, Spiellogik und Spielstand (`FarmState`), Darstellung |
| `Assets/Scripts/Level` | Level-Daten und Wellen, Spiellogik (`LevelManager`), Spielfeld, Kühe |
| `Assets/Scripts/Enemies` | Alien-Arten und ihr Verhalten |
| `Assets/Scripts/Towers` | Turm-Arten, Türme, Geschosse |
| `Assets/Scripts/UI` | Bedienoberflächen von Hof und Level |

Werte zum Ausbalancieren stehen in `FarmBuildingType.cs` (Hof), `TowerType.cs`, `EnemyType.cs` und `LevelDefinition.cs` (Levels).
