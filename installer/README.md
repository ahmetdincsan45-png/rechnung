# Installer Akışı

Bu klasör, uygulamayı publish edip kurulum paketi üretmek için gereken dosyaları içerir.

## Hızlı kullanım

1. Publish al:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1`
2. Inno Setup kuruluysa setup üret:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1 -BuildInstaller -AutoIncrementVersion`
3. En kolay yol:
   - `installer\Oluştur-Setup.bat` dosyasını çift tıklayın

## Çıktılar

- Publish klasörü: `installer\artifacts\publish`
- Setup klasörü: `installer\artifacts\setup`
- Güncelleme manifesti: `installer\artifacts\setup\latest.json`

## Profesyonel kullanım önerisi

- Otomatik patch artırarak setup üretin:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1 -BuildInstaller -AutoIncrementVersion`
- İsterseniz manuel sürüm de verebilirsiniz:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1 -BuildInstaller -Version 1.2.0`
- Son kullanılan sürüm `installer\version.txt` içinde tutulur.
- Oluşan setup adı sürümü içerir:
	- `Rechnung-Setup-1.0.1.exe`
- İsterseniz yayın adresi de verebilirsiniz:
	- `powershell -ExecutionPolicy Bypass -File .\installer\publish-installer.ps1 -BuildInstaller -AutoIncrementVersion -UpdateBaseUrl https://example.com/downloads`
- Bu durumda `latest.json` içindeki indirme bağlantısı tam URL olarak yazılır.
- Aynı anda publish çıktısına `update-settings.json` da eklenir; uygulama açılışta bu dosyadan `latest.json` adresini otomatik okur.

## Dosyalar

- `publish-installer.ps1` - publish ve isteğe bağlı setup üretimi
- `Oluştur-Setup.bat` - tek tıkla Setup.exe üretimi
- `Örnek.iss` - Inno Setup tanımı
- `install.ps1` - script tabanlı alternatif yerel kurulum
- `uninstall.ps1` - script tabanlı alternatif kaldırma
