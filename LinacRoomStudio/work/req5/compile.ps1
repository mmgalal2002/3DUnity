$ErrorActionPreference='Stop'
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $projectRoot
$unityData='C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data'
$compiler=Join-Path $unityData 'DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll'
$runtime=Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$summary=@('Unity 6000.6.0f1 source compilation: '+[DateTime]::UtcNow.ToString('O'))
foreach($target in @(@('editor','1900b0aE'),@('windows','1900b0aP'),@('webgl','2000b0aP'))){
 $folder=Join-Path $PSScriptRoot $target[0]
 New-Item -ItemType Directory -Path $folder -Force | Out-Null
 $sourceRsp=Join-Path $projectRoot ('Library/Bee/artifacts/'+$target[1]+'.dag/Assembly-CSharp.rsp')
 $lines=Get-Content -LiteralPath $sourceRsp | Where-Object {$_ -notmatch '^-(out|refout):' -and $_ -notmatch '^"Assets/Scripts/'}
 $lines+='-out:"'+(Join-Path $folder 'Assembly-CSharp.dll')+'"'
 $lines+='-refout:"'+(Join-Path $folder 'Assembly-CSharp.ref.dll')+'"'
 foreach($source in Get-ChildItem -LiteralPath Assets/Scripts -Filter '*.cs'){$lines+='"'+$source.FullName+'"'}
 $rsp=Join-Path $folder 'compile.rsp'
 [IO.File]::WriteAllLines($rsp,$lines)
 $diagnostics=& $runtime $compiler ('@'+$rsp) 2>&1
 $exit=$LASTEXITCODE
 $diagnostics | Set-Content -LiteralPath (Join-Path $folder 'compile.log')
 Write-Output ($target[0]+': compiler exit '+$exit)
 $summary+=($target[0]+': compiler exit '+$exit+'; diagnostics '+@($diagnostics).Count)
 if($diagnostics){Write-Output $diagnostics}
 if($exit -ne 0){throw 'Compilation failed'}
}
$editorFolder=Join-Path $PSScriptRoot 'editor'
$lines=Get-Content -LiteralPath 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp' | Where-Object {$_ -notmatch '^-(out|refout):'}
$lines=$lines -replace '-r:"Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.ref.dll"',('-r:"'+(Join-Path $editorFolder 'Assembly-CSharp.ref.dll')+'"')
$lines+='-out:"'+(Join-Path $editorFolder 'Assembly-CSharp-Editor.dll')+'"'
$rsp=Join-Path $editorFolder 'checks-compile.rsp'
[IO.File]::WriteAllLines($rsp,$lines)
$diagnostics=& $runtime $compiler ('@'+$rsp) 2>&1
$exit=$LASTEXITCODE
$diagnostics | Set-Content -LiteralPath (Join-Path $editorFolder 'checks-compile.log')
Write-Output ('editor checks: compiler exit '+$exit)
if($diagnostics){Write-Output $diagnostics}
if($exit -ne 0){throw 'Editor check compilation failed'}
$summary+=('editor checks: compiler exit '+$exit+'; diagnostics '+@($diagnostics).Count)
$summary | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'compilation.log')
