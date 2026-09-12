#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Minimal deployment script for the Book My Show Movie Function App Azure resources.
.DESCRIPTION
    Creates the resource group (if needed) and deploys bicep/main.bicep using
    `az deployment group create` with inline parameters. Edit the variables below
    to change the target subscription/resource group/region.
#>

# ---- Inline deployment variables ----
$resourceGroupName = "rg-moviefunctionapp"
$location = "eastus"
$appName = "moviefunctionapp"
$appServicePlanSku = "B1"

$ErrorActionPreference = "Stop"

az group create `
  --name $resourceGroupName `
  --location $location

az deployment group create `
  --resource-group $resourceGroupName `
  --template-file "$PSScriptRoot/main.bicep" `
  --parameters appName=$appName location=$location appServicePlanSku=$appServicePlanSku
