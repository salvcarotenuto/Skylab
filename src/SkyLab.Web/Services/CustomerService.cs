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
        const string sql="SELECT Codice,Nome,CodFi,Piva,Citta,Cap,Provincia,Via,Civico,Contatto,Telefono1,Telefono2,Email1,Pec,CodSDI,Notes,Attivo,COALESCE(Listino,0) FROM Clienti WHERE Codice=@id";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",id);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        string S(int i)=>r.IsDBNull(i)?"":r.GetString(i);
        return new(){Code=r.GetInt32(0),Name=S(1),TaxCode=S(2),VatNumber=S(3),City=S(4),PostalCode=S(5),Province=S(6),Street=S(7),StreetNumber=S(8),Contact=S(9),Phone1=S(10),Phone2=S(11),Email=S(12),CertifiedEmail=S(13),SdiCode=S(14),Notes=S(15),Active=!r.IsDBNull(16)&&r.GetBoolean(16),PriceList=r.GetByte(17)};
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
        const string sql="""INSERT INTO Clienti(Codice,Nome,CodFi,Piva,Citta,Cap,Provincia,Via,Civico,Contatto,Telefono1,Telefono2,Email1,Pec,CodSDI,Notes,Attivo,Listino) VALUES(@Code,@Name,@TaxCode,@VatNumber,@City,@PostalCode,@Province,@Street,@StreetNumber,@Contact,@Phone1,@Phone2,@Email,@CertifiedEmail,@SdiCode,@Notes,@Active,@PriceList) ON DUPLICATE KEY UPDATE Nome=VALUES(Nome),CodFi=VALUES(CodFi),Piva=VALUES(Piva),Citta=VALUES(Citta),Cap=VALUES(Cap),Provincia=VALUES(Provincia),Via=VALUES(Via),Civico=VALUES(Civico),Contatto=VALUES(Contatto),Telefono1=VALUES(Telefono1),Telefono2=VALUES(Telefono2),Email1=VALUES(Email1),Pec=VALUES(Pec),CodSDI=VALUES(CodSDI),Notes=VALUES(Notes),Attivo=VALUES(Attivo),Listino=VALUES(Listino)""";
        await using var cmd=new MySqlCommand(sql,cn,tx);AddModel(cmd,m);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);return m.Code;
    }

    public async Task<string> CustomerNameAsync(int id,CancellationToken ct){await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT COALESCE(Nome,'') FROM Clienti WHERE Codice=@id",cn);cmd.Parameters.AddWithValue("@id",id);return Convert.ToString(await cmd.ExecuteScalarAsync(ct))??"";}
    public async Task<IReadOnlyList<CityLookupItem>> SearchCitiesAsync(string? search,CancellationToken ct)
    {
        var term=search?.Trim()??"";if(term.Length<2)return [];
        const string sql="SELECT Nome,COALESCE(Cap,''),COALESCE(Prov,'') FROM Comuni WHERE Nome LIKE CONCAT(@term,'%') OR Cap LIKE CONCAT(@term,'%') ORDER BY Nome,Prov LIMIT 30";
        var result=new List<CityLookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@term",term);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetString(0),r.GetString(1),r.GetString(2)));return result;
    }
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
    public async Task<ArticleEditModel?> ArticleAsync(string? code,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return null;
        const string sql="SELECT Codice,Descrizione,Categoria,Gruppo,Marca,COALESCE(Uma,''),COALESCE(Uml,''),COALESCE(Umv,''),Peso,Pezzi,Durata,Consumo,CostoStd,PrezzoStd,Giacin,ScortaMin,ScortaMax,COALESCE(Ubicazione,''),COALESCE(Codiva,''),COALESCE(Notes,''),Fornitore,COALESCE(CodiceFornitore,'') FROM Articoli WHERE Codice=@code";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",code.Trim());await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        return new(){Code=S(r,0),Description=S(r,1),CategoryCode=r.GetInt16(2),GroupCode=r.GetInt16(3),BrandCode=r.GetInt16(4),PurchaseUnit=S(r,5),WorkUnit=S(r,6),SalesUnit=S(r,7),Weight=r.GetDecimal(8),Pieces=r.GetInt16(9),DurationDays=r.GetInt16(10),DailyConsumption=r.GetDecimal(11),Cost=r.GetDecimal(12),Price=r.GetDecimal(13),Stock=r.GetDecimal(14),MinimumStock=r.GetDecimal(15),MaximumStock=r.GetDecimal(16),Location=S(r,17),VatCode=S(r,18),Notes=S(r,19),SupplierCode=r.IsDBNull(20)?null:r.GetInt32(20),SupplierArticleCode=S(r,21)};
    }
    public async Task SaveArticleAsync(ArticleEditModel m,bool isNew,CancellationToken ct)
    {
        m.Code=m.Code.Trim().ToUpperInvariant();m.Description=m.Description.Trim();m.PurchaseUnit=(m.PurchaseUnit??"").Trim();m.WorkUnit=(m.WorkUnit??"").Trim();m.SalesUnit=(m.SalesUnit??"").Trim();m.SupplierArticleCode=(m.SupplierArticleCode??"").Trim();m.Location=(m.Location??"").Trim();m.VatCode=m.VatCode.Trim();m.Notes=(m.Notes??"").Trim();
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        if(isNew){await using var exists=new MySqlCommand("SELECT COUNT(*) FROM Articoli WHERE Codice=@Code",cn);exists.Parameters.AddWithValue("@Code",m.Code);if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException($"Esiste già un articolo con codice {m.Code}.");}
        const string sql="""
            INSERT INTO Articoli(Codice,Descrizione,Categoria,Gruppo,Specie,Marca,Livello,Uma,Uml,Umv,Peso,Pezzi,Durata,Consumo,Fornitore,CodiceFornitore,ScortaMin,ScortaMax,Ubicazione,Giacin,CostoStd,PrezzoStd,Codiva,Notes)
            VALUES(@Code,@Description,@CategoryCode,@GroupCode,0,@BrandCode,0,@PurchaseUnit,@WorkUnit,@SalesUnit,@Weight,@Pieces,@DurationDays,@DailyConsumption,NULLIF(@SupplierCode,0),@SupplierArticleCode,@MinimumStock,@MaximumStock,@Location,@Stock,@Cost,@Price,NULLIF(@VatCode,''),@Notes)
            ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione),Categoria=VALUES(Categoria),Gruppo=VALUES(Gruppo),Marca=VALUES(Marca),Uma=VALUES(Uma),Uml=VALUES(Uml),Umv=VALUES(Umv),Peso=VALUES(Peso),Pezzi=VALUES(Pezzi),Durata=VALUES(Durata),Consumo=VALUES(Consumo),Fornitore=VALUES(Fornitore),CodiceFornitore=VALUES(CodiceFornitore),ScortaMin=VALUES(ScortaMin),ScortaMax=VALUES(ScortaMax),Ubicazione=VALUES(Ubicazione),Giacin=VALUES(Giacin),CostoStd=VALUES(CostoStd),PrezzoStd=VALUES(PrezzoStd),Codiva=VALUES(Codiva),Notes=VALUES(Notes)
            """;
        await using var cmd=new MySqlCommand(sql,cn);AddModel(cmd,m);await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<string?> DeleteArticleAsync(string? code,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return "Selezionare un articolo da eliminare.";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        try{await using var cmd=new MySqlCommand("DELETE FROM Articoli WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code.Trim());return await cmd.ExecuteNonQueryAsync(ct)==1?null:"Articolo non trovato.";}
        catch(MySqlException ex) when(ex.Number==1451){return "L'articolo è già utilizzato e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<ArticleBarcodeItem>> ArticleBarcodesAsync(string? articleCode,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(articleCode))return [];
        const string sql="""
            SELECT b.ID,b.Barcode,b.Tipo,b.Fornitore,COALESCE(f.Nome,'')
            FROM Barcodes b LEFT JOIN Fornitori f ON f.Codice=b.Fornitore
            WHERE b.Articolo=@code ORDER BY CASE WHEN b.Tipo=0 AND b.Fornitore IS NULL THEN 0 ELSE 1 END,b.Tipo,b.ID
            """;
        var result=new List<ArticleBarcodeItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",articleCode.Trim());await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)){var value=S(r,1);var type=r.GetByte(2);var supplier=r.IsDBNull(3)?(int?)null:r.GetInt32(3);var valid=type==13&&IsValidEan13(value);result.Add(new(r.GetInt32(0),value,type,supplier,S(r,4),valid,type==13&&!valid));}return result;
    }
    public async Task SaveArticleBarcodeAsync(string articleCode,int id,string? value,int? supplierCode,CancellationToken ct)
    {
        articleCode=(articleCode??"").Trim().ToUpperInvariant();value=(value??"").Trim();if(articleCode.Length==0)throw new InvalidOperationException("Articolo non valido.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        int? supplier=supplierCode>0?supplierCode:null;if(supplier.HasValue){await using var supplierCmd=new MySqlCommand("SELECT COUNT(*) FROM Fornitori WHERE Codice=@supplier",cn);supplierCmd.Parameters.AddWithValue("@supplier",supplier.Value);if(Convert.ToInt32(await supplierCmd.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Fornitore non valido.");}
        string? oldValue=null;byte? oldType=null;if(id>0){await using var old=new MySqlCommand("SELECT Barcode,Tipo FROM Barcodes WHERE ID=@id AND Articolo=@code",cn);old.Parameters.AddWithValue("@id",id);old.Parameters.AddWithValue("@code",articleCode);await using var row=await old.ExecuteReaderAsync(ct);if(await row.ReadAsync(ct)){oldValue=row.GetString(0);oldType=row.GetByte(1);}else throw new InvalidOperationException("Barcode non trovato.");}
        if(value.Length==0&&!supplier.HasValue)value=articleCode;if(value.Length==0)throw new InvalidOperationException("Inserire il barcode.");if(value.Length>30)throw new InvalidOperationException("Il barcode non può superare 30 caratteri.");var validEan=IsValidEan13(value);if(!validEan&&value.Any(c=>c<32||c>126))throw new InvalidOperationException("Tipo di barcode non riconosciuto.");var type=(byte)(validEan?13:0);if(id>0&&oldType==13&&string.Equals(oldValue,value,StringComparison.Ordinal))type=13;
        try{await using var cmd=new MySqlCommand(id==0?"INSERT INTO Barcodes(Articolo,Barcode,Tipo,Fornitore) VALUES(@code,@value,@type,@supplier)":"UPDATE Barcodes SET Barcode=@value,Tipo=@type,Fornitore=@supplier WHERE ID=@id AND Articolo=@code",cn);cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@code",articleCode);cmd.Parameters.AddWithValue("@value",value);cmd.Parameters.AddWithValue("@type",type);cmd.Parameters.AddWithValue("@supplier",supplier is null?DBNull.Value:supplier.Value);await cmd.ExecuteNonQueryAsync(ct);}
        catch(MySqlException ex) when(ex.Number==1062){throw new InvalidOperationException("Il barcode è già presente in archivio.");}
    }
    public async Task DeleteArticleBarcodeAsync(string articleCode,int id,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("DELETE FROM Barcodes WHERE ID=@id AND Articolo=@code",cn);cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@code",articleCode.Trim());if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Barcode non trovato.");
    }
    public async Task<IReadOnlyList<ArticlePhotoItem>> ArticlePhotosAsync(string? articleCode,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(articleCode))return [];var code=articleCode.Trim();const string sql="SELECT FileName,DataOraFoto,COALESCE(Notes,'') FROM Artfoto WHERE Codice=@code ORDER BY DataOraFoto DESC,FileName";var result=new List<ArticlePhotoItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",code);await using var r=await cmd.ExecuteReaderAsync(ct);var folder=Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(code));while(await r.ReadAsync(ct)){var file=r.GetString(0);var url=file.StartsWith('/')?file:$"/uploads/articoli/{folder}/{Uri.EscapeDataString(file)}";result.Add(new(file,r.IsDBNull(1)?null:r.GetDateTime(1),r.GetString(2),url));}return result;
    }
    public async Task AddArticlePhotoAsync(string articleCode,string fileName,string description,CancellationToken ct)
    {
        const string sql="INSERT INTO Artfoto(Codice,FileName,DataOraFoto,Notes) SELECT Codice,@file,NOW(),NULLIF(@description,'') FROM Articoli WHERE Codice=@code";await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",articleCode.Trim());cmd.Parameters.AddWithValue("@file",fileName);cmd.Parameters.AddWithValue("@description",description.Trim());if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Articolo non trovato.");
    }
    public async Task<string?> DeleteArticlePhotoAsync(string articleCode,string fileName,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("DELETE FROM Artfoto WHERE Codice=@code AND FileName=@file",cn);cmd.Parameters.AddWithValue("@code",articleCode.Trim());cmd.Parameters.AddWithValue("@file",fileName);return await cmd.ExecuteNonQueryAsync(ct)==1?fileName:null;
    }
    public async Task<IReadOnlyList<ArticlePriceListEditModel>> ArticlePriceListsAsync(string? articleCode,decimal vatRate,CancellationToken ct)
    {
        var result=Enumerable.Range(1,6).Select(n=>new ArticlePriceListEditModel{ListNumber=(byte)n}).ToList();if(string.IsNullOrWhiteSpace(articleCode))return result;
        const string sql="SELECT Listino,Ricarico,Prezzo,PrIvato FROM ArtListini WHERE Articolo=@code AND Listino BETWEEN 1 AND 6 ORDER BY Listino";await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",articleCode.Trim());await using var r=await cmd.ExecuteReaderAsync(ct);var factor=1m+vatRate/100m;
        while(await r.ReadAsync(ct)){var number=r.GetByte(0);var row=result.First(x=>x.ListNumber==number);row.Markup=r.GetDecimal(1);row.Price=r.GetDecimal(2);row.VatPrice=r.GetDecimal(3);if(row.VatPrice==0&&row.Price!=0)row.VatPrice=decimal.Round(row.Price*factor,3,MidpointRounding.AwayFromZero);}return result;
    }
    public async Task<decimal> ArticleVatRateAsync(string? articleCode,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(articleCode))return 0;const string sql="SELECT COALESCE(i.Aliquota,0) FROM Articoli a LEFT JOIN Codiciiva i ON i.Codice=a.Codiva WHERE a.Codice=@code";await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",articleCode.Trim());return Convert.ToDecimal(await cmd.ExecuteScalarAsync(ct));
    }
    public async Task SaveArticlePriceListsAsync(string articleCode,IReadOnlyList<ArticlePriceListEditModel> rows,CancellationToken ct)
    {
        articleCode=(articleCode??"").Trim();if(articleCode.Length==0)throw new InvalidOperationException("Articolo non valido.");if(rows.Count!=6||rows.Select(x=>x.ListNumber).Distinct().Count()!=6||rows.Any(x=>x.ListNumber is <1 or >6))throw new InvalidOperationException("Righe listino non valide.");if(rows.Any(x=>x.Markup<0||x.Markup>999.99m||x.VatPrice<0||x.VatPrice>999999999.999m))throw new InvalidOperationException("Valori listino non validi.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);decimal vatRate;await using(var rate=new MySqlCommand("SELECT COALESCE(i.Aliquota,0) FROM Articoli a LEFT JOIN Codiciiva i ON i.Codice=a.Codiva WHERE a.Codice=@code",cn,tx)){rate.Parameters.AddWithValue("@code",articleCode);var value=await rate.ExecuteScalarAsync(ct);if(value is null)throw new InvalidOperationException("Articolo non trovato.");vatRate=Convert.ToDecimal(value);}var factor=1m+vatRate/100m;
        foreach(var row in rows){if(row.Markup==0&&row.VatPrice==0){await using var delete=new MySqlCommand("DELETE FROM ArtListini WHERE Articolo=@article AND Listino=@list",cn,tx);delete.Parameters.AddWithValue("@article",articleCode);delete.Parameters.AddWithValue("@list",row.ListNumber);await delete.ExecuteNonQueryAsync(ct);continue;}var price=decimal.Round(row.VatPrice/factor,3,MidpointRounding.AwayFromZero);await using var save=new MySqlCommand("INSERT INTO ArtListini(Articolo,Listino,Ricarico,Prezzo,PrIvato) VALUES(@article,@list,@markup,@price,@vatPrice) ON DUPLICATE KEY UPDATE Ricarico=VALUES(Ricarico),Prezzo=VALUES(Prezzo),PrIvato=VALUES(PrIvato)",cn,tx);save.Parameters.AddWithValue("@article",articleCode);save.Parameters.AddWithValue("@list",row.ListNumber);save.Parameters.AddWithValue("@markup",decimal.Round(row.Markup,2,MidpointRounding.AwayFromZero));save.Parameters.AddWithValue("@price",price);save.Parameters.AddWithValue("@vatPrice",decimal.Round(row.VatPrice,3,MidpointRounding.AwayFromZero));await save.ExecuteNonQueryAsync(ct);}await tx.CommitAsync(ct);
    }
    private static bool IsValidEan13(string value)
    {
        if(value.Length!=13||value.Any(c=>c<'0'||c>'9'))return false;var sum=0;for(var i=0;i<12;i++)sum+=(value[i]-'0')*(i%2==0?1:3);return (10-sum%10)%10==value[12]-'0';
    }
    public async Task<IReadOnlyList<LookupItem>> ArticleCategoriesAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Descrizione FROM Categorie ORDER BY Descrizione",ct);
    public async Task<IReadOnlyList<LookupItem>> InventoryGroupsAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Descrizione FROM Gruppi ORDER BY Descrizione",ct);
    public async Task<InventoryGroupEditModel?> InventoryGroupAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT Codice,COALESCE(Descrizione,'') FROM Gruppi WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?new(){Code=r.GetInt16(0),Description=r.GetString(1)}:null;
    }
    public async Task SaveInventoryGroupAsync(InventoryGroupEditModel model,bool isNew,CancellationToken ct)
    {
        model.Description=model.Description.Trim();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        if(isNew){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Gruppi FOR UPDATE",cn,tx);model.Code=Convert.ToInt16(await next.ExecuteScalarAsync(ct));}
        const string sql="INSERT INTO Gruppi(Codice,Descrizione) VALUES(@code,@description) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione)";await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<string?> DeleteInventoryGroupAsync(short code,CancellationToken ct)
    {
        try{await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("DELETE FROM Gruppi WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "Il gruppo è utilizzato da uno o più articoli e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<LookupItem>> InventoryBrandsAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Descrizione FROM Marche ORDER BY Descrizione",ct);
    public async Task<InventoryBrandEditModel?> InventoryBrandAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT Codice,COALESCE(Descrizione,'') FROM Marche WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?new(){Code=r.GetInt16(0),Description=r.GetString(1)}:null;
    }
    public async Task SaveInventoryBrandAsync(InventoryBrandEditModel model,bool isNew,CancellationToken ct)
    {
        model.Description=model.Description.Trim();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        if(isNew){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Marche FOR UPDATE",cn,tx);model.Code=Convert.ToInt16(await next.ExecuteScalarAsync(ct));}
        const string sql="INSERT INTO Marche(Codice,Descrizione) VALUES(@code,@description) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione)";await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<string?> DeleteInventoryBrandAsync(short code,CancellationToken ct)
    {
        try{await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("DELETE FROM Marche WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "Il marchio è utilizzato da uno o più articoli e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<LookupItem>> GoodsAppearancesAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Descrizione FROM Aspetto ORDER BY Descrizione",ct);
    public async Task<GoodsAppearanceEditModel?> GoodsAppearanceAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT Codice,COALESCE(Descrizione,'') FROM Aspetto WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?new(){Code=r.GetInt16(0),Description=r.GetString(1)}:null;
    }
    public async Task SaveGoodsAppearanceAsync(GoodsAppearanceEditModel model,bool isNew,CancellationToken ct)
    {
        model.Description=model.Description.Trim();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        if(isNew){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Aspetto FOR UPDATE",cn,tx);model.Code=Convert.ToInt16(await next.ExecuteScalarAsync(ct));}
        const string sql="INSERT INTO Aspetto(Codice,Descrizione) VALUES(@code,@description) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione)";await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<string?> DeleteGoodsAppearanceAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        const string usedSql="""
            SELECT SUM(UsedCount) FROM (
                SELECT COUNT(*) UsedCount FROM Ddt WHERE Aspetto=@code
                UNION ALL SELECT COUNT(*) FROM Fatture WHERE Aspetto=@code
                UNION ALL SELECT COUNT(*) FROM Preventivi WHERE Aspetto=@code
                UNION ALL SELECT COUNT(*) FROM RicFiscali WHERE Aspetto=@code
            ) x
            """;
        await using(var used=new MySqlCommand(usedSql,cn)){used.Parameters.AddWithValue("@code",code);if(Convert.ToInt32(await used.ExecuteScalarAsync(ct))>0)return "L'aspetto beni è utilizzato da uno o più documenti e non può essere eliminato.";}
        try{await using var cmd=new MySqlCommand("DELETE FROM Aspetto WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "L'aspetto beni è utilizzato e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<LookupItem>> ArticleGroupsAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Descrizione FROM Gruppi ORDER BY Descrizione",ct);
    public async Task<IReadOnlyList<LookupItem>> ArticleBrandsAsync(CancellationToken ct)=>await LookupAsync("SELECT Codice,Descrizione FROM Marche ORDER BY Descrizione",ct);
    public async Task<IReadOnlyList<CodeLookupItem>> UnitMeasuresAsync(CancellationToken ct)
    {
        const string sql="SELECT Codice,COALESCE(Descrizione,'') FROM Umisura ORDER BY Descrizione,Codice";
        var result=new List<CodeLookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(S(r,0),S(r,1)));return result;
    }
    public async Task<UnitMeasureEditModel?> UnitMeasureAsync(string? code,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return null;await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT Codice,COALESCE(Descrizione,'') FROM Umisura WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code.Trim());await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?new(){Code=S(r,0),Description=S(r,1)}:null;
    }
    public async Task SaveUnitMeasureAsync(UnitMeasureEditModel model,bool isNew,CancellationToken ct)
    {
        model.Code=model.Code.Trim().ToUpperInvariant();model.Description=model.Description.Trim();if(model.Code.Length==0)throw new InvalidOperationException("Inserire il codice unità di misura.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        if(isNew){await using var exists=new MySqlCommand("SELECT COUNT(*) FROM Umisura WHERE Codice=@code",cn);exists.Parameters.AddWithValue("@code",model.Code);if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException($"Esiste già l'unità di misura {model.Code}.");}
        const string sql="INSERT INTO Umisura(Codice,Descrizione) VALUES(@code,@description) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione)";await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<string?> DeleteUnitMeasureAsync(string? code,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return "Selezionare un'unità di misura da eliminare.";code=code.Trim();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using(var used=new MySqlCommand("SELECT COUNT(*) FROM Articoli WHERE Uma=@code OR Uml=@code OR Umv=@code",cn)){used.Parameters.AddWithValue("@code",code);if(Convert.ToInt32(await used.ExecuteScalarAsync(ct))>0)return "L'unità di misura è utilizzata da uno o più articoli e non può essere eliminata.";}
        try{await using var cmd=new MySqlCommand("DELETE FROM Umisura WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "L'unità di misura è utilizzata e non può essere eliminata.";}
    }
    public async Task<IReadOnlyList<CodeLookupItem>> VatCodesAsync(CancellationToken ct)
    {
        const string sql="SELECT Codice,CONCAT(Descrizione,' · ',FORMAT(Aliquota,2,'it_IT'),'%') FROM Codiciiva ORDER BY Descrizione,Codice";
        var result=new List<CodeLookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(S(r,0),S(r,1)));return result;
    }
    public async Task<IReadOnlyList<VatCodeListItem>> VatCodeListAsync(CancellationToken ct)
    {
        const string sql="SELECT Codice,COALESCE(Descrizione,''),Aliquota,Detrazione,COALESCE(CodiceFE,'') FROM Codiciiva ORDER BY Codice";
        var result=new List<VatCodeListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(S(r,0),S(r,1),r.GetDecimal(2),r.GetDecimal(3),S(r,4)));return result;
    }
    public async Task<IReadOnlyList<CodeLookupItem>> ElectronicInvoiceVatNaturesAsync(CancellationToken ct)
    {
        const string sql="SELECT Codice,COALESCE(Descrizione,'') FROM fecodiciiva ORDER BY Codice";
        var result=new List<CodeLookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(S(r,0),S(r,1)));return result;
    }
    public async Task SaveVatCodeAsync(VatCodeEditModel model,bool isNew,CancellationToken ct)
    {
        model.Code=model.Code.Trim().ToUpperInvariant();model.Description=model.Description.Trim();model.Nature=(model.Nature??"").Trim().ToUpperInvariant();if(model.Code.Length==0)throw new InvalidOperationException("Inserire il codice IVA.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        if(isNew){await using var exists=new MySqlCommand("SELECT COUNT(*) FROM Codiciiva WHERE Codice=@code",cn);exists.Parameters.AddWithValue("@code",model.Code);if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException($"Esiste già il codice IVA {model.Code}.");}
        const string sql="INSERT INTO Codiciiva(Codice,Descrizione,Aliquota,Detrazione,CodiceFE) VALUES(@code,@description,@rate,@deduction,NULLIF(@nature,'')) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione),Aliquota=VALUES(Aliquota),Detrazione=VALUES(Detrazione),CodiceFE=VALUES(CodiceFE)";
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);cmd.Parameters.AddWithValue("@rate",model.Rate);cmd.Parameters.AddWithValue("@deduction",model.Deduction);cmd.Parameters.AddWithValue("@nature",model.Nature);await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<string?> DeleteVatCodeAsync(string? code,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return "Selezionare un codice IVA da eliminare.";code=code.Trim();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        const string usedSql="""
            SELECT SUM(UsedCount) FROM (
                SELECT COUNT(*) UsedCount FROM Articoli WHERE Codiva=@code
                UNION ALL SELECT COUNT(*) FROM DdtRg WHERE CodIva=@code
                UNION ALL SELECT COUNT(*) FROM FattureRg WHERE CodIva=@code
                UNION ALL SELECT COUNT(*) FROM PreventiviRg WHERE CodIva=@code
                UNION ALL SELECT COUNT(*) FROM RicFiscaliRg WHERE CodIva=@code
                UNION ALL SELECT COUNT(*) FROM MovIva WHERE Codiva1=@code OR Codiva2=@code OR Codiva3=@code
            ) x
            """;
        await using(var used=new MySqlCommand(usedSql,cn)){used.Parameters.AddWithValue("@code",code);if(Convert.ToInt32(await used.ExecuteScalarAsync(ct))>0)return "Il codice IVA è utilizzato e non può essere eliminato.";}
        try{await using var cmd=new MySqlCommand("DELETE FROM Codiciiva WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "Il codice IVA è utilizzato e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<AccountMasterListItem>> AccountMastersAsync(CancellationToken ct)
    {
        const string sql="SELECT Codice,COALESCE(Descrizione,''),COALESCE(Tipo,''),COALESCE(Locked,0) FROM mastri ORDER BY Codice";
        var result=new List<AccountMasterListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt16(0),S(r,1),S(r,2),r.GetBoolean(3)));return result;
    }
    public async Task SaveAccountMasterAsync(AccountMasterEditModel model,bool isNew,CancellationToken ct)
    {
        model.Description=model.Description.Trim();model.Type=(model.Type??"").Trim().ToUpperInvariant();if(model.Description.Length==0)throw new InvalidOperationException("Inserire la descrizione del mastro.");if(model.Type is not ("P" or "C" or "R"))throw new InvalidOperationException("Tipo mastro non valido.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        if(isNew&&model.Code<=0)model.Code=await NextAccountMasterCodeAsync(cn,ct);
        if(model.Code<1||model.Code>999)throw new InvalidOperationException("Il codice mastro deve essere compreso tra 001 e 999.");
        if(isNew){await using var exists=new MySqlCommand("SELECT COUNT(*) FROM mastri WHERE Codice=@code",cn);exists.Parameters.AddWithValue("@code",model.Code);if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException($"Esiste già il mastro {model.Code:000}.");}
        else{await using var locked=new MySqlCommand("SELECT Locked FROM mastri WHERE Codice=@code",cn);locked.Parameters.AddWithValue("@code",model.Code);var value=await locked.ExecuteScalarAsync(ct);if(value is not null&&value is not DBNull&&Convert.ToBoolean(value))throw new InvalidOperationException("Il mastro è bloccato e non può essere modificato.");}
        const string sql="INSERT INTO mastri(Codice,Descrizione,Tipo,Locked) VALUES(@code,@description,@type,0) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione),Tipo=VALUES(Tipo)";
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);cmd.Parameters.AddWithValue("@type",model.Type);await cmd.ExecuteNonQueryAsync(ct);
    }
    private static async Task<short> NextAccountMasterCodeAsync(MySqlConnection cn,CancellationToken ct)
    {
        var used=new HashSet<short>();await using var cmd=new MySqlCommand("SELECT Codice FROM mastri WHERE Codice BETWEEN 1 AND 999 ORDER BY Codice",cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))used.Add(r.GetInt16(0));for(short i=1;i<=999;i++)if(!used.Contains(i))return i;throw new InvalidOperationException("Non ci sono codici mastro disponibili.");
    }
    public async Task<string?> DeleteAccountMasterAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using(var locked=new MySqlCommand("SELECT Locked FROM mastri WHERE Codice=@code",cn)){locked.Parameters.AddWithValue("@code",code);var value=await locked.ExecuteScalarAsync(ct);if(value is not null&&value is not DBNull&&Convert.ToBoolean(value))return "Il mastro è bloccato e non può essere eliminato.";}
        await using(var used=new MySqlCommand("SELECT COUNT(*) FROM conti WHERE Mastro=@code",cn)){used.Parameters.AddWithValue("@code",code);if(Convert.ToInt32(await used.ExecuteScalarAsync(ct))>0)return "Il mastro è utilizzato nel piano dei conti e non può essere eliminato.";}
        try{await using var cmd=new MySqlCommand("DELETE FROM mastri WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "Il mastro è utilizzato e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<AccountListItem>> AccountsAsync(CancellationToken ct)
    {
        const string sql="""
            SELECT c.Codice,COALESCE(c.Descrizione,''),COALESCE(c.Tipo,''),COALESCE(c.Mastro,0),COALESCE(m.Descrizione,''),COALESCE(c.Ditta,''),COALESCE(c.Locked,0),COALESCE(c.Carico,0)
            FROM conti c LEFT JOIN mastri m ON m.Codice=c.Mastro ORDER BY c.Codice
            """;
        var result=new List<AccountListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt16(0),S(r,1),S(r,2),r.GetInt16(3),S(r,4),S(r,5),r.GetBoolean(6),r.GetBoolean(7)));return result;
    }
    public async Task SaveAccountAsync(AccountEditModel model,bool isNew,CancellationToken ct)
    {
        model.Description=model.Description.Trim();model.PartyKind=(model.PartyKind??"").Trim().ToUpperInvariant();if(model.Description.Length==0)throw new InvalidOperationException("Inserire la descrizione del conto.");if(model.PartyKind is not ("" or "C" or "F" or "D" or "B"))throw new InvalidOperationException("Tipo soggetto non valido.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        if(isNew&&model.Code<=0)model.Code=await NextAccountCodeAsync(cn,ct);
        if(model.Code<1||model.Code>999)throw new InvalidOperationException("Il codice conto deve essere compreso tra 001 e 999.");
        await using(var master=new MySqlCommand("SELECT Tipo FROM mastri WHERE Codice=@master",cn)){master.Parameters.AddWithValue("@master",model.Master);var masterType=await master.ExecuteScalarAsync(ct);if(masterType is null||masterType is DBNull)throw new InvalidOperationException("Selezionare il mastro.");model.Type=Convert.ToString(masterType)?.Trim().ToUpperInvariant()??"";}
        if(model.Type is not ("P" or "C" or "R"))throw new InvalidOperationException("Tipo mastro non valido.");
        if(isNew){await using var exists=new MySqlCommand("SELECT COUNT(*) FROM conti WHERE Codice=@code",cn);exists.Parameters.AddWithValue("@code",model.Code);if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException($"Esiste già il conto {model.Code:000}.");}
        else{await using var locked=new MySqlCommand("SELECT Locked FROM conti WHERE Codice=@code",cn);locked.Parameters.AddWithValue("@code",model.Code);var value=await locked.ExecuteScalarAsync(ct);if(value is not null&&value is not DBNull&&Convert.ToBoolean(value))throw new InvalidOperationException("Il conto è bloccato e non può essere modificato.");}
        const string sql="INSERT INTO conti(Codice,Descrizione,Tipo,Mastro,Ditta,Locked,Carico) VALUES(@code,@description,@type,@master,NULLIF(@party,''),0,@load) ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione),Tipo=VALUES(Tipo),Mastro=VALUES(Mastro),Ditta=VALUES(Ditta),Carico=VALUES(Carico)";
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);cmd.Parameters.AddWithValue("@type",model.Type);cmd.Parameters.AddWithValue("@master",model.Master);cmd.Parameters.AddWithValue("@party",model.PartyKind);cmd.Parameters.AddWithValue("@load",model.Load);await cmd.ExecuteNonQueryAsync(ct);
    }
    private static async Task<short> NextAccountCodeAsync(MySqlConnection cn,CancellationToken ct)
    {
        var used=new HashSet<short>();await using var cmd=new MySqlCommand("SELECT Codice FROM conti WHERE Codice BETWEEN 1 AND 999 ORDER BY Codice",cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))used.Add(r.GetInt16(0));for(short i=1;i<=999;i++)if(!used.Contains(i))return i;throw new InvalidOperationException("Non ci sono codici conto disponibili.");
    }
    public async Task<string?> DeleteAccountAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using(var locked=new MySqlCommand("SELECT Locked FROM conti WHERE Codice=@code",cn)){locked.Parameters.AddWithValue("@code",code);var value=await locked.ExecuteScalarAsync(ct);if(value is not null&&value is not DBNull&&Convert.ToBoolean(value))return "Il conto è bloccato e non può essere eliminato.";}
        await using(var used=new MySqlCommand("SELECT SUM(UsedCount) FROM (SELECT COUNT(*) UsedCount FROM movcontrg WHERE Conto=@code UNION ALL SELECT COUNT(*) FROM banche WHERE Conto=@code) x",cn)){used.Parameters.AddWithValue("@code",code);if(Convert.ToInt32(await used.ExecuteScalarAsync(ct)??0)>0)return "Il conto è utilizzato e non può essere eliminato.";}
        try{await using var cmd=new MySqlCommand("DELETE FROM conti WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "Il conto è utilizzato e non può essere eliminato.";}
    }
    public async Task<IReadOnlyList<AccountingCauseListItem>> AccountingCausesAsync(CancellationToken ct)
    {
        const string sql="""
            SELECT Codice,COALESCE(Descrizione,''),COALESCE(TipoMov,''),COALESCE(CliFor,''),COALESCE(Segno,''),COALESCE(EnUs,''),COALESCE(TipoCau,''),COALESCE(TipoPag,''),
                   Cassa,Fattura,Scadenza,Titolo,Stipendio,Stampa,Locked,
                   Dare1,Dare2,Dare3,Dare4,Dare5,Dare6,Avere1,Avere2,Avere3,Avere4,Avere5,Avere6
            FROM causalicont ORDER BY Codice
            """;
        var result=new List<AccountingCauseListItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt16(0),S(r,1),S(r,2),S(r,3),S(r,4),S(r,5),S(r,6),S(r,7),r.GetBoolean(8),r.GetBoolean(9),r.GetBoolean(10),r.GetBoolean(11),r.GetBoolean(12),r.GetBoolean(13),r.GetBoolean(14),r.GetInt16(15),r.GetInt16(16),r.GetInt16(17),r.GetInt16(18),r.GetInt16(19),r.GetInt16(20),r.GetInt16(21),r.GetInt16(22),r.GetInt16(23),r.GetInt16(24),r.GetInt16(25),r.GetInt16(26)));return result;
    }
    public async Task<AccountingCauseEditModel?> AccountingCauseAsync(short code,CancellationToken ct)
    {
        const string sql="""
            SELECT Codice,COALESCE(Descrizione,''),COALESCE(TipoMov,''),COALESCE(CliFor,''),COALESCE(Segno,''),COALESCE(EnUs,''),COALESCE(TipoCau,''),COALESCE(TipoPag,''),
                   Cassa,Fattura,Scadenza,Titolo,Stipendio,Stampa,Locked,
                   Dare1,Dare2,Dare3,Dare4,Dare5,Dare6,Avere1,Avere2,Avere3,Avere4,Avere5,Avere6
            FROM causalicont WHERE Codice=@code
            """;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",code);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;return new(){Code=r.GetInt16(0),Description=S(r,1),MovementType=S(r,2),PartyKind=S(r,3),Sign=S(r,4),InOut=S(r,5),CauseType=S(r,6),PaymentType=S(r,7),Cash=r.GetBoolean(8),Invoice=r.GetBoolean(9),DueDate=r.GetBoolean(10),Title=r.GetBoolean(11),Salary=r.GetBoolean(12),Print=r.GetBoolean(13),Locked=r.GetBoolean(14),Debit1=r.GetInt16(15),Debit2=r.GetInt16(16),Debit3=r.GetInt16(17),Debit4=r.GetInt16(18),Debit5=r.GetInt16(19),Debit6=r.GetInt16(20),Credit1=r.GetInt16(21),Credit2=r.GetInt16(22),Credit3=r.GetInt16(23),Credit4=r.GetInt16(24),Credit5=r.GetInt16(25),Credit6=r.GetInt16(26)};
    }
    public async Task SaveAccountingCauseAsync(AccountingCauseEditModel model,bool isNew,CancellationToken ct)
    {
        model.Description=model.Description.Trim();model.MovementType=(model.MovementType??"").Trim().ToUpperInvariant();model.PartyKind=(model.PartyKind??"").Trim().ToUpperInvariant();model.Sign=(model.Sign??"").Trim().ToUpperInvariant();model.InOut=(model.InOut??"").Trim().ToUpperInvariant();model.CauseType=(model.CauseType??"").Trim().ToUpperInvariant();model.PaymentType=(model.PaymentType??"").Trim().ToUpperInvariant();
        if(model.Description.Length==0)throw new InvalidOperationException("Inserire la descrizione della causale.");if(model.MovementType.Length==0)throw new InvalidOperationException("Selezionare il tipo movimento.");if(model.MovementType is not ("C" or "B" or "V"))throw new InvalidOperationException("Tipo movimento non valido.");
        if(model.PartyKind is not ("" or "C" or "F" or "B" or "D"))throw new InvalidOperationException("Soggetto non valido.");if(model.Sign is not ("" or "A" or "D"))throw new InvalidOperationException("Segno non valido.");if(model.InOut is not ("" or "E" or "U"))throw new InvalidOperationException("Entrata/Uscita non valida.");if(model.CauseType is not ("" or "C" or "P" or "N"))throw new InvalidOperationException("Tipo causale non valido.");if(model.PaymentType is not ("" or "C" or "D" or "E" or "N"))throw new InvalidOperationException("Tipo pagamento non valido.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);if(isNew&&model.Code<=0)model.Code=await NextAccountingCauseCodeAsync(cn,ct);if(model.Code<1||model.Code>999)throw new InvalidOperationException("Il codice causale deve essere compreso tra 001 e 999.");
        if(isNew){await using var exists=new MySqlCommand("SELECT COUNT(*) FROM causalicont WHERE Codice=@code",cn);exists.Parameters.AddWithValue("@code",model.Code);if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException($"Esiste già la causale {model.Code:000}.");}
        else{await using var locked=new MySqlCommand("SELECT Locked FROM causalicont WHERE Codice=@code",cn);locked.Parameters.AddWithValue("@code",model.Code);var value=await locked.ExecuteScalarAsync(ct);if(value is not null&&value is not DBNull&&Convert.ToBoolean(value))throw new InvalidOperationException("La causale è bloccata e non può essere modificata.");}
        const string sql="""
            INSERT INTO causalicont(Codice,Descrizione,TipoMov,CliFor,Segno,EnUs,TipoCau,TipoPag,Cassa,Fattura,Scadenza,Titolo,Stipendio,Stampa,Locked,Dare1,Dare2,Dare3,Dare4,Dare5,Dare6,Avere1,Avere2,Avere3,Avere4,Avere5,Avere6)
            VALUES(@code,@description,@movement,@party,@sign,@inout,@cause,@payment,@cash,@invoice,@due,@title,@salary,@print,@locked,@d1,@d2,@d3,@d4,@d5,@d6,@c1,@c2,@c3,@c4,@c5,@c6)
            ON DUPLICATE KEY UPDATE Descrizione=VALUES(Descrizione),TipoMov=VALUES(TipoMov),CliFor=VALUES(CliFor),Segno=VALUES(Segno),EnUs=VALUES(EnUs),TipoCau=VALUES(TipoCau),TipoPag=VALUES(TipoPag),Cassa=VALUES(Cassa),Fattura=VALUES(Fattura),Scadenza=VALUES(Scadenza),Titolo=VALUES(Titolo),Stipendio=VALUES(Stipendio),Stampa=VALUES(Stampa),Locked=VALUES(Locked),Dare1=VALUES(Dare1),Dare2=VALUES(Dare2),Dare3=VALUES(Dare3),Dare4=VALUES(Dare4),Dare5=VALUES(Dare5),Dare6=VALUES(Dare6),Avere1=VALUES(Avere1),Avere2=VALUES(Avere2),Avere3=VALUES(Avere3),Avere4=VALUES(Avere4),Avere5=VALUES(Avere5),Avere6=VALUES(Avere6)
            """;
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@code",model.Code);cmd.Parameters.AddWithValue("@description",model.Description);cmd.Parameters.AddWithValue("@movement",model.MovementType);cmd.Parameters.AddWithValue("@party",model.PartyKind);cmd.Parameters.AddWithValue("@sign",model.Sign);cmd.Parameters.AddWithValue("@inout",model.InOut);cmd.Parameters.AddWithValue("@cause",model.CauseType);cmd.Parameters.AddWithValue("@payment",model.PaymentType);cmd.Parameters.AddWithValue("@cash",model.Cash);cmd.Parameters.AddWithValue("@invoice",model.Invoice);cmd.Parameters.AddWithValue("@due",model.DueDate);cmd.Parameters.AddWithValue("@title",model.Title);cmd.Parameters.AddWithValue("@salary",model.Salary);cmd.Parameters.AddWithValue("@print",model.Print);cmd.Parameters.AddWithValue("@locked",model.Locked);cmd.Parameters.AddWithValue("@d1",model.Debit1);cmd.Parameters.AddWithValue("@d2",model.Debit2);cmd.Parameters.AddWithValue("@d3",model.Debit3);cmd.Parameters.AddWithValue("@d4",model.Debit4);cmd.Parameters.AddWithValue("@d5",model.Debit5);cmd.Parameters.AddWithValue("@d6",model.Debit6);cmd.Parameters.AddWithValue("@c1",model.Credit1);cmd.Parameters.AddWithValue("@c2",model.Credit2);cmd.Parameters.AddWithValue("@c3",model.Credit3);cmd.Parameters.AddWithValue("@c4",model.Credit4);cmd.Parameters.AddWithValue("@c5",model.Credit5);cmd.Parameters.AddWithValue("@c6",model.Credit6);await cmd.ExecuteNonQueryAsync(ct);
    }
    private static async Task<short> NextAccountingCauseCodeAsync(MySqlConnection cn,CancellationToken ct)
    {
        var used=new HashSet<short>();await using var cmd=new MySqlCommand("SELECT Codice FROM causalicont WHERE Codice BETWEEN 1 AND 999 ORDER BY Codice",cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))used.Add(r.GetInt16(0));for(short i=1;i<=999;i++)if(!used.Contains(i))return i;throw new InvalidOperationException("Non ci sono codici causale disponibili.");
    }
    public async Task<string?> DeleteAccountingCauseAsync(short code,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using(var locked=new MySqlCommand("SELECT Locked FROM causalicont WHERE Codice=@code",cn)){locked.Parameters.AddWithValue("@code",code);var value=await locked.ExecuteScalarAsync(ct);if(value is not null&&value is not DBNull&&Convert.ToBoolean(value))return "La causale è bloccata e non può essere eliminata.";}
        await using(var used=new MySqlCommand("SELECT COUNT(*) FROM movcont WHERE Causale=@code",cn)){used.Parameters.AddWithValue("@code",code);if(Convert.ToInt32(await used.ExecuteScalarAsync(ct))>0)return "La causale è utilizzata nei movimenti contabili e non può essere eliminata.";}
        try{await using var cmd=new MySqlCommand("DELETE FROM causalicont WHERE Codice=@code",cn);cmd.Parameters.AddWithValue("@code",code);await cmd.ExecuteNonQueryAsync(ct);return null;}
        catch(MySqlException ex) when(ex.Number==1451){return "La causale è utilizzata e non può essere eliminata.";}
    }
    public async Task<IReadOnlyList<SupplierLookupItem>> SuppliersAsync(CancellationToken ct)
    {
        const string sql="SELECT Codice,Nome,COALESCE(Citta,''),COALESCE(Provincia,'') FROM Fornitori ORDER BY Nome,Codice";
        var result=new List<SupplierLookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),S(r,1),S(r,2),S(r,3)));return result;
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
    public async Task<int> SaveSiteAsync(SiteEditModel m,CancellationToken ct){m.Name=m.Name.Trim();m.City=(m.City??"").Trim();m.PostalCode=(m.PostalCode??"").Trim();m.Province=(m.Province??"").Trim().ToUpperInvariant();m.Street=(m.Street??"").Trim();m.StreetNumber=(m.StreetNumber??"").Trim();m.Contact=(m.Contact??"").Trim();m.ContactPhone=(m.ContactPhone??"").Trim();m.ContactEmail=(m.ContactEmail??"").Trim();m.Notes=(m.Notes??"").Trim();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);if(m.Id==0){await using var next=new MySqlCommand("SELECT COALESCE(MAX(Codice),0)+1 FROM Destini WHERE CliFor='C' AND Ditta=@customer FOR UPDATE",cn,tx);next.Parameters.AddWithValue("@customer",m.CustomerId);m.Code=Convert.ToInt32(await next.ExecuteScalarAsync(ct));}const string sql="""INSERT INTO Destini(ID,CliFor,Ditta,Codice,Nome,Citta,Cap,Provincia,Via,Civico,Contatto,TelefonoC,EmailC,Notes,Attivo) VALUES(NULLIF(@Id,0),'C',@CustomerId,@Code,@Name,@City,@PostalCode,@Province,@Street,@StreetNumber,@Contact,@ContactPhone,@ContactEmail,@Notes,@Active) ON DUPLICATE KEY UPDATE Nome=VALUES(Nome),Citta=VALUES(Citta),Cap=VALUES(Cap),Provincia=VALUES(Provincia),Via=VALUES(Via),Civico=VALUES(Civico),Contatto=VALUES(Contatto),TelefonoC=VALUES(TelefonoC),EmailC=VALUES(EmailC),Notes=VALUES(Notes),Attivo=VALUES(Attivo)""";await using var cmd=new MySqlCommand(sql,cn,tx);AddModel(cmd,m);await cmd.ExecuteNonQueryAsync(ct);if(m.Id==0)m.Id=(int)cmd.LastInsertedId;await tx.CommitAsync(ct);return m.Id;}
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
