using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public class RemoteServiceExceptionFactory : IRemoteServiceExceptionFactory
    {
        protected virtual IEnumerable<HttpStatusCode> StatusCodes { get; } = new[]
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadRequest,
        };

        public async Task<Exception?> Create(HttpResponseMessage response)
        {
            if (this.StatusCodes.Contains(response.StatusCode) &&
                response.Content.Headers.ContentLength > 0 &&
                response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var content = await response.Content.ReadAsStreamAsync();
                try
                {
                    var message = await System.Text.Json.JsonSerializer.DeserializeAsync<ErrorMessage>(
                        content,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = false,
                        });

                    return new RemoteServiceException(message);
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
    }
}
