using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Common.Services;

namespace BackendApi
{
    public class Echo
    {
        private string _instance;
        private string _tenantId;
        private string _clientId;
        private string[] _allowedScopes = new string[] { "user.echo", "app.echo" };

        public Echo()
        {
        }

        private async Task OnExecutingAsync(HttpRequest request, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            // Extract token from header, return 'Unauthorized' error if the token is null.
            string token = string.Empty;
            if (request.Headers.ContainsKey("Authorization") && request.Headers["Authorization"][0].StartsWith("Bearer "))
                token = request.Headers["Authorization"][0].Substring("Bearer ".Length);
            else
            {
                request.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                throw new HttpRequestException("Unauthorized");
            }

            // Get Azure AD env settings
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            _instance = config["AzureAd:Instance"];
            _tenantId = config["AzureAd:TenantId"];
            _clientId = config["AzureAd:ClientId"];

            // Validate token (authorization)
            string audience = $"api://{_clientId}";
            await TokenValidation.VerifyUserHasAnyAcceptedScope(token, _instance, _tenantId, _clientId, audience, _allowedScopes, new CancellationToken());
        }

        [FunctionName("Echo")]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "echo/{message}")] HttpRequest req,
            string message,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("Backend Echo HTTP trigger function processed a request.");

            await OnExecutingAsync(req, context);

            return $"Backend echo response: {message}";
        }
    }
}
