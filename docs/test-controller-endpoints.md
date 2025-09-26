# TestController - Endpoints para Teste do Service Bus

Esta controller fornece endpoints para testar o envio de mensagens para as diferentes filas do Azure Service Bus.

## 📋 Endpoints Disponíveis

### 1. Test Payment Processed Message
**POST** `/api/test/service-bus/payment-processed`

Envia uma mensagem de pagamento processado com sucesso.

**Response:**
```json
{
  "message": "Payment processed message sent successfully",
  "transactionId": "generated-guid"
}
```

### 2. Test Payment Failed Message  
**POST** `/api/test/service-bus/payment-failed`

Envia uma mensagem de pagamento que falhou.

**Response:**
```json
{
  "message": "Payment failed message sent successfully", 
  "transactionId": "generated-guid"
}
```

### 3. Test Notification Message
**POST** `/api/test/service-bus/notification`

Envia uma mensagem de notificação para o cliente.

**Response:**
```json
{
  "message": "Notification message sent successfully",
  "transactionId": "generated-guid"
}
```

### 4. Test Refund Request Message
**POST** `/api/test/service-bus/refund-request`

Envia uma mensagem de solicitação de reembolso.

**Response:**
```json
{
  "message": "Refund request message sent successfully",
  "refundId": "generated-guid"
}
```

### 5. Test High Value Approval Message
**POST** `/api/test/service-bus/high-value-approval`

Envia uma mensagem para aprovação de transação de alto valor.

**Response:**
```json
{
  "message": "High value approval message sent successfully",
  "transactionId": "generated-guid"
}
```

### 6. Test All Queues
**POST** `/api/test/service-bus/test-all`

Testa todas as filas em sequência com delay de 1 segundo entre cada envio.

**Response:**
```json
{
  "message": "All queues tested successfully",
  "results": [
    { "queue": "payment-processed", "status": "Success" },
    { "queue": "payment-failed", "status": "Success" },
    { "queue": "notifications", "status": "Success" },
    { "queue": "refund-requests", "status": "Success" },
    { "queue": "high-value-approval", "status": "Success" }
  ]
}
```

## 🧪 Como Testar

### Via Swagger UI
1. Execute a aplicação
2. Acesse `https://localhost:7xxx/swagger`
3. Encontre a seção "Test" 
4. Execute os endpoints desejados

### Via cURL

```bash
# Teste de pagamento processado
curl -X POST "https://localhost:7xxx/api/test/service-bus/payment-processed" \
  -H "Content-Type: application/json"

# Teste de pagamento falhado  
curl -X POST "https://localhost:7xxx/api/test/service-bus/payment-failed" \
  -H "Content-Type: application/json"

# Teste de notificação
curl -X POST "https://localhost:7xxx/api/test/service-bus/notification" \
  -H "Content-Type: application/json"

# Teste de reembolso
curl -X POST "https://localhost:7xxx/api/test/service-bus/refund-request" \
  -H "Content-Type: application/json"

# Teste de aprovação de alto valor
curl -X POST "https://localhost:7xxx/api/test/service-bus/high-value-approval" \
  -H "Content-Type: application/json"

# Teste de todas as filas
curl -X POST "https://localhost:7xxx/api/test/service-bus/test-all" \
  -H "Content-Type: application/json"
```

## 📊 Dados de Teste

### PaymentProcessedMessage
- **TransactionId**: GUID gerado automaticamente
- **CustomerId**: "TEST_CUSTOMER_001"  
- **Amount**: R$ 150,00
- **PaymentMethod**: "CREDIT_CARD"
- **Items**: 1 produto teste

### PaymentFailedMessage  
- **TransactionId**: GUID gerado automaticamente
- **CustomerId**: "TEST_CUSTOMER_002"
- **Amount**: R$ 250,00
- **FailureReason**: "Insufficient funds"
- **ErrorCode**: "51"

### NotificationMessage
- **CustomerId**: "TEST_CUSTOMER_003"
- **NotificationType**: "PAYMENT_CONFIRMATION"
- **Channel**: "EMAIL"
- **Data**: Nome do cliente, valor, método de pagamento

### RefundRequestMessage
- **RefundId**: GUID gerado automaticamente
- **CustomerId**: "TEST_CUSTOMER_004"
- **RefundAmount**: R$ 75,50
- **Reason**: "Customer requested cancellation"

### HighValueApprovalMessage
- **TransactionId**: GUID gerado automaticamente
- **CustomerId**: "TEST_CUSTOMER_005"
- **Amount**: R$ 8.500,00
- **RiskScore**: 45

## 🔍 Monitoramento

### Logs da Aplicação
Cada endpoint gera logs estruturados:
```
[INFO] Test message sent to {queue-name} queue
```

### Azure Service Bus
1. Acesse o portal do Azure
2. Navegue até seu Service Bus Namespace
3. Verifique as filas para ver as mensagens enviadas
4. Use Service Bus Explorer para inspecionar o conteúdo

### Service Bus Explorer (Opcional)
Ferramenta desktop para gerenciar e monitorar mensagens:
- Download: https://github.com/paolosalvatori/ServiceBusExplorer

## ⚠️ Importante

- **Ambiente de Teste**: Use apenas em ambiente de desenvolvimento/teste
- **Connection String**: Certifique-se que a connection string do Key Vault está configurada
- **Filas**: As filas devem existir no Azure Service Bus antes do teste
- **Permissões**: Verifique se a aplicação tem permissões de envio nas filas

## 🚀 Próximos Passos

Após testar com sucesso:
1. Remover ou desabilitar a TestController em produção
2. Implementar consumers para processar as mensagens
3. Configurar dead letter queues para mensagens com falha
4. Adicionar métricas e alertas no Azure Monitor