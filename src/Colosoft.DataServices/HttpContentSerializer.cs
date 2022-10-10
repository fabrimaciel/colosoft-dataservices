using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class HttpContentSerializer : IHttpContentSerializer
    {
        private readonly JsonSerializerOptions jsonSerializerOptions;

        public HttpContentSerializer()
            : this(new JsonSerializerOptions())
        {
            this.jsonSerializerOptions.PropertyNameCaseInsensitive = true;
            this.jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            this.jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
            this.jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        public HttpContentSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        public async Task<T> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken)
        {
            var item = await content.ReadFromJsonAsync<T>(this.jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            return item!;
        }
    }
}
