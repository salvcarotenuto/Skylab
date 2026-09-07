using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Tabelle.AliquoteIva;

public sealed class IndexModel(CustomerService service) : PageModel
{
    public IReadOnlyList<VatCodeListItem> Items { get; private set; } = [];
    public IReadOnlyList<CodeLookupItem> NatureCodes { get; private set; } = [];
    [BindProperty] public VatCodeEditModel CodiceIva { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            await service.SaveVatCodeAsync(CodiceIva, IsNew, ct);
            Message = "Codice IVA salvato.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(CodiceIva.Code), ex.Message);
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string code, CancellationToken ct)
    {
        Message = await service.DeleteVatCodeAsync(code, ct) ?? "Codice IVA eliminato.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Items = await service.VatCodeListAsync(ct);
        NatureCodes = await service.ElectronicInvoiceVatNaturesAsync(ct);
    }
}
