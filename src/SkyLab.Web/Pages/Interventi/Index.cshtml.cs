using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;
namespace SkyLab.Web.Pages.Interventi;
public sealed class IndexModel(InterventionService service) : PageModel
{
    public IReadOnlyList<Intervention> Items { get; private set; }=[];
    public void OnGet() => Items=service.All();
}
