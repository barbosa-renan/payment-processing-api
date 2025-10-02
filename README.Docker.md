# Docker Setup - Payment Processing API

Este documento explica como executar a Payment Processing API usando Docker com integração segura ao Azure Key Vault.

## 📋 Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) instalado
- [Docker Compose](https://docs.docker.com/compose/install/) instalado
- Pelo menos 4GB de RAM disponível
- Portas 5000, 5001, 1433, 6379 e 5341 livres
- **Azure Key Vault configurado com as secrets necessárias (para produção)**

## 🔐 Configuração de Segurança com Azure Key Vault

### Secrets Necessárias no Key Vault

Configure as seguintes secrets no seu Azure Key Vault:

| Secret Name | Descrição | Exemplo |
|-------------|-----------|---------|
| `ConnectionStrings--DefaultConnection` | Connection string do banco de produção | `Server=xxx;Database=PaymentProcessingDB;...` |
| `AzureServiceBus--ConnectionString` | Connection string do Service Bus | `Endpoint=sb://xxx.servicebus.windows.net/...` |
| `EventGrid--TopicEndpoint` | Endpoint do Event Grid | `https://xxx.eventgrid.azure.net/api/events` |
| `EventGrid--AccessKey` | Chave de acesso do Event Grid | `xxx` |
| `PaymentGateway--ApiKey` | API Key do gateway de pagamento | `xxx` |
| `PaymentGateway--BaseUrl` | URL base do gateway | `https://api.paymentgateway.com` |
| `Jwt--SecretKey` | Chave secreta para JWT | `xxx` (mín. 32 caracteres) |

### Autenticação no Azure

A aplicação suporta dois métodos de autenticação:

#### 1. Managed Identity (Recomendado para produção)
```bash
# Sem configuração adicional - usa a identidade gerenciada do recurso Azure
AZURE_KEYVAULT_URI=https://your-keyvault.vault.azure.net/
```

#### 2. Service Principal
```bash
AZURE_KEYVAULT_URI=https://your-keyvault.vault.azure.net/
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
AZURE_TENANT_ID=your-tenant-id
```

## 🚀 Início Rápido

### 1. Configuração das Variáveis de Ambiente

Copie o arquivo de exemplo e configure as variáveis:

```bash
# Windows (PowerShell)
Copy-Item .env.example .env

# Linux/Mac
cp .env.example .env
```

**IMPORTANTE**: Edite o arquivo `.env` e configure:
- `AZURE_KEYVAULT_URI`: URI do seu Key Vault
- `DB_SA_PASSWORD`: Senha do SQL Server (apenas para desenvolvimento local)
- Credenciais do Azure (se não estiver usando Managed Identity)

### 2. Configurar Azure Key Vault

Antes de executar em produção, configure todas as secrets no Azure Key Vault conforme a tabela acima.

### 3. Executar com Docker Compose

#### Para Desenvolvimento Local (sem Key Vault)
```bash
# Usar configuração local sem secrets
ASPNETCORE_ENVIRONMENT=LocalDevelopment
```

#### Para Produção (com Key Vault)
```bash
# Usar configuração Docker com Key Vault
ASPNETCORE_ENVIRONMENT=Docker
```

#### Windows (PowerShell)
```powershell
# Construir e iniciar todos os serviços
.\docker.ps1 up

# Ou usando comandos diretos
docker-compose up -d
```

#### Linux/Mac
```bash
# Tornar o script executável
chmod +x docker.sh

# Construir e iniciar todos os serviços
./docker.sh up

# Ou usando comandos diretos
docker-compose up -d
```

### 3. Verificar se os Serviços Estão Funcionando

- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Seq (Logs)**: http://localhost:5341
- **SQL Server**: localhost:1433 (user: sa, password: YourStrong!Passw0rd)
- **Redis**: localhost:6379

## 🔧 Scripts de Automação

### Windows (PowerShell)

```powershell
# Mostrar ajuda
.\docker.ps1 help

# Construir imagem
.\docker.ps1 build

# Iniciar serviços
.\docker.ps1 up

# Parar serviços
.\docker.ps1 down

# Reiniciar serviços
.\docker.ps1 restart

# Ver logs
.\docker.ps1 logs

# Executar migrações
.\docker.ps1 db-migrate

# Ver status
.\docker.ps1 status

# Limpeza completa
.\docker.ps1 clean
```

### Linux/Mac (Bash)

```bash
# Mostrar ajuda
./docker.sh help

# Construir imagem
./docker.sh build

# Iniciar serviços
./docker.sh up

# Parar serviços
./docker.sh down

# Reiniciar serviços
./docker.sh restart

# Ver logs
./docker.sh logs

# Executar migrações
./docker.sh db-migrate

# Ver status
./docker.sh status

# Limpeza completa
./docker.sh clean
```

## 🐳 Serviços Incluídos

### 1. Payment Processing API
- **Porta**: 5000 (HTTP), 5001 (HTTPS)
- **Healthcheck**: Ativo
- **Logs**: Enviados para console, arquivo e Seq

### 2. SQL Server 2022 Express
- **Porta**: 1433
- **Usuário**: sa
- **Senha**: YourStrong!Passw0rd
- **Volume persistente**: sqlserver_data

### 3. Redis Cache
- **Porta**: 6379
- **Volume persistente**: redis_data

### 4. Seq (Log Aggregation)
- **Porta**: 5341
- **Volume persistente**: seq_data
- **Interface Web**: http://localhost:5341

## 🔐 Configuração de Segurança

### Princípios de Segurança Implementados

1. **Sem Secrets em Código**: Todas as informações sensíveis estão no Azure Key Vault
2. **Separação por Ambiente**: Configurações diferentes para desenvolvimento, Docker e produção
3. **Autenticação Robusta**: Suporte a Managed Identity e Service Principal
4. **Rotação de Secrets**: Key Vault permite rotação automática de secrets
5. **Auditoria**: Todas as acessos aos secrets são logados no Azure

### Ambientes de Configuração

| Ambiente | Arquivo de Config | Descrição |
|----------|------------------|-----------|
| `Development` | `appsettings.Development.json` | Desenvolvimento local com IIS Express |
| `LocalDevelopment` | `appsettings.LocalDevelopment.json` | Desenvolvimento local sem secrets |
| `Docker` | `appsettings.Docker.json` | Execução em containers Docker |
| `Production` | `appsettings.json` + Key Vault | Produção com Azure Key Vault |

### Variáveis de Ambiente Importantes

**Obrigatórias para Produção:**
```bash
AZURE_KEYVAULT_URI=https://your-keyvault.vault.azure.net/
```

**Opcionais (quando não usar Managed Identity):**
```bash
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
AZURE_TENANT_ID=your-tenant-id
```

**Apenas para Desenvolvimento Local:**
```bash
DB_SA_PASSWORD=YourStrong!Passw0rd
```

## 📊 Monitoramento

### Health Checks
A aplicação inclui health checks para:
- Conectividade com banco de dados
- Status da aplicação
- Disponibilidade dos serviços

Acesse: http://localhost:5000/health

### Logs
Os logs são enviados para múltiplos destinos:
- **Console**: Visível com `docker-compose logs`
- **Arquivo**: Armazenado em `/app/logs/` dentro do container
- **Seq**: Interface web em http://localhost:5341

### Métricas
- CPU e memória via Docker stats
- Health checks automáticos
- Rate limiting configurado

## 🔄 Desenvolvimento

### Desenvolvimento Local
Para desenvolvimento, você pode montar o código fonte:

```yaml
# Adicionar ao service payment-api no docker-compose.yml
volumes:
  - ./src:/app/src
```

### Debug
Para debug, exponha a porta de debug:

```yaml
# Adicionar ao service payment-api
ports:
  - "5000:8080"
  - "5001:8081"
  - "5005:5005"  # Debug port
```

## 🗄️ Banco de Dados

### Migrações
```bash
# Windows
.\docker.ps1 db-migrate

# Linux/Mac
./docker.sh db-migrate
```

### Acesso Direto
```bash
# Conectar ao SQL Server
docker exec -it payment-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd"

# Conectar ao Redis
docker exec -it payment-redis redis-cli
```

## 🧹 Limpeza

### Parar Serviços
```bash
docker-compose down
```

### Limpeza Completa (Remove volumes e dados)
```bash
# Windows
.\docker.ps1 clean

# Linux/Mac
./docker.sh clean
```

## 🚨 Troubleshooting

### Problemas Comuns

1. **Porta já em uso**
   - Verifique se as portas 5000, 1433, 6379, 5341 estão livres
   - Altere as portas no docker-compose.yml se necessário

2. **Erro de memória no SQL Server**
   - Certifique-se de ter pelo menos 4GB de RAM disponível
   - Ajuste os limites de memória no docker-compose.yml

3. **Erro de conexão com banco**
   - Aguarde o SQL Server inicializar completamente (pode levar 1-2 minutos)
   - Verifique os logs: `docker-compose logs sqlserver`

4. **Aplicação não inicia**
   - Verifique as variáveis de ambiente no arquivo .env
   - Verifique os logs: `docker-compose logs payment-api`

### Logs de Debug
```bash
# Ver logs de todos os serviços
docker-compose logs

# Ver logs de um serviço específico
docker-compose logs payment-api
docker-compose logs sqlserver
docker-compose logs redis

# Seguir logs em tempo real
docker-compose logs -f payment-api
```

## 🔧 Customização

### Configurações Personalizadas
Edite o arquivo `appsettings.Docker.json` para configurações específicas do Docker.

### Variáveis de Ambiente
Todas as configurações podem ser sobrescritas via variáveis de ambiente no arquivo `.env` ou no `docker-compose.yml`.

### Volumes Adicionais
Adicione volumes personalizados no `docker-compose.yml` conforme necessário.

---

## 📝 Notas

- O ambiente Docker usa o profile `Docker` do ASP.NET Core
- Os certificados HTTPS são auto-assinados (adequado apenas para desenvolvimento)
- Para produção, configure certificados SSL válidos
- Mantenha as senhas e chaves seguras em produção