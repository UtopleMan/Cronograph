using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonOptions
{
    private static JsonSerializerOptions options;
    public static JsonSerializerOptions Options
    {
        get
        {
            if (options != null)
                return options;

            options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}