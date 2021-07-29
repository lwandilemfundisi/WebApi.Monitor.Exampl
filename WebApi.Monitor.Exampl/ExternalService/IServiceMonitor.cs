using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Monitor.Exampl.ExternalService.ExternalServiceModels;

namespace WebApi.Monitor.Exampl.ExternalService
{
    public interface IServiceMonitor
    {
        Task<HttpResponseMessage> LogServiceMethod(
            ServiceMethodEntryExternalModel externalModel, 
            CancellationToken cancellationToken);
    }
}
