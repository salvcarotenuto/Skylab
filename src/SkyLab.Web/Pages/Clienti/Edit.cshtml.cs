using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Clienti;
public sealed class EditModel(CustomerService service):PageModel
{
 [BindProperty] public CustomerEditModel Cliente{get;set;}=new();
 public async Task<IActionResult> OnGetAsync(int? id,CancellationToken ct){if(id is null)return Page();var x=await service.CustomerAsync(id.Value,ct);if(x is null)return NotFound();Cliente=x;return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();await service.SaveCustomerAsync(Cliente,ct);return RedirectToPage("Index");}
}
