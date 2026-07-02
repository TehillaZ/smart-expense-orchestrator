using ApprovalService.Models;

namespace ApprovalService.Services;

public interface IPolicySettingsStore
{
    Task<PolicySettings> GetAsync();

    Task SaveAsync(PolicySettings settings);
}
