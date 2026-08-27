using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Interventi;
public sealed class DetailModel(InterventionService service) : PageModel
{
    public Intervention Item { get; private set; }=null!;
    [BindProperty] public string Barcode { get; set; }="";
    [BindProperty] public decimal Quantity { get; set; }=1;
    [BindProperty] public string Notes { get; set; }="";
    public IActionResult OnGet(int id) { var item=service.Find(id); if(item is null)return NotFound(); Item=item; Notes=item.Notes; return Page(); }
    public IActionResult OnPostMaterial(int id) { service.AddMaterial(id,Barcode,Quantity); return RedirectToPage(new{id}); }
    public IActionResult OnPostComplete(int id) { service.Complete(id,Notes); return RedirectToPage(new{id}); }
}
