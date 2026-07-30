# Infraestrutura Azure com Bicep

Este diretório substitui a criacao manual de recursos Azure por deploy declarativo com Bicep.

## O que esta provisionado

- Resource Group
- Log Analytics Workspace
- Application Insights
- Key Vault com RBAC habilitado
- Service Bus Namespace + filas principais
- Event Grid Topic
- Storage Account
- SQL Server + Database
- App Service Plan Linux
- Web App (API)
- Function App
- Role assignment Key Vault Secrets User para as identities da Web App e Function App

## Estrutura

- `main.bicep`: orquestracao principal
- `modules/`: modulos por recurso
- `params/dev.bicepparam`: parametros dev
- `params/hml.bicepparam`: parametros hml
- `params/prod.bicepparam`: parametros prod
- `deploy.ps1`: script unico de deploy

## Pre-requisitos

1. Azure CLI instalado
2. Bicep habilitado no Azure CLI
3. Login ativo no Azure

Comandos:

```powershell
az login
az account set --subscription "<subscription-id-ou-nome>"
```

## Deploy rapido

```powershell
cd infra/bicep
./deploy.ps1 -Environment dev
```

Para visualizar mudancas sem aplicar:

```powershell
./deploy.ps1 -Environment dev -WhatIf
```

## Observacoes importantes

1. A senha do SQL e solicitada no momento do deploy e nao fica salva nos arquivos de parametros.
2. Os nomes de recursos nos arquivos `*.bicepparam` devem ser globalmente unicos quando exigido (ex.: Storage, Service Bus, Web App).
3. O template esta otimizado para velocidade de adocao. Hardening adicional recomendado para producao:
   - Private Endpoints
   - Desabilitar acesso publico em recursos de dados
   - RBAC detalhado no Service Bus
   - Politicas de seguranca e governanca

## Proximos passos de evolucao

1. Adicionar private endpoints e VNet integration
2. Adicionar Event Grid subscriptions declarativas
3. Parametrizar app settings da API e Functions com referencias ao Key Vault
4. Criar pipeline CI/CD com `what-if` + aprovacao manual para hml/prod
