using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class SchedaModel(WorkService service,IWebHostEnvironment environment) : PageModel
{
    [BindProperty] public WorkEditModel Lavoro { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int Azione { get; set; } = 3;
    public IReadOnlyList<WorkLookupItem> Stati { get; private set; }=[];
    public IReadOnlyList<WorkLookupItem> Esiti { get; private set; }=[];
    public IReadOnlyList<OperatorLookupItem> Operatori { get; private set; }=[];
    public IReadOnlyList<WorkSiteLookupItem> Sedi { get; private set; }=[];
    public IReadOnlyList<WorkDetailItem> RighePreventivo { get; private set; }=[];
    public IReadOnlyList<WorkDetailItem> RigheConsuntivo { get; private set; }=[];
    public IReadOnlyList<WorkReferenceLookup> Riferimenti { get; private set; }=[];
    public IReadOnlyList<WorkPhotoItem> Fotografie { get; private set; }=[];
    public IReadOnlyList<WorkDocumentItem> Documenti { get; private set; }=[];
    public IReadOnlyList<WorkHistoryItem> Storico { get; private set; }=[];
    public bool PreventivoBloccato => Lavoro.StatusId >= 3;

    public async Task<IActionResult> OnGetAsync(int id,CancellationToken ct)
    {
        var item=await service.WorkAsync(id,ct); if(item is null) return NotFound();
        Lavoro=item; await Lookups(ct); RighePreventivo=await service.PlannedDetailsAsync(id,ct); RigheConsuntivo=await service.ActualDetailsAsync(id,ct); Riferimenti=await service.WorkReferencesAsync(ct); Fotografie=await service.PhotosAsync(id,ct); Documenti=await service.DocumentsAsync(id,ct); Storico=await service.HistoryAsync(id,ct); return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if(Lavoro.Id<=0) return BadRequest();
        var precedente=await service.WorkAsync(Lavoro.Id,ct);
        if(precedente is null) return NotFound();
        Lavoro.PlannedLabour=precedente.PlannedLabour;
        Lavoro.PlannedMaterials=precedente.PlannedMaterials;
        Lavoro.ActualLabour=precedente.ActualLabour;
        Lavoro.ActualMaterials=precedente.ActualMaterials;
        await service.SaveAsync(Lavoro,ct);
        return Azione == 103
            ? RedirectToPage("/Interventi/Index")
            : RedirectToPage("Schede");
    }
    private async Task Lookups(CancellationToken ct)
    { Stati=await service.StatusesAsync(ct);Esiti=await service.OutcomesAsync(ct);Operatori=await service.OperatorsAsync(ct);Sedi=await service.WorkSitesAsync(Lavoro.CustomerId,ct); }

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

    public async Task<IActionResult> OnPostAddPhotoAsync(int id,IFormFile? foto,string? descrizione,CancellationToken ct)
    {
        if(foto is null||foto.Length==0)return Redirect($"/Lavori/Scheda?id={id}#documentazione");
        if(foto.Length>15*1024*1024)throw new InvalidOperationException("La fotografia supera 15 MB.");
        var extension=Path.GetExtension(foto.FileName).ToLowerInvariant();if(extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))throw new InvalidOperationException("Formato immagine non valido.");
        var directory=Path.Combine(environment.WebRootPath,"uploads","lavori",id.ToString());Directory.CreateDirectory(directory);
        var fileName=$"{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}";var path=Path.Combine(directory,fileName);
        await using(var stream=System.IO.File.Create(path))await foto.CopyToAsync(stream,ct);
        try{await service.AddPhotoAsync(id,fileName,descrizione??"",ct);}catch{System.IO.File.Delete(path);throw;}
        return Redirect($"/Lavori/Scheda?id={id}#documentazione");
    }

    public async Task<IActionResult> OnPostDeletePhotoAsync(int id,short numero,CancellationToken ct)
    {
        var file=await service.DeletePhotoAsync(id,numero,ct);
        if(!string.IsNullOrWhiteSpace(file)&&Path.GetFileName(file)==file){var path=Path.Combine(environment.WebRootPath,"uploads","lavori",id.ToString(),file);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
        return Redirect($"/Lavori/Scheda?id={id}#documentazione");
    }

    public async Task<IActionResult> OnPostAddDocumentAsync(int id,IFormFile? documento,string? descrizione,CancellationToken ct)
    {
        if(documento is null||documento.Length==0)return Redirect($"/Lavori/Scheda?id={id}#documentazione");
        if(documento.Length>25*1024*1024)throw new InvalidOperationException("Il documento supera 25 MB.");
        var extension=Path.GetExtension(documento.FileName).ToLowerInvariant();if(extension is not (".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".txt"))throw new InvalidOperationException("Formato documento non valido.");
        var directory=Path.Combine(environment.WebRootPath,"uploads","lavori",id.ToString(),"documenti");Directory.CreateDirectory(directory);
        var fileName=$"{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}";var path=Path.Combine(directory,fileName);
        await using(var stream=System.IO.File.Create(path))await documento.CopyToAsync(stream,ct);
        var originalName=Path.GetFileName(documento.FileName);
        try{await service.AddDocumentAsync(id,fileName,originalName,descrizione??"",ct);}catch{System.IO.File.Delete(path);throw;}
        return Redirect($"/Lavori/Scheda?id={id}#documentazione");
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(int id,short numero,CancellationToken ct)
    {
        var file=await service.DeleteDocumentAsync(id,numero,ct);
        if(!string.IsNullOrWhiteSpace(file)&&Path.GetFileName(file)==file){var path=Path.Combine(environment.WebRootPath,"uploads","lavori",id.ToString(),"documenti",file);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
        return Redirect($"/Lavori/Scheda?id={id}#documentazione");
    }
}
