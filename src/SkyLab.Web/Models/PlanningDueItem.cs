namespace SkyLab.Web.Models;

public sealed record PlanningDueItem(
    int MachineId,
    int CustomerId,
    string CustomerName,
    string City,
    short DistrictId,
    string District,
    byte CustomerType,
    string ArticleCode,
    string ArticleDescription,
    string Category,
    decimal? Value,
    short? Duration,
    DateTime? LastServiceDate,
    DateTime DueDate,
    int? CommitmentId,
    int? WorkId,
    string CommitmentStatus,
    DateTime? AgreedOn,
    TimeSpan? AgreedAt,
    string CommitmentDescription,
    string CommitmentNotes);

public sealed record PlanningCustomerGroup(
    int CustomerId,
    string CustomerName,
    string City,
    string District,
    byte CustomerType,
    IReadOnlyList<PlanningDueItem> Items);

public sealed record PlanningExtraordinaryItem(
    int CommitmentId,
    int? WorkId,
    int CustomerId,
    string CustomerName,
    string City,
    string District,
    string Site,
    string Description,
    DateTime? LastServiceDate,
    DateTime AgreedOn,
    TimeSpan? AgreedAt,
    string Notes,
    string Origin,
    string ArticleCode);

public sealed record PlanningExtraordinaryGroup(
    int CustomerId,
    string CustomerName,
    string City,
    string District,
    IReadOnlyList<PlanningExtraordinaryItem> Items);

public sealed record PlanningDistrict(short Code, string Description);

public sealed record PlanningCategory(short Code, string Description);

public sealed record PlanningDayCommitment(string Time, string Customer, string Description, string Operator);

public sealed record PlanningAgendaItem(DateTime Date, string Time, string Customer, string Site, string Description, string Operator, string Kind);

public sealed record PlanningDayAvailability(
    DateTime Date,
    bool IsWorkingDay,
    string CalendarStatus,
    int? ActiveOperators,
    int AssignedOperators,
    int Reservations,
    int PlannedWorks,
    IReadOnlyList<PlanningDayCommitment> Commitments);

public sealed record InstalledMachine(
    int Id,
    int CustomerId,
    string CustomerName,
    string City,
    string Category,
    string ArticleCode,
    string ArticleDescription,
    decimal? Value,
    DateTime? InstalledOn,
    DateTime? NextServiceOn);
