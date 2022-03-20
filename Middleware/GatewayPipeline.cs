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

    private static async Task<string> GetApiToken(HttpContext ctx, ApiTokenService apiTokenService, string token, ApiConfig? apiConfig)
    {
        string? apiToken = null;
        if (apiConfig != null && !string.IsNullOrEmpty(apiConfig.ApiScopes))
        {
            apiToken = await apiTokenService.LookupApiToken(ctx, apiConfig, token);
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

    public static void UseGatewayPipeline(this IReverseProxyApplicationBuilder pipeline)
    {
        var tokenRefreshService = pipeline.ApplicationServices.GetRequiredService<TokenRefreshService>();
        var config = pipeline.ApplicationServices.GetRequiredService<GatewayConfig>();
        var apiTokenService = pipeline.ApplicationServices.GetRequiredService<ApiTokenService>();
        
        pipeline.Use(async (ctx, next) =>
        {
            if (IsExpired(ctx) && HasRefreshToken(ctx))
            {
                apiTokenService.InvalidateApiTokens(ctx);
                await Refresh(ctx, tokenRefreshService);
            }

            var token = ctx.Session.GetString(SessionKeys.ACCESS_TOKEN);
            var currentUrl = ctx.Request.Path.ToString().ToLower();

            var apiConfig = config.ApiConfigs.FirstOrDefault(c => currentUrl.StartsWith(c.ApiPath));

            if (!string.IsNullOrEmpty(token) && apiConfig != null)
            {
                var apiToken = await GetApiToken(ctx, apiTokenService, token, apiConfig);
                ctx.Request.Headers.Add("Authorization", "Bearer " + apiToken);
            }
            await next().ConfigureAwait(false);
        });
    }

}