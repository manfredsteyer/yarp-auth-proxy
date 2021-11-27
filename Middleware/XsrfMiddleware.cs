
using Microsoft.AspNetCore.Antiforgery;

public static class XsrfMiddleware
{

    public static void UseXsrfCookie(this WebApplication app, string apiPath) {
        app.UseXsrfCookieCreator();
        app.USeXsrfCookieChecks(apiPath);
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

    public static void USeXsrfCookieChecks(this WebApplication app, string apiPath)
    {
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