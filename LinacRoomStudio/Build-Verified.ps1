param(
 [string]$EditorPath='C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
 [int]$TimeoutSeconds=600
)
$ErrorActionPreference='Stop'
if(-not (Test-Path -LiteralPath $EditorPath)){throw 'Unity Editor was not found. Supply -EditorPath or use Unity Hub.'}
$projectPath=$PSScriptRoot
$logPath=Join-Path $projectPath ('build-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
$cachePath=Join-Path $projectPath 'Library\PackageCacheLocal'
New-Item -ItemType Directory -Path $cachePath -Force | Out-Null
$previousCache=$env:UPM_CACHE_ROOT
try{
 $env:UPM_CACHE_ROOT=$cachePath
 $buildArgs=@('-batchmode','-nographics','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','BuildStudio.Build','-logFile',('"'+$logPath+'"'))
 $buildProcess=Start-Process -FilePath $EditorPath -ArgumentList $buildArgs -WindowStyle Hidden -PassThru
 if(-not $buildProcess.WaitForExit($TimeoutSeconds*1000)){
  # This is only the editor instance launched by this script.
  $buildProcess.Kill()
  throw "Unity build timed out. Read $logPath and confirm Unity Hub license activation."
 }
 $buildProcess.Refresh()
 $logText=if(Test-Path -LiteralPath $logPath){Get-Content -LiteralPath $logPath -Raw}else{''}
 $appPath=Join-Path (Split-Path $projectPath -Parent) 'Windows\LinacRoomStudio.exe'
 if($buildProcess.ExitCode -ne 0 -or $logText -notmatch 'ROOM_STUDIO_BUILD_SUCCESS' -or -not (Test-Path -LiteralPath $appPath)){
  throw "Unity did not produce a verified build. Read $logPath. Resolve any Unity Hub license or Package Manager error first."
 }
 Write-Output "BUILD VERIFIED: $appPath"
 Write-Output 'This verifies compilation/build only. Screenshots and smoke tests are prohibited; interactive acceptance remains separate.'
}finally{$env:UPM_CACHE_ROOT=$previousCache}
