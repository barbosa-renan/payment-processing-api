@description('Nome do Key Vault')
param keyVaultName string

@description('ID do principal (Managed Identity, usuario, grupo ou SPN)')
param principalId string

@description('Role definition ID no formato GUID')
param roleDefinitionId string

@description('Valor estavel usado para gerar nome unico da role assignment')
param assignmentNameSeed string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, principalId, roleDefinitionId, assignmentNameSeed)
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalType: 'ServicePrincipal'
  }
}
