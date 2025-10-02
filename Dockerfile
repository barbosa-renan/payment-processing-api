# Dockerfile multi-stage para aplicação .NET 8
# Estágio 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar arquivos de projeto e restaurar dependências
COPY PaymentProcessingAPI.sln ./
COPY src/PaymentProcessingAPI/*.csproj ./src/PaymentProcessingAPI/
COPY src/PaymentProcessingAPI.Tests/*.csproj ./src/PaymentProcessingAPI.Tests/
COPY src/PaymentProcessingAPI.IntegrationTests/*.csproj ./src/PaymentProcessingAPI.IntegrationTests/

# Restaurar dependências
RUN dotnet restore

# Copiar todo o código fonte
COPY . .

# Build da aplicação
WORKDIR /app/src/PaymentProcessingAPI
RUN dotnet build -c Release --no-restore

# Publicar a aplicação
RUN dotnet publish -c Release --no-restore -o /app/publish

# Estágio 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Criar usuário não-root para segurança
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

# Copiar arquivos publicados do estágio de build
COPY --from=build /app/publish .

# Expor porta da aplicação
EXPOSE 8080
EXPOSE 8081

# Configurar variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Comando de inicialização
ENTRYPOINT ["dotnet", "PaymentProcessingAPI.dll"]