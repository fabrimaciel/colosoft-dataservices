using Refit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices.Refit
{
    internal class RequestBuilder<TApi> : RequestBuilder, IRequestBuilder<TApi>
    {
        private readonly IServiceResultFactory serviceResultFactory;

        public RequestBuilder(IServiceResultFactory serviceResultFactory, RefitSettings? refitSettings = null)
            : base(typeof(TApi), refitSettings)
        {
            this.serviceResultFactory = serviceResultFactory;
        }

        protected override async Task<T> DeserializeContentAsync<T>(RestMethodInfo restMethod, HttpResponseMessage resp, HttpContent content, CancellationToken cancellationToken)
        {
            var returnType = typeof(T);

            if (returnType.IsGenericType)
            {
                var genericDefinition = returnType.GetGenericTypeDefinition();
                if (typeof(IPagedResult<>).IsAssignableFrom(genericDefinition) ||
                     typeof(ISortedResult<>).IsAssignableFrom(genericDefinition))
                {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
                    var deserializeMethod = typeof(RequestBuilder<TApi>)
                        .GetMethod(nameof(this.DeserializeServiceResult), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) !
                        .MakeGenericMethod(returnType.GetGenericArguments()[0]);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

                    object result = await (Task<System.Collections.IEnumerable>)deserializeMethod.Invoke(this, new object[] { resp, cancellationToken }) !;
                    return (T)result;
                }
                else if (typeof(IEnumerable<>).IsAssignableFrom(genericDefinition) && resp.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return (T)typeof(Enumerable)
                        .GetMethod("Empty", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public) !
                        .MakeGenericMethod(returnType.GetGenericArguments().First())
                        .Invoke(null, null) !;
                }
            }

            return await base.DeserializeContentAsync<T>(restMethod, resp, content, cancellationToken);
        }

        private async Task<System.Collections.IEnumerable> DeserializeServiceResult<T>(HttpResponseMessage responseMessage, CancellationToken cancellationToken) =>
            await this.serviceResultFactory.Create<T>(responseMessage, cancellationToken);
    }
}