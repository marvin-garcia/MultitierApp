using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Common.Models;
using Common.Interfaces;
using System.Linq;
using Microsoft.Identity.Client;

namespace FrontendApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private IAuthToken _authToken;
        private IConfiguration _configuration;
        private TelemetryClient _telemetryClient;

        public AuthController(IAuthToken authToken, IConfiguration configuration, TelemetryClient telemetryClient)
        {
            _authToken = authToken;
            _configuration = configuration;
            _telemetryClient = telemetryClient;
        }

        [HttpGet("IdToken")]
        public string GetIdToken()
        {
            return Request.Headers["X-MS-TOKEN-AAD-ID-TOKEN"];
        }

        //[HttpGet("AccessToken")]
        //public string GetAccessToken()
        //{
        //    return Request.Headers["X-MS-TOKEN-AAD-ACCESS-TOKEN"];
        //}

        //[HttpGet("RefreshToken")]
        //public string GetRefreshToken()
        //{
        //    return Request.Headers["X-MS-TOKEN-AAD-REFRESH-TOKEN"];
        //}

        //[HttpGet("TokenExpiration")]
        //public string GetTokenExpiration()
        //{
        //    return Request.Headers["X-MS-TOKEN-AAD-EXPIRES-ON"];
        //}

        //[HttpGet("IdTokenOnBehalfOf")]
        //public async Task<AccessTokenResult> GetUserToken()
        //{
        //    string[] scopes = _configuration["AzureAd:RequestedScopes"].Split(',')
        //        .Select(x => $"api://{_configuration["AzureAd:MidtierClientId"]}/{x}").ToArray();

        //    scopes = new string[]
        //    {
        //        $"api://{_configuration["AzureAd:MidtierClientId"]}/.default"
        //    };

        //    var accessTokenResult = await _authToken.GetOnBehalfOf(
        //        _configuration["AzureAd:TenantId"],
        //        _configuration["AzureAd:ClientId"],
        //        _configuration["AzureAd:ClientSecret"],
        //        Request.Headers["X-MS-TOKEN-AAD-ID-TOKEN"],
        //        scopes);

        //    return accessTokenResult;
        //}
    }
}