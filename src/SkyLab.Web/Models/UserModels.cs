using System.ComponentModel.DataAnnotations;
namespace SkyLab.Web.Models;
public sealed record UserListItem(int Code,string LastName,string FirstName,string UserName,string City,string Phone,string Email,bool IsLocked,bool IsActive,int TypeCode,string TypeDescription);
public sealed record LookupOption(int Code,string Description);
public sealed record UserLookups(IReadOnlyList<LookupOption> Locations,IReadOnlyList<LookupOption> Types,IReadOnlyList<LookupOption> Qualifications);
public sealed record UserDeleteResult(bool Deleted,string Message);
public sealed class UserEditModel
{
 public int Code{get;set;}
 [Required(ErrorMessage="Campo Cognome obbligatorio"),StringLength(255),Display(Name="Cognome")] public string LastName{get;set;}="";
 [Required(ErrorMessage="Campo Nome obbligatorio"),StringLength(255),Display(Name="Nome")] public string FirstName{get;set;}="";
 [StringLength(50),Display(Name="Città")] public string? City{get;set;}
 [StringLength(50),Display(Name="Indirizzo")] public string? Address{get;set;}
 [StringLength(16),Display(Name="Codice fiscale")] public string? TaxCode{get;set;}
 [StringLength(30),Display(Name="Telefono")] public string? Phone{get;set;}
 [StringLength(50),EmailAddress(ErrorMessage="Indirizzo e-mail non valido."),Display(Name="E-mail")] public string? Email{get;set;}
 [Required(ErrorMessage="Campo Username obbligatorio"),StringLength(50),Display(Name="Username")] public string UserName{get;set;}="";
 [Required(ErrorMessage="Campo Password obbligatorio"),StringLength(50),Display(Name="Password")] public string Password{get;set;}="";
 [Required(ErrorMessage="Campo Tipo utente obbligatorio"),Range(1,4,ErrorMessage="Selezionare il tipo utente."),Display(Name="Tipo utente")] public int? TypeCode{get;set;}
 [Display(Name="Qualifica")] public int? QualificationCode{get;set;}
 [Display(Name="Unità locale")] public int? LocationCode{get;set;}
 [Display(Name="Sesso")] public string? Gender{get;set;}
 [Display(Name="Utente attivo")] public bool IsActive{get;set;}=true;
 [Display(Name="Utente bloccato")] public bool IsLocked{get;set;}
}
