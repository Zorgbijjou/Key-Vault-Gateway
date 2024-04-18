using System.Text.Json.Serialization;

namespace KeyStoreApi.Health;

public sealed class ServiceStatusResponse {
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ServiceHealth Status { get; set; }

    public required string Description { get; set; }
}