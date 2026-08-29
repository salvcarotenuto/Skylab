using MySqlConnector;
using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public sealed class CustomerService(IConfiguration configuration)
{
    private string ConnectionString
    {
        get
        {
            var configured = configuration.GetConnectionString("SkyLabDb") ?? configuration.GetConnectionString("MicronoteDb")
                ?? throw new InvalidOperationException("Connessione MySQL SkyLab non configurata.");
            var builder = new MySqlConnectionStringBuilder(configured) { Database = "skylab_0001", SslMode = MySqlSslMode.None };
            return builder.ConnectionString;
        }
    }

    public async Task<IReadOnlyList<CustomerListItem>> SearchAsync(string? search, bool includeInactive, CancellationToken ct)
    {
        const string sql = """
            SELECT c.Codice,COALESCE(c.Nome,''),COALESCE(c.Citta,''),COALESCE(c.Provincia,''),COALESCE(c.Telefono1,''),COALESCE(c.Email1,''),COALESCE(c.Attivo,0),
                   (SELECT COUNT(*) FROM Destini d WHERE d.CliFor='C' AND d.Ditta=c.Codice),
                   (SELECT COUNT(*) FROM MacchineCli m WHERE m.Cliente=c.Codice)
            FROM Clienti c WHERE (@q='' OR c.Nome LIKE CONCAT('%',@q,'%') OR c.Codice=@code)
              AND (@inactive=1 OR COALESCE(c.Attivo,0)=1) ORDER BY c.Nome LIMIT 300
            """;
        var result = new List<CustomerListItem>();
        await using var cn = new MySqlConnection(ConnectionString); await cn.OpenAsync(ct);
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@q", search?.Trim() ?? ""); cmd.Parameters.AddWithValue("@code", int.TryParse(search, out var code) ? code : -1); cmd.Parameters.AddWithValue("@inactive", includeInactive);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetBoolean(6),r.GetInt32(7),r.GetInt32(8)));
        return result;
    }

    public async Task<CustomerEditModel?> CustomerAsync(int id, CancellationToken ct)
    {
        const string sql="SELECT Codice,Nome,CodFi,Piva,Citta,Cap,Provincia,Via,Civico,Contatto,Telefono1,Telefono2,Email1,Pec,CodSDI,Notes,Attivo FROM Clienti WHERE Codice=@id";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",id);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        string S(int i)=>r.IsDBNull(i)?"":r.GetString(i);
        return new(){Code=r.GetInt32(0),Name=S(1),TaxCode=S(2),VatNumber=S(3),City=S(4),PostalCode=S(5),Province=S(6),Street=S(7),StreetNumber=S(8),Contact=S(9),Phone1=S(10),Phone2=S(11),Email=S(12),CertifiedEmail=S(13),SdiCode=S(14),Notes=S(15),Active=!r.IsDBNull(16)&&r.GetBoolean(16)};
    }

    public async Task<int> SaveCustomerAsync(CustomerEditModel m, CancellationToken ct)
    {
        m.Name=m.Name.Trim();
        m.TaxCode=m.TaxCode?.Trim()??"";m.VatNumber=m.VatNumber?.Trim()??"";m.City=m.City?.Trim()??"";
        m.PostalCode=m.PostalCode?.Trim()??"";m.Province=m.Province?.Trim()??"";m.Street=m.Street?.Trim()??"";
        m.StreetNumber=m.StreetNumber?.Trim()??"";m.Contact=m.Contact?.Trim()??"";m.Phone1=m.Phone1?.Trim()??"";
        m.Phone2=m.Phone2?.Trim()??"";m.Email=m.Email?.Trim()??"";m.CertifiedEmail=m.CertifiedEmail?.Trim()??"";
        m.SdiCode=m.SdiCode?.Trim()??"";m.Notes=m.Notes?.Trim()??"";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        if(m.Code==0){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Clienti FOR UPDATE",cn,tx);m.Code=Convert.ToInt32(await next.ExecuteScalarAsync(ct));}
        const string sql="""INSERT INTO Clienti(Codice,Nome,CodFi,Piva,Citta,Cap,Provincia,Via,Civico,Contatto,Telefono1,Telefono2,Email1,Pec,CodSDI,Notes,Attivo) VALUES(@Code,@Name,@TaxCode,@VatNumber,@City,@PostalCode,@Province,@Street,@StreetNumber,@Contact,@Phone1,@Phone2,@Email,@CertifiedEmail,@SdiCode,@Notes,@Active) ON DUPLICATE KEY UPDATE Nome=VALUES(Nome),CodFi=VALUES(CodFi),Piva=VALUES(Piva),Citta=VALUES(Citta),Cap=VALUES(Cap),Provincia=VALUES(Provincia),Via=VALUES(Via),Civico=VALUES(Civico),Contatto=VALUES(Contatto),Telefono1=VALUES(Telefono1),Telefono2=VALUES(Telefono2),Email1=VALUES(Email1),Pec=VALUES(Pec),CodSDI=VALUES(CodSDI),Notes=VALUES(Notes),Attivo=VALUES(Attivo)""";
        await using var cmd=new MySqlCommand(sql,cn,tx);AddModel(cmd,m);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);return m.Code;
    }

    public async Task<string> CustomerNameAsync(int id,CancellationToken ct){await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT COALESCE(Nome,'') FROM Clienti WHERE Codice=@id",cn);cmd.Parameters.AddWithValue("@id",id);return Convert.ToString(await cmd.ExecuteScalarAsync(ct))??"";}
    public async Task<string> ArticleDescriptionAsync(string? code,CancellationToken ct){if(string.IsNullOrWhiteSpace(code))return "";await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT COALESCE(Descrizione,'') FROM Articoli WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code.Trim());return Convert.ToString(await cmd.ExecuteScalarAsync(ct))??"";}
    public async Task<ArticleDetail?> ArticleDetailAsync(string? code,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return null;
        const string sql="""
            SELECT COALESCE(a.Codice,''),COALESCE(a.Descrizione,''),
                   COALESCE(c.Descrizione,''),COALESCE(g.Descrizione,''),COALESCE(m.Descrizione,''),
                   COALESCE(a.Uma,''),COALESCE(a.Uml,''),COALESCE(a.Umv,''),COALESCE(a.Peso,0),COALESCE(a.Pezzi,0),
                   COALESCE(a.Durata,0),COALESCE(a.Consumo,0),COALESCE(a.CostoStd,0),COALESCE(a.PrezzoStd,0),
                   COALESCE(a.Giacin,0),COALESCE(a.ScortaMin,0),COALESCE(a.ScortaMax,0),COALESCE(a.Ubicazione,''),COALESCE(a.Notes,'')
            FROM Articoli a
            LEFT JOIN Categorie c ON c.Codice=a.Categoria
            LEFT JOIN Gruppi g ON g.Codice=a.Gruppo
            LEFT JOIN Marche m ON m.Codice=a.Marca
            WHERE a.Codice=@code
            """;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",code.Trim());await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        return new(S(r,0),S(r,1),S(r,2),S(r,3),S(r,4),S(r,5),S(r,6),S(r,7),r.GetDecimal(8),r.GetInt16(9),r.GetInt16(10),r.GetDecimal(11),r.GetDecimal(12),r.GetDecimal(13),r.GetDecimal(14),r.GetDecimal(15),r.GetDecimal(16),S(r,17),S(r,18));
    }
    public async Task<IReadOnlyList<ArticleChoice>> ArticleChoicesAsync(CancellationToken ct){const string sql="SELECT COALESCE(a.Codice,''),COALESCE(a.Descrizione,''),COALESCE(a.Categoria,0),COALESCE(c.Descrizione,''),COALESCE(a.PrezzoStd,0),COALESCE(a.Durata,0),COALESCE(a.Consumo,0) FROM Articoli a LEFT JOIN Categorie c ON c.Codice=a.Categoria ORDER BY a.Descrizione,a.Codice";var result=new List<ArticleChoice>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(S(r,0),S(r,1),r.GetInt16(2),S(r,3),r.GetDecimal(4),r.GetInt16(5),r.GetDecimal(6)));return result;}
    public async Task<IReadOnlyList<ArticleListItem>> ArticlesAsync(CancellationToken ct)
    {
        const string sql="""
            SELECT COALESCE(a.Codice,''),COALESCE(a.Descrizione,''),COALESCE(a.Categoria,0),COALESCE(c.Descrizione,''),
                   COALESCE(a.Gruppo,0),COALESCE(g.Descrizione,''),COALESCE(a.Marca,0),COALESCE(m.Descrizione,''),COALESCE(a.Umv,''),
                   COALESCE(a.PrezzoStd,0),COALESCE(a.Durata,0),COALESCE(a.Consumo,0)
            FROM Articoli a
            LEFT JOIN Categorie c ON c.Codice=a.Categoria
            LEFT JOIN Gruppi g ON g.Codice=a.Gruppo
            LEFT JOIN Marche m ON m.Codice=a.Marca
            ORDER BY a.Descrizione,a.Codice
            """;
        var result=new List<ArticleListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(S(r,0),S(r,1),r.GetInt16(2),S(r,3),r.GetInt16(4),S(r,5),r.GetInt16(6),S(r,7),S(r,8),r.GetDecimal(9),r.GetInt16(10),r.GetDecimal(11)));return result;
    }
    public async Task<IReadOnlyList<SiteListItem>> SitesAsync(int customerId,CancellationToken ct){const string sql="SELECT ID,Codice,Nome,Citta,Provincia,CONCAT_WS(' ',Via,Civico),Contatto,Attivo FROM Destini WHERE CliFor='C' AND Ditta=@id ORDER BY Nome";var x=new List<SiteListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",customerId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))x.Add(new(r.GetInt32(0),r.GetInt32(1),S(r,2),S(r,3),S(r,4),S(r,5),S(r,6),!r.IsDBNull(7)&&r.GetBoolean(7)));return x;}
    public async Task<SiteEditModel?> SiteAsync(int id,CancellationToken ct){const string sql="SELECT ID,Ditta,Codice,Nome,Citta,Cap,Provincia,Via,Civico,Contatto,TelefonoC,EmailC,Notes,Attivo FROM Destini WHERE ID=@id AND CliFor='C'";await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",id);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;return new(){Id=r.GetInt32(0),CustomerId=r.GetInt32(1),Code=r.GetInt32(2),Name=S(r,3),City=S(r,4),PostalCode=S(r,5),Province=S(r,6),Street=S(r,7),StreetNumber=S(r,8),Contact=S(r,9),ContactPhone=S(r,10),ContactEmail=S(r,11),Notes=S(r,12),Active=!r.IsDBNull(13)&&r.GetBoolean(13)};}
    public async Task<int> SaveSiteAsync(SiteEditModel m,CancellationToken ct){await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);if(m.Id==0){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Destini WHERE CliFor='C' AND Ditta=@customer FOR UPDATE",cn,tx);next.Parameters.AddWithValue("@customer",m.CustomerId);m.Code=Convert.ToInt32(await next.ExecuteScalarAsync(ct));}const string sql="""INSERT INTO Destini(ID,CliFor,Ditta,Codice,Nome,Citta,Cap,Provincia,Via,Civico,Contatto,TelefonoC,EmailC,Notes,Attivo) VALUES(NULLIF(@Id,0),'C',@CustomerId,@Code,@Name,@City,@PostalCode,@Province,@Street,@StreetNumber,@Contact,@ContactPhone,@ContactEmail,@Notes,@Active) ON DUPLICATE KEY UPDATE Nome=VALUES(Nome),Citta=VALUES(Citta),Cap=VALUES(Cap),Provincia=VALUES(Provincia),Via=VALUES(Via),Civico=VALUES(Civico),Contatto=VALUES(Contatto),TelefonoC=VALUES(TelefonoC),EmailC=VALUES(EmailC),Notes=VALUES(Notes),Attivo=VALUES(Attivo)""";await using var cmd=new MySqlCommand(sql,cn,tx);AddModel(cmd,m);await cmd.ExecuteNonQueryAsync(ct);if(m.Id==0)m.Id=(int)cmd.LastInsertedId;await tx.CommitAsync(ct);return m.Id;}
    public async Task<MachineEditModel?> MachineAsync(int id,CancellationToken ct){const string sql="SELECT ID,Cliente,DestinoID,Articolo,Categoria,DataRif,Valore,Durata,QuantitaFornita,ConsumoGiornaliero,ProxData FROM MacchineCli WHERE ID=@id";await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",id);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;return new(){Id=r.GetInt32(0),CustomerId=r.GetInt32(1),SiteId=r.IsDBNull(2)?null:r.GetInt32(2),ArticleCode=S(r,3),CategoryId=r.IsDBNull(4)?null:r.GetInt16(4),InstalledOn=r.IsDBNull(5)?null:r.GetDateTime(5),Value=r.IsDBNull(6)?null:r.GetDecimal(6),DurationDays=r.IsDBNull(7)?null:r.GetInt16(7),SuppliedQuantity=r.IsDBNull(8)?null:r.GetDecimal(8),DailyConsumption=r.IsDBNull(9)?null:r.GetDecimal(9),NextServiceOn=r.IsDBNull(10)?null:r.GetDateTime(10)};}
    public async Task<IReadOnlyList<OperationalMachine>> OperationalMachinesAsync(int customerId,CancellationToken ct)
    {
        const string sql="""
            SELECT m.ID,COALESCE(m.Articolo,''),COALESCE(a.Descrizione,''),COALESCE(cat.Descrizione,''),m.Valore,m.DataRif,m.ProxData,
                   d.ID,COALESCE(d.Nome,''),CONCAT_WS(' ',NULLIF(d.Via,''),NULLIF(d.Civico,''),NULLIF(d.Citta,''))
            FROM MacchineCli m
            LEFT JOIN Articoli a ON a.Codice=m.Articolo
            LEFT JOIN Categorie cat ON cat.Codice=m.Categoria
            LEFT JOIN Destini d ON d.ID=m.DestinoID AND d.Ditta=m.Cliente AND d.CliFor='C'
            WHERE m.Cliente=@customer ORDER BY COALESCE(d.Nome,''),a.Descrizione,m.Articolo
            """;
        var result=new List<OperationalMachine>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@customer",customerId);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),S(r,1),S(r,2),S(r,3),r.IsDBNull(4)?null:r.GetDecimal(4),r.IsDBNull(5)?null:r.GetDateTime(5),r.IsDBNull(6)?null:r.GetDateTime(6),r.IsDBNull(7)?null:r.GetInt32(7),S(r,8),S(r,9)));return result;
    }
    public async Task<int> SaveMachineAsync(MachineEditModel m,CancellationToken ct){if(m.NextServiceOn is null&&m.InstalledOn.HasValue){var days=m.CategoryId==3&&m.SuppliedQuantity>0&&m.DailyConsumption>0?(int)Math.Ceiling(m.SuppliedQuantity.Value/m.DailyConsumption.Value):m.DurationDays.GetValueOrDefault();if(days>0)m.NextServiceOn=m.InstalledOn.Value.AddDays(days);}await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);short row=0;if(m.Id==0){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Riga),0)+1 FROM MacchineCli WHERE Cliente=@customer FOR UPDATE",cn,tx);next.Parameters.AddWithValue("@customer",m.CustomerId);row=Convert.ToInt16(await next.ExecuteScalarAsync(ct));}const string sql="""INSERT INTO MacchineCli(ID,Cliente,DestinoID,Riga,Articolo,Categoria,DataRif,Valore,Durata,QuantitaFornita,ConsumoGiornaliero,ProxData) VALUES(NULLIF(@Id,0),@CustomerId,@SiteId,@Row,@ArticleCode,@CategoryId,@InstalledOn,@Value,@DurationDays,@SuppliedQuantity,@DailyConsumption,@NextServiceOn) ON DUPLICATE KEY UPDATE Cliente=VALUES(Cliente),DestinoID=VALUES(DestinoID),Articolo=VALUES(Articolo),Categoria=VALUES(Categoria),DataRif=VALUES(DataRif),Valore=VALUES(Valore),Durata=VALUES(Durata),QuantitaFornita=VALUES(QuantitaFornita),ConsumoGiornaliero=VALUES(ConsumoGiornaliero),ProxData=VALUES(ProxData)""";await using var cmd=new MySqlCommand(sql,cn,tx);AddModel(cmd,m);cmd.Parameters.AddWithValue("@Row",row);await cmd.ExecuteNonQueryAsync(ct);if(m.Id==0)m.Id=(int)cmd.LastInsertedId;await tx.CommitAsync(ct);return m.Id;}
    public async Task<bool> DeleteMachineAsync(int id,int customerId,CancellationToken ct){await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("DELETE FROM MacchineCli WHERE ID=@id AND Cliente=@customer",cn);cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@customer",customerId);return await cmd.ExecuteNonQueryAsync(ct)==1;}
    public async Task<IReadOnlyList<LookupItem>> CustomerLookupAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Nome FROM Clienti ORDER BY Nome",ct);
    public async Task<IReadOnlyList<LookupItem>> SiteLookupAsync(int customer,CancellationToken ct)=>await LookupAsync("SELECT ID,CONCAT(Nome,' · ',Citta) FROM Destini WHERE CliFor='C' AND Ditta="+customer+" ORDER BY Nome",ct);
    public async Task<IReadOnlyList<LookupItem>> ArticleLookupAsync(CancellationToken ct)=>await LookupAsync("SELECT 0,CONCAT(Codice,' · ',Descrizione) FROM Articoli ORDER BY Descrizione",ct,true);
    private async Task<IReadOnlyList<LookupItem>> LookupAsync(string sql,CancellationToken ct,bool stringId=false){var x=new List<LookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))x.Add(new(stringId?0:r.GetInt32(0),S(r,1)));return x;}
    private static string S(MySqlDataReader r,int i)=>r.IsDBNull(i)?"":r.GetString(i);
    private static void AddModel(MySqlCommand cmd,object model){foreach(var p in model.GetType().GetProperties())cmd.Parameters.AddWithValue("@"+p.Name,p.GetValue(model)??DBNull.Value);}
}
