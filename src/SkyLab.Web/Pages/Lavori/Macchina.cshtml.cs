using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Lavori;
public sealed class MacchinaModel(CustomerService customers,PlanningService planning):PageModel
{
 [BindProperty]public MachineEditModel Macchina{get;set;}=new();
 [BindProperty(SupportsGet=true)]public string Origine{get;set;}="installate";
 public bool EsciAlFascicolo=>string.Equals(Origine,"operativo",StringComparison.OrdinalIgnoreCase)&&Macchina.CustomerId>0;
 public string DescrizioneArticolo{get;private set;}="";
 public IReadOnlyList<LookupItem> Clienti{get;private set;}=[];public IReadOnlyList<LookupItem>Sedi{get;private set;}=[];public IReadOnlyList<PlanningCategory>Categorie{get;private set;}=[];public IReadOnlyList<ArticleChoice> Articoli{get;private set;}=[];
 public async Task<IActionResult> OnGetAsync(int? id,int? clienteId,CancellationToken ct){if(id.HasValue){var x=await customers.MachineAsync(id.Value,ct);if(x is null)return NotFound();Macchina=x;}else if(clienteId.HasValue)Macchina.CustomerId=clienteId.Value;await LoadAsync(ct);return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){await LoadAsync(ct);if(!ModelState.IsValid)return Page();await customers.SaveMachineAsync(Macchina,ct);return RedirectToPage("/Clienti/Operativo",new{id=Macchina.CustomerId});}
 public async Task<IActionResult> OnPostDeleteAsync(CancellationToken ct){if(Macchina.Id<=0||Macchina.CustomerId<=0)return BadRequest();if(!await customers.DeleteMachineAsync(Macchina.Id,Macchina.CustomerId,ct))return NotFound();return RedirectToPage("/Clienti/Operativo",new{id=Macchina.CustomerId});}
 private async Task LoadAsync(CancellationToken ct){Clienti=await customers.CustomerLookupAsync(ct);Categorie=await planning.CategoriesAsync(ct);Articoli=await customers.ArticleChoicesAsync(ct);DescrizioneArticolo=await customers.ArticleDescriptionAsync(Macchina.ArticleCode,ct);if(Macchina.CustomerId>0)Sedi=await customers.SiteLookupAsync(Macchina.CustomerId,ct);}
}
