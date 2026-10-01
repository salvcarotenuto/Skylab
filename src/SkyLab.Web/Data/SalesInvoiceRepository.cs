using System.Globalization;
using System.Text;
using MySqlConnector;

namespace SkyLab.Web.Data;

public sealed record SalesInvoiceLineSaveRequest(
    string ArticleCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Amount,
    decimal VatRate,
    string VatCode);

public sealed record SalesInvoiceSaveRequest(
    string DocumentNumber,
    DateOnly DocumentDate,
    int Cause,
    int CustomerCode,
    int StoreCode,
    int AgentCode,
    string Activity,
    int WorkSheetId,
    decimal WorkAmount,
    decimal WorkVatRate,
    decimal TaxableTotal,
    decimal VatTotal,
    decimal InvoiceTotal,
    IReadOnlyList<SalesInvoiceLineSaveRequest>? Lines);

public sealed record SalesInvoiceSaveResult(int Id, int Year, int Code);

public sealed record SalesInvoiceEditResult(
    int Id, int Year, int Code, string DocumentNumber, DateOnly DocumentDate, int Cause,
    int CustomerCode, string CustomerName, int StoreCode, int AgentCode, string Activity,
    int WorkSheetId, string WorkSheetReference, decimal WorkAmount, decimal WorkVatRate,
    IReadOnlyList<SalesInvoiceLineSaveRequest> Lines);

public sealed class SalesInvoiceRepository(SkyLabDatabaseOptions databaseOptions)
{
    private const int SalesSector = 30;

