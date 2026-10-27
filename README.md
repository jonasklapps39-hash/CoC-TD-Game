# Muh Defense (Arbeitstitel)

Handyspiel, das Hof-Aufbau im Stil von Clash of Clans mit Tower Defense im Stil von Bloons TD6 verbindet: Baue deinen Bauernhof auf und verteidige deine Kühe gegen milchsüchtige Aliens.

- Engine: Unity 6
- Plattform: iOS und Android, Querformat

Das vollständige Spielkonzept steht in [docs/Spielkonzept.md](docs/Spielkonzept.md).

## Aktueller Stand: Prototyp Tower Defense (Etappe 1)

Ein spielbares Level, mit dem wir prüfen, ob das Tower-Defense-Gameplay Spaß macht:

- 1 Level („Heimattal 1 – Heinz' Weide“) mit 10 Wellen und einem Boss in der letzten Welle
- 3 Türme: Mistgabel-Bauer, Heuballen-Katapult, Eismilch-Sprüher – jeweils mit 3 Stufen, verkaufbar
- 4 Aliens: Grünling, Flitzer, Brocken (gepanzert), Oberbrocken Gorg (Boss)
- 10 Kühe auf der Weide: Aliens schnappen sich eine Kuh und tragen sie zurück zum Ufo. Wird das Alien vorher besiegt, läuft die Kuh zurück.
- Sterne-Wertung: 3 Sterne ohne Verluste, 2 Sterne bei höchstens 2 verlorenen Kühen, 1 Stern sonst
- Grafik bewusst nur aus Unity-Grundkörpern (Platzhalter), UI als einfache IMGUI-Oberfläche

Noch **nicht** enthalten: Hof-Aufbau, Helden, weitere Türme, echte 3D-Modelle, Sound.

## Starten

1. **Unity Hub** öffnen → **Add** → **Add project from disk** → diesen Ordner auswählen.
2. Mit einer **Unity-6-Version** öffnen (falls Unity Hub nach einer anderen Version fragt, einfach die installierte Unity-6-Version wählen).
3. Beliebige Szene öffnen (z. B. die leere Standardszene) und auf **Play** drücken.

Das Level baut sich komplett per Code auf (`Assets/Scripts/Core/GameBootstrap.cs`), es sind keine Szenen oder Prefabs nötig.
Tipp: Im Game-Fenster ein Querformat wie *1920×1080* einstellen.

**Alternative:** Ein neues Unity-6-Projekt (z. B. Vorlage *Universal 3D*) anlegen und den Ordner `Assets/Scripts` hineinkopieren – das funktioniert auch mit URP.

## Steuerung

- Turm in der Seitenleiste wählen → auf ein hell markiertes Feld tippen
- Auf einen Turm tippen → verbessern oder verkaufen
- „Welle starten“, wenn du bereit bist

## Code-Übersicht

| Ordner | Inhalt |
|---|---|
| `Assets/Scripts/Core` | Start des Spiels, Kamera, Hilfen für Platzhalter-Grafik |
| `Assets/Scripts/Level` | Level-Daten und Wellen, Spiellogik (`LevelManager`), Spielfeld, Kühe |
| `Assets/Scripts/Enemies` | Alien-Arten und ihr Verhalten |
| `Assets/Scripts/Towers` | Turm-Arten, Türme, Geschosse |
| `Assets/Scripts/UI` | Bedienoberfläche |

Werte zum Ausbalancieren (Kosten, Schaden, Leben, Wellen) stehen in `TowerType.cs`, `EnemyType.cs` und `LevelDefinition.cs`.
