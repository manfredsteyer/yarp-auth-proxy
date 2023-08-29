public static class GetewayConfigReader
{
    public static GatewayConfig GetGatewayConfig(this ConfigurationManager config)
    {
        var result = new GatewayConfig
        {
            Url = config.GetValue<string>("Gateway:Url", ""),
            SessionTimeoutInMin = config.GetValue<int>("Gateway:SessionTimeoutInMin", 60),
            TokenExchangeStrategy = config.GetValue<string>("Gateway:TokenExchangeStrategy", ""),
            
            Authority = config.GetValue<string>("OpenIdConnect:Authority"),
            ClientId = config.GetValue<string>("OpenIdConnect:ClientId"),
            ClientSecret = config.GetValue<string>("OpenIdConnect:ClientSecret"),
            Scopes = config.GetValue<string>("OpenIdConnect:Scopes", ""),
            LogoutUrl = config.GetValue<string>("OpenIdConnect:LogoutUrl", ""),
            QueryUserInfoEndpoint = config.GetValue<bool>("OpenIdConnect:QueryUserInfoEndpoint", true),

            ApiConfigs = config.GetSection("Apis").Get<ApiConfig[]>(),
        };

        return result;
    }

}