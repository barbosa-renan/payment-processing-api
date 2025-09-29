## Pré-requisitos
- API de pagamentos já criada e funcionando localmente
- Conta Azure ativa
- Azure CLI instalado e configurado
- Visual Studio ou VS Code
- Postman ou similar para testes

## Fase 1: Configuração Inicial do Azure

### 1.1 Criar Resource Group
```bash
# Criar resource group para organizar todos os recursos
az group create --name rg-payment-processing --location brazilsouth

# Verificar criação
az group show --name rg-payment-processing
```

### 1.2 Configurar Variáveis de Ambiente
```powershell
# Definir variáveis para reutilização
### 1.2 Configurar Variáveis de Ambiente
```powershell
# Definir variáveis para reutilização
$resourceGroup = "rg-payment-processing"
$location = "brazilsouth"
$appServicePlan = "asp-payment-api"
$webAppName = "payment-api-8530"
$serviceBusNamespace = "sb-payment-processing-8530"
$eventGridTopic = "payment-events-topic"
$functionAppName = "func-payment-processor-8530"
$storageAccount = "stpaymentproc-8530"
$logicAppName = "logic-payment-workflow"
$sqlServerName = "sql-payment-8530"
$databaseName = "PaymentDB"
$keyVaultName = "kv-payment-api"
```

## Fase 2: Azure Service Bus

### 2.1 Criar Service Bus Namespace e Armazenar connection String no key Vault
```bash
# Criar namespace
az servicebus namespace create --resource-group $resourceGroup --name $serviceBusNamespace --location $location --sku Standard

# Obter connection string
$connectionString=$(az servicebus namespace authorization-rule keys list --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)

# Criar o Key Vault (se ainda não existir)
az group create -n $resourceGroup -l $location
az keyvault create -n $keyVaultName -g $resourceGroup -l $location

# Salvar a connection string como segredo
# Dica: use nomes "hierárquicos" com duplo hífen para mapear para "seções" (ServiceBus:ConnectionString)
az keyvault secret set -n ServiceBus--ConnectionString --vault-name $keyVaultName --value $connectionString
```

```bash
# Se tiver problemas com o keyvault, pode ser que o seu Key Vault está usando RBAC e a identidade 
# com a qual você está logado não tem papel para “setar” segredos.
$kvId=$(az keyvault show -n $keyVaultName -g $resourceGroup --query id -o tsv) 

$myObjId=$(az ad signed-in-user show --query id -o tsv)

# Conceda permissão para gerenciar segredos
az role assignment create --assignee $myObjId --role "Key Vault Secrets Officer" --scope $kvId
```

### 2.2 Criar Filas
```bash
# Fila para pagamentos processados
az servicebus queue create --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name payment-processed --max-delivery-count 3 --default-message-time-to-live PT24H

# Fila para pagamentos falhados
az servicebus queue create --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name payment-failed --max-delivery-count 5 --default-message-time-to-live PT72H

# Fila para notificações
az servicebus queue create --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name notifications --max-delivery-count 3

# Fila para estornos
az servicebus queue create --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name refund-requests --max-delivery-count 5

# Fila para transações de alto valor (requer aprovação)
az servicebus queue create --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name high-value-approval --max-delivery-count 3
```

### 2.3 Integração na API
```json
// Adicionar ao appsettings.json
{
  "ConnectionStrings": {
    "ServiceBus": "<sua-connection-service-bus>"
  },
  "ServiceBusQueues": {
    "PaymentProcessed": "payment-processed",
    "PaymentFailed": "payment-failed",
    "Notifications": "notifications",
    "RefundRequests": "refund-requests",
    "HighValueApproval": "high-value-approval"
  }
}
```

**Tarefas a completar:**
- [x] Anotar a connection string do Service Bus
- [x] Implementar ServiceBusService na API
- [x] Testar envio de mensagens para cada fila
- [x] Configurar dead letter queues
- [x] Implementar retry policies
- [x] Testar cenários de falha

## Fase 3: Event Grid

### 3.1 Criar Event Grid Topic
```bash
# Criar custom topic
az eventgrid topic create --resource-group $resourceGroup --name $eventGridTopic --location brazilsouth

# Obter endpoint
$eventGridTopicEndpoint=$(az eventgrid topic show --resource-group $resourceGroup --name $eventGridTopic --query "endpoint" -o tsv)

