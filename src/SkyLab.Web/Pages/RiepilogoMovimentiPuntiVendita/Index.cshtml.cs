using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace SkyLab.Web.Pages.RiepilogoMovimentiPuntiVendita;

public sealed class IndexModel(
    StoreMovementSummaryRepository repository,
    ApplicationState applicationState,
    StoreMovementSummaryPdfService pdfService) : PageModel
{
    public StoreMovementSummaryPageModel Report { get; private set; } = new();

    public async Task OnGetAsync(
        bool run,
        int? year,
        CancellationToken cancellationToken)
    {
        var selectedYear = year ?? applicationState.Esercizio;
        var baseYear = applicationState.Esercizio;
        Report = run
            ? await repository.GetAsync(selectedYear, baseYear, cancellationToken)
            : await repository.GetInitialAsync(selectedYear, baseYear, cancellationToken);
    }

    public async Task<IActionResult> OnPostPdfAsync(
        [FromBody] StoreMovementSummaryPrintRequest request,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetAsync(request.Year, applicationState.Esercizio, cancellationToken);
        var bytes = pdfService.Create(report, DateTime.Now);
        return File(bytes, "application/pdf", $"RiepilogoPV_{request.Year}.pdf");
    }
}
