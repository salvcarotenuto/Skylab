using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.PianoConti;

public sealed class IndexModel(CustomerService service, UserService users) : PageModel
{
    public IReadOnlyList<AccountListItem> Items { get; private set; } = [];
    public IReadOnlyList<AccountMasterListItem> Masters { get; private set; } = [];
    public short NextCode { get; private set; } = 1;
    [BindProperty] public AccountEditModel Conto { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [BindProperty] public string AssistancePassword { get; set; } = "";
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            var assistanceAuthorized = IsNew || !await service.AccountLockedAsync(Conto.Code, ct) || await users.VerifyAssistancePasswordAsync(AssistancePassword, ct);
            if (!assistanceAuthorized) return StatusCode(403, new { requiresPassword = true, message = "Conto bloccato" });
            await service.SaveAccountAsync(Conto, IsNew, assistanceAuthorized, ct);
            Message = "Conto salvato.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "Conto bloccato") return StatusCode(403, new { requiresPassword = true, message = ex.Message });
            ModelState.AddModelError(nameof(Conto.Description), ex.Message);
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostValidateDeleteAsync(short code, CancellationToken ct)
    {
        if (await service.AccountInUseAsync(code, ct)) return StatusCode(409, new { message = "Il conto è utilizzato e non può essere eliminato." });
        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, string? assistancePassword, CancellationToken ct)
    {
        var result = await service.DeleteAccountAsync(code, token => users.VerifyAssistancePasswordAsync(assistancePassword ?? "", token), ct);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result is null) return new JsonResult(new { ok = true });
            return StatusCode(result == "Conto bloccato" ? 403 : 409, new { message = result, requiresPassword = result == "Conto bloccato" });
        }
        Message = result ?? "Conto eliminato.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Items = await service.AccountsAsync(ct);
        Masters = await service.AccountMastersAsync(ct);
        var used = Items.Select(x => x.Code).ToHashSet();
        for (short i = 1; i <= 999; i++)
        {
            if (!used.Contains(i)) { NextCode = i; return; }
        }
    }
}
