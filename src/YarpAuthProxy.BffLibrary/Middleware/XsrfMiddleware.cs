using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using YarpAuthProxy.BffLibrary.Utils.Config;

namespace YarpAuthProxy.BffLibrary.Middleware;

public static class XsrfMiddleware
{
    public static void UseXsrfCookie(this WebApplication app)
    {
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
        var apiConfigs = config.ApiConfigs;

        app.Use(async (ctx, next) =>
        {
            var antiforgery = app.Services.GetService<IAntiforgery>();

            if (antiforgery == null)
            {
                throw new Exception("IAntiforgery service exptected!");
            }

            var currentUrl = ctx.Request.Path.ToString().ToLower();
            if (apiConfigs.Any(c => currentUrl.StartsWith(c.ApiPath))
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
