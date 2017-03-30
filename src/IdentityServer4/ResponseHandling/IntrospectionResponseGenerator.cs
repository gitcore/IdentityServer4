﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.ResponseHandling
{
    /// <summary>
    /// The introspection response generator
    /// </summary>
    /// <seealso cref="IdentityServer4.ResponseHandling.IIntrospectionResponseGenerator" />
    public class IntrospectionResponseGenerator : IIntrospectionResponseGenerator
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntrospectionResponseGenerator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public IntrospectionResponseGenerator(ILogger<IntrospectionResponseGenerator> logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Processes the response.
        /// </summary>
        /// <param name="validationResult">The validation result.</param>
        /// <returns></returns>
        public virtual async Task<Dictionary<string, object>> ProcessAsync(IntrospectionRequestValidationResult validationResult)
        {
            Logger.LogTrace("Creating introspection response");

            var response = new Dictionary<string, object>()
            {
                { "active", false }
            };

            if (validationResult.IsActive == false)
            {
                Logger.LogDebug("Creating introspection response for inactive token.");
                return response;
            }

            // expected scope not present
            if (await AreExpectedScopesPresent(validationResult) == false)
            {
                return response;
            }

            Logger.LogDebug("Creating introspection response for active token.");

            response = validationResult.Claims.Where(c => c.Type != JwtClaimTypes.Scope).ToClaimsDictionary();
            response.Add("active", true);

            var allowedScopes = validationResult.Api.Scopes.Select(x => x.Name);
            var scopes = validationResult.Claims.Where(c => c.Type == JwtClaimTypes.Scope).Select(x => x.Value);
            scopes = scopes.Where(x => allowedScopes.Contains(x));
            response.Add("scope", scopes.ToSpaceSeparatedString());
            
            return response;
        }

        /// <summary>
        /// Checks if the API resource is allowed to introspect the scopes.
        /// </summary>
        /// <param name="validationResult">The validation result.</param>
        /// <returns></returns>
        protected virtual Task<bool> AreExpectedScopesPresent(IntrospectionRequestValidationResult validationResult)
        {
            // check expected scopes
            var supportedScopes = validationResult.Api.Scopes.Select(x => x.Name);
            var expectedScopes = validationResult.Claims.Where(
                c => c.Type == JwtClaimTypes.Scope && supportedScopes.Contains(c.Value));

            Logger.LogError("Expected scope {scopes} is missing in token", supportedScopes);

            return Task.FromResult(expectedScopes.Any());
        }
    }
}