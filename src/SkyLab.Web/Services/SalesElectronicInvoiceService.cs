using System.Globalization;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using MySqlConnector;
using SkyLab.Web.Data;
using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public sealed record SalesElectronicInvoicePrepared(string FileName, bool AlreadyPrepared);

public sealed class SalesElectronicInvoiceService(
    SkyLabDatabaseOptions databaseOptions,
    SalesInvoiceRepository invoices,
    CustomerService customers,
    SkyLabServicePaths paths,
    IWebHostEnvironment environment)
{
    private const int StatusEmessa = 0;
    private const int StatusPrepared = 1;
    private const int StatusSent = 2;
    private const int StatusRejected = 3;
    private const ulong MaxFileSerial = 60_466_175;
    private const string Base36Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public async Task<SalesElectronicInvoicePrepared> PrepareAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await invoices.FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Fattura non trovata.");
        var customer = await customers.CustomerAsync(invoice.CustomerCode, cancellationToken)
            ?? throw new InvalidOperationException("Cliente della fattura non trovato.");
        var options = await LoadOptionsAsync(cancellationToken);
        var vatCodeInfo = await LoadVatCodeInfoAsync(invoice.Lines, cancellationToken);
        var vatLines = BuildVatLines(invoice, vatCodeInfo);
        ValidateInvoice(invoice, customer, options, vatLines);

        paths.EnsureCreated();
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        string? createdFile = null;
        var committed = false;
        try
        {
            int state;
            string previousFile;
            decimal storedTaxable, storedVat, storedTotal;
            await using (var current = new MySqlCommand(
                "SELECT COALESCE(Stato,0),COALESCE(FeName,''),COALESCE(Imponibile,0),COALESCE(Iva,0),COALESCE(Totale,0) FROM Fatture WHERE ID=@id FOR UPDATE;",
                connection, transaction))
            {
                current.Parameters.AddWithValue("@id", id);
                await using var reader = await current.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Fattura non trovata.");
                state = reader.GetInt32(0);
                previousFile = reader.GetString(1);
                storedTaxable = reader.GetDecimal(2);
                storedVat = reader.GetDecimal(3);
                storedTotal = reader.GetDecimal(4);
            }

            if (state == StatusPrepared)
            {
                if (!string.IsNullOrWhiteSpace(previousFile) && File.Exists(Path.Combine(paths.FEVendite, Path.GetFileName(previousFile))))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new SalesElectronicInvoicePrepared(Path.GetFileName(previousFile), true);
                }
                throw new InvalidOperationException("La fattura risulta elaborata, ma il file archiviato non è disponibile.");
            }
            if (state == StatusSent)
            {
                var sentFileName = Path.GetFileName(previousFile);
                if (string.IsNullOrWhiteSpace(sentFileName) || !File.Exists(Path.Combine(paths.FEVendite, sentFileName)))
                    throw new InvalidOperationException($"Incoerenza fattura: lo stato indica FE inviata, ma il file XML {sentFileName} non è presente nell'archivio. Nessun reinvio è stato eseguito.");
                throw new InvalidOperationException("La fattura risulta già inviata allo SdI.");
            }
            if (state == StatusRejected) throw new InvalidOperationException("La fattura è stata rifiutata dallo SdI: correggere i dati prima di una nuova trasmissione.");
            if (state != StatusEmessa) throw new InvalidOperationException($"Stato fattura non gestito ({state}).");

            var taxable = Round(invoice.WorkAmount + invoice.Lines.Sum(row => row.Amount));
            var tax = Round(vatLines.Sum(row => row.Tax));
            var total = Round(taxable + tax);
            if (Math.Abs(taxable - storedTaxable) > 0.01m || Math.Abs(tax - storedVat) > 0.01m || Math.Abs(total - storedTotal) > 0.01m)
                throw new InvalidOperationException("I totali calcolati non coincidono con quelli registrati in fattura.");

            var (lastSerial, lastProgressive) = await ReserveOptionRowsAsync(connection, transaction, cancellationToken);
            var nextSerial = ParseLastSerial(lastSerial) + 1;
            if (nextSerial > MaxFileSerial) throw new InvalidOperationException("Il seriale del nome file ha raggiunto il limite di 5 caratteri Base36.");
            var serialText = ToBase36(nextSerial).PadLeft(5, '0');
            var nextProgressive = IncrementProgressive(lastProgressive);
            var matrix = NormalizeLetters(Get(options, "SiglaStato"), 2) + Digits(Get(options, "PartitaIva"), 11);
            var fileName = $"{matrix}_{serialText}.xml";
            var xml = BuildXml(invoice, customer, options, nextProgressive, vatLines, taxable, tax, total);
            var destination = Path.Combine(paths.FEVendite, fileName);
            if (File.Exists(destination)) throw new InvalidOperationException("Il nome del file XML esiste già nell'archivio; nessun file è stato sovrascritto.");

            await using (var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, useAsync: true))
            {
                await xml.SaveAsync(file, SaveOptions.None, cancellationToken);
            }
            createdFile = destination;

            await SaveOptionAsync(connection, transaction, "FeUltimoSerialeNomeFile", nextSerial.ToString(CultureInfo.InvariantCulture), cancellationToken);
            await SaveOptionAsync(connection, transaction, "FeUltimoProgressivoInvioXml", nextProgressive, cancellationToken);
            await using (var update = new MySqlCommand("UPDATE Fatture SET FeName=@name, Stato=@state WHERE ID=@id AND Stato=@oldState;", connection, transaction))
            {
                update.Parameters.AddWithValue("@name", fileName);
                update.Parameters.AddWithValue("@state", StatusPrepared);
                update.Parameters.AddWithValue("@id", id);
                update.Parameters.AddWithValue("@oldState", StatusEmessa);
                if (await update.ExecuteNonQueryAsync(cancellationToken) != 1) throw new InvalidOperationException("Lo stato della fattura è cambiato durante la preparazione.");
            }

            await transaction.CommitAsync(cancellationToken);
            committed = true;
            return new SalesElectronicInvoicePrepared(fileName, false);
        }
        catch
        {
            if (!committed && createdFile is not null)
            {
                try { File.Delete(createdFile); } catch { }
            }
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<string> ReadArchivedHtmlAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        string storedName;
        await using (var command = new MySqlCommand("SELECT COALESCE(FeName,'') FROM Fatture WHERE ID=@id LIMIT 1;", connection))
        {
            command.Parameters.AddWithValue("@id", id);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull) throw new InvalidOperationException("Fattura non trovata.");
            storedName = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        var safeName = Path.GetFileName(storedName);
        if (string.IsNullOrWhiteSpace(safeName) || !string.Equals(safeName, storedName, StringComparison.Ordinal))
            throw new InvalidOperationException("La fattura non ha un nome XML valido associato.");
        var root = Path.GetFullPath(paths.FEVendite) + Path.DirectorySeparatorChar;
        var filePath = Path.GetFullPath(Path.Combine(paths.FEVendite, safeName));
        if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
            throw new InvalidOperationException("Il file XML non è presente nell'archivio.");
        var stylesheetPath = Path.Combine(environment.WebRootPath, "xsl", "fattura_elettronica.xsl");
        if (!File.Exists(stylesheetPath)) throw new InvalidOperationException("Foglio XSL per la visualizzazione non disponibile.");
        try
        {
            var transform = new XslCompiledTransform();
            transform.Load(stylesheetPath);
            var document = XDocument.Load(filePath);
            if (document.Root is null) throw new XmlException("Documento XML senza elemento radice.");
            foreach (var element in document.Root.Descendants().ToList())
                element.Name = element.Name.LocalName;
            using var reader = document.CreateReader();
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            transform.Transform(reader, null, writer);
            return writer.ToString();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or XsltException)
        {
            throw new InvalidOperationException("Il file XML non è leggibile o non può essere visualizzato.", exception);
        }
    }

    public async Task<string> SendAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            string fileName;
            await using (var current = new MySqlCommand("SELECT COALESCE(Stato,0),COALESCE(FeName,'') FROM Fatture WHERE ID=@id FOR UPDATE;", connection, transaction))
            {
                current.Parameters.AddWithValue("@id", id);
                await using var reader = await current.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Fattura non trovata.");
                var state = reader.GetInt32(0);
                fileName = reader.GetString(1);
                if (state is not (StatusPrepared or StatusRejected)) throw new InvalidOperationException(state == StatusSent ? "La fattura risulta già inviata. Registrare prima l'eventuale rifiuto dello SdI." : "Preparare e archiviare la fattura prima dell'invio.");
            }

            var safeName = Path.GetFileName(fileName);
            var filePath = Path.GetFullPath(Path.Combine(paths.FEVendite, safeName));
            var archiveRoot = Path.GetFullPath(paths.FEVendite) + Path.DirectorySeparatorChar;
            if (!filePath.StartsWith(archiveRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
                throw new InvalidOperationException("File XML archiviato non trovato.");

            var options = await LoadOptionsAsync(connection, transaction, cancellationToken);
            var recipient = Get(options, "FePecDestinazioneSdi").Trim();
            if (recipient.Length == 0) throw new InvalidOperationException("Indirizzo PEC SDI non configurato in Opzioni.");
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            await SendSmtpAsync(options, recipient, safeName, fileBytes, cancellationToken);

            await using var update = new MySqlCommand("UPDATE Fatture SET Stato=@sent WHERE ID=@id AND Stato IN (@prepared,@rejected);", connection, transaction);
            update.Parameters.AddWithValue("@sent", StatusSent);
            update.Parameters.AddWithValue("@id", id);
            update.Parameters.AddWithValue("@prepared", StatusPrepared);
            update.Parameters.AddWithValue("@rejected", StatusRejected);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1) throw new InvalidOperationException("Impossibile aggiornare lo stato di invio della fattura.");
            await transaction.CommitAsync(cancellationToken);
            return safeName;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkRejectedAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            "UPDATE Fatture SET Stato=@rejected WHERE ID=@id AND Stato=@sent;", connection);
        command.Parameters.AddWithValue("@rejected", StatusRejected);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@sent", StatusSent);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException("È possibile registrare un rifiuto SdI solo per una fattura già inviata (stato 2). La fattura non è stata modificata.");
    }

    private async Task<Dictionary<string, string>> LoadOptionsAsync(CancellationToken ct)
    {
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(ct);
        return await LoadOptionsAsync(connection, null, ct);
    }

    private static async Task<Dictionary<string, string>> LoadOptionsAsync(MySqlConnection connection, MySqlTransaction? transaction, CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new MySqlCommand("SELECT Chiave,COALESCE(Valore,'') FROM opzioni;", connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result[reader.GetString(0)] = reader.GetString(1);
        return result;
    }

    private async Task<IReadOnlyDictionary<string, VatCodeInfo>> LoadVatCodeInfoAsync(IReadOnlyList<SalesInvoiceLineSaveRequest> lines, CancellationToken ct)
    {
        var result = new Dictionary<string, VatCodeInfo>(StringComparer.OrdinalIgnoreCase);
        if (lines.Count == 0) return result;
        await using var connection = new MySqlConnection(databaseOptions.BuildCompanyConnectionString());
        await connection.OpenAsync(ct);
        foreach (var row in lines)
        {
            var vatCode = row.VatCode.Trim();
            if (vatCode.Length == 0)
            {
                await using var articleCommand = new MySqlCommand("SELECT COALESCE(CodIva,'') FROM Articoli WHERE Codice=@article LIMIT 1;", connection);
                articleCommand.Parameters.AddWithValue("@article", row.ArticleCode);
                vatCode = Convert.ToString(await articleCommand.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture)?.Trim() ?? "";
            }

            var key = VatLineKey(row.ArticleCode, row.VatCode);
            if (vatCode.Length == 0) continue;
            await using var vatCommand = new MySqlCommand("SELECT COALESCE(CodiceFE,'') FROM Codiciiva WHERE Codice=@code LIMIT 1;", connection);
            vatCommand.Parameters.AddWithValue("@code", vatCode);
            await using var vatReader = await vatCommand.ExecuteReaderAsync(ct);
            if (await vatReader.ReadAsync(ct)) result[key] = new VatCodeInfo(vatReader.GetString(0));
        }
        return result;
    }

    private static string VatLineKey(string articleCode, string vatCode) => $"{articleCode.Trim()}\u001f{vatCode.Trim()}";

    private static IReadOnlyList<VatSummary> BuildVatLines(SalesInvoiceEditResult invoice, IReadOnlyDictionary<string, VatCodeInfo> vatCodes)
    {
        var lines = new List<(decimal Rate, string Nature, decimal Amount)>();
        if (invoice.WorkAmount > 0)
        {
            if (invoice.WorkVatRate == 0) throw new InvalidOperationException("L'aliquota IVA del lavoro è zero: per generare l'XML occorre specificare la relativa Natura IVA.");
            lines.Add((invoice.WorkVatRate, "", invoice.WorkAmount));
        }
        foreach (var row in invoice.Lines)
        {
            var rate = row.VatRate;
            var nature = "";
            if (vatCodes.TryGetValue(VatLineKey(row.ArticleCode, row.VatCode), out var vatCode))
            {
                nature = vatCode.Nature;
            }
            if (rate == 0 && string.IsNullOrWhiteSpace(nature))
                throw new InvalidOperationException($"Natura IVA mancante per il codice IVA {row.VatCode} dell'articolo {row.ArticleCode}.");
            lines.Add((rate, nature.Trim(), row.Amount));
        }
        return lines.GroupBy(row => (row.Rate, row.Nature))
            .Select(group => new VatSummary(group.Key.Rate, group.Key.Nature, Round(group.Sum(row => row.Amount)), Round(group.Sum(row => row.Amount) * group.Key.Rate / 100m)))
            .OrderBy(row => row.Rate).ThenBy(row => row.Nature).ToArray();
    }

    private static void ValidateInvoice(SalesInvoiceEditResult invoice, CustomerEditModel customer, IReadOnlyDictionary<string, string> options, IReadOnlyList<VatSummary> vatLines)
    {
        var country = NormalizeLetters(Get(options, "SiglaStato"), 2);
        var vat = Digits(Get(options, "PartitaIva"), 11);
        Need(!string.IsNullOrWhiteSpace(Get(options, "RagioneSociale")), "Ragione sociale azienda mancante in Opzioni.");
        Need(country.Length == 2, "Sigla stato della sede legale non valida in Opzioni.");
        Need(vat.Length == 11, "Partita IVA azienda non valida in Opzioni.");
        Need(!string.IsNullOrWhiteSpace(Get(options, "FeRegimeFiscale")), "Regime fiscale non selezionato in Opzioni.");
        Need(!string.IsNullOrWhiteSpace(Get(options, "SedeLegaleIndirizzo")), "Indirizzo della sede legale mancante in Opzioni.");
        Need(!string.IsNullOrWhiteSpace(Get(options, "SedeLegaleCitta")), "Comune della sede legale mancante in Opzioni.");
        Need(Digits(Get(options, "SedeLegaleCap"), 5).Length == 5, "CAP della sede legale non valido in Opzioni.");
        Need(!string.IsNullOrWhiteSpace(invoice.DocumentNumber), "Numero documento mancante.");
        Need(invoice.DocumentDate != default, "Data documento mancante.");
        Need(invoice.CustomerCode > 0 && !string.IsNullOrWhiteSpace(customer.Name), "Cliente o nome cliente mancante.");
        Need(!string.IsNullOrWhiteSpace(customer.Street), "Indirizzo del cliente mancante.");
        Need(!string.IsNullOrWhiteSpace(customer.City), "Comune del cliente mancante.");
        Need(Digits(customer.PostalCode, 5).Length == 5, "CAP del cliente non valido.");
        Need(!string.IsNullOrWhiteSpace(customer.VatNumber) || !string.IsNullOrWhiteSpace(customer.TaxCode), "Partita IVA o codice fiscale del cliente mancante.");
        Need(invoice.Cause is 30 or 31 or 32, "Tipo documento non gestito per la fattura elettronica.");
        Need(invoice.WorkAmount >= 0 && invoice.WorkVatRate is >= 0 and <= 100, "Importo o aliquota IVA lavoro non validi.");
        Need(invoice.WorkAmount == 0 || !string.IsNullOrWhiteSpace(invoice.Activity), "Descrizione delle prestazioni mancante.");
        Need(invoice.Lines.All(row => !string.IsNullOrWhiteSpace(row.Description) && row.Quantity > 0 && row.UnitPrice >= 0 && row.Amount >= 0 && row.VatRate is >= 0 and <= 100), "Una o più righe articolo non sono complete o contengono valori non validi.");
        Need(vatLines.Count > 0, "La fattura non contiene righe imponibili.");
        Need(!string.IsNullOrWhiteSpace(customer.SdiCode) || string.IsNullOrWhiteSpace(customer.CertifiedEmail) || MailAddress.TryCreate(customer.CertifiedEmail, out _), "PEC cliente non valida.");
    }

    private static XDocument BuildXml(SalesInvoiceEditResult invoice, CustomerEditModel customer, IReadOnlyDictionary<string, string> options, string progressive, IReadOnlyList<VatSummary> summaries, decimal taxable, decimal vat, decimal total)
    {
        XNamespace ns = "http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2";
        var country = NormalizeLetters(Get(options, "SiglaStato"), 2);
        var companyVat = Digits(Get(options, "PartitaIva"), 11);
        var customerSdi = (customer.SdiCode ?? "").Trim().ToUpperInvariant();
        if (customerSdi.Length is not (0 or 6 or 7) || customerSdi.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new InvalidOperationException("Codice SDI del cliente non valido: deve contenere 6 o 7 caratteri alfanumerici.");
        var destinationCode = customerSdi.Length == 0 ? "0000000" : customerSdi;
        var format = destinationCode.Length == 6 ? "FPA12" : "FPR12";
        var version = format;
        var senderId = new XElement(ns + "IdFiscaleIVA", new XElement(ns + "IdPaese", country), new XElement(ns + "IdCodice", companyVat));
        var sellerDetails = new XElement(ns + "DatiAnagrafici", senderId);
        var sellerFiscalCode = NormalizeCode(Get(options, "CodiceFiscale"), 16);
        if (sellerFiscalCode.Length > 0) sellerDetails.Add(new XElement(ns + "CodiceFiscale", sellerFiscalCode));
        sellerDetails.Add(new XElement(ns + "Anagrafica", new XElement(ns + "Denominazione", Get(options, "RagioneSociale"))));
        sellerDetails.Add(new XElement(ns + "RegimeFiscale", Get(options, "FeRegimeFiscale")));

        var sellerHeader = new XElement(ns + "CedentePrestatore", sellerDetails,
            new XElement(ns + "Sede", AddressElements(ns, Get(options, "SedeLegaleIndirizzo"), Get(options, "SedeLegaleCap"), Get(options, "SedeLegaleCitta"), Get(options, "SedeLegaleProvincia"), country)));
        var buyerDetails = new XElement(ns + "DatiAnagrafici");
        var customerVat = Digits(customer.VatNumber, 11);
        if (customerVat.Length > 0) buyerDetails.Add(new XElement(ns + "IdFiscaleIVA", new XElement(ns + "IdPaese", "IT"), new XElement(ns + "IdCodice", customerVat)));
        var customerFiscalCode = NormalizeCode(customer.TaxCode, 16);
        if (customerFiscalCode.Length > 0) buyerDetails.Add(new XElement(ns + "CodiceFiscale", customerFiscalCode));
        buyerDetails.Add(new XElement(ns + "Anagrafica", new XElement(ns + "Denominazione", customer.Name)));
        var buyerHeader = new XElement(ns + "CessionarioCommittente", buyerDetails,
            new XElement(ns + "Sede", AddressElements(ns, JoinStreet(customer.Street, customer.StreetNumber), customer.PostalCode, customer.City, customer.Province, "IT")));

        var transport = new XElement(ns + "DatiTrasmissione",
            new XElement(ns + "IdTrasmittente", new XElement(ns + "IdPaese", country), new XElement(ns + "IdCodice", companyVat)),
            new XElement(ns + "ProgressivoInvio", progressive),
            new XElement(ns + "FormatoTrasmissione", format),
            new XElement(ns + "CodiceDestinatario", destinationCode));
        if (destinationCode == "0000000" && !string.IsNullOrWhiteSpace(customer.CertifiedEmail))
            transport.Add(new XElement(ns + "PECDestinatario", customer.CertifiedEmail.Trim()));

        var type = invoice.Cause switch { 31 => "TD05", 32 => "TD04", _ => "TD01" };
        var generalDocument = new XElement(ns + "DatiGeneraliDocumento",
            new XElement(ns + "TipoDocumento", type),
            new XElement(ns + "Divisa", "EUR"),
            new XElement(ns + "Data", invoice.DocumentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(ns + "Numero", invoice.DocumentNumber.Trim()),
            new XElement(ns + "ImportoTotaleDocumento", Format(total)));
        if (!string.IsNullOrWhiteSpace(invoice.Activity)) generalDocument.Add(new XElement(ns + "Causale", invoice.Activity.Trim()));
        var details = new XElement(ns + "DatiBeniServizi");
        var lineNumber = 1;
        if (invoice.WorkAmount > 0)
        {
            details.Add(new XElement(ns + "DettaglioLinee",
                new XElement(ns + "NumeroLinea", lineNumber++),
                new XElement(ns + "Descrizione", invoice.Activity.Trim()),
                new XElement(ns + "Quantita", FormatQuantity(1m)),
                new XElement(ns + "PrezzoUnitario", Format(invoice.WorkAmount)),
                new XElement(ns + "PrezzoTotale", Format(invoice.WorkAmount)),
                new XElement(ns + "AliquotaIVA", Format(invoice.WorkVatRate))));
        }
        foreach (var line in invoice.Lines)
        {
            var detail = new XElement(ns + "DettaglioLinee",
                new XElement(ns + "NumeroLinea", lineNumber++));
            if (!string.IsNullOrWhiteSpace(line.ArticleCode)) detail.Add(new XElement(ns + "CodiceArticolo", new XElement(ns + "CodiceTipo", "INTERNO"), new XElement(ns + "CodiceValore", line.ArticleCode)));
            detail.Add(new XElement(ns + "Descrizione", line.Description.Trim()));
            detail.Add(new XElement(ns + "Quantita", FormatQuantity(line.Quantity)));
            if (!string.IsNullOrWhiteSpace(line.Unit)) detail.Add(new XElement(ns + "UnitaMisura", line.Unit.Trim()));
            detail.Add(new XElement(ns + "PrezzoUnitario", Format(line.UnitPrice)));
            if (line.Discount > 0) detail.Add(new XElement(ns + "ScontoMaggiorazione", new XElement(ns + "Tipo", "SC"), new XElement(ns + "Percentuale", Format(line.Discount))));
            detail.Add(new XElement(ns + "PrezzoTotale", Format(line.Amount)));
            detail.Add(new XElement(ns + "AliquotaIVA", Format(line.VatRate)));
            details.Add(detail);
        }
        foreach (var summary in summaries)
        {
            var summaryElement = new XElement(ns + "DatiRiepilogo",
                new XElement(ns + "AliquotaIVA", Format(summary.Rate)));
            if (summary.Rate == 0) summaryElement.Add(new XElement(ns + "Natura", summary.Nature));
            summaryElement.Add(new XElement(ns + "ImponibileImporto", Format(summary.Amount)));
            summaryElement.Add(new XElement(ns + "Imposta", Format(summary.Tax)));
            summaryElement.Add(new XElement(ns + "EsigibilitaIVA", "I"));
            details.Add(summaryElement);
        }

        var root = new XElement(ns + "FatturaElettronica", new XAttribute("versione", version),
            new XElement(ns + "FatturaElettronicaHeader", transport, sellerHeader, buyerHeader),
            new XElement(ns + "FatturaElettronicaBody",
                new XElement(ns + "DatiGenerali", generalDocument),
                details));
        _ = taxable;
        _ = vat;
        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private static object[] AddressElements(XNamespace ns, string street, string postalCode, string city, string province, string country)
    {
        var elements = new List<object>
        {
            new XElement(ns + "Indirizzo", street.Trim()),
            new XElement(ns + "CAP", Digits(postalCode, 5)),
            new XElement(ns + "Comune", city.Trim())
        };
        var normalizedProvince = NormalizeLetters(province, 2);
        if (normalizedProvince.Length == 2) elements.Add(new XElement(ns + "Provincia", normalizedProvince));
        elements.Add(new XElement(ns + "Nazione", country));
        return elements.ToArray();
    }

    private static string JoinStreet(string? street, string? number) => string.Join(" ", new[] { street?.Trim(), number?.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private async Task SendSmtpAsync(IReadOnlyDictionary<string, string> options, string recipient, string fileName, byte[] attachment, CancellationToken ct)
    {
        var server = Get(options, "MailPecServerSmtp").Trim();
        var from = Get(options, "MailPecEmailMittente").Trim();
        if (server.Length == 0 || !MailAddress.TryCreate(from, out _)) throw new InvalidOperationException("Mittente PEC o server SMTP non configurato correttamente.");
        if (!MailAddress.TryCreate(recipient, out _)) throw new InvalidOperationException("Indirizzo PEC SDI non valido.");
        if (!int.TryParse(Get(options, "MailPecPortaSmtp"), out var port) || port is < 1 or > 65535) throw new InvalidOperationException("Porta SMTP PEC non valida.");
        var security = Get(options, "MailPecSicurezza").Trim();
        if (security is not ("1" or "2" or "3")) throw new InvalidOperationException("Configurare la sicurezza SMTP PEC in Opzioni.");
        var authenticate = Get(options, "MailPecAutenticazione") == "1";
        var username = Get(options, "MailPecUsername");
        var password = Get(options, "MailPecPassword");
        if (authenticate && (username.Length == 0 || password.Length == 0)) throw new InvalidOperationException("Credenziali SMTP PEC mancanti.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        using var client = new TcpClient();
        await client.ConnectAsync(server, port, timeout.Token);
        Stream stream = client.GetStream();
        SslStream? secureStream = null;
        if (security == "3")
        {
            secureStream = new SslStream(stream, false);
            await secureStream.AuthenticateAsClientAsync(server);
            stream = secureStream;
        }

        StreamReader reader = new(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
        StreamWriter writer = new(stream, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        try
        {
            await ExpectSmtpAsync(reader, "220", timeout.Token);
            await SendCommandAsync(reader, writer, "EHLO skylab.local", "250", timeout.Token);
            if (security == "2")
            {
                await SendCommandAsync(reader, writer, "STARTTLS", "220", timeout.Token);
                reader.Dispose(); writer.Dispose();
                secureStream = new SslStream(stream, false);
                await secureStream.AuthenticateAsClientAsync(server);
                stream = secureStream;
                reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
                await SendCommandAsync(reader, writer, "EHLO skylab.local", "250", timeout.Token);
            }
            if (authenticate)
            {
                await SendCommandAsync(reader, writer, "AUTH LOGIN", "334", timeout.Token);
                await SendCommandAsync(reader, writer, Convert.ToBase64String(Encoding.UTF8.GetBytes(username)), "334", timeout.Token);
                await SendCommandAsync(reader, writer, Convert.ToBase64String(Encoding.UTF8.GetBytes(password)), "235", timeout.Token);
            }
            await SendCommandAsync(reader, writer, $"MAIL FROM:<{from}>", "250", timeout.Token);
            await SendCommandAsync(reader, writer, $"RCPT TO:<{recipient}>", "250", timeout.Token);
            await SendCommandAsync(reader, writer, "DATA", "354", timeout.Token);
            var boundary = $"SkyLab_{Guid.NewGuid():N}";
            var subject = EncodeHeader("Invio fattura elettronica");
            var mime = new StringBuilder()
                .Append("From: <").Append(from).Append(">\r\nTo: <").Append(recipient).Append(">\r\nSubject: ").Append(subject)
                .Append("\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"").Append(boundary).Append("\"\r\n\r\n")
                .Append("--").Append(boundary).Append("\r\nContent-Type: text/plain; charset=UTF-8\r\nContent-Transfer-Encoding: base64\r\n\r\n")
                .Append(WrapBase64(Encoding.UTF8.GetBytes("Si allega la fattura elettronica in formato XML."))).Append("\r\n--")
                .Append(boundary).Append("\r\nContent-Type: application/xml; name=\"").Append(fileName).Append("\"\r\nContent-Transfer-Encoding: base64\r\nContent-Disposition: attachment; filename=\"").Append(fileName).Append("\"\r\n\r\n")
                .Append(WrapBase64(attachment)).Append("\r\n--").Append(boundary).Append("--\r\n").ToString();
            foreach (var line in mime.Split("\r\n"))
                await writer.WriteLineAsync(line.StartsWith('.') ? "." + line : line);
            await writer.WriteLineAsync(".");
            await writer.FlushAsync();
            await ExpectSmtpAsync(reader, "250", timeout.Token);
            try { await writer.WriteLineAsync("QUIT"); } catch { }
        }
        finally
        {
            reader.Dispose(); writer.Dispose(); secureStream?.Dispose();
        }
    }

    private static async Task SendCommandAsync(StreamReader reader, StreamWriter writer, string command, string expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await writer.WriteLineAsync(command);
        await writer.FlushAsync();
        await ExpectSmtpAsync(reader, expected, ct);
    }

    private static async Task ExpectSmtpAsync(StreamReader reader, string expected, CancellationToken ct)
    {
        string line;
        do
        {
            ct.ThrowIfCancellationRequested();
            line = await reader.ReadLineAsync(ct) ?? throw new InvalidOperationException("Il server SMTP ha chiuso la connessione.");
            if (line.Length < 3 || !line.StartsWith(expected, StringComparison.Ordinal))
                throw new InvalidOperationException($"Il server SMTP ha risposto: {line}");
        } while (line.Length >= 4 && line[3] == '-');
    }

    private static string EncodeHeader(string value) => $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";
    private static string WrapBase64(byte[] bytes) => string.Join("\r\n", Convert.ToBase64String(bytes).Chunk(76).Select(chars => new string(chars)));

    private static async Task<(string Serial, string Progressive)> ReserveOptionRowsAsync(MySqlConnection connection, MySqlTransaction transaction, CancellationToken ct)
    {
        foreach (var key in new[] { "FeUltimoSerialeNomeFile", "FeUltimoProgressivoInvioXml" })
        {
            await using var ensure = new MySqlCommand("INSERT INTO opzioni(Chiave,Valore) VALUES(@key,'') ON DUPLICATE KEY UPDATE Chiave=VALUES(Chiave);", connection, transaction);
            ensure.Parameters.AddWithValue("@key", key);
            await ensure.ExecuteNonQueryAsync(ct);
        }
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new MySqlCommand("SELECT Chiave,COALESCE(Valore,'') FROM opzioni WHERE Chiave IN ('FeUltimoSerialeNomeFile','FeUltimoProgressivoInvioXml') FOR UPDATE;", connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) values[reader.GetString(0)] = reader.GetString(1);
        return (values.GetValueOrDefault("FeUltimoSerialeNomeFile", ""), values.GetValueOrDefault("FeUltimoProgressivoInvioXml", ""));
    }

    private static async Task SaveOptionAsync(MySqlConnection connection, MySqlTransaction transaction, string key, string value, CancellationToken ct)
    {
        await using var command = new MySqlCommand("INSERT INTO opzioni(Chiave,Valore) VALUES(@key,@value) ON DUPLICATE KEY UPDATE Valore=VALUES(Valore);", connection, transaction);
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static ulong ParseLastSerial(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0) return 0;
        if (normalized.Length > 8 || normalized.Any(ch => !char.IsDigit(ch))
            || !ulong.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
            || result > MaxFileSerial)
            throw new InvalidOperationException("Ultimo seriale nome file non valido: inserire un valore numerico compatibile con 5 caratteri Base36.");
        return result;
    }

    private static string IncrementProgressive(string value)
    {
        var current = value.Trim().ToUpperInvariant();
        if (current.Length == 0) return "1";
        if (current.Length > 10 || current.Any(ch => !Base36Alphabet.Contains(ch))) throw new InvalidOperationException("Ultimo ProgressivoInvio XML non valido: sono ammessi al massimo 10 caratteri alfanumerici.");
        if (current.All(char.IsDigit))
        {
            if (!ulong.TryParse(current, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number >= 9_999_999_999UL)
                throw new InvalidOperationException("ProgressivoInvio XML esaurito.");
            return (number + 1).ToString(CultureInfo.InvariantCulture);
        }
        return IncrementBase36(current);
    }

    private static string IncrementBase36(string value)
    {
        var chars = value.ToCharArray();
        for (var index = chars.Length - 1; index >= 0; index--)
        {
            var digit = Base36Alphabet.IndexOf(chars[index]);
            if (digit < 35) { chars[index] = Base36Alphabet[digit + 1]; return new string(chars); }
            chars[index] = '0';
        }
        if (chars.Length >= 10) throw new InvalidOperationException("ProgressivoInvio XML esaurito.");
        return "1" + new string(chars);
    }

    private static string ToBase36(ulong value)
    {
        if (value == 0) return "0";
        var result = new StringBuilder();
        while (value > 0) { result.Insert(0, Base36Alphabet[(int)(value % 36)]); value /= 36; }
        return result.ToString();
    }

    private static string Get(IReadOnlyDictionary<string, string> options, string key) => options.GetValueOrDefault(key, "").Trim();
    private static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string FormatQuantity(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string Digits(string? value, int maxLength) => new((value ?? "").Where(char.IsDigit).Take(maxLength).ToArray());
    private static string NormalizeLetters(string? value, int maxLength) => new((value ?? "").Trim().ToUpperInvariant().Where(char.IsLetter).Take(maxLength).ToArray());
    private static string NormalizeCode(string? value, int maxLength) => new((value ?? "").Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).Take(maxLength).ToArray());
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed record VatSummary(decimal Rate, string Nature, decimal Amount, decimal Tax);
    private sealed record VatCodeInfo(string Nature);
}
