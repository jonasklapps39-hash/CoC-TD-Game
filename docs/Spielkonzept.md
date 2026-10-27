# Muh Defense – Spielkonzept

> Arbeitstitel (finaler Name wird später festgelegt). Stand: Entwurf v0.2 – wird gemeinsam weiterentwickelt.

## 1. Kurzbeschreibung

**Muh Defense** ist ein Handyspiel, das **Hof-Aufbau wie in Clash of Clans** mit **klassischem Tower Defense wie in Bloons TD6** verbindet.
Der Spieler baut seinen eigenen Bauernhof auf und verteidigt in Levels die Kühe gegen Aliens, die süchtig nach Milch sind.

- **Plattform:** iOS und Android
- **Engine:** Unity 6
- **Stil:** Low-Poly-3D im Comic-Look, mit leicht schräger Draufsicht (wie Clash of Clans)
- **Ton:** lustig, schräg, familienfreundlich
- **Modus:** Einzelspieler, kein Multiplayer
- **Ausrichtung:** Querformat
- **Keine Energie und keine Leben:** Man kann so viele Levels spielen, wie man will.

---

## 2. Story

Ein kleines Alien-Aufklärungsschiff vom Planeten **Zorb** macht eine Bruchlandung auf der Weide von **Bauer Heinz**.
Das Erste, was die Aliens auf der Erde sehen, ist **Berta, die Kuh**.
Aus Neugier probiert ein Alien ihre Milch, und das ist das Beste, was es je getrunken hat. Auf Zorb schmeckt alles nach grauem Brei.

Das Alien funkt nach Hause: *„Hier gibt es MILCH!“*
Kurz darauf schickt der milchsüchtige Herrscher **Imperator Schlürf** seine ganze Flotte, um alle Kühe der Erde zu entführen.

Bauer Heinz, Oma Gertrud und Berta selbst verteidigen sich: zuerst den eigenen Hof, dann die Nachbarhöfe und am Ende das Mutterschiff.

---

## 3. Der Spielkreislauf

Das Spiel hat **zwei Ebenen**, die sich gegenseitig brauchen:

```
   ┌──────────────────────────┐          ┌─────────────────────────┐
   │        DEIN HOF          │          │        LEVELS           │
   │  (Base, wie Clash of     │          │ (Tower Defense, wie     │
   │   Clans)                 │          │  Bloons TD6)            │
   │                          │  stärkere │                         │
   │ • Gebäude bauen/upgraden │  Türme &  │ • Türme platzieren      │
   │ • Rohstoffe produzieren  │ ───────▶ │ • Kühe beschützen       │
   │ • Türme & Helden         │  Helden   │ • 1–3 Sterne verdienen  │
   │   freischalten           │           │                         │
   │                          │ ◀─────── │                         │
   │                          │ Mais,     │                         │
   │                          │ Milch,    │                         │
   │                          │ Sterne    │                         │
   └──────────────────────────┘          └─────────────────────────┘
```

1. Levels spielen → **Rohstoffe und Sterne** bekommen
2. Auf dem Hof **Gebäude bauen und upgraden**
3. Dadurch **neue Türme, höhere Ausbaustufen und Helden** freischalten
4. Schwierigere Levels schaffen → zurück zu Schritt 1

### Was der Hof bestimmt

- Welche **Türme** im Level verfügbar sind (Freischaltung über Werkstätten)
- Bis zu welcher **Ausbaustufe** ein Turm im Level verbessert werden darf
- **Dauerhafte Verbesserungen** aus der Forschungsscheune (z. B. +5 % Schaden)
- Welche **Helden** verfügbar sind und wie stark sie sind

### Was im Level passiert

- Jedes Level startet fair mit **Level-Geld** (Münzen), das nur im Level gilt.
- Mit dem Level-Geld baut und verbessert man Türme. Besiegte Aliens bringen mehr Geld.
- Die Aliens laufen oder fliegen über feste Wege zur Weide und versuchen, Kühe zu entführen.
- Wird ein Alien mit einer Kuh besiegt, läuft die Kuh zurück zur Weide.

---

## 4. Währungen

