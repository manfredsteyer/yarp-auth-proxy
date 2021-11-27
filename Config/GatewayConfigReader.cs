
public static class GetewayConfigReader
{
    public static GatewayConfig GetGatewayConfig(this ConfigurationManager config)
    {
        var result = new GatewayConfig
        {
            Url = config.GetValue<string>("Gateway:Url", ""),
            SessionTimeoutInMin = config.GetValue<int>("Gateway:SessionTimeoutInMin", 60),
            ApiPath = config.GetValue<string>("Gateway:ApiPath", "/api/"),
            Authority = config.GetValue<string>("OpenIdConnect:Authority"),
            ClientId = config.GetValue<string>("OpenIdConnect:ClientId"),
            ClientSecret = config.GetValue<string>("OpenIdConnect:ClientSecret"),
            Scopes = config.GetValue<string>("OpenIdConnect:Scopes", "")
        };

        return result;
    }

}