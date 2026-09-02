using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace SkyLab.Web.Services;

public sealed class MobileAuthService(IDataProtectionProvider dataProtectionProvider)
{
    private readonly ITimeLimitedDataProtector protector = dataProtectionProvider
        .CreateProtector("SkyLab.Mobile.Session.v1")
        .ToTimeLimitedDataProtector();

    public string CreateSession(string username)
    {
        return protector.Protect(username, TimeSpan.FromHours(8));
    }

    public string? GetUsername(string? authorization)
    {
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var token = authorization[prefix.Length..].Trim();
        try
        {
            var username = protector.Unprotect(token, out var expiresAt);
            return expiresAt > DateTimeOffset.UtcNow ? username : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
