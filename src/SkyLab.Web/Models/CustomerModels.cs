using System.ComponentModel.DataAnnotations;

namespace SkyLab.Web.Models;

public sealed record CustomerListItem(int Code, string Name, string City, string Province, string Phone, string Email, bool Active, int Sites, int Machines);
public sealed record LookupItem(int Id, string Label);
public sealed class InventoryGroupEditModel
{
    public short Code { get; set; }
    [Required(ErrorMessage = "Inserire la descrizione del gruppo."), StringLength(100)] public string Description { get; set; } = "";
}
public sealed class InventoryBrandEditModel
{
    public short Code { get; set; }
    [Required(ErrorMessage = "Inserire la descrizione del marchio."), StringLength(100)] public string Description { get; set; } = "";
}
public sealed class UnitMeasureEditModel
{
    [Required(ErrorMessage = "Inserire il codice unità di misura."), StringLength(4)] public string Code { get; set; } = "";
    [Required(ErrorMessage = "Inserire la descrizione dell'unità di misura."), StringLength(100)] public string Description { get; set; } = "";
}
public sealed class GoodsAppearanceEditModel
{
    public short Code { get; set; }
    [Required(ErrorMessage = "Inserire la descrizione dell'aspetto beni."), StringLength(100)] public string Description { get; set; } = "";
}
public sealed class VatCodeEditModel
{
    [Required(ErrorMessage = "Inserire il codice IVA."), StringLength(12)] public string Code { get; set; } = "";
    [Required(ErrorMessage = "Inserire la descrizione del codice IVA."), StringLength(250)] public string Description { get; set; } = "";
    [Range(0, 100, ErrorMessage = "Aliquota non valida.")] public decimal Rate { get; set; }
    [Range(0, 100, ErrorMessage = "Detrazione non valida.")] public decimal Deduction { get; set; }
    [StringLength(10)] public string? Nature { get; set; }
}
public sealed record CodeLookupItem(string Code, string Label);
public sealed record VatCodeListItem(string Code, string Description, decimal Rate, decimal Deduction, string Nature);
public sealed record AccountMasterListItem(short Code, string Description, string Type, bool Locked);
public sealed class AccountMasterEditModel
{
    public short Code { get; set; }
    [Required(ErrorMessage = "Inserire la descrizione del mastro."), StringLength(100)] public string Description { get; set; } = "";
    [Required(ErrorMessage = "Selezionare il tipo mastro."), StringLength(1)] public string Type { get; set; } = "P";
}
public sealed record AccountListItem(short Code, string Description, string Type, short Master, string MasterDescription, string PartyKind, bool Locked, bool Load);
public sealed class AccountEditModel
{
    public short Code { get; set; }
    [Required(ErrorMessage = "Inserire la descrizione del conto."), StringLength(100)] public string Description { get; set; } = "";
    [Required(ErrorMessage = "Selezionare il tipo conto."), StringLength(1)] public string Type { get; set; } = "P";
    [Range(1, 999, ErrorMessage = "Selezionare il mastro.")] public short Master { get; set; }
    [StringLength(1)] public string? PartyKind { get; set; }
    public bool Load { get; set; }
}
public sealed record AccountingCauseListItem(short Code, string Description, string MovementType, string PartyKind, string Sign, string InOut, string CauseType, string PaymentType, bool Cash, bool Invoice, bool DueDate, bool Title, bool Salary, bool Print, bool Locked, short Debit1, short Debit2, short Debit3, short Debit4, short Debit5, short Debit6, short Credit1, short Credit2, short Credit3, short Credit4, short Credit5, short Credit6);
public sealed class AccountingCauseEditModel
{
    public short Code { get; set; }
    [Required(ErrorMessage = "Inserire la descrizione della causale."), StringLength(100)] public string Description { get; set; } = "";
    [Required(ErrorMessage = "Selezionare il tipo movimento."), StringLength(1)] public string MovementType { get; set; } = "";
    [StringLength(1)] public string? PartyKind { get; set; }
    [StringLength(1)] public string? Sign { get; set; }
    [StringLength(1)] public string? InOut { get; set; }
    [StringLength(1)] public string? CauseType { get; set; }
    [StringLength(1)] public string? PaymentType { get; set; }
    public bool Cash { get; set; }
    public bool Invoice { get; set; }
    public bool DueDate { get; set; }
    public bool Title { get; set; }
    public bool Salary { get; set; }
    public bool Print { get; set; }
    public bool Locked { get; set; }
    public short Debit1 { get; set; }
    public short Debit2 { get; set; }
    public short Debit3 { get; set; }
    public short Debit4 { get; set; }
    public short Debit5 { get; set; }
    public short Debit6 { get; set; }
    public short Credit1 { get; set; }
    public short Credit2 { get; set; }
    public short Credit3 { get; set; }
    public short Credit4 { get; set; }
    public short Credit5 { get; set; }
    public short Credit6 { get; set; }
}
public sealed record CityLookupItem(string Name, string PostalCode, string Province);
public sealed record SupplierLookupItem(int Code, string Name, string City, string Province);
public sealed record PartyLookupItem(int Code, string Name, string City, string Province);
public sealed record PartyLookupModel(string Kind, IReadOnlyList<PartyLookupItem> Items);

