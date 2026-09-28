using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.FattureVendita;

public sealed class EditModel(
    ApplicationState applicationState,
    CustomerService customerService,
    SkyLabDatabase database) : PageModel
{
    public const int AccountingSector = 30;
    public const int SalesInvoiceCause = 30;
    public const int DebitNoteCause = 31;
    public const int CreditNoteCause = 32;

    public int Azione { get; private set; } = 2;
    public int Partita { get; private set; } = 1;
    public int Anno { get; private set; }
    public string DocumentNumber { get; private set; } = "";
    public DateOnly? DocumentDate { get; private set; }
    public IReadOnlyList<VatCodeListItem> VatCodes { get; private set; } = [];
    public decimal StandardVatRate { get; private set; } = 22m;
    public IReadOnlyList<CodeLookupItem> Agents { get; private set; } = [];
    public IReadOnlyList<CodeLookupItem> UnitLocations { get; private set; } = [];
    public IReadOnlyList<CodeLookupItem> UnitMeasures { get; private set; } = [];
    public IReadOnlyList<PartyLookupItem> Customers { get; private set; } = [];

    public async Task OnGetAsync(int azione = 2, CancellationToken cancellationToken = default)
    {
        Azione = azione;
        Anno = applicationState.Esercizio;
        if (Azione == 2)
        {
            DocumentDate = DateOnly.FromDateTime(DateTime.Today);
            DocumentNumber = await NextDocumentNumberAsync(Anno, cancellationToken);
        }
        VatCodes = await customerService.VatCodeListAsync(cancellationToken);
        StandardVatRate = await customerService.StandardSalesVatRateAsync(cancellationToken);
        Customers = (await customerService.SearchAsync(null, false, cancellationToken))
            .Select(customer => new PartyLookupItem(
                customer.Code,
                customer.Name,
                customer.City,
                customer.Province))
            .ToArray();
        Agents = await LoadAgentsAsync(cancellationToken);
        UnitLocations = await LoadUnitLocationsAsync(cancellationToken);
        UnitMeasures = await customerService.UnitMeasuresAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<CodeLookupItem>> LoadAgentsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Codice, COALESCE(Nome, '') FROM agenti ORDER BY Nome, Codice;";
        var agents = new List<CodeLookupItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            agents.Add(new CodeLookupItem(
                Convert.ToInt32(reader.GetValue(0)).ToString("000"),
                reader.GetString(1)));
        }

        return agents;
    }

    private async Task<IReadOnlyList<CodeLookupItem>> LoadUnitLocationsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Codice, COALESCE(NomeBreve, '') FROM unitalocali ORDER BY Codice;";
        var unitLocations = new List<CodeLookupItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            unitLocations.Add(new CodeLookupItem(
                Convert.ToInt32(reader.GetValue(0)).ToString("000"),
                reader.GetString(1)));
        }

        return unitLocations;
    }

    private async Task<string> NextDocumentNumberAsync(int year, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(MAX(
                CASE
                    WHEN TRIM(COALESCE(NumDoc, '')) REGEXP '^[0-9]+$'
                    THEN CAST(TRIM(NumDoc) AS UNSIGNED)
                    ELSE 0
                END
            ), 0) + 1
            FROM fatture
            WHERE Anno = @year;
            """;
        var yearParameter = command.CreateParameter();
        yearParameter.ParameterName = "@year";
        yearParameter.Value = year;
        command.Parameters.Add(yearParameter);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
