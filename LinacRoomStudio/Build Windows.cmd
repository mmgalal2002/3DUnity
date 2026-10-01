@echo off
set "ROOM_STUDIO_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
if not exist "%ROOM_STUDIO_EDITOR%" (
  echo Unity 6000.6.0f1 was not found. Build from Unity using Room Studio / Build Windows app.
  pause
  exit /b 1
)
echo Close this project in Unity before building.
"%ROOM_STUDIO_EDITOR%" -batchmode -nographics -quit -projectPath "%~dp0." -buildTarget StandaloneWindows64 -executeMethod BuildStudio.Build -logFile "%~dp0build.log"
echo Check build.log for ROOM_STUDIO_BUILD_SUCCESS. The app is written to the Windows folder beside this project.
pause
