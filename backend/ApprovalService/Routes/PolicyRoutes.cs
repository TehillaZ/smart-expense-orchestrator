using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Routes;

public static class PolicyRoutes
{
    public static IEndpointRouteBuilder MapPolicyRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/policy", async (IPolicySettingsStore policySettingsStore) =>
        {
            var settings = await policySettingsStore.GetAsync();
            return Results.Ok(settings);
        })
        .WithName("GetPolicySettings");

        app.MapPut("/policy", async (
            PolicySettingsUpdateRequest request,
            IPolicySettingsStore policySettingsStore,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PolicyRoutes");
            var errors = PolicySettingsValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(new ValidationErrorResponse(errors));
            }

            var current = await policySettingsStore.GetAsync();
            var hasChanges = request.AutonomyCeiling is not null
                || request.AutonomyConfidence is not null
                || request.CategoryRules is not null;

            var updated = current with
            {
                AutonomyCeiling = request.AutonomyCeiling ?? current.AutonomyCeiling,
                AutonomyConfidence = request.AutonomyConfidence ?? current.AutonomyConfidence,
                CategoryRules = request.CategoryRules ?? current.CategoryRules,
                PolicyVersion = hasChanges
                    ? PolicyVersionHelper.Bump(current.PolicyVersion)
                    : current.PolicyVersion,
            };

            await policySettingsStore.SaveAsync(updated);
            logger.LogInformation(
                "Updated policy settings. Ceiling={Ceiling}, Confidence={Confidence}.",
                updated.AutonomyCeiling,
                updated.AutonomyConfidence);

            return Results.Ok(updated);
        })
        .WithName("UpdatePolicySettings");

        return app;
    }
}
