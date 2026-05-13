# Minimal deploy script for the MovieFunctionApp infrastructure.
# All deployment parameters are passed inline (no parameters file required).

$ResourceGroup = 'rg-movieapp'
$Location      = 'eastus'
$AppName       = 'movieapp'

az group create --name $ResourceGroup --location $Location

az deployment group create `
    --resource-group $ResourceGroup `
    --template-file "$PSScriptRoot/main.bicep" `
    --parameters appName=$AppName location=$Location functionPlanSku=Y1 functionPlanTier=Dynamic functionsWorkerRuntime=dotnet-isolated
