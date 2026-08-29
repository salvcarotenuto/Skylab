using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public static class MainMenuCatalog
{
    public static IReadOnlyList<QuickLinkDefinition> QuickLinks { get; } =
    [
        new("Interventi in scadenza", "IS", "/Lavori/Pianificazione"),
        new("Agenda del giorno", "AG", "/Interventi/Index"),
        new("Schede lavori", "LV", "/Lavori/Schede"),
        new("Clienti", "CL", "/Clienti/Index"),
        new("Articoli", "AR", "/Magazzino/Articoli/Index")
    ];

    public static IReadOnlyList<MenuSectionDefinition> Sections { get; } =
    [
        new("soggetti", "Soggetti", "SG", "accent-tables",
        [
            Group("Anagrafiche", Link("Clienti", "/Clienti/Index"), Pending("Fornitori"), Pending("Agenti"), Pending("Vettori"), Pending("Banche"), Pending("Utenti")),
            Group("Aggiornamenti", "Aggiornamento piano dei conti", "Aggiornamento tabella ditte")
        ]),
        new("tabelle", "Tabelle", "TB", "accent-tables",
        [
            Group("Magazzino", "Categorie", "Gruppi", "Marchi", "Unità di misura", "Aspetto beni", "Causali di movimento"),
            Group("Contabili", "Aliquote IVA", "Codici di pagamento", "Tipi di pagamento", "Piano dei conti", "Causali contabili"),
            Group("Altre", "Attività", "Comuni", "Distretti territoriali", "Nazioni")
        ]),
        new("magazzino", "Magazzino", "MG", "accent-stock",
        [
            Group("Articoli", Link("Lista articoli", "/Magazzino/Articoli/Index"), Pending("Carico da inventario"), Pending("Generazione automatica listini"), Pending("Importazione listino fornitore"), Pending("Stampa listino"), Pending("Stampa etichette")),
            Group("Movimenti", "Registrazione movimenti di carico", "Estratto conto articolo", "Lista movimenti di magazzino", "Lista movimenti per raggruppamento", "Ordini a fornitori"),
            Group("Statistiche", "Inventario di magazzino", "Articoli sotto scorta", "Statistiche di vendita")
        ]),
        new("lavorazione", "Lavorazione", "LV", "accent-employees",
        [
            Group("Pianificazione",
                Link("Interventi in scadenza", "/Lavori/Pianificazione"),
                Link("Agenda del giorno", "/Interventi/Index"),
                Link("Agenda lavori", "/Interventi/Index"),
                Link("Macchine installate", "/Lavori/MacchineInstallate")),
            Group("Esecuzione",
                Link("Schede lavori", "/Lavori/Schede"),
                Link("Lavori su dispositivo mobile", "/Interventi/Index"),
                Pending("Scheda di revisione"),
                Pending("Lavoro di revisione"))
        ]),
        new("vendita", "Vendita", "VN", "accent-sales",
        [
            Group("Documenti di vendita", "Documento di trasporto", "Fattura di vendita", "Fattura differita", "Visualizza fattura elettronica", "Ricevuta fiscale", "Preventivo di vendita"),
            Group("Vendita al dettaglio", "Vendita al banco", "Rendiconto vendite", "Buono acquisto")
        ]),
        new("contabilita", "Contabilità", "CN", "accent-accounting",
        [
            Group("Movimenti IVA", "Fatture di acquisto", "Fatture di vendita", "Vendite per corrispettivi", "Visualizza fattura elettronica"),
            Group("Movimenti contabili", "Movimenti di prima nota", "Incasso da clienti", "Pagamento fornitori", "Visualizza conto", "Stampa lista movimenti"),
            Group("Clienti e fornitori", "Scadenze attive", "Scadenze passive", "Estratto conto clienti", "Estratto conto fornitori", "Estratto conto clienti per partite", "Estratto conto fornitori per partite", "Saldi clienti", "Saldi fornitori"),
            Group("Banche e titoli", "Titoli di credito", "Estratto conto banca")
        ]),
        new("strumenti", "Strumenti", "ST", "accent-tools",
        [
            Group("Applicazione", "Opzioni azienda", "Cambia utente", "Opzioni utenti", "Parametri applicazione", "Note di aggiornamento", "Info applicazione"),
            Group("Archivio", "Cambia azienda", "Cambia esercizio", "Copie di sicurezza", "Ripristino copie di sicurezza", "Configurazione server", "Recupero archivio", "Aggiornamento database"),
            Group("Lavori di servizio", "Assistenza remota", "Attività utenti", "Importazione dati SQL Server", "Importazione dati OleDb", "Importazione dati CSV", "Esportazione dati CSV", "Azzeramento tabelle")
        ])
    ];

    private static MenuGroupDefinition Group(string title, params string[] labels) =>
        new(title, labels.Select(Pending).ToArray());

    private static MenuGroupDefinition Group(string title, params MenuItemDefinition[] items) =>
        new(title, items);

    private static MenuItemDefinition Link(string label, string page) => new(label, page);
    private static MenuItemDefinition Pending(string label) => new(label);
}
