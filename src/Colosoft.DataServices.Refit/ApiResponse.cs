using Refit;
using System;
using System.Net.Http;

namespace Colosoft.DataServices.Refit
{
    internal static class ApiResponse
    {
        public static T Create<T, TBody>(HttpResponseMessage resp, object? content, RefitSettings settings, ApiException? error = null)
        {
            return (T)Activator.CreateInstance(typeof(ApiResponse<TBody>), resp, content, settings, error) !;
        }
    }
}