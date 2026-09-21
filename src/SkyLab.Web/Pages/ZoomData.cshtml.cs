using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages;

public sealed class ZoomDataModel(CustomerService customers) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string catalogo, CancellationToken ct)
    {
        if (!string.Equals(catalogo, "articoli", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var articles = await customers.ArticleChoicesAsync(ct);
        return new JsonResult(new
        {
            title = "Lista articoli",
            defaultSort = "description",
            columns = new object[]
            {
                new { key = "code", label = "Codice", width = "180px" },
                new { key = "description", label = "Descrizione" },
                new { key = "category", label = "Categoria", width = "220px" },
                new { key = "price", label = "Prezzo", type = "number", width = "120px", align = "right" }
            },
            rows = articles.Select(article => new
            {
                code = article.Code,
                description = article.Description,
                categoryCode = article.CategoryCode,
                category = article.Category,
                price = article.Price,
                duration = article.DurationDays,
                consumption = article.DailyConsumption
            })
        });
    }
}
