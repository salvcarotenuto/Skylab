using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public static class MainMenuCatalog
{
    public static IReadOnlyList<QuickLinkDefinition> QuickLinks { get; } =
    [
        new("Clienti", "CL", "/Clienti/Index"),
        new("Articoli", "AR", "/Magazzino/Articoli/Index"),
        new("Interventi in scadenza", "IS", "/Lavori/Pianificazione"),
        new("Schede lavori", "LV", "/Lavori/Schede"),
        new("Agenda lavori", "AG", "/Interventi/Index")
    ];

    public static IReadOnlyList<MenuSectionDefinition> Sections { get; } =
    [
        new("soggetti", "Soggetti", "SG", "accent-tables",
        [
            Group("Anagrafiche", Link("Clienti", "/Clienti/Index"), Link("Fornitori", "/Fornitori/Index"), Pending("Agenti"), Pending("Vettori"), Pending("Banche"), Link("Utenti", "/Utenti/Index"))
        ]),
        new("tabelle", "Tabelle", "TB", "accent-tables",
        [
            Group("Magazzino", Link("Categorie", "/Tabelle/Categorie/Index"), Link("Gruppi", "/Tabelle/Gruppi/Index"), Link("Marchi", "/Tabelle/Marchi/Index"), Link("Unità di misura", "/Tabelle/Unita/Index"), Link("Aspetto beni", "/Tabelle/Aspetto/Index")),
            Group("Contabili", Link("Aliquote IVA", "/Tabelle/AliquoteIva/Index"), Link("Causali contabili", "/Tabelle/CausaliContabili/Index"), Link("Mastri di conto", "/Tabelle/Mastri/Index"), Link("Piano dei conti", "/Tabelle/PianoConti/Index"), Pending("Tipi di pagamento"), Pending("Codici di pagamento")),
            Group("Altre", "Attività", "Comuni", "Distretti territoriali", "Nazioni")
        ]),
        new("magazzino", "Magazzino", "MG", "accent-stock",
        [
            Group("Articoli", Link("Lista articoli", "/Magazzino/Articoli/Index"), Pending("Carico da inventario"), Pending("Stampa listino"), Pending("Stampa etichette"), Pending("Inventario di magazzino"), Pending("Articoli sotto scorta")),
            Group("Movimenti", Link("Carico per acquisti", "/Magazzino/CaricoAcquisti/Index"), Pending("Estratto conto articolo"), Pending("Lista movimenti di magazzino"), Pending("Lista movimenti per raggruppamento"), Pending("Ordini a fornitori"))
        ]),
        new("lavorazione", "Lavorazione", "LV", "accent-employees",
        [
            Group("Pianificazione",
                Link("Interventi in scadenza", "/Lavori/Pianificazione"),
                Link("Intervento straordinario", "/Lavori/InterventoStraordinario"),
                Link("Nuovo lavoro", "/Lavori/NuovoLavoro"),
                Pending("Distinta base"),
                Link("Macchine installate", "/Lavori/MacchineInstallate")),
            Group("Esecuzione",
                Link("Schede lavori", "/Lavori/Schede"),
                Link("Agenda lavori", "/Interventi/Index"),
                Pending("Produzione interna"),
                Link("Lavori su dispositivo mobile", "/Interventi/Index"),
                Pending("Scheda di revisione"),
                Pending("Lavoro di revisione"))
        ]),
        new("acquisti", "Acquisti", "AQ", "accent-stock",
        [
            Group("Documenti di acquisto", Link("Fatture di acquisto", "/FattureAcquisto/Index"), Link("Caricamento fatture elettroniche", "/CaricamentoFeAcquisti/Index"), Pending("Visualizza fattura elettronica"))
        ]),
        new("vendita", "Vendite", "VN", "accent-sales",
        [
            Group("Documenti di vendita", Pending("Documento di trasporto"), Link("Fattura di vendita", "/FattureVendita/Index"), Pending("Ricevuta fiscale"), Pending("Preventivo di vendita")),
            Group("Statistiche di vendita", "Rendiconto vendite", "Statistiche di vendita")
        ]),
        new("contabilita", "Contabilità", "CN", "accent-accounting",
        [
            Group("Prima nota contabile", Link("Lista movimenti di prima nota", "/PrimaNota/Index"), Link("Movimenti di prima nota", "/PrimaNota/Edit", "menu", FormAzione.InserimentoContinuativo), Pending("Scadenze passive"), Link("Saldi iniziali clienti e fornitori", "/SaldoInizialeClientiFornitori/Index"), Link("Apertura conti patrimoniali", "/AperturaContiPatrimoniali/Index")),
            Group("Situazione economica", Link("Estratto conto fornitori", "/EstrattoContoClientiFornitori/Index"), Link("Estratto conto clienti", "/EstrattoContoClientiFornitori/Index"), Link("Scheda contabile", "/SchedaContabile/Index"), Link("Saldi clienti e fornitori", "/SaldiClientiFornitori/Index"), Pending("Liquidazione periodica IVA"), Link("Bilancio di verifica", "/BilancioVerifica/Index"), Link("Rendiconto cassa", "/RendicontoCassa/Index"), Link("Riepilogo movimenti contabili", "/RiepilogoMovimentiContabili/Index"), Link("Riepilogo movimenti per punto vendita", "/RiepilogoMovimentiPuntiVendita/Index")),
            Group("Banche e titoli", Link("Movimenti di banca", "/MovimentiBanca/Index"), Pending("Titoli di credito"), Pending("Estratto conto banca"))
        ]),
        new("strumenti", "Strumenti", "ST", "accent-tools",
        [
            Group("Applicazione", Link("Opzioni azienda", "/Opzioni/Index"), Pending("Cambia azienda"), Link("Cambia esercizio", "/CambiaEsercizio/Index"), Pending("Cambia utente")),
            Group("Archivio", "Copie di sicurezza", "Ripristino copie di sicurezza", "Elimina movimenti per anno"),
            Group("Lavori di servizio", "Attività utenti")
        ])
    ];

    private static MenuGroupDefinition Group(string title, params string[] labels) =>
        new(title, labels.Select(Pending).ToArray());

    private static MenuGroupDefinition Group(string title, params MenuItemDefinition[] items) =>
        new(title, items);

    private static MenuItemDefinition Link(string label, string page, string? returnTo = null, int? azione = null) => new(label, page, returnTo, azione);
    private static MenuItemDefinition Pending(string label) => new(label);
}
