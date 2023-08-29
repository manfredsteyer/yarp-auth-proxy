public class TokenRefreshService {

    private DiscoveryDocument disco;
    private GatewayConfig config;
    
    public TokenRefreshService(GatewayConfig config, DiscoveryDocument disco) {
        this.disco = disco;
        this.config = config;
    }

    public async Task<RefreshResponse?> RefreshAsync(string refreshToken) {
        var payload = new Dictionary<string, string>();
        payload.Add("grant_type", "refresh_token");
        payload.Add("refresh_token", refreshToken);
        payload.Add("client_id", config.ClientId);
        payload.Add("client_secret", config.ClientSecret);

        var httpClient = new HttpClient();
        
        var request = new HttpRequestMessage {
            RequestUri = new Uri(disco.token_endpoint),
            Method = HttpMethod.Post,
            Content = new FormUrlEncodedContent(payload)
        };

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode) {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<RefreshResponse>();

        return result;

    }
}
 