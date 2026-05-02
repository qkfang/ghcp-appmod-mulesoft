#requires -Version 7.0
<#
.SYNOPSIS
    Minimal Bicep deployment for the MovieFunctionApp.
.DESCRIPTION
    Creates the resource group (if needed) and runs `az deployment group create`
    against bicep/main.bicep with all parameters supplied inline. Intended as a
    quick one-shot deployment for demos.
.NOTES
    Requires the Azure CLI (`az`) signed in to the target subscription.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $SubscriptionId,
    [string] $ResourceGroup  = 'rg-movie-appmod',
    [string] $Location       = 'australiaeast',
    [string] $BaseName       = 'movieapp'
)

$ErrorActionPreference = 'Stop'

Write-Host "Setting subscription to $SubscriptionId..." -ForegroundColor Cyan
az account set --subscription $SubscriptionId | Out-Null

Write-Host "Ensuring resource group '$ResourceGroup' in '$Location'..." -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location | Out-Null

$bicepPath = Join-Path $PSScriptRoot 'main.bicep'

Write-Host "Deploying $bicepPath ..." -ForegroundColor Cyan
az deployment group create `
    --resource-group $ResourceGroup `
    --template-file $bicepPath `
    --parameters baseName=$BaseName location=$Location `
    --output table

Write-Host "Done." -ForegroundColor Green
