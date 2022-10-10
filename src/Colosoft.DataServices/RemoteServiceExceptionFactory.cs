using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class RemoteServiceExceptionFactory : IRemoteServiceExceptionFactory
    {
        public async Task<Exception?> Create(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError &&
                response.Content.Headers.ContentLength > 0 &&
                response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var message = System.Text.Json.JsonSerializer.Deserialize<ErrorMessage>(content);
                    return new RemoteServiceException(message!.Message);
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        private sealed class ErrorMessage
        {
            [JsonPropertyName("codigo")]
            public int Code { get; set; }

            [JsonPropertyName("mensagem")]
            public string? Message { get; set; }
        }
    }
}
