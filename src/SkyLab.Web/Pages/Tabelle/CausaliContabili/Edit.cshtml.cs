using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.CausaliContabili;

public sealed class EditModel(CustomerService service) : PageModel
{
    public IReadOnlyList<AccountListItem> Accounts { get; private set; } = [];
    [BindProperty] public AccountingCauseEditModel Causale { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
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
            await service.SaveAccountingCauseAsync(Causale, IsNew, ct);
            if (ajax)
            {
                return new JsonResult(new { ok = true, url = Url.Page("/Tabelle/CausaliContabili/Index") ?? "/Tabelle/CausaliContabili/Index" });
            }
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            if (ajax)
            {
                return BadRequest(new { ok = false, message = ex.Message });
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
