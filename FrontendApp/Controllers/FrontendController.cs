using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Common.Interfaces;
using System.Linq;
using Microsoft.Identity.Client;

namespace FrontendApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FrontendController : Controller
    {
        private IAuthToken _authToken;
        private IHttpClient _httpClient;
        private IConfiguration _configuration;
        private string _midtierUrl;

        public FrontendController(IConfiguration configuration, IHttpClient httpClient, IAuthToken authToken)
        {
            _authToken = authToken;
            _httpClient = httpClient;
            _configuration = configuration;
            _midtierUrl = $"{configuration["MidtierUrl"]}/api";
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            string[] scopes = _configuration["AzureAd:RequestedScopes"].Split(',')
                .Select(x => $"api://{_configuration["AzureAd:MidtierClientId"]}/{x}").ToArray();

            // Request token
            var accessTokenResult = _authToken.GetOnBehalfOf(
                _configuration["AzureAd:TenantId"],
                _configuration["AzureAd:ClientId"],
                _configuration["AzureAd:ClientSecret"],
                Request.Headers["X-MS-TOKEN-AAD-ID-TOKEN"],
                scopes
            ).ContinueWith((r) =>
            {
                return r.Result;
            }).Result;

            // Inject token in auth header
            _httpClient.SetAuthenticationHeader("Bearer", accessTokenResult.AccessToken);
        }

        [HttpGet("userclaim", Name = "UserClaim")]
        public async Task<OkObjectResult> GetUserClaim()
        {
            var response = await _httpClient.GetAsync($"{_midtierUrl}/userclaim");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Function.GetClaim failed with status code {response.StatusCode}. Message: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            return new OkObjectResult(responseContent);
        }

        [HttpGet("serviceclaim", Name = "ServiceClaim")]
        public async Task<OkObjectResult> GetServiceClaim()
        {
            var response = await _httpClient.GetAsync($"{_midtierUrl}/serviceclaim");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Function.GetUPN failed with status code {response.StatusCode}. Message: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            return new OkObjectResult(responseContent);
        }

        [HttpGet("echo/{message}", Name = "Echo")]
        public async Task<OkObjectResult> Echo(string message)
        {
            var response = await _httpClient.GetAsync($"{_midtierUrl}/echo/{message}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Function.Echo failed with status code {response.StatusCode}. Message: {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            return new OkObjectResult(responseContent);
        }
    }
}
