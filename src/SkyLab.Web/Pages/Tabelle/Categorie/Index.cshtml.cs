using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Categorie;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Items = await service.ArticleCategoriesAsync(cancellationToken);
}