public sealed class CustomerEditModel
{
    public int Code { get; set; }
    [Required(ErrorMessage = "Inserire il nome cliente."), StringLength(250)] public string Name { get; set; } = "";
    [StringLength(20)] public string? TaxCode { get; set; }
    [StringLength(20)] public string? VatNumber { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(5)] public string? PostalCode { get; set; }
    [StringLength(2)] public string? Province { get; set; }
    [StringLength(100)] public string? Street { get; set; }
    [StringLength(20)] public string? StreetNumber { get; set; }
    [StringLength(100)] public string? Contact { get; set; }
    [StringLength(30)] public string? Phone1 { get; set; }
    [StringLength(30)] public string? Phone2 { get; set; }
    [EmailAddress, StringLength(60)] public string? Email { get; set; }
    [EmailAddress, StringLength(60)] public string? CertifiedEmail { get; set; }
    [StringLength(10)] public string? SdiCode { get; set; }
    [Range(0, 6, ErrorMessage = "Selezionare un listino valido.")] public byte PriceList { get; set; }
    [StringLength(255)] public string? Notes { get; set; }
    public bool Active { get; set; } = true;
}

public sealed record SiteListItem(int Id, int Code, string Name, string City, string Province, string Street, string Contact, bool Active);
public sealed class SiteEditModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int Code { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(100)] public string? City { get; set; }
    [StringLength(5)] public string? PostalCode { get; set; }
    [StringLength(2)] public string? Province { get; set; }
    [StringLength(100)] public string? Street { get; set; }
    [StringLength(20)] public string? StreetNumber { get; set; }
    [StringLength(100)] public string? Contact { get; set; }
    [StringLength(25)] public string? ContactPhone { get; set; }
    [EmailAddress, StringLength(100)] public string? ContactEmail { get; set; }
    [StringLength(255)] public string? Notes { get; set; }
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
    public decimal? SuppliedQuantity { get; set; }
    public decimal? DailyConsumption { get; set; }
    public DateTime? NextServiceOn { get; set; }
}

public sealed record OperationalMachine(
    int Id, string ArticleCode, string Description, string Category, decimal? Value,
    DateTime? InstalledOn, DateTime? NextServiceOn, int? SiteId, string SiteName, string SiteAddress);

public sealed record OperationalSiteGroup(
    int? SiteId, string Name, string Address, IReadOnlyList<OperationalMachine> Machines);

public sealed record ArticleChoice(
    string Code, string Description, short CategoryCode, string Category,
    decimal Price, short DurationDays, decimal DailyConsumption, string UnitMeasure, decimal VatRate);
public sealed record ArticleListItem(
    string Code, string Description, short CategoryCode, string Category,
    short GroupCode, string Group, short BrandCode, string Brand, string SalesUnit,
    decimal Price, short DurationDays, decimal DailyConsumption);
public sealed record ArticleBarcodeItem(
    int Id, string Value, byte Type, int? SupplierCode, string SupplierName,
    bool IsValidEan, bool IsLegacy);
public sealed record ArticlePhotoItem(string FileName,DateTime? TakenOn,string Description,string Url);
public sealed class ArticlePriceListEditModel
{
    public byte ListNumber { get; set; }
    public decimal Markup { get; set; }
    public decimal Price { get; set; }
    public decimal VatPrice { get; set; }
}

public sealed record ArticleDetail(
    string Code, string Description,
    string Category, string Group, string Brand,
    string PurchaseUnit, string WorkUnit, string SalesUnit,
    decimal Weight, short Pieces, short DurationDays, decimal DailyConsumption,
    decimal Cost, decimal Price, decimal Stock, decimal MinimumStock, decimal MaximumStock,
    string Location, string Notes);

public sealed class ArticleEditModel
{
    [Required(ErrorMessage = "Inserire il codice articolo."), StringLength(30)] public string Code { get; set; } = "";
    [Required(ErrorMessage = "Inserire la descrizione."), StringLength(255)] public string Description { get; set; } = "";
    public short CategoryCode { get; set; }
    public short GroupCode { get; set; }
    public short BrandCode { get; set; }
    [StringLength(4)] public string? PurchaseUnit { get; set; }
    [StringLength(4)] public string? WorkUnit { get; set; }
    [StringLength(4)] public string? SalesUnit { get; set; }
    public int? SupplierCode { get; set; }
    [StringLength(30)] public string? SupplierArticleCode { get; set; }
    public decimal Weight { get; set; }
    public short Pieces { get; set; }
    public short DurationDays { get; set; }
    public decimal DailyConsumption { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    [StringLength(50)] public string? Location { get; set; }
    [StringLength(12)] public string? VatCode { get; set; }
    [StringLength(255)] public string? Notes { get; set; }
}