| Währung | Woher? | Wofür? |
|---|---|---|
| 🌽 **Mais** | Maisfelder auf dem Hof, Level-Belohnungen | Gebäude bauen und upgraden |
| 🥛 **Milch** | Kühe im Kuhstall (mehr Kühe = mehr Milch), Level-Belohnungen | Türme erforschen, Helden trainieren |
| ⭐ **Sterne** | **Nur** durch Levels, nicht kaufbar | Neue Gebiete freischalten, Talentbaum „Bauernwissen“ |
| 💎 **Diamanten** | Echtgeld; kleine Mengen über Erfolge und Ereignisse | Bauzeit verkürzen, zusätzliche Knechte, Helden, Skins, Rohstoffpakete |
| 🪙 *Level-Münzen* | Nur innerhalb eines Levels | Türme im Level bauen und verbessern (werden nach dem Level verworfen) |

**Grundsatz:** Mit Diamanten kommt man schneller voran, aber **Sterne kann man nicht kaufen**. Die Levels muss man selbst schaffen.

### Kühe als roter Faden

- Auf dem Hof sind Kühe die **Milchquelle**. Mehr Kühe bedeuten mehr Milch.
- In den Levels sind Kühe das, was man **beschützt**.
- In besonderen „Befreiungs-Levels“ kann man **entführte Kühe retten** und auf den eigenen Hof mitnehmen, darunter seltene Kühe (z. B. „Schoko-Kuh“ mit mehr Milchproduktion).

---

## 5. Sterne-System

| Ergebnis | Sterne |
|---|---|
| Keine Kuh entführt | ⭐⭐⭐ |
| Höchstens 2 Kühe entführt | ⭐⭐ |
| Mindestens eine Kuh übrig | ⭐ |
| Alle Kühe entführt | Niederlage (Level wiederholen, keine Strafe) |

**Sterne ausgeben für:**
- **Gebiete freischalten:** Jedes neue Gebiet auf der Weltkarte braucht eine Mindestzahl an Sternen.
- **Talentbaum „Bauernwissen“:** dauerhafte Boni, z. B. +10 % Reichweite für Mistgabel-Bauern, +50 Start-Münzen, Helden-Fähigkeit lädt schneller.

Später möglich: Herausforderungs-Modi pro Level (z. B. „Nur Support- und Spezial-Türme“) für Bonus-Sterne.

---

## 6. Weltkarte

| # | Gebiet | Besonderheit | Levels |
|---|---|---|---|
| 1 | **Heimattal** | Tutorial, Bauer Heinz' Hof | 10 |
| 2 | **Maisfeld-Labyrinth** | Verwinkelte Wege, getarnte Aliens tauchen auf | 10 |
| 3 | **Almwiesen** | Mehrere Wege gleichzeitig, viele Ufos | 10 |
| 4 | **Moor & Sumpf** | Nebel verringert Reichweite, Schild-Aliens | 10 |
| 5 | **Wintertal** | Eis lässt Aliens schneller rutschen | 10 |
| 6 | **Das Mutterschiff** | Finale gegen Imperator Schlürf | 10 |

Jedes Gebiet endet mit einem **Boss-Level**.

---

## 7. Der Hof (Base)

| Gebäude | Funktion | Vorbild in Clash of Clans |
|---|---|---|
| 🏠 **Bauernhaus** | Hauptgebäude; seine Stufe begrenzt alle anderen Gebäude | Rathaus |
| 🌽 **Maisfeld** | Produziert Mais | Goldmine |
| 🐄 **Kuhstall** | Hält die Kühe und produziert Milch | Elixiersammler |
| 🏚️ **Maissilo** | Erhöht das Mais-Lager | Goldlager |
| 🛢️ **Milchtank** | Erhöht das Milch-Lager | Elixierlager |
| 🛖 **Knechthütte** | Jeder Knecht kann ein Gebäude gleichzeitig bauen | Bauarbeiterhütte |
| 🔬 **Forschungsscheune** | Erforscht dauerhafte Turm-Verbesserungen (kostet Milch und Zeit) | Labor |
| ⚒️ **5 Werkstätten** | Je eine pro Turm-Kategorie; schalten Türme und Ausbaustufen frei | Kaserne |
| 🦸 **Heldenhütte** | Helden trainieren, Fähigkeiten verbessern | Heldenaltar |
| 🌻 **Deko** | Zäune, Blumen, Statuen; teilweise mit Diamanten oder als Belohnung | Dekorationen |

