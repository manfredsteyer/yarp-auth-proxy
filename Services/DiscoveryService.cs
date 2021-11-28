public class DiscoveryService {

    private readonly string DISCO_URL = ".well-known/openid-configuration";

    private static string CombineUrls(string uri1, string uri2)
    {
        uri1 = uri1.TrimEnd('/');
        uri2 = uri2.TrimStart('/');
        return string.Format("{0}/{1}", uri1, uri2);
    }

    public async Task<DiscoveryDocument> loadDiscoveryDocument(string authority) {
        var httpClient = new HttpClient(); 

        var url = CombineUrls(authority, DISCO_URL);

        Console.WriteLine("disco: " + url);

        var doc = await httpClient.GetFromJsonAsync<DiscoveryDocument>(url);
        
        if (doc == null) {
            throw new Exception("error loading discovery document from " + url);
        }

        return doc;
    }
}