using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Magazzino.CaricoAcquisti;

public sealed class EditModel(SkyLabServicePaths servicePaths, SkyLabDatabaseOptions databaseOptions, CustomerService customerService, ApplicationState applicationState) : PageModel
{
    private const int StockLoadSector = 10;
    public int Id { get; private set; }
    public int Azione { get; private set; } = 2;
    public int Partita { get; private set; } = 124;
    public int Anno { get; private set; } = 2026;
    public int CauseCode { get; private set; } = 10;
    public string DocumentNumber { get; private set; } = "";
    public DateOnly? DocumentDate { get; private set; }
    public int SupplierCode { get; private set; }
    public string SupplierName { get; private set; } = "";
    public int StoreCode { get; private set; }
    public string ElectronicInvoiceName { get; private set; } = "";
    public string ElectronicInvoicePath { get; private set; } = "";
    public decimal Total { get; private set; }
    public IReadOnlyList<PurchaseLoadLine> Lines { get; private set; } = [];
    public string ElectronicInvoiceFolderDefault { get; private set; } = "";
    public IReadOnlyList<CodeLookupItem> UnitMeasures { get; private set; } = [];

    [BindProperty]
    public string StockLoadPayload { get; set; } = "";

    public async Task OnGetAsync(
        int azione = 2,
        int? id = null,
        string? returnTo = null,
        string? importXml = null,
        string? documentNumber = null,
        DateOnly? documentDate = null,
        int? supplierCode = null,
        string? supplierName = null,
        int? storeCode = null,
        CancellationToken cancellationToken = default)
    {
        Azione = azione;
        servicePaths.EnsureCreated();
        ElectronicInvoiceFolderDefault = servicePaths.FEAcquistiTransito;
        UnitMeasures = await customerService.UnitMeasuresAsync(cancellationToken);

        if (azione is not (2 or 102))
        {
            await LoadExistingDocumentAsync(id ?? 0, cancellationToken);
            return;
        }

        Anno = applicationState.Esercizio;
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            "SELECT GREATEST(COALESCE(MAX(Codice), 0), @initialLastCode) + 1 FROM carico WHERE Anno = @year;",
            connection);
        command.Parameters.AddWithValue("@year", Anno);
        command.Parameters.AddWithValue("@initialLastCode", Anno == 2026 ? 124 : 0);
        Partita = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

