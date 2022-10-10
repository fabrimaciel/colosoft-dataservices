using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public static class HttpResponseMessageErrorExtensions
    {
        public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if ((response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.StatusCode != System.Net.HttpStatusCode.Accepted) &&
                response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var error = await JsonSerializer.DeserializeAsync<ErrorMessage>(stream, cancellationToken: cancellationToken);

                    if (error != null)
                    {
                        throw new InvalidOperationException(error.Message);
                    }

                    throw new InvalidOperationException("Server invalid response");
                }
            }
            else
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
