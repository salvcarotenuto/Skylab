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
    string Site,
    string Summary,
    string AssignedOperator,
    short AssignedOperatorId,
    byte StatusId,
    string Status,
    byte? OutcomeId,
    string Outcome,
    decimal PlannedAmount,
    decimal RequestedAmount,
    int? InvoiceId,
    bool DispatchedToWork);

public sealed record WorkLookupItem(byte Id, string Description);

public sealed class WorkEditModel
{
    public int Id { get; set; }
    public short Year { get; set; }
    public int Code { get; set; }
    public int CustomerId { get; set; }
    public string Customer { get; set; } = "";
    public int? SiteId { get; set; }
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
    public bool DispatchedToWork { get; set; }
}

public sealed record OperatorLookupItem(short Id, string Description);
public sealed record WorkSiteLookupItem(int? Id,string Description);
public sealed record MobileWorkItem(int Id,string Number,DateTime? PlannedOn,TimeSpan? PlannedAt,string Customer,string Site,string Summary,string Status);
public sealed record MobileWorkDetailRow(string Reference,string Description,decimal Quantity,decimal UnitPrice,decimal Amount);
public sealed record MobileWorkDetailItem(
    int Id,string Number,DateTime? DraftedOn,DateTime? PlannedOn,TimeSpan? PlannedAt,DateTime? LastServiceOn,
    string Customer,string Site,byte PriceList,string AssignedOperator,string Status,string Outcome,string Summary,string Instructions,
    decimal PlannedLabour,decimal PlannedMaterials,decimal PlannedNet,
    IReadOnlyList<MobileWorkDetailRow> Services,IReadOnlyList<MobileWorkDetailRow> Materials);

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

public sealed record WorkReferenceLookup(
    string Type,string Reference,string Description,string Category,string Unit,decimal Price,
    decimal? Price1,decimal? Price2,decimal? Price3,decimal? Price4,decimal? Price5,decimal? Price6,string Barcodes);

public sealed record MobileReportRow(string Type,string Reference,decimal Quantity,decimal Price);
public sealed record MobileReportRequest(
    string SubmissionId,string CompletedOn,string CompletedAt,string ManHours,string Outcome,string WorkPerformed,
    decimal CollectedAmount,string Notes,IReadOnlyList<MobileReportRow> Rows);
public sealed record MobileReportInboxItem(long Id,string SubmissionId,int WorkId,string WorkNumber,string Customer,string Username,DateTime ReceivedOn,string Status,string Error);
public sealed record MobileReportPreview(MobileReportInboxItem Inbox,MobileReportRequest Report);
public sealed record AgendaFlowItem(
    int WorkId,string WorkNumber,string Customer,string WorkStatus,string SheetFlow,
    long? InboxId,DateTime? ReceivedOn,string Username);

public sealed record WorkPhotoItem(short Number,string FileName,DateTime? TakenOn,string Description,string Url);
public sealed record WorkDocumentItem(short Number,string FileName,string OriginalName,DateTime UploadedOn,string Description,string Url);
public sealed record WorkHistoryItem(
    long Id,DateTime EventOn,string EventType,string PreviousStatus,string NewStatus,string PreviousOutcome,string NewOutcome,
    DateTime? PreviousDueOn,DateTime? NewDueOn,DateTime? PreviousPlannedOn,DateTime? NewPlannedOn,
    DateTime? CompletedOn,DateTime? SkippedOn,DateTime? RealignedOn,string Notes,string User);
