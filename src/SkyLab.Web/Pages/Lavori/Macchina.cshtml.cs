using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class MacchinaModel(CustomerService customers, PlanningService planning) : PageModel
{
    [BindProperty]
    public MachineEditModel Macchina { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Origine { get; set; } = "installate";

    [BindProperty(SupportsGet = true)]
    public string ReturnTo { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "";

    public bool EsciAlFascicolo =>
        string.Equals(Origine, "operativo", StringComparison.OrdinalIgnoreCase) && Macchina.CustomerId > 0;

    public string DescrizioneArticolo { get; private set; } = "";
    public IReadOnlyList<LookupItem> Clienti { get; private set; } = [];
    public IReadOnlyList<LookupItem> Sedi { get; private set; } = [];
    public IReadOnlyList<PlanningCategory> Categorie { get; private set; } = [];
    public IReadOnlyList<ArticleChoice> Articoli { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? id, int? clienteId, CancellationToken ct)
    {
        if (id.HasValue)
        {
            var x = await customers.MachineAsync(id.Value, ct);
            if (x is null)
            {
                return NotFound();
            }

            Macchina = x;
        }
        else if (clienteId.HasValue)
        {
            Macchina.CustomerId = clienteId.Value;
        }

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await customers.SaveMachineAsync(Macchina, ct);
        return ReturnToCaller();
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken ct)
    {
        if (Macchina.Id <= 0 || Macchina.CustomerId <= 0)
        {
            return BadRequest();
        }

        var deleted = await customers.DeleteMachineAsync(Macchina.Id, Macchina.CustomerId, ct);
        if (!deleted)
        {
            return NotFound();
        }

        return ReturnToCaller();
    }

    private IActionResult ReturnToCaller()
    {
        var normalizedReturnUrl = NormalizeReturnUrl(ReturnUrl);
        if (!string.IsNullOrWhiteSpace(normalizedReturnUrl))
        {
            return Redirect(normalizedReturnUrl);
        }

        if (string.Equals(ReturnTo, "operativo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Origine, "operativo", StringComparison.OrdinalIgnoreCase))
        {
            if (Macchina.CustomerId > 0)
            {
                return RedirectToPage("/Clienti/Operativo", new { id = Macchina.CustomerId });
            }
        }

        if (string.Equals(ReturnTo, "installate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Origine, "installate", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("MacchineInstallate");
        }

        return RedirectToPage("MacchineInstallate");
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "";
        }

        var trimmed = returnUrl.Trim();
        if (!trimmed.StartsWith("/"))
        {
            return "";
        }

        if (trimmed.IndexOf("://", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "";
        }

        return trimmed;
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Clienti = await customers.CustomerLookupAsync(ct);
        Categorie = await planning.CategoriesAsync(ct);
        Articoli = await customers.ArticleChoicesAsync(ct);
        DescrizioneArticolo = await customers.ArticleDescriptionAsync(Macchina.ArticleCode, ct);
        if (Macchina.CustomerId > 0)
        {
            Sedi = await customers.SiteLookupAsync(Macchina.CustomerId, ct);
        }
    }
}
