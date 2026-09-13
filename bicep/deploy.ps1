<#
.SYNOPSIS
    Minimal deployment script for the MovieFunctionApp Azure infrastructure.

.DESCRIPTION
    Creates (if needed) a resource group and deploys bicep/main.bicep using
    `az deployment group create` with inline parameters. Edit the variables
    below or pass them as script parameters.

.EXAMPLE
    ./deploy.ps1 -ResourceGroupName "rg-movieapp" -Location "eastus" -AppName "moviefnapp01"
#>

param(
    [string]$ResourceGroupName = "rg-movieapp",
    [string]$Location = "eastus",
    [string]$AppName = "moviefnapp01"
)

az group create `
    --name $ResourceGroupName `
    --location $Location

az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "$PSScriptRoot/main.bicep" `
    --parameters appName=$AppName location=$Location
