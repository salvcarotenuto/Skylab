using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.MovimentiBanca;

public sealed class IndexModel(
    AccountingMovementRepository repository,
    ApplicationState applicationState) : PageModel
{
    public BankMovementListPageModel List { get; private set; } = new();

    public async Task OnGetAsync(
        int? year,
        int? month,
        int? bankCode,
        int? causeCode,
        int? selectedId,
        CancellationToken cancellationToken)
    {
        List = await repository.GetBankMovementsAsync(
            applicationState.Esercizio,
            year,
            month,
            bankCode,
            causeCode,
            selectedId,
            cancellationToken);
    }
    public async Task<IActionResult> OnPostDeleteAsync(
        int deleteId,
        int? year,
        int? month,
        int? bankCode,
        int? causeCode,
        CancellationToken cancellationToken)
    {
        if (deleteId > 0)
        {
            await repository.DeleteBankMovementAsync(deleteId, cancellationToken);
        }

        return RedirectToPage("./Index", new
        {
            year,
            month = month is > 0 ? month : null,
            bankCode,
            causeCode
        });
    }
}
