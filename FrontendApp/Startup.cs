﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Interfaces;
using Common.Services;
using Microsoft.AspNetCore.Hosting;

namespace FrontendApp
{
    public class Startup
    {   
        private IConfiguration Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                var appiInsightsOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();
                // Disables adaptive sampling
                appiInsightsOptions.EnableAdaptiveSampling = false;
                // Disables QuickPulse (Live Metrics stream)
                appiInsightsOptions.EnableQuickPulseMetricStream = false;

                services.AddSingleton<IConfiguration>(Configuration)
                    .AddSingleton<IHttpClient, StandardHttpClient>()
                    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                    .AddSingleton<IAuthToken, AuthToken>()
                    .AddApplicationInsightsTelemetry(appiInsightsOptions)
                    .AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

                // Register the Swagger generator, defining one or more Swagger documents
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Info { Title = "Frontend API", Version = "v1" });
                });

                var container = new ContainerBuilder();
                container.Populate(services);

                return new AutofacServiceProvider(container.Build());
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            try
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Frontend API V1");
                });

                app.UseHttpsRedirection();
                app.UseDefaultFiles();
                app.UseStaticFiles();
                app.UseMvc();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
