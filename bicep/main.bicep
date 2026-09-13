@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Base name used to derive resource names (must be globally-unique for the storage account and function app).')
@minLength(3)
@maxLength(17)
param appName string

@description('Name of the storage account backing the Function App. Must be globally unique, lowercase, 3-24 chars.')
param storageAccountName string = toLower('${replace(appName, '-', '')}sa')

@description('SKU for the Linux App Service Plan hosting the Function App. .NET 10 isolated is not supported on the Consumption (Y1) plan.')
param appServicePlanSkuName string = 'B1'

@description('Tier for the App Service Plan SKU.')
param appServicePlanSkuTier string = 'Basic'

var functionAppName = '${appName}-func'
var appServicePlanName = '${appName}-plan'
var appInsightsName = '${appName}-appi'

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
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSkuName
    tier: appServicePlanSkuTier
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
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

@description('The default hostname of the deployed Function App.')
output functionAppHostName string = functionApp.properties.defaultHostName

@description('The name of the deployed Function App.')
output functionAppName string = functionApp.name
