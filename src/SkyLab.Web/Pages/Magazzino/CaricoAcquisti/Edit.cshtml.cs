using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkyLab.Web.Pages.Magazzino.CaricoAcquisti;

public sealed class EditModel : PageModel
{
    public int Azione { get; private set; } = 2;
    public void OnGet(int azione = 2) => Azione = azione;
}
