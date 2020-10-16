using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;

namespace Profile
{
    public static class Profile
    {
        

        [FunctionName("profile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var envName = Environment.GetEnvironmentVariable("ENVIRONMENT_NAME") ?? "Development";
            try
            {
                var configuration = new ConfigurationComponent();
                var config = configuration.BuildConfiguration();                
                var domain = config["AzureAd:Domain"];
                var audience = config["AzureAd:Audience"];
                var clientID = config["AzureAd:ClientId"];
                var tenantID = config["AzureAd:TenantId"];

                string name = req.Query["name"];
                var accessToken = configuration.GetAccessToken(req);
                if (accessToken != null)
                {
                    var claimsPrincipal = await configuration.ValidateAccessToken(accessToken, config, log);
                    name = claimsPrincipal.Identity.Name;
                }
                else {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);
                    name = name ?? data?.name;
                }
                log.LogInformation("C# HTTP trigger function processed a request.");               
                

                string responseMessage = string.IsNullOrEmpty(name)
                    ? $" This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                    : $"Hello {name}, This HTTP triggered function executed successfully.";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                
                log.LogError(ex, "error function");
                log.LogError(ex.StackTrace, "stack");
                return new OkObjectResult(envName + ex.Message);
            }
            
        }
    }
}