# Salvar topic endpoint como segredo
# Dica: use nomes "hierárquicos" com duplo hífen para mapear para "seções" (EventGrid:TopicEndpoint)
az keyvault secret set -n EventGrid--TopicEndpoint --vault-name $keyVaultName --value $eventGridTopicEndpoint

# Obter access key
$eventGridTopicAccessKey=$(az eventgrid topic key list --resource-group $resourceGroup --name $eventGridTopic --query "key1" -o tsv)

# Salvar access key string como segredo
# Dica: use nomes "hierárquicos" com duplo hífen para mapear para "seções" (EventGrid:AccessKey)
az keyvault secret set -n EventGrid--AccessKey --vault-name $keyVaultName --value $eventGridTopicAccessKey
```

### 3.2 Event Schemas Definidos

#### Schema 1: Payment Processed
```json
{
  "eventType": "PaymentProcessing.Payment.Processed",
  "subject": "payment/{transactionId}",
  "eventTime": "2024-01-01T00:00:00Z",
  "dataVersion": "1.0",
  "data": {
    "transactionId": "string",
    "amount": "decimal",
    "currency": "string",
    "status": "Approved",
    "processedAt": "datetime",
    "customerId": "string",
    "paymentMethod": "string",
    "authorizationCode": "string"
  }
}
```

#### Schema 2: High Value Transaction
```json
{
  "eventType": "PaymentProcessing.Transaction.HighValue",
  "subject": "transaction/{transactionId}",
  "eventTime": "2024-01-01T00:00:00Z",
  "dataVersion": "1.0",
  "data": {
    "transactionId": "string",
    "amount": "decimal",
    "currency": "string",
    "customerId": "string",
    "riskScore": "int",
    "requiresApproval": "boolean",
    "customerTier": "string"
  }
}
```

#### Schema 3: Payment Failed
```json
{
  "eventType": "PaymentProcessing.Payment.Failed",
  "subject": "payment/{transactionId}",
  "eventTime": "2024-01-01T00:00:00Z",
  "dataVersion": "1.0",
  "data": {
    "transactionId": "string",
    "amount": "decimal",
    "currency": "string",
    "failureReason": "string",
    "errorCode": "string",
    "customerId": "string",
    "retryable": "boolean"
  }
}
```

**Tarefas a completar:**
- [x] Anotar endpoint e access key do Event Grid
- [ ] Implementar EventGridService na API
- [ ] Criar eventos para diferentes cenários
- [ ] Testar publicação de eventos
- [ ] Configurar event subscriptions
- [ ] Validar schemas dos eventos

## Fase 4: Azure Functions

### 4.1 Criar Storage Account e Function App
```bash
# Criar storage account para Function App
az storage account create \
  --name $storageAccount \
  --resource-group $resourceGroup \
  --location brazilsouth \
  --sku Standard_LRS

# Criar Function App
az functionapp create \
  --resource-group $resourceGroup \
  --consumption-plan-location brazilsouth \
  --runtime dotnet \
  --runtime-version 8 \
  --functions-version 4 \
  --name $functionAppName \
  --storage-account $storageAccount
