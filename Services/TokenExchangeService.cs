
public class TokenExchangeService {

    private DiscoveryDocument disco;
    private GatewayConfig config;
    
    public TokenExchangeService(GatewayConfig config, DiscoveryDocument disco) {
        this.disco = disco;
        this.config = config;
    }

    public async Task<TokenExchangeResponse> Exchange(string accessToken) {

        var httpClient = new HttpClient();

        var scope = this.config.ApiScopes;

        var dict = new Dictionary<string, string>();
        dict["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        
        // Simplification: We assume just one auth server
        dict["client_id"] = this.config.ClientId;
        dict["client_secret"] = this.config.ClientSecret;
        dict["assertion"] = accessToken;
        dict["scope"] = scope;
        dict["requested_token_use"] = "on_behalf_of";

        var content = new FormUrlEncodedContent(dict);
        var httpResponse = await httpClient.PostAsync(this.disco.token_endpoint, content);
        var response = await httpResponse.Content.ReadFromJsonAsync<TokenExchangeResponse>();

        if (response == null) {
            throw new Exception("error exchaning token at " + disco.token_endpoint);
        }

        return response;
    }

}