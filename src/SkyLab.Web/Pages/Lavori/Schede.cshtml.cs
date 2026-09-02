using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class SchedeModel(WorkService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? Da { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Al { get; set; }
    [BindProperty(SupportsGet = true)] public string OrdinaPer { get; set; } = "lavoro";
    [BindProperty(SupportsGet = true)] public byte Stato { get; set; }
    [BindProperty(SupportsGet = true)] public short Operatore { get; set; }
    [BindProperty(SupportsGet = true)] public byte Esito { get; set; }

    public IReadOnlyList<WorkListItem> Items { get; private set; } = [];
    public IReadOnlyList<WorkLookupItem> Stati { get; private set; } = [];
    public IReadOnlyList<OperatorLookupItem> Operatori { get; private set; } = [];
    public IReadOnlyList<WorkLookupItem> Esiti { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (OrdinaPer != "lavoro") OrdinaPer = "scheda";
        if (Da is null)
        {
            Da = OrdinaPer == "lavoro" ? DateTime.Today : new DateTime(DateTime.Today.Year, 1, 1);
            ModelState.Remove(nameof(Da));
        }
        if (Al is null)
        {
            Al = OrdinaPer == "lavoro" ? DateTime.Today.AddYears(1) : DateTime.Today;
            ModelState.Remove(nameof(Al));
        }
        if (Al < Da) (Da, Al) = (Al, Da);
        Stati = await service.StatusesAsync(cancellationToken);
        Operatori = await service.OperatorsAsync(cancellationToken);
        Esiti = await service.OutcomesAsync(cancellationToken);
        Items = await service.SearchAsync(Da.Value, Al.Value, OrdinaPer, Stato, Operatore, Esito, cancellationToken);
    }
}
