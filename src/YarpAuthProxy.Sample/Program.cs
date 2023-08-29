using System.IdentityModel.Tokens.Jwt;

using YarpAuthProxy.BffLibrary.Middleware;
using YarpAuthProxy.BffLibrary.Services;
using YarpAuthProxy.BffLibrary.Utils.Config;

// Disable claim mapping to get claims 1:1 from the tokens
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigFiles();

// Read config and OIDC discovery document
var config = builder.Configuration.GetGatewayConfig();
var discoService = new DiscoveryService();
var disco = await discoService.loadDiscoveryDocument(config.Authority);

// Configure Services
builder.Services.AddDistributedMemoryCache();
builder.AddGateway(config, disco);

// Build App and add Middleware
var app = builder.Build();
app.UseGateway();

// Start Gateway
if (string.IsNullOrEmpty(config.Url))
{
    app.Run();
}
else
{
    app.Run(config.Url);
}