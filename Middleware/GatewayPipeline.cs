public static class GatewayPipeline
{
    public static void UseGatewayPipeline(this IReverseProxyApplicationBuilder pipeline)
    {
        var gatewayService = pipeline.ApplicationServices.GetRequiredService<GatewayService>();

        pipeline.Use(async (ctx, next) =>
        {
            await gatewayService.AddToken(ctx);
            await next().ConfigureAwait(false);
        });
    }

}