using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace SkyLab.Web.Services;

public sealed class MobileAuthService
{
    private readonly ConcurrentDictionary<string, Session> sessions = new();

    public string CreateSession(string username)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        sessions[token] = new(username, DateTimeOffset.UtcNow.AddHours(8));
        return token;
    }

    public string? GetUsername(string? authorization)
    {
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var token = authorization[prefix.Length..].Trim();
        if (!sessions.TryGetValue(token, out var session)) return null;
        if (session.ExpiresAt > DateTimeOffset.UtcNow) return session.Username;
        sessions.TryRemove(token, out _);
        return null;
    }

    private sealed record Session(string Username, DateTimeOffset ExpiresAt);
}
