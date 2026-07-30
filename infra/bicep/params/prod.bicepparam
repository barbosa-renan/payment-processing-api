using '../main.bicep'

param environment = 'prod'
param location = 'brazilsouth'
param resourceGroupName = 'rg-payment-processing-prod'

param names = {
  logAnalyticsWorkspace: 'log-payment-prod'
  appInsights: 'appi-payment-prod'
  keyVault: 'kv-payment-prod-8530'
  serviceBusNamespace: 'sb-payment-prod-8530'
  eventGridTopic: 'evgt-payment-prod'
  storageAccount: 'stpayprod8530'
  appServicePlan: 'asp-payment-prod'
  webApp: 'payment-api-prod-8530'
  functionApp: 'func-payment-prod-8530'
  sqlServer: 'sql-payment-prod-8530'
  sqlDatabase: 'PaymentDB'
}

param serviceBusSku = 'Premium'
param appServicePlanSku = 'P1v3'
param logRetentionInDays = 90
param sqlAdministratorLogin = 'paymentadmin'
param sqlDatabaseSkuName = 'S1'
param sqlPublicNetworkAccess = 'Enabled'
