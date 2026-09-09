using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.FattureAcquisto;

public sealed class IndexModel(
    PurchaseInvoiceRepository repository,
    ApplicationState applicationState) : PageModel
{
    public PurchaseInvoiceListPageModel List { get; private set; } = new();

    public async Task OnGetAsync(
        int? year,
        int? month,
        string? causeCode,
        int? supplierCode,
        int? storeCode,
        int? contraAccountCode,
        bool showDueDates,
        int? selectedId,
        CancellationToken cancellationToken)
    {
        var selectedYear = year
            ?? await repository.GetDefaultListYearAsync(applicationState.Esercizio, cancellationToken);

        List = await repository.GetListAsync(
            selectedYear,
            month,
            causeCode,
            supplierCode,
            storeCode,
            contraAccountCode,
            showDueDates,
            selectedId,
            cancellationToken);
    }
    public async Task<IActionResult> OnPostDeleteAsync(
        int invoiceId,
        int? year,
        int? month,
        string? causeCode,
        int? supplierCode,
        int? storeCode,
        int? contraAccountCode,
        bool showDueDates,
        CancellationToken cancellationToken)
    {
        if (invoiceId > 0)
        {
            await repository.DeleteByIdAsync(invoiceId, cancellationToken);
        }

        return RedirectToPage("./Index", new
        {
            year,
            month,
            causeCode,
            supplierCode,
            storeCode,
            contraAccountCode,
            showDueDates
        });
    }
}
