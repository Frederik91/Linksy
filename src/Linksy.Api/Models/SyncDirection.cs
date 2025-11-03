namespace Linksy.Api.Models;

/// <summary>
/// Direction of file synchronization
/// </summary>
public enum SyncDirection
{
    OneWaySourceToTarget,
    OneWayTargetToSource,
    Bidirectional
}
