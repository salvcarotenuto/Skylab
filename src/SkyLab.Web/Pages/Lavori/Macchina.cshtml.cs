using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Lavori;
public sealed class MacchinaModel(CustomerService customers,PlanningService planning):PageModel
{
 [BindProperty]public MachineEditModel Macchina{get;set;}=new();
 public IReadOnlyList<LookupItem> Clienti{get;private set;}=[];public IReadOnlyList<LookupItem>Sedi{get;private set;}=[];public IReadOnlyList<PlanningCategory>Categorie{get;private set;}=[];
 public async Task<IActionResult> OnGetAsync(int? id,int? clienteId,CancellationToken ct){if(id.HasValue){var x=await customers.MachineAsync(id.Value,ct);if(x is null)return NotFound();Macchina=x;}else if(clienteId.HasValue)Macchina.CustomerId=clienteId.Value;await LoadAsync(ct);return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){await LoadAsync(ct);if(!ModelState.IsValid)return Page();var id=await customers.SaveMachineAsync(Macchina,ct);TempData["Message"]="Scheda macchina salvata";return RedirectToPage(new{id});}
 private async Task LoadAsync(CancellationToken ct){Clienti=await customers.CustomerLookupAsync(ct);Categorie=await planning.CategoriesAsync(ct);if(Macchina.CustomerId>0)Sedi=await customers.SiteLookupAsync(Macchina.CustomerId,ct);}
}