        if (string.Equals(returnTo, "purchaseInvoice", StringComparison.OrdinalIgnoreCase))
        {
            DocumentNumber = documentNumber?.Trim() ?? "";
            DocumentDate = documentDate;
            SupplierCode = supplierCode.GetValueOrDefault();
            SupplierName = supplierName?.Trim() ?? "";
            StoreCode = storeCode.GetValueOrDefault();

            var fileName = Path.GetFileName(importXml ?? "");
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(fileName)
                && (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".p7m", StringComparison.OrdinalIgnoreCase)))
            {
                ElectronicInvoiceName = fileName;
                ElectronicInvoicePath = Path.Combine(servicePaths.FEAcquistiTransito, fileName);
            }
        }
    }

    public async Task<IActionResult> OnPostValidateAsync(CancellationToken cancellationToken)
    {
        var command = ParseCommand();
        if (command is null) return new JsonResult(new { success = false, message = "Dati di registrazione mancanti o non leggibili." });
        var result = await new StockLoadSaveService(databaseOptions, applicationState).CheckAsync(command, cancellationToken);
        return new JsonResult(new
        {
            success = result.Success,
            message = result.Message,
            requiresMissingConfirmation = result.MissingArticles.Count > 0,
            requiresDuplicateConfirmation = result.IsDuplicate,
            missingArticles = result.MissingArticles
        });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var command = ParseCommand();
        if (command is null) return new JsonResult(new { success = false, message = "Dati di registrazione mancanti o non leggibili." });
        try
        {
            var result = await new StockLoadSaveService(databaseOptions, applicationState).SaveAsync(command, cancellationToken);
            var archiveWarning = result.Success
                ? CopyElectronicInvoiceToArchive(command.ElectronicInvoicePath)
                : "";
            return new JsonResult(new
            {
                success = result.Success,
                message = result.Message,
                id = result.Id,
                year = applicationState.Esercizio,
                code = result.Code,
                requiresMissingConfirmation = result.RequiresMissingConfirmation,
                requiresDuplicateConfirmation = result.RequiresDuplicateConfirmation,
                missingArticles = result.MissingArticles,
                electronicInvoiceArchiveWarning = archiveWarning,
                listUrl = Url.Page("/Magazzino/CaricoAcquisti/Index")
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Registrazione del carico non riuscita. Motivo: {ex.Message}" });
        }
    }

    private string CopyElectronicInvoiceToArchive(string? requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath)) return "";
        try
        {
            var sourcePath = Path.GetFullPath(requestedPath);
            if (!System.IO.File.Exists(sourcePath))
                return "Carico registrato, ma il file XML non e' stato trovato nella cartella Transito.";

            var fileName = Path.GetFileName(sourcePath);
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(fileName)
                || (!extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".p7m", StringComparison.OrdinalIgnoreCase)))
                return "";

            Directory.CreateDirectory(servicePaths.FEAcquistiArchivio);
            var archiveDirectory = Path.GetFullPath(servicePaths.FEAcquistiArchivio);
            var archivePath = Path.GetFullPath(Path.Combine(archiveDirectory, fileName));
            var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? "";
            if (string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceDirectory)),
                Path.TrimEndingDirectorySeparator(archiveDirectory),
                StringComparison.OrdinalIgnoreCase)) return "";

            System.IO.File.Copy(sourcePath, archivePath, overwrite: true);
            return "";
        }
        catch (UnauthorizedAccessException)
        {
            return "Carico registrato, ma i permessi non consentono di copiare il file XML nell'Archivio.";
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException)
        {
            return "Carico registrato, ma non e' stato possibile copiare il file XML nell'Archivio.";
        }
    }

    private StockLoadSaveCommand? ParseCommand()
    {
        try
        {
            return JsonSerializer.Deserialize<StockLoadSaveCommand>(StockLoadPayload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task LoadExistingDocumentAsync(int id, CancellationToken ct)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(ct);
        const string headerStatement = """
            SELECT c.ID,c.Anno,c.Codice,c.Causale,COALESCE(c.NumDoc,''),c.DataDoc,c.Ditta,
                   COALESCE(c.ULocale,0),COALESCE(c.Totale,0),
                   COALESCE(CASE c.CliFor WHEN 'C' THEN cli.Nome WHEN 'F' THEN forn.Nome ELSE '' END,'')
            FROM carico c
            LEFT JOIN clienti cli ON c.CliFor='C' AND cli.Codice=c.Ditta
            LEFT JOIN fornitori forn ON c.CliFor='F' AND forn.Codice=c.Ditta
            WHERE c.ID=@id
            LIMIT 1;
            """;
        await using (var header = new MySqlCommand(headerStatement, connection))
        {
            header.Parameters.AddWithValue("@id", id);
            await using var reader = await header.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return;
            Id = reader.GetInt32(0);
            Anno = reader.GetInt32(1);
            Partita = reader.GetInt32(2);
            CauseCode = reader.GetInt32(3);
            DocumentNumber = reader.GetString(4);
            DocumentDate = reader.IsDBNull(5) ? null : DateOnly.FromDateTime(reader.GetDateTime(5));
            SupplierCode = reader.GetInt32(6);
            StoreCode = reader.GetInt32(7);
            Total = reader.GetDecimal(8);
            SupplierName = reader.GetString(9);
        }

        const string rowsStatement = """
            SELECT r.Articolo,COALESCE(a.Descrizione,''),COALESCE(r.Um,''),r.Quantita,r.Prezzo,
                   r.Sconto,r.Importo,COALESCE(iva.Aliquota,0),CASE WHEN a.Codice IS NULL THEN 0 ELSE 1 END
            FROM caricorg r
            LEFT JOIN articoli a ON a.Codice=r.Articolo
            LEFT JOIN codiciiva iva ON iva.Codice=a.Codiva
            WHERE r.ID=@id
            ORDER BY r.Riga;
            """;
        var lines = new List<PurchaseLoadLine>();
        await using (var rows = new MySqlCommand(rowsStatement, connection))
        {
            rows.Parameters.AddWithValue("@id", id);
            await using var reader = await rows.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                lines.Add(new(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetDecimal(3),reader.GetDecimal(4),reader.GetDecimal(5),reader.GetDecimal(6),reader.GetDecimal(7),reader.GetInt32(8)==1));
        }
        Lines = lines;
    }

    public sealed record PurchaseLoadLine(string Article, string Description, string Unit, decimal Quantity, decimal Price, decimal Discount, decimal Amount, decimal VatRate, bool ArticleExists);
}
