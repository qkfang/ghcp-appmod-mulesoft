az group create --name rg-bookmyshow --location eastus

az deployment group create `
    --resource-group rg-bookmyshow `
    --template-file "$PSScriptRoot/main.bicep" `
    --parameters "@$PSScriptRoot/main.parameters.json"
