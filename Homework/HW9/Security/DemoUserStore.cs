using Microsoft.AspNetCore.Identity;
using HW9.Models;

namespace HW9.Security;

public sealed class DemoUserStore
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly Dictionary<string, UserAccount> _users;

    public DemoUserStore()
    {
        _users = new Dictionary<string, UserAccount>(StringComparer.OrdinalIgnoreCase);
        Add("alice", "p@ss", "user", "admin");
        Add("bob", "p@ss", "user");
    }

    public UserAccount? Validate(string username, string password)
    {
        if (!_users.TryGetValue(username, out var user))
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded ? user : null;
    }

    public UserAccount? Find(string username) =>
        _users.GetValueOrDefault(username);

    private void Add(string username, string password, params string[] roles)
    {
        var draft = new UserAccount(username, string.Empty, roles);
        _users[username] = draft with
        {
            PasswordHash = _hasher.HashPassword(draft, password)
        };
    }
}
