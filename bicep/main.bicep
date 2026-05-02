// ----------------------------------------------------------------------------
// Bicep template for the migrated MovieFunctionApp (.NET 10 isolated worker).
//
// Provisions:
//   * Storage account (required by the Functions runtime)
//   * Log Analytics workspace + Application Insights
//   * Linux Consumption (Y1) hosting plan
//   * Function App (Linux, dotnet-isolated, .NET 10) with system-assigned MI
// ----------------------------------------------------------------------------

@description('Base name used to derive resource names. 3-11 chars, lowercase.')
@minLength(3)
@maxLength(11)
param baseName string = 'movieapp'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Function runtime worker. Keep dotnet-isolated for .NET 10.')
param functionsWorkerRuntime string = 'dotnet-isolated'

@description('Functions extension version.')
param functionsExtensionVersion string = '~4'

@description('Linux FX version (runtime stack) for the Function App.')
param linuxFxVersion string = 'DOTNET-ISOLATED|10.0'

var uniqueSuffix = uniqueString(resourceGroup().id, baseName)
var storageAccountName = toLower('st${baseName}${take(uniqueSuffix, 6)}')
var hostingPlanName = '${baseName}-plan'
var functionAppName = '${baseName}-${take(uniqueSuffix, 6)}'
var appInsightsName = '${baseName}-ai'
var logAnalyticsName = '${baseName}-law'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
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
      linuxFxVersion: linuxFxVersion
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: functionsExtensionVersion
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: functionsWorkerRuntime
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output storageAccountName string = storage.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
