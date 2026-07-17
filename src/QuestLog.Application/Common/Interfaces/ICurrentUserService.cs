namespace QuestLog.Application.Common.Interfaces;

/// <summary>
/// Provides the authenticated user's ID to Application layer handlers,
/// abstracting away the HTTP context so handlers stay testable.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
}
