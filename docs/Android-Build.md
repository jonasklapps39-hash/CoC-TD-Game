# Android-App automatisch bauen (GitHub Actions)

GitHub baut die APK in der Cloud. Du brauchst dafür keinen Mac und kein Unity – nur einmal eine Unity-Lizenzdatei.

## Einmalig einrichten (ca. 10 Minuten, am Mac)

1. **Lizenzdatei finden.** Auf dem Mac, auf dem Unity Hub angemeldet ist, liegt sie hier:
   `/Library/Application Support/Unity/Unity_lic.ulf`
   (Im Finder: *Gehe zu → Gehe zum Ordner …* und den Pfad einfügen.)
   Die Datei mit TextEdit öffnen und den **ganzen Inhalt** kopieren.
2. **Secrets bei GitHub anlegen.** Auf github.com im Repository *CoC-TD-Game*:
   *Settings → Secrets and variables → Actions → New repository secret*. Drei Secrets anlegen:
   - `UNITY_LICENSE` – der kopierte Inhalt der `.ulf`-Datei
   - `UNITY_EMAIL` – die E-Mail-Adresse deines Unity-Kontos
   - `UNITY_PASSWORD` – das Passwort deines Unity-Kontos
3. Fertig. Die Daten sind verschlüsselt und nur für die Builds sichtbar.

## App bauen und aufs Handy laden

1. Auf github.com: *Actions → Android-App bauen → Run workflow*.
   (Läuft außerdem automatisch, wenn neue Spiel-Änderungen gepusht werden.)
2. Der Build dauert beim ersten Mal ca. 30–60 Minuten, danach meist schneller.
3. Wenn er grün ist: den Lauf anklicken, unten bei *Artifacts* **MuhDefense-APK** herunterladen (ZIP).
4. Auf dem Android-Handy die ZIP öffnen und `MuhDefense.apk` antippen. Beim ersten Mal fragt Android,
   ob Apps aus dieser Quelle installiert werden dürfen – erlauben.

Tipp: Das geht auch direkt vom Handy aus über die GitHub-Webseite im Browser.

## Wenn etwas schiefgeht

- **Build übersprungen** (Hinweis „Noch keine Unity-Lizenz“): Secret `UNITY_LICENSE` fehlt.
- **Lizenzfehler:** Unity Hub am Mac einmal neu anmelden, dann die `.ulf` neu kopieren und das Secret ersetzen.
- **Andere Fehler:** Screenshot vom roten Schritt schicken.

Hinweis: Private Repositories haben bei GitHub ein kostenloses Kontingent an Build-Minuten pro Monat (2000).
Ein Android-Build braucht ca. 20–60 Minuten davon.
