az group create --name rg-bookmyshow --location eastus

az deployment group create --resource-group rg-bookmyshow --template-file "main.bicep" --parameters "main.parameters.json"
