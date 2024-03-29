﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Resource
{
    public static class ScopesRequiredHttpContextExtensions
    {
        /// <summary>
        /// Intended for .NET Core APIs.
        /// When applied to an <see cref="HttpContext"/>, verifies that the user authenticated in the
        /// web API has any of the accepted scopes. 
        /// If the authentication user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method throws an HTTP Unauthorized with the message telling which scopes are expected in the token
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API</param>
        /// <exception cref="HttpRequestException"/> with a <see cref="HttpResponse.StatusCode"/> set to 
        /// <see cref="HttpStatusCode.Unauthorized"/>
        public static void VerifyUserHasAnyAcceptedScope(this HttpContext context, params string[] acceptedScopes)
        {
            if (acceptedScopes == null)
            {
                throw new ArgumentNullException(nameof(acceptedScopes));
            }

            Claim scopeClaim = context?.User?.FindFirst("http://schemas.microsoft.com/identity/claims/scope");

            if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                string scopeClaimValue = scopeClaim == null ? null : scopeClaim.Value;
                string message = $"The 'scope' claim does not contain scopes '{string.Join(",", acceptedScopes)}' or was not found. Supported scopes are: {scopeClaimValue}";
                throw new HttpRequestException(message);
            }
        }
    }
}
