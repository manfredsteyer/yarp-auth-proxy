public static class GatewayPipeline
{
    private static bool isExpired(HttpContext ctx)
    {
        var expiresAt = Convert.ToInt64(ctx.Session.GetString(SessionKeys.EXPIRES_AT)) - 30;
        var now = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                     
        var expired = now >= expiresAt;
        return expired;
    }

    private static bool hasRefreshToken(HttpContext ctx) {
        var refreshToken = ctx.Session.GetString(SessionKeys.REFRESH_TOKEN);
        return !string.IsNullOrEmpty(refreshToken);
    }

    private static string getRefreshToken(HttpContext ctx) {
        var refreshToken = ctx.Session.GetString(SessionKeys.REFRESH_TOKEN);
        return refreshToken ?? "";
    }

    private static async Task refresh(HttpContext ctx, TokenRefreshService tokenRefreshService)
    {
        var refreshToken = getRefreshToken(ctx);

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

    private static async Task<string> LookupApiToken(HttpContext ctx, TokenExchangeService tokenExchangeService, string apiPath, string token)
    {
        // TODO: Respect serveral APIs from several auth severs
        // TODO: Perform individual token refresh

        if (ctx.Session.Keys.Contains(SessionKeys.API_ACCESS_TOKEN)) {
            return ctx.Session.GetString(SessionKeys.API_ACCESS_TOKEN) ?? "";
        }

        var response = await tokenExchangeService.Exchange(token);
        var accessToken = response.access_token;

        ctx.Session.SetString(SessionKeys.API_ACCESS_TOKEN, accessToken);
        return accessToken;
    }

    private static void InvalidateApiTokens(HttpContext ctx)
    {
        ctx.Session.Remove(SessionKeys.API_ACCESS_TOKEN);
    }

    public static void UseGatewayPipeline(this IReverseProxyApplicationBuilder pipeline)
    {
        var tokenRefreshService = pipeline.ApplicationServices.GetRequiredService<TokenRefreshService>();
        var config = pipeline.ApplicationServices.GetRequiredService<GatewayConfig>();
        var tokenExchangeService = pipeline.ApplicationServices.GetRequiredService<TokenExchangeService>();
        
        var apiPath = config.ApiPath;
        
        pipeline.Use(async (ctx, next) =>
        {
            if (isExpired(ctx) && hasRefreshToken(ctx))
            {
                InvalidateApiTokens(ctx);
                await refresh(ctx, tokenRefreshService);
            }

            var token = ctx.Session.GetString(SessionKeys.ACCESS_TOKEN);
            var currentUrl = ctx.Request.Path.ToString().ToLower();

            if (!string.IsNullOrEmpty(token) && currentUrl.StartsWith(apiPath))
            {
                if (!string.IsNullOrEmpty(config.ApiScopes))
                {
                    token = await LookupApiToken(ctx, tokenExchangeService, apiPath, token);
                    ShowDebugMessage(token);
                }

                ctx.Request.Headers.Add("Authorization", "Bearer " + token);
            }
            await next().ConfigureAwait(false);
        });
    }

    private static void ShowDebugMessage(string? token)
    {
        // For demonstration purposes
        // Don't do this in production!
        
        Console.WriteLine("---- api access_token ----");
        Console.WriteLine(token);
        Console.WriteLine("--------");
    }
}