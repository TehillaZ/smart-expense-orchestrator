using System.ComponentModel.DataAnnotations;
using ApiGateway.Auth;

namespace ApiGateway.Routes;

public static class AuthRoutes
{
    public static IEndpointRouteBuilder MapAuthRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", (
            LoginRequest request,
            JwtTokenService tokenService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthRoutes");

            if (!AppRoles.IsValid(request.Role))
            {
                return Results.BadRequest(new { message = "Role must be Submitter, Approver, or Admin." });
            }

            try
            {
                var username = string.IsNullOrWhiteSpace(request.Username)
                    ? request.Role.ToLowerInvariant()
                    : request.Username.Trim();

                var tokenResult = tokenService.CreateToken(request.Role, username);

                logger.LogInformation("Issued JWT for role {Role} and user {Username}.", request.Role, username);

                return Results.Ok(new LoginResponse(
                    tokenResult.Token,
                    tokenResult.Role,
                    username,
                    tokenResult.ExpiresAt));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to issue JWT for role {Role}.", request.Role);
                return Results.Problem("Unable to issue authentication token.");
            }
        })
        .WithName("Login")
        .AllowAnonymous();

        return app;
    }
}

public sealed record LoginRequest(
    [property: Required] string Role,
    string? Username);

public sealed record LoginResponse(
    string Token,
    string Role,
    string Username,
    DateTimeOffset ExpiresAt);
