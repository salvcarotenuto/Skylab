using System.ComponentModel.DataAnnotations;

namespace SkyLab.Web.Models;

public sealed record SupplierListItem(int Code,string Name,string City,string Province,string VatNumber,string Phone,string Email,string LocalUnit);
public sealed record SupplierOption(int Id,string Label);

public sealed class SupplierEditModel
{
    public int Code { get; set; }
    [Required(ErrorMessage="Inserire il nome del fornitore."),StringLength(250)] public string Name { get; set; }="";
    [StringLength(20)] public string? TaxCode { get; set; }
    [StringLength(20)] public string? VatNumber { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(5)] public string? PostalCode { get; set; }
    [StringLength(2)] public string? Province { get; set; }
    [StringLength(100)] public string? Street { get; set; }
    [StringLength(20)] public string? StreetNumber { get; set; }
    public short? CountryCode { get; set; }
    [StringLength(20)] public string? Phone1 { get; set; }
    [StringLength(20)] public string? Phone2 { get; set; }
    [EmailAddress,StringLength(100)] public string? Email { get; set; }
    [EmailAddress,StringLength(100)] public string? CertifiedEmail { get; set; }
    [StringLength(100)] public string? Website { get; set; }
    [StringLength(100)] public string? Contact { get; set; }
    public short? PaymentCode { get; set; }
    public int? BankCode { get; set; }
    public short? LocalUnitCode { get; set; }
    [StringLength(8)] public string? AccountCode { get; set; }
    [StringLength(50)] public string? Iban { get; set; }
    [StringLength(255)] public string? Notes { get; set; }
}
