using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Clienti.Sedi;
public sealed class IndexModel(CustomerService service):PageModel{public int ClienteId{get;private set;}public string Cliente{get;private set;}="";public IReadOnlyList<SiteListItem> Items{get;private set;}=[];public async Task OnGetAsync(int clienteId,CancellationToken ct){ClienteId=clienteId;Cliente=await service.CustomerNameAsync(clienteId,ct);Items=await service.SitesAsync(clienteId,ct);}}
