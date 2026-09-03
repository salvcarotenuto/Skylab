using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Interventi;

public sealed class IndexModel(WorkService service) : PageModel
{
    public IReadOnlyList<Intervention> Items { get; private set; } = [];
    public IReadOnlyList<OperatorLookupItem> Operators { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Il periodo viene applicato lato client: carichiamo anche lo storico,
        // altrimenti un intervallo personalizzato non può raggiungere le schede legacy.
        var works = await service.SearchAsync(new DateTime(1900, 1, 1), null, "lavoro", 0, 0, 0, cancellationToken);
        var flows = (await service.AgendaFlowAsync(cancellationToken)).ToDictionary(item => item.WorkId);
        Operators=await service.OperatorsAsync(cancellationToken);
        Items = works
            .Where(item => item.PlannedOn is not null)
            .Select(item => new Intervention
            {
                Id = item.Id,
                Customer = item.Customer,
                Site = item.Site,
                Plant = item.AssignedOperator,
                ScheduledAt = item.PlannedOn!.Value.Date + (item.PlannedAt ?? TimeSpan.Zero),
                DraftedAt = item.DraftedOn,
                Kind = item.Summary,
                AssignedOperatorId=item.AssignedOperatorId,
                DispatchedToWork=item.DispatchedToWork,
                WorkStatus=item.Status,
                SheetFlow=flows.TryGetValue(item.Id,out var flow)?flow.SheetFlow:(item.DispatchedToWork?"Sul mobile":"Da scaricare"),
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

    public async Task<IActionResult> OnPostAssignOperatorAsync(int workId,short operatorId,CancellationToken cancellationToken)
    {
        try{await service.AssignOperatorForDispatchAsync(workId,operatorId,cancellationToken);return new JsonResult(new{ok=true});}
        catch(InvalidOperationException exception){return BadRequest(new{message=exception.Message});}
    }

    public async Task<IActionResult> OnPostDispatchAsync([FromBody] int[] workIds,CancellationToken cancellationToken)
    {
        try{var count=await service.DispatchToWorkAsync(workIds,cancellationToken);return new JsonResult(new{ok=true,count});}
        catch(InvalidOperationException exception){return BadRequest(new{message=exception.Message});}
    }

    public async Task<IActionResult> OnGetFlowAsync(CancellationToken cancellationToken) =>
        new JsonResult(await service.AgendaFlowAsync(cancellationToken));
}
