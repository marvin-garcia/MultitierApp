using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Common.Interfaces;
using Common.Services;

[assembly: FunctionsStartup(typeof(MidtierApi.Startup))]
namespace MidtierApi
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
                    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                    .AddSingleton<IAuthToken, AuthToken>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}