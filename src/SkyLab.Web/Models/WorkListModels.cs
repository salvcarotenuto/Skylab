namespace SkyLab.Web.Models;

public sealed record WorkListItem(
    int Id,
    short Year,
    int Code,
    DateTime? DraftedOn,
    DateTime? PlannedOn,
    TimeSpan? PlannedAt,
    int CustomerId,
    string Customer,
    string Summary,
    string AssignedOperator,
    byte StatusId,
    string Status,
    byte? OutcomeId,
    string Outcome,
    decimal PlannedAmount,
    decimal RequestedAmount,
    int? InvoiceId);

public sealed record WorkLookupItem(byte Id, string Description);

public sealed class WorkEditModel
{
    public int Id { get; set; }
    public short Year { get; set; }
    public int Code { get; set; }
    public int CustomerId { get; set; }
    public string Customer { get; set; } = "";
    public DateTime? DraftedOn { get; set; }
    public DateTime? PlannedOn { get; set; }
    public TimeSpan? PlannedAt { get; set; }
    public DateTime? LastServiceOn { get; set; }
    public short AssignedOperator { get; set; }
    public byte StatusId { get; set; }
    public byte? OutcomeId { get; set; }
    public string Summary { get; set; } = "";
    public string Instructions { get; set; } = "";
    public decimal PlannedLabour { get; set; }
    public decimal PlannedMaterials { get; set; }
    public decimal PlannedNet { get; set; }
    public DateTime? CompletedOn { get; set; }
    public TimeSpan? CompletedAt { get; set; }
    public short? ExecutingOperator { get; set; }
    public decimal? ManHours { get; set; }
    public string WorkPerformed { get; set; } = "";
    public decimal ActualLabour { get; set; }
    public decimal ActualMaterials { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public int? InvoiceId { get; set; }
    public string Notes { get; set; } = "";
}

public sealed record OperatorLookupItem(short Id, string Description);

public sealed record WorkDetailItem(
    short Row,
    string Type,
    string Reference,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    bool TypeInferred)
{
    public decimal Amount => Math.Round(Quantity * UnitPrice, 2);
}

public sealed record WorkReferenceLookup(string Type,string Reference,string Description,string Category,decimal Price);

public sealed record WorkPhotoItem(short Number,string FileName,DateTime? TakenOn,string Description,string Url);
public sealed record WorkDocumentItem(short Number,string FileName,string OriginalName,DateTime UploadedOn,string Description,string Url);
public sealed record WorkHistoryItem(
    long Id,DateTime EventOn,string EventType,string PreviousStatus,string NewStatus,string PreviousOutcome,string NewOutcome,
    DateTime? PreviousDueOn,DateTime? NewDueOn,DateTime? PreviousPlannedOn,DateTime? NewPlannedOn,
    DateTime? CompletedOn,DateTime? SkippedOn,DateTime? RealignedOn,string Notes,string User);
