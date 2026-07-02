namespace ApprovalService.Models;

public static class ApprovalStatuses
{
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string AutoApproved = "AutoApproved";
    public const string Escalated = "Escalated";
    public const string HumanApproved = "HumanApproved";
    public const string Rejected = "Rejected";
}
