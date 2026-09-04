using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Microsoft.AspNetCore.Mvc.Rendering;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Fornitori;
public sealed class EditModel(SupplierService service):PageModel
{
 [BindProperty] public SupplierEditModel Fornitore{get;set;}=new(); public IReadOnlyList<SelectListItem> Countries{get;private set;}=[];public IReadOnlyList<SelectListItem> Payments{get;private set;}=[];public IReadOnlyList<SelectListItem> Banks{get;private set;}=[];public IReadOnlyList<SelectListItem> LocalUnits{get;private set;}=[];
 public async Task<IActionResult> OnGetAsync(int? id,CancellationToken ct){if(id.HasValue){Fornitore=await service.GetAsync(id.Value,ct)??new();if(Fornitore.Code==0)return NotFound();}await LoadAsync(ct);return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid){await LoadAsync(ct);return Page();}try{await service.SaveAsync(Fornitore,ct);TempData["Message"]="Fornitore salvato.";return RedirectToPage("Index");}catch(Exception ex){ModelState.AddModelError("",ex.Message);await LoadAsync(ct);return Page();}}
 private async Task LoadAsync(CancellationToken ct){Countries=Map(await service.CountriesAsync(ct));Payments=Map(await service.PaymentsAsync(ct));Banks=Map(await service.BanksAsync(ct));LocalUnits=Map(await service.LocalUnitsAsync(ct));}private static IReadOnlyList<SelectListItem> Map(IReadOnlyList<SupplierOption> x)=>x.Select(v=>new SelectListItem(v.Label,v.Id.ToString())).ToList();
}