### Werkstätten und Turm-Freischaltung

| Werkstatt | Kategorie | Türme |
|---|---|---|
| Schmiede | Basis | Mistgabel-Bauer, Eier-Huhn, Bienenstock |
| Zimmerei | Explosiv | Heuballen-Katapult, Popcorn-Kanone, Kürbis-Mörser |
| Tüftlerschuppen | Technik | Traktor-Tesla, Eismilch-Sprüher, Windmühle |
| Maschinenhalle | Spezial | Güllefass, Mähdrescher, Hofhund-Hütte |
| Hofladen-Kontor | Support | Vogelscheuche, Wetterhahn, Hofladen |

Werkstatt Stufe 1 schaltet den ersten Turm frei, Stufe 3 den zweiten, Stufe 5 den dritten. Höhere Werkstatt-Stufen erlauben höhere Ausbaustufen der Türme im Level.

---

## 8. Türme (15)

Jeder Turm hat im Level **zwei Ausbaupfade mit je 4 Stufen**. Man kann einen Pfad komplett ausbauen und den anderen bis Stufe 2 (ähnlich wie in BTD6, aber einfacher).

### Kategorie: Basis

**1. Mistgabel-Bauer** 🔱
Ein Bauer wirft Mistgabeln. Zuverlässiger Einzelschaden, trifft Boden- und Fluggegner.
- Pfad A *„Gabel-Hagel“*: wirft mehrere Gabeln gleichzeitig
- Pfad B *„Goldene Gabel“*: durchbohrt mehrere Aliens, mehr Schaden gegen Panzer

**2. Eier-Huhn** 🐔
Ein Huhn im Stall feuert Eier in schneller Folge. Billig, schnell, wenig Schaden pro Treffer.
- Pfad A *„Maschinenhuhn“*: extrem hohe Feuerrate
- Pfad B *„Faule Eier“*: Treffer verlangsamen und schwächen die Rüstung

**3. Bienenstock** 🐝
Bienen schwärmen aus und verfolgen ihre Ziele. Schaden über Zeit, gut gegen Fluggegner.
- Pfad A *„Killerbienen“*: mehr Bienen, mehr Schaden
- Pfad B *„Königin“*: Honig klebt Aliens fest (verlangsamt)

### Kategorie: Explosiv

**4. Heuballen-Katapult** 🌾
Schleudert Heuballen mit Flächenschaden. Trifft nur Bodengegner.
- Pfad A *„Brennender Ballen“*: hinterlässt eine Feuerfläche
- Pfad B *„Riesenballen“*: rollt den Weg entlang und walzt alles platt

**5. Popcorn-Kanone** 🍿
Schießt Maiskolben, die in der Luft zu Popcorn platzen und in alle Richtungen streuen.
- Pfad A *„Karamell-Popcorn“*: Splitter kleben und verlangsamen
- Pfad B *„Mega-Plopp“*: größerer Radius, mehr Splitter

**6. Kürbis-Mörser** 🎃
Artillerie mit riesiger Reichweite und großem Explosionsradius, aber langsam.
- Pfad A *„Panzerknacker“*: zerstört die Rüstung von Aliens
- Pfad B *„Kürbis-Regen“*: feuert mehrere Kürbisse auf einmal

### Kategorie: Technik

**7. Traktor-Tesla** 🚜⚡
Ein umgebauter Traktor mit Tesla-Spule. Blitze springen zwischen mehreren Aliens hin und her.
- Pfad A *„Gewitter“*: mehr Sprünge, mehr Ziele
- Pfad B *„Überladung“*: betäubt getroffene Aliens kurz

**8. Eismilch-Sprüher** 🥛❄️
Versprüht eiskalte Milch und verlangsamt alle Aliens in der Nähe.
- Pfad A *„Tiefkühltruhe“*: friert Aliens kurz komplett ein
- Pfad B *„Milchshake“*: eingefrorene Aliens nehmen mehr Schaden

**9. Windmühle** 🌬️
Pustet Aliens den Weg zurück. Spezialist gegen Ufos: kann Fluggegner zum Absturz bringen (sie werden zu Bodengegnern).
- Pfad A *„Orkan“*: stärkerer Rückstoß, größerer Bereich
- Pfad B *„Ufo-Fänger“*: holt auch große Ufos vom Himmel

