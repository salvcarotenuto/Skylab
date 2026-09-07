using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Aspetto;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    [BindProperty] public GoodsAppearanceEditModel Aspetto { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => Items = await service.GoodsAppearancesAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { Items = await service.GoodsAppearancesAsync(ct); return Page(); }
        await service.SaveGoodsAppearanceAsync(Aspetto, IsNew, ct);
        Message = "Aspetto beni salvato.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, CancellationToken ct)
    {
        Message = await service.DeleteGoodsAppearanceAsync(code, ct) ?? "Aspetto beni eliminato.";
        return RedirectToPage();
    }
}
