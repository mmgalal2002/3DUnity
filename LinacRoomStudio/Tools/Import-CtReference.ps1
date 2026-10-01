param(
 [string]$Source = (Join-Path $PSScriptRoot '..\..\CT_Point_of_Interest_Shielding_Unity_Specification.md'),
 [string]$Destination = (Join-Path $PSScriptRoot '..\Assets\Resources\CT')
)
$ErrorActionPreference = 'Stop'
$culture = [Globalization.CultureInfo]::InvariantCulture
$markdown = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $Source))
$blocks = [regex]::Matches($markdown, '(?ms)^```(?<language>json|csv)\s*\r?\n(?<body>.*?)^```')
$ct = $null
$primaryCsv = $null
foreach ($block in $blocks) {
 $body = $block.Groups['body'].Value
 if ($block.Groups['language'].Value -eq 'json') {
  $record = $body | ConvertFrom-Json
  if ($record.datasetId -eq 'NCRP147_APPENDIX_A_CT_SECONDARY') { if ($null -ne $ct) { throw 'Duplicate CT dataset.' }; $ct = $record }
 } elseif ($body.StartsWith('material,kvp,anode,alpha_per_mm,beta_per_mm,gamma,source_state,pdf_page,report_page')) {
  if ($null -ne $primaryCsv) { throw 'Duplicate primary coefficient archive.' }
  $primaryCsv = @($body | ConvertFrom-Csv)
 }
}
if ($null -eq $ct -or $ct.fits.Count -ne 4 -or $ct.beamFamily -ne 'CT_SECONDARY' -or $ct.thicknessUnit -ne 'mm' -or $ct.interpolationEnabled -or $ct.extrapolationEnabled) { throw 'Incomplete or incompatible CT dataset.' }
$expected = @{
 CT_PB_120 = @(120, 2.246, 5.73, 0.547, 'lead')
 CT_PB_140 = @(140, 2.009, 3.99, 0.342, 'lead')
 CT_CONCRETE_120 = @(120, 0.0383, 0.0142, 0.658, 'concrete')
 CT_CONCRETE_140 = @(140, 0.0336, 0.0122, 0.519, 'concrete')
}
$fitIds = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($fit in $ct.fits) {
 if (-not $fitIds.Add($fit.id) -or -not $expected.ContainsKey($fit.id)) { throw 'Unknown or duplicate CT fit.' }
 $values = $expected[$fit.id]
 if ($fit.kvp -ne $values[0] -or $fit.alpha -ne $values[1] -or $fit.beta -ne $values[2] -or $fit.gamma -ne $values[3] -or $fit.material -ne $values[4]) { throw ('CT coefficient mismatch: ' + $fit.id) }
}
if ($primaryCsv.Count -ne 156) { throw 'Expected 156 primary archive grid rows.' }
$primaryRows = foreach ($row in $primaryCsv) {
 $provided = $row.source_state -eq 'provided'
 if (-not $provided -and $row.source_state -ne 'blank_triplet' -and $row.source_state -ne 'row_omitted') { throw 'Unknown primary source state.' }
 if (-not $provided -and ($row.alpha_per_mm -or $row.beta_per_mm -or $row.gamma)) { throw 'Missing archive row contains invented coefficients.' }
 [ordered]@{
  material = $row.material; kvp = [int]$row.kvp; anode = $row.anode
  beamFamily = $(if ($row.anode -eq 'molybdenum') { 'PRIMARY_MAMMOGRAPHIC' } else { 'PRIMARY_RADIOGRAPHIC' })
  provided = $provided; sourceState = $row.source_state
  alpha = $(if ($provided) { [double]::Parse($row.alpha_per_mm, $culture) } else { 0 })
  beta = $(if ($provided) { [double]::Parse($row.beta_per_mm, $culture) } else { 0 })
  gamma = $(if ($provided) { [double]::Parse($row.gamma, $culture) } else { 0 })
  pdfPage = [int]$row.pdf_page; reportPage = [int]$row.report_page
 }
}
$providedRows = @($primaryRows | Where-Object { $_.provided })
if ($providedRows.Count -ne 146 -or @($providedRows | Where-Object { $_.beta -lt 0 }).Count -ne 8) { throw 'Primary archive count/sign mismatch.' }
foreach ($row in $providedRows) {
 if ($row.alpha -le 0 -or $row.gamma -le 0 -or $row.alpha + $row.beta -le 0) { throw 'Invalid primary Archer coefficients.' }
}
$wood110 = $providedRows | Where-Object { $_.material -eq 'wood' -and $_.kvp -eq 110 }
if ($wood110.gamma -ne 3.309) { throw 'Wood 110-kVp gamma was not preserved.' }
$sourceHash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash.ToLowerInvariant()
$pdfHash = '168bd82cfe61ff318c73e81becbdb381b04b0998af9a355246bf31158f801e81'
$ct | Add-Member -NotePropertyName schemaVersion -NotePropertyValue 1
$ct | Add-Member -NotePropertyName specificationSha256 -NotePropertyValue $sourceHash
$ct | Add-Member -NotePropertyName reportedPdfSha256 -NotePropertyValue $pdfHash
$archive = [ordered]@{
 schemaVersion = 1; datasetId = 'NCRP147_APPENDIX_A_TABLE_A1_PRIMARY'
 thicknessUnit = 'mm'; coefficientInverseLengthUnit = '1/mm'; ctFallbackEnabled = $false
 specificationSha256 = $sourceHash; reportedPdfSha256 = $pdfHash; rows = @($primaryRows)
}
[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($Destination)) | Out-Null
$encoding = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $Destination 'ct-secondary.json'), ($ct | ConvertTo-Json -Depth 10), $encoding)
[IO.File]::WriteAllText((Join-Path $Destination 'primary-archive.json'), ($archive | ConvertTo-Json -Depth 10), $encoding)
Write-Output ('ROOM_STUDIO_CT_REFERENCE_IMPORT_PASSED: 4 CT fits, 156 archive rows, 146 provided, 8 negative-beta wood; specification SHA256 ' + $sourceHash)