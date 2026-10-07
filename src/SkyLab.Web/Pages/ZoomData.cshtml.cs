using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Data;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages;

public sealed class ZoomDataModel(CustomerService customers, LookupRepository lookup) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string catalogo, string? tipo, CancellationToken ct)
    {
        if (string.Equals(catalogo, "anagrafiche", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedType = tipo?.Trim().ToUpperInvariant();
            var lookupType = normalizedType switch
            {
                "C" => "clienti",
                "F" => "fornitori",
                "B" => "banche",
                "A" => "agenti",
                "D" => "dipendenti",
                _ => null
            };
            if (lookupType is null)
            {
                return BadRequest();
            }

            var title = normalizedType switch
            {
                "C" => "Clienti",
                "F" => "Fornitori",
                "B" => "Banche",
                "A" => "Agenti",
                "D" => "Dipendenti",
                _ => "Anagrafiche"
            };
            var entries = await lookup.SearchAnagraficheAsync(lookupType, null, ct);
            return new JsonResult(new
            {
                title,
                defaultSort = "label",
                columns = new object[]
                {
                    new { key = "code", label = "Codice", width = "104px", align = "center" },
                    new { key = "label", label = "Nome" },
                    new { key = "detail", label = "Sede", width = "330px" }
                },
                rows = entries.Select(entry => new
                {
                    code = entry.CodeLabel,
                    label = entry.Label,
                    detail = entry.Detail
                })
            });
        }

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
                new { key = "code", label = "Codice", width = "180px", align = "left" },
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
                vatCode = article.VatCode,
                duration = article.DurationDays,
                consumption = article.DailyConsumption
            })
        });
    }
}
