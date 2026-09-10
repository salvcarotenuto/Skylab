using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SkyLab.Web.Services;
using SkyLab.Web.Data;
using MySqlConnector;
using System.Reflection;

namespace SkyLab.Web.Pages.Opzioni;

public sealed class IndexModel(CustomerService customerService, SkyLabDatabase database) : PageModel
{
    private static readonly PropertyInfo[] OptionProperties =
        typeof(OptionsDraftModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);

    [BindProperty]
    public OptionsDraftModel Options { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Options = await LoadOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        NormalizeOptions(Options);
        await SaveOptionsAsync(Options, cancellationToken);
        return RedirectToPage("/Index");
    }

    public async Task<JsonResult> OnGetCitiesAsync(string? q, CancellationToken cancellationToken) =>
        new(await customerService.SearchCitiesAsync(q, cancellationToken));

    private async Task<OptionsDraftModel> LoadOptionsAsync(CancellationToken cancellationToken)
    {
        await EnsureOptionsTableAsync(cancellationToken);
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(
            """
            SELECT Chiave, COALESCE(Valore, '') AS Valore
            FROM Opzioni;
            """,
            connection);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values[reader.GetString("Chiave")] = reader.GetString("Valore");
        }

        var options = new OptionsDraftModel();
        foreach (var property in OptionProperties)
        {
            if (values.TryGetValue(property.Name, out var value))
            {
                property.SetValue(options, value);
            }
        }

        return options;
    }

    private async Task SaveOptionsAsync(OptionsDraftModel options, CancellationToken cancellationToken)
    {
        await EnsureOptionsTableAsync(cancellationToken);
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var property in OptionProperties)
            {
                await SaveOptionAsync(
                    connection,
                    transaction,
                    property.Name,
                    Convert.ToString(property.GetValue(options))?.Trim() ?? "",
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task EnsureOptionsTableAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(
            """
            CREATE TABLE IF NOT EXISTS Opzioni (
                Chiave VARCHAR(100) NOT NULL PRIMARY KEY,
                Valore TEXT NULL
            );

            ALTER TABLE Opzioni
                MODIFY Chiave VARCHAR(100) NOT NULL,
                MODIFY Valore TEXT NULL;
            """,
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SaveOptionAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO Opzioni (Chiave, Valore)
            VALUES (@key, @value)
            ON DUPLICATE KEY UPDATE Valore = VALUES(Valore);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void NormalizeOptions(OptionsDraftModel options)
    {
        options.CodiceFiscale = NormalizeCode(options.CodiceFiscale, 16);
        options.PartitaIva = Digits(options.PartitaIva, 11);
        options.SiglaStato = NormalizeLetters(options.SiglaStato, 2);
        options.SedeLegaleProvincia = NormalizeLetters(options.SedeLegaleProvincia, 2);
        options.SedeOperativaProvincia = NormalizeLetters(options.SedeOperativaProvincia, 2);
        options.SedeLegaleCap = Digits(options.SedeLegaleCap, 5);
        options.SedeOperativaCap = Digits(options.SedeOperativaCap, 5);
    }

    private static string NormalizeCode(string? value, int maxLength) =>
        new((value ?? "")
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(maxLength)
            .ToArray());

    private static string NormalizeLetters(string? value, int maxLength) =>
        new((value ?? "")
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetter)
            .Take(maxLength)
            .ToArray());

    private static string Digits(string? value, int maxLength) =>
        new((value ?? "")
            .Where(char.IsDigit)
            .Take(maxLength)
            .ToArray());
}

public sealed class OptionsDraftModel
{
    public string? RagioneSociale { get; set; }
    public string? AttivitaEsercitata { get; set; }
    public string? CodiceFiscale { get; set; }
    public string? PartitaIva { get; set; }
    public string? NumeroRea { get; set; }
    public string? Telefono { get; set; }
    public string? SitoWeb { get; set; }
    public string? NaturaGiuridica { get; set; }
    public string? SedeLegaleCitta { get; set; }
    public string? SedeLegaleProvincia { get; set; }
    public string? SedeLegaleCap { get; set; }
    public string? SedeLegaleIndirizzo { get; set; }
    public string? AzPec { get; set; }
    public string? SiglaStato { get; set; }
    public string? SedeOperativaCitta { get; set; }
    public string? SedeOperativaProvincia { get; set; }
    public string? SedeOperativaCap { get; set; }
    public string? SedeOperativaIndirizzo { get; set; }
    public string? AzEmail { get; set; }
}
