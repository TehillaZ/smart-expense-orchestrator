namespace PaymentService.Models;

public static class PaymentStatuses
{
    public const string PaymentPending = "PaymentPending";
    public const string Paid = "Paid";
    public const string PaymentFailed = "PaymentFailed";
    public const string Compensated = "Compensated";
}

public static class PaymentSagaPhases
{
    public const string Started = "Started";
    public const string Reserved = "Reserved";
    public const string Executed = "Executed";
    public const string Compensated = "Compensated";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class ApprovalOutcomeStatuses
{
    public const string AutoApproved = "AutoApproved";
    public const string HumanApproved = "HumanApproved";
}
