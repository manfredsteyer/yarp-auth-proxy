namespace YarpAuthProxy.BffLibrary.Services;

public class RefreshResponse
{
    public string access_token { get; set; } = "";
    public string id_token { get; set; } = "";
    public string refresh_token { get; set; } = "";
    public long expires { get; set; }
}
