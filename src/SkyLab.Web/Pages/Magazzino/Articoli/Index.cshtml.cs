using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Magazzino.Articoli;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<ArticleListItem> Items { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) => Items = await service.ArticlesAsync(ct);
}
