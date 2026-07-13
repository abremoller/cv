<#
.SYNOPSIS
    Publishes / updates the CV data by PUTting a JSON payload to the secured admin API.

.DESCRIPTION
    Reads a CV data JSON file and sends it to PUT {BaseUrl}/api/cv with the admin
    API key. The key is read from the -ApiKey parameter or the CV_ADMIN_API_KEY
    environment variable — it is never stored in the repo.

.EXAMPLE
    $env:CV_ADMIN_API_KEY = '<your-secret>'
    ./tools/publish-cv.ps1 -BaseUrl https://cv.example.com -DataFile ./cv-data.local.json

.EXAMPLE
    ./tools/publish-cv.ps1 -BaseUrl https://cv.example.com -DataFile ./cv-data.local.json -ApiKey '<your-secret>'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $BaseUrl,
    [string] $DataFile = "./cv-data.local.json",
    [string] $ApiKey = $env:CV_ADMIN_API_KEY
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "No API key. Pass -ApiKey or set the CV_ADMIN_API_KEY environment variable."
}
if (-not (Test-Path $DataFile)) {
    throw "Data file not found: $DataFile"
}

# Validate the JSON before sending.
# NOTE: use .NET ReadAllText (UTF-8 by default, with BOM detection) rather than
# Get-Content -Raw. Under Windows PowerShell 5.1, Get-Content decodes a BOM-less
# file with the system ANSI code page, corrupting multi-byte UTF-8 (e.g. "é" ->
# "Ã©"), which then gets double-encoded when re-serialised to UTF-8 bytes below.
$json = [System.IO.File]::ReadAllText($DataFile)
try { $null = $json | ConvertFrom-Json } catch { throw "Invalid JSON in ${DataFile}: $_" }

$uri = "$($BaseUrl.TrimEnd('/'))/api/cv"
Write-Host "Publishing $DataFile -> $uri" -ForegroundColor Cyan

# Send the body as UTF-8 bytes so non-ASCII characters survive intact
# (Windows PowerShell 5.1 otherwise corrupts multi-byte UTF-8 in string bodies).
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json)

Invoke-RestMethod -Method Post -Uri $uri -Body $bytes -ContentType 'application/json; charset=utf-8' `
    -Headers @{ 'X-Api-Key' = $ApiKey }

Write-Host "CV published successfully." -ForegroundColor Green
