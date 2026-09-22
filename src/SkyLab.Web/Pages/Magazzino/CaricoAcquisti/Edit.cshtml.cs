using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using SkyLab.Web.Data;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Magazzino.CaricoAcquisti;

public sealed class EditModel(SkyLabServicePaths servicePaths, SkyLabDatabaseOptions databaseOptions) : PageModel
{
    public int Azione { get; private set; } = 2;
    public int Partita { get; private set; } = 124;
    public int Anno { get; private set; } = 2026;
    public string ElectronicInvoiceFolderDefault { get; private set; } = "";

    public async Task OnGetAsync(int azione = 2, CancellationToken cancellationToken = default)
    {
        Azione = azione;
        servicePaths.EnsureCreated();
        ElectronicInvoiceFolderDefault = servicePaths.FEAcquistiTransito;

        if (azione is not (2 or 102))
        {
            return;
        }

        Anno = DateTime.Today.Year;
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            "SELECT GREATEST(COALESCE(MAX(Codice), 0), @initialLastCode) + 1 FROM carico WHERE Anno = @year;",
            connection);
        command.Parameters.AddWithValue("@year", Anno);
        command.Parameters.AddWithValue("@initialLastCode", Anno == 2026 ? 124 : 0);
        Partita = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }
}
