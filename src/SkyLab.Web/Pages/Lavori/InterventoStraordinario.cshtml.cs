using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class InterventoStraordinarioModel(CustomerService customers, WorkService works, PlanningService planning) : PageModel
{
    public IReadOnlyList<CustomerListItem> Clienti { get; private set; } = [];
    public IReadOnlyList<WorkReferenceLookup> Prestazioni { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Clienti = await customers.SearchAsync(null, false, cancellationToken);
        Prestazioni = (await works.WorkReferencesAsync(cancellationToken)).Where(x => x.Type == "P").ToList();
    }

    public async Task<JsonResult> OnGetCustomerDataAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.CustomerAsync(customerId, cancellationToken);
        if (customer is null) return new JsonResult(new { found = false });
        var sites = await customers.SitesAsync(customerId, cancellationToken);
        var machines = await customers.OperationalMachinesAsync(customerId, cancellationToken);
        var mainSite = string.Join(" · ", new[]
        {
            "Sede principale",
            string.Join(" ", new[] { customer.Street, customer.StreetNumber }.Where(x => !string.IsNullOrWhiteSpace(x))),
            customer.City
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new JsonResult(new
        {
            found = true,
            sites = new[] { new { id = 0, label = mainSite } }.Concat(sites.Where(x => x.Active).Select(x => new
            {
                id = x.Id,
                label = string.Join(" · ", new[] { x.Name, x.Street, x.City }.Where(v => !string.IsNullOrWhiteSpace(v)))
            })),
            machines = machines.Select(x => new
            {
                id = x.Id,
                siteId = x.SiteId ?? 0,
                label = string.Join(" · ", new[] { x.ArticleCode, x.Description }.Where(v => !string.IsNullOrWhiteSpace(v)))
            })
        });
    }

    public async Task<IActionResult> OnPostAsync(int clienteId,int? sedeId,int? macchinaId,string? descrizione,DateTime? dataIntervento,TimeSpan? oraIntervento,string? note,CancellationToken cancellationToken)
    {
        if(clienteId<=0||string.IsNullOrWhiteSpace(descrizione)||dataIntervento is null)
        {
            TempData["ErrorMessage"]="Compilare cliente, intervento richiesto e data concordata.";
            return RedirectToPage();
        }
        try
        {
            await planning.CreateExtraordinaryCommitmentAsync(clienteId,sedeId.GetValueOrDefault()>0?sedeId:null,macchinaId,dataIntervento.Value,oraIntervento,descrizione,note,cancellationToken);
            var defaultFrom=DateTime.Today.AddDays(-60).Date;
            var defaultTo=DateTime.Today.AddDays(60).Date;
            var plannedDate=dataIntervento.Value.Date;
            var visibleFrom=plannedDate<defaultFrom?plannedDate:defaultFrom;
            var visibleTo=plannedDate>defaultTo?plannedDate:defaultTo;
            return RedirectToPage("Pianificazione",new{dal=visibleFrom.ToString("yyyy-MM-dd"),al=visibleTo.ToString("yyyy-MM-dd")});
        }
        catch(InvalidOperationException exception)
        {
            TempData["ErrorMessage"]=exception.Message;
            return RedirectToPage();
        }
    }
}
