# Payment Processing API - Azure Integration

Sistema de processamento de pagamentos desenvolvido para demonstrar a integração entre uma API .NET e principais serviços do Azure. Este é um projeto de estudo focado na aplicação prática de tecnologias Azure em um cenário real de negócio.

## Visão Geral

Esta aplicação implementa uma API completa para processamento de pagamentos, incluindo fluxos como aprovação manual de transações de alto valor, análise de fraude, processamento de estornos e notificações automáticas. O projeto utiliza uma arquitetura orientada a eventos (event-driven) integrada com o ecossistema Azure.

## Tecnologias Utilizadas

### Backend
- .NET 8 Web API
- Entity Framework Core 8
- AutoMapper
- FluentValidation
- Serilog

### Azure Services
- App Service (hospedagem da API)
- SQL Database (armazenamento transacional)
- Service Bus (messaging assíncrono)
- Event Grid (publicação/subscrição de eventos)
- Azure Functions (processamento serverless)
- Logic Apps (workflows visuais)
- Key Vault (gerenciamento de secrets)
- Application Insights (monitoramento e logs)

### Testes e Qualidade
- xUnit
- TestContainers
- Polly (resilience patterns)
- HealthChecks

## Principais Funcionalidades

### Processamento de Pagamentos
- Suporte a múltiplos métodos de pagamento (cartão de crédito, PIX, boleto)
- Validações robustas de dados (CPF/CNPJ, cartões, valores)
- Controle de estados de transação
- Timeout e recuperação de transações órfãs

### Fluxos Avançados
- Aprovação manual para transações de alto valor
- Análise automática de fraude
- Processamento completo de estornos
- Sistema de notificações multi-canal

### Integração Azure
- Mensageria assíncrona via Service Bus
- Eventos de domínio via Event Grid
- Processamento serverless com Azure Functions
- Workflows de negócio com Logic Apps
- Monitoramento completo com Application Insights

## Arquitetura

O sistema utiliza uma arquitetura orientada a eventos com os seguintes fluxos principais:

1. **Pagamento Normal**: Cliente → API → Gateway → Service Bus → Azure Functions → Notificação
2. **Alto Valor**: Cliente → API → Event Grid → Logic Apps → Aprovação Manual → Processamento
3. **Estorno**: Cliente → API → Service Bus → Logic Apps → Gateway → Confirmação
4. **Monitoramento**: Timer → Azure Functions → Consulta Status → Atualização


![Desenho da Solução](/docs/payment-api.drawio.png)

## Como Executar

### Pré-requisitos
- .NET 8 SDK
- Visual Studio 2022 ou VS Code
- Docker Desktop
- Azure CLI
- Conta Azure ativa

### Execução Local

1. Clone o repositório:
```bash
git clone https://github.com/seu-usuario/payment-processing-api.git
cd payment-processing-api
```

2. Configure as connection strings:
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "sua-connection-string"
dotnet user-secrets set "ConnectionStrings:ServiceBus" "sua-servicebus-connection"
dotnet user-secrets set "EventGrid:TopicEndpoint" "seu-eventgrid-endpoint"
dotnet user-secrets set "EventGrid:AccessKey" "sua-eventgrid-key"
```

3. Execute as dependências com Docker:
```bash
docker-compose up -d
```

4. Execute as migrations do banco:
```bash
dotnet ef database update
```

5. Inicie a aplicação:
```bash
dotnet run --project PaymentProcessingAPI
```

A API estará disponível em: `https://localhost:7001`

### Teste da API

Exemplo de requisição para processar um pagamento:

```bash
curl -X POST https://localhost:7001/api/payment/process \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "550e8400-e29b-41d4-a716-446655440000",
    "amount": 100.00,
    "currency": "BRL",
    "paymentMethod": "CREDIT_CARD",
    "customer": {
      "customerId": "CUST001",
      "name": "João Silva",
      "email": "joao@email.com",
      "document": "12345678901"
    },
    "card": {
      "number": "4111111111111111",
      "holderName": "JOAO SILVA",
      "expiryMonth": "12",
      "expiryYear": "2025",
      "cvv": "123",
      "brand": "VISA"
    }
  }'
```

## Deploy no Azure

TODO: Disponiblizar scripts

### Deploy da Aplicação

```bash
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip \
  --resource-group rg-payment-processing \
  --name sua-webapp \
  --src "./publish.zip"
```

## Endpoints Principais

- `POST /api/payment/process` - Processar novo pagamento
- `GET /api/payment/{id}` - Consultar status do pagamento  
- `POST /api/payment/{id}/cancel` - Cancelar pagamento
- `POST /api/payment/{id}/refund` - Solicitar estorno
- `GET /api/payment` - Listar pagamentos
- `GET /health` - Health check da aplicação

## Testes

Execute os testes unitários:
```bash
dotnet test PaymentProcessingAPI.Tests
```

Execute os testes de integração:
```bash
dotnet test PaymentProcessingAPI.IntegrationTests
```

## Padrões Implementados

- Event-Driven Architecture
- CQRS (Command Query Responsibility Segregation)
- Saga Pattern para transações distribuídas
- Circuit Breaker e Retry Patterns
- Repository Pattern
- Clean Architecture principles

## Propósito Educacional

Este projeto foi desenvolvido especificamente para:

- Demonstrar integração prática entre .NET e Azure
- Implementar padrões modernos de arquitetura distribuída
- Mostrar uso de serviços Azure em cenários reais
- Servir como referência para desenvolvimento cloud-native
- Praticar DevOps e CI/CD com Azure

O código foi estruturado seguindo boas práticas de desenvolvimento e está preparado para ambientes de produção, mas seu objetivo principal é educacional e demonstrativo.

## Estrutura do Projeto

```
PaymentProcessingAPI/
├── PaymentProcessingAPI/          # API principal
├── PaymentProcessingAPI.Tests/    # Testes unitários
├── PaymentProcessingAPI.IntegrationTests/  # Testes de integração
├── scripts/                       # Scripts de deploy
├── docs/                         # Documentação
└── docker-compose.yml            # Dependências locais
```

## Licença

Este projeto está sob licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

## Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para:
- Reportar bugs
- Sugerir novas funcionalidades
- Melhorar a documentação
- Enviar pull requests

Para contribuir, faça um fork do projeto, crie uma branch para sua feature e envie um pull request.