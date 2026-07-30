@description('Nome do App Service Plan')
param appServicePlanName string

@description('Localizacao do App Service Plan')
param location string

@description('SKU do plano, ex.: B1, P1v3')
param skuName string = 'B1'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: skuName
    tier: contains(['B1', 'B2', 'B3'], skuName) ? 'Basic' : 'PremiumV3'
  }
  properties: {
    reserved: true
  }
}

output appServicePlanId string = plan.id
output appServicePlanName string = plan.name
