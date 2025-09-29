namespace PaymentProcessingAPI.Configuration;

public class EventGridConfiguration
{
    [ConfigurationKeyName("TopicEndpoint")]
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string DataVersion { get; set; } = "1.0";
}