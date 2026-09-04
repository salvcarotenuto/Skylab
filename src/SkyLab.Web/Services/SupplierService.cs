using MySqlConnector;
using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public sealed class SupplierService(IConfiguration configuration)
{
    private string ConnectionString { get { var cs=configuration.GetConnectionString("SkyLabDb")??configuration.GetConnectionString("MicronoteDb")??throw new InvalidOperationException("Connessione MySQL non configurata.");return new MySqlConnectionStringBuilder(cs){Database="skylab_0001",SslMode=MySqlSslMode.None}.ConnectionString; } }

    public async Task<IReadOnlyList<SupplierListItem>> SearchAsync(string? search,CancellationToken ct)
    {
        const string sql="""SELECT f.Codice,COALESCE(f.Nome,''),COALESCE(f.Citta,''),COALESCE(f.Provincia,''),COALESCE(f.Piva,''),COALESCE(f.Telefono1,''),COALESCE(f.Email,''),COALESCE(u.Nome,'') FROM Fornitori f LEFT JOIN UnitaLocali u ON u.Codice=f.ULocale WHERE (@q='' OR f.Nome LIKE CONCAT('%',@q,'%') OR f.Codice=@code OR f.Piva LIKE CONCAT('%',@q,'%')) ORDER BY f.Nome LIMIT 500""";
        var result=new List<SupplierListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@q",search?.Trim()??"");cmd.Parameters.AddWithValue("@code",int.TryParse(search,out var code)?code:-1);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),S(r,1),S(r,2),S(r,3),S(r,4),S(r,5),S(r,6),S(r,7)));return result;
    }
    public async Task<SupplierEditModel?> GetAsync(int code,CancellationToken ct)
    {
        const string sql="SELECT Codice,Nome,Codfi,Piva,Citta,Cap,Provincia,Via,Nazione,Civico,Telefono1,Telefono2,Pec,Email,SitoWeb,Contatto,Pagamento,Banca,CtPartita,CodIban,Notes,ULocale FROM Fornitori WHERE Codice=@code";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",code);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        return new(){Code=r.GetInt32(0),Name=S(r,1),TaxCode=S(r,2),VatNumber=S(r,3),City=S(r,4),PostalCode=S(r,5),Province=S(r,6),Street=S(r,7),CountryCode=N16(r,8),StreetNumber=S(r,9),Phone1=S(r,10),Phone2=S(r,11),CertifiedEmail=S(r,12),Email=S(r,13),Website=S(r,14),Contact=S(r,15),PaymentCode=N16(r,16),BankCode=N32(r,17),AccountCode=S(r,18),Iban=S(r,19),Notes=S(r,20),LocalUnitCode=N16(r,21)};
    }
    public async Task<int> SaveAsync(SupplierEditModel m,CancellationToken ct)
    {
        m.Name=m.Name.Trim(); foreach(var p in typeof(SupplierEditModel).GetProperties().Where(x=>x.PropertyType==typeof(string))){var value=(string?)p.GetValue(m);p.SetValue(m,value?.Trim()??"");}
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);if(m.Code==0){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Fornitori FOR UPDATE",cn,tx);m.Code=Convert.ToInt32(await next.ExecuteScalarAsync(ct));}
        const string sql="""INSERT INTO Fornitori(Codice,Nome,Codfi,Piva,Citta,Cap,Provincia,Via,Nazione,Civico,Telefono1,Telefono2,Pec,Email,SitoWeb,Contatto,Pagamento,Banca,CtPartita,CodIban,Notes,ULocale) VALUES(@Code,@Name,@TaxCode,@VatNumber,@City,@PostalCode,@Province,@Street,@CountryCode,@StreetNumber,@Phone1,@Phone2,@CertifiedEmail,@Email,@Website,@Contact,@PaymentCode,@BankCode,@AccountCode,@Iban,@Notes,@LocalUnitCode) ON DUPLICATE KEY UPDATE Nome=VALUES(Nome),Codfi=VALUES(Codfi),Piva=VALUES(Piva),Citta=VALUES(Citta),Cap=VALUES(Cap),Provincia=VALUES(Provincia),Via=VALUES(Via),Nazione=VALUES(Nazione),Civico=VALUES(Civico),Telefono1=VALUES(Telefono1),Telefono2=VALUES(Telefono2),Pec=VALUES(Pec),Email=VALUES(Email),SitoWeb=VALUES(SitoWeb),Contatto=VALUES(Contatto),Pagamento=VALUES(Pagamento),Banca=VALUES(Banca),CtPartita=VALUES(CtPartita),CodIban=VALUES(CodIban),Notes=VALUES(Notes),ULocale=VALUES(ULocale)""";
        await using var cmd=new MySqlCommand(sql,cn,tx);Add(cmd,m);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);return m.Code;
    }
    public async Task<string?> DeleteAsync(int code,CancellationToken ct){await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);try{await using var cmd=new MySqlCommand("DELETE FROM Fornitori WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);return await cmd.ExecuteNonQueryAsync(ct)==1?null:"Fornitore non trovato.";}catch(MySqlException ex) when(ex.Number==1451){return "Il fornitore è già utilizzato e non può essere eliminato.";}}
    public Task<IReadOnlyList<SupplierOption>> CountriesAsync(CancellationToken ct)=>OptionsAsync("SELECT Codice,Nome FROM Nazioni ORDER BY Nome",ct);
    public Task<IReadOnlyList<SupplierOption>> PaymentsAsync(CancellationToken ct)=>OptionsAsync("SELECT Codice,Descrizione FROM Pagamenti ORDER BY Descrizione",ct);
    public Task<IReadOnlyList<SupplierOption>> BanksAsync(CancellationToken ct)=>OptionsAsync("SELECT Codice,Nome FROM Banche ORDER BY Nome",ct);
    public Task<IReadOnlyList<SupplierOption>> LocalUnitsAsync(CancellationToken ct)=>OptionsAsync("SELECT Codice,Nome FROM UnitaLocali WHERE COALESCE(Attivo,1)=1 ORDER BY Nome",ct);
    private async Task<IReadOnlyList<SupplierOption>> OptionsAsync(string sql,CancellationToken ct){var x=new List<SupplierOption>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))x.Add(new(r.GetInt32(0),S(r,1)));return x;}
    private static string S(MySqlDataReader r,int i)=>r.IsDBNull(i)?"":r.GetString(i);private static short? N16(MySqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetInt16(i);private static int? N32(MySqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetInt32(i);private static void Add(MySqlCommand c,object m){foreach(var p in m.GetType().GetProperties())c.Parameters.AddWithValue("@"+p.Name,p.GetValue(m)??DBNull.Value);}
}
