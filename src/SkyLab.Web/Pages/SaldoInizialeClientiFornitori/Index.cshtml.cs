using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.SaldoInizialeClientiFornitori;

public class IndexModel(
    InitialCustomerSupplierBalanceRepository repository,
    ApplicationState applicationState) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InitialCustomerSupplierBalanceList List { get; private set; } =
        new(applicationState.Esercizio, [], [], []);

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty]
    public string Payload { get; set; } = "";

    [TempData]
    public string? SavedMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var selectedYear = Year.GetValueOrDefault(applicationState.Esercizio);
        if (!IsAvailableYear(selectedYear))
        {
            ModelState.AddModelError(string.Empty, "L'esercizio selezionato non è disponibile.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var rows = ParsePayload();
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await repository.SaveAsync(selectedYear, rows, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(cancellationToken);
            return Page();
        }

        SavedMessage = $"Saldi iniziali clienti e fornitori salvati correttamente per l'esercizio {selectedYear}.";
        return RedirectToPage(new { year = selectedYear });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var selectedYear = Year.GetValueOrDefault(applicationState.Esercizio);
        if (!IsAvailableYear(selectedYear))
        {
            selectedYear = applicationState.Esercizio;
        }

        List = await repository.ListAsync(selectedYear, applicationState.Esercizio, cancellationToken);
        Year = List.Year;
    }

    private bool IsAvailableYear(int year) =>
        year >= applicationState.Esercizio - 5 && year <= applicationState.Esercizio;

    private IReadOnlyList<InitialCustomerSupplierBalanceSaveRow> ParsePayload()
    {
        if (string.IsNullOrWhiteSpace(Payload))
        {
            ModelState.AddModelError("", "Nessun saldo da salvare.");
            return [];
        }

        try
        {
            var rows = JsonSerializer.Deserialize<List<InitialCustomerSupplierBalanceSaveRow>>(
                Payload,
                JsonOptions) ?? [];
            if (rows.Count == 0)
            {
                ModelState.AddModelError("", "Nessun saldo da salvare.");
            }

            return rows;
        }
        catch (JsonException)
        {
            ModelState.AddModelError("", "Dati saldi non validi.");
            return [];
        }
    }
}
