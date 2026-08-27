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
    DateTime DueDate);

public sealed record PlanningCustomerGroup(
    int CustomerId,
    string CustomerName,
    string City,
    string District,
    byte CustomerType,
    IReadOnlyList<PlanningDueItem> Items);

public sealed record PlanningDistrict(short Code, string Description);

public sealed record PlanningCategory(short Code, string Description);

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
