using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SkyLab.Web.Pages.Magazzino.CaricoAcquisti;

public sealed class IndexModel : PageModel
{
    public IReadOnlyList<PurchaseLoadPreview> Documents { get; } =
    [
        new(1, 2026, 124, "FT 318", new DateOnly(2026, 9, 12), 27, "Forniture Tecniche Italia", 1840.00m, 404.80m, 2244.80m, "IT027318_001.xml"),
        new(2, 2026, 123, "874/A", new DateOnly(2026, 9, 8), 14, "Ricambi Industriali Campania", 965.40m, 212.39m, 1177.79m, "IT014874_A.xml.p7m"),
        new(3, 2026, 122, "DDT 551", new DateOnly(2026, 9, 2), 41, "Componenti e Servizi", 438.00m, 96.36m, 534.36m, string.Empty)
    ];

    public IReadOnlyList<PurchaseLoadDetailPreview> Details { get; } =
    [
        new(1, "ADDOLCITORE.CLACK.20", "Addolcitore 20 LT di resina con bombola", "PZ", 2m, 610m, 0m, 1220m, 22m),
        new(1, "SALE25", "Sale in pastiglie sacco 25 KG", "SC", 20m, 18.50m, 0m, 370m, 22m),
        new(1, "RACC-34", "Raccordo rapido 3/4", "PZ", 25m, 10m, 0m, 250m, 22m),
        new(2, "RIC-IND-01", "Ricambio industriale standard", "PZ", 3m, 215m, 0m, 645m, 22m),
        new(2, "MAN-TEC", "Manutenzione tecnica accessoria", "H", 4m, 80.10m, 0m, 320.40m, 22m),
        new(3, "COMP-551", "Componente di servizio DDT 551", "PZ", 6m, 73m, 0m, 438m, 22m)
    ];

    public void OnGet() { }

    public sealed record PurchaseLoadPreview(int Id, int Year, int Batch, string Number, DateOnly Date, int SupplierCode, string SupplierName, decimal Goods, decimal Vat, decimal Total, string ElectronicInvoice);
    public sealed record PurchaseLoadDetailPreview(int DocumentId, string Article, string Description, string Unit, decimal Quantity, decimal Price, decimal Discount, decimal Amount, decimal VatRate);
}
