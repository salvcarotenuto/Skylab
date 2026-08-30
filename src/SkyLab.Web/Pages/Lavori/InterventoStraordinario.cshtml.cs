using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Lavori;

public sealed class InterventoStraordinarioModel(CustomerService customers) : PageModel
{
    public IReadOnlyList<CustomerListItem> Clienti { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Clienti = await customers.SearchAsync(null, false, cancellationToken);

    public async Task<JsonResult> OnGetCustomerDataAsync(int customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.CustomerAsync(customerId, cancellationToken);
        if (customer is null) return new JsonResult(new { found = false });
        var sites = await customers.SitesAsync(customerId, cancellationToken);
        var machines = await customers.OperationalMachinesAsync(customerId, cancellationToken);
        var mainSite = string.Join(" · ", new[]
        {
            "Sede principale",
            string.Join(" ", new[] { customer.Street, customer.StreetNumber }.Where(x => !string.IsNullOrWhiteSpace(x))),
            customer.City
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new JsonResult(new
        {
            found = true,
            sites = new[] { new { id = 0, label = mainSite } }.Concat(sites.Where(x => x.Active).Select(x => new
            {
                id = x.Id,
                label = string.Join(" · ", new[] { x.Name, x.Street, x.City }.Where(v => !string.IsNullOrWhiteSpace(v)))
            })),
            machines = machines.Select(x => new
            {
                id = x.Id,
                siteId = x.SiteId ?? 0,
                label = string.Join(" · ", new[] { x.ArticleCode, x.Description }.Where(v => !string.IsNullOrWhiteSpace(v)))
            })
        });
    }
}
