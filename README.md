# Muh Defense (Arbeitstitel)

Handyspiel, das Hof-Aufbau im Stil von Clash of Clans mit Tower Defense im Stil von Bloons TD6 verbindet: Baue deinen Bauernhof auf und verteidige deine Kühe gegen milchsüchtige Aliens.

- Engine: Unity 6
- Plattform: Android, Querformat

Das vollständige Spielkonzept steht in [docs/Spielkonzept.md](docs/Spielkonzept.md).

## Aktueller Stand: Hof + Tower Defense (Etappe 1–3)

Der komplette Spielkreislauf ist als Prototyp spielbar: **Level spielen → Hof ausbauen → stärkere Türme → nächstes Level.**

**Hof (Base, wie in Clash of Clans)**
- Gebäude: Bauernhaus, Maisfeld, Kuhstall, Maissilo, Milchtank, Knechthütte und 3 Werkstätten (Schmiede, Zimmerei, Tüftlerschuppen)
- Maisfelder und Kühe produzieren in Echtzeit Mais und Milch – auch während eines Levels oder wenn das Spiel geschlossen ist. Antippen sammelt ein.
- Bauen und Verbessern kostet Rohstoffe und Bauzeit. Jeder Knecht baut ein Gebäude gleichzeitig.
- Das Bauernhaus begrenzt Anzahl und Stufe der anderen Gebäude.
- Gebäude lassen sich verschieben. Der Hof wird automatisch gespeichert.

**Levels (Tower Defense, wie in Bloons TD6)**
- Weltkarte mit 3 Levels im Heimattal, jeweils 10 Wellen mit Boss
- 3 Türme: Mistgabel-Bauer, Heuballen-Katapult, Eismilch-Sprüher
- 4 Aliens: Grünling, Flitzer, Brocken (gepanzert), Oberbrocken Gorg (Boss)
- Aliens schnappen sich Kühe von der Weide. Sterne: 3 ohne Verluste, 2 bei höchstens 2 verlorenen Kühen, sonst 1.

**Verbindung**
- Levels bringen Mais, Milch und Sterne für den Hof (anteilig nach Sternen).
- Werkstätten schalten Türme frei: höchste Turm-Stufe im Level = Werkstatt-Stufe + 1.
- Ein Level wird freigeschaltet, wenn das vorherige geschafft ist.

Grafik bewusst nur aus Unity-Grundkörpern (Platzhalter), UI als einfache IMGUI-Oberfläche. Zeiten und Kosten sind für schnelles Testen kurz gehalten.

Noch **nicht** enthalten: Helden, weitere Türme, Diamanten und Shop, Talentbaum für Sterne, echte 3D-Modelle, Sound.

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
- „Bauen“ → Gebäude wählen → auf den Hof tippen (grün = frei) → „Bestätigen“
- „Zur Weltkarte“ → Level wählen

**Level**
- Turm in der Seitenleiste wählen → auf ein hell markiertes Feld tippen
- Auf einen Turm tippen → verbessern oder verkaufen
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
