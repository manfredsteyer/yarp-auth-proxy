using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public class TokenHandler {

    private ILogger<TokenHandler> logger;

    public TokenHandler(ILogger<TokenHandler> logger) {
        this.logger = logger;
    }

    public void HandleToken(TokenValidatedContext context)
    {
        if (context.TokenEndpointResponse == null)
        {
            throw new Exception("TokenEndpointResponse expected!");
        }

        var accessToken = context.TokenEndpointResponse.AccessToken;
        var idToken = context.TokenEndpointResponse.IdToken;
        var refreshToken = context.TokenEndpointResponse.RefreshToken;
        var expiresIn = context.TokenEndpointResponse.ExpiresIn;
        var expiresAt = new DateTimeOffset(DateTime.Now).AddSeconds(Convert.ToInt32(expiresIn));

        ShowDebugInfo(accessToken, idToken, refreshToken);

        context.HttpContext.Session.SetString(SessionKeys.ACCESS_TOKEN, accessToken);
        context.HttpContext.Session.SetString(SessionKeys.ID_TOKEN, idToken);
        context.HttpContext.Session.SetString(SessionKeys.REFRESH_TOKEN, refreshToken);
        context.HttpContext.Session.SetString(SessionKeys.EXPIRES_AT, "" + expiresAt.ToUnixTimeSeconds());
    }

    private void ShowDebugInfo(string accessToken, string idToken, string refreshToken)
    {
        logger.LogDebug($"---- ACCESS_TOKEN ----\n{accessToken}\n--------");
        logger.LogDebug($"---- ID_TOKEN ----\n{idToken}\n--------");
        logger.LogDebug($"---- REFRESH_TOKEN ----\n{refreshToken}\n--------");
    }
}
