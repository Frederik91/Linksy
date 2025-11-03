namespace Linksy.Api.Models;

/// <summary>
/// Conflict resolution policy for sync bindings
/// </summary>
public enum ConflictPolicy
{
    SourceWins,
    TargetWins,
    LastWriterWins,
    ManualHold
}
