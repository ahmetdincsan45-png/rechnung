@echo off
setlocal
cd /d "%~dp0"
echo Setup olusturuluyor. Surum otomatik artirilacak.
powershell -ExecutionPolicy Bypass -File ".\publish-installer.ps1" -BuildInstaller -AutoIncrementVersion
if errorlevel 1 (
	echo.
	echo Setup olusturma basarisiz oldu.
	pause
	exit /b 1
)
echo.
echo Setup olusturma tamamlandi.
pause
