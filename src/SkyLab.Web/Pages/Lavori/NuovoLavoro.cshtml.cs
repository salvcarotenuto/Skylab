using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class NuovoLavoroModel(CustomerService customers,PlanningService planning,WorkService works) : PageModel
{
    public IReadOnlyList<CustomerListItem> Clienti { get; private set; }=[];
    public IReadOnlyList<ArticleChoice> Articoli { get; private set; }=[];
    public IReadOnlyList<WorkReferenceLookup> Prestazioni { get; private set; }=[];
    public async Task OnGetAsync(CancellationToken ct){Clienti=await customers.SearchAsync(null,false,ct);Articoli=await customers.ArticleChoicesAsync(ct);Prestazioni=(await works.WorkReferencesAsync(ct)).Where(x=>x.Type=="P").ToList();}
    public async Task<JsonResult> OnGetCustomerDataAsync(int customerId,CancellationToken ct)
    {
        var customer=await customers.CustomerAsync(customerId,ct);if(customer is null)return new(new{found=false});var sites=await customers.SitesAsync(customerId,ct);
        var main=string.Join(" · ",new[]{"Sede principale",string.Join(" ",new[]{customer.Street,customer.StreetNumber}.Where(x=>!string.IsNullOrWhiteSpace(x))),customer.City}.Where(x=>!string.IsNullOrWhiteSpace(x)));
        return new(new{found=true,sites=new[]{new{id=0,label=main}}.Concat(sites.Where(x=>x.Active).Select(x=>new{id=x.Id,label=string.Join(" · ",new[]{x.Name,x.Street,x.City}.Where(v=>!string.IsNullOrWhiteSpace(v)))}))});
    }
    public async Task<JsonResult> OnPostCreateCustomerAsync(string? name,CancellationToken ct)
    {if(string.IsNullOrWhiteSpace(name))return new(new{ok=false,message="Inserire il nome del cliente."});var model=new CustomerEditModel{Name=name.Trim(),Active=true};var code=await customers.SaveCustomerAsync(model,ct);return new(new{ok=true,code,name=model.Name});}
    public async Task<IActionResult> OnPostAsync(int clienteId,int? sedeId,string? articolo,string? descrizione,DateTime? dataIntervento,TimeSpan? oraIntervento,string? note,CancellationToken ct)
    {
        if(clienteId<=0||string.IsNullOrWhiteSpace(articolo)||string.IsNullOrWhiteSpace(descrizione)||dataIntervento is null){TempData["ErrorMessage"]="Compilare cliente, articolo, lavoro richiesto e data concordata.";await OnGetAsync(ct);return Page();}
        try{await planning.CreateNewWorkCommitmentAsync(clienteId,sedeId.GetValueOrDefault()>0?sedeId:null,articolo,dataIntervento.Value,oraIntervento,descrizione,note,ct);var from=DateTime.Today.AddDays(-60);var to=DateTime.Today.AddDays(60);if(dataIntervento.Value.Date>to)to=dataIntervento.Value.Date;if(dataIntervento.Value.Date<from)from=dataIntervento.Value.Date;return RedirectToPage("Pianificazione",new{dal=from.ToString("yyyy-MM-dd"),al=to.ToString("yyyy-MM-dd")});}
        catch(InvalidOperationException ex){TempData["ErrorMessage"]=ex.Message;await OnGetAsync(ct);return Page();}
    }
}
