// ---------------------------------------------------------------------------
// MovieFunctionApp - Azure infrastructure
// Provisions: Storage, Log Analytics, Application Insights, Linux Consumption
// plan, and a .NET 10 isolated Function App. No database resources are
// provisioned because the migrated app uses an in-memory EF Core store.
// ---------------------------------------------------------------------------

@description('Base name used to derive all resource names. Must be globally unique-ish (3-11 lowercase chars).')
@minLength(3)
@maxLength(11)
param appName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Tags applied to every resource.')
param tags object = {
  app: 'MovieFunctionApp'
  source: 'mulesoft-migration'
}

var suffix             = toLower(uniqueString(resourceGroup().id, appName))
var storageAccountName = toLower('st${appName}${suffix}')
var planName           = 'plan-${appName}-${suffix}'
var functionAppName    = 'func-${appName}-${suffix}'
var appInsightsName    = 'appi-${appName}-${suffix}'
var logAnalyticsName   = 'log-${appName}-${suffix}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take(storageAccountName, 24)
  location: location
  tags: tags
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

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
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
          name: 'OpenApi__HideSwaggerUI'
          value: 'false'
        }
        {
          name: 'OpenApi__Version'
          value: 'v3'
        }
      ]
    }
  }
}

output functionAppName string  = functionApp.name
output functionAppUrl  string  = 'https://${functionApp.properties.defaultHostName}'
output swaggerUrl      string  = 'https://${functionApp.properties.defaultHostName}/api/swagger/ui'
