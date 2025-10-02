# Script para configurar Azure Key Vault com as secrets necessárias para a Payment Processing API

param(
    [Parameter(Mandatory=$true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateKeyVault = $false
)

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Verificar se Azure CLI está instalado
try {
    az --version | Out-Null
} catch {
    Write-Error-Custom "Azure CLI não está instalado. Instale o Azure CLI antes de continuar."
    Write-Info "Download: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Verificar se está logado no Azure
try {
    $account = az account show --query "user.name" -o tsv
    Write-Info "Logado como: $account"
} catch {
    Write-Error-Custom "Não está logado no Azure. Execute 'az login' primeiro."
    exit 1
}

# Criar Key Vault se solicitado
if ($CreateKeyVault) {
    Write-Info "Criando Key Vault: $KeyVaultName"
    
    try {
        az keyvault create `
            --name $KeyVaultName `
            --resource-group $ResourceGroupName `
            --location $Location `
            --enable-soft-delete true `
            --enable-purge-protection true
            
        Write-Info "Key Vault criado com sucesso!"
    } catch {
        Write-Error-Custom "Erro ao criar Key Vault. Verifique se o nome não está em uso."
        exit 1
    }
}

# Verificar se o Key Vault existe
try {
    az keyvault show --name $KeyVaultName --resource-group $ResourceGroupName | Out-Null
    Write-Info "Key Vault encontrado: $KeyVaultName"
} catch {
    Write-Error-Custom "Key Vault '$KeyVaultName' não encontrado no grupo de recursos '$ResourceGroupName'."
    Write-Info "Use o parâmetro -CreateKeyVault para criar um novo Key Vault."
    exit 1
}

# Lista de secrets necessárias com descrições
$secrets = @(
    @{
        Name = "ConnectionStrings--DefaultConnection"
        Description = "Connection string do banco de dados de produção"
        Example = "Server=tcp:yourserver.database.windows.net,1433;Database=PaymentProcessingDB;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;"
    },
    @{
        Name = "AzureServiceBus--ConnectionString"
        Description = "Connection string do Azure Service Bus"
        Example = "Endpoint=sb://yournamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=yourkey"
    },
    @{
        Name = "EventGrid--TopicEndpoint"
        Description = "Endpoint do Azure Event Grid"
        Example = "https://yourtopic.eastus-1.eventgrid.azure.net/api/events"
    },
    @{
        Name = "EventGrid--AccessKey"
        Description = "Chave de acesso do Azure Event Grid"
        Example = "youraccesskey"
    },
    @{
        Name = "PaymentGateway--ApiKey"
        Description = "API Key do gateway de pagamento"
        Example = "your-payment-gateway-api-key"
    },
    @{
        Name = "PaymentGateway--BaseUrl"
        Description = "URL base do gateway de pagamento"
        Example = "https://api.paymentgateway.com"
    },
    @{
        Name = "Jwt--SecretKey"
        Description = "Chave secreta para JWT (mínimo 32 caracteres)"
        Example = "your-super-secret-jwt-key-at-least-32-characters-long"
    }
)

Write-Info "Configurando secrets no Key Vault..."
Write-Warning-Custom "Você será solicitado a inserir os valores para cada secret."
Write-Warning-Custom "Pressione ENTER para pular uma secret (manterá o valor existente se houver)."

foreach ($secret in $secrets) {
    Write-Host ""
    Write-Host "=" * 80 -ForegroundColor Cyan
    Write-Host "SECRET: $($secret.Name)" -ForegroundColor Cyan
    Write-Host "DESCRIÇÃO: $($secret.Description)" -ForegroundColor Yellow
    Write-Host "EXEMPLO: $($secret.Example)" -ForegroundColor Gray
    Write-Host "=" * 80 -ForegroundColor Cyan
    
    # Verificar se a secret já existe
    $existingSecret = $null
    try {
        $existingSecret = az keyvault secret show --vault-name $KeyVaultName --name $secret.Name --query "value" -o tsv 2>$null
        if ($existingSecret) {
            Write-Info "Secret já existe. Valor atual: $($existingSecret.Substring(0, [Math]::Min(20, $existingSecret.Length)))..."
        }
    } catch {
        # Secret não existe
    }
    
    $value = Read-Host "Digite o valor para '$($secret.Name)' (ou ENTER para pular)"
    
    if (![string]::IsNullOrWhiteSpace($value)) {
        try {
            az keyvault secret set --vault-name $KeyVaultName --name $secret.Name --value $value | Out-Null
            Write-Info "Secret '$($secret.Name)' configurada com sucesso!"
        } catch {
            Write-Error-Custom "Erro ao configurar secret '$($secret.Name)'."
        }
    } else {
        if ($existingSecret) {
            Write-Info "Mantendo valor existente para '$($secret.Name)'."
        } else {
            Write-Warning-Custom "Secret '$($secret.Name)' não foi configurada."
        }
    }
}

Write-Host ""
Write-Info "Configuração do Key Vault concluída!"
Write-Info "URI do Key Vault: https://$KeyVaultName.vault.azure.net/"

# Verificar permissões
Write-Info "Verificando suas permissões no Key Vault..."
try {
    az keyvault secret list --vault-name $KeyVaultName --query "length(@)" -o tsv | Out-Null
    Write-Info "Permissões de leitura OK."
} catch {
    Write-Warning-Custom "Você pode não ter permissões adequadas para listar secrets."
    Write-Info "Solicite ao administrador permissões de 'Key Vault Secrets User' ou superior."
}

Write-Host ""
Write-Host "=" * 80 -ForegroundColor Green
Write-Host "PRÓXIMOS PASSOS:" -ForegroundColor Green
Write-Host "=" * 80 -ForegroundColor Green
Write-Host "1. Configure a variável de ambiente AZURE_KEYVAULT_URI:"
Write-Host "   AZURE_KEYVAULT_URI=https://$KeyVaultName.vault.azure.net/"
Write-Host ""
Write-Host "2. Para aplicações locais, configure as credenciais do Azure:"
Write-Host "   AZURE_CLIENT_ID=your-client-id"
Write-Host "   AZURE_CLIENT_SECRET=your-client-secret"
Write-Host "   AZURE_TENANT_ID=your-tenant-id"
Write-Host ""
Write-Host "3. Para aplicações no Azure, use Managed Identity (recomendado)"
Write-Host ""
Write-Host "4. Atualize o arquivo .env com a URI do Key Vault"
Write-Host "=" * 80 -ForegroundColor Green