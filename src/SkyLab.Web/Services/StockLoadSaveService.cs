using MySqlConnector;
using SkyLab.Web.Data;

namespace SkyLab.Web.Services;

public sealed class StockLoadSaveService(SkyLabDatabaseOptions databaseOptions, ApplicationState applicationState)
{
    private const int Sector = 10;

    public async Task<StockLoadCheckResult> CheckAsync(StockLoadSaveCommand command, CancellationToken ct)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(ct);
        return await ValidateAsync(connection, null, command, ct);
    }

    public async Task<StockLoadSaveResult> SaveAsync(StockLoadSaveCommand command, CancellationToken ct)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var check = await ValidateAsync(connection, transaction, command, ct);
            if (!check.Success)
            {
                await transaction.RollbackAsync(ct);
                return new(false, check.Message, 0, 0, check.MissingArticles, false, false);
            }
            if (check.IsDuplicate && !command.AllowDuplicate)
            {
                await transaction.RollbackAsync(ct);
                return new(false, check.Message, 0, 0, check.MissingArticles, false, true);
            }
            if (check.MissingArticles.Count > 0 && !command.AllowMissingArticles)
            {
                await transaction.RollbackAsync(ct);
                return new(false, MissingMessage(check.MissingArticles), 0, 0, check.MissingArticles, true, false);
            }

            var rows = command.Rows
                .Where(row => !check.MissingArticles.Contains(row.ArticleCode, StringComparer.OrdinalIgnoreCase))
                .Select((row, index) => Normalize(row, index + 1))
                .ToArray();
            if (rows.Length == 0)
            {
                await transaction.RollbackAsync(ct);
                return new(false, "Nessun articolo presente in anagrafica da registrare.", 0, 0, check.MissingArticles, false, false);
            }

            var existing = command.Id > 0 ? await ExistingAsync(connection, transaction, command.Id, ct) : null;
            if (command.Id > 0 && existing is null)
            {
                await transaction.RollbackAsync(ct);
                return new(false, "Documento di carico non trovato.", 0, 0, check.MissingArticles, false, false);
            }

            var code = existing?.Code ?? await NextCodeAsync(connection, transaction, command.Year, ct);
            var goods = rows.Sum(row => row.Amount);
            var vat = rows.Sum(row => Math.Round(row.Amount * row.VatRate / 100m, 2, MidpointRounding.AwayFromZero));
            var total = goods + vat;
            var id = existing?.Id ?? await InsertHeaderAsync(connection, transaction, command, code, total, ct);
            if (existing is not null)
            {
                await UpdateHeaderAsync(connection, transaction, command, code, total, ct);
                await DeleteRowsAsync(connection, transaction, existing, ct);
            }
            await InsertRowsAsync(connection, transaction, command, id, code, rows, ct);
            await transaction.CommitAsync(ct);
            return new(true, "", id, code, check.MissingArticles, false, false);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<StockLoadCheckResult> ValidateAsync(MySqlConnection connection, MySqlTransaction? transaction, StockLoadSaveCommand command, CancellationToken ct)
    {
        command.Year = applicationState.Esercizio;
        if (string.IsNullOrWhiteSpace(command.DocumentNumber)) return Error("Numero documento obbligatorio.");
        if (command.DocumentDate is null) return Error("Data documento obbligatoria.");
        if (command.CauseCode is not (10 or 12)) return Error("Tipo carico obbligatorio o non valido.");
        if (command.PartyCode <= 0) return Error(command.CauseCode == 12 ? "Cliente obbligatorio." : "Fornitore obbligatorio.");
        if (command.DocumentDate.Value.Year != applicationState.Esercizio)
            return Error($"La data documento deve appartenere all'esercizio in linea ({applicationState.Esercizio}).");
        if (command.Rows.Count == 0 || command.Rows.All(row => string.IsNullOrWhiteSpace(row.ArticleCode)))
            return Error("Inserire almeno una riga articolo.");

        var table = command.CauseCode == 12 ? "clienti" : "fornitori";
        await using (var party = new MySqlCommand($"SELECT COUNT(*) FROM {table} WHERE Codice=@code;", connection, transaction))
        {
            party.Parameters.AddWithValue("@code", command.PartyCode);
            if (Convert.ToInt32(await party.ExecuteScalarAsync(ct)) == 0)
                return Error(command.CauseCode == 12 ? "Cliente non presente in anagrafica." : "Fornitore non presente in anagrafica.");
        }

        var missing = new List<string>();
        foreach (var code in command.Rows.Select(row => row.ArticleCode.Trim()).Where(code => code.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await using var article = new MySqlCommand("SELECT COUNT(*) FROM articoli WHERE Codice=@code;", connection, transaction);
            article.Parameters.AddWithValue("@code", code);
            if (Convert.ToInt32(await article.ExecuteScalarAsync(ct)) == 0) missing.Add(code);
        }
        var duplicateCode = await DuplicateCodeAsync(connection, transaction, command, ct);
        var message = duplicateCode.HasValue
            ? $"Il documento risulta già registrato nella partita {duplicateCode.Value:000000}. Registrarlo nuovamente?"
            : missing.Count > 0 ? MissingMessage(missing) : "";
        return new(true, message, missing, duplicateCode.HasValue);
    }

    private static StockLoadCheckResult Error(string message) => new(false, message, [], false);
    private static string MissingMessage(IReadOnlyList<string> articles) =>
        $"I seguenti articoli non sono presenti in anagrafica e non saranno registrati: {string.Join(", ", articles)}. Procedere con il salvataggio delle altre righe?";

    private static StockLoadSaveRow Normalize(StockLoadSaveRow row, int number)
    {
        var quantity = Math.Round(row.Quantity, 4, MidpointRounding.AwayFromZero);
        var price = Math.Round(row.Price, 4, MidpointRounding.AwayFromZero);
        var discount = Math.Round(row.Discount, 4, MidpointRounding.AwayFromZero);
        var amount = Math.Round(quantity * price * (1m - discount / 100m), 2, MidpointRounding.AwayFromZero);
        return row with { RowNumber = number, ArticleCode = row.ArticleCode.Trim(), UnitMeasure = row.UnitMeasure.Trim(), Quantity = quantity, Price = price, Discount = discount, Amount = amount, VatRate = Math.Round(row.VatRate, 2, MidpointRounding.AwayFromZero) };
    }

    private static async Task<ExistingDocument?> ExistingAsync(MySqlConnection connection, MySqlTransaction transaction, int id, CancellationToken ct)
    {
        await using var sql = new MySqlCommand("SELECT ID,Anno,Codice FROM carico WHERE ID=@id LIMIT 1;", connection, transaction);
        sql.Parameters.AddWithValue("@id", id);
        await using var reader = await sql.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(Convert.ToInt32(reader["ID"]), Convert.ToInt32(reader["Anno"]), Convert.ToInt32(reader["Codice"])) : null;
    }

    private static async Task<int?> DuplicateCodeAsync(MySqlConnection connection, MySqlTransaction? transaction, StockLoadSaveCommand command, CancellationToken ct)
    {
        const string statement = "SELECT Codice FROM carico WHERE CliFor=@partyType AND Ditta=@partyCode AND UPPER(TRIM(COALESCE(NumDoc,'')))=@number AND DataDoc=@date AND (@id=0 OR ID<>@id) ORDER BY ID LIMIT 1;";
        await using var sql = new MySqlCommand(statement, connection, transaction);
        sql.Parameters.AddWithValue("@partyType", PartyType(command));
        sql.Parameters.AddWithValue("@partyCode", command.PartyCode);
        sql.Parameters.AddWithValue("@number", command.DocumentNumber.Trim().ToUpperInvariant());
        sql.Parameters.Add("@date", MySqlDbType.Date).Value = command.DocumentDate!.Value.ToDateTime(TimeOnly.MinValue);
        sql.Parameters.AddWithValue("@id", command.Id);
        var value = await sql.ExecuteScalarAsync(ct);
        return value is null or DBNull ? null : Convert.ToInt32(value);
    }

    private static async Task<int> NextCodeAsync(MySqlConnection connection, MySqlTransaction transaction, int year, CancellationToken ct)
    {
        await using var sql = new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM carico WHERE Anno=@year FOR UPDATE;", connection, transaction);
        sql.Parameters.AddWithValue("@year", year);
        return Convert.ToInt32(await sql.ExecuteScalarAsync(ct));
    }

    private static async Task<int> InsertHeaderAsync(MySqlConnection connection, MySqlTransaction transaction, StockLoadSaveCommand command, int code, decimal total, CancellationToken ct)
    {
        const string statement = "INSERT INTO carico (Anno,Codice,Causale,NumDoc,DataDoc,CliFor,Ditta,ULocale,Totale) VALUES (@year,@code,@cause,@number,@date,@partyType,@partyCode,@store,@total);";
        await using var sql = new MySqlCommand(statement, connection, transaction);
        AddHeaderParameters(sql, command, code, total);
        await sql.ExecuteNonQueryAsync(ct);
        return Convert.ToInt32(sql.LastInsertedId);
    }

    private static async Task UpdateHeaderAsync(MySqlConnection connection, MySqlTransaction transaction, StockLoadSaveCommand command, int code, decimal total, CancellationToken ct)
    {
        const string statement = "UPDATE carico SET Anno=@year,Codice=@code,Causale=@cause,NumDoc=@number,DataDoc=@date,CliFor=@partyType,Ditta=@partyCode,ULocale=@store,Totale=@total WHERE ID=@id;";
        await using var sql = new MySqlCommand(statement, connection, transaction);
        AddHeaderParameters(sql, command, code, total);
        sql.Parameters.AddWithValue("@id", command.Id);
        await sql.ExecuteNonQueryAsync(ct);
    }

    private static void AddHeaderParameters(MySqlCommand sql, StockLoadSaveCommand command, int code, decimal total)
    {
        sql.Parameters.AddWithValue("@year", command.Year);
        sql.Parameters.AddWithValue("@code", code);
        sql.Parameters.AddWithValue("@number", command.DocumentNumber.Trim());
        sql.Parameters.Add("@date", MySqlDbType.Date).Value = command.DocumentDate!.Value.ToDateTime(TimeOnly.MinValue);
        sql.Parameters.AddWithValue("@cause", command.CauseCode);
        sql.Parameters.AddWithValue("@partyType", PartyType(command));
        sql.Parameters.AddWithValue("@partyCode", command.PartyCode);
        sql.Parameters.AddWithValue("@total", total);
        sql.Parameters.AddWithValue("@store", command.StoreCode);
    }

    private static async Task DeleteRowsAsync(MySqlConnection connection, MySqlTransaction transaction, ExistingDocument document, CancellationToken ct)
    {
        await using (var detail = new MySqlCommand("DELETE FROM caricorg WHERE ID=@id OR (Anno=@year AND Codice=@code);", connection, transaction))
        {
            detail.Parameters.AddWithValue("@id", document.Id); detail.Parameters.AddWithValue("@year", document.Year); detail.Parameters.AddWithValue("@code", document.Code);
            await detail.ExecuteNonQueryAsync(ct);
        }
        await using var movements = new MySqlCommand("DELETE FROM movimenti WHERE Anno=@year AND Settore=@sector AND Codice=@code;", connection, transaction);
        movements.Parameters.AddWithValue("@year", document.Year); movements.Parameters.AddWithValue("@sector", Sector); movements.Parameters.AddWithValue("@code", document.Code);
        await movements.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertRowsAsync(MySqlConnection connection, MySqlTransaction transaction, StockLoadSaveCommand command, int id, int code, IReadOnlyList<StockLoadSaveRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            const string detailStatement = "INSERT INTO caricorg (ID,Anno,Codice,Riga,Articolo,Um,Quantita,Prezzo,Sconto,PrNetto,Importo) VALUES (@id,@year,@code,@row,@article,@unit,@quantity,@price,@discount,@netPrice,@amount);";
            await using (var detail = new MySqlCommand(detailStatement, connection, transaction))
            {
                detail.Parameters.AddWithValue("@id", id); detail.Parameters.AddWithValue("@year", command.Year); detail.Parameters.AddWithValue("@code", code); detail.Parameters.AddWithValue("@row", row.RowNumber);
                detail.Parameters.AddWithValue("@article", row.ArticleCode); detail.Parameters.AddWithValue("@unit", row.UnitMeasure);
                detail.Parameters.AddWithValue("@quantity", row.Quantity); detail.Parameters.AddWithValue("@price", row.Price); detail.Parameters.AddWithValue("@discount", row.Discount); detail.Parameters.AddWithValue("@netPrice", row.Price * (1m - row.Discount / 100m)); detail.Parameters.AddWithValue("@amount", row.Amount);
                await detail.ExecuteNonQueryAsync(ct);
            }
            const string movementStatement = "INSERT INTO movimenti (Anno,Settore,Codice,Riga,Causale,DataMov,NumDoc,DataDoc,TipoMov,ULocale,Articolo,CliFor,Ditta,Quantita,Prezzo,Importo) VALUES (@year,@sector,@code,@row,@cause,@date,@number,@date,@movementType,@store,@article,@partyType,@party,@quantity,@netPrice,@amount);";
            await using var movement = new MySqlCommand(movementStatement, connection, transaction);
            movement.Parameters.AddWithValue("@year", command.Year); movement.Parameters.AddWithValue("@sector", Sector); movement.Parameters.AddWithValue("@code", code); movement.Parameters.AddWithValue("@row", row.RowNumber); movement.Parameters.AddWithValue("@cause", command.CauseCode); movement.Parameters.AddWithValue("@number", command.DocumentNumber.Trim());
            movement.Parameters.Add("@date", MySqlDbType.Date).Value = command.DocumentDate!.Value.ToDateTime(TimeOnly.MinValue);
            movement.Parameters.AddWithValue("@partyType", PartyType(command)); movement.Parameters.AddWithValue("@party", command.PartyCode); movement.Parameters.AddWithValue("@movementType", "C"); movement.Parameters.AddWithValue("@article", row.ArticleCode); movement.Parameters.AddWithValue("@quantity", row.Quantity); movement.Parameters.AddWithValue("@netPrice", row.Price * (1m - row.Discount / 100m)); movement.Parameters.AddWithValue("@amount", row.Amount); movement.Parameters.AddWithValue("@store", command.StoreCode);
            await movement.ExecuteNonQueryAsync(ct);
        }
    }

    private static string PartyType(StockLoadSaveCommand command) => command.CauseCode == 12 ? "C" : "F";
    private sealed record ExistingDocument(int Id, int Year, int Code);
}

public sealed class StockLoadSaveCommand
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Code { get; set; }
    public int CauseCode { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateOnly? DocumentDate { get; set; }
    public int PartyCode { get; set; }
    public int StoreCode { get; set; }
    public string ElectronicInvoiceName { get; set; } = "";
    public string ElectronicInvoicePath { get; set; } = "";
    public bool AllowMissingArticles { get; set; }
    public bool AllowDuplicate { get; set; }
    public IReadOnlyList<StockLoadSaveRow> Rows { get; set; } = [];
}

public sealed record StockLoadSaveRow(int RowNumber, string ArticleCode, string Description, string UnitMeasure, decimal Quantity, decimal Price, decimal Discount, decimal Amount, decimal VatRate, int ArticleState);
public sealed record StockLoadCheckResult(bool Success, string Message, IReadOnlyList<string> MissingArticles, bool IsDuplicate);
public sealed record StockLoadSaveResult(bool Success, string Message, int Id, int Code, IReadOnlyList<string> MissingArticles, bool RequiresMissingConfirmation, bool RequiresDuplicateConfirmation);
