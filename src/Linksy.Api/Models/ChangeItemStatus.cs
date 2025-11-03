namespace Linksy.Api.Models;

/// <summary>
/// Status of an individual change item
/// </summary>
public enum ChangeItemStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Quarantined,
    Skipped
}
