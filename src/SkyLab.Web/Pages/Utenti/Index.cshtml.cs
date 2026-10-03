using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Utenti;

public sealed class IndexModel(UserService service, ApplicationAuthService auth) : PageModel
{
    public IReadOnlyList<UserListItem> Users { get; private set; } = [];
    public bool CanCreate { get; private set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!auth.IsLoggedIn() || auth.CurrentUserCode is not int actorCode) return RedirectToPage("/Login");
        var actorRank = await service.ManagementRankAsync(actorCode, ct);
        if (actorRank is null) { auth.Logout(); return RedirectToPage("/Login"); }
        Users = await service.SearchManageableAsync(actorCode, actorRank.Value, ct);
        CanCreate = service.AssignableTypes(actorRank.Value).Count > 0;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int code, CancellationToken ct)
    {
        if (!auth.IsLoggedIn() || auth.CurrentUserCode is not int actorCode) return RedirectToPage("/Login");
        if (code == actorCode || !await service.CanManageAsync(actorCode, code, ct))
        {
            ErrorMessage = "Non sei autorizzato a eliminare questo utente.";
            return RedirectToPage();
        }
        var result = await service.DeleteAsync(code, ct);
        if (!result.Deleted) ErrorMessage = result.Message;
        return RedirectToPage();
    }
}