```

### 4.2 Functions a Implementar

#### Function 1: Payment Processor (Service Bus Trigger)
```csharp
// PaymentProcessorFunction.cs
[FunctionName("ProcessPaymentQueue")]
public static async Task ProcessPaymentQueue(
    [ServiceBusTrigger("payment-processed", Connection = "ServiceBusConnection")] 
    PaymentProcessedMessage message,
    ILogger log)
{
    log.LogInformation($"Processing payment: {message.TransactionId}");
    
    // Tarefas:
    // 1. Validar dados da mensagem
    // 2. Enviar email de confirmação para cliente
    // 3. Atualizar sistemas de inventário
    // 4. Gerar entrada contábil
    // 5. Integrar com ERP
    // 6. Enviar notificação push
}
```

#### Function 2: High Value Monitor (Event Grid Trigger)
```csharp
// HighValueMonitorFunction.cs
[FunctionName("HighValueTransactionMonitor")]
public static async Task HighValueTransactionMonitor(
    [EventGridTrigger] EventGridEvent eventGridEvent,
    ILogger log)
{
    log.LogInformation($"High value transaction detected: {eventGridEvent.Subject}");
    
    // Tarefas:
    // 1. Analisar padrões de fraude
    // 2. Consultar APIs de score de crédito
    // 3. Verificar listas de sanções
    // 4. Calcular risk score
    // 5. Se necessário, pausar transação
    // 6. Notificar equipe de compliance
}
```

#### Function 3: Payment Notification Sender (HTTP Trigger)
```csharp
// NotificationFunction.cs
[FunctionName("SendPaymentNotification")]
public static async Task<IActionResult> SendPaymentNotification(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notification")] 
    HttpRequest req,
    ILogger log)
{
    // Tarefas:
    // 1. Receber webhook da API principal
    // 2. Determinar tipo de notificação (email/SMS/push)
    // 3. Selecionar template baseado no evento
    // 4. Personalizar mensagem para o cliente
    // 5. Enviar via provedores (SendGrid, Twilio, etc.)
    // 6. Log de auditoria
}
```

#### Function 4: Payment Status Checker (Timer Trigger)
```csharp
// StatusCheckerFunction.cs
[FunctionName("PaymentStatusChecker")]
public static async Task PaymentStatusChecker(
    [TimerTrigger("0 */10 * * * *")] TimerInfo timer, // A cada 10 minutos
    ILogger log)
{
    // Tarefas:
    // 1. Buscar pagamentos com status "Processing" há mais de 30min
    // 2. Consultar status nos gateways externos
    // 3. Atualizar status na API principal
    // 4. Publicar eventos de mudança de status
    // 5. Alertar sobre transações órfãs
}
```

#### Function 5: Refund Processor (Service Bus Trigger)
```csharp
// RefundProcessorFunction.cs
[FunctionName("ProcessRefundRequest")]
public static async Task ProcessRefundRequest(
    [ServiceBusTrigger("refund-requests", Connection = "ServiceBusConnection")] 
    RefundRequestMessage message,
    ILogger log)
{
    // Tarefas:
    // 1. Validar se refund é permitido
    // 2. Calcular taxas de cancelamento
    // 3. Chamar gateway de pagamento para estorno
    // 4. Atualizar status do pagamento original
    // 5. Gerar comprovante de estorno
    // 6. Enviar notificação ao cliente
}
```

### 4.3 Configurações das Functions
```bash
# Configurar connection strings para as Functions
az functionapp config appsettings set \
  --name $functionAppName \
  --resource-group $resourceGroup \
  --settings "ServiceBusConnection=sua-servicebus-connection-string" \
           "PaymentApiBaseUrl=https://sua-api.azurewebsites.net" \
           "SendGridApiKey=sua-sendgrid-key" \
           "TwilioAccountSid=seu-twilio-sid" \
           "ApplicationInsights__InstrumentationKey=sua-ai-key"
```

**Tarefas a completar:**
- [ ] Criar projeto Azure Functions local
- [ ] Implementar as 5 functions listadas acima
- [ ] Configurar todas as connection strings
- [ ] Testar cada function localmente
- [ ] Deploy das functions para Azure
- [ ] Configurar monitoring e alertas
- [ ] Testar triggers em ambiente Azure

## Fase 5: Logic Apps

### 5.1 Criar Logic Apps Standard
```bash
# Criar App Service Plan para Logic Apps Standard
az appservice plan create \
  --name "plan-logic-apps" \
  --resource-group $resourceGroup \
  --location brazilsouth \
  --sku WS1 \
  --is-linux true

# Criar Logic App Standard
az logicapp create \
  --name $logicAppName \
  --resource-group $resourceGroup \
  --plan "plan-logic-apps" \
  --storage-account $storageAccount
