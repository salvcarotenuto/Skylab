using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.PrimaNota;

public sealed class IndexModel(
    AccountingMovementRepository repository,
    ApplicationState applicationState) : PageModel
{
    public AccountingMovementListPageModel List { get; private set; } = new();

    [TempData]
    public string? DeleteWarning { get; set; }

    public async Task OnGetAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? causeCode,
        string? movementType,
        string? subjectType,
        int? dittaCode,
        int? selectedId,
        CancellationToken cancellationToken)
    {
        var year = applicationState.Esercizio;
        var start = dateFrom ?? new DateOnly(year, 1, 1);
        var end = dateTo ?? DefaultEndDate(year);

        List = await repository.GetListAsync(
            year,
            start,
            end,
            null,
            causeCode,
            movementType,
            subjectType,
            dittaCode,
            selectedId,
            cancellationToken);
    }

    private static DateOnly DefaultEndDate(int year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return today.Year == year ? today : new DateOnly(year, 12, 31);
    }
    public async Task<IActionResult> OnPostDeleteAsync(
        int movementId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? causeCode,
        string? movementType,
        string? subjectType,
        int? dittaCode,
        CancellationToken cancellationToken)
    {
        if (movementId > 0)
        {
            var result = await repository.DeleteMovementAsync(movementId, cancellationToken);
            if (result == AccountingMovementDeleteResult.FiscalDocumentMovement)
            {
                DeleteWarning = "Questo movimento è generato da un documento fiscale e non può essere cancellato in Prima Nota. Per cancellarlo, utilizzare il modulo specifico del documento.";
            }
        }

        return RedirectToPage("./Index", new
        {
            dateFrom = dateFrom?.ToString("yyyy-MM-dd"),
            dateTo = dateTo?.ToString("yyyy-MM-dd"),
            causeCode,
            movementType,
            subjectType,
            dittaCode
        });
    }
}
