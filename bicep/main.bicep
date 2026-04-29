@description('Base name used to generate names for all resources.')
param appName string = 'movieapp'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('SKU for the App Service plan that hosts the Function App.')
param functionPlanSku string = 'Y1'

@description('Tier for the App Service plan that hosts the Function App.')
param functionPlanTier string = 'Dynamic'

@description('Runtime stack for the Function App worker.')
param functionsWorkerRuntime string = 'dotnet-isolated'

var uniqueSuffix = uniqueString(resourceGroup().id, appName)
var storageAccountName = toLower(substring('${replace(appName, '-', '')}st${uniqueSuffix}', 0, 24))
var functionAppName = '${appName}-func-${uniqueSuffix}'
var hostingPlanName = '${appName}-plan-${uniqueSuffix}'
var appInsightsName = '${appName}-ai-${uniqueSuffix}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: functionPlanSku
    tier: functionPlanTier
  }
  properties: {
    reserved: true
  }
  kind: 'linux'
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: functionsWorkerRuntime
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output storageAccountName string = storageAccount.name
output appInsightsName string = appInsights.name
