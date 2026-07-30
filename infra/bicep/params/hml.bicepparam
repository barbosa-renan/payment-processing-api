using '../main.bicep'

param environment = 'hml'
param location = 'brazilsouth'
param resourceGroupName = 'rg-payment-processing-hml'

param names = {
  logAnalyticsWorkspace: 'log-payment-hml'
  appInsights: 'appi-payment-hml'
  keyVault: 'kv-payment-hml-8530'
  serviceBusNamespace: 'sb-payment-hml-8530'
  eventGridTopic: 'evgt-payment-hml'
  storageAccount: 'stpayhml8530'
  appServicePlan: 'asp-payment-hml'
  webApp: 'payment-api-hml-8530'
  functionApp: 'func-payment-hml-8530'
  sqlServer: 'sql-payment-hml-8530'
  sqlDatabase: 'PaymentDB'
}

param serviceBusSku = 'Standard'
param appServicePlanSku = 'B1'
param logRetentionInDays = 30
param sqlAdministratorLogin = 'paymentadmin'
param sqlDatabaseSkuName = 'S0'
param sqlPublicNetworkAccess = 'Enabled'
