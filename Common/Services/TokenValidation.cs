using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Globalization;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Common.Constants;

namespace Common.Services
{
    public static class TokenValidation
    {
        public static async Task<ClaimsPrincipal> VerifyUserHasAnyAcceptedScope(string token, string aadInstance, string tenantId, string clientId, string audience, string[] acceptedScopes, CancellationToken cancellationToken)
        {
            string authority = string.Format(CultureInfo.InvariantCulture, aadInstance, tenantId);
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{authority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
            ISecurityTokenValidator tokenValidator = new JwtSecurityTokenHandler();

            // For debugging/development purposes, one can enable additional detail in exceptions by setting IdentityModelEventSource.ShowPII to true.
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            // Pull OIDC discovery document from Azure AD. For example, the tenant-independent version of the document is located
            // at https://login.microsoftonline.com/common/.well-known/openid-configuration.
            OpenIdConnectConfiguration config = null;
            try
            {
                config = await configManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw e;
            }

            // You can get a list of issuers for the various Azure AD deployments (global & sovereign) from the following endpoint
            // https://login.microsoftonline.com/common/discovery/instance?authorization_endpoint=https://login.microsoftonline.com/common/oauth2/v2.0/authorize&api-version=1.1;

            IList<string> validIssuers = new List<string>()
            {
                $"https://login.microsoftonline.com/{tenantId}/",
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://login.windows.net/{tenantId}/",
                $"https://login.microsoft.com/{tenantId}/",
                $"https://sts.windows.net/{tenantId}/"
            };

            // Initialize the token validation parameters
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                // App Id URI and AppId of this service application are both valid audiences.
                ValidAudiences = new[] { audience, clientId },

                // Support Azure AD V1 and V2 endpoints.
                ValidIssuers = validIssuers,
                IssuerSigningKeys = config.SigningKeys
            };

            try
            {
                // Validate token.
                SecurityToken securityToken;
                var claimsPrincipal = tokenValidator.ValidateToken(token, validationParameters, out securityToken);

                // This check is required to ensure that the Web API only accepts tokens from tenants where it has been consented to and provisioned.
                if (!claimsPrincipal.Claims.Any(x => x.Type == ClaimConstants.ScopeClaimType)
                   && !claimsPrincipal.Claims.Any(y => y.Type == ClaimConstants.RoleClaimType))
                    throw new HttpRequestException("Forbidden: Neither 'scope' or 'roles' claim was found in the bearer token.");
                
                // If the token is scoped, verify that required permission is set in the scope claim. This could be done later at the controller level as well
                IEnumerable<Claim> scopeClaims = claimsPrincipal?.FindAll(ClaimConstants.ScopeClaimType);
                if (scopeClaims == null || scopeClaims.Count() == 0)
                    scopeClaims = claimsPrincipal?.FindAll(ClaimConstants.RoleClaimType);

                if (scopeClaims == null || scopeClaims.Where(c => c.Value.Split(' ').Intersect(acceptedScopes).Any()).Count() == 0)
                {
                    string scopeClaimValue = scopeClaims == null ? null : string.Join(", ", scopeClaims.Select(c => c.Value));
                    string message = $"The 'scope' claim does not contain scopes '{string.Join(",", acceptedScopes)}' or was not found. Supported scopes are: {scopeClaimValue}";
                    throw new HttpRequestException(message);
                }

                return claimsPrincipal;
            }
            catch (SecurityTokenValidationException stex)
            {
                throw stex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
