namespace Class.Domain.Constants;

public static class LabStatuses
{
    public const string PendingAssets = "pending_assets";
    public const string Active = "active";
    public const string Archived = "archived";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        PendingAssets,
        Active,
        Archived
    };

    public static bool IsValid(string status)
    {
        return All.Contains(status);
    }
}
