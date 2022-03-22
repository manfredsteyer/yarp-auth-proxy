public class ApiTokenService {

    private TokenExchangeService tokenExchangeService;
    private ILogger<ApiTokenService> logger;

    public ApiTokenService(
        TokenExchangeService tokenExchangeService,
        ILogger<ApiTokenService> logger
    ) {
        this.tokenExchangeService = tokenExchangeService;
        this.logger = logger;
    }

    public void InvalidateApiTokens(HttpContext ctx)
    {
        ctx.Session.Remove(SessionKeys.API_ACCESS_TOKEN);
    }

    private TokenExchangeResponse? GetCachedApiToken(HttpContext ctx, ApiConfig apiConfig) {
        var cache = ctx.Session.GetObject<Dictionary<string, TokenExchangeResponse>>(SessionKeys.API_ACCESS_TOKEN);
        if (cache == null) {
            return null;
        }

        if (!cache.ContainsKey(apiConfig.ApiPath)) {
            return null;
        }

        return cache[apiConfig.ApiPath];
    }

    private void SetCachedApiToken(HttpContext ctx, ApiConfig apiConfig, TokenExchangeResponse response) {
        var cache = ctx.Session.GetObject<Dictionary<string, TokenExchangeResponse>>(SessionKeys.API_ACCESS_TOKEN);
        if (cache == null) {
            cache = new Dictionary<string, TokenExchangeResponse>();
        }

        cache[apiConfig.ApiPath] = response;

        ctx.Session.SetObject<Dictionary<string, TokenExchangeResponse>>(SessionKeys.API_ACCESS_TOKEN, cache);
    }

    public async Task<string> LookupApiToken(HttpContext ctx, ApiConfig apiConfig, string token)
    {
        var apiToken = GetCachedApiToken(ctx, apiConfig);

        if (apiToken != null) {
            // TODO: Perform individual token refresh
            return apiToken.access_token;
        }

        logger.LogDebug($"---- Perform Token Exchange for {apiConfig.ApiScopes} ----");

        var response = await tokenExchangeService.Exchange(token, apiConfig);
        SetCachedApiToken(ctx, apiConfig, response);

        return response.access_token;
    }

}