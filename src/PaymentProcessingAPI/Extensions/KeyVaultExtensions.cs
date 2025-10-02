using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Serilog;

namespace PaymentProcessingAPI.Extensions;

public static class KeyVaultExtensions
{
    public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            try
            {
                var credential = GetAzureCredential(builder.Configuration);
                
                builder.Configuration.AddAzureKeyVault(
                    new Uri(keyVaultUri),
                    credential,
                    new AzureKeyVaultConfigurationOptions 
                    { 
                        ReloadInterval = TimeSpan.FromMinutes(5)
                    }
                );
                
                Log.Information("Azure Key Vault configurado com sucesso: {VaultUri}", keyVaultUri);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao configurar Azure Key Vault: {VaultUri}", keyVaultUri);
                // Em desenvolvimento, continua sem Key Vault
                if (builder.Environment.IsProduction())
                {
                    throw;
                }
            }
        }
        else
        {
            Log.Warning("AzureKeyVault:VaultUri nao definido. Key Vault nao sera carregado.");
        }

        return builder;
    }
    
    private static Azure.Core.TokenCredential GetAzureCredential(IConfiguration configuration)
    {
        // Priorizar Managed Identity em produção
        var clientId = configuration["AZURE_CLIENT_ID"];
        var clientSecret = configuration["AZURE_CLIENT_SECRET"];
        var tenantId = configuration["AZURE_TENANT_ID"];
        
        if (!string.IsNullOrWhiteSpace(clientId) && 
            !string.IsNullOrWhiteSpace(clientSecret) && 
            !string.IsNullOrWhiteSpace(tenantId))
        {
            Log.Information("Usando ClientSecretCredential para autenticação no Key Vault");
            return new ClientSecretCredential(tenantId, clientId, clientSecret);
        }
        
        Log.Information("Usando DefaultAzureCredential para autenticação no Key Vault");
        return new DefaultAzureCredential();
    }

    public static async Task<WebApplicationBuilder> LoadEventGridSecretsAsync(this WebApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
        string? egEndpoint = builder.Configuration["EventGrid:TopicEndpoint"];
        string? egKey = builder.Configuration["EventGrid:AccessKey"];

        if ((string.IsNullOrWhiteSpace(egEndpoint) || string.IsNullOrWhiteSpace(egKey)) && !string.IsNullOrWhiteSpace(keyVaultUri))
        {
            var credential = new DefaultAzureCredential();
            var secretClient = new SecretClient(new Uri(keyVaultUri), credential);

            try
            {
                var endpointSecret = await secretClient.GetSecretAsync("EventGrid--TopicEndpoint");
                var keySecret = await secretClient.GetSecretAsync("EventGrid--AccessKey");

                egEndpoint = endpointSecret.Value.Value;
                egKey = keySecret.Value.Value;

                if (!string.IsNullOrWhiteSpace(egEndpoint))
                    builder.Configuration["EventGrid:TopicEndpoint"] = egEndpoint;

                if (!string.IsNullOrWhiteSpace(egKey))
                    builder.Configuration["EventGrid:AccessKey"] = egKey;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao carregar segredos do EventGrid diretamente do Key Vault.");
            }
        }

        return builder;
    }
}