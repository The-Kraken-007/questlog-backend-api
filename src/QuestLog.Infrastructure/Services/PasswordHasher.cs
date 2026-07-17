using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Infrastructure.Services;

/// <summary>
/// BCrypt implementation of IPasswordHasher.
/// Work factor 12 is a good balance of security vs. speed for 2024+.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
