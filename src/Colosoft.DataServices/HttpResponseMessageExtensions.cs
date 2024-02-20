using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Colosoft.DataServices
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<T?> ReadAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    if (stream.Length > 0)
                    {
                        return (await JsonSerializer.DeserializeAsync<T>(
                            stream,
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                PropertyNameCaseInsensitive = true,
                            },
                            cancellationToken)) !;
                    }
                    else
                    {
                        return default;
                    }
                }
            }
            catch
            {
                var responseAsString = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseAsString))
                {
                    if (response.Content?.Headers?.ContentType?.MediaType == "application/json")
                    {
                        JsonDocument json;
                        try
                        {
                            json = JsonDocument.Parse(responseAsString);
                        }
                        catch
                        {
                            throw new InvalidOperationException("Unable to parse the response.");
                        }

                        JsonElement error;
                        if (json.RootElement.TryGetProperty("error", out error))
                        {
                            JsonElement message;
                            if (error.TryGetProperty("message", out message))
                            {
                                throw new InvalidOperationException(message.GetString());
                            }
                        }
                    }
                    else
                    {
                        XElement? error = null;
                        try
                        {
                            var xml = XDocument.Parse(responseAsString);
                            var innerException = xml.Descendants().SingleOrDefault(p => p.Name.LocalName == "internalexception");
                            if (innerException != null)
                            {
                                error = innerException;
                            }
                            else
                            {
                                error = xml.Descendants().SingleOrDefault(p => p.Name.LocalName == "error");
                            }
                        }
                        catch
                        {
                            throw new InvalidOperationException("Unable to parse the response.");
                        }

                        if (error != null)
                        {
                            throw new InvalidOperationException(error.Value);
                        }
                    }
                }

                throw;
            }
        }

        internal static int? GetTotalCount(this HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-Total-Count", out var headerValues) &&
                int.TryParse(headerValues.FirstOrDefault(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var totalCount))
            {
                return totalCount;
            }

            return null;
        }

        internal static LinkHeader GetLinkHeader(this HttpResponseMessage response)
        {
            LinkHeader? result;

            if (response.Headers.TryGetValues("link", out var headerValues))
            {
                result = LinkHeader.LinksFromHeader(headerValues.FirstOrDefault());
            }
            else
            {
                result = new LinkHeader
                {
                    FirstLink = response.RequestMessage?.RequestUri?.ToString(),
                };
            }

            return result ?? new LinkHeader();
        }
    }
}
