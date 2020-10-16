using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Profile
{
    public class ConfigurationComponent
    {
        public IConfiguration BuildConfiguration()
        {
            var envName = Environment.GetEnvironmentVariable("ENVIRONMENT_NAME") ?? "Development";
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

            if (envName == "Development")
            {
                var configuration = configurationBuilder
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{envName}.json", true, true)
                        .AddUserSecrets(Assembly.GetExecutingAssembly(), false)
                        .AddEnvironmentVariables().Build();
                return configuration;
            }
            else
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        azureServiceTokenProvider.KeyVaultTokenCallback));

                var config = configurationBuilder
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{envName}.json", true, true)
                        .AddAzureKeyVault(
                            $"https://owlvey-key-vault.vault.azure.net/",
                            keyVaultClient,
                            new CustomKeyVaultSecretManager("BackOfficeProfile--"))
                        .AddAzureAppConfiguration(options =>
                        {
                            var target = $"https://owlvey-key-vault.vault.azure.net/secrets/FeatureConfig";
                            var endpoint = keyVaultClient.GetSecretAsync(target).GetAwaiter().GetResult().Value;
                            options.Connect(endpoint).UseFeatureFlags();
                        })
                        .AddEnvironmentVariables().Build();                               
                
                return config;
            }
        }
        public string GetAccessToken(HttpRequest req)
        {
            var authorizationHeader = req.Headers?["Authorization"];
            string[] parts = authorizationHeader?.ToString().Split(null) ?? new string[0];
            if (parts.Length == 2 && parts[0].Equals("Bearer"))
                return parts[1];
            return null;
        }   

        //private async Task<IActionResult> Authenticated(HttpRequest req, string config, ILogger log)
        //{
        //    var accessToken = GetAccessToken(req);
        //    var claimsPrincipal = await ValidateAccessToken(accessToken, config, log);
        //    if (claimsPrincipal != null)
        //    {
        //        return (ActionResult)new OkObjectResult(claimsPrincipal.Identity.Name);
        //    }
        //    else
        //    {
        //        return (ActionResult)new UnauthorizedResult();
        //    }
        //}
        public async Task<ClaimsPrincipal> ValidateAccessToken(string accessToken, IConfiguration configuration, ILogger log)
        {
            var domain = configuration["AzureAd:Domain"];
            var audience = configuration["AzureAd:Audience"];
            var clientID = configuration["AzureAd:ClientId"]; 
            var tenantID = configuration["AzureAd:TenantId"];
            var authority = $"https://login.microsoftonline.com/{domain}";
            var validIssuers = $"https://sts.windows.net/{tenantID}/";

            IdentityModelEventSource.ShowPII = true;

            ConfigurationManager<OpenIdConnectConfiguration> configManager =
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{authority}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());

            OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().GetAwaiter().GetResult();

            ISecurityTokenValidator tokenValidator = new JwtSecurityTokenHandler();

            // Initialize the token validation parameters
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                // App Id URI and AppId of this service application are both valid audiences.
                ValidAudiences = new[] { audience, clientID },

                // Support Azure AD V1 and V2 endpoints.
                ValidIssuers = new List<string>() { validIssuers },
                IssuerSigningKeys = config.SigningKeys
            };

            try
            {
                SecurityToken securityToken;
                var claimsPrincipal = tokenValidator.ValidateToken(accessToken, validationParameters, out securityToken);
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return null;
        }
    }
}
