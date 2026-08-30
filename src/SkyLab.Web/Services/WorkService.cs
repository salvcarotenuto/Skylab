using MySqlConnector;
using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public sealed class WorkService(IConfiguration configuration)
{
    private string ConnectionString
    {
        get
        {
            var configured = configuration.GetConnectionString("SkyLabDb")
                ?? configuration.GetConnectionString("MicronoteDb")
                ?? throw new InvalidOperationException("Connessione MySQL SkyLab non configurata.");
            var builder = new MySqlConnectionStringBuilder(configured)
            {
                Database = "skylab_0001",
                SslMode = MySqlSslMode.None
            };
            return builder.ConnectionString;
        }
    }

    public async Task<IReadOnlyList<WorkLookupItem>> StatusesAsync(CancellationToken cancellationToken) =>
        await LoadLookupAsync("SELECT ID, Descrizione FROM StatiLavoro ORDER BY Ordine", cancellationToken);

    public async Task<IReadOnlyList<WorkLookupItem>> OutcomesAsync(CancellationToken cancellationToken) =>
        await LoadLookupAsync("SELECT ID, Descrizione FROM EsitiLavoro ORDER BY Ordine", cancellationToken);

    public async Task<IReadOnlyList<WorkListItem>> SearchAsync(
        DateTime from, string order, byte statusId, byte outcomeId, CancellationToken cancellationToken)
    {
        var dateColumn = order == "lavoro" ? "l.DataInterventoPianificata" : "l.DataRedazione";
        var sql = $"""
            SELECT l.ID, l.Anno, l.Codice, l.DataRedazione, l.DataInterventoPianificata,
                   l.OraInterventoPianificata, l.Cliente, COALESCE(c.Nome,''),
                   COALESCE(l.DescrizioneSintetica,''), COALESCE(u.Username,''),
                   l.StatoLavoro_ID, s.Descrizione, l.EsitoLavoro_ID, COALESCE(e.Descrizione,''),
                   l.ImportoPreventivato, l.ImportoRichiesto, l.Fattura_ID
            FROM Lavori l
            LEFT JOIN Clienti c ON c.Codice = l.Cliente
            LEFT JOIN Utenti u ON u.Codice = l.OperatoreAssegnato
            INNER JOIN StatiLavoro s ON s.ID = l.StatoLavoro_ID
            LEFT JOIN EsitiLavoro e ON e.ID = l.EsitoLavoro_ID
            WHERE {dateColumn} >= @from
              AND (@statusId = 0 OR l.StatoLavoro_ID = @statusId)
              AND (@outcomeId = 0 OR l.EsitoLavoro_ID = @outcomeId)
            ORDER BY {dateColumn} DESC, l.OraInterventoPianificata DESC, l.ID DESC
            """;

        var result = new List<WorkListItem>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@from", from.Date);
        command.Parameters.AddWithValue("@statusId", statusId);
        command.Parameters.AddWithValue("@outcomeId", outcomeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new(
                reader.GetInt32(0), reader.GetInt16(1), reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetTimeSpan(5),
                reader.GetInt32(6), reader.GetString(7), reader.GetString(8), reader.GetString(9),
                reader.GetByte(10), reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetByte(12), reader.GetString(13),
                reader.GetDecimal(14), reader.GetDecimal(15),
                reader.IsDBNull(16) ? null : reader.GetInt32(16)));
        }
        return result;
    }

    public async Task<DateTime?> LatestDateAsync(string order, CancellationToken cancellationToken)
    {
        var column = order == "lavoro" ? "DataInterventoPianificata" : "DataRedazione";
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand($"SELECT MAX({column}) FROM Lavori", connection);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToDateTime(value);
    }

    public async Task<WorkEditModel?> WorkAsync(int id, CancellationToken ct)
    {
        const string sql = """
            SELECT l.ID,l.Anno,l.Codice,l.Cliente,COALESCE(c.Nome,''),l.DataRedazione,
                   l.DataInterventoPianificata,l.OraInterventoPianificata,l.DataUltimoIntervento,
                   l.OperatoreAssegnato,l.StatoLavoro_ID,l.EsitoLavoro_ID,
                   COALESCE(l.DescrizioneSintetica,''),COALESCE(l.IstruzioniOperative,''),
                   l.ImportoManodoperaPreventivato,l.ImportoMaterialiPreventivato,l.ImportoPreventivoNetto,
                   l.DataInterventoEffettiva,l.OraInterventoEffettiva,l.OperatoreEsecutore,
                   l.OreUomoConsuntive,COALESCE(l.AttivitaEseguita,''),
                   l.ImportoManodoperaConsuntivo,l.ImportoMaterialiConsuntivo,
                   l.ImportoRichiesto,l.ImportoIncassato,l.Fattura_ID,COALESCE(l.NoteConsuntive,'')
            FROM Lavori l LEFT JOIN Clienti c ON c.Codice=l.Cliente WHERE l.ID=@id
            """;
        await using var cn=new MySqlConnection(ConnectionString); await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn); cmd.Parameters.AddWithValue("@id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) return null;
        return new WorkEditModel {
            Id=r.GetInt32(0),Year=r.GetInt16(1),Code=r.GetInt32(2),CustomerId=r.GetInt32(3),Customer=r.GetString(4),
            DraftedOn=D(r,5),PlannedOn=D(r,6),PlannedAt=T(r,7),LastServiceOn=D(r,8),AssignedOperator=r.GetInt16(9),
            StatusId=r.GetByte(10),OutcomeId=r.IsDBNull(11)?null:r.GetByte(11),Summary=r.GetString(12),Instructions=r.GetString(13),
            PlannedLabour=r.GetDecimal(14),PlannedMaterials=r.GetDecimal(15),PlannedNet=r.GetDecimal(16),CompletedOn=D(r,17),CompletedAt=T(r,18),
            ExecutingOperator=r.IsDBNull(19)?null:r.GetInt16(19),ManHours=r.IsDBNull(20)?null:r.GetDecimal(20),WorkPerformed=r.GetString(21),
            ActualLabour=r.GetDecimal(22),ActualMaterials=r.GetDecimal(23),RequestedAmount=r.GetDecimal(24),CollectedAmount=r.GetDecimal(25),
            InvoiceId=r.IsDBNull(26)?null:r.GetInt32(26),Notes=r.GetString(27)
        };
    }

    public async Task<IReadOnlyList<OperatorLookupItem>> OperatorsAsync(CancellationToken ct)
    {
        var x=new List<OperatorLookupItem>();
        await using var cn=new MySqlConnection(ConnectionString); await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand("SELECT Codice,COALESCE(Username,'') FROM Utenti ORDER BY Username",cn);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) x.Add(new(r.GetInt16(0),r.GetString(1)));
        return x;
    }

    public async Task<IReadOnlyList<WorkDetailItem>> PlannedDetailsAsync(int workId,CancellationToken ct)
    {
        const string sql="""
            SELECT r.Riga,
                   COALESCE(r.TipoRiga,CASE WHEN a.Codice IS NOT NULL THEN 'A' WHEN p.Codice IS NOT NULL THEN 'P' ELSE '?' END),
                   COALESCE(r.Articolo,''),
                   CASE WHEN a.Codice IS NOT NULL THEN COALESCE(a.Descrizione,'')
                        WHEN p.Codice IS NOT NULL THEN COALESCE(p.Descrizione,'') ELSE '' END,
                   COALESCE(r.Quantita,0),COALESCE(r.Prezzo,0),r.TipoRiga IS NULL
            FROM LavoriRg r
            LEFT JOIN Articoli a ON a.Codice=TRIM(r.Articolo)
            LEFT JOIN Prestazioni p ON CAST(p.Codice AS CHAR)=TRIM(r.Articolo)
                 OR LPAD(CAST(p.Codice AS CHAR),3,'0')=TRIM(r.Articolo)
            WHERE r.ID=@id ORDER BY r.Riga
            """;
        var result=new List<WorkDetailItem>();
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",workId);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt16(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetDecimal(4),r.GetDecimal(5),r.GetBoolean(6)));
        return result;
    }

    public async Task<IReadOnlyList<WorkDetailItem>> ActualDetailsAsync(int workId,CancellationToken ct)
    {
        const string sql="""
            SELECT r.Riga,
                   COALESCE(r.TipoRiga,CASE WHEN a.Codice IS NOT NULL THEN 'A' WHEN p.Codice IS NOT NULL THEN 'P' ELSE '?' END),
                   COALESCE(r.Articolo,''),
                   CASE WHEN a.Codice IS NOT NULL THEN COALESCE(a.Descrizione,'')
                        WHEN p.Codice IS NOT NULL THEN COALESCE(p.Descrizione,'') ELSE '' END,
                   COALESCE(r.Quantita,0),COALESCE(r.Prezzo,0),r.TipoRiga IS NULL
            FROM LavoriChiusiRg r
            LEFT JOIN Articoli a ON a.Codice=TRIM(r.Articolo)
            LEFT JOIN Prestazioni p ON CAST(p.Codice AS CHAR)=TRIM(r.Articolo)
                 OR LPAD(CAST(p.Codice AS CHAR),3,'0')=TRIM(r.Articolo)
            WHERE r.ID=@id ORDER BY r.Riga
            """;
        var result=new List<WorkDetailItem>();
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",workId);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt16(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetDecimal(4),r.GetDecimal(5),r.GetBoolean(6)));
        return result;
    }

    public async Task<IReadOnlyList<WorkReferenceLookup>> WorkReferencesAsync(CancellationToken ct)
    {
        const string sql="""
            SELECT 'A',a.Codice,COALESCE(a.Descrizione,''),COALESCE(c.Descrizione,''),COALESCE(a.PrezzoStd,0)
            FROM Articoli a LEFT JOIN Categorie c ON c.Codice=a.Categoria
            UNION ALL
            SELECT 'P',CAST(p.Codice AS CHAR),p.Descrizione,'Prestazione',COALESCE(p.Prezzo,0)
            FROM Prestazioni p ORDER BY 1,3
            """;
        var x=new List<WorkReferenceLookup>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))x.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetDecimal(4)));
        return x;
    }

    public async Task<IReadOnlyList<WorkPhotoItem>> PhotosAsync(int workId,CancellationToken ct)
    {
        const string sql="SELECT Numero,FileName,DataOraFoto,COALESCE(Notes,'') FROM Lavorifoto WHERE ID=@id ORDER BY Numero";
        var x=new List<WorkPhotoItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",workId);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)){var file=r.GetString(1);var url=file.StartsWith('/')?file:$"/uploads/lavori/{workId}/{Uri.EscapeDataString(file)}";x.Add(new(r.GetInt16(0),file,r.IsDBNull(2)?null:r.GetDateTime(2),r.GetString(3),url));}
        return x;
    }

    public async Task AddPhotoAsync(int workId,string fileName,string description,CancellationToken ct)
    {
        const string sql="""
            INSERT INTO Lavorifoto(ID,Numero,Anno,Cliente,Articolo,Codice,FileName,DataOraFoto,Notes)
            SELECT l.ID,COALESCE((SELECT MAX(f.Numero)+1 FROM Lavorifoto f WHERE f.ID=l.ID),1),l.Anno,l.Cliente,NULL,l.Codice,@file,NOW(),NULLIF(@description,'')
            FROM Lavori l WHERE l.ID=@id
            """;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);
        cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@file",fileName);cmd.Parameters.AddWithValue("@description",description.Trim());
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Scheda lavoro non trovata.");
    }

    public async Task<string?> DeletePhotoAsync(int workId,short number,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        string? file=null;await using(var find=new MySqlCommand("SELECT FileName FROM Lavorifoto WHERE ID=@id AND Numero=@number",cn,tx)){find.Parameters.AddWithValue("@id",workId);find.Parameters.AddWithValue("@number",number);file=Convert.ToString(await find.ExecuteScalarAsync(ct));}
        if(string.IsNullOrWhiteSpace(file))return null;
        await using(var del=new MySqlCommand("DELETE FROM Lavorifoto WHERE ID=@id AND Numero=@number",cn,tx)){del.Parameters.AddWithValue("@id",workId);del.Parameters.AddWithValue("@number",number);await del.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);return file;
    }

    public async Task<IReadOnlyList<WorkDocumentItem>> DocumentsAsync(int workId,CancellationToken ct)
    {
        const string sql="SELECT Numero,NomeFile,NomeOriginale,DataOra,COALESCE(Descrizione,'') FROM LavoriDocumenti WHERE Lavoro_ID=@id ORDER BY Numero";
        var x=new List<WorkDocumentItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",workId);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)){var file=r.GetString(1);x.Add(new(r.GetInt16(0),file,r.GetString(2),r.GetDateTime(3),r.GetString(4),$"/uploads/lavori/{workId}/documenti/{Uri.EscapeDataString(file)}"));}
        return x;
    }

    public async Task AddDocumentAsync(int workId,string fileName,string originalName,string description,CancellationToken ct)
    {
        const string sql="""
            INSERT INTO LavoriDocumenti(Lavoro_ID,Numero,NomeFile,NomeOriginale,DataOra,Descrizione)
            SELECT l.ID,COALESCE((SELECT MAX(d.Numero)+1 FROM LavoriDocumenti d WHERE d.Lavoro_ID=l.ID),1),@file,@original,NOW(),NULLIF(@description,'')
            FROM Lavori l WHERE l.ID=@id
            """;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);
        cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@file",fileName);cmd.Parameters.AddWithValue("@original",originalName);cmd.Parameters.AddWithValue("@description",description.Trim());
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Scheda lavoro non trovata.");
    }

    public async Task<string?> DeleteDocumentAsync(int workId,short number,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        string? file=null;await using(var find=new MySqlCommand("SELECT NomeFile FROM LavoriDocumenti WHERE Lavoro_ID=@id AND Numero=@number",cn,tx)){find.Parameters.AddWithValue("@id",workId);find.Parameters.AddWithValue("@number",number);file=Convert.ToString(await find.ExecuteScalarAsync(ct));}
        if(string.IsNullOrWhiteSpace(file))return null;
        await using(var del=new MySqlCommand("DELETE FROM LavoriDocumenti WHERE Lavoro_ID=@id AND Numero=@number",cn,tx)){del.Parameters.AddWithValue("@id",workId);del.Parameters.AddWithValue("@number",number);await del.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);return file;
    }

    public async Task<IReadOnlyList<WorkHistoryItem>> HistoryAsync(int workId,CancellationToken ct)
    {
        const string sql="""
            SELECT h.ID,h.DataEvento,h.TipoEvento,
                   COALESCE(sp.Descrizione,''),COALESCE(sn.Descrizione,''),COALESCE(ep.Descrizione,''),COALESCE(en.Descrizione,''),
                   h.DataScadenzaPrecedente,h.DataScadenzaNuova,h.DataPianificataPrecedente,h.DataPianificataNuova,
                   h.DataInterventoEffettiva,h.DataSaltata,h.DataRiallineata,COALESCE(h.Note,''),COALESCE(u.Username,'')
            FROM LavoriStorico h
            LEFT JOIN StatiLavoro sp ON sp.ID=h.StatoPrecedente_ID
            LEFT JOIN StatiLavoro sn ON sn.ID=h.StatoNuovo_ID
            LEFT JOIN EsitiLavoro ep ON ep.ID=h.EsitoPrecedente_ID
            LEFT JOIN EsitiLavoro en ON en.ID=h.EsitoNuovo_ID
            LEFT JOIN Utenti u ON u.Codice=h.Utente_ID
            WHERE h.Lavoro_ID=@id ORDER BY h.DataEvento DESC,h.ID DESC
            """;
        var x=new List<WorkHistoryItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",workId);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))x.Add(new(r.GetInt64(0),r.GetDateTime(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),D(r,7),D(r,8),D(r,9),D(r,10),D(r,11),D(r,12),D(r,13),r.GetString(14),r.GetString(15)));
        return x;
    }

    public async Task AddPlannedDetailAsync(int workId,string type,string reference,CancellationToken ct)
    {
        if(type is not ("A" or "P")||string.IsNullOrWhiteSpace(reference))throw new InvalidOperationException("Riga non valida.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        const string validate="""
            SELECT CASE WHEN @type='A' THEN (SELECT COUNT(*) FROM Articoli WHERE Codice=@reference)
                        ELSE (SELECT COUNT(*) FROM Prestazioni WHERE CAST(Codice AS CHAR)=@reference) END
            """;
        await using(var check=new MySqlCommand(validate,cn,tx)){check.Parameters.AddWithValue("@type",type);check.Parameters.AddWithValue("@reference",reference);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Riferimento non trovato.");}
        const string sql="""
            INSERT INTO LavoriRg(ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
            SELECT l.ID,l.Anno,l.Codice,COALESCE((SELECT MAX(r.Riga)+1 FROM LavoriRg r WHERE r.ID=l.ID),1),
                   @reference,@type,1,
                   CASE WHEN @type='A' THEN COALESCE((SELECT PrezzoStd FROM Articoli WHERE Codice=@reference),0)
                        ELSE COALESCE((SELECT Prezzo FROM Prestazioni WHERE CAST(Codice AS CHAR)=@reference),0) END
            FROM Lavori l WHERE l.ID=@id AND l.StatoLavoro_ID<3
            """;
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@type",type);cmd.Parameters.AddWithValue("@reference",reference);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Il preventivo non è modificabile.");
        await RecalculatePlannedAmountsAsync(cn,tx,workId,ct);
        await tx.CommitAsync(ct);
    }

    public async Task AddActualDetailAsync(int workId,string type,string reference,CancellationToken ct)
    {
        if(type is not ("A" or "P")||string.IsNullOrWhiteSpace(reference))throw new InvalidOperationException("Riga non valida.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        const string validate="""SELECT CASE WHEN @type='A' THEN (SELECT COUNT(*) FROM Articoli WHERE Codice=@reference) ELSE (SELECT COUNT(*) FROM Prestazioni WHERE CAST(Codice AS CHAR)=@reference) END""";
        await using(var check=new MySqlCommand(validate,cn,tx)){check.Parameters.AddWithValue("@type",type);check.Parameters.AddWithValue("@reference",reference);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Riferimento non trovato.");}
        const string sql="""
            INSERT INTO LavoriChiusiRg(ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo)
            SELECT l.ID,l.Anno,l.Codice,COALESCE((SELECT MAX(r.Riga)+1 FROM LavoriChiusiRg r WHERE r.ID=l.ID),1),
                   @reference,@type,1,
                   CASE WHEN @type='A' THEN COALESCE((SELECT PrezzoStd FROM Articoli WHERE Codice=@reference),0)
                        ELSE COALESCE((SELECT Prezzo FROM Prestazioni WHERE CAST(Codice AS CHAR)=@reference),0) END
            FROM Lavori l WHERE l.ID=@id
            """;
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@type",type);cmd.Parameters.AddWithValue("@reference",reference);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La riga consuntiva non è inseribile.");
        await RecalculateActualAmountsAsync(cn,tx,workId,ct);
        await tx.CommitAsync(ct);
    }

    public async Task UpdatePlannedDetailAsync(int workId,short row,decimal quantity,decimal price,CancellationToken ct)
    {
        if(row<=0||quantity<=0||price<0)throw new InvalidOperationException("Quantità o prezzo non validi.");
        const string sql="""
            UPDATE LavoriRg r JOIN Lavori l ON l.ID=r.ID
            SET r.Quantita=@quantity,r.Prezzo=@price
            WHERE r.ID=@id AND r.Riga=@row AND l.StatoLavoro_ID<3
            """;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@row",row);cmd.Parameters.AddWithValue("@quantity",quantity);cmd.Parameters.AddWithValue("@price",price);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La riga non è modificabile.");
        await RecalculatePlannedAmountsAsync(cn,tx,workId,ct);await tx.CommitAsync(ct);
    }

    public async Task DeletePlannedDetailAsync(int workId,short row,CancellationToken ct)
    {
        const string sql="""
            DELETE r FROM LavoriRg r JOIN Lavori l ON l.ID=r.ID
            WHERE r.ID=@id AND r.Riga=@row AND l.StatoLavoro_ID<3
            """;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@row",row);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La riga non è eliminabile.");
        await RecalculatePlannedAmountsAsync(cn,tx,workId,ct);await tx.CommitAsync(ct);
    }

    public async Task UpdateActualDetailAsync(int workId,short row,decimal quantity,decimal price,CancellationToken ct)
    {
        if(row<=0||quantity<=0||price<0)throw new InvalidOperationException("Quantità o prezzo non validi.");
        const string sql="UPDATE LavoriChiusiRg SET Quantita=@quantity,Prezzo=@price WHERE ID=@id AND Riga=@row";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);await using var cmd=new MySqlCommand(sql,cn,tx);
        cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@row",row);cmd.Parameters.AddWithValue("@quantity",quantity);cmd.Parameters.AddWithValue("@price",price);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La riga consuntiva non è modificabile.");
        await RecalculateActualAmountsAsync(cn,tx,workId,ct);await tx.CommitAsync(ct);
    }

    public async Task DeleteActualDetailAsync(int workId,short row,CancellationToken ct)
    {
        const string sql="DELETE FROM LavoriChiusiRg WHERE ID=@id AND Riga=@row";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);await using var cmd=new MySqlCommand(sql,cn,tx);
        cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@row",row);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La riga consuntiva non è eliminabile.");
        await RecalculateActualAmountsAsync(cn,tx,workId,ct);await tx.CommitAsync(ct);
    }

    private static async Task RecalculateActualAmountsAsync(MySqlConnection cn,MySqlTransaction tx,int workId,CancellationToken ct)
    {
        const string sql="""
            UPDATE Lavori l SET
              l.ImportoManodoperaConsuntivo=COALESCE((SELECT SUM(r.Quantita*r.Prezzo) FROM LavoriChiusiRg r WHERE r.ID=l.ID AND r.TipoRiga='P'),0),
              l.ImportoMaterialiConsuntivo=COALESCE((SELECT SUM(r.Quantita*r.Prezzo) FROM LavoriChiusiRg r WHERE r.ID=l.ID AND r.TipoRiga='A'),0),
              l.ImportoRichiesto=COALESCE((SELECT SUM(r.Quantita*r.Prezzo) FROM LavoriChiusiRg r WHERE r.ID=l.ID AND r.TipoRiga IN ('A','P')),0)
            WHERE l.ID=@id
            """;
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@id",workId);await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task RecalculatePlannedAmountsAsync(MySqlConnection cn,MySqlTransaction tx,int workId,CancellationToken ct)
    {
        const string sql="""
            UPDATE Lavori l SET
              l.ImportoManodoperaPreventivato=COALESCE((
                SELECT SUM(r.Quantita*r.Prezzo) FROM LavoriRg r
                WHERE r.ID=l.ID AND (r.TipoRiga='P' OR (r.TipoRiga IS NULL
                  AND NOT EXISTS(SELECT 1 FROM Articoli a WHERE a.Codice=TRIM(r.Articolo))
                  AND EXISTS(SELECT 1 FROM Prestazioni p WHERE CAST(p.Codice AS CHAR)=TRIM(r.Articolo) OR LPAD(CAST(p.Codice AS CHAR),3,'0')=TRIM(r.Articolo))))),0),
              l.ImportoMaterialiPreventivato=COALESCE((
                SELECT SUM(r.Quantita*r.Prezzo) FROM LavoriRg r
                WHERE r.ID=l.ID AND (r.TipoRiga='A' OR (r.TipoRiga IS NULL
                  AND EXISTS(SELECT 1 FROM Articoli a WHERE a.Codice=TRIM(r.Articolo))))),0),
              l.ImportoPreventivoNetto=COALESCE((SELECT SUM(r.Quantita*r.Prezzo) FROM LavoriRg r WHERE r.ID=l.ID
                AND (r.TipoRiga IN ('A','P') OR r.TipoRiga IS NULL)),0)
            WHERE l.ID=@id
            """;
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@id",workId);await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveAsync(WorkEditModel m,CancellationToken ct)
    {
        const string sql="""
            UPDATE Lavori SET DataRedazione=@DraftedOn,DataInterventoPianificata=@PlannedOn,
            OraInterventoPianificata=@PlannedAt,DataUltimoIntervento=@LastServiceOn,
            OperatoreAssegnato=@AssignedOperator,StatoLavoro_ID=@StatusId,EsitoLavoro_ID=@OutcomeId,
            DescrizioneSintetica=@Summary,IstruzioniOperative=@Instructions,
            ImportoManodoperaPreventivato=@PlannedLabour,ImportoMaterialiPreventivato=@PlannedMaterials,ImportoPreventivoNetto=@PlannedNet,
            DataInterventoEffettiva=@CompletedOn,OraInterventoEffettiva=@CompletedAt,
            OperatoreEsecutore=@ExecutingOperator,OreUomoConsuntive=@ManHours,AttivitaEseguita=@WorkPerformed,
            ImportoManodoperaConsuntivo=@ActualLabour,ImportoMaterialiConsuntivo=@ActualMaterials,
            ImportoRichiesto=@RequestedAmount,ImportoIncassato=@CollectedAmount,NoteConsuntive=@Notes WHERE ID=@Id
            """;
        await using var cn=new MySqlConnection(ConnectionString); await cn.OpenAsync(ct);
        await using var tx=await cn.BeginTransactionAsync(ct);
        DateTime? previousLastService,previousPlannedOn,previousCompletedOn;
        byte previousStatus;byte? previousOutcome;
        await using(var oldCmd=new MySqlCommand("SELECT DataUltimoIntervento,DataInterventoPianificata,DataInterventoEffettiva,StatoLavoro_ID,EsitoLavoro_ID FROM Lavori WHERE ID=@Id",cn,tx))
        {
            oldCmd.Parameters.AddWithValue("@Id",m.Id);
            await using var old=await oldCmd.ExecuteReaderAsync(ct);if(!await old.ReadAsync(ct))throw new InvalidOperationException("Scheda lavoro non trovata.");
            previousLastService=D(old,0);previousPlannedOn=D(old,1);previousCompletedOn=D(old,2);previousStatus=old.GetByte(3);previousOutcome=old.IsDBNull(4)?null:old.GetByte(4);
        }
        await using var cmd=new MySqlCommand(sql,cn,tx);
        foreach(var p in typeof(WorkEditModel).GetProperties()) cmd.Parameters.AddWithValue("@"+p.Name,p.GetValue(m)??DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        var statusChanged=previousStatus!=m.StatusId;var outcomeChanged=previousOutcome!=m.OutcomeId;
        var plannedChanged=previousPlannedOn?.Date!=m.PlannedOn?.Date;var completedChanged=previousCompletedOn?.Date!=m.CompletedOn?.Date;
        if(statusChanged||outcomeChanged||plannedChanged||completedChanged)
        {
            var changes=new List<string>();if(statusChanged)changes.Add("stato");if(outcomeChanged)changes.Add("esito");if(plannedChanged)changes.Add("pianificazione");if(completedChanged)changes.Add("data lavoro");
            var eventType=statusChanged&&!outcomeChanged&&!plannedChanged&&!completedChanged?"CAMBIO_STATO":
                outcomeChanged&&!statusChanged&&!plannedChanged&&!completedChanged?"CAMBIO_ESITO":
                plannedChanged&&!statusChanged&&!outcomeChanged&&!completedChanged?"RIPIANIFICAZIONE":
                completedChanged&&!statusChanged&&!outcomeChanged&&!plannedChanged?"REGISTRAZIONE_INTERVENTO":"AGGIORNAMENTO_SCHEDA";
            const string eventSql="""
                INSERT INTO LavoriStorico
                (Lavoro_ID,TipoEvento,DataEvento,StatoPrecedente_ID,StatoNuovo_ID,EsitoPrecedente_ID,EsitoNuovo_ID,
                 DataPianificataPrecedente,DataPianificataNuova,DataInterventoEffettiva,Note)
                VALUES(@Id,@Type,NOW(),@OldStatus,@NewStatus,@OldOutcome,@NewOutcome,@OldPlanned,@NewPlanned,@Completed,@Note)
                """;
            await using var history=new MySqlCommand(eventSql,cn,tx);history.Parameters.AddWithValue("@Id",m.Id);history.Parameters.AddWithValue("@Type",eventType);
            history.Parameters.AddWithValue("@OldStatus",statusChanged?previousStatus:DBNull.Value);history.Parameters.AddWithValue("@NewStatus",statusChanged?m.StatusId:DBNull.Value);
            history.Parameters.AddWithValue("@OldOutcome",outcomeChanged?(object?)previousOutcome??DBNull.Value:DBNull.Value);history.Parameters.AddWithValue("@NewOutcome",outcomeChanged?(object?)m.OutcomeId??DBNull.Value:DBNull.Value);
            history.Parameters.AddWithValue("@OldPlanned",plannedChanged?(object?)previousPlannedOn??DBNull.Value:DBNull.Value);history.Parameters.AddWithValue("@NewPlanned",plannedChanged?(object?)m.PlannedOn??DBNull.Value:DBNull.Value);
            history.Parameters.AddWithValue("@Completed",completedChanged?(object?)m.CompletedOn??DBNull.Value:DBNull.Value);history.Parameters.AddWithValue("@Note","Variazione: "+string.Join(", ",changes));
            await history.ExecuteNonQueryAsync(ct);
        }
        if(previousLastService?.Date!=m.LastServiceOn?.Date)
        {
            const string historySql="""
                INSERT INTO LavoriStorico
                (Lavoro_ID,TipoEvento,DataEvento,DataScadenzaPrecedente,DataScadenzaNuova,Note)
                VALUES(@Id,'CORREZIONE_ULTIMO_INTERVENTO',NOW(),@OldDate,@NewDate,'Correzione manuale dalla scheda lavoro')
                """;
            await using var history=new MySqlCommand(historySql,cn,tx);
            history.Parameters.AddWithValue("@Id",m.Id);
            history.Parameters.AddWithValue("@OldDate",(object?)previousLastService??DBNull.Value);
            history.Parameters.AddWithValue("@NewDate",(object?)m.LastServiceOn??DBNull.Value);
            await history.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private static DateTime? D(MySqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetDateTime(i);
    private static TimeSpan? T(MySqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetTimeSpan(i);

    private async Task<IReadOnlyList<WorkLookupItem>> LoadLookupAsync(string sql, CancellationToken cancellationToken)
    {
        var result = new List<WorkLookupItem>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetByte(0), reader.GetString(1)));
        return result;
    }
}
