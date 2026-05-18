<#
.SYNOPSIS
    Minimal Bicep deployment for the MovieFunctionApp infrastructure.

.DESCRIPTION
    Creates the target resource group (if missing) and deploys main.bicep with
    all parameters supplied inline via `az deployment group create`.

.EXAMPLE
    ./deploy.ps1
    ./deploy.ps1 -ResourceGroup my-rg -Location eastus -AppName movieapi
#>
param(
    [string]$ResourceGroup = 'rg-moviefunctionapp',
    [string]$Location      = 'australiaeast',
    [string]$AppName       = 'movieapi'
)

$ErrorActionPreference = 'Stop'

Write-Host "Ensuring resource group '$ResourceGroup' in '$Location'..." -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --output none

$deploymentName = "moviefunc-$(Get-Date -Format 'yyyyMMddHHmmss')"
$bicepFile      = Join-Path $PSScriptRoot 'main.bicep'

Write-Host "Deploying $bicepFile (deployment: $deploymentName)..." -ForegroundColor Cyan
az deployment group create `
    --resource-group $ResourceGroup `
    --name $deploymentName `
    --template-file $bicepFile `
    --parameters appName=$AppName location=$Location `
    --output table

Write-Host "Deployment '$deploymentName' completed." -ForegroundColor Green