### Kategorie: Spezial

**10. Güllefass** 💩
Verteilt Gülle auf dem Weg. Aliens, die durchlaufen, nehmen Giftschaden und rutschen langsamer.
- Pfad A *„Extra-Würzig“*: mehr Giftschaden, wirkt länger
- Pfad B *„Gülle-Fontäne“*: spritzt auch auf Fluggegner

**11. Mähdrescher** 🌾🚜
Fährt selbst auf dem Weg hin und her und mäht Aliens nieder. Hoher Nahkampfschaden.
- Pfad A *„Turbo-Mäher“*: schneller, mehr Schaden
- Pfad B *„Strohballen-Presse“*: besiegte Aliens werden zu Strohballen, die den Weg kurz blockieren

**12. Hofhund-Hütte** 🐕
Schickt Hofhunde los, die Aliens auf dem Weg aufhalten und beißen (blockieren den Weg wie Soldaten).
- Pfad A *„Rudel“*: mehr Hunde
- Pfad B *„Bernhardiner“*: große, zähe Hunde, die Kühe zurücktragen können

### Kategorie: Support

**13. Vogelscheuche** 🧑‍🌾
Erschreckt Aliens: Sie bleiben kurz stehen oder laufen ein Stück zurück. Kein Schaden.
- Pfad A *„Gruselscheuche“*: erschreckt mehrere Aliens gleichzeitig
- Pfad B *„Krähenschwarm“*: Krähen picken Aliens (wenig Schaden, aber gegen getarnte)

**14. Wetterhahn** 🐓
Macht **getarnte Aliens sichtbar** und gibt Türmen in der Nähe mehr Reichweite.
- Pfad A *„Radarhahn“*: größerer Sichtbereich, mehr Reichweite
- Pfad B *„Kampfhahn“*: Türme in der Nähe schießen schneller

**15. Hofladen** 🏪
Verdient zusätzliche Level-Münzen pro Welle (wie die Bananenfarm in BTD6).
- Pfad A *„Großmarkt“*: mehr Münzen
- Pfad B *„Bio-Siegel“*: Türme in der Nähe werden günstiger

---

## 9. Helden (3)

Helden stellt man im Level einmal kostenlos auf. Sie **steigen während des Levels von Stufe 1 bis 10 auf** und schalten dabei Fähigkeiten frei. Auf dem Hof werden sie in der **Heldenhütte** dauerhaft trainiert (Milch) und können Skins bekommen.

### 🧔 Bauer Heinz – der Allrounder
Der Besitzer des ersten angegriffenen Hofs. Grummelig, aber mutig. Trifft Boden- und Fluggegner.
- **Angriff:** Schrotflinte mit Streuschuss
- **Fähigkeit 1 (Stufe 3) „Ladung Schrot“:** Kegel-Schuss mit hohem Schaden
- **Fähigkeit 2 (Stufe 7) „Traktor-Raserei“:** Heinz fährt mit dem Traktor einmal über den ganzen Weg und überrollt alle Bodengegner
- **Freischaltung:** von Anfang an

### 👵 Oma Gertrud – die Unterstützerin
Heinz' Mutter. Lieb, aber mit dem Nudelholz unberechenbar.
- **Angriff:** Nudelholz im Nahkampf, wirft Kekse
- **Passiv:** Türme in ihrer Nähe schießen schneller („Kaffee und Kuchen“)
- **Fähigkeit 1 (Stufe 3) „Apfelkuchen-Wurf“:** Flächenschaden plus Verlangsamung
- **Fähigkeit 2 (Stufe 7) „Sonntagsbraten-Duft“:** Alle Aliens auf dem Feld bleiben einige Sekunden verzückt stehen
- **Freischaltung:** Sterne oder Diamanten