```

### 5.2 Logic Apps a Implementar

#### Logic App 1: Payment Approval Workflow
**Nome:** payment-approval-process
**Trigger:** Event Grid - PaymentProcessing.Transaction.HighValue
**Fluxo:**
1. **Receber evento** de transação de alto valor
2. **Condição:** Valor > R$ 5.000?
   - Se SIM: continua para aprovação
   - Se NÃO: aprovar automaticamente
3. **Ação:** Buscar dados do cliente na API
4. **Ação:** Enviar email para gerente com:
   - Dados da transação
   - Dados do cliente
   - Botões de Aprovar/Rejeitar
5. **Aguardar aprovação** (timeout 2 horas)
6. **Condição:** Aprovado?
   - Se APROVADO: Chamar API para processar
   - Se REJEITADO: Chamar API para cancelar
7. **Ação final:** Enviar notificação ao cliente

#### Logic App 2: Refund Processing Workflow
**Nome:** refund-processing-workflow
**Trigger:** Service Bus - refund-requests queue
**Fluxo:**
1. **Receber mensagem** de solicitação de estorno
2. **Ação:** Buscar dados do pagamento original
3. **Condição:** Pagamento permite estorno?
   - Verificar tempo limite
   - Verificar status atual
   - Verificar valor disponível
4. **Se permitido:**
   - Chamar gateway de pagamento
   - Atualizar status na API
   - Gerar comprovante
   - Enviar email ao cliente
5. **Se não permitido:**
   - Enviar email explicando motivo
   - Registrar tentativa de estorno inválida

#### Logic App 3: Fraud Detection Workflow
**Nome:** fraud-detection-workflow
**Trigger:** Event Grid - PaymentProcessing.Payment.Failed (com errorCode específico)
**Fluxo:**
1. **Receber evento** de pagamento falhado suspeito
2. **Ação:** Analisar padrão de falhas do cliente
3. **Ação:** Consultar API externa de score de risco
4. **Condição:** Score indica fraude?
5. **Se suspeito:**
   - Bloquear cliente temporariamente
   - Criar ticket no sistema de suporte
   - Enviar alerta para equipe de segurança
   - Notificar cliente sobre bloqueio
6. **Logging:** Registrar todas as ações no sistema de auditoria

#### Logic App 4: Customer Communication Workflow
**Nome:** customer-communication-workflow
**Trigger:** Service Bus - notifications queue
**Fluxo:**
1. **Receber mensagem** de notificação
2. **Ação:** Determinar canal preferido do cliente (email/SMS/push)
3. **Ação:** Selecionar template baseado no tipo de evento
4. **Condições paralelas:**
   - **Branch Email:** Usar SendGrid/Outlook
   - **Branch SMS:** Usar Twilio
   - **Branch Push:** Usar Azure Notification Hub
5. **Ação:** Personalizar mensagem com dados do cliente
6. **Ação:** Enviar notificação
7. **Ação:** Registrar entrega no histórico de comunicação

### 5.3 Configurações dos Logic Apps
- **Connections necessárias:**
  - Service Bus
  - Event Grid  
  - Office 365 Outlook (para emails)
  - Twilio (para SMS)
  - HTTP (para chamadas à API)
  - SQL Server (para consultas diretas se necessário)

**Tarefas a completar:**
- [ ] Criar os 4 Logic Apps via Azure Portal
- [ ] Configurar todos os triggers e connections
- [ ] Implementar cada fluxo passo a passo
- [ ] Testar cada workflow isoladamente
- [ ] Configurar error handling e retry policies
- [ ] Documentar cada workflow
- [ ] Testar integração end-to-end

## Fase 6: Deploy da API

### 6.1 Criar SQL Database
```bash
# Criar SQL Server
az sql server create \
  --name $sqlServerName \
  --resource-group $resourceGroup \
  --location brazilsouth \
  --admin-user paymentadmin \
  --admin-password "SuaSenhaForte123!"

# Configurar firewall para permitir serviços Azure
az sql server firewall-rule create \
  --resource-group $resourceGroup \
  --server $sqlServerName \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Criar database
az sql db create \
  --resource-group $resourceGroup \
  --server $sqlServerName \
  --name $databaseName \
  --service-objective Basic
```

### 6.2 Criar App Service
```bash
# Criar App Service Plan
az appservice plan create \
  --name $appServicePlan \
  --resource-group $resourceGroup \
  --sku B1 \
  --is-linux true

# Criar Web App
az webapp create \
  --resource-group $resourceGroup \
  --plan $appServicePlan \
  --name $webAppName \
  --runtime "DOTNETCORE:8.0"
```

### 6.3 Configurar App Settings
```bash
# Connection string do banco
az webapp config connection-string set \
  --resource-group $resourceGroup \
  --name $webAppName \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=$databaseName;Persist Security Info=False;User ID=paymentadmin;Password=SuaSenhaForte123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Service Bus connection string
az webapp config connection-string set \
  --resource-group $resourceGroup \
  --name $webAppName \
  --connection-string-type Custom \
  --settings ServiceBus="sua-servicebus-connection-string"

# Configurar app settings
az webapp config appsettings set \
  --resource-group $resourceGroup \
  --name $webAppName \
  --settings "EventGrid__TopicEndpoint=seu-eventgrid-endpoint" \
           "EventGrid__AccessKey=sua-eventgrid-key" \
           "PaymentGateway__Environment=sandbox" \
           "PaymentGateway__TimeoutSeconds=30" \
           "ASPNETCORE_ENVIRONMENT=Production"
