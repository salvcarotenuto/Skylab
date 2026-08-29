using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class SchedaModel(WorkService service) : PageModel
{
    [BindProperty] public WorkEditModel Lavoro { get; set; } = new();
    public IReadOnlyList<WorkLookupItem> Stati { get; private set; }=[];
    public IReadOnlyList<WorkLookupItem> Esiti { get; private set; }=[];
    public IReadOnlyList<OperatorLookupItem> Operatori { get; private set; }=[];
    public IReadOnlyList<WorkDetailItem> RighePreventivo { get; private set; }=[];
    public IReadOnlyList<WorkDetailItem> RigheConsuntivo { get; private set; }=[];
    public IReadOnlyList<WorkReferenceLookup> Riferimenti { get; private set; }=[];
    public bool PreventivoBloccato => Lavoro.StatusId >= 3;

    public async Task<IActionResult> OnGetAsync(int id,CancellationToken ct)
    {
        var item=await service.WorkAsync(id,ct); if(item is null) return NotFound();
        Lavoro=item; await Lookups(ct); RighePreventivo=await service.PlannedDetailsAsync(id,ct); RigheConsuntivo=await service.ActualDetailsAsync(id,ct); Riferimenti=await service.WorkReferencesAsync(ct); return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if(Lavoro.Id<=0) return BadRequest();
        var precedente=await service.WorkAsync(Lavoro.Id,ct);
        if(precedente is null) return NotFound();
        if(precedente.StatusId>=3)
        {
            Lavoro.PlannedLabour=precedente.PlannedLabour;
            Lavoro.PlannedMaterials=precedente.PlannedMaterials;
        }
        await service.SaveAsync(Lavoro,ct);
        return RedirectToPage("Schede");
    }
    private async Task Lookups(CancellationToken ct)
    { Stati=await service.StatusesAsync(ct);Esiti=await service.OutcomesAsync(ct);Operatori=await service.OperatorsAsync(ct); }

    public async Task<IActionResult> OnPostAddDetailAsync(int id,string tipo,string riferimento,string ambito,CancellationToken ct)
    {
        if(ambito=="C")await service.AddActualDetailAsync(id,tipo,riferimento,ct);else await service.AddPlannedDetailAsync(id,tipo,riferimento,ct);
        return Redirect($"/Lavori/Scheda?id={id}#{(ambito=="C"?"consuntivo":"previsto")}");
    }
    public async Task<IActionResult> OnPostUpdateDetailAsync(int id,short riga,decimal quantita,decimal prezzo,string ambito,CancellationToken ct)
    {
        if(ambito=="C")await service.UpdateActualDetailAsync(id,riga,quantita,prezzo,ct);else await service.UpdatePlannedDetailAsync(id,riga,quantita,prezzo,ct);
        return Redirect($"/Lavori/Scheda?id={id}#{(ambito=="C"?"consuntivo":"previsto")}");
    }
    public async Task<IActionResult> OnPostDeleteDetailAsync(int id,short riga,string ambito,CancellationToken ct)
    {
        if(ambito=="C")await service.DeleteActualDetailAsync(id,riga,ct);else await service.DeletePlannedDetailAsync(id,riga,ct);
        return Redirect($"/Lavori/Scheda?id={id}#{(ambito=="C"?"consuntivo":"previsto")}");
    }
}
