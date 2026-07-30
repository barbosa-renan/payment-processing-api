@description('Nome do SQL Server')
param sqlServerName string

@description('Nome do banco de dados')
param databaseName string

@description('Localizacao do SQL')
param location string

@description('Usuario administrador do SQL Server')
param administratorLogin string

@secure()
@description('Senha do administrador do SQL Server')
param administratorPassword string

@description('SKU do banco, ex.: Basic, S0, GP_S_Gen5_1')
param databaseSkuName string = 'Basic'

@description('Permite acesso publico no SQL Server')
param publicNetworkAccess string = 'Enabled'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '12.0'
    publicNetworkAccess: publicNetworkAccess
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: databaseName
  parent: sqlServer
  location: location
  sku: {
    name: databaseSkuName
  }
}

output sqlServerName string = sqlServer.name
output databaseName string = sqlDb.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
