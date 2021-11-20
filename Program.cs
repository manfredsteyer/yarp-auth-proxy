using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

var sessionTimeoutInMin = builder.Configuration.GetValue<int>("SessionTimeoutInMin", 60);
var url = builder.Configuration.GetValue<string>("Url", "http://+:8080");

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutInMin);
});

builder.Services.AddAntiforgery(setup => {
    setup.HeaderName = "X-XSRF-TOKEN";
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("corsPolicy", builder =>
    {
        builder.AllowAnyOrigin();
    });
});

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
.AddCookie(setup => { 
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
    foreach(var scope in scopeArray) {
        options.Scope.Add(scope);
    }

    options.Events.OnTokenValidated = (context) =>
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

        context.HttpContext.Session.SetString("accessToken", accessToken);
        context.HttpContext.Session.SetString("idToken", idToken);
        context.HttpContext.Session.SetString("refreshToken", refreshToken);
        context.HttpContext.Session.SetString("expiresAt", "" + expiresAt.ToUnixTimeSeconds());

        // Console.WriteLine("LoggedIn");
        // Console.WriteLine("accessToken: "+ accessToken);
        // Console.WriteLine("idToken: "+ idToken);
        // Console.WriteLine("refreshToken: "+ refreshToken);

        return Task.FromResult(0);
    };
});

var app = builder.Build();

app.UseRouting();
app.UseCors();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseCookiePolicy();

app.Use((ctx, next) => {
    var antiforgery = app.Services.GetService<IAntiforgery>();

    if (antiforgery == null) {
        throw new Exception("IAntiforgery service exptected!");
    }

    var tokens = antiforgery.GetAndStoreTokens(ctx);

    if (tokens.RequestToken == null) {
        throw new Exception("token exptected!");
    }

    ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, 
                new CookieOptions() { HttpOnly = false });
    return next(ctx);
});

app.MapGet("/userinfo", (ClaimsPrincipal user) => {

    var claims = user.Claims;
    var dict = new Dictionary<string, string>();

    foreach (var entry in claims)
    {
        dict[entry.Type] = entry.Value;
    }

    return dict;
});

app.MapGet("/login", (string? redirectUrl, HttpContext ctx) => {

    if (string.IsNullOrEmpty(redirectUrl)) {
        redirectUrl = "/";
    }

    ctx.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties{
        RedirectUri = redirectUrl
    });
});

app.MapGet("/logout", (string? redirectUrl, HttpContext ctx) => {
    if (string.IsNullOrEmpty(redirectUrl)) {
        redirectUrl = "/";
    }

    ctx.Session.Clear();

    var authProps = new AuthenticationProperties {
        RedirectUri = redirectUrl
    };

    var authSchemes = new string[] {
        CookieAuthenticationDefaults.AuthenticationScheme,
        OpenIdConnectDefaults.AuthenticationScheme
    };

    return Results.SignOut(authProps, authSchemes);
});

app.MapReverseProxy(pipeline =>
{
    pipeline.Use(async (ctx, next) =>
    {
        // var token = await ctx.GetTokenAsync("access_token");
        // foreach(var key in ctx.Session.Keys) {
        //     Console.Write("Session: " + key + ": " + ctx.Session.GetString(key) );
        // }

        var token = ctx.Session.GetString("accessToken");
        // Console.WriteLine("Add access_token: " + token);

        // TODO: Refresh Token if expired

        ctx.Request.Headers.Add("Authorization", "Bearer " + token);
        await next().ConfigureAwait(false);
    });
});

app.Run(url);