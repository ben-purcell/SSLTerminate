using System.Text.Json;

namespace SSLTerminate.ACME.Common
{
    class Json
    {
        public static JsonSerializerOptions JsonSerializerOptions => new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            IgnoreNullValues = true,
        };

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        }

        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize<object>(obj, JsonSerializerOptions);
        }
    }
}
