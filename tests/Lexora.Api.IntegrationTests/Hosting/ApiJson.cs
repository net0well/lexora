using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lexora.Api.IntegrationTests.Hosting;

public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
