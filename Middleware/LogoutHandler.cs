
using Microsoft.AspNetCore.Authentication.OpenIdConnect;


public static class LogoutHandler {
    public static void HandleLogout(RedirectContext context, GatewayConfig config)
    {
        if (!string.IsNullOrEmpty(config.LogoutUrl))
        {
            var req = context.Request;
            var gatewayUrl = Uri.EscapeDataString(req.Scheme + "://" + req.Host + req.PathBase);

            var logoutUri = config.LogoutUrl
                                    .Replace("{authority}", config.Authority)
                                    .Replace("{clientId}", config.ClientId)
                                    .Replace("{gatewayUrl}", gatewayUrl);

            context.Response.Redirect(logoutUri);
            context.HandleResponse();
        }
    }
}