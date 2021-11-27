public class DiscoveryService {

    private readonly string DISCO_URL = ".well-known/openid-configuration";

    public async Task<DiscoveryDocument> loadDiscoveryDocument(string authority) {
        var httpClient = new HttpClient(); 

        var baseUrl = new Uri(authority);
        var url = new Uri(baseUrl, DISCO_URL);

        var doc = await httpClient.GetFromJsonAsync<DiscoveryDocument>(url);
        
        if (doc == null) {
            throw new Exception("error loading discovery document from " + url);
        }

        return doc;
    }
}