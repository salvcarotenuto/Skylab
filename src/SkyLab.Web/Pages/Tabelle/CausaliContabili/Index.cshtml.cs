using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.CausaliContabili;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<AccountingCauseListItem> Items { get; private set; } = [];
    public IReadOnlyList<AccountListItem> Accounts { get; private set; } = [];
    public short NextCode { get; private set; } = 1;
    [BindProperty] public AccountingCauseEditModel Causale { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            await service.SaveAccountingCauseAsync(Causale, IsNew, ct);
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(Causale.Description), ex.Message);
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, CancellationToken ct)
    {
        Message = await service.DeleteAccountingCauseAsync(code, ct) ?? "Causale contabile eliminata.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Items = await service.AccountingCausesAsync(ct);
        Accounts = await service.AccountsAsync(ct);
        var used = Items.Select(x => x.Code).ToHashSet();
        for (short i = 1; i <= 999; i++)
        {
            if (!used.Contains(i)) { NextCode = i; return; }
        }
    }
}
