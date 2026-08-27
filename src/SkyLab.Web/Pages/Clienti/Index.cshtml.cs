using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Clienti;
public sealed class IndexModel(CustomerService service):PageModel
{
 [BindProperty(SupportsGet=true)] public string Cerca{get;set;}="";
 [BindProperty(SupportsGet=true)] public bool IncludiNonAttivi{get;set;}
 public IReadOnlyList<CustomerListItem> Items{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct)=>Items=await service.SearchAsync(Cerca,IncludiNonAttivi,ct);
}
