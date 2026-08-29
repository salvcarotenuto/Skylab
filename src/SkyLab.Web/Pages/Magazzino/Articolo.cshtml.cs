using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Magazzino;

public sealed class ArticoloModel(CustomerService customers) : PageModel
{
    public ArticleDetail Articolo { get; private set; } = null!;
    public int ClienteId { get; private set; }

    public async Task<IActionResult> OnGetAsync(string codice, int clienteId = 0, CancellationToken ct = default)
    {
        var article = await customers.ArticleDetailAsync(codice, ct);
        if (article is null) return NotFound();
        Articolo = article;
        ClienteId = clienteId;
        return Page();
    }
}
