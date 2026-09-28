using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class SchedeModel(WorkService service) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? Da { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Al { get; set; }
    [BindProperty(SupportsGet = true)] public string OrdinaPer { get; set; } = "lavoro";
    [BindProperty(SupportsGet = true)] public byte Stato { get; set; }
    [BindProperty(SupportsGet = true)] public short Operatore { get; set; }
    [BindProperty(SupportsGet = true)] public byte Esito { get; set; }

    public IReadOnlyList<WorkListItem> Items { get; private set; } = [];
    public IReadOnlyList<WorkLookupItem> Stati { get; private set; } = [];
    public IReadOnlyList<OperatorLookupItem> Operatori { get; private set; } = [];
    public IReadOnlyList<WorkLookupItem> Esiti { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (OrdinaPer != "lavoro") OrdinaPer = "scheda";
        if (Da is null)
        {
            Da = OrdinaPer == "lavoro" ? DateTime.Today : new DateTime(DateTime.Today.Year, 1, 1);
            ModelState.Remove(nameof(Da));
        }
        if (Al is null)
        {
            Al = OrdinaPer == "lavoro" ? DateTime.Today.AddYears(1) : DateTime.Today;
            ModelState.Remove(nameof(Al));
        }
        if (Al < Da) (Da, Al) = (Al, Da);
        Stati = await service.StatusesAsync(cancellationToken);
        Operatori = await service.OperatorsAsync(cancellationToken);
        Esiti = await service.OutcomesAsync(cancellationToken);
        Items = await service.SearchAsync(Da.Value, Al.Value, OrdinaPer, Stato, Operatore, Esito, cancellationToken);
    }

    public async Task<JsonResult> OnGetInvoiceCandidatesAsync(int cliente, DateTime? dal, DateTime? al, CancellationToken cancellationToken)
    {
        var today=DateTime.Today;
        var from=(dal??new DateTime(today.Year,1,1)).Date;
        var to=(al??today).Date;
        if(to<from)(from,to)=(to,from);
        var items=await service.InvoiceCandidatesAsync(cliente,from,to,cancellationToken);
        return new JsonResult(items.Select(x=>new
        {
            id=x.Id,
            year=x.Year,
            code=x.Code,
            customerId=x.CustomerId,
            customer=x.Customer,
            completedOn=x.CompletedOn.ToString("yyyy-MM-dd"),
            completedOnText=x.CompletedOn.ToString("dd/MM/yyyy"),
            workPerformed=x.WorkPerformed,
            requestedAmount=x.RequestedAmount
        }));
    }

    public async Task<IActionResult> OnGetInvoiceCandidateAsync(int id, CancellationToken cancellationToken)
    {
        var work=await service.WorkAsync(id,cancellationToken);
        if(work is null||work.CompletedOn is null||work.InvoiceId is not null)return NotFound();
        var details=await service.ActualDetailsAsync(id,cancellationToken);
        var references=await service.WorkReferencesAsync(cancellationToken);
        var vatRates=await service.ArticleVatRatesAsync(cancellationToken);
        var services=details.Where(x=>x.Type=="P").ToArray();
        var materials=details.Where(x=>x.Type=="A").Select(row=>
        {
            var reference=references.FirstOrDefault(x=>x.Type=="A"&&string.Equals(x.Reference,row.Reference,StringComparison.OrdinalIgnoreCase));
            return new
            {
                code=row.Reference,
                description=row.Description,
                unit=reference?.Unit??"",
                quantity=row.Quantity,
                unitPrice=row.UnitPrice,
                amount=row.Amount,
                vatRate=vatRates.TryGetValue(row.Reference,out var vatRate)?vatRate:null
            };
        }).ToArray();
        var activity=services.Length==0
            ? work.WorkPerformed
            : string.Join(Environment.NewLine,services.Select(x=>x.Description));
        return new JsonResult(new
        {
            activity,
            serviceTotal=services.Sum(x=>x.Amount),
            materialTotal=materials.Sum(x=>x.amount),
            materials
        });
    }
}
