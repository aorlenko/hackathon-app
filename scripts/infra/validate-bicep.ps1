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
    # az writes linter warnings to stderr; do not treat as terminating under StrictMode/Stop
    $prevEap = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        az bicep build --file $file 2>&1 | Out-Null
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[parameter/tooling] Bicep build failed for: $file (az exit code $LASTEXITCODE)" -ForegroundColor Red
        Write-Host "Next steps: run 'az bicep build --file $file' locally for full errors; ensure Azure CLI is current ('az upgrade') and Bicep is available ('az bicep install')." -ForegroundColor Yellow
        throw "Bicep build failed for $file."
    }
}

$parameterFile = "infra/environments/hackathon/parameters.dev.json"
Write-Host "Validating parameter file $parameterFile..." -ForegroundColor Cyan
try {
    $raw = Get-Content $parameterFile -Raw -ErrorAction Stop
    $null = $raw | ConvertFrom-Json
}
catch {
    Write-Host "[parameter] Could not parse JSON: $parameterFile" -ForegroundColor Red
    Write-Host "Next steps: validate JSON syntax (trailing commas, quotes); compare structure to ARM deploymentParameters schema." -ForegroundColor Yellow
    throw
}

Write-Host "Bicep validation completed successfully." -ForegroundColor Green
