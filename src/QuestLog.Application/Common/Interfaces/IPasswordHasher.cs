namespace QuestLog.Application.Common.Interfaces;

/// <summary>
/// Abstracts password hashing so the Application layer doesn't depend
/// on the BCrypt library directly (which lives in Infrastructure).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
