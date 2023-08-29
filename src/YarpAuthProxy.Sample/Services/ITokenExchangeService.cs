public interface ITokenExchangeService
{
    Task<TokenExchangeResponse> Exchange(string accessToken, ApiConfig apiConfig);
}