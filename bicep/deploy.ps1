<#
.SYNOPSIS
    Deploys the Movie Function App Azure infrastructure with all variables inline.

.DESCRIPTION
    Minimal `az deployment group create` wrapper for `bicep/main.bicep`.
    Edit the parameter values below or override them on the command line.

.EXAMPLE
    ./deploy.ps1
    ./deploy.ps1 -ResourceGroup rg-movie-prod -Location westus2 -AppName moviedemo
#>
param(
    [string]$SubscriptionId = '',
    [string]$ResourceGroup  = 'rg-movie-functionapp',
    [string]$Location       = 'eastus',
    [string]$AppName        = 'movieapp'
)

$ErrorActionPreference = 'Stop'

if ($SubscriptionId) {
    az account set --subscription $SubscriptionId
}

az group create `
    --name  $ResourceGroup `
    --location $Location `
    --output none

$templateFile = Join-Path $PSScriptRoot 'main.bicep'

az deployment group create `
    --resource-group $ResourceGroup `
    --template-file  $templateFile `
    --parameters     appName=$AppName location=$Location `
    --output         table
