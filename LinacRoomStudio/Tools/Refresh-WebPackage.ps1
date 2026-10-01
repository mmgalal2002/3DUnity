param(
 [string]$SourcePath=(Join-Path $PSScriptRoot '..\..\WebGL'),
 [string]$DestinationPath=(Join-Path $PSScriptRoot '..\..\web'),
 [string]$EvidencePath,
 [switch]$VerifyOnly
)
$ErrorActionPreference='Stop'

function Get-PayloadFiles([string]$Root){
 $files=@(Get-Item -LiteralPath (Join-Path $Root 'index.html'))
 foreach($directory in @('Build','StreamingAssets')){
  $directoryPath=Join-Path $Root $directory
  if(-not (Test-Path -LiteralPath $directoryPath -PathType Container)){
   throw "Missing payload directory: $directoryPath"
  }
  $files+=@(Get-ChildItem -LiteralPath $directoryPath -File -Recurse)
 }
 foreach($file in ($files | Sort-Object FullName)){
  [pscustomobject]@{
   path=$file.FullName.Substring($Root.Length+1).Replace('\','/')
   bytes=$file.Length
   sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
  }
 }
}

$sourceRoot=[IO.Path]::GetFullPath($SourcePath).TrimEnd('\','/')
$destinationRoot=[IO.Path]::GetFullPath($DestinationPath).TrimEnd('\','/')
if($sourceRoot.Equals($destinationRoot,[StringComparison]::OrdinalIgnoreCase) -or
 $sourceRoot.StartsWith($destinationRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or
 $destinationRoot.StartsWith($sourceRoot+'\',[StringComparison]::OrdinalIgnoreCase)){
 throw 'Source and destination must be separate, non-nested directories.'
}
foreach($required in @('index.html','Build/WebGL.loader.js','Build/WebGL.data','Build/WebGL.framework.js','Build/WebGL.wasm','StreamingAssets/ProShield/browser-worker.mjs')){
 if(-not (Test-Path -LiteralPath (Join-Path $sourceRoot $required) -PathType Leaf)){
  throw "Incomplete source build: $required"
 }
}
$sourceFiles=@(Get-PayloadFiles $sourceRoot)
$sourceByPath=@{}
foreach($file in $sourceFiles){$sourceByPath[$file.path]=$file}

if(-not $VerifyOnly){
 New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
 foreach($file in $sourceFiles){
  $destinationFile=Join-Path $destinationRoot $file.path
  New-Item -ItemType Directory -Path (Split-Path $destinationFile -Parent) -Force | Out-Null
  Copy-Item -LiteralPath (Join-Path $sourceRoot $file.path) -Destination $destinationFile -Force
 }
 foreach($file in @(Get-PayloadFiles $destinationRoot)){
  if(-not $sourceByPath.ContainsKey($file.path)){
   Remove-Item -LiteralPath (Join-Path $destinationRoot $file.path) -Force
  }
 }
}

$destinationFiles=@(Get-PayloadFiles $destinationRoot)
$destinationByPath=@{}
foreach($file in $destinationFiles){$destinationByPath[$file.path]=$file}
$differences=@()
foreach($file in $sourceFiles){
 if(-not $destinationByPath.ContainsKey($file.path)){
  $differences+="Missing: $($file.path)"
 }elseif($destinationByPath[$file.path].sha256 -ne $file.sha256){
  $differences+="Hash mismatch: $($file.path)"
 }
}
foreach($file in $destinationFiles){
 if(-not $sourceByPath.ContainsKey($file.path)){$differences+="Unexpected: $($file.path)"}
}
if($differences.Count -gt 0){throw ($differences -join [Environment]::NewLine)}

if($EvidencePath){
 $evidenceFile=[IO.Path]::GetFullPath($EvidencePath)
 New-Item -ItemType Directory -Path (Split-Path $evidenceFile -Parent) -Force | Out-Null
 [pscustomobject]@{
  verifiedAtUtc=[DateTime]::UtcNow.ToString('o')
  source=$sourceRoot
  destination=$destinationRoot
  fileCount=$sourceFiles.Count
  files=$sourceFiles
 } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $evidenceFile -Encoding UTF8
}
Write-Output "ROOM_STUDIO_WEB_PACKAGE_VERIFIED: $($sourceFiles.Count) matching SHA-256 hashes"