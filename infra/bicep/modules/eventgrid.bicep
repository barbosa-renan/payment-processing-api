@description('Nome do topico custom do Event Grid')
param topicName string

@description('Localizacao do Event Grid')
param location string

resource topic 'Microsoft.EventGrid/topics@2024-06-01-preview' = {
  name: topicName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    inputSchema: 'EventGridSchema'
  }
}

output topicId string = topic.id
output topicEndpoint string = topic.properties.endpoint
