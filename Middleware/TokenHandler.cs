using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public static class TokenHandler {

    public static void HandleToken(TokenValidatedContext context)
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

    private static void ShowDebugInfo(string accessToken, string idToken, string refreshToken)
    {
        // For demonstration purposes
        // Don't do this in production!
        
        Console.Write("---- DEBUG INFOS ----");
        Console.WriteLine("ACCESS_TOKEN: " + accessToken);
        Console.Write("--------");
        Console.WriteLine("ID_TOKEN: " + idToken);
        Console.Write("--------");
        Console.WriteLine("REFRESH_TOKEN: " + refreshToken);
        Console.Write("--------\n");
    }
}
