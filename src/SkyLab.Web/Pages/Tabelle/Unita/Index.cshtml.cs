using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Unita;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<CodeLookupItem> Items { get; private set; } = [];
    [BindProperty] public UnitMeasureEditModel Unita { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => Items = await service.UnitMeasuresAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { Items = await service.UnitMeasuresAsync(ct); return Page(); }
        try
        {
            await service.SaveUnitMeasureAsync(Unita, IsNew, ct);
            Message = "Unità di misura salvata.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(Unita.Code), ex.Message);
            Items = await service.UnitMeasuresAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string code, CancellationToken ct)
    {
        Message = await service.DeleteUnitMeasureAsync(code, ct) ?? "Unità di misura eliminata.";
        return RedirectToPage();
    }
}
