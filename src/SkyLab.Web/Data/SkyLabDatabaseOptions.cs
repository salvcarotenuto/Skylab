using System.Globalization;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace SkyLab.Web.Data;

// Same configuration convention as Micronote; company 0001 is automatic for this release.
public sealed class SkyLabDatabaseOptions(IConfiguration configuration)
{
    private string? registeredDatabase;
    public int CompanyCode => configuration.GetValue("SkyLab:Database:DefaultCompanyCode", 1);
    public string CompanyKey => CompanyCode.ToString("0000", CultureInfo.InvariantCulture);
    public string MasterDatabase => Validate(configuration["SkyLab:Database:MasterDatabase"] ?? "skylab_master");
    public string CompanyDatabaseName
    {
        get
        {
            if (CompanyCode is < 1 or > 9999) throw new InvalidOperationException("Codice azienda non valido.");
            if (registeredDatabase is not null) return registeredDatabase;
            return Validate(string.Format(CultureInfo.InvariantCulture,
                configuration["SkyLab:Database:CompanyDatabasePattern"] ?? "skylab_{0:0000}", CompanyCode));
        }
    }
    public string BuildCompanyConnectionString() => BuildConnectionString(CompanyDatabaseName);
    public string BuildMasterConnectionString() => BuildConnectionString(MasterDatabase);
    public async Task LoadDefaultCompanyAsync(CancellationToken ct = default)
    {
        if (!configuration.GetValue("SkyLab:Database:UseMasterRegistry", false)) return;
        await using var connection = new MySqlConnection(BuildMasterConnectionString());
        await connection.OpenAsync(ct);
        await using var command = new MySqlCommand("SELECT NomeDatabase,Attiva,Bloccata FROM Aziende WHERE Codice=@code", connection);
        command.Parameters.AddWithValue("@code", CompanyCode);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || !reader.GetBoolean(1) || reader.GetBoolean(2))
            throw new InvalidOperationException("Azienda SkyLab assente, inattiva o bloccata nel registro centrale.");
        if (!reader.IsDBNull(0) && !string.IsNullOrWhiteSpace(reader.GetString(0)))
            registeredDatabase = Validate(reader.GetString(0));
    }
    private string BuildConnectionString(string database)
    {
        var configured = configuration.GetConnectionString("SkyLabServer")
            ?? configuration.GetConnectionString("SkyLab")
            ?? throw new InvalidOperationException("Connessione MySQL SkyLab non configurata.");
        return new MySqlConnectionStringBuilder(configured) { Database = database }.ConnectionString;
    }
    private static string Validate(string name)
    {
        var value = name.Trim();
        if (!Regex.IsMatch(value, "^[A-Za-z0-9_]+$")) throw new InvalidOperationException("Nome database non valido.");
        return value;
    }
}
