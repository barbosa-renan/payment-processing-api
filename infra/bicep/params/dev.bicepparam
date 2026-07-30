using '../main.bicep'

param environment = 'dev'
param location = 'brazilsouth'
param resourceGroupName = 'rg-payment-processing-dev'

param names = {
  logAnalyticsWorkspace: 'log-payment-dev'
  appInsights: 'appi-payment-dev'
  keyVault: 'kv-payment-dev-8530'
  serviceBusNamespace: 'sb-payment-dev-8530'
  eventGridTopic: 'evgt-payment-dev'
  storageAccount: 'stpaydev8530'
  appServicePlan: 'asp-payment-dev'
  webApp: 'payment-api-dev-8530'
  functionApp: 'func-payment-dev-8530'
  sqlServer: 'sql-payment-dev-8530'
  sqlDatabase: 'PaymentDB'
}

param serviceBusSku = 'Standard'
param appServicePlanSku = 'B1'
param logRetentionInDays = 30
param sqlAdministratorLogin = 'paymentadmin'
param sqlDatabaseSkuName = 'Basic'
param sqlPublicNetworkAccess = 'Enabled'
