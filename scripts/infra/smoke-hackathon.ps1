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
        [string]$Url
    )

    if (-not $Url) {
        throw "Missing required URL for $Name."
    }

    Write-Host "Checking $Name at $Url..." -ForegroundColor Cyan
    $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 30

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 400) {
        throw "$Name check failed with status code $($response.StatusCode)."
    }
}

Assert-Url -Name "frontend" -Url $FrontendUrl
Assert-Url -Name "market health" -Url ($MarketApiUrl.TrimEnd("/") + "/health")
Assert-Url -Name "trade health" -Url ($TradeApiUrl.TrimEnd("/") + "/health")
Assert-Url -Name "settlement health" -Url ($SettlementApiUrl.TrimEnd("/") + "/health")

Write-Host "Basic smoke checks passed." -ForegroundColor Green
Write-Warning "Lifecycle login -> order -> trade -> settlement automation is still pending app implementation (T057)."