```

### 6.4 Deploy da Aplicação
```bash
# Build e deploy (executar na pasta da API)
dotnet publish -c Release -o ./bin/Release/publish

# Criar zip para deploy
Compress-Archive -Path "./bin/Release/publish/*" -DestinationPath "./api-deploy.zip"

# Deploy via Azure CLI
az webapp deployment source config-zip \
  --resource-group $resourceGroup \
  --name $webAppName \
  --src "./api-deploy.zip"
```

**Tarefas a completar:**
- [ ] Executar migrations do Entity Framework
- [ ] Configurar CI/CD pipeline no Azure DevOps/GitHub Actions
- [ ] Configurar SSL/TLS personalizado
- [ ] Configurar custom domain (opcional)
- [ ] Testar API em ambiente de produção
- [ ] Configurar scaling rules
- [ ] Implementar health checks

## Fase 7: Testes de Integração End-to-End

### 7.1 Configurar Application Insights
```bash
# Criar Application Insights
az monitor app-insights component create \
  --resource-group $resourceGroup \
  --app payment-api-insights \
  --location brazilsouth \
  --application-type web

# Obter instrumentation key
az monitor app-insights component show \
  --resource-group $resourceGroup \
  --app payment-api-insights \
  --query "instrumentationKey" -o tsv
```

### 7.2 Cenários de Teste Completos

#### Cenário 1: Pagamento Cartão Aprovado (Valor Normal)
**Sequência esperada:**
1. `POST /api/payment/process` (valor: R$ 100,00)
2. ✅ API retorna status 201 com transactionId
3. ✅ Mensagem publicada no Service Bus queue `payment-processed`
4. ✅ Evento publicado no Event Grid `PaymentProcessing.Payment.Processed`
5. ✅ Azure Function `ProcessPaymentQueue` executada
6. ✅ Logic App `customer-communication-workflow` executado
7. ✅ Email de confirmação enviado ao cliente

#### Cenário 2: Pagamento Alto Valor (Requer Aprovação)
**Sequência esperada:**
1. `POST /api/payment/process` (valor: R$ 8.000,00)
2. ✅ API retorna status 202 (aceito para processamento)
3. ✅ Evento `PaymentProcessing.Transaction.HighValue` publicado no Event Grid
4. ✅ Azure Function `HighValueTransactionMonitor` executada
5. ✅ Logic App `payment-approval-process` disparado
6. ✅ Email de aprovação enviado para gerente
7. ✅ Aguardar aprovação manual
8. ✅ Após aprovação, pagamento processado
9. ✅ Notificação final enviada ao cliente

#### Cenário 3: Pagamento Recusado (Cartão Inválido)
**Sequência esperada:**
1. `POST /api/payment/process` (cartão inválido)
2. ✅ API retorna status 400 ou 402
3. ✅ Mensagem publicada no Service Bus queue `payment-failed`
4. ✅ Evento `PaymentProcessing.Payment.Failed` publicado
5. ✅ Logic App `fraud-detection-workflow` avalia se é suspeito
6. ✅ Notificação de falha enviada ao cliente

#### Cenário 4: Solicitação de Estorno
**Sequência esperada:**
1. `POST /api/payment/{transactionId}/refund`
2. ✅ API valida se estorno é permitido
3. ✅ Mensagem publicada no Service Bus queue `refund-requests`
4. ✅ Logic App `refund-processing-workflow` executado
5. ✅ Gateway de pagamento processado
6. ✅ Status atualizado na API
7. ✅ Comprovante de estorno enviado ao cliente

#### Cenário 5: Monitoramento de Pagamentos Pendentes
**Sequência esperada:**
1. Criar pagamento que fica "travado" em Processing
2. ✅ Aguardar 10 minutos
3. ✅ Azure Function `PaymentStatusChecker` executada automaticamente
4. ✅ Status consultado no gateway
5. ✅ Status atualizado na API
6. ✅ Evento de mudança de status publicado

### 7.3 Collection do Postman
Criar collection com:
```json
{
  "info": {
    "name": "Payment Processing API - E2E Tests",
    "description": "Testes end-to-end para validar integrações Azure"
  },
  "item": [
    {
      "name": "1. Pagamento Normal - Cartão",
      "request": {
        "method": "POST",
        "url": "{{api_url}}/api/payment/process",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"transactionId\": \"{{$guid}}\",\n  \"amount\": 100.00,\n  \"currency\": \"BRL\",\n  \"paymentMethod\": \"CREDIT_CARD\",\n  \"customer\": {\n    \"customerId\": \"CUST001\",\n    \"name\": \"João Silva\",\n    \"email\": \"joao@email.com\",\n    \"document\": \"12345678901\"\n  },\n  \"card\": {\n    \"number\": \"4111111111111111\",\n    \"holderName\": \"JOAO SILVA\",\n    \"expiryMonth\": \"12\",\n    \"expiryYear\": \"2025\",\n    \"cvv\": \"123\",\n    \"brand\": \"VISA\"\n  }\n}"
        }
      }
    },
    {
      "name": "2. Pagamento Alto Valor",
      "request": {
        "method": "POST",
        "url": "{{api_url}}/api/payment/process",
        "body": {
          "mode": "raw", 
          "raw": "{\n  \"transactionId\": \"{{$guid}}\",\n  \"amount\": 8000.00,\n  \"currency\": \"BRL\",\n  \"paymentMethod\": \"CREDIT_CARD\",\n  \"customer\": {\n    \"customerId\": \"CUST002\",\n    \"name\": \"Maria Santos\",\n    \"email\": \"maria@email.com\",\n    \"document\": \"98765432109\"\n  },\n  \"card\": {\n    \"number\": \"4111111111111111\",\n    \"holderName\": \"MARIA SANTOS\",\n    \"expiryMonth\": \"12\",\n    \"expiryYear\": \"2025\",\n    \"cvv\": \"123\",\n    \"brand\": \"VISA\"\n  }\n}"
        }
      }
    }
  ],
  "variable": [
    {
      "key": "api_url",
      "value": "https://sua-api.azurewebsites.net"
    }
  ]
}
```

**Tarefas a completar:**
- [ ] Criar collection completa no Postman
- [ ] Implementar todos os 5 cenários de teste
- [ ] Configurar environment variables
- [ ] Executar testes e documentar resultados
- [ ] Criar dashboard de monitoramento no Application Insights
- [ ] Configurar alertas para falhas

## Fase 8: Segurança e Governança

### 8.1 Azure Key Vault
```bash
# Criar Key Vault
az keyvault create \
  --name "kv-payment-secrets" \
  --resource-group $resourceGroup \
  --location brazilsouth \
  --sku standard

