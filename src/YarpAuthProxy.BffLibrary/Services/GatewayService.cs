using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YarpAuthProxy.BffLibrary.Middleware;
using YarpAuthProxy.BffLibrary.Utils.Config;

namespace YarpAuthProxy.BffLibrary.Services;

public class GatewayService
{
    private TokenRefreshService tokenRefreshService;
    private GatewayConfig config;
    private ApiTokenService apiTokenService;
    private ILogger<GatewayService> logger;

    public GatewayService(
        TokenRefreshService tokenRefreshService,
        GatewayConfig config,
        ApiTokenService apiTokenService,
        ILogger<GatewayService> logger
    )
    {
        this.tokenRefreshService = tokenRefreshService;
        this.config = config;
        this.apiTokenService = apiTokenService;
        this.logger = logger;
    }

    private bool IsExpired(HttpContext ctx)
    {
        var expiresAt = Convert.ToInt64(ctx.Session.GetString(SessionKeys.EXPIRES_AT)) - 30;
        var now = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

        var expired = now >= expiresAt;
        return expired;
    }

    private bool HasRefreshToken(HttpContext ctx)
    {
        var refreshToken = ctx.Session.GetString(SessionKeys.REFRESH_TOKEN);
        return !string.IsNullOrEmpty(refreshToken);
    }

    private string GetRefreshToken(HttpContext ctx)
    {
        var refreshToken = ctx.Session.GetString(SessionKeys.REFRESH_TOKEN);
        return refreshToken ?? "";
    }

    private async Task Refresh(HttpContext ctx, TokenRefreshService tokenRefreshService)
    {
        var refreshToken = GetRefreshToken(ctx);

        var resp = await tokenRefreshService.RefreshAsync(refreshToken);

        if (resp == null)
        {
            // Next call to API will fail with 401 and client can take action
            return;
        }

        var expiresAt = new DateTimeOffset(DateTime.Now).AddSeconds(Convert.ToInt32(resp.expires));

        ctx.Session.SetString(SessionKeys.ACCESS_TOKEN, resp.access_token);
        ctx.Session.SetString(SessionKeys.ID_TOKEN, resp.id_token);
        ctx.Session.SetString(SessionKeys.REFRESH_TOKEN, resp.refresh_token);
        ctx.Session.SetString(SessionKeys.EXPIRES_AT, "" + expiresAt.ToUnixTimeSeconds());
    }

    private async Task<string> GetApiToken(HttpContext ctx, ApiTokenService apiTokenService, string token, ApiConfig? apiConfig)
    {
        string? apiToken = null;
        if (!string.IsNullOrEmpty(apiConfig?.ApiScopes) || !string.IsNullOrEmpty(apiConfig?.ApiAudience))
        {
            apiToken = await apiTokenService.LookupApiToken(ctx, apiConfig, token);
            ShowDebugMessage(apiToken);
        }

        if (!string.IsNullOrEmpty(apiToken))
        {
            return apiToken;
        }
        else
        {
            return token;
        }
    }

    private void ShowDebugMessage(string? token)
    {
        this.logger.LogDebug($"---- api access_token ----\n{token}\n--------");
    }

    public async Task AddToken(HttpContext ctx)
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

            logger.LogDebug($"---- Adding Token for reqeuest ----\n{currentUrl}\n\n{apiToken}\n--------");

            ctx.Request.Headers.Add("Authorization", "Bearer " + apiToken);
        }
    }

}