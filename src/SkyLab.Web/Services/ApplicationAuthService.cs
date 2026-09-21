using MySqlConnector;
using SkyLab.Web.Data;

namespace SkyLab.Web.Services;

// Micronote session convention, with automatic company selection for this release.
public sealed class ApplicationAuthService(IHttpContextAccessor accessor, SkyLabDatabaseOptions options)
{
    public const string UserCodeKey = "skylab_user_code";
    private const string LastSeenKey = "skylab_last_seen";
    public bool IsLoggedIn()
    {
        var session = accessor.HttpContext?.Session;
        if (session?.GetInt32(UserCodeKey) is not > 0) return false;
        if (!long.TryParse(session.GetString(LastSeenKey), out var lastSeen)
            || DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSeen > 1800)
        {
            session.Clear();
            return false;
        }
        Touch(session);
        return true;
    }
    public async Task<bool> LoginAsync(string username, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
        await using var connection = new MySqlConnection(options.BuildCompanyConnectionString());
        await connection.OpenAsync(ct);
        await using var command = new MySqlCommand("SELECT Codice FROM Utenti WHERE Username=@username AND Passwd=@password AND COALESCE(Attivo,0)<>0 AND COALESCE(Bloccato,0)=0 AND COALESCE(Tipo,0) IN (1,2,3) LIMIT 1", connection);
        command.Parameters.AddWithValue("@username", username.Trim());
        command.Parameters.AddWithValue("@password", password);
        var code = await command.ExecuteScalarAsync(ct);
        if (code is null || code is DBNull) return false;
        var session = accessor.HttpContext!.Session;
        session.Clear();
        session.SetInt32(UserCodeKey, Convert.ToInt32(code));
        session.SetString("skylab_user_name", username.Trim());
        session.SetInt32("skylab_company_code", options.CompanyCode);
        session.SetString("skylab_company_database", options.CompanyDatabaseName);
        Touch(session);
        return true;
    }
    public void Logout() => accessor.HttpContext?.Session.Clear();
    private static void Touch(ISession session) => session.SetString(LastSeenKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
}
