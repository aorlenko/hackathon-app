[CmdletBinding()]
param(
    [string]$FrontendUrl = $env:FRONTEND_URL,
    [string]$MarketApiUrl = $env:MARKET_API_URL,
    [string]$TradeApiUrl = $env:TRADE_API_URL,
    [string]$SettlementApiUrl = $env:SETTLEMENT_API_URL
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Url {
    param(
        [string]$Name,
        [string]$Url,
        [string]$EnvVarHint
    )

    if (-not $Url) {
        throw "[parameter] Missing URL for $Name. Set environment variable $EnvVarHint (workflow passes this from 'Resolve container app endpoints'). See contracts/deployment-pipeline-contract.md section F."
    }

    Write-Host "Checking $Name at $Url..." -ForegroundColor Cyan
    $iwr = Get-Command Invoke-WebRequest
    $canSkipHttp = $iwr.Parameters.ContainsKey('SkipHttpErrorCheck')
    try {
        if ($canSkipHttp) {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 30 -SkipHttpErrorCheck
        }
        else {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 30
        }
    }
    catch {
        throw "[smoke] Request failed for $Name at $Url : $($_.Exception.Message). Confirm deploy finished and ingress FQDNs resolve."
    }

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 400) {
        throw "[smoke] $Name returned HTTP $($response.StatusCode) (expected 2xx). Check app logs and Container Apps health/probes."
    }
}

Assert-Url -Name "frontend" -Url $FrontendUrl -EnvVarHint "FRONTEND_URL"
Assert-Url -Name "market health" -Url ($MarketApiUrl.TrimEnd("/") + "/health") -EnvVarHint "MARKET_API_URL"
Assert-Url -Name "trade health" -Url ($TradeApiUrl.TrimEnd("/") + "/health") -EnvVarHint "TRADE_API_URL"
Assert-Url -Name "settlement health" -Url ($SettlementApiUrl.TrimEnd("/") + "/health") -EnvVarHint "SETTLEMENT_API_URL"

Write-Host "Basic smoke checks passed." -ForegroundColor Green
Write-Warning "Lifecycle login -> order -> trade -> settlement automation is still pending app implementation (T057)."
