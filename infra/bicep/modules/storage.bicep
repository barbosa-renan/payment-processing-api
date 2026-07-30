@description('Nome da conta de armazenamento (3-24 chars, minusculo e numeros)')
param storageAccountName string

@description('Localizacao da conta de armazenamento')
param location string

@description('SKU da conta')
param skuName string = 'Standard_LRS'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: skuName
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

output storageAccountName string = storage.name
output storageAccountId string = storage.id
