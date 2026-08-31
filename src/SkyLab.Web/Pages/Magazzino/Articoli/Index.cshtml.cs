using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Magazzino.Articoli;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<ArticleListItem> Items { get; private set; } = [];
    [TempData] public string? ErrorMessage { get; set; }
    public async Task OnGetAsync(CancellationToken ct) => Items = await service.ArticlesAsync(ct);
    public async Task<IActionResult> OnPostDeleteAsync(string codice,CancellationToken ct)
    {
        ErrorMessage=await service.DeleteArticleAsync(codice,ct);
        return RedirectToPage();
    }
}
