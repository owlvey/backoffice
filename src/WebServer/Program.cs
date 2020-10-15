using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace WebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { })
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (context.HostingEnvironment.IsProduction())
                    {
                        var builtConfig = config.Build();                        

                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var keyVaultClient = new KeyVaultClient(
                            new KeyVaultClient.AuthenticationCallback(
                                azureServiceTokenProvider.KeyVaultTokenCallback));

                        config.AddAzureKeyVault(
                            $"https://{builtConfig["KeyVaultName"]}.vault.azure.net/",
                            keyVaultClient,
                            new CustomKeyVaultSecretManager(builtConfig["KeyVaultPrefix"]));
                        
                        config.AddAzureAppConfiguration(options =>
                        {                        
                            var target = $"https://{builtConfig["KeyVaultName"]}.vault.azure.net/secrets/FeatureConfig";
                            var endpoint = keyVaultClient.GetSecretAsync(target).GetAwaiter().GetResult().Value;
                            options.Connect(endpoint).UseFeatureFlags();                            
                        });
                    }
                    else {
                        var builtConfig = config.Build();
                        config.AddAzureAppConfiguration(options =>
                        {
                            options.Connect(builtConfig["FeatureConfig:Endpoint"]).UseFeatureFlags();                            
                        });
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
