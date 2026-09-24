using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkyLab.Web.Data;

namespace SkyLab.Web.Pages.Magazzino.CaricoAcquisti;

public sealed class IndexModel(SkyLabDatabaseOptions databaseOptions, SkyLab.Web.Services.ApplicationState applicationState) : PageModel
{
    public int CurrentYear => applicationState.Esercizio;
    public IReadOnlyList<PurchaseLoadPreview> Documents { get; private set; } = [];
    public IReadOnlyList<PurchaseLoadDetailPreview> Details { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        Details = await LoadDetailsAsync(connection, cancellationToken);
        var articleSearch = Details
            .GroupBy(line => line.DocumentId)
            .ToDictionary(group => group.Key, group => string.Join(' ', group.Select(line => $"{line.Article} {line.Description}")));
        Documents = await LoadDocumentsAsync(connection, articleSearch, cancellationToken);
    }

    private static async Task<IReadOnlyList<PurchaseLoadPreview>> LoadDocumentsAsync(MySqlConnection connection, IReadOnlyDictionary<int, string> articleSearch, CancellationToken cancellationToken)
    {
        const string statement = """
            SELECT c.ID, c.Anno, c.Codice, COALESCE(c.NumDoc, ''), c.DataDoc, c.Ditta,
                   COALESCE(NULLIF(CASE c.CliFor WHEN 'C' THEN cli.Nome WHEN 'F' THEN forn.Nome ELSE '' END, ''),
                            CONCAT(COALESCE(c.CliFor, ''), ' ', LPAD(c.Ditta, 5, '0'))) AS DittaNome,
                   COALESCE(SUM(r.Importo), 0) AS Merce,
                   COALESCE(SUM(ROUND(r.Importo * COALESCE(iva.Aliquota, 0) / 100, 2)), 0) AS Iva,
                   COALESCE(c.Totale, 0) AS Totale
            FROM carico c
            LEFT JOIN caricorg r ON r.ID = c.ID
            LEFT JOIN articoli a ON a.Codice = r.Articolo
            LEFT JOIN codiciiva iva ON iva.Codice = a.Codiva
            LEFT JOIN clienti cli ON c.CliFor = 'C' AND cli.Codice = c.Ditta
            LEFT JOIN fornitori forn ON c.CliFor = 'F' AND forn.Codice = c.Ditta
            GROUP BY c.ID, c.Anno, c.Codice, c.NumDoc, c.DataDoc, c.CliFor, c.Ditta, cli.Nome, forn.Nome, c.Totale
            ORDER BY c.Anno DESC, c.Codice DESC, c.ID DESC;
            """;
        var result = new List<PurchaseLoadPreview>();
        await using var command = new MySqlCommand(statement, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            result.Add(new(id, reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : DateOnly.FromDateTime(reader.GetDateTime(4)),
                reader.GetInt32(5), reader.GetString(6), reader.GetDecimal(7), reader.GetDecimal(8), reader.GetDecimal(9), "",
                articleSearch.GetValueOrDefault(id, "")));
        }
        return result;
    }

    private static async Task<IReadOnlyList<PurchaseLoadDetailPreview>> LoadDetailsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string statement = """
            SELECT r.ID, r.Articolo, COALESCE(a.Descrizione, ''), COALESCE(r.Um, ''),
                   r.Quantita, r.Prezzo, r.Sconto, r.Importo, COALESCE(iva.Aliquota, 0)
            FROM caricorg r
            LEFT JOIN articoli a ON a.Codice = r.Articolo
            LEFT JOIN codiciiva iva ON iva.Codice = a.Codiva
            ORDER BY r.ID, r.Riga;
            """;
        var result = new List<PurchaseLoadDetailPreview>();
        await using var command = new MySqlCommand(statement, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetDecimal(4), reader.GetDecimal(5), reader.GetDecimal(6), reader.GetDecimal(7), reader.GetDecimal(8)));
        }
        return result;
    }

    public sealed record PurchaseLoadPreview(int Id, int Year, int Batch, string Number, DateOnly? Date, int SupplierCode, string SupplierName, decimal Goods, decimal Vat, decimal Total, string ElectronicInvoice, string ArticleSearch);
    public sealed record PurchaseLoadDetailPreview(int DocumentId, string Article, string Description, string Unit, decimal Quantity, decimal Price, decimal Discount, decimal Amount, decimal VatRate);
}
