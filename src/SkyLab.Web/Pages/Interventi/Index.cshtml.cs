using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Interventi;

public sealed class IndexModel(WorkService service) : PageModel
{
    public IReadOnlyList<Intervention> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Il periodo viene applicato lato client: carichiamo anche lo storico,
        // altrimenti un intervallo personalizzato non può raggiungere le schede legacy.
        var works = await service.SearchAsync(new DateTime(1900, 1, 1), "lavoro", 0, 0, cancellationToken);
        Items = works
            .Where(item => item.PlannedOn is not null)
            .Select(item => new Intervention
            {
                Id = item.Id,
                Customer = item.Customer,
                Site = "",
                Plant = item.AssignedOperator,
                ScheduledAt = item.PlannedOn!.Value.Date + (item.PlannedAt ?? TimeSpan.Zero),
                Kind = item.Summary,
                Status = item.StatusId switch
                {
                    3 => InterventionStatus.InProgress,
                    >= 5 => InterventionStatus.Completed,
                    _ => InterventionStatus.Planned
                }
            })
            .OrderBy(item => item.ScheduledAt)
            .ToList();
    }
}
