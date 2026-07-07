using ApprovalService.Models;

namespace ApprovalService.Services;

public static class PolicySettingsValidator
{
    public static IReadOnlyList<string> Validate(PolicySettingsUpdateRequest request)
    {
        var errors = new List<string>();

        if (request.AutonomyCeiling is <= 0m)
        {
            errors.Add("autonomyCeiling must be greater than 0.");
        }

        if (request.AutonomyConfidence is < 0m or > 1m)
        {
            errors.Add("autonomyConfidence must be between 0 and 1.");
        }

        if (request.CategoryRules is not null)
        {
            foreach (var rule in request.CategoryRules)
            {
                if (string.IsNullOrWhiteSpace(rule.Category))
                {
                    errors.Add("Each category rule must include a category name.");
                    continue;
                }

                if (rule.MaxAmount <= 0m)
                {
                    errors.Add($"Category rule for '{rule.Category}' must have maxAmount greater than 0.");
                }

                if (string.IsNullOrWhiteSpace(rule.RuleId))
                {
                    errors.Add($"Category rule for '{rule.Category}' must include a ruleId.");
                }
            }
        }

        return errors;
    }
}
