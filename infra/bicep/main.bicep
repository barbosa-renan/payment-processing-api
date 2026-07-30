targetScope = 'subscription'

@description('Nome do ambiente: dev, hml ou prod')
param environment string

@description('Localizacao padrao dos recursos')
param location string = 'brazilsouth'

@description('Nome do Resource Group')
param resourceGroupName string

@description('Nomes base dos recursos')
param names object

@description('SKU do Service Bus: Standard ou Premium')
param serviceBusSku string = 'Standard'

@description('SKU do plano App Service')
param appServicePlanSku string = 'B1'

@description('Retencao de logs em dias')
param logRetentionInDays int = 30

@description('Usuario administrador do SQL Server')
param sqlAdministratorLogin string

@secure()
@description('Senha do administrador do SQL Server')
param sqlAdministratorPassword string

@description('SKU do banco SQL, ex.: Basic, S0, S1')
param sqlDatabaseSkuName string = 'Basic'

@description('Permite acesso publico no SQL Server')
@allowed([
  'Enabled'
  'Disabled'
])
param sqlPublicNetworkAccess string = 'Enabled'

var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6'

module rg './modules/resource-group.bicep' = {
  name: 'rg-${environment}'
  scope: subscription()
  params: {
    name: resourceGroupName
    location: location
  }
}

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    workspaceName: names.logAnalyticsWorkspace
    appInsightsName: names.appInsights
    location: location
    retentionInDays: logRetentionInDays
  }
  dependsOn: [
    rg
  ]
}

module keyvault './modules/keyvault.bicep' = {
  name: 'keyvault-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    keyVaultName: names.keyVault
    location: location
    enablePurgeProtection: environment == 'prod'
  }
  dependsOn: [
    rg
  ]
}

module servicebus './modules/servicebus.bicep' = {
  name: 'servicebus-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    namespaceName: names.serviceBusNamespace
    location: location
    sku: serviceBusSku
    queues: [
      'payment-processed'
      'payment-failed'
      'notifications'
      'refund-requests'
      'high-value-approval'
    ]
  }
  dependsOn: [
    rg
  ]
}

module eventgrid './modules/eventgrid.bicep' = {
  name: 'eventgrid-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    topicName: names.eventGridTopic
    location: location
  }
  dependsOn: [
    rg
  ]
}

module storage './modules/storage.bicep' = {
  name: 'storage-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    storageAccountName: names.storageAccount
    location: location
    skuName: 'Standard_LRS'
  }
  dependsOn: [
    rg
  ]
}

module appPlan './modules/appservice-plan.bicep' = {
  name: 'appplan-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    appServicePlanName: names.appServicePlan
    location: location
    skuName: appServicePlanSku
  }
  dependsOn: [
    rg
  ]
}

module webapp './modules/webapp.bicep' = {
  name: 'webapp-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    webAppName: names.webApp
    location: location
    serverFarmId: appPlan.outputs.appServicePlanId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultUri: keyvault.outputs.keyVaultUri
  }
}

module functionapp './modules/functionapp.bicep' = {
  name: 'functionapp-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    functionAppName: names.functionApp
    location: location
    serverFarmId: appPlan.outputs.appServicePlanId
    storageAccountName: names.storageAccount
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

module sql './modules/sql.bicep' = {
  name: 'sql-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    sqlServerName: names.sqlServer
    databaseName: names.sqlDatabase
    location: location
    administratorLogin: sqlAdministratorLogin
    administratorPassword: sqlAdministratorPassword
    databaseSkuName: sqlDatabaseSkuName
    publicNetworkAccess: sqlPublicNetworkAccess
  }
  dependsOn: [
    rg
  ]
}

module webAppKvSecretsUser './modules/role-assignment.bicep' = {
  name: 'webapp-kv-secrets-user-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    keyVaultName: names.keyVault
    principalId: webapp.outputs.webAppPrincipalId
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
    assignmentNameSeed: 'webapp-kv-secrets-user'
  }
}

module functionAppKvSecretsUser './modules/role-assignment.bicep' = {
  name: 'functionapp-kv-secrets-user-${environment}'
  scope: resourceGroup(resourceGroupName)
  params: {
    keyVaultName: names.keyVault
    principalId: functionapp.outputs.functionAppPrincipalId
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
    assignmentNameSeed: 'functionapp-kv-secrets-user'
  }
}

output resourceGroup string = resourceGroupName
output keyVaultUri string = keyvault.outputs.keyVaultUri
output serviceBusNamespace string = servicebus.outputs.serviceBusNamespaceName
output eventGridTopicEndpoint string = eventgrid.outputs.topicEndpoint
output webAppUrl string = 'https://${webapp.outputs.defaultHostName}'
output functionAppUrl string = 'https://${functionapp.outputs.defaultHostName}'
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
