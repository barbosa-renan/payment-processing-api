# Script PowerShell para facilitar o uso do Docker com a Payment Processing API

param(
    [Parameter(Position=0)]
    [string]$Command = "help",
    
    [Parameter()]
    [string]$KeyVaultName,
    
    [Parameter()]
    [string]$ResourceGroupName
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

function Show-Help {
    Write-Host "Uso: .\docker.ps1 [COMANDO]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Comandos disponíveis:" -ForegroundColor Cyan
    Write-Host "  build       - Constrói a imagem Docker"
    Write-Host "  up          - Inicia todos os serviços"
    Write-Host "  down        - Para todos os serviços"
    Write-Host "  restart     - Reinicia todos os serviços"
    Write-Host "  logs        - Mostra os logs da aplicação"
    Write-Host "  db-migrate  - Executa as migrações do banco de dados"
    Write-Host "  clean       - Remove containers, imagens e volumes"
    Write-Host "  status      - Mostra o status dos containers"
    Write-Host "  setup-kv    - Configura Azure Key Vault"
    Write-Host "  help        - Mostra esta ajuda"
    Write-Host ""
    Write-Host "Exemplos:" -ForegroundColor Yellow
    Write-Host "  .\docker.ps1 setup-kv -KeyVaultName 'meu-keyvault' -ResourceGroupName 'meu-rg'"
    Write-Host "  .\docker.ps1 up"
}

function Build-Image {
    Write-Info "Construindo imagem Docker..."
    docker-compose build --no-cache
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Imagem construída com sucesso!"
    } else {
        Write-Error-Custom "Erro ao construir a imagem."
        exit 1
    }
}

function Start-Services {
    Write-Info "Iniciando serviços..."
    
    # Verificar se arquivo .env existe
    if (-not (Test-Path ".env")) {
        Write-Warning-Custom "Arquivo .env não encontrado. Criando a partir do .env.example..."
        if (Test-Path ".env.example") {
            Copy-Item ".env.example" ".env"
            Write-Warning-Custom "Configure as variáveis no arquivo .env antes de prosseguir."
            Write-Warning-Custom "IMPORTANTE: Configure o AZURE_KEYVAULT_URI para usar secrets do Azure Key Vault."
        }
    }
    
    # Verificar configuração do Key Vault
    $envContent = Get-Content ".env" -ErrorAction SilentlyContinue
    $keyVaultUri = $envContent | Where-Object { $_ -match "^AZURE_KEYVAULT_URI=" } | ForEach-Object { $_.Split("=")[1] }
    
    if ([string]::IsNullOrWhiteSpace($keyVaultUri) -or $keyVaultUri -eq "https://your-keyvault.vault.azure.net/") {
        Write-Warning-Custom "Azure Key Vault não está configurado."
        Write-Warning-Custom "Para produção, execute: .\setup-keyvault.ps1 -KeyVaultName 'seu-keyvault' -ResourceGroupName 'seu-rg'"
        Write-Warning-Custom "Para desenvolvimento local, use ASPNETCORE_ENVIRONMENT=LocalDevelopment"
    }
    
    docker-compose up -d
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Serviços iniciados!"
        Write-Info "API disponível em: http://localhost:5000"
        Write-Info "Swagger disponível em: http://localhost:5000/swagger"
        Write-Info "Seq (logs) disponível em: http://localhost:5341"
        Write-Info "Health Check: http://localhost:5000/health"
    } else {
        Write-Error-Custom "Erro ao iniciar os serviços."
    }
}

function Stop-Services {
    Write-Info "Parando serviços..."
    docker-compose down
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Serviços parados!"
    }
}

function Restart-Services {
    Write-Info "Reiniciando serviços..."
    docker-compose restart
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Serviços reiniciados!"
    }
}

function Show-Logs {
    Write-Info "Mostrando logs da aplicação..."
    docker-compose logs -f payment-api
}

function Run-Migrations {
    Write-Info "Executando migrações do banco de dados..."
    docker-compose exec payment-api dotnet ef database update
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Migrações executadas!"
    } else {
        Write-Error-Custom "Erro ao executar migrações."
    }
}

function Clean-All {
    Write-Warning-Custom "Esta operação irá remover todos os containers, imagens e volumes relacionados."
    $confirmation = Read-Host "Tem certeza? (y/N)"
    
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
        Write-Info "Removendo containers..."
        docker-compose down -v --remove-orphans
        
        Write-Info "Removendo imagens..."
        $images = docker images "*payment*" -q
        if ($images) {
            docker rmi $images
        }
        
        Write-Info "Removendo volumes..."
        docker volume prune -f
        
        Write-Info "Limpeza concluída!"
    } else {
        Write-Info "Operação cancelada."
    }
}

function Setup-KeyVault {
    if ([string]::IsNullOrWhiteSpace($KeyVaultName) -or [string]::IsNullOrWhiteSpace($ResourceGroupName)) {
        Write-Error-Custom "KeyVaultName e ResourceGroupName são obrigatórios."
        Write-Info "Uso: .\docker.ps1 setup-kv -KeyVaultName 'meu-keyvault' -ResourceGroupName 'meu-rg'"
        return
    }
    
    Write-Info "Executando configuração do Azure Key Vault..."
    & ".\setup-keyvault.ps1" -KeyVaultName $KeyVaultName -ResourceGroupName $ResourceGroupName
}

function Show-Status {
    Write-Info "Status dos containers:"
    docker-compose ps
}

# Verificar se Docker está instalado
try {
    docker --version | Out-Null
} catch {
    Write-Error-Custom "Docker não está instalado. Instale o Docker antes de continuar."
    exit 1
}

try {
    docker-compose --version | Out-Null
} catch {
    Write-Error-Custom "Docker Compose não está instalado. Instale o Docker Compose antes de continuar."
    exit 1
}

# Processar comandos
switch ($Command.ToLower()) {
    "build" {
        Build-Image
    }
    "up" -or "start" {
        Start-Services
    }
    "down" -or "stop" {
        Stop-Services
    }
    "restart" {
        Restart-Services
    }
    "logs" {
        Show-Logs
    }
    "db-migrate" -or "migrate" {
        Run-Migrations
    }
    "clean" {
        Clean-All
    }
    "status" {
        Show-Status
    }
    "setup-kv" -or "setup-keyvault" {
        Setup-KeyVault
    }
    "help" -or "--help" -or "-h" {
        Show-Help
    }
    default {
        Write-Error-Custom "Comando desconhecido: $Command"
        Show-Help
        exit 1
    }
}