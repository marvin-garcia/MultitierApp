using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(BackendApi.Startup))]
namespace BackendApi
{
    public class Startup : FunctionsStartup
    {
        public IConfiguration Configuration { get; }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            try
            {
                builder.Services.AddLogging();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
