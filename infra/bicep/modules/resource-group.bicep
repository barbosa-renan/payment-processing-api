targetScope = 'subscription'

@description('Nome do Resource Group')
param name string

@description('Localizacao do Resource Group')
param location string

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: name
  location: location
}

output name string = rg.name
output id string = rg.id
