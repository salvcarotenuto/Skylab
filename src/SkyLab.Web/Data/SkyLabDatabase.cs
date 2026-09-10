using MySqlConnector;

namespace SkyLab.Web.Data;

public sealed class SkyLabDatabase(IConfiguration configuration)
{
    public string CurrentCompanyDatabaseName => "skylab_0001";

    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var configured = configuration.GetConnectionString("SkyLab")
            ?? throw new InvalidOperationException("Connessione MySQL SkyLab non configurata.");
        var builder = new MySqlConnectionStringBuilder(configured)
        {
            Database = CurrentCompanyDatabaseName,
            SslMode = MySqlSslMode.None
        };
        var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
