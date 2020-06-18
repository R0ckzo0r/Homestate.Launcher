# Launcher


**DO NOT USE ATM LAUNCHER IST NICHT FERTIG UND GULPFILE + ÄNDERUNGEN NOCH NICHT GEPUSHT**

**#Methode 1 - --gulpfile**

1) `npm install -g yarn gulp`
2) `yarn`
3) `yarn release` oder `yarn build`

Alle Files werden sich bei Start über IDE im entsprechendend "Debug" oder "Release" Order befinden.

**yarn release oder yarn build reicht nicht aus, das Projekt muss neu gebildet werden!!!**

Pfad für Launcher Files : `./Launcher/HomeState.Launcher/bin/x64/{Configuration}`

Pfad für ManifestGenerator : `./Launcher/HomeState.Launcher.ManigestGenerator/bin/x64/{Configuration}`

Prepare Update:
1) **Version** in der **AssemblyInfo.cs** im HomeState.Launcher.csproj **hochschrauben**!
2) **IDE** im **Release** mit richtiger Configuration **starten**.
3) Gucken ob der Launcher richtig funktioniert.
4) Dienst wieder stoppen.
5) Im **ManifestGenerator Pfad** die **HomeState.Launcher.ManifestGenerator.exe benutzen**.
6) Alle Files im Launcher Pfad in eine ReleasevVERSIONSNUMMER (z.B. Releasev3.0) zippen.
6.1) unter plugins/core/ kann die libcef.dll gelöscht werden (107 mb).
7) In der generierten update.xml im Manifestgenerator Pfad den Namen der .zip anpassen! ACHTUNG! Das .zip muss bleiben!!!
8) die .zip hochladen und in der bestehenden update.xml die letzten 3 Zeilen (LauncherVersion, LauncherHash, LauncherArchive) überschreiben und nach upload speichern.


**#Methode 2 Visual Studio**

1) **Version** in der **AssemblyInfo.cs** im HomeState.Launcher.csproj **hochschrauben**!
2) Erstellen > Projektmappe erstellen (STRG+UMSCHALT+B)
2.1) .bundle "manuell" per CMD erstellen lassen. Hier ein Beispiel

`7z.exe a -m0=Copy -tzip "C:\Users\Daniel\RiderProjects\Launcher\HomeState.Launcher\bin\x64\$ConfigurationName$\plugins\core\ui.bundle" "C:\Users\Daniel\RiderProjects\Launcher\HomeState.Launcher.Core\ui\*"`

`7z.exe a -m0=Copy -tzip "C:\Users\Daniel\RiderProjects\Launcher\HomeState.Launcher\bin\x64\$ConfigurationName$\plugins\core\savegame.bundle" "C:\Users\Daniel\RiderProjects\Launcher\HomeState.Launcher.Core\savegame\*"`

3) die Nancy.* (3 Dateien) von /plugins/core in Hauptverzeichnis kopieren

4) Gucken ob der Launcher richtig funktioniert über die HomeState.Launcher.exe im Compilerverzeichnis.
5) Im **ManifestGenerator Pfad** die **HomeState.Launcher.ManifestGenerator.exe benutzen**.
6) Alle Files im Launcher Pfad in eine ReleasevVERSIONSNUMMER (z.B. Releasev3.0) zippen.
7) unter plugins/core/ kann die libcef.dll gelöscht werden (107 mb).
8) In der generierten update.xml im Manifestgenerator Pfad den Namen der .zip anpassen! ACHTUNG! Das .zip muss bleiben!!!
9) die .zip hochladen und in der bestehenden update.xml die letzten 3 Zeilen (LauncherVersion, LauncherHash, LauncherArchive) überschreiben und nach upload speichern.
