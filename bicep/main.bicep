// Azure infrastructure for the migrated MovieFunctionApp.
// Provisions: Storage account, Log Analytics workspace, Application Insights,
// Linux consumption plan, and the Function App (.NET 10 isolated).
// All names are derived from a shared name prefix to keep the deployment
// idempotent and easy to identify per environment.

@description('Short name prefix used for all resources (lowercase, 3-11 chars).')
@minLength(3)
@maxLength(11)
param namePrefix string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Runtime stack version for the Function App worker.')
param netFrameworkVersion string = 'v10.0'

var uniqueSuffix = uniqueString(resourceGroup().id, namePrefix)
var storageAccountName = toLower('${namePrefix}st${uniqueSuffix}')
var logAnalyticsName = '${namePrefix}-law'
var appInsightsName = '${namePrefix}-ai'
var planName = '${namePrefix}-plan'
var functionAppName = '${namePrefix}-func-${uniqueSuffix}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(replace(storageAccountName, '-', ''), 24)
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
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

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    // Windows consumption plan (reserved=false). The .NET isolated worker
    // is fully supported on Windows and the function app below uses the
    // Windows-style `netFrameworkVersion` setting (Linux would require
    // `linuxFxVersion` instead).
    reserved: false
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: netFrameworkVersion
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
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
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
output appInsightsName string = appInsights.name
output storageAccountName string = storage.name
