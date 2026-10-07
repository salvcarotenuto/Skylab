using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.CausaliContabili;

public sealed class IndexModel(CustomerService service, UserService users) : PageModel
{
    public IReadOnlyList<AccountingCauseListItem> Items { get; private set; } = [];
    public IReadOnlyList<AccountListItem> Accounts { get; private set; } = [];
    public short NextCode { get; private set; } = 1;
    [BindProperty] public AccountingCauseEditModel Causale { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [BindProperty] public string AssistancePassword { get; set; } = "";
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            var assistanceAuthorized = IsNew;
            if (!IsNew)
            {
                var current = await service.AccountingCauseAsync(Causale.Code, ct);
                assistanceAuthorized = current is { Locked: false } || await users.VerifyAssistancePasswordAsync(AssistancePassword, ct);
                if (!assistanceAuthorized) throw new InvalidOperationException("Causale bloccata");
            }
            await service.SaveAccountingCauseAsync(Causale, IsNew, assistanceAuthorized, ct);
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(Causale.Description), ex.Message);
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, string? assistancePassword, CancellationToken ct)
    {
        var result = await service.DeleteAccountingCauseAsync(code, token => users.VerifyAssistancePasswordAsync(assistancePassword ?? "", token), ct);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result is null) return new JsonResult(new { ok = true });
            return StatusCode(result == "Causale bloccata" ? 403 : 409, new { ok = false, message = result, requiresPassword = result == "Causale bloccata" });
        }
        Message = result ?? "Causale contabile eliminata.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostValidateDeleteAsync(short code, CancellationToken ct)
    {
        if (await service.AccountingCauseInUseAsync(code, ct))
        {
            return StatusCode(409, new { ok = false, message = "La causale è utilizzata nei movimenti contabili e non può essere eliminata." });
        }
        return new JsonResult(new { ok = true });
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
