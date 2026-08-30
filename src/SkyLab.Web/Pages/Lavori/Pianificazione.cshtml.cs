using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class PianificazioneModel(PlanningService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? Dal { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Al { get; set; }
    [BindProperty(SupportsGet = true)] public string Cerca { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public byte Tipologia { get; set; }
    [BindProperty(SupportsGet = true)] public short Distretto { get; set; }

    public IReadOnlyList<PlanningDistrict> Distretti { get; private set; } = [];
    public IReadOnlyList<PlanningCustomerGroup> Clienti { get; private set; } = [];
    public int TotaleMacchine => Clienti.Sum(x => x.Items.Count);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dal ??= DateTime.Today.AddDays(-60);
        Al ??= DateTime.Today.AddDays(60);
        if (Al < Dal) (Dal, Al) = (Al, Dal);

        Distretti = await service.DistrictsAsync(cancellationToken);
        var items = await service.DueAsync(Dal.Value, Al.Value, Cerca, Tipologia, Distretto, cancellationToken);
        Clienti = items.GroupBy(x => new { x.CustomerId, x.CustomerName, x.City, x.District, x.CustomerType })
            .Select(g => new PlanningCustomerGroup(g.Key.CustomerId, g.Key.CustomerName, g.Key.City,
                g.Key.District, g.Key.CustomerType, g.ToList()))
            .ToList();
    }

    public async Task<IActionResult> OnPostAcquireAsync(int macchinaId,int clienteId,DateTime scadenza,DateTime? dataIntervento,TimeSpan? oraIntervento,string? descrizione,string? note,CancellationToken cancellationToken)
    {
        if (dataIntervento is null)
        {
            ModelState.AddModelError(string.Empty, "La data concordata è obbligatoria.");
            await OnGetAsync(cancellationToken);
            return Page();
        }

        await service.AcquireCommitmentAsync(macchinaId,clienteId,scadenza,dataIntervento,oraIntervento,descrizione,note,cancellationToken);
        return RedirectToPage(new { dal=Dal?.ToString("yyyy-MM-dd"),al=Al?.ToString("yyyy-MM-dd"),cerca=Cerca,tipologia=Tipologia,distretto=Distretto });
    }

    public async Task<IActionResult> OnPostPrepareWorkAsync(int impegnoId,CancellationToken cancellationToken)
    {
        try
        {
            var workId=await service.CreateWorkFromCommitmentAsync(impegnoId,cancellationToken);
            return RedirectToPage("/Lavori/Scheda",new{id=workId});
        }
        catch(InvalidOperationException exception)
        {
            TempData["ErrorMessage"]=exception.Message;
            return RedirectToPage(new{Dal,Al,Cerca,Tipologia,Distretto});
        }
    }

    public async Task<JsonResult> OnGetAvailabilityAsync(DateTime date,CancellationToken cancellationToken)
        => new(await service.DayAvailabilityAsync(date,cancellationToken));

    public async Task<JsonResult> OnGetAgendaAsync(DateTime? to,CancellationToken cancellationToken)
    {
        var start=DateTime.Today;
        var end=(to??DateTime.Today.AddDays(60)).Date;
        if(end<start)end=start;
        if((end-start).TotalDays>366)end=start.AddDays(366);
        return new JsonResult(await service.AgendaAsync(start,end,cancellationToken));
    }
}
