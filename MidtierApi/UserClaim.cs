using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Common.Services;
using Common.Interfaces;

namespace MidtierApi
{
    public class UserClaim
    {
        private IAuthToken _authToken;
        private IHttpClient _httpClient;
        private string _backendUrl;
        private string _instance;
        private string _tenantId;
        private string _clientId;
        private string _clientSecret;
        private string[] _allowedScopes;
        private string _scope = "user.claim";

        public UserClaim(IAuthToken authToken, IHttpClient httpClient)
        {
            _authToken = authToken;
            _httpClient = httpClient;
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

            _backendUrl = config["BackendUrl"];
            _instance = config["AzureAd:Instance"];
            _tenantId = config["AzureAd:TenantId"];
            _clientId = config["AzureAd:ClientId"];
            _clientSecret = config["AzureAd:ClientSecret"];
            _allowedScopes = config["AzureAd:AllowedScopes"].Split(',');
            
            // Validate token (authorization)
            string audience = $"api://{_clientId}";
            await TokenValidation.VerifyUserHasAnyAcceptedScope(token, _instance, _tenantId, _clientId, audience, _allowedScopes, new CancellationToken());

            // Request token
            string[] requestedScopes = new string[] { $"api://{config["AzureAd:BackendClientId"]}/{_scope}" };
            var accessTokenResult = await _authToken.GetOnBehalfOf(
                _tenantId,
                _clientId,
                _clientSecret,
                token,
                requestedScopes);

            // Inject token in auth header
            _httpClient.SetAuthenticationHeader("Bearer", accessTokenResult.AccessToken);
        }

        [FunctionName("UserClaim")]
        public async Task<OkObjectResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "userclaim")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("Midtier user claim HTTP trigger function processed a request.");

            await OnExecutingAsync(req, context);

            var response = await _httpClient.GetAsync($"{_backendUrl}/api/claim");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Function.UserClaim failed with status code {response.StatusCode}. Message: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            return new OkObjectResult(responseContent);
        }
    }
}
