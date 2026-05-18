// =============================================================================
// MovieFunctionApp - Azure infrastructure
// Provisions a Linux Consumption Function App (.NET 10 isolated worker) with
// the supporting Storage account and Application Insights workspace.
// =============================================================================

@description('Short name used to derive resource names. Lowercase, 3-11 chars.')
@minLength(3)
@maxLength(11)
param appName string = 'movieapi'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Suffix appended to resource names for uniqueness.')
param nameSuffix string = uniqueString(resourceGroup().id)

@description('Runtime stack version exposed to the worker.')
param netFrameworkVersion string = 'v10.0'

var storageAccountName = toLower('${appName}st${nameSuffix}')
var hostingPlanName     = '${appName}-plan-${nameSuffix}'
var functionAppName     = '${appName}-func-${nameSuffix}'
var appInsightsName     = '${appName}-ai-${nameSuffix}'
var logWorkspaceName    = '${appName}-log-${nameSuffix}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(storageAccountName, 24)
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource logWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logWorkspaceName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logWorkspace.id
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true // Linux
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
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      netFrameworkVersion: netFrameworkVersion
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
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

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output storageAccountName string = storage.name
output appInsightsName string = appInsights.name
