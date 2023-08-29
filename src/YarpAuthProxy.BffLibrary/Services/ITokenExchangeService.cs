using YarpAuthProxy.BffLibrary.Utils.Config;

namespace YarpAuthProxy.BffLibrary.Services;

public interface ITokenExchangeService
{
    Task<TokenExchangeResponse> Exchange(string accessToken, ApiConfig apiConfig);
}