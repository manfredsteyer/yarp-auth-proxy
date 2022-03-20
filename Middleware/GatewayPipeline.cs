public static class GatewayPipeline
{
    private static bool IsExpired(HttpContext ctx)
    {
        var expiresAt = Convert.ToInt64(ctx.Session.GetString(SessionKeys.EXPIRES_AT)) - 30;
        var now = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                     
        var expired = now >= expiresAt;
        return expired;
    }

    private static bool HasRefreshToken(HttpContext ctx) {
        var refreshToken = ctx.Session.GetString(SessionKeys.REFRESH_TOKEN);
        return !string.IsNullOrEmpty(refreshToken);
    }

    private static string GetRefreshToken(HttpContext ctx) {
        var refreshToken = ctx.Session.GetString(SessionKeys.REFRESH_TOKEN);
        return refreshToken ?? "";
    }

    private static async Task Refresh(HttpContext ctx, TokenRefreshService tokenRefreshService)
    {
        var refreshToken = GetRefreshToken(ctx);

        var resp = await tokenRefreshService.RefreshAsync(refreshToken);

        if (resp == null) {
            // Next call to API will fail with 401 and client can take action
            return;
        }

        var expiresAt = new DateTimeOffset(DateTime.Now).AddSeconds(Convert.ToInt32(resp.expires));

        ctx.Session.SetString(SessionKeys.ACCESS_TOKEN, resp.access_token);
        ctx.Session.SetString(SessionKeys.ID_TOKEN, resp.id_token);
        ctx.Session.SetString(SessionKeys.REFRESH_TOKEN, resp.refresh_token);
        ctx.Session.SetString(SessionKeys.EXPIRES_AT, "" + expiresAt.ToUnixTimeSeconds());

    }

    private static TokenExchangeResponse? GetCachedApiToken(HttpContext ctx, ApiConfig apiConfig) {
        var cache = ctx.Session.GetObject<Dictionary<string, TokenExchangeResponse>>(SessionKeys.API_ACCESS_TOKEN);
        if (cache == null) {
            return null;
        }

        if (!cache.ContainsKey(apiConfig.ApiPath)) {
            return null;
        }

        return cache[apiConfig.ApiPath];
    }

    private static void SetCachedApiToken(HttpContext ctx, ApiConfig apiConfig, TokenExchangeResponse response) {
        var cache = ctx.Session.GetObject<Dictionary<string, TokenExchangeResponse>>(SessionKeys.API_ACCESS_TOKEN);
        if (cache == null) {
            cache = new Dictionary<string, TokenExchangeResponse>();
        }

        cache[apiConfig.ApiPath] = response;

        ctx.Session.SetObject<Dictionary<string, TokenExchangeResponse>>(SessionKeys.API_ACCESS_TOKEN, cache);
    }

    private static async Task<string> LookupApiToken(HttpContext ctx, TokenExchangeService tokenExchangeService, ApiConfig apiConfig, string token)
    {
        var apiToken = GetCachedApiToken(ctx, apiConfig);

        if (apiToken != null) {
            // TODO: Perform individual token refresh
            return apiToken.access_token;
        }

        Console.WriteLine("--- Perform Token Exchange ---");

        var response = await tokenExchangeService.Exchange(token, apiConfig);
        SetCachedApiToken(ctx, apiConfig, response);

        return response.access_token;
    }



    public static void UseGatewayPipeline(this IReverseProxyApplicationBuilder pipeline)
    {
        var tokenRefreshService = pipeline.ApplicationServices.GetRequiredService<TokenRefreshService>();
        var config = pipeline.ApplicationServices.GetRequiredService<GatewayConfig>();
        var tokenExchangeService = pipeline.ApplicationServices.GetRequiredService<TokenExchangeService>();
        
        pipeline.Use(async (ctx, next) =>
        {
            if (IsExpired(ctx) && HasRefreshToken(ctx))
            {
                InvalidateApiTokens(ctx);
                await Refresh(ctx, tokenRefreshService);
            }

            var token = ctx.Session.GetString(SessionKeys.ACCESS_TOKEN);
            var currentUrl = ctx.Request.Path.ToString().ToLower();

            var apiConfig = config.ApiConfigs.FirstOrDefault(c => currentUrl.StartsWith(c.ApiPath));

            if (!string.IsNullOrEmpty(token) && apiConfig != null)
            {
                token = await GetApiToken(ctx, tokenExchangeService, token, apiConfig);
                ctx.Request.Headers.Add("Authorization", "Bearer " + token);
            }
            await next().ConfigureAwait(false);
        });
    }
    private static async Task<string> GetApiToken(HttpContext ctx, TokenExchangeService tokenExchangeService, string token, ApiConfig? apiConfig)
    {
        string? apiToken = null;
        if (apiConfig != null && !string.IsNullOrEmpty(apiConfig.ApiScopes))
        {
            apiToken = await LookupApiToken(ctx, tokenExchangeService, apiConfig, token);
            ShowDebugMessage(token);
        }

        if (!string.IsNullOrEmpty(apiToken)) {
            return apiToken;
        }
        else {
            return token;
        }
    }

    private static void ShowDebugMessage(string? token)
    {
        // For demonstration purposes
        // Don't do this in production!
        
        Console.WriteLine("---- api access_token ----");
        Console.WriteLine(token);
        Console.WriteLine("--------");
    }

    private static void InvalidateApiTokens(HttpContext ctx)
    {
        ctx.Session.Remove(SessionKeys.API_ACCESS_TOKEN);
    }
}