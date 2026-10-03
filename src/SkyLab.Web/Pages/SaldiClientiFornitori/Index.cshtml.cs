using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkyLab.Web.Pages.SaldiClientiFornitori;

public sealed class IndexModel(
    CustomerSupplierBalanceSummaryRepository repository,
    ApplicationState applicationState) : PageModel
{
    public CustomerSupplierBalanceSummaryPageModel Report { get; private set; } = new();

    public async Task OnGetAsync(
        int? year,
        bool showZero,
        int? storeCode,
        CancellationToken cancellationToken)
    {
        Report = await repository.GetAsync(
            year ?? applicationState.Esercizio,
            applicationState.Esercizio,
            showZero,
            storeCode,
            cancellationToken);
    }
}
