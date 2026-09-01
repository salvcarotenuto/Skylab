using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class SchedeModel(WorkService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? Da { get; set; }
    [BindProperty(SupportsGet = true)] public string OrdinaPer { get; set; } = "scheda";
    [BindProperty(SupportsGet = true, Name = "indietro")] public bool Indietro { get; set; }
    [BindProperty(SupportsGet = true, Name = "indietroAnno")] public bool IndietroAnno { get; set; }
    [BindProperty(SupportsGet = true)] public byte Stato { get; set; }
    [BindProperty(SupportsGet = true)] public byte Esito { get; set; }

    public IReadOnlyList<WorkListItem> Items { get; private set; } = [];
    public IReadOnlyList<WorkLookupItem> Stati { get; private set; } = [];
    public IReadOnlyList<WorkLookupItem> Esiti { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (OrdinaPer != "lavoro") OrdinaPer = "scheda";
        if (Da is null)
        {
            Da = DateTime.Today.AddDays(-29);
        }
        if (Indietro)
        {
            Da = Da.Value.AddDays(-30);
            ModelState.Remove(nameof(Da));
        }
        if (IndietroAnno)
        {
            Da = Da.Value.AddYears(-1);
            ModelState.Remove(nameof(Da));
        }
        Stati = await service.StatusesAsync(cancellationToken);
        Esiti = await service.OutcomesAsync(cancellationToken);
        Items = await service.SearchAsync(Da.Value, OrdinaPer, Stato, Esito, cancellationToken);
    }
}
