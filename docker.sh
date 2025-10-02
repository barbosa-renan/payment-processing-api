#!/bin/bash

# Script para facilitar o uso do Docker com a Payment Processing API

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

show_help() {
    echo "Uso: ./docker.sh [COMANDO]"
    echo ""
    echo "Comandos disponíveis:"
    echo "  build     - Constrói a imagem Docker"
    echo "  up        - Inicia todos os serviços"
    echo "  down      - Para todos os serviços"
    echo "  restart   - Reinicia todos os serviços"
    echo "  logs      - Mostra os logs da aplicação"
    echo "  db-migrate - Executa as migrações do banco de dados"
    echo "  clean     - Remove containers, imagens e volumes"
    echo "  status    - Mostra o status dos containers"
    echo "  help      - Mostra esta ajuda"
}

build_image() {
    print_info "Construindo imagem Docker..."
    docker-compose build --no-cache
    print_info "Imagem construída com sucesso!"
}

start_services() {
    print_info "Iniciando serviços..."
    
    # Verificar se arquivo .env existe
    if [ ! -f .env ]; then
        print_warning "Arquivo .env não encontrado. Criando a partir do .env.example..."
        cp .env.example .env
        print_warning "Configure as variáveis no arquivo .env antes de prosseguir."
    fi
    
    docker-compose up -d
    print_info "Serviços iniciados!"
    print_info "API disponível em: http://localhost:5000"
    print_info "Swagger disponível em: http://localhost:5000/swagger"
    print_info "Seq (logs) disponível em: http://localhost:5341"
}

stop_services() {
    print_info "Parando serviços..."
    docker-compose down
    print_info "Serviços parados!"
}

restart_services() {
    print_info "Reiniciando serviços..."
    docker-compose restart
    print_info "Serviços reiniciados!"
}

show_logs() {
    print_info "Mostrando logs da aplicação..."
    docker-compose logs -f payment-api
}

run_migrations() {
    print_info "Executando migrações do banco de dados..."
    docker-compose exec payment-api dotnet ef database update
    print_info "Migrações executadas!"
}

clean_all() {
    print_warning "Esta operação irá remover todos os containers, imagens e volumes relacionados."
    read -p "Tem certeza? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Removendo containers..."
        docker-compose down -v --remove-orphans
        
        print_info "Removendo imagens..."
        docker rmi $(docker images "*payment*" -q) 2>/dev/null || true
        
        print_info "Removendo volumes..."
        docker volume prune -f
        
        print_info "Limpeza concluída!"
    else
        print_info "Operação cancelada."
    fi
}

show_status() {
    print_info "Status dos containers:"
    docker-compose ps
}

# Verificar se Docker está instalado
if ! command -v docker &> /dev/null; then
    print_error "Docker não está instalado. Instale o Docker antes de continuar."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    print_error "Docker Compose não está instalado. Instale o Docker Compose antes de continuar."
    exit 1
fi

# Processar comandos
case "${1:-help}" in
    build)
        build_image
        ;;
    up|start)
        start_services
        ;;
    down|stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    logs)
        show_logs
        ;;
    db-migrate|migrate)
        run_migrations
        ;;
    clean)
        clean_all
        ;;
    status)
        show_status
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        print_error "Comando desconhecido: $1"
        show_help
        exit 1
        ;;
esac