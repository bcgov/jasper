namespace Scv.Core.Infrastructure;

public interface ICurrentUser
{
    /// <summary>
    /// User ID of the authenticated user. Null or empty if there is no authenticated user. 
    /// </summary>
    string UserId { get; }
}
