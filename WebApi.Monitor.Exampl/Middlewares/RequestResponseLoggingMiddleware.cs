using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Monitor.Exampl.ExternalService;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Monitor.Exampl.ExternalService.ExternalServiceModels;
using System.Threading;
using Microsoft.Extensions.Configuration;
using WebApi.Monitor.Exampl.Enums;

namespace WebApi.Monitor.Exampl.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var serviceMonitor = serviceProvider.GetService<IServiceMonitor>();
            var request = await FormatRequest(context.Request);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;
                    await _next(context);
                    var elapsed = stopwatch.Elapsed;
                    var response = await FormatResponse(context.Response);
                    
                    await serviceMonitor.LogServiceMethod(
                        new ServiceMethodEntryExternalModel 
                        {
                            ServiceId = configuration["ServiceMonitor:ServiceId"],
                            MethodName = request.Key,
                            RequestUri = request.Value,
                            Response = response.Key,
                            MethodExecutionTime = DateTime.Now,
                            ElapsedTime = stopwatch.Elapsed,
                            ExecutionsStatus = response.Value.ToString()
                        }, CancellationToken.None);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception e)
            {
                await serviceMonitor.LogServiceMethod(
                        new ServiceMethodEntryExternalModel
                        {
                            ServiceId = configuration["ServiceMonitor:ServiceId"],
                            MethodName = request.Key,
                            RequestUri = request.Value,
                            Response = e.ToString(),
                            MethodExecutionTime = DateTime.Now,
                            ElapsedTime = stopwatch.Elapsed,
                            ExecutionsStatus = ExecutionStatus.Ex_Fail.ToString()
                        }, CancellationToken.None);

                throw;
            }
        }

        private async Task<KeyValuePair<string, string>> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);
            return new KeyValuePair<string, string>(request.Path, $"{request.Scheme}://{request.Host}{request.Path} {request.QueryString} {body}");
        }

        private async Task<KeyValuePair<string, ExecutionStatus>> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return new KeyValuePair<string, ExecutionStatus>(
                $"{response.StatusCode}: {text}",
                (response.StatusCode >= 400) && (response.StatusCode <= 599)
                ? ExecutionStatus.Ex_Fail :
                  ExecutionStatus.Ex_Suc);
        }
    }
}
