// Minimal Azure resources for the Book My Show movie ticket booking API
// (migrated from the Mulesoft app under /mulesoft into src/MovieFunctionApp).
//
// Resources:
//   - Storage account (required by Azure Functions)
//   - Application Insights (monitoring)
//   - App Service plan (Linux, Dedicated/Basic - supports the .NET 10 isolated worker)
//   - Function App (Linux, .NET 10 isolated) with an in-memory database, so no
//     database resource is provisioned.
@description('Base name used to derive the names of all resources.')
param appName string = 'moviefunctionapp'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('SKU name for the Linux App Service plan hosting the function app.')
param appServicePlanSku string = 'B1'

@description('Tags applied to all resources.')
param tags object = {
  application: 'book-my-show'
  source: 'mulesoft-migration'
}

var resourceToken = uniqueString(resourceGroup().id, appName)
var storageAccountName = toLower('st${take(replace(appName, '-', ''), 11)}${take(resourceToken, 8)}')
var hostingPlanName = '${appName}-plan'
var applicationInsightsName = '${appName}-appi'
var functionAppName = '${appName}-${resourceToken}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  sku: {
    tier: 'Basic'
    name: appServicePlanSku
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  properties: {
    reserved: true
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
  }
}

@description('The default hostname of the deployed function app.')
output functionAppName string = functionApp.name
@description('The base URL of the deployed API, e.g. for GET {apiBaseUrl}/movies.')
output apiBaseUrl string = 'https://${functionApp.properties.defaultHostName}/api'
