# Minimal Bicep deployment script for MovieFunctionApp.
# Runs `az deployment group create` with all variables inline.
# Usage:
#   ./deploy.ps1 -ResourceGroup my-rg [-Location australiaeast] [-NamePrefix movieapp]
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [string]$Location = 'australiaeast',

    [ValidateLength(3, 11)]
    [string]$NamePrefix = 'movieapp'
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$templateFile = Join-Path $scriptRoot 'main.bicep'

Write-Host "Ensuring resource group '$ResourceGroup' exists in '$Location'..."
az group create --name $ResourceGroup --location $Location --output none

Write-Host "Deploying Bicep template '$templateFile'..."
az deployment group create `
    --resource-group $ResourceGroup `
    --name "moviefunctionapp-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))" `
    --template-file $templateFile `
    --parameters namePrefix=$NamePrefix location=$Location `
    --output table
