using LogisticsPlatform.Common.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogisticsPlatform.Testing.Extensions.Extensions
{
    public class JsonSerializerExtensions
    {
        public static JsonSerializerOptions DefaultSerializerOptions => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(),
                new DateOnlyConverter(),
                new TimeOnlyConverter()
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static JsonSerializerOptions SerializationOptionsWithNullValues
        {
            get
            {
                var serialziationOptions = DefaultSerializerOptions;
                serialziationOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
                return serialziationOptions;
            }
        }
    }
}
