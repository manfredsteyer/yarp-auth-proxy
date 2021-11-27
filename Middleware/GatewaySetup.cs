
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

public static class GatewaySetup
{

    public static void AddGateway(this WebApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        var sessionTimeoutInMin = builder.Configuration.GetValue<int>("Gateway:SessionTimeoutInMin", 60);
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutInMin);
        });

        builder.Services.AddAntiforgery(setup =>
        {
            setup.HeaderName = "X-XSRF-TOKEN";
        });

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("authPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        });

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(setup =>
        {
            setup.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutInMin);
            setup.SlidingExpiration = true;
        })
        .AddOpenIdConnect(options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = builder.Configuration.GetValue<string>("OpenIdConnect:Authority");
            options.ClientId = builder.Configuration.GetValue<string>("OpenIdConnect:ClientId");
            options.UsePkce = true;
            options.ClientSecret = builder.Configuration.GetValue<string>("OpenIdConnect:ClientSecret");
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.RequireHttpsMetadata = false;

            var scopes = builder.Configuration.GetValue<string>("OpenIdConnect:Scopes");
            var scopeArray = scopes.Split(" ");
            foreach (var scope in scopeArray)
            {
                options.Scope.Add(scope);
            }

            options.Events.OnTokenValidated = (context) =>
            {
                TokenHandler.HandleToken(context);
                return Task.FromResult(0);
            };
        });
    }

    public static void UseGateway(this WebApplication app, string apiPath) {
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCookiePolicy();

        app.UseXsrfCookie(apiPath);
        app.UseGatewayEndpoints();

        app.MapReverseProxy(pipeline =>
        {
            pipeline.UseGatewayPipeline(apiPath);
        });
    }
}
