# Android-App bauen

## Weg 1: Direkt in Unity am Mac (funktioniert immer)

1. Unity öffnen, oben im Menü **Muh Defense → Android einrichten** (stellt alles ein, legt die Startszene an, setzt das App-Icon).
2. **File → Build Profiles → Android → Build**, als Ziel z. B. den Schreibtisch wählen, Name `MuhDefense.apk`.
   Der erste Bau dauert 5–15 Minuten.
3. Die APK auf **drive.google.com** hochladen, am Handy in der Google-Drive-App antippen und installieren
   (beim ersten Mal „Apps aus dieser Quelle zulassen“ erlauben).
   Alternativ: Handy per USB anschließen (USB-Debugging an) und **Build And Run** wählen.

## Weg 2: Automatisch in der Cloud (GitHub Actions)

Klappt nur, wenn eine Lizenzdatei `Unity_lic.ulf` vorhanden ist – neuere Unity-Hub-Versionen legen sie nicht mehr an.

GitHub baut die APK in der Cloud. Du brauchst dafür keinen Mac und kein Unity – nur einmal eine Unity-Lizenzdatei.

## Einmalig einrichten (ca. 5 Minuten)

Auf github.com im Repository *CoC-TD-Game*: *Settings → Secrets and variables → Actions → New repository secret*.

- `UNITY_EMAIL` – die E-Mail-Adresse deines Unity-Kontos
- `UNITY_PASSWORD` – das Passwort deines Unity-Kontos
- *(nur falls vorhanden)* `UNITY_LICENSE` – Inhalt von `/Library/Application Support/Unity/Unity_lic.ulf`.
  Neuere Unity-Hub-Versionen legen diese Datei nicht mehr an (stattdessen `~/Library/Unity/licenses/UnityEntitlementLicense.xml`) – dann einfach weglassen.

Die Daten sind verschlüsselt und nur für die Builds sichtbar.

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
