﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExampleApp.Custom
{
    public class GoogleAuthenticationHandler : ExternalAuthenticationHandler
    {
        private readonly ILogger<GoogleAuthenticationHandler> _logger;

        public GoogleAuthenticationHandler(
            IOptions<GoogleAuthenticationOptions> options,
            IDataProtectionProvider provider,
            HttpClient httpClient,
            ILogger<GoogleAuthenticationHandler> logger) : base(options, provider, httpClient, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<string> GetAuthenticationUrl(AuthenticationProperties properties)
        {
            if (CheckCredentials())
            {
                return await base.GetAuthenticationUrl(properties);
            }
            else
            {
                return string.Format(Options.ErrorUrlTemplate, ErrorMessage);
            }
        }

        // Map a different set of JSON properties to claims when signing a user into the application.
        // Each authentication service returns data in a different format.
        protected override IEnumerable<Claim> GetClaims(JsonDocument jsonDoc)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, jsonDoc.RootElement.GetString("id")),

                // The Google data will contain a name such as Adam Freeman, which won’t be accepted as an Identity
                // account name. To avoid a validation error, I replace spaces with underscores (the _ character).
                new Claim(ClaimTypes.Name, jsonDoc.RootElement.GetString("name")?.Replace(" ", "_")),
                new Claim(ClaimTypes.Email, jsonDoc.RootElement.GetString("email"))
            };
            return claims;
        }

        private bool CheckCredentials()
        {
            var secret = Options.ClientSecret;
            var id = Options.ClientId;
            var defaultVal = "ReplaceMe";

            if (!string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(id) || defaultVal.Equals(secret))
            {
                ErrorMessage = "External Authentication Secret or Client Id not set";
                _logger.LogError("External Authentication Secret or Client Id not set");

                return false;
            }

            return true;
        }
    }
}