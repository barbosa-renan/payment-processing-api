# Configuração do Azure Key Vault

Este documento descreve como configurar o Azure Key Vault para armazenar a connection string do Service Bus de forma segura.

## Configuração Atual

O projeto foi configurado para carregar a connection string do Service Bus diretamente do Azure Key Vault usando Managed Identity.

### Secret no Key Vault

- **Nome do Secret**: `ServiceBus--ConnectionString`
- **Valor**: `<valor-da-connection-aqui>`
- **Vault URI**: `<uri-aqui>`

### Como Funciona

1. O Azure Key Vault é configurado no `Program.cs` usando `DefaultAzureCredential`
2. O secret `ServiceBus--ConnectionString` é automaticamente mapeado para `ServiceBus:ConnectionString` na configuração
3. O `ServiceBusClient` usa essa configuração para se conectar ao Service Bus

## Configuração para Ambiente de Desenvolvimento

### Opção 1: Usar Azure Key Vault (Recomendado)

1. Instale o Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
2. Faça login no Azure: `az login`
3. Configure as permissões necessárias no Key Vault para seu usuário
4. O projeto automaticamente carregará os secrets do Key Vault

### Opção 2: Configuração Local

Para desenvolvimento local sem Key Vault, você pode configurar a connection string diretamente no `appsettings.Development.json`:

```json
{
  "AzureKeyVault": {
    "VaultUri": ""
  },
  "ServiceBus": {
    "ConnectionString": "sua-connection-string-aqui"
  }
}
```

## Configuração para Produção

### Managed Identity

O projeto usa `DefaultAzureCredential` que automaticamente detecta:

1. **Managed Identity** (quando rodando no Azure)
2. **Azure CLI** (desenvolvimento local)
3. **Visual Studio** (desenvolvimento local)
4. **Environment Variables** (CI/CD)

### Permissões Necessárias

A Managed Identity ou usuário precisa ter as seguintes permissões no Key Vault:

- `Key Vault Secrets User` role ou
- Política de acesso com permissão `Get` para secrets

## Variáveis de Ambiente

Para CI/CD ou outros cenários, você pode usar essas variáveis:

```bash
AZURE_CLIENT_ID=<managed-identity-client-id>
AZURE_TENANT_ID=<tenant-id>
AZURE_CLIENT_SECRET=<client-secret> # Apenas para Service Principal
```

## Validação

Para validar se a configuração está funcionando:

1. Inicie a aplicação
2. Verifique os logs para confirmar que não há erros de autenticação
3. Teste os endpoints que usam Service Bus

## Troubleshooting

### Erro: "ServiceBus connection string not found"

- Verifique se o secret `ServiceBus--ConnectionString` existe no Key Vault
- Verifique se você tem permissões para acessar o Key Vault
- Confirme se a `VaultUri` está correta no `appsettings.json`

### Erro de Autenticação

- Execute `az login` para autenticar localmente
- Verifique se a Managed Identity está configurada corretamente no Azure
- Confirme as permissões no Key Vault

### Para Desenvolvimento Local sem Azure

- Configure a connection string diretamente no `appsettings.Development.json`
- Deixe `VaultUri` vazio para desabilitar o Key Vault