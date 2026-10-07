using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.Mastri;

public sealed class IndexModel(CustomerService service, UserService users) : PageModel
{
    public IReadOnlyList<AccountMasterListItem> Items { get; private set; } = [];
    public short NextCode { get; private set; } = 1;
    [BindProperty] public AccountMasterEditModel Mastro { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [BindProperty] public string? AssistancePassword { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            var assistanceAuthorized = IsNew || !await service.AccountMasterLockedAsync(Mastro.Code, ct) || await users.VerifyAssistancePasswordAsync(AssistancePassword ?? "", ct);
            if (!assistanceAuthorized) return StatusCode(403, new { message = "Password non corretta." });
            await service.SaveAccountMasterAsync(Mastro, IsNew, assistanceAuthorized, ct);
            Message = "Mastro salvato.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "Mastro bloccato") return StatusCode(403, new { message = "Password non corretta." });
            ModelState.AddModelError(nameof(Mastro.Description), ex.Message);
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostValidateDeleteAsync(short code, CancellationToken ct)
    {
        if (await service.AccountMasterInUseAsync(code, ct)) return StatusCode(409, new { message = "Il mastro è utilizzato nel piano dei conti e non può essere eliminato." });
        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostValidateAssistanceAsync(CancellationToken ct)
    {
        if (IsNew || Mastro.Code <= 0) return BadRequest(new { ok = false, message = "Mastro non valido." });
        if (!await service.AccountMasterLockedAsync(Mastro.Code, ct)) return new JsonResult(new { ok = true });
        if (await users.VerifyAssistancePasswordAsync(AssistancePassword ?? "", ct)) return new JsonResult(new { ok = true });
        return StatusCode(403, new { ok = false, message = "Password non corretta." });
    }

    public async Task<IActionResult> OnPostDeleteAsync(short code, string? assistancePassword, CancellationToken ct)
    {
        var result = await service.DeleteAccountMasterAsync(code, token => users.VerifyAssistancePasswordAsync(assistancePassword ?? "", token), ct);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (result is null) return new JsonResult(new { ok = true });
            return StatusCode(result == "Mastro bloccato" ? 403 : 409, new { message = result, requiresPassword = result == "Mastro bloccato" });
        }
        Message = result ?? "Mastro eliminato.";
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