    public async Task<SalesInvoiceSaveResult> SaveAsync(
        SalesInvoiceSaveRequest invoice,
        int currentYear,
        CancellationToken cancellationToken = default)
    {
        Validate(invoice, currentYear);
        var lines = invoice.Lines ?? [];
        var taxableTotal = Round(invoice.WorkAmount + lines.Sum(row => row.Amount));
        var vatRows = BuildVatRows(invoice, lines);
        var vatTotal = Round(vatRows.Sum(row => row.Tax));
        var invoiceTotal = Round(taxableTotal + vatTotal);

        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var code = await NextCodeAsync(connection, transaction, currentYear, cancellationToken);
            var invoiceId = await InsertInvoiceAsync(
                connection, transaction, invoice, currentYear, code, taxableTotal, vatTotal, invoiceTotal, cancellationToken);

            var rowColumns = await TableColumnsAsync(connection, transaction, "fatturerg", cancellationToken);
            for (var index = 0; index < lines.Count; index++)
            {
                await InsertLineAsync(
                    connection, transaction, rowColumns, invoiceId, currentYear, code, index + 1, lines[index], cancellationToken);
            }

            var vatMovementId = await InsertVatMovementAsync(
                connection, transaction, invoice, currentYear, code, cancellationToken);
            await InsertVatRowsAsync(connection, transaction, vatMovementId, invoice.StoreCode, vatRows, cancellationToken);

            var accountingMovementId = await InsertAccountingMovementAsync(
                connection, transaction, invoice, invoiceId, currentYear, code, invoiceTotal, cancellationToken);
            await LinkVatMovementAsync(connection, transaction, vatMovementId, accountingMovementId, cancellationToken);

            if (invoice.WorkSheetId > 0)
            {
                await using var linkCommand = new MySqlCommand(
                    """
                    UPDATE Lavori
                    SET Fattura_ID = @invoiceId
                    WHERE ID = @workSheetId
                      AND COALESCE(Fattura_ID, 0) = 0;
                    """, connection, transaction);
                linkCommand.Parameters.AddWithValue("@invoiceId", invoiceId);
                linkCommand.Parameters.AddWithValue("@workSheetId", invoice.WorkSheetId);
                if (await linkCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("La scheda lavoro risulta già fatturata o non è più disponibile.");
            }

            await transaction.CommitAsync(cancellationToken);
            return new SalesInvoiceSaveResult(invoiceId, currentYear, code);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SalesInvoiceEditResult?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            """
            SELECT f.ID,f.Anno,f.Codice,COALESCE(f.NumDoc,''),f.DataDoc,COALESCE(f.Causale,30),
                   COALESCE(f.Cliente,0),COALESCE(c.Nome,''),COALESCE(f.ULocale,0),COALESCE(f.Agente,0),
                   COALESCE(f.Attivita,''),COALESCE(f.SchedaLavoro,0),
                   COALESCE(CONCAT('Numero ',LPAD(l.Codice,6,'0'),'/',l.Anno,'  del ',DATE_FORMAT(l.DataInterventoEffettiva,'%d/%m/%Y')),''),
                   COALESCE(f.Imponibile,0)
            FROM Fatture f
            LEFT JOIN Clienti c ON c.Codice=f.Cliente
            LEFT JOIN Lavori l ON l.ID=f.SchedaLavoro
            WHERE f.ID=@id LIMIT 1;
            """, connection);
        command.Parameters.AddWithValue("@id", id);
        int year, code, cause, customerCode, storeCode, agentCode, workSheetId;
        string documentNumber, customerName, activity, workSheetReference;
        DateOnly documentDate;
        decimal workAmount;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            year=reader.GetInt32(1);code=reader.GetInt32(2);documentNumber=reader.GetString(3);
            documentDate=DateOnly.FromDateTime(reader.GetDateTime(4));cause=reader.GetInt32(5);
            customerCode=reader.GetInt32(6);customerName=reader.GetString(7);storeCode=reader.GetInt32(8);
            agentCode=reader.GetInt32(9);activity=reader.GetString(10);workSheetId=reader.GetInt32(11);
            workSheetReference=reader.GetString(12);workAmount=reader.GetDecimal(13);
        }
        var columns=await TableColumnsAsync(connection,null,"fatturerg",cancellationToken);
        string Pick(string fallback,params string[] names)=>names.FirstOrDefault(columns.ContainsKey) is { } name?$"COALESCE(`{columns[name]}`,{fallback})":fallback;
        var lineSql=$"""
            SELECT {Pick("''","Articolo")},{Pick("''","Descrizione")},{Pick("''","UMisura","UnMisura","UnitaMisura","Um")},
                   {Pick("0","Quantita")},{Pick("0","Prezzo")},{Pick("0","Sconto")},{Pick("0","Importo")},{Pick("0","AliqIva")},{Pick("''","CodIva")}
            FROM FattureRg WHERE ID=@id ORDER BY Riga;
            """;
        var lines=new List<SalesInvoiceLineSaveRequest>();
        await using var lineCommand=new MySqlCommand(lineSql,connection);lineCommand.Parameters.AddWithValue("@id",id);
        await using var lineReader=await lineCommand.ExecuteReaderAsync(cancellationToken);
        decimal ReadLineDecimal(int ordinal) => Convert.ToDecimal(lineReader.GetValue(ordinal), CultureInfo.InvariantCulture);
        while(await lineReader.ReadAsync(cancellationToken)) lines.Add(new(lineReader.GetString(0),lineReader.GetString(1),lineReader.GetString(2),ReadLineDecimal(3),ReadLineDecimal(4),ReadLineDecimal(5),ReadLineDecimal(6),ReadLineDecimal(7),lineReader.GetString(8)));
        await lineReader.DisposeAsync();
        workAmount=Round(workAmount-lines.Sum(line=>line.Amount));
        var workVatRate=await WorkVatRateAsync(connection,id,workAmount,cancellationToken);
        return new(id,year,code,documentNumber,documentDate,cause,customerCode,customerName,storeCode,agentCode,activity,workSheetId,workSheetReference,workAmount,workVatRate,lines);
    }

    public async Task UpdateAsync(int id, SalesInvoiceSaveRequest invoice, int currentYear, CancellationToken cancellationToken = default)
    {
        Validate(invoice,currentYear);var lines=invoice.Lines??[];var taxable=Round(invoice.WorkAmount+lines.Sum(x=>x.Amount));var vatRows=BuildVatRows(invoice,lines);var vat=Round(vatRows.Sum(x=>x.Tax));var total=Round(taxable+vat);
        await using var connection=new MySqlConnection(databaseOptions.BuildCompanyConnectionString());await connection.OpenAsync(cancellationToken);await using var transaction=await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            int code;await using(var find=new MySqlCommand("SELECT Codice FROM Fatture WHERE ID=@id AND Anno=@year LIMIT 1",connection,transaction)){find.Parameters.AddWithValue("@id",id);find.Parameters.AddWithValue("@year",currentYear);var value=await find.ExecuteScalarAsync(cancellationToken);if(value is null)throw new InvalidOperationException("Fattura non trovata.");code=Convert.ToInt32(value);}
            await using(var update=new MySqlCommand("""UPDATE Fatture SET Causale=@cause,NumDoc=@number,DataDoc=@date,Cliente=@customer,ULocale=@store,Agente=@agent,Attivita=@activity,SchedaLavoro=@work,Imponibile=@taxable,Iva=@vat,Totale=@total WHERE ID=@id;""",connection,transaction)){update.Parameters.AddWithValue("@cause",invoice.Cause);update.Parameters.AddWithValue("@number",invoice.DocumentNumber.Trim());update.Parameters.AddWithValue("@date",invoice.DocumentDate.ToDateTime(TimeOnly.MinValue));update.Parameters.AddWithValue("@customer",invoice.CustomerCode);update.Parameters.AddWithValue("@store",invoice.StoreCode);update.Parameters.AddWithValue("@agent",invoice.AgentCode);update.Parameters.AddWithValue("@activity",invoice.Activity.Trim());update.Parameters.AddWithValue("@work",invoice.WorkSheetId);update.Parameters.AddWithValue("@taxable",taxable);update.Parameters.AddWithValue("@vat",vat);update.Parameters.AddWithValue("@total",total);update.Parameters.AddWithValue("@id",id);await update.ExecuteNonQueryAsync(cancellationToken);}
            await using(var clear=new MySqlCommand("UPDATE Lavori SET Fattura_ID=NULL WHERE Fattura_ID=@id; DELETE FROM FattureRg WHERE ID=@id;",connection,transaction)){clear.Parameters.AddWithValue("@id",id);await clear.ExecuteNonQueryAsync(cancellationToken);}
            var columns=await TableColumnsAsync(connection,transaction,"fatturerg",cancellationToken);for(var index=0;index<lines.Count;index++)await InsertLineAsync(connection,transaction,columns,id,currentYear,code,index+1,lines[index],cancellationToken);
            await DeleteMovementsAsync(connection,transaction,id,currentYear,code,cancellationToken);
            var vatId=await InsertVatMovementAsync(connection,transaction,invoice,currentYear,code,cancellationToken);await InsertVatRowsAsync(connection,transaction,vatId,invoice.StoreCode,vatRows,cancellationToken);var accountingId=await InsertAccountingMovementAsync(connection,transaction,invoice,id,currentYear,code,total,cancellationToken);await LinkVatMovementAsync(connection,transaction,vatId,accountingId,cancellationToken);
            if(invoice.WorkSheetId>0){await using var link=new MySqlCommand("UPDATE Lavori SET Fattura_ID=@id WHERE ID=@work AND COALESCE(Fattura_ID,0)=0",connection,transaction);link.Parameters.AddWithValue("@id",id);link.Parameters.AddWithValue("@work",invoice.WorkSheetId);if(await link.ExecuteNonQueryAsync(cancellationToken)!=1)throw new InvalidOperationException("La scheda lavoro risulta già fatturata o non è più disponibile.");}
            await transaction.CommitAsync(cancellationToken);
        }
        catch{await transaction.RollbackAsync(cancellationToken);throw;}
    }

    public async Task DeleteAsync(int id,CancellationToken cancellationToken=default)
    {
        await using var connection=new MySqlConnection(databaseOptions.BuildCompanyConnectionString());await connection.OpenAsync(cancellationToken);await using var transaction=await connection.BeginTransactionAsync(cancellationToken);
        try{int year,code;await using(var find=new MySqlCommand("SELECT Anno,Codice FROM Fatture WHERE ID=@id LIMIT 1",connection,transaction)){find.Parameters.AddWithValue("@id",id);await using var reader=await find.ExecuteReaderAsync(cancellationToken);if(!await reader.ReadAsync(cancellationToken))throw new InvalidOperationException("Fattura non trovata.");year=reader.GetInt32(0);code=reader.GetInt32(1);}await using(var unlink=new MySqlCommand("UPDATE Lavori SET Fattura_ID=NULL WHERE Fattura_ID=@id",connection,transaction)){unlink.Parameters.AddWithValue("@id",id);await unlink.ExecuteNonQueryAsync(cancellationToken);}await DeleteMovementsAsync(connection,transaction,id,year,code,cancellationToken);await using(var delete=new MySqlCommand("DELETE FROM Fatture WHERE ID=@id",connection,transaction)){delete.Parameters.AddWithValue("@id",id);await delete.ExecuteNonQueryAsync(cancellationToken);}await transaction.CommitAsync(cancellationToken);}catch{await transaction.RollbackAsync(cancellationToken);throw;}
    }

    private static async Task<decimal> WorkVatRateAsync(MySqlConnection connection,int invoiceId,decimal workAmount,CancellationToken ct)
    {if(workAmount==0)return 0;await using var command=new MySqlCommand("""SELECT COALESCE(r.AliqIva,0) FROM MovIvaRg r JOIN MovIva v ON v.ID=r.ID WHERE v.Settore=30 AND v.Codice=(SELECT Codice FROM Fatture WHERE ID=@id) AND v.Anno=(SELECT Anno FROM Fatture WHERE ID=@id) ORDER BY ABS(r.Imponibile-@workAmount),r.Imponibile DESC LIMIT 1;""",connection);command.Parameters.AddWithValue("@id",invoiceId);command.Parameters.AddWithValue("@workAmount",workAmount);var value=await command.ExecuteScalarAsync(ct);return value is null or DBNull?0:Convert.ToDecimal(value);}

    private static async Task DeleteMovementsAsync(MySqlConnection connection,MySqlTransaction transaction,int invoiceId,int year,int code,CancellationToken ct)
    {var commands=new[]{"DELETE rg FROM MovIvaRg rg JOIN MovIva v ON v.ID=rg.ID WHERE v.Anno=@year AND v.Settore=30 AND v.Codice=@code","DELETE FROM MovIva WHERE Anno=@year AND Settore=30 AND Codice=@code","DELETE rg FROM MovContRg rg JOIN MovCont m ON m.ID=rg.ID WHERE m.Settore=30 AND m.Documento=@id","DELETE dc FROM MovContDc dc JOIN MovCont m ON m.ID=dc.Mov_Id WHERE m.Settore=30 AND m.Documento=@id","DELETE FROM MovCont WHERE Settore=30 AND Documento=@id"};foreach(var sql in commands){await using var command=new MySqlCommand(sql,connection,transaction);command.Parameters.AddWithValue("@year",year);command.Parameters.AddWithValue("@code",code);command.Parameters.AddWithValue("@id",invoiceId);await command.ExecuteNonQueryAsync(ct);}}

    private static void Validate(SalesInvoiceSaveRequest invoice, int currentYear)
    {
        if (string.IsNullOrWhiteSpace(invoice.DocumentNumber))
            throw new InvalidOperationException("Campo Numero documento obbligatorio.");
        if (invoice.DocumentDate == default)
            throw new InvalidOperationException("Campo Data documento obbligatorio.");
        if (invoice.DocumentDate.Year != currentYear)
            throw new InvalidOperationException($"La data documento deve appartenere all'esercizio contabile in linea ({currentYear}).");
        if (invoice.CustomerCode <= 0)
            throw new InvalidOperationException("Campo Cliente obbligatorio.");
        if (invoice.InvoiceTotal <= 0)
            throw new InvalidOperationException("Il Totale fattura deve essere maggiore di zero.");
        if (invoice.Cause is < 30 or > 32)
            throw new InvalidOperationException("Tipo documento non valido.");
        if (invoice.WorkAmount < 0 || invoice.WorkVatRate is < 0 or > 100)
            throw new InvalidOperationException("Valori economici del lavoro non validi.");
        if ((invoice.Lines ?? []).Any(row => string.IsNullOrWhiteSpace(row.ArticleCode)
            || row.Quantity <= 0 || row.UnitPrice < 0 || row.Discount is < 0 or > 100 || row.VatRate is < 0 or > 100))
            throw new InvalidOperationException("Una o più righe articolo contengono valori non validi.");
    }

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<VatRow> BuildVatRows(
        SalesInvoiceSaveRequest invoice,
        IReadOnlyList<SalesInvoiceLineSaveRequest> lines)
    {
        var values = new List<(decimal Rate, decimal Taxable)>();
        if (invoice.WorkAmount != 0) values.Add((invoice.WorkVatRate, Round(invoice.WorkAmount)));
        values.AddRange(lines.Where(row => row.Amount != 0).Select(row => (row.VatRate, Round(row.Amount))));
        return values
            .GroupBy(row => row.Rate)
            .Select(group => new VatRow(group.Key, Round(group.Sum(row => row.Taxable)),
                Round(group.Sum(row => row.Taxable) * group.Key / 100m)))
            .ToArray();
    }

    private static async Task<int> NextCodeAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        int year,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            "SELECT COALESCE(MAX(Codice), 0) + 1 FROM Fatture WHERE Anno = @year FOR UPDATE;",
            connection, transaction);
        command.Parameters.AddWithValue("@year", year);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> InsertInvoiceAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SalesInvoiceSaveRequest invoice,
        int year,
        int code,
        decimal taxableTotal,
        decimal vatTotal,
        decimal invoiceTotal,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO Fatture
                (Anno, Codice, FeName, Causale, Sezione, Stato, NumDoc, DataDoc,
                 Cliente, ULocale, Agente, Attivita, SchedaLavoro, PrintScheda,
                 Pagamento, Imponibile, Iva, Totale)
            VALUES
                (@year, @code, '', @cause, '', 0, @documentNumber, @documentDate,
                 @customerCode, @storeCode, @agentCode, @activity, @workSheetId, 0,
                 0, @taxable, @vat, @total);
            """, connection, transaction);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@code", code);
        command.Parameters.AddWithValue("@cause", invoice.Cause);
        command.Parameters.AddWithValue("@documentNumber", invoice.DocumentNumber.Trim());
        command.Parameters.AddWithValue("@documentDate", invoice.DocumentDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@customerCode", invoice.CustomerCode);
        command.Parameters.AddWithValue("@storeCode", invoice.StoreCode);
        command.Parameters.AddWithValue("@agentCode", invoice.AgentCode);
        command.Parameters.AddWithValue("@activity", (invoice.Activity ?? "").Trim());
        command.Parameters.AddWithValue("@workSheetId", invoice.WorkSheetId);
        command.Parameters.AddWithValue("@taxable", taxableTotal);
        command.Parameters.AddWithValue("@vat", vatTotal);
        command.Parameters.AddWithValue("@total", invoiceTotal);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<Dictionary<string, string>> TableColumnsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            SELECT COLUMN_NAME
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@table);
            """, connection, transaction);
        command.Parameters.AddWithValue("@table", table);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result[reader.GetString(0)] = reader.GetString(0);
        return result;
    }

    private static async Task InsertLineAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        IReadOnlyDictionary<string, string> available,
        int invoiceId,
        int year,
        int code,
        int rowNumber,
        SalesInvoiceLineSaveRequest row,
        CancellationToken cancellationToken)
    {
        var values = new List<(string Column, object Value)>();
        Add(values, available, "ID", invoiceId);
        Add(values, available, "Anno", year);
        Add(values, available, "Codice", code);
        Add(values, available, "Riga", rowNumber);
        Add(values, available, "Articolo", row.ArticleCode.Trim());
        Add(values, available, "Descrizione", (row.Description ?? "").Trim());
        AddFirst(values, available, row.Unit ?? "", "UMisura", "UnMisura", "UnitaMisura", "Um");
        Add(values, available, "Quantita", row.Quantity);
        Add(values, available, "Prezzo", row.UnitPrice);
        Add(values, available, "Sconto", row.Discount);
        Add(values, available, "Importo", Round(row.Amount));
        Add(values, available, "AliqIva", row.VatRate);
        Add(values, available, "Iva", Round(row.Amount * row.VatRate / 100m));

        if (available.ContainsKey("CodIva"))
        {
            var vatCode = string.IsNullOrWhiteSpace(row.VatCode)
                ? await ArticleVatCodeAsync(connection, transaction, row.ArticleCode, cancellationToken)
                : row.VatCode.Trim();
            Add(values, available, "CodIva", string.IsNullOrWhiteSpace(vatCode) ? DBNull.Value : vatCode);
        }

        if (!values.Any(value => value.Column.Equals("ID", StringComparison.OrdinalIgnoreCase))
            || !values.Any(value => value.Column.Equals("Riga", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("La struttura della tabella FattureRg non è compatibile con il salvataggio.");

        var columns = string.Join(", ", values.Select(value => $"`{value.Column}`"));
        var parameters = string.Join(", ", values.Select((_, index) => $"@p{index}"));
        await using var command = new MySqlCommand(
            $"INSERT INTO FattureRg ({columns}) VALUES ({parameters});", connection, transaction);
        for (var index = 0; index < values.Count; index++) command.Parameters.AddWithValue($"@p{index}", values[index].Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Add(
        ICollection<(string Column, object Value)> values,
        IReadOnlyDictionary<string, string> available,
        string requestedColumn,
        object value)
    {
        if (available.TryGetValue(requestedColumn, out var actualColumn)) values.Add((actualColumn, value));
    }

    private static void AddFirst(
        ICollection<(string Column, object Value)> values,
        IReadOnlyDictionary<string, string> available,
        object value,
        params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!available.TryGetValue(candidate, out var actualColumn)) continue;
            values.Add((actualColumn, value));
            return;
        }
    }

    private static async Task<string> ArticleVatCodeAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string articleCode,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            "SELECT COALESCE(CodIva, '') FROM Articoli WHERE Codice = @code LIMIT 1;",
            connection, transaction);
        command.Parameters.AddWithValue("@code", articleCode.Trim());
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? "";
    }

    private static async Task<int> InsertVatMovementAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SalesInvoiceSaveRequest invoice,
        int year,
        int code,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO MovIva
                (Anno, Settore, Codice, Causale, NumDoc, DataDoc, Ditta, CtPartita,
                 ULocale, Pagamento, Banca, FeName, Note)
            VALUES
                (@year, @sector, @code, @cause, @documentNumber, @documentDate, @customerCode, 0,
                 @storeCode, 0, 0, '', @notes);
            """, connection, transaction);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@sector", SalesSector);
        command.Parameters.AddWithValue("@code", code);
        command.Parameters.AddWithValue("@cause", invoice.Cause);
        command.Parameters.AddWithValue("@documentNumber", invoice.DocumentNumber.Trim());
        command.Parameters.AddWithValue("@documentDate", invoice.DocumentDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@customerCode", invoice.CustomerCode);
        command.Parameters.AddWithValue("@storeCode", invoice.StoreCode);
        command.Parameters.AddWithValue("@notes", (invoice.Activity ?? "").Trim());
        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task InsertVatRowsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int movementId,
        int storeCode,
        IReadOnlyList<VatRow> rows,
        CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            await using var command = new MySqlCommand(
                """
                INSERT INTO MovIvaRg (ID, ULocale, AliqIva, Imponibile, Iva)
                VALUES (@id, @storeCode, @rate, @taxable, @tax);
                """, connection, transaction);
            command.Parameters.AddWithValue("@id", movementId);
            command.Parameters.AddWithValue("@storeCode", storeCode);
            command.Parameters.AddWithValue("@rate", row.Rate);
            command.Parameters.AddWithValue("@taxable", row.Taxable);
            command.Parameters.AddWithValue("@tax", row.Tax);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<int> InsertAccountingMovementAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SalesInvoiceSaveRequest invoice,
        int invoiceId,
        int year,
        int code,
        decimal invoiceTotal,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO MovCont
                (Anno, Settore, Codice, Causale, DataMov, CliFor, Ditta, NumDoc,
                 Documento, Importo, ULocale, Descrizione)
            VALUES
                (@year, @sector, @code, @cause, @movementDate, 'C', @customerCode, @documentNumber,
                 @document, @amount, @storeCode, @description);
            """, connection, transaction);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@sector", SalesSector);
        command.Parameters.AddWithValue("@code", code);
        command.Parameters.AddWithValue("@cause", invoice.Cause);
        command.Parameters.AddWithValue("@movementDate", invoice.DocumentDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@customerCode", invoice.CustomerCode);
        command.Parameters.AddWithValue("@documentNumber", invoice.DocumentNumber.Trim());
        command.Parameters.AddWithValue("@document", invoiceId);
        command.Parameters.AddWithValue("@amount", invoiceTotal);
        command.Parameters.AddWithValue("@storeCode", invoice.StoreCode);
        command.Parameters.AddWithValue("@description", $"Fattura vendita {invoice.DocumentNumber.Trim()}");
        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task LinkVatMovementAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int vatMovementId,
        int accountingMovementId,
        CancellationToken cancellationToken)
    {
        var columns = await TableColumnsAsync(connection, transaction, "moviva", cancellationToken);
        if (!columns.TryGetValue("MovCont_Id", out var column)) return;
        await using var command = new MySqlCommand(
            $"UPDATE MovIva SET `{column}` = @accountingId WHERE ID = @vatId;", connection, transaction);
        command.Parameters.AddWithValue("@accountingId", accountingMovementId);
        command.Parameters.AddWithValue("@vatId", vatMovementId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record VatRow(decimal Rate, decimal Taxable, decimal Tax);
}
