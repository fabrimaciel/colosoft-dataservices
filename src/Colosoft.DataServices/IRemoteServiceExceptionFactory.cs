using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    public interface IRemoteServiceExceptionFactory
    {
        Task<Exception?> Create(HttpResponseMessage response);
    }
}
