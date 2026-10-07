using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.CausaliContabili;

public sealed class EditModel(CustomerService service, UserService users) : PageModel
{
    public IReadOnlyList<AccountListItem> Accounts { get; private set; } = [];
    [BindProperty] public AccountingCauseEditModel Causale { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [BindProperty] public string? AssistancePassword { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(int? code, CancellationToken ct)
    {
        Accounts = await service.AccountsAsync(ct);
        if (code is null or <= 0)
        {
            IsNew = true;
            Causale.Code = await NextCodeAsync(ct);
            return Page();
        }

        if (code > short.MaxValue) return RedirectToPage("./Index");
        var item = await service.AccountingCauseAsync((short)code.Value, ct);
        if (item is null) return RedirectToPage("./Index");
        Causale = item;
        IsNew = false;
        return Page();
    }

    public async Task<IActionResult> OnPostValidateAssistanceAsync(CancellationToken ct)
    {
        if (IsNew || Causale.Code <= 0) return BadRequest(new { ok = false, message = "Causale non valida." });

        var current = await service.AccountingCauseAsync(Causale.Code, ct);
        if (current is null) return NotFound(new { ok = false, message = "Causale non trovata." });
        if (!current.Locked || await users.VerifyAssistancePasswordAsync(AssistancePassword ?? "", ct))
            return new JsonResult(new { ok = true });

        return StatusCode(403, new { ok = false, message = "Password non corretta." });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Accounts = await service.AccountsAsync(ct);
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        if (!ModelState.IsValid)
        {
            if (ajax)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                    .ToArray();
                return BadRequest(new { ok = false, message = errors.Length == 0 ? "Controllare i dati inseriti nella causale." : string.Join("\n", errors) });
            }
            return Page();
        }
        try
        {
            var assistanceAuthorized = IsNew;
            if (!IsNew)
            {
                var current = await service.AccountingCauseAsync(Causale.Code, ct);
                assistanceAuthorized = current is { Locked: false } || await users.VerifyAssistancePasswordAsync(AssistancePassword ?? "", ct);
                if (!assistanceAuthorized) throw new InvalidOperationException("Password non corretta.");
            }
            await service.SaveAccountingCauseAsync(Causale, IsNew, assistanceAuthorized, ct);
            if (ajax)
            {
                return new JsonResult(new { ok = true, url = Url.Page("/Tabelle/CausaliContabili/Index") ?? "/Tabelle/CausaliContabili/Index" });
            }
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException ex)
        {
            var message = ex.Message == "Causale bloccata" ? "Password non corretta." : ex.Message;
            ModelState.AddModelError(string.Empty, message);
            if (ajax)
            {
                return BadRequest(new { ok = false, message });
            }
            return Page();
        }
    }

    private async Task<short> NextCodeAsync(CancellationToken ct)
    {
        var used = (await service.AccountingCausesAsync(ct)).Select(x => x.Code).ToHashSet();
        for (short i = 1; i <= 999; i++) if (!used.Contains(i)) return i;
        return 999;
    }
}
