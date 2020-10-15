using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace WebServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IConfiguration _configuration;
        private readonly IFeatureManager _featureManager;

        public HomeController(IHttpClientFactory clientFactory,
         ITokenAcquisition tokenAcquisition,
         IConfiguration configuration,
         IFeatureManager featureManager)
        {
            _clientFactory = clientFactory;
            _tokenAcquisition = tokenAcquisition;
            _configuration = configuration;
            _featureManager = featureManager;
        }

        public async Task<IActionResult> Level() {

            StringBuilder sb = new StringBuilder();  
            if (await this._featureManager.IsEnabledAsync(nameof(Components.Features.Level1))) {
                sb.Append("level 1 ");
            }
            if (await this._featureManager.IsEnabledAsync(nameof(Components.Features.Level2)))
            {
                sb.Append("level 2 ");
            }
            if (await this._featureManager.IsEnabledAsync(nameof(Components.Features.Level3)))
            {
                sb.Append("level 3 ");
            }
            if (await this._featureManager.IsEnabledAsync(nameof(Components.Features.Level4)))
            {
                sb.Append("level 4 ");
            }
            return this.Ok(sb.ToString()); 
        }

        [AuthorizeForScopes(ScopeKeySection = "MSGraphApi:CalledApiScopes")]
        public async Task<IActionResult> Me()
        {

            var scope = this._configuration["MSGraphApi:CalledApiScopes"];
            var scopes = new[] { scope };            
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

            //var client = _clientFactory.CreateClient();
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //var response = await client.GetStringAsync("https://graph.microsoft.com/v1.0/me");
            //return this.Ok(response);

            GraphServiceClient graphClient = new GraphServiceClient(authenticationProvider: null);
            List<HeaderOption> requestHeaders = new List<HeaderOption>() { new HeaderOption("Authorization", "Bearer " + accessToken) };
            User me = await graphClient.Me.Request(requestHeaders).GetAsync();
            return this.Ok(me.DisplayName);            

        }
    }
}
