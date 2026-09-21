using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SkyLab.Web.Services;
using SkyLab.Web.Data;
using MySqlConnector;
using System.Reflection;
namespace SkyLab.Web.Pages.Opzioni;

public sealed class IndexModel(CustomerService customerService, SkyLabDatabase database, SmtpConnectionTester smtpTester) : PageModel
{
    private static readonly PropertyInfo[] OptionProperties =
        typeof(OptionsDraftModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);

    
    [BindProperty]
    public OptionsDraftModel Options { get; set; } = new();
    [BindProperty]
    public int ActiveTab { get; set; }

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
    public async Task<JsonResult> OnPostTestMailAsync([FromBody] SmtpTestRequest? request, CancellationToken cancellationToken) =>
        new(await smtpTester.TestAsync(request, cancellationToken));

    private async Task<OptionsDraftModel> LoadOptionsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(
            """
            SELECT Chiave, COALESCE(Valore, '') AS Valore
            FROM opzioni;
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
private static async Task SaveOptionAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            """
            INSERT INTO opzioni (Chiave, Valore)
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
    public string? SedeOperativaNazione { get; set; }

    public string? TitolareCognome { get; set; }
    public string? TitolareNome { get; set; }
    public string? TitolareDataNascita { get; set; }
    public string? TitolareCodiceFiscale { get; set; }
    public string? TitolareCitta { get; set; }
    public string? TitolareProvincia { get; set; }
    public string? TitolareCap { get; set; }
    public string? TitolareIndirizzo { get; set; }
    public string? TitolareTelefono { get; set; }
    public string? TitolareEmail { get; set; }
    public string? TitolarePec { get; set; }

    public string? AttivitaEsenzioneIva { get; set; }
    public string? CodiceEsenzioneIva { get; set; }
    public string? IvaSpeseIncasso { get; set; }
    public string? AliqIvaVendite { get; set; }
    public string? AliquotaRitenutaIrpef { get; set; }

    public string? FattPrefissoDocumenti { get; set; }
    public string? FattPagamentoStandard { get; set; }
    public string? FattBancaAppoggio { get; set; }
    public string? FattIban { get; set; }
    public string? FattSwift { get; set; }
    public string? FattSpeseIncasso { get; set; }

    public string? FeCodiceSdiAzienda { get; set; }
    public string? FePecDestinazioneSdi { get; set; }
    public string? FeMatriceNomeXml { get; set; }
    public string? FeRegimeFiscale { get; set; }
    public string? FeTipoRitenuta { get; set; }
    public string? FeCausaleRitenuta { get; set; }

    public string? MailOrdNomeMittente { get; set; }
    public string? MailOrdEmailMittente { get; set; }
    public string? MailOrdServerSmtp { get; set; }
    public string? MailOrdPortaSmtp { get; set; }
    public string? MailOrdSicurezza { get; set; }
    public string? MailOrdAutenticazione { get; set; }
    public string? MailOrdUsername { get; set; }
    public string? MailOrdPassword { get; set; }

    public string? MailPecNomeMittente { get; set; }
    public string? MailPecEmailMittente { get; set; }
    public string? MailPecServerSmtp { get; set; }
    public string? MailPecPortaSmtp { get; set; }
    public string? MailPecSicurezza { get; set; }
    public string? MailPecAutenticazione { get; set; }
    public string? MailPecUsername { get; set; }
    public string? MailPecPassword { get; set; }

    public string? BackupAbilitaAutomatica { get; set; }
    public string? BackupPosizione { get; set; }
    public string? BackupOrario { get; set; }
    public string? BackupCancellaVecchie { get; set; }
    public string? BackupGiorniVecchie { get; set; }

    public string? StampaDatiAzienda { get; set; }
    public string? StampaFontName { get; set; }
    public string? StampaFontSize { get; set; }
    public string? StampaAlign { get; set; }
    public string? TimbroTxt { get; set; }
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






