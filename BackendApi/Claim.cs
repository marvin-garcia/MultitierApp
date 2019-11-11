using System.Net;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Common.Services;
using System.Security.Claims;

namespace BackendApi
{
    public class Claim
    {
        private string _instance;
        private string _tenantId;
        private string _clientId;
        private string[] _allowedScopes = new string[] { "user.claim", "app.claim" };
        private ClaimsPrincipal _userClaim;

        public Claim()
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
            _userClaim = await TokenValidation.VerifyUserHasAnyAcceptedScope(token, _instance, _tenantId, _clientId, audience, _allowedScopes, new CancellationToken());
        }

        [FunctionName("Claim")]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "claim")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("Backend user claim HTTP trigger function processed a request.");

            await OnExecutingAsync(req, context);

            return string.Join(' ', _userClaim.Identities.Select(i => string.Join(", ", i.Claims.Select(c => c.Value))));
        }
    }
}
