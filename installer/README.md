# Installer und GitHub Release-Ablauf

Dieser Ordner enthält den Ablauf, um Rechnung zu veröffentlichen, ein Setup zu erzeugen und die Update-Dateien mit GitHub Releases kompatibel zu halten.

## Schnellstart

1. Publish erstellen:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1`
2. Setup erstellen:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1 -BuildInstaller -AutoIncrementVersion`
3. Einfachster Weg:
	- `installer\Oluştur-Setup.bat` doppelklicken

## Ausgabeordner

- Publish: `installer\artifacts\publish`
- Setup: `installer\artifacts\setup`
- Release-Manifest: `installer\artifacts\setup\latest.json`

## GitHub Release-Ablauf

1. Neue Version erzeugen:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1 -BuildInstaller -AutoIncrementVersion`
2. Das erzeugte Setup hochladen:
	- Datei: `Rechnung-Setup-X.Y.Z.exe`
3. Auf GitHub einen Release mit passendem Tag anlegen:
	- Tag-Format: `vX.Y.Z`
4. Das Setup als Release Asset hochladen.
5. Die aktualisierten Dateien committen und pushen:
	- `Örnek/update-settings.json`
	- `Örnek/updates/update-manifest.json`

## Wichtige Hinweise

- Die Versionsnummer wird in `installer\version.txt` geführt.
- Das Setup heißt immer:
	- `Rechnung-Setup-X.Y.Z.exe`
- Das Setup und App-Updates enthalten nur Programmdateien.
- Kundendaten, alte Rechnungen, Archivpfade und andere Benutzerinhalte bleiben pro Computer getrennt in den Benutzerordnern gespeichert.
- Eine neue Installation auf einem anderen Computer startet deshalb ohne Ihre lokalen Inhalte.
- Der Publish-Script erzeugt automatisch:
	- die gebündelte `update-settings.json`
	- die Quell-Datei `Örnek/update-settings.json`
	- die Quell-Datei `Örnek/updates/update-manifest.json`
	- das Release-Manifest `installer\artifacts\setup\latest.json`
- Bei einer Deinstallation werden nur die installierten Programmdateien entfernt; Benutzerinhalte bleiben erhalten.
- Das GitHub-Repo ist fest auf `ahmetdincsan45-png/rechnung` ausgerichtet.

## Dateien

- `publish-installer.ps1` - Publish, Setup und Manifest-Aktualisierung
- `Oluştur-Setup.bat` - einfacher Start des Setup-Builds
- `Örnek.iss` - Inno Setup Definition
- `install.ps1` - alternatives lokales Installationsskript
- `uninstall.ps1` - alternatives lokales Deinstallationsskript
