$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$proShield = Join-Path $projectRoot 'unity\LinacRoomStudio\Assets\StreamingAssets\ProShield'
$snapshot = Join-Path $PSScriptRoot 'fixture-snapshot'
New-Item -ItemType Directory -Path $snapshot -Force | Out-Null

$fixtures = @('example-design.json', 'golden-report.json', 'import-fixture.json', 'imported-design-fixture.json')
$originalHashes = @{}
foreach ($name in $fixtures) {
 $source = Join-Path $proShield $name
 if (!(Test-Path -LiteralPath $source)) { throw "Missing tracked fixture: $source" }
 $originalHashes[$name] = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
 Copy-Item -LiteralPath $source -Destination (Join-Path $snapshot $name) -Force
}

$scripts = @('benchmark.mjs', 'checks.mjs', 'workspace-checks.mjs', 'floor-plan-checks.mjs', 'precision-checks.mjs', 'plan-authoring-checks.mjs', 'door-checks.mjs')
$results = [System.Collections.Generic.List[object]]::new()
$nodePath = (Get-Command node -ErrorAction Stop).Source
try {
 foreach ($name in $scripts) {
  $output = & $nodePath (Join-Path $proShield $name) 2>&1 | Out-String
  $exitCode = $LASTEXITCODE
  $results.Add([pscustomobject]@{script=$name;exitCode=$exitCode;output=$output.TrimEnd()})
 }
}
finally {
 foreach ($name in $fixtures) {
  $destination = Join-Path $proShield $name
  [System.IO.File]::WriteAllBytes($destination, [System.IO.File]::ReadAllBytes((Join-Path $snapshot $name)))
 }
}

foreach ($name in $fixtures) {
 $restoredHash = (Get-FileHash -LiteralPath (Join-Path $proShield $name) -Algorithm SHA256).Hash
 if ($restoredHash -ne $originalHashes[$name]) { throw "Fixture restoration mismatch: $name" }
}
$report = [pscustomobject]@{
 utc = (Get-Date).ToUniversalTime().ToString('o')
 node = (& $nodePath --version)
 fixtureSha256 = $originalHashes
 fixturesRestoredByteForByte = $true
 results = $results
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'proshield-node-results.json') -Encoding utf8
$results | ForEach-Object { '{0}: exit {1} - {2}' -f $_.script,$_.exitCode,($_.output -replace '\s+',' ') }
if (@($results | Where-Object { $_.exitCode -ne 0 }).Count -ne 0) { throw 'One or more ProShield Node checks failed; see proshield-node-results.json.' }
