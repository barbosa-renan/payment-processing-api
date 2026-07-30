@description('Nome da Web App')
param webAppName string

@description('Localizacao da Web App')
param location string

@description('ID do App Service Plan')
param serverFarmId string

@description('Runtime linux, ex.: DOTNETCORE|8.0')
param linuxFxVersion string = 'DOTNETCORE|8.0'

@description('Connection string do Application Insights')
param appInsightsConnectionString string

@description('URI do Key Vault para a API')
param keyVaultUri string

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: serverFarmId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'AzureKeyVault__VaultUri'
          value: keyVaultUri
        }
      ]
    }
  }
}

output webAppName string = webApp.name
output webAppId string = webApp.id
output webAppPrincipalId string = webApp.identity.principalId
output defaultHostName string = webApp.properties.defaultHostName
