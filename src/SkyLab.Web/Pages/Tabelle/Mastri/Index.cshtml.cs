using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Mastri;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<AccountMasterListItem> Items { get; private set; } = [];
    public short NextCode { get; private set; } = 1;
    [BindProperty] public AccountMasterEditModel Mastro { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            await service.SaveAccountMasterAsync(Mastro, IsNew, ct);
            Message = "Mastro salvato.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(Mastro.Description), ex.Message);
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, CancellationToken ct)
    {
        Message = await service.DeleteAccountMasterAsync(code, ct) ?? "Mastro eliminato.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Items = await service.AccountMastersAsync(ct);
        var used = Items.Select(x => x.Code).ToHashSet();
        for (short i = 1; i <= 999; i++)
        {
            if (!used.Contains(i)) { NextCode = i; return; }
        }
    }
}
