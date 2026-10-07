using MySqlConnector;
using SkyLab.Web.Models;

namespace SkyLab.Web.Data;

public sealed class AccountingCauseRepository(SkyLabDatabase database)
{
    public async Task<IReadOnlyList<AccountingCauseAccountOption>> GetAccountOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Codice,
                   COALESCE(c.Descrizione, '') AS Descrizione,
                   COALESCE(c.Mastro, 0) AS Mastro,
                   COALESCE(m.Descrizione, '') AS MastroDescrizione,
                   COALESCE(c.Tipo, '') AS Tipo
            FROM conti c
            LEFT JOIN mastri m ON m.Codice = c.Mastro
            ORDER BY c.Codice;
            """;

        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        var options = new List<AccountingCauseAccountOption>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new AccountingCauseAccountOption(
                Convert.ToInt32(reader["Codice"]),
                reader.GetString("Descrizione"),
                Convert.ToInt32(reader["Mastro"]),
                reader.GetString("MastroDescrizione"),
                reader.GetString("Tipo").Trim().ToUpperInvariant()));
        }

        return options;
    }
}
