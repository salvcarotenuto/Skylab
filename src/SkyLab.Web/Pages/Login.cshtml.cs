using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages;

[EnableRateLimiting("login")]
public sealed class LoginModel(ApplicationAuthService auth, UserService users) : PageModel
{
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; private set; }
    public IReadOnlyList<SkyLab.Web.Models.UserListItem> Users { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (auth.IsLoggedIn()) return RedirectToPage("/Index");
        await LoadUsers(ct);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        try
        {
            if (await auth.LoginAsync(Username, Password, ct)) return RedirectToPage("/Index");
            Error = "Utente o password non corretti, oppure accesso non consentito.";
        }
        catch (MySqlConnector.MySqlException)
        {
            Error = "Servizio non disponibile. Riprovare tra poco.";
        }
        Password = "";
        ModelState.Remove(nameof(Password));
        await LoadUsers(ct);
        return Page();
    }
    public IActionResult OnPostLogout()
    {
        auth.Logout();
        return RedirectToPage();
    }
    private async Task LoadUsers(CancellationToken ct)
    {
        try { Users = (await users.SearchAsync("", ct)).Where(x => x.IsActive && !x.IsLocked && x.TypeCode is 1 or 2 or 3).ToList(); }
        catch (MySqlConnector.MySqlException) { Error = "Servizio non disponibile. Riprovare tra poco."; }
    }
}
