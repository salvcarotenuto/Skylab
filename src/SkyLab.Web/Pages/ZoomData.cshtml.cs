using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages;

public sealed class ZoomDataModel(CustomerService customers) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string catalogo, CancellationToken ct)
    {
        if (string.Equals(catalogo, "fornitori", StringComparison.OrdinalIgnoreCase))
        {
            var suppliers = await customers.SuppliersAsync(ct);
            return new JsonResult(new
            {
                title = "Lista fornitori",
                defaultSort = "name",
                columns = new object[]
                {
                    new { key = "code", label = "Codice", width = "110px", align = "center" },
                    new { key = "name", label = "Nome" },
                    new { key = "city", label = "Città", width = "220px" },
                    new { key = "province", label = "Prov.", width = "80px", align = "center" }
                },
                rows = suppliers.Select(supplier => new
                {
                    code = supplier.Code.ToString("00000"),
                    name = supplier.Name,
                    city = supplier.City,
                    province = supplier.Province
                })
            });
        }

        if (string.Equals(catalogo, "clienti", StringComparison.OrdinalIgnoreCase))
        {
            var clients = await customers.CustomerLookupAsync(ct);
            return new JsonResult(new
            {
                title = "Lista clienti",
                defaultSort = "name",
                columns = new object[]
                {
                    new { key = "code", label = "Codice", width = "110px", align = "center" },
                    new { key = "name", label = "Nome" }
                },
                rows = clients.Select(client => new
                {
                    code = client.Id.ToString("00000"),
                    name = client.Label
                })
            });
        }

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
                unitMeasure = article.UnitMeasure,
                vatRate = article.VatRate,
                duration = article.DurationDays,
                consumption = article.DailyConsumption
            })
        });
    }
}
