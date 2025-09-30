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
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential(),
                new AzureKeyVaultConfigurationOptions { ReloadInterval = TimeSpan.FromMinutes(5) }
            );
        }
        else
        {
            Log.Warning("AzureKeyVault:VaultUri nao definido. Key Vault nao sera carregado.");
        }

        return builder;
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