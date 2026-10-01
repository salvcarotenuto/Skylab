using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.FattureVendita;

public sealed class IndexModel(
    ApplicationState applicationState,
    CustomerService customerService,
    SkyLabDatabaseOptions databaseOptions) : PageModel
{
    public int SelectedYear { get; private set; }
    public int? SelectedCustomerCode { get; private set; }
    public string SelectedCustomerName { get; private set; } = "";
    public int? SelectedStoreCode { get; private set; }
    public int? SelectedMonth { get; private set; }
    public int? SelectedCauseCode { get; private set; }
    public IReadOnlyList<int> Years { get; private set; } = [];
    public IReadOnlyList<PartyLookupItem> Customers { get; private set; } = [];
    public IReadOnlyList<PurchaseInvoiceStoreOption> Stores { get; private set; } = [];
    public IReadOnlyList<SalesInvoiceListRow> Invoices { get; private set; } = [];

    public async Task OnGetAsync(
        int? year = null,
        int? customerCode = null,
        int? storeCode = null,
        int? month = null,
        int? causeCode = null,
        CancellationToken cancellationToken = default)
    {
        SelectedYear = year ?? applicationState.Esercizio;
        Years = Enumerable.Range(applicationState.Esercizio - 5, 6)
            .OrderByDescending(value => value)
            .ToArray();

        var customers = await customerService.SearchAsync(null, false, cancellationToken);
        Customers = customers
            .Select(item => new PartyLookupItem(item.Code, item.Name, item.City, item.Province))
            .ToArray();

        var selectedCustomer = customerCode.HasValue
            ? Customers.FirstOrDefault(item => item.Code == customerCode.Value)
            : null;
        SelectedCustomerCode = selectedCustomer?.Code;
        SelectedCustomerName = selectedCustomer?.Name ?? "";

        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            "SELECT Codice, COALESCE(NomeBreve, '') FROM unitalocali ORDER BY Codice;",
            connection);
        var stores = new List<PurchaseInvoiceStoreOption>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var code = reader.GetInt32(0);
            var description = reader.GetString(1);
            stores.Add(new PurchaseInvoiceStoreOption(code, $"{code:000} - {description}"));
        }
        Stores = stores;
        SelectedStoreCode = Stores.Any(item => item.Code == storeCode) ? storeCode : null;
        SelectedMonth = month is >= 1 and <= 12 ? month : null;
        SelectedCauseCode = causeCode is >= 30 and <= 32 ? causeCode : null;

        await reader.DisposeAsync();
        await using var invoicesCommand = new MySqlCommand(
            """
            SELECT f.ID,
                   f.Anno,
                   f.Codice,
                   COALESCE(f.NumDoc, ''),
                   f.DataDoc,
                   COALESCE(f.Causale, 0),
                   COALESCE(f.Cliente, 0),
                   COALESCE(c.Nome, ''),
                   COALESCE(f.Imponibile, 0),
                   COALESCE(f.Iva, 0),
                   COALESCE(f.Totale, 0),
                   COALESCE(f.Stato, 0),
                   COALESCE(f.FeName, '')
            FROM Fatture f
            LEFT JOIN Clienti c ON c.Codice = f.Cliente
            WHERE f.Anno = @year
              AND (@month = 0 OR MONTH(f.DataDoc) = @month)
              AND (@cause = 0 OR f.Causale = @cause)
              AND (@customer = 0 OR f.Cliente = @customer)
              AND (@store = 0 OR f.ULocale = @store)
            ORDER BY f.Anno DESC, f.Codice DESC, f.ID DESC;
            """, connection);
        invoicesCommand.Parameters.AddWithValue("@year", SelectedYear);
        invoicesCommand.Parameters.AddWithValue("@month", SelectedMonth ?? 0);
        invoicesCommand.Parameters.AddWithValue("@cause", SelectedCauseCode ?? 0);
        invoicesCommand.Parameters.AddWithValue("@customer", SelectedCustomerCode ?? 0);
        invoicesCommand.Parameters.AddWithValue("@store", SelectedStoreCode ?? 0);
        var invoices = new List<SalesInvoiceListRow>();
        await using var invoicesReader = await invoicesCommand.ExecuteReaderAsync(cancellationToken);
        while (await invoicesReader.ReadAsync(cancellationToken))
        {
            invoices.Add(new SalesInvoiceListRow(
                invoicesReader.GetInt32(0),
                invoicesReader.GetInt32(1),
                invoicesReader.GetInt32(2),
                invoicesReader.GetString(3),
                invoicesReader.IsDBNull(4) ? null : DateOnly.FromDateTime(invoicesReader.GetDateTime(4)),
                invoicesReader.GetInt32(5),
                invoicesReader.GetInt32(6),
                invoicesReader.GetString(7),
                invoicesReader.GetDecimal(8),
                invoicesReader.GetDecimal(9),
                invoicesReader.GetDecimal(10),
                invoicesReader.GetInt32(11),
                invoicesReader.GetString(12)));
        }
        Invoices = invoices;
    }
}

public sealed record SalesInvoiceListRow(
    int Id,
    int Year,
    int Code,
    string DocumentNumber,
    DateOnly? DocumentDate,
    int Cause,
    int CustomerCode,
    string CustomerName,
    decimal Taxable,
    decimal Vat,
    decimal Total,
    int State,
    string ElectronicInvoiceName)
{
    public string ElectronicInvoiceStatus => State switch
    {
        0 => "Emessa",
        1 => "FE elaborata",
        2 => "FE inviata",
        3 => "FE rifiutata",
        _ => "Stato non gestito"
    };

    public string CauseDescription => Cause switch
    {
        31 => "Nota di addebito",
        32 => "Nota di accredito",
        _ => "Fattura di vendita"
    };
}
