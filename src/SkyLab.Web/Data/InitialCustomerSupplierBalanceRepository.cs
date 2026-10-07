using SkyLab.Web.Models;
using MySqlConnector;

namespace SkyLab.Web.Data;

public sealed class InitialCustomerSupplierBalanceRepository(SkyLabDatabase database)
{
    public async Task<InitialCustomerSupplierBalanceList> ListAsync(
        int year,
        int currentYear,
        CancellationToken cancellationToken = default)
    {
        var customers = await ListRowsAsync("C", "Clienti", year, cancellationToken);
        var suppliers = await ListRowsAsync("F", "Fornitori", year, cancellationToken);
        var years = Enumerable.Range(currentYear - 5, 6)
            .Reverse()
            .ToArray();

        return new InitialCustomerSupplierBalanceList(year, years, customers, suppliers);
    }

    public async Task SaveAsync(
        int year,
        IReadOnlyList<InitialCustomerSupplierBalanceSaveRow> rows,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var customerCodes = await ReadCodesAsync(connection, transaction, "Clienti", cancellationToken);
        var supplierCodes = await ReadCodesAsync(connection, transaction, "Fornitori", cancellationToken);
        var expected = customerCodes.Select(code => (Type: "C", Code: code))
            .Concat(supplierCodes.Select(code => (Type: "F", Code: code)))
            .ToHashSet();
        var received = new HashSet<(string Type, int Code)>();

        if (rows.Any(row => row is null
                || row.Type is not ("C" or "F")
                || row.Code <= 0
                || !received.Add((row.Type, row.Code)))
            || !received.SetEquals(expected))
        {
            throw new InvalidOperationException("L'elenco dei saldi è incompleto o non valido. Ricaricare la pagina e riprovare.");
        }

        await using (var delete = new MySqlCommand(
            "DELETE FROM SaldoIniCf WHERE Anno = @year;",
            connection,
            transaction))
        {
            delete.Parameters.Add("@year", MySqlDbType.Int32).Value = year;
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        var rowsToInsert = rows.Where(row => row.Balance != 0).ToArray();
        const int batchSize = 500;
        foreach (var batch in rowsToInsert.Chunk(batchSize))
        {
            var values = string.Join(", ", batch.Select((_, index) =>
                $"(@year{index}, @type{index}, @code{index}, @balance{index})"));
            var sql = $"INSERT INTO SaldoIniCf (Anno, CliFor, Ditta, Importo) VALUES {values};";
            await using var command = new MySqlCommand(sql, connection, transaction);

            for (var index = 0; index < batch.Length; index++)
            {
                var row = batch[index];
                command.Parameters.Add($"@year{index}", MySqlDbType.Int32).Value = year;
                command.Parameters.Add($"@type{index}", MySqlDbType.VarChar).Value = row.Type;
                command.Parameters.Add($"@code{index}", MySqlDbType.Int32).Value = row.Code;
                command.Parameters.Add($"@balance{index}", MySqlDbType.Decimal).Value = row.Balance;
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<HashSet<int>> ReadCodesAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand($"SELECT Codice FROM {table};", connection, transaction);
        var codes = new HashSet<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            codes.Add(Convert.ToInt32(reader[0]));
        }

        return codes;
    }

    private async Task<IReadOnlyList<InitialCustomerSupplierBalanceRow>> ListRowsAsync(
        string type,
        string table,
        int year,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                a.Codice,
                COALESCE(a.Nome, '') AS Nome,
                COALESCE(a.Citta, '') AS Citta,
                COALESCE(a.Piva, '') AS Piva,
                COALESCE(s.Importo, 0) AS Importo
            FROM {table} a
            LEFT JOIN SaldoIniCf s
                ON s.Anno = @year
               AND s.CliFor = @type
               AND s.Ditta = a.Codice
            ORDER BY a.Codice;
            """;

        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.Add("@year", MySqlDbType.Int32).Value = year;
        command.Parameters.Add("@type", MySqlDbType.VarChar).Value = type;

        var rows = new List<InitialCustomerSupplierBalanceRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new InitialCustomerSupplierBalanceRow(
                type,
                Convert.ToInt32(reader["Codice"]),
                Convert.ToString(reader["Nome"]) ?? "",
                Convert.ToString(reader["Citta"]) ?? "",
                Convert.ToString(reader["Piva"]) ?? "",
                Convert.ToDecimal(reader["Importo"])));
        }

        return rows;
    }
}
