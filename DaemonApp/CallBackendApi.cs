using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Common.Interfaces;

namespace DaemonApp
{
    public class CallBackendApi
    {
        private static string _tenantId;
        private static string _clientId;
        private static string _clientSecret;
        private static string[] _scopes;
        private static string _backendUrl;
        private static IHttpClient _httpClient;

        public enum CallType
        {
            Echo,
            Claim,
        }

        public CallBackendApi(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [FunctionName("CallBackendApi")]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "callbackend")] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            CallType callType = (CallType)Enum.Parse(typeof(CallType), req.Query["callType"], true);

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            _tenantId = config["AzureAd:TenantId"];
            _clientId = config["AzureAd:ClientId"];
            _clientSecret = config["AzureAd:ClientSecret"];
            _backendUrl = config["BackendUrl"];
            _scopes = new string[] { $"api://{config["AzureAd:BackendClientId"]}/.default" };

            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
               .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
               .WithClientSecret(_clientSecret)
               .Build();

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(_scopes)
                    .ExecuteAsync();

            }
            catch (MsalServiceException ex)
            {
                throw ex;
            }

            _httpClient.SetAuthenticationHeader("Bearer", result.AccessToken);

            string uri = string.Empty;
            switch (callType)
            {
                case CallType.Echo:
                    uri = $"{_backendUrl}/api/echo/daemon echo call";
                    break;

                case CallType.Claim:
                    uri = $"{_backendUrl}/api/claim";
                    break;
            }

            var response = await _httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"CallBackend function failed with status code {response.StatusCode}. Reason phrase: {response.ReasonPhrase}");

            string responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
    }
}
