namespace PaymentProcessingAPI.Configuration;

public class EventGridConfiguration
{
    public string Endpoint { get; set; } = string.Empty; // Virá do Key Vault: EventGrid--Endpoint
    public string AccessKey { get; set; } = string.Empty; // Virá do Key Vault: EventGrid--AccessKey
    public string DataVersion { get; set; } = "1.0";
}