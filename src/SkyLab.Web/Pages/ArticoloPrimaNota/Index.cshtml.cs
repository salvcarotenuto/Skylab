using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Models;

namespace SkyLab.Web.Pages.ArticoloPrimaNota;

public sealed class IndexModel(AccountingMovementRepository repository) : PageModel
{
    public AccountingArticleDetailModel? Detail { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        Detail = await repository.GetArticleDetailAsync(id, cancellationToken);
        return Detail is null ? NotFound() : Page();
    }
}
