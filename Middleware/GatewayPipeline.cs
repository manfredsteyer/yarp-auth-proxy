
public static class GatewayPipeline
{

    public static void UseGatewayPipeline(this IReverseProxyApplicationBuilder pipeline, string apiPath)
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

        var currentUrl = ctx.Request.Path.ToString().ToLower();
               if (!string.IsNullOrEmpty(token) && currentUrl.StartsWith(apiPath))
               {
                   ctx.Request.Headers.Add("Authorization", "Bearer " + token);
               }
               await next().ConfigureAwait(false);
           });
    }

}