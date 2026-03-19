[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI ('az') is required to validate Bicep templates."
}

$filesToBuild = @(
    "infra/bicep/modules/monitoring.bicep",
    "infra/bicep/main.bicep"
)

foreach ($file in $filesToBuild) {
    Write-Host "Building $file..." -ForegroundColor Cyan
    az bicep build --file $file | Out-Null
}

$parameterFile = "infra/environments/hackathon/parameters.dev.json"
Write-Host "Validating parameter file $parameterFile..." -ForegroundColor Cyan
Get-Content $parameterFile -Raw | ConvertFrom-Json | Out-Null

Write-Host "Bicep validation completed successfully." -ForegroundColor Green
