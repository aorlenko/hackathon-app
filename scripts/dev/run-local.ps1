[CmdletBinding()]
param(
    [switch]$DependenciesOnly,
    [switch]$SkipDependencies,
    [switch]$IncludeContainerizedApps
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$serviceDefinitions = @(
    @{
        Name = "market-service"
        WorkingDirectory = "apps/services/market-service"
        ProbePath = "apps/services/market-service/src/MarketService/MarketService.csproj"
        Command = 'dotnet watch run --project "src/MarketService/MarketService.csproj" --urls http://localhost:7001'
    },
    @{
        Name = "trade-service"
        WorkingDirectory = "apps/services/trade-service"
        ProbePath = "apps/services/trade-service/src/TradeService/TradeService.csproj"
        Command = 'dotnet watch run --project "src/TradeService/TradeService.csproj" --urls http://localhost:7002'
    },
    @{
        Name = "settlement-service"
        WorkingDirectory = "apps/services/settlement-service"
        ProbePath = "apps/services/settlement-service/src/SettlementService/SettlementService.csproj"
        Command = 'dotnet watch run --project "src/SettlementService/SettlementService.csproj" --urls http://localhost:7003'
    },
    @{
        Name = "frontend-spa"
        WorkingDirectory = "apps/frontend-spa"
        ProbePath = "apps/frontend-spa/package.json"
        Command = 'npm run dev -- --host 0.0.0.0 --port 5173'
    }
)

function Start-Dependencies {
    Write-Host "Starting local dependency containers (SQL Server, Azurite)..." -ForegroundColor Cyan
    docker compose up -d sqlserver azurite
}

function Start-ContainerizedApps {
    Write-Host "Starting containerized app profile..." -ForegroundColor Cyan
    docker compose --profile apps up -d frontend-spa market-service trade-service settlement-service
}

function Start-SourceApp {
    param(
        [hashtable]$Definition
    )

    $probe = Join-Path $repoRoot $Definition.ProbePath
    if (-not (Test-Path $probe)) {
        Write-Warning "Skipping $($Definition.Name): missing $($Definition.ProbePath)"
        return
    }

    $workingDirectory = Join-Path $repoRoot $Definition.WorkingDirectory
    $command = "Set-Location `"$workingDirectory`"; $($Definition.Command)"

    Write-Host "Launching $($Definition.Name) in a new PowerShell window..." -ForegroundColor Green
    Start-Process -FilePath "powershell.exe" -ArgumentList @(
        "-NoExit",
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-Command", $command
    ) | Out-Null
}

if (-not $SkipDependencies) {
    Start-Dependencies
}

if ($DependenciesOnly) {
    Write-Host "Dependencies started. Application launch skipped by request." -ForegroundColor Yellow
    return
}

if ($IncludeContainerizedApps) {
    Start-ContainerizedApps
    return
}

foreach ($serviceDefinition in $serviceDefinitions) {
    Start-SourceApp -Definition $serviceDefinition
}

Write-Host ""
Write-Host "Expected local endpoints:" -ForegroundColor Cyan
Write-Host "  Frontend:        http://localhost:5173"
Write-Host "  Market API/Hub:  http://localhost:7001"
Write-Host "  Trade API:       http://localhost:7002"
Write-Host "  Settlement API:  http://localhost:7003"
Write-Host ""
Write-Host "Tip: use -DependenciesOnly to start shared containers without opening app terminals." -ForegroundColor DarkGray
