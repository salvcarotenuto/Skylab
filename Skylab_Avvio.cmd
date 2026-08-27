@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Skylab.ps1"

if errorlevel 1 (
    echo.
    echo Avvio di SkyLab non riuscito. Premi un tasto per chiudere.
    pause >nul
)
