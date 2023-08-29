using System.Net.Http.Json;
using YarpAuthProxy.BffLibrary.Utils.Config;

namespace YarpAuthProxy.BffLibrary.Services;

public class AzureAdTokenExchangeService : ITokenExchangeService
{
    private DiscoveryDocument disco;
    private GatewayConfig config;

    public AzureAdTokenExchangeService(GatewayConfig config, DiscoveryDocument disco)
    {
        this.disco = disco;
        this.config = config;
    }

    public async Task<TokenExchangeResponse> Exchange(string accessToken, ApiConfig apiConfig)
    {

        var httpClient = new HttpClient();
        var scope = apiConfig.ApiScopes;

        // TODO: Allow to config different settings per API
        //  e. g. client_id, client_secrets, token_endpoint 

        var url = this.disco.token_endpoint;
        var dict = new Dictionary<string, string>();
        dict["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        dict["client_id"] = this.config.ClientId;
        dict["client_secret"] = this.config.ClientSecret;
        dict["assertion"] = accessToken;
        dict["scope"] = scope;
        dict["requested_token_use"] = "on_behalf_of";

        var content = new FormUrlEncodedContent(dict);
        var httpResponse = await httpClient.PostAsync(url, content);
        var response = await httpResponse.Content.ReadFromJsonAsync<TokenExchangeResponse>();

        if (response == null)
        {
            throw new Exception("error exchaning token at " + disco.token_endpoint);
        }

        return response;
    }

}