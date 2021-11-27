
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public static class TokenHandler {

    public static void HandleToken(TokenValidatedContext context) {
        if (context.TokenEndpointResponse == null)
        {
            throw new Exception("TokenEndpointResponse expected!");
        }

        var accessToken = context.TokenEndpointResponse.AccessToken;
        var idToken = context.TokenEndpointResponse.IdToken;
        var refreshToken = context.TokenEndpointResponse.RefreshToken;
        var expiresIn = context.TokenEndpointResponse.ExpiresIn;
        var expiresAt = new DateTimeOffset(DateTime.Now).AddSeconds(Convert.ToInt32(expiresIn));

        context.HttpContext.Session.SetString(SessionKeys.ACCESS_TOKEN, accessToken);
        context.HttpContext.Session.SetString(SessionKeys.ID_TOKEN, idToken);
        context.HttpContext.Session.SetString(SessionKeys.REFRESH_TOKEN, refreshToken);
        context.HttpContext.Session.SetString(SessionKeys.EXPIRES_AT, "" + expiresAt.ToUnixTimeSeconds());
        // context.HttpContext.Session.SetString("tokenEndpoint", options.Configuration.TokenEndpoint);

        // Console.WriteLine("LoggedIn");
        // Console.WriteLine("accessToken: "+ accessToken);
        // Console.WriteLine("idToken: "+ idToken);
        // Console.WriteLine("refreshToken: "+ refreshToken);
    }
}