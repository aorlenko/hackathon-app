[CmdletBinding()]
param(
    [string]$EnvironmentName = "dev",
    [string]$Location = "eastus",
    [string]$ResourceGroupName = "rg-trading-hackathon-dev",
    [string]$TemplateFile = "infra/bicep/main.bicep",
    [string]$ParametersFile = "infra/environments/hackathon/parameters.dev.json",
    [string]$SqlAdminLogin = $env:SQL_ADMIN_LOGIN,
    [string]$SqlAdminPassword = $env:SQL_ADMIN_PASSWORD,
    [string]$Auth0Domain = $env:AUTH0_DOMAIN,
    [string]$Auth0Audience = $env:AUTH0_AUDIENCE,
    [string]$Auth0ClientId = $env:AUTH0_CLIENT_ID,
    [string]$AlertEmailAddress = $env:ALERT_EMAIL_ADDRESS,
    [string]$FrontendImage = "",
    [string]$MarketServiceImage = "",
    [string]$TradeServiceImage = "",
    [string]$SettlementServiceImage = "",
    [switch]$WhatIf,
    [switch]$CreateResourceGroup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI ('az') is required to deploy the hackathon environment."
}

if ($CreateResourceGroup) {
    Write-Host "Ensuring resource group $ResourceGroupName exists in $Location..." -ForegroundColor Cyan
    az group create --name $ResourceGroupName --location $Location | Out-Null
}

$deploymentName = "hackathon-$EnvironmentName-$(Get-Date -Format 'yyyyMMddHHmmss')"
$parameterArguments = @(
    "@$ParametersFile",
    "environmentName=$EnvironmentName"
)

if ($SqlAdminLogin) {
    $parameterArguments += "sqlAdminLogin=$SqlAdminLogin"
}

if ($SqlAdminPassword) {
    $parameterArguments += "sqlAdminPassword=$SqlAdminPassword"
}

if ($Auth0Domain) {
    $parameterArguments += "auth0Domain=$Auth0Domain"
}

if ($Auth0Audience) {
    $parameterArguments += "auth0Audience=$Auth0Audience"
}

if ($Auth0ClientId) {
    $parameterArguments += "auth0ClientId=$Auth0ClientId"
}

if ($AlertEmailAddress) {
    $parameterArguments += "alertEmailAddress=$AlertEmailAddress"
}

if ($FrontendImage) {
    $parameterArguments += "frontendImage=$FrontendImage"
}

if ($MarketServiceImage) {
    $parameterArguments += "marketServiceImage=$MarketServiceImage"
}

if ($TradeServiceImage) {
    $parameterArguments += "tradeServiceImage=$TradeServiceImage"
}

if ($SettlementServiceImage) {
    $parameterArguments += "settlementServiceImage=$SettlementServiceImage"
}

$operation = if ($WhatIf) { "what-if" } else { "create" }
$baseArgs = @(
    "deployment", "group",
    $operation,
    "--resource-group", $ResourceGroupName,
    "--name", $deploymentName,
    "--template-file", $TemplateFile,
    "--parameters"
) + $parameterArguments

Write-Host "Running az deployment group $operation for $ResourceGroupName using $TemplateFile..." -ForegroundColor Cyan
& az @baseArgs

if ($LASTEXITCODE -ne 0) {
    throw "Deployment command failed."
}
