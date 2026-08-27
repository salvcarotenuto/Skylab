using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public sealed class InterventionService
{
    private readonly List<Intervention> _items =
    [
        new() { Id=1001, Customer="Ristorante Il Glicine", Site="Via Roma 18, Catania", Plant="Addolcitore AQ-500 · SN 18422", ScheduledAt=DateTime.Today.AddHours(9), Kind="Manutenzione programmata", Status=InterventionStatus.InProgress },
        new() { Id=1002, Customer="Bar Centrale", Site="Piazza Duomo 4, Acireale", Plant="Osmosi RO-200 · SN 22901", ScheduledAt=DateTime.Today.AddHours(11.5), Kind="Sostituzione filtri", Status=InterventionStatus.Planned },
        new() { Id=1003, Customer="Hotel Aurora", Site="Via Etnea 82, Catania", Plant="Addolcitore AQ-900 · SN 11987", ScheduledAt=DateTime.Today.AddHours(15), Kind="Controllo durezza", Status=InterventionStatus.Planned }
    ];

    public IReadOnlyList<Intervention> All() => _items.OrderBy(x => x.ScheduledAt).ToList();
    public Intervention? Find(int id) => _items.SingleOrDefault(x => x.Id == id);
    public bool AddMaterial(int id, string barcode, decimal quantity)
    {
        var item=Find(id); if(item is null || string.IsNullOrWhiteSpace(barcode) || quantity<=0) return false;
        var descriptions = new Dictionary<string,string>{{"800001","Filtro sedimenti 10\""},{"800002","Cartuccia carbone attivo"},{"800003","Sale in pastiglie 25 kg"}};
        item.Materials.Add(new(barcode.Trim(), descriptions.GetValueOrDefault(barcode.Trim(), "Articolo da verificare"), quantity));
        item.Status=InterventionStatus.InProgress; return true;
    }
    public bool Complete(int id, string notes)
    { var item=Find(id); if(item is null) return false; item.Notes=notes?.Trim()??""; item.Status=InterventionStatus.Completed; return true; }
}
