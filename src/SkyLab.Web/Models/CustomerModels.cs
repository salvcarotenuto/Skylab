using System.ComponentModel.DataAnnotations;

namespace SkyLab.Web.Models;

public sealed record CustomerListItem(int Code, string Name, string City, string Province, string Phone, string Email, bool Active, int Sites, int Machines);
public sealed record LookupItem(int Id, string Label);

public sealed class CustomerEditModel
{
    public int Code { get; set; }
    [Required, StringLength(250)] public string Name { get; set; } = "";
    [StringLength(20)] public string TaxCode { get; set; } = "";
    [StringLength(20)] public string VatNumber { get; set; } = "";
    [StringLength(100)] public string City { get; set; } = "";
    [StringLength(5)] public string PostalCode { get; set; } = "";
    [StringLength(2)] public string Province { get; set; } = "";
    [StringLength(100)] public string Street { get; set; } = "";
    [StringLength(20)] public string StreetNumber { get; set; } = "";
    [StringLength(100)] public string Contact { get; set; } = "";
    [StringLength(30)] public string Phone1 { get; set; } = "";
    [StringLength(30)] public string Phone2 { get; set; } = "";
    [EmailAddress, StringLength(60)] public string Email { get; set; } = "";
    [EmailAddress, StringLength(60)] public string CertifiedEmail { get; set; } = "";
    [StringLength(10)] public string SdiCode { get; set; } = "";
    [StringLength(255)] public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed record SiteListItem(int Id, int Code, string Name, string City, string Province, string Street, string Contact, bool Active);
public sealed class SiteEditModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int Code { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(100)] public string City { get; set; } = "";
    [StringLength(5)] public string PostalCode { get; set; } = "";
    [StringLength(2)] public string Province { get; set; } = "";
    [StringLength(100)] public string Street { get; set; } = "";
    [StringLength(20)] public string StreetNumber { get; set; } = "";
    [StringLength(100)] public string Contact { get; set; } = "";
    [StringLength(25)] public string ContactPhone { get; set; } = "";
    [EmailAddress, StringLength(100)] public string ContactEmail { get; set; } = "";
    [StringLength(255)] public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed class MachineEditModel
{
    public int Id { get; set; }
    [Required] public int CustomerId { get; set; }
    public int? SiteId { get; set; }
    [Required, StringLength(30)] public string ArticleCode { get; set; } = "";
    public short? CategoryId { get; set; }
    public DateTime? InstalledOn { get; set; }
    public decimal? Value { get; set; }
    public short? DurationDays { get; set; }
    public DateTime? NextServiceOn { get; set; }
}

public sealed record OperationalMachine(
    int Id, string ArticleCode, string Description, string Category, decimal? Value,
    DateTime? InstalledOn, DateTime? NextServiceOn, int? SiteId, string SiteName, string SiteAddress);

public sealed record OperationalSiteGroup(
    int? SiteId, string Name, string Address, IReadOnlyList<OperationalMachine> Machines);
