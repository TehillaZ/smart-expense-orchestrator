namespace ApiGateway.Routes;

public static class HealthRoutes
{
    public static IEndpointRouteBuilder MapHealthRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "ApiGateway" }))
            .WithName("GatewayHealth");

        return app;
    }
}
