using Refit;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Colosoft.DataServices.Refit
{
    public sealed class ApiResponse<T> : IApiResponse<T>
    {
        private readonly HttpResponseMessage response;
        private bool disposed;

        public ApiResponse(
            HttpResponseMessage response,
            T content,
            RefitSettings settings,
            ApiException? error = null)
        {
            this.response = response ?? throw new ArgumentNullException(nameof(response));
            this.Error = error;
            this.Content = content;
            this.Settings = settings;
        }

        public T Content { get; }

        public RefitSettings Settings { get; }

        public HttpResponseHeaders Headers => this.response.Headers;

        public HttpContentHeaders? ContentHeaders => this.response.Content?.Headers;

        public bool IsSuccessStatusCode => this.response.IsSuccessStatusCode;

        public string? ReasonPhrase => this.response.ReasonPhrase;

        public HttpRequestMessage? RequestMessage => this.response.RequestMessage;

        public HttpStatusCode StatusCode => this.response.StatusCode;

        public Version Version => this.response.Version;

        public ApiException? Error { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<ApiResponse<T>> EnsureSuccessStatusCodeAsync()
        {
            if (!this.IsSuccessStatusCode)
            {
                var exception = await ApiException.Create(
                    this.response.RequestMessage!,
                    this.response.RequestMessage!.Method,
                    this.response,
                    this.Settings).ConfigureAwait(false);

                this.Dispose();

                throw exception;
            }

            return this;
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.response.Dispose();
        }
    }
}
