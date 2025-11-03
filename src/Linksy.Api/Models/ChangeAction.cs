namespace Linksy.Api.Models;

/// <summary>
/// Action to be performed on a file or folder
/// </summary>
public enum ChangeAction
{
    Create,
    Update,
    Delete,
    Move,
    Rename
}
