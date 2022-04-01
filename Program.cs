using System.IdentityModel.Tokens.Jwt;

// Disable claim mapping to get claims 1:1 from the tokens
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

if (args.Count() > 0) {
    builder.Configuration.AddJsonFile(args[0], false, true);
}

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
if (string.IsNullOrEmpty(config.Url)) {
    app.Run();
}
else {
    app.Run(config.Url);
}

