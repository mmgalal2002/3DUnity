@echo off
set "ROOM_STUDIO_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
if not exist "%ROOM_STUDIO_EDITOR%" (
  echo Unity 6000.6.0f1 was not found. Open this folder from Unity Hub.
  pause
  exit /b 1
)
start "" "%ROOM_STUDIO_EDITOR%" -projectPath "%~dp0."
