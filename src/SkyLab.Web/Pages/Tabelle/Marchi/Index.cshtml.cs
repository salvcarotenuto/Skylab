using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Marchi;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    [BindProperty] public InventoryBrandEditModel Marchio { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => Items = await service.InventoryBrandsAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { Items = await service.InventoryBrandsAsync(ct); return Page(); }
        await service.SaveInventoryBrandAsync(Marchio, IsNew, ct);
        Message = "Marchio salvato.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, CancellationToken ct)
    {
        Message = await service.DeleteInventoryBrandAsync(code, ct) ?? "Marchio eliminato.";
        return RedirectToPage();
    }
}
