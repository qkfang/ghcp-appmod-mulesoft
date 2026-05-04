// =============================================================================
//  Movie Function App — Azure infrastructure
// -----------------------------------------------------------------------------
//  Provisions a minimal Azure Functions environment for the .NET 10 isolated
//  worker app migrated from the original MuleSoft project under `mulesoft/`.
//
//  Resources:
//    - Storage Account (required by the Functions runtime)
//    - Log Analytics Workspace + Application Insights
//    - App Service Plan (Linux, Consumption Y1 by default)
//    - Function App (linuxFxVersion = DOTNET-ISOLATED|10.0)
// =============================================================================

@description('Base name used to derive all resource names. Keep short (3-12 chars).')
@minLength(3)
@maxLength(12)
param appName string = 'movieapp'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Functions hosting plan SKU. Y1 = Consumption.')
param planSku string = 'Y1'

@description('.NET isolated worker version, used as the Functions linuxFxVersion.')
param dotnetVersion string = '10.0'

@description('Tags applied to every resource.')
param tags object = {
  workload: 'movie-function-app'
  source: 'mulesoft-migration'
}

var uniqueSuffix = uniqueString(resourceGroup().id, appName)
var storageAccountName = toLower('${appName}${uniqueSuffix}')
var hostingPlanName = '${appName}-plan'
var functionAppName = '${appName}-${uniqueSuffix}'
var appInsightsName = '${appName}-ai'
var logAnalyticsName = '${appName}-logs'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(replace(storageAccountName, '-', ''), 24)
  location: location
  tags: tags
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
  tags: tags
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
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  sku: {
    name: planSku
    tier: planSku == 'Y1' ? 'Dynamic' : 'ElasticPremium'
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
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|${dotnetVersion}'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
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

@description('Default hostname of the deployed Function App.')
output functionAppHostName string = functionApp.properties.defaultHostName

@description('Name of the deployed Function App resource.')
output functionAppName string = functionApp.name

@description('Application Insights connection string for the Function App.')
output appInsightsConnectionString string = appInsights.properties.ConnectionString
