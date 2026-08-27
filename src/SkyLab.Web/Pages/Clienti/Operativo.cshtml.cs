using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Clienti;
public sealed class OperativoModel(CustomerService service):PageModel
{
 public int ClienteId{get;private set;}public string Cliente{get;private set;}="";public IReadOnlyList<OperationalSiteGroup> Sedi{get;private set;}=[];
 public int Totale=>Sedi.Sum(x=>x.Machines.Count);public int Scadute=>Sedi.Sum(x=>x.Machines.Count(m=>m.NextServiceOn<DateTime.Today));public int SenzaScadenza=>Sedi.Sum(x=>x.Machines.Count(m=>m.NextServiceOn is null));public decimal Valore=>Sedi.Sum(x=>x.Machines.Sum(m=>m.Value??0));
 public async Task<IActionResult> OnGetAsync(int id,CancellationToken ct){ClienteId=id;Cliente=await service.CustomerNameAsync(id,ct);if(string.IsNullOrWhiteSpace(Cliente))return NotFound();var machines=await service.OperationalMachinesAsync(id,ct);Sedi=machines.GroupBy(m=>new{m.SiteId,m.SiteName,m.SiteAddress}).Select(g=>new OperationalSiteGroup(g.Key.SiteId,g.Key.SiteId is null?"Indirizzo principale":g.Key.SiteName,g.Key.SiteAddress,g.ToList())).ToList();return Page();}
}
