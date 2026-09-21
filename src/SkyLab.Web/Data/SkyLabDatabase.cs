using MySqlConnector;

namespace SkyLab.Web.Data;

public sealed class SkyLabDatabase(SkyLabDatabaseOptions options)
{
    public string CurrentCompanyDatabaseName => options.CompanyDatabaseName;

    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(options.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
