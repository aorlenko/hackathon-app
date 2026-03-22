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
    [string]$FrontendImage = "",
    [string]$MarketServiceImage = "",
    [string]$TradeServiceImage = "",
    [string]$SettlementServiceImage = "",
    [bool]$DeployApps = $true,
    [switch]$WhatIf,
    [switch]$CreateResourceGroup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "[tooling] Azure CLI ('az') is required. Install it and ensure 'az' is on PATH, then re-run."
}

# Bicep requires sqlAdminPassword; parameters file does not carry the secret.
if (-not $SqlAdminPassword) {
    throw "[secret/config] SQL admin password is missing. Set environment variable SQL_ADMIN_PASSWORD (GitHub: secret SQL_ADMIN_PASSWORD on environment 'hackathon')."
}

if (-not $SqlAdminLogin) {
    Write-Warning "[secret/config] SQL_ADMIN_LOGIN is empty; Bicep default or parameters file value will be used. For CI, set variable SQL_ADMIN_LOGIN on environment 'hackathon'."
}

function Write-DeploymentFailureHints {
    Write-Host ""
    Write-Host "Deployment command exited non-zero. Quick mapping (see quickstart.md Common failures):" -ForegroundColor Yellow
    Write-Host "  [identity]   AADSTS*, federated credential subject, wrong tenant/client" -ForegroundColor DarkYellow
    Write-Host "  [secret]     missing/wrong SQL or Key Vault-related auth" -ForegroundColor DarkYellow
    Write-Host "  [RBAC]       Authorization failed / 403 on subscription or resource group" -ForegroundColor DarkYellow
    Write-Host "  [quota]      quota exceeded, region capacity" -ForegroundColor DarkYellow
    Write-Host "  [parameter]  invalid parameter file path, wrong Bicep parameter names" -ForegroundColor DarkYellow
    Write-Host "  [ARM policy] RequestDisallowedByPolicy, policy violation messages" -ForegroundColor DarkYellow
}

if ($CreateResourceGroup) {
    Write-Host "Ensuring resource group $ResourceGroupName exists in $Location..." -ForegroundColor Cyan
    az group create --name $ResourceGroupName --location $Location | Out-Null
}

$deploymentName = "hackathon-$EnvironmentName-$(Get-Date -Format 'yyyyMMddHHmmss')"
$parameterArguments = @(
    "@$ParametersFile",
    "environmentName=$EnvironmentName",
    "location=$Location",
    "deployApps=$($DeployApps.ToString().ToLowerInvariant())"
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
$prevEap = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & az @baseArgs
}
finally {
    $ErrorActionPreference = $prevEap
}

if ($LASTEXITCODE -ne 0) {
    Write-DeploymentFailureHints
    throw "[ARM] Deployment command failed (exit code $LASTEXITCODE). Inspect Azure CLI output above."
}
