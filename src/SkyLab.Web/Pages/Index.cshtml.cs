using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SkyLab.Web.Models;
using SkyLab.Web.Services;
namespace SkyLab.Web.Pages;

public class IndexModel : PageModel
{
    public IReadOnlyList<QuickLinkDefinition> QuickLinks => MainMenuCatalog.QuickLinks;
    public IReadOnlyList<MenuSectionDefinition> Sections => MainMenuCatalog.Sections;
    public int Esercizio => DateTime.Today.Year;
    public void OnGet() { }
}
