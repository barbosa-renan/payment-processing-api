@description('Nome do namespace do Service Bus')
param namespaceName string

@description('Localizacao do Service Bus')
param location string

@description('SKU do Service Bus: Standard ou Premium')
@allowed([
  'Standard'
  'Premium'
])
param sku string = 'Standard'

@description('Lista de filas para criar')
param queues array

resource sbNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: namespaceName
  location: location
  sku: {
    name: sku
    tier: sku
  }
}

resource sbQueues 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = [for queueName in queues: {
  name: queueName
  parent: sbNamespace
  properties: {
    maxDeliveryCount: 5
    deadLetteringOnMessageExpiration: true
    lockDuration: 'PT1M'
    defaultMessageTimeToLive: 'P7D'
    enablePartitioning: sku == 'Standard'
  }
}]

output serviceBusNamespaceName string = sbNamespace.name
output serviceBusNamespaceId string = sbNamespace.id
