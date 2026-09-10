using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SkyLab.Web.Models;
using SkyLab.Web.Services;
namespace SkyLab.Web.Pages;

public class IndexModel(ApplicationState applicationState) : PageModel
{
    public IReadOnlyList<QuickLinkDefinition> QuickLinks => MainMenuCatalog.QuickLinks;
    public IReadOnlyList<MenuSectionDefinition> Sections => MainMenuCatalog.Sections;
    public int Esercizio => applicationState.Esercizio;
    public void OnGet() { }
}
