using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using SkyLab.Web.Models;using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Clienti;
public sealed class EditModel(CustomerService service):PageModel
{
 [BindProperty(SupportsGet=true)] public bool Modal{get;set;}
 [BindProperty] public CustomerEditModel Cliente{get;set;}=new();
 public async Task<IActionResult> OnGetAsync(int? id,CancellationToken ct){if(id is null)return Page();var x=await service.CustomerAsync(id.Value,ct);if(x is null)return NotFound();Cliente=x;return Page();}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();var code=await service.SaveCustomerAsync(Cliente,ct);if(Modal){var payload=System.Text.Json.JsonSerializer.Serialize(new{type="skylab-customer-saved",code,name=Cliente.Name});return Content($"<!doctype html><html><body><script>parent.postMessage({payload},location.origin);</script></body></html>","text/html");}return RedirectToPage("Index");}
}