### 🐄 Berta – die Kampfkuh
Die erste Kuh, die je entführt wurde. Sie ist mit Alien-Technik entkommen und will Rache.
- **Angriff:** Laser aus den Augen, trifft Boden und Luft
- **Passiv:** Kühe in ihrer Nähe können nicht entführt werden (Schutzkreis)
- **Fähigkeit 1 (Stufe 3) „Hörner-Ramme“:** Stürmt den Weg entlang und wirft Aliens zurück
- **Fähigkeit 2 (Stufe 7) „Muh-Schallwelle“:** Schaltet Schilde aller Aliens aus und holt Ufos vom Himmel
- **Freischaltung:** Sterne oder Diamanten

---

## 10. Aliens

| Alien | Eigenschaft | Gegenmittel |
|---|---|---|
| 👽 **Grünling** | Standard-Alien | alles |
| 💨 **Flitzer** | Sehr schnell, wenig Leben | Eier-Huhn, Eismilch |
| 🛡️ **Brocken** | Gepanzert, langsam | Kürbis-Mörser, Goldene Gabel |
| 👻 **Schleicher** | Getarnt, unsichtbar ohne Wetterhahn | Wetterhahn, Krähenschwarm |
| 🔵 **Schild-Alien** | Energieschild schluckt die ersten Treffer | Traktor-Tesla, Berta |
| 🛸 **Ufo** | Fliegt direkt zur Weide, ignoriert Wege | Windmühle, Bienenstock, Mistgabel |
| 🩹 **Sanitäter** | Heilt Aliens in der Nähe | Kürbis-Mörser, Fokus-Feuer |
| 🧬 **Teiler** | Teilt sich beim Tod in zwei kleine Aliens | Flächenschaden |
| 🍼 **Saugrüssler** | Saugt Kühe aus der Entfernung an | Hofhund-Hütte, Vogelscheuche |
| 👑 **Bosse** | Pro Gebiet ein Boss, Finale: **Imperator Schlürf** | Helden-Fähigkeiten |

---

## 11. Shop und Einnahmen

- **Diamanten-Pakete** in verschiedenen Größen
- **Starterpaket** (einmalig, günstig): Diamanten, Rohstoffe, ein Skin
- **Hof-Pass** (optional, saisonal): Belohnungen für Level-Fortschritt, Gratis- und Premium-Spur
- **Werbung mit Belohnung** (optional, immer freiwillig): Video ansehen → doppelte Level-Belohnung oder Bauzeit verkürzen
- **Kosmetik:** Skins für Helden, Kühe, Türme und das Bauernhaus
- **Fairness:** Sterne sind nicht kaufbar, kein Turm ist nur gegen Echtgeld erhältlich

---

## 12. Stil und Technik

- **Unity 6** mit URP (Universal Render Pipeline) für gute Optik bei guter Leistung auf Handys
- **Low-Poly-3D**, kräftige Farben, weiche Schatten, Comic-Proportionen
- Kamera: schräge Draufsicht, auf dem Hof frei zoom- und schiebbar
- **Querformat** – auf dem Hof und in den Levels (mehr Platz für Wege und Türme, wie in BTD6)
- Keine Spielgeschwindigkeits-Erhöhung (2x) in der ersten Version
- Speicherstand lokal, später optional Cloud-Speicherung (Google Play / iCloud)

---

## 13. Vorschlag für die Entwicklung in Etappen

1. **Prototyp Tower Defense:** ein Level, 3 Türme, 3 Aliens, Sterne-Wertung – um zu prüfen, ob es Spaß macht *(in Arbeit, siehe README)*
2. **Prototyp Hof:** Bauernhaus, Maisfeld, Kuhstall, Lager, Knechte, Upgrades mit Bauzeit
3. **Verbindung:** Belohnungen aus Levels, Werkstätten schalten Türme frei
4. **Vertical Slice:** Gebiet 1 komplett (10 Levels), 6 Türme, Bauer Heinz, Grafik in Zielqualität
5. **Ausbau:** restliche Türme, Helden, Gebiete, Shop
6. **Soft Launch** in einem Testmarkt, dann weltweiter Start

---

## 14. Entscheidungen

- **Querformat** für das ganze Spiel
- **Keine doppelte Geschwindigkeit** in der ersten Version (kann später ergänzt werden)
- **Name:** vorerst egal, „Muh Defense“ bleibt Arbeitstitel

## 15. Offene Fragen

- Sollen Aliens später doch auch den eigenen Hof angreifen (z. B. als Ereignis)?
- Endlos-Modus für Fortgeschrittene?
