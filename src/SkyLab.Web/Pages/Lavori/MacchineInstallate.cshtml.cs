using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class MacchineInstallateModel(PlanningService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? InstallateAl { get; set; }
    [BindProperty(SupportsGet = true)] public string Cerca { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public short Categoria { get; set; }
    [BindProperty(SupportsGet = true)] public string Ordina { get; set; } = "articolo";

    public IReadOnlyList<PlanningCategory> Categorie { get; private set; } = [];
    public IReadOnlyList<InstalledMachine> Macchine { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        InstallateAl ??= DateTime.Today;
        Categorie = await service.CategoriesAsync(cancellationToken);
        Macchine = await service.InstalledAsync(InstallateAl.Value, Cerca, Categoria, Ordina, cancellationToken);
    }
}
