using Microsoft.AspNetCore.Antiforgery;

public static class XsrfMiddleware
{
    public static void UseXsrfCookie(this WebApplication app) {
        app.UseXsrfCookieCreator();
        app.UseXsrfCookieChecks();
    }

    public static void UseXsrfCookieCreator(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var antiforgery = app.Services.GetService<IAntiforgery>();

            if (antiforgery == null)
            {
                throw new Exception("IAntiforgery service exptected!");
            }

            var tokens = antiforgery!.GetAndStoreTokens(ctx);

            if (tokens.RequestToken == null)
            {
                throw new Exception("token exptected!");
            }

            ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken,
                        new CookieOptions() { HttpOnly = false });

            await next(ctx);
        });
    }

    public static void UseXsrfCookieChecks(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<GatewayConfig>();
        var apiPath = config.ApiPath;

        app.Use(async (ctx, next) =>
        {
            var antiforgery = app.Services.GetService<IAntiforgery>();

            if (antiforgery == null)
            {
                throw new Exception("IAntiforgery service exptected!");
            }

            var currentUrl = ctx.Request.Path.ToString().ToLower();
            if (currentUrl.StartsWith(apiPath)
                && !await antiforgery.IsRequestValidAsync(ctx))
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    Error = "XSRF token validadation failed"
                });
                return;
            }

            await next(ctx);
        });
    }

}
