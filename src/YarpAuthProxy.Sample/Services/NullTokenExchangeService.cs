public class NullTokenExchangeService : ITokenExchangeService
{
    public Task<TokenExchangeResponse> Exchange(string accessToken, ApiConfig apiConfig)
    {
        var result = new TokenExchangeResponse {
            access_token = "",
            expires_in = 0,
            refresh_token = "",
        };

        return Task.FromResult(result);
    }

}