# Adicionar secrets
az keyvault secret set \
  --vault-name "kv-payment-secrets" \
  --name "ServiceBusConnectionString" \
  --value "sua-connection-string"

az keyvault secret set \
  --vault-name "kv-payment-secrets" \
  --name "EventGridAccessKey" \
  --value "sua-access-key"
```

### 8.2 Managed Identity
```bash
# Habilitar Managed Identity na Web App
az webapp identity assign \
  --name $webAppName \
  --resource-group $resourceGroup

# Dar permissão ao Key Vault
az keyvault set-policy \
  --name "kv-payment-secrets" \
  --resource-group $resourceGroup \
  --object-id "object-id-da-webapp" \
  --secret-permissions get list
```

### 8.3 Network Security
```bash
# Criar Virtual Network
az network vnet create \
  --resource-group $resourceGroup \
  --name vnet-payment \
  --address-prefix 10.0.0.0/16

# Criar subnet para App Service
az network vnet subnet create \
  --resource-group $resourceGroup \
  --vnet-name vnet-payment \
  --name subnet-appservice \
  --address-prefix 10.0.1.0/24

# Integrar App Service com VNet
az webapp vnet-integration add \
  --resource-group $resourceGroup \
  --name $webAppName \
  --vnet vnet-payment \
  --subnet subnet-appservice
```

### 8.4 Backup e Disaster Recovery
```bash
# Configurar backup automático do SQL Database
az sql db ltr-policy set \
  --resource-group $resourceGroup \
  --server $sqlServerName \
  --database $databaseName \
  --weekly-retention P4W \
  --monthly-retention P12M \
  --yearly-retention P7Y \
  --week-of-year 1
```

**Tarefas de Segurança:**
- [ ] Migrar todas as secrets para Key Vault
- [ ] Configurar Managed Identity em todos os recursos
- [ ] Implementar Network Security Groups
- [ ] Configurar RBAC para todos os recursos
- [ ] Implementar Azure Policy para governança
- [ ] Configurar backup automático do banco
- [ ] Documentar plano de disaster recovery
- [ ] Testar procedimentos de restore
- [ ] Implementar WAF no App Service
- [ ] Configurar DDoS Protection
