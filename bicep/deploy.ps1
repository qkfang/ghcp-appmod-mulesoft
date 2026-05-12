# ---------------------------------------------------------------------------
# Minimal deploy script for MovieFunctionApp infrastructure.
# Usage:
#   ./deploy.ps1                                  # uses defaults below
#   ./deploy.ps1 -ResourceGroup my-rg -Location eastus -AppName movies
# Requires: Azure CLI (az) and an active 'az login' session.
# All variables are passed inline to `az deployment group create`.
# ---------------------------------------------------------------------------

param(
    [string]$ResourceGroup = "rg-moviefunc",
    [string]$Location      = "australiaeast",
    [string]$AppName       = "movieapp"
)

$ErrorActionPreference = "Stop"

Write-Host "Ensuring resource group '$ResourceGroup' in '$Location'..." -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --output none

Write-Host "Deploying main.bicep..." -ForegroundColor Cyan
az deployment group create `
    --resource-group $ResourceGroup `
    --name "moviefunc-$(Get-Date -Format yyyyMMddHHmmss)" `
    --template-file "$PSScriptRoot/main.bicep" `
    --parameters appName=$AppName location=$Location `
    --output table

Write-Host "Deployment complete." -ForegroundColor Green
Write-Host "To publish the function code, run from src/MovieFunctionApp:" -ForegroundColor Yellow
Write-Host "  func azure functionapp publish <functionAppName>" -ForegroundColor Yellow
