<#
.SYNOPSIS
    Deploys the Movie Function App Azure resources via Bicep.

.DESCRIPTION
    Creates the resource group (if it does not exist) and runs an
    `az deployment group create` against bicep/main.bicep using inline
    parameter values only. No parameter file is required.

.EXAMPLE
    ./deploy.ps1
    ./deploy.ps1 -ResourceGroupName my-rg -Location eastus -AppName mymovieapp
#>
param(
    [string]$ResourceGroupName = 'rg-movieapp',
    [string]$Location          = 'australiaeast',
    [string]$AppName           = 'movieapp',
    [string]$DeploymentName    = "movieapp-$((Get-Date).ToString('yyyyMMddHHmmss'))"
)

$ErrorActionPreference = 'Stop'

$bicepFile = Join-Path $PSScriptRoot 'bicep/main.bicep'

Write-Host "Ensuring resource group '$ResourceGroupName' in '$Location'..."
az group create --name $ResourceGroupName --location $Location --output none

Write-Host "Deploying $bicepFile to resource group '$ResourceGroupName'..."
az deployment group create `
    --name $DeploymentName `
    --resource-group $ResourceGroupName `
    --template-file $bicepFile `
    --parameters appName=$AppName location=$Location `
    --output table

Write-Host "Deployment '$DeploymentName' completed."
