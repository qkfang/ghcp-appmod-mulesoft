@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Base name used to derive all resource names.')
param appName string = 'bookmyshow'

@description('MySQL administrator login name.')
param mysqlAdminLogin string

@description('MySQL administrator password.')
@secure()
param mysqlAdminPassword string

@description('MySQL database name.')
param mysqlDatabaseName string = 'bookmyshow'

// ── Derived names ────────────────────────────────────────────────────────────
var suffix            = uniqueString(resourceGroup().id)
var storageAccountName = 'bms${suffix}'
var hostingPlanName   = '${appName}-plan-${suffix}'
var functionAppName   = '${appName}-func-${suffix}'
var mysqlServerName   = '${appName}-mysql-${suffix}'
var keyVaultName      = 'bms-kv-${suffix}'

// ── Storage Account (required by Function App) ───────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'Storage'
  tags: {
    SecurityControl: 'Ignore'
  }
}

// ── Consumption Hosting Plan (serverless) ────────────────────────────────────
resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true   // required for Linux
  }
}

// ── MySQL Flexible Server ─────────────────────────────────────────────────────
resource mysqlServer 'Microsoft.DBforMySQL/flexibleServers@2023-06-30' = {
  name: mysqlServerName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: mysqlAdminLogin
    administratorLoginPassword: mysqlAdminPassword
    storage: { storageSizeGB: 20 }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    version: '8.0.21'
  }
}

resource mysqlDatabase 'Microsoft.DBforMySQL/flexibleServers/databases@2023-06-30' = {
  parent: mysqlServer
  name: mysqlDatabaseName
  properties: {
    charset: 'utf8mb4'
    collation: 'utf8mb4_unicode_ci'
  }
}

// Allow all Azure-internal IPs (0.0.0.0) so the Function App can connect
resource mysqlFirewallAzure 'Microsoft.DBforMySQL/flexibleServers/firewallRules@2023-06-30' = {
  parent: mysqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── Key Vault (stores MySQL password securely) ────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
  }
}

resource kvSecretDbPassword 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'db-password'
  properties: {
    value: mysqlAdminPassword
  }
}

// ── Azure Function App (.NET 8 isolated) ─────────────────────────────────────
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
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
          name: 'DB_HOST'
          value: '${mysqlServerName}.mysql.database.azure.com'
        }
        {
          name: 'DB_PORT'
          value: '3306'
        }
        {
          name: 'DB_USER'
          value: mysqlAdminLogin
        }
        {
          // Key Vault reference – the password is never stored as plain text
          name: 'DB_PASSWORD'
          value: '@Microsoft.KeyVault(SecretUri=${kvSecretDbPassword.properties.secretUri})'
        }
        {
          name: 'DB_NAME'
          value: mysqlDatabaseName
        }
      ]
    }
    httpsOnly: true
  }
}

// Grant the Function App's managed identity the Key Vault Secrets User role
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionApp.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
output mysqlServerFqdn string = mysqlServer.properties.fullyQualifiedDomainName
output keyVaultName string = keyVault.name
