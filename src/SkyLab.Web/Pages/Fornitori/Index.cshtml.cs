using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Fornitori;
public sealed class IndexModel(SupplierService service):PageModel
{
 [BindProperty(SupportsGet=true)] public string Cerca{get;set;}=""; public IReadOnlyList<SupplierListItem> Items{get;private set;}=[];[TempData] public string? Message{get;set;}
 public async Task OnGetAsync(CancellationToken ct)=>Items=await service.SearchAsync(Cerca,ct);
 public async Task<IActionResult> OnPostDeleteAsync(int code,CancellationToken ct){Message=await service.DeleteAsync(code,ct)??"Fornitore eliminato.";return RedirectToPage();}
}
