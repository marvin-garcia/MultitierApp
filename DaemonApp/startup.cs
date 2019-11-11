using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Common.Services;
using Common.Interfaces;

[assembly: FunctionsStartup(typeof(DaemonApp.Startup))]
namespace DaemonApp
{
    public class Startup : FunctionsStartup
    {
        public IConfiguration Configuration { get; }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            try
            {
                builder.Services
                    .AddLogging()
                    .AddSingleton<IHttpClient, StandardHttpClient>()
                    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
