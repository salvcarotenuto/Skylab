using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkyLab.Web.Pages.SchedaContabile;

public sealed class IndexModel(
    AccountingStatementRepository repository,
    ApplicationState applicationState) : PageModel
{
    public AccountingStatementPageModel Report { get; private set; } = new();

    public string ReturnUrl { get; private set; } = "/";

    public bool IsReadOnlyNavigation =>
        ReturnUrl.StartsWith("/BilancioVerifica", StringComparison.OrdinalIgnoreCase);

    public async Task OnGetAsync(
        int? accountCode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        var year = applicationState.Esercizio;
        var from = dateFrom ?? new DateOnly(year, 1, 1);
        var to = dateTo ?? DefaultDateTo(year);

        Report = accountCode is > 0
            ? await repository.GetAsync(accountCode.Value, from, to, cancellationToken)
            : await repository.GetInitialAsync(year, cancellationToken);
    }

    private static DateOnly DefaultDateTo(int year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return today.Year == year ? today : new DateOnly(year, 12, 31);
    }
}
