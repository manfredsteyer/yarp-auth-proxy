public record GatewayConfig {
    public string Url { get; set; } = "";
    public int SessionTimeoutInMin { get; set; }
    public string ApiPath { get; set; } = "";
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scopes { get; set; } = "";
    public string LogoutUrl { get; set; } = "";
}
