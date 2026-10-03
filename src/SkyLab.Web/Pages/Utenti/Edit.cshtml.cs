using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Utenti;

public sealed class EditModel(UserService service, ApplicationAuthService auth) : PageModel
{
    [BindProperty] public UserEditModel AppUser { get; set; } = new();
    [BindProperty] public int Azione { get; set; } = FormAzione.Inserimento;
    public bool IsNew => FormAzione.IsInserimento(Azione);
    public int DisplayCode { get; private set; }
    public SelectList Locations { get; private set; } = Empty();
    public SelectList Types { get; private set; } = Empty();
    public SelectList Qualifications { get; private set; } = Empty();

    public async Task<IActionResult> OnGetAsync(int? code, int? azione, CancellationToken ct)
    {
        if (!auth.IsLoggedIn() || auth.CurrentUserCode is not int actorCode) return RedirectToPage("/Login");
        var actorRank = await service.ManagementRankAsync(actorCode, ct);
        if (actorRank is null) { auth.Logout(); return RedirectToPage("/Login"); }

        Azione = FormAzione.ForRecord(code.HasValue);
        if (code.HasValue)
        {
            if (!await service.CanManageAsync(actorCode, code.Value, ct)) return Forbid();
            var found = await service.GetAsync(code.Value, ct);
            if (found is null) return NotFound();
            AppUser = found;
        }
        else
        {
            if (service.AssignableTypes(actorRank.Value).Count == 0) return Forbid();
            DisplayCode = await service.NextCodeAsync(ct);
        }

        await LoadAsync(actorRank.Value, code ?? 0, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!auth.IsLoggedIn() || auth.CurrentUserCode is not int actorCode) return RedirectToPage("/Login");
        var actorRank = await service.ManagementRankAsync(actorCode, ct);
        if (actorRank is null) { auth.Logout(); return RedirectToPage("/Login"); }

        var isNew = AppUser.Code <= 0;
        Azione = isNew ? FormAzione.Inserimento : FormAzione.Modifica;
        Normalize();

        UserEditModel? original = null;
        if (isNew)
        {
            if (AppUser.TypeCode is not int newType || newType <= actorRank.Value)
                ModelState.AddModelError(nameof(AppUser.TypeCode), "È possibile assegnare solo un tipo utente di rango inferiore.");
            if (string.IsNullOrWhiteSpace(AppUser.Password))
                ModelState.AddModelError(nameof(AppUser.Password), "Inserire la password per il nuovo utente.");
        }
        else
        {
            if (!await service.CanManageAsync(actorCode, AppUser.Code, ct)) return Forbid();
            original = await service.GetAsync(AppUser.Code, ct);
            if (original is null) return NotFound();

            if (AppUser.Code == actorCode)
            {
                AppUser.TypeCode = original.TypeCode;
                AppUser.IsActive = original.IsActive;
                AppUser.IsLocked = original.IsLocked;
            }
            else if (AppUser.TypeCode is not int newType || newType <= actorRank.Value)
            {
                ModelState.AddModelError(nameof(AppUser.TypeCode), "L'utente deve mantenere un rango inferiore al tuo.");
            }
        }

        if (!ModelState.IsValid)
        {
            if (isNew) DisplayCode = await service.NextCodeAsync(ct);
            await LoadAsync(actorRank.Value, isNew ? 0 : AppUser.Code, ct);
            return Page();
        }

        try
        {
            await service.SaveAsync(AppUser, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAsync(actorRank.Value, isNew ? 0 : AppUser.Code, ct);
            return Page();
        }
        catch (MySqlException ex)
        {
            ModelState.AddModelError("", $"Salvataggio non riuscito: {ex.Message}");
            await LoadAsync(actorRank.Value, isNew ? 0 : AppUser.Code, ct);
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private async Task LoadAsync(int actorRank, int targetCode, CancellationToken ct)
    {
        var lookups = await service.GetLookupsAsync(ct);
        Locations = Select(lookups.Locations);
        Qualifications = Select(lookups.Qualifications);
        var allowedTypes = targetCode == auth.CurrentUserCode
            ? lookups.Types.Where(x => x.Code == actorRank)
            : lookups.Types.Where(x => x.Code > actorRank);
        Types = Select(allowedTypes.ToArray());
    }

    private void Normalize()
    {
        AppUser.LastName = AppUser.LastName?.Trim() ?? "";
        AppUser.FirstName = AppUser.FirstName?.Trim() ?? "";
        AppUser.UserName = AppUser.UserName?.Trim() ?? "";
        AppUser.Password = AppUser.Password?.Trim() ?? "";
        AppUser.City = AppUser.City?.Trim();
        AppUser.Address = AppUser.Address?.Trim();
        AppUser.TaxCode = AppUser.TaxCode?.Trim().ToUpperInvariant();
        AppUser.Phone = AppUser.Phone?.Trim();
        AppUser.Email = AppUser.Email?.Trim();
    }

    private static SelectList Select(IEnumerable<LookupOption> items) => new(items, nameof(LookupOption.Code), nameof(LookupOption.Description));
    private static SelectList Empty() => new(Array.Empty<LookupOption>());
}
