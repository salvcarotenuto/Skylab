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
    public IReadOnlyList<int> Years { get; private set; } = [];
    public IReadOnlyList<PartyLookupItem> Customers { get; private set; } = [];
    public IReadOnlyList<PurchaseInvoiceStoreOption> Stores { get; private set; } = [];

    public async Task OnGetAsync(
        int? year = null,
        int? customerCode = null,
        int? storeCode = null,
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
    }
}
