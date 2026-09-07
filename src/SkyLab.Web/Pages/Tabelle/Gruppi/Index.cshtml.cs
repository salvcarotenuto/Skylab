using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Gruppi;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    [BindProperty] public InventoryGroupEditModel Gruppo { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => Items = await service.InventoryGroupsAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { Items = await service.InventoryGroupsAsync(ct); return Page(); }
        await service.SaveInventoryGroupAsync(Gruppo, IsNew, ct);
        Message = "Gruppo salvato.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, CancellationToken ct)
    {
        Message = await service.DeleteInventoryGroupAsync(code, ct) ?? "Gruppo eliminato.";
        return RedirectToPage();
    }
}
