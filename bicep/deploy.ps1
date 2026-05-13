<#
.SYNOPSIS
    Deploys the Movie API Azure Functions infrastructure (Bicep) with all
    variables provided inline. No parameter files required.

.EXAMPLE
    ./deploy.ps1 -SubscriptionId 00000000-0000-0000-0000-000000000000 `
                 -ResourceGroup rg-movie-dev `
                 -Location australiaeast `
                 -BaseName movie
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $SubscriptionId,
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,
    [string] $Location = 'australiaeast',
    [string] $BaseName = 'movie',
    [string] $Sku = 'Y1'
)

$ErrorActionPreference = 'Stop'

Write-Host "Setting subscription $SubscriptionId" -ForegroundColor Cyan
az account set --subscription $SubscriptionId | Out-Null

Write-Host "Ensuring resource group $ResourceGroup in $Location" -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --output none

$deploymentName = "movie-api-$(Get-Date -Format 'yyyyMMddHHmmss')"
$bicepFile = Join-Path $PSScriptRoot 'main.bicep'

Write-Host "Deploying $bicepFile as $deploymentName" -ForegroundColor Cyan
az deployment group create `
    --resource-group $ResourceGroup `
    --name $deploymentName `
    --template-file $bicepFile `
    --parameters baseName=$BaseName location=$Location sku=$Sku `
    --output table

Write-Host "Deployment '$deploymentName' completed." -ForegroundColor Green
