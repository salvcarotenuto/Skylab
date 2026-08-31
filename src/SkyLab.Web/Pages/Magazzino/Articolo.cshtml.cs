using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Magazzino;

public sealed class ArticoloModel(CustomerService service,IWebHostEnvironment environment) : PageModel
{
    [BindProperty] public ArticleEditModel Articolo { get; set; } = new();
    [BindProperty] public bool IsNew { get; set; }
    public IReadOnlyList<LookupItem> Categorie { get; private set; } = [];
    public IReadOnlyList<LookupItem> Gruppi { get; private set; } = [];
    public IReadOnlyList<LookupItem> Marche { get; private set; } = [];
    public IReadOnlyList<CodeLookupItem> UnitaMisura { get; private set; } = [];
    public IReadOnlyList<CodeLookupItem> CodiciIva { get; private set; } = [];
    public IReadOnlyList<SupplierLookupItem> Fornitori { get; private set; } = [];
    public IReadOnlyList<ArticleBarcodeItem> Barcodes { get; private set; } = [];
    public IReadOnlyList<ArticlePhotoItem> Photos { get; private set; } = [];
    [BindProperty] public List<ArticlePriceListEditModel> PriceLists { get; set; } = [];
    public decimal VatRate { get; private set; }
    public string ActiveTab { get; private set; } = "anagrafica";
    [TempData] public string? BarcodeError { get; set; }
    [TempData] public string? ListError { get; set; }
    [TempData] public string? PhotoError { get; set; }
    public string NomeFornitore => Fornitori.FirstOrDefault(x=>x.Code==Articolo.SupplierCode)?.Name ?? "";

    public async Task<IActionResult> OnGetAsync(string? codice,bool nuovo=false,string? tab=null,CancellationToken ct=default)
    {
        IsNew=nuovo;ActiveTab=!nuovo&&tab is "barcode" or "listini" or "immagini"?tab:"anagrafica";
        if(!nuovo){var article=await service.ArticleAsync(codice,ct);if(article is null)return NotFound();Articolo=article;}
        await LoadLookupsAsync(ct);if(!nuovo&&ActiveTab=="barcode")Barcodes=await service.ArticleBarcodesAsync(Articolo.Code,ct);if(!nuovo&&ActiveTab=="listini"){VatRate=await service.ArticleVatRateAsync(Articolo.Code,ct);PriceLists=(await service.ArticlePriceListsAsync(Articolo.Code,VatRate,ct)).ToList();}if(!nuovo&&ActiveTab=="immagini")Photos=await service.ArticlePhotosAsync(Articolo.Code,ct);return Page();
    }
    public async Task<IActionResult> OnPostSaveBarcodeAsync(string codice,int barcodeId,string? barcodeValue,int? barcodeSupplierCode,CancellationToken ct)
    {
        try{await service.SaveArticleBarcodeAsync(codice,barcodeId,barcodeValue,barcodeSupplierCode,ct);}catch(InvalidOperationException ex){BarcodeError=ex.Message;}return RedirectToPage(new{codice,tab="barcode"});
    }
    public async Task<IActionResult> OnPostDeleteBarcodeAsync(string codice,int barcodeId,CancellationToken ct)
    {
        try{await service.DeleteArticleBarcodeAsync(codice,barcodeId,ct);}catch(InvalidOperationException ex){BarcodeError=ex.Message;}return RedirectToPage(new{codice,tab="barcode"});
    }
    public async Task<IActionResult> OnPostSavePriceListsAsync(string codice,CancellationToken ct)
    {
        for(var i=0;i<PriceLists.Count;i++)PriceLists[i].ListNumber=(byte)(i+1);
        try{await service.SaveArticlePriceListsAsync(codice,PriceLists,ct);}catch(InvalidOperationException ex){ListError=ex.Message;}return RedirectToPage(new{codice,tab="listini"});
    }
    public async Task<IActionResult> OnPostAddPhotoAsync(string codice,IFormFile? foto,string? descrizione,CancellationToken ct)
    {
        if(foto is null||foto.Length==0)return RedirectToPage(new{codice,tab="immagini"});try{if(foto.Length>15*1024*1024)throw new InvalidOperationException("L'immagine supera 15 MB.");var extension=Path.GetExtension(foto.FileName).ToLowerInvariant();if(extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))throw new InvalidOperationException("Formato immagine non valido.");var folder=Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(codice.Trim()));var directory=Path.Combine(environment.WebRootPath,"uploads","articoli",folder);Directory.CreateDirectory(directory);var fileName=$"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}{extension}";var path=Path.Combine(directory,fileName);await using(var stream=System.IO.File.Create(path))await foto.CopyToAsync(stream,ct);try{await service.AddArticlePhotoAsync(codice,fileName,descrizione??"",ct);}catch{System.IO.File.Delete(path);throw;}}catch(InvalidOperationException ex){PhotoError=ex.Message;}return RedirectToPage(new{codice,tab="immagini"});
    }
    public async Task<IActionResult> OnPostDeletePhotoAsync(string codice,string? fileName,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(fileName)||Path.GetFileName(fileName)!=fileName)return RedirectToPage(new{codice,tab="immagini"});var deleted=await service.DeleteArticlePhotoAsync(codice,fileName,ct);if(deleted is not null){var folder=Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(codice.Trim()));var path=Path.Combine(environment.WebRootPath,"uploads","articoli",folder,deleted);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}return RedirectToPage(new{codice,tab="immagini"});
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if(!ModelState.IsValid){await LoadLookupsAsync(ct);return Page();}
        try{await service.SaveArticleAsync(Articolo,IsNew,ct);}
        catch(InvalidOperationException ex){ModelState.AddModelError("Articolo.Code",ex.Message);await LoadLookupsAsync(ct);return Page();}
        return RedirectToPage("/Magazzino/Articoli/Index");
    }

    private async Task LoadLookupsAsync(CancellationToken ct)
    {
        Categorie=await service.ArticleCategoriesAsync(ct);Gruppi=await service.ArticleGroupsAsync(ct);Marche=await service.ArticleBrandsAsync(ct);UnitaMisura=await service.UnitMeasuresAsync(ct);CodiciIva=await service.VatCodesAsync(ct);Fornitori=await service.SuppliersAsync(ct);
    }
}
