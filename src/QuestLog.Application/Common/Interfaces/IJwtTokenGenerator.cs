namespace QuestLog.Application.Common.Interfaces;

/// <summary>
/// Generates a signed JWT token for a given user.
/// Lives in Application so the auth command handler can reference it
/// without depending on the Infrastructure JWT library directly.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string username);
}
