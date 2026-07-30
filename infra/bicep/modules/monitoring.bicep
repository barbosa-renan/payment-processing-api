@description('Nome do workspace do Log Analytics')
param workspaceName string

@description('Nome do Application Insights')
param appInsightsName string

@description('Localizacao dos recursos')
param location string

@description('Retencao de logs em dias')
param retentionInDays int = 30

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    retentionInDays: retentionInDays
    sku: {
      name: 'PerGB2018'
    }
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

output workspaceId string = logAnalytics.id
output appInsightsConnectionString string = appInsights.properties.ConnectionString
