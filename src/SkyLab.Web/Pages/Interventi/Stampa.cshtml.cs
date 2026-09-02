using Microsoft.AspNetCore.Mvc.RazorPages;
using SkyLab.Web.Models;
using SkyLab.Web.Services;

namespace SkyLab.Web.Pages.Interventi;

public sealed class StampaModel(WorkService service) : PageModel
{
    public string Tipo { get; private set; } = "riepilogo";
    public IReadOnlyList<AgendaPrintItem> Items { get; private set; } = [];

    public async Task OnGetAsync(string? ids, string? tipo, CancellationToken cancellationToken)
    {
        Tipo = string.Equals(tipo, "sheets", StringComparison.OrdinalIgnoreCase) ? "schede" : "riepilogo";
        var workIds = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0).Distinct().Take(100).ToArray();
        var operators = await service.OperatorsAsync(cancellationToken);
        var operatorNames = operators.ToDictionary(item => item.Id, item => item.Description);
        var result = new List<AgendaPrintItem>();
        foreach (var id in workIds)
        {
            var work = await service.WorkAsync(id, cancellationToken);
            if (work is null) continue;
            var assigned = operatorNames.GetValueOrDefault(work.AssignedOperator) ?? "";
            var printedOperator = string.IsNullOrWhiteSpace(assigned) ? "Da assegnare" : assigned;
            var details = Tipo == "schede" ? await service.PlannedDetailsAsync(id, cancellationToken) : [];
            result.Add(new(work, printedOperator, details));
        }
        Items = result.OrderBy(item => item.Work.PlannedOn).ThenBy(item => item.Work.PlannedAt).ToList();
    }
}

public sealed record AgendaPrintItem(WorkEditModel Work, string Operator, IReadOnlyList<WorkDetailItem> Details);
