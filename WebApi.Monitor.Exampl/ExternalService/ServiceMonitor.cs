using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Monitor.Exampl.Extensions;
using WebApi.Monitor.Exampl.ExternalService.ExternalServiceModels;

namespace WebApi.Monitor.Exampl.ExternalService
{
    public class ServiceMonitor : IServiceMonitor
    {
        private readonly HttpClient _clientHttp;
        private readonly IConfiguration _configuration;

        public ServiceMonitor(HttpClient httpClient, IConfiguration configuration)
        {
            _clientHttp = httpClient;
            _configuration = configuration;
        }

        public async Task<HttpResponseMessage> LogServiceMethod(
            ServiceMethodEntryExternalModel externalModel, 
            CancellationToken cancellationToken)
        {
            return await _clientHttp.PostAsync(
                _configuration["ServiceMonitor:Uri"],
                externalModel.SerializeToJson(),
                cancellationToken);
        }
    }
}
