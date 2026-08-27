namespace SkyLab.Web.Models;

public enum InterventionStatus { Planned, InProgress, Completed }

public sealed class Intervention
{
    public int Id { get; init; }
    public string Customer { get; init; } = "";
    public string Site { get; init; } = "";
    public string Plant { get; init; } = "";
    public DateTime ScheduledAt { get; init; }
    public string Kind { get; init; } = "";
    public InterventionStatus Status { get; set; }
    public string Notes { get; set; } = "";
    public List<UsedMaterial> Materials { get; } = [];
}

public sealed record UsedMaterial(string Barcode, string Description, decimal Quantity);
