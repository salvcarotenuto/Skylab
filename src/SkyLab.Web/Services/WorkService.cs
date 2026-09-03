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

    public async Task<IReadOnlyList<AgendaFlowItem>> AgendaFlowAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT l.ID,CONCAT(l.Codice,'/',l.Anno),COALESCE(c.Nome,''),COALESCE(s.Descrizione,''),
                   COALESCE(l.ScaricatoLavorazione,0),m.ID,m.RicevutoIl,m.Stato,COALESCE(m.Username,'')
            FROM Lavori l
            LEFT JOIN Clienti c ON c.Codice=l.Cliente
            LEFT JOIN StatiLavoro s ON s.ID=l.StatoLavoro_ID
            LEFT JOIN MobileConsuntivi m ON m.ID=(
                SELECT mx.ID FROM MobileConsuntivi mx WHERE mx.Lavoro_ID=l.ID
                ORDER BY mx.RicevutoIl DESC,mx.ID DESC LIMIT 1)
            WHERE l.DataInterventoPianificata IS NOT NULL
            """;
        var result=new List<AgendaFlowItem>();
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))
        {
            var dispatched=r.GetBoolean(4);var inboxId=r.IsDBNull(5)?(long?)null:r.GetInt64(5);
            var received=r.IsDBNull(6)?(DateTime?)null:r.GetDateTime(6);var state=r.IsDBNull(7)?"":r.GetString(7);
            var flow=state switch { "RICEVUTO"=>"Da confermare", "ACQUISITO"=>"Confermato", "ERRORE"=>"Errore", _ when dispatched=>"Sul mobile", _=>"Da scaricare" };
            result.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3),flow,inboxId,received,r.GetString(8)));
        }
        return result;
    }

    public async Task<string> WorkSheetFlowAsync(int workId,CancellationToken ct)
    {
        const string sql="""SELECT COALESCE(l.ScaricatoLavorazione,0),(SELECT m.Stato FROM MobileConsuntivi m WHERE m.Lavoro_ID=l.ID ORDER BY m.RicevutoIl DESC,m.ID DESC LIMIT 1) FROM Lavori l WHERE l.ID=@id""";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",workId);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new InvalidOperationException("Scheda non disponibile.");
        var dispatched=r.GetBoolean(0);var state=r.IsDBNull(1)?"":r.GetString(1);
        return state switch { "RICEVUTO"=>"Da confermare", "ACQUISITO"=>"Confermato", "ERRORE"=>"Errore", _ when dispatched=>"Sul mobile", _=>"Da scaricare" };
    }



    public async Task<IReadOnlyList<WorkListItem>> SearchAsync(
        DateTime from, DateTime? to, string order, byte statusId, short operatorId, byte outcomeId, CancellationToken cancellationToken)
    {
        var dateColumn = order == "lavoro" ? "l.DataInterventoPianificata" : "l.DataRedazione";
        var direction = order == "lavoro" ? "ASC" : "DESC";
        var sql = $"""
            SELECT l.ID, l.Anno, l.Codice, l.DataRedazione, l.DataInterventoPianificata,
                   l.OraInterventoPianificata, l.Cliente, COALESCE(c.Nome,''),
                   COALESCE(NULLIF(CONCAT_WS(' · ',NULLIF(TRIM(d.Nome),''),
                     NULLIF(CONCAT_WS(' ',NULLIF(TRIM(d.Via),''),NULLIF(TRIM(d.Civico),''),NULLIF(TRIM(d.Citta),'')),'')),''),
                     NULLIF(CONCAT_WS(' ',NULLIF(TRIM(c.Via),''),NULLIF(TRIM(c.Civico),''),NULLIF(TRIM(c.Citta),'')),''),'Sede principale'),
                   COALESCE(l.DescrizioneSintetica,''),
                    CASE WHEN l.OperatoreAssegnato=0 THEN '' ELSE
                      COALESCE(NULLIF(TRIM(u.Username),''),CONCAT('Operatore ',l.OperatoreAssegnato)) END,
                   l.OperatoreAssegnato,
                   l.StatoLavoro_ID, s.Descrizione, l.EsitoLavoro_ID, COALESCE(e.Descrizione,''),
                   l.ImportoPreventivato, l.ImportoRichiesto, l.Fattura_ID,
                   COALESCE(l.ScaricatoLavorazione,0)
            FROM Lavori l
            LEFT JOIN Clienti c ON c.Codice = l.Cliente
            LEFT JOIN Destini d ON d.ID = l.Destino_ID
            LEFT JOIN Utenti u ON u.Codice = l.OperatoreAssegnato
            INNER JOIN StatiLavoro s ON s.ID = l.StatoLavoro_ID
            LEFT JOIN EsitiLavoro e ON e.ID = l.EsitoLavoro_ID
            WHERE {dateColumn} >= @from
              AND (@to IS NULL OR {dateColumn} <= @to)
              AND (@statusId = 0 OR l.StatoLavoro_ID = @statusId)
              AND (@operatorId = 0 OR l.OperatoreAssegnato = @operatorId)
              AND (@outcomeId = 0 OR l.EsitoLavoro_ID = @outcomeId)
            ORDER BY {dateColumn} {direction}, l.OraInterventoPianificata {direction}, l.ID {direction}
            """;

        var result = new List<WorkListItem>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@from", from.Date);
        command.Parameters.AddWithValue("@to", to?.Date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@statusId", statusId);
        command.Parameters.AddWithValue("@operatorId", operatorId);
        command.Parameters.AddWithValue("@outcomeId", outcomeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new(
                reader.GetInt32(0), reader.GetInt16(1), reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetTimeSpan(5),
                reader.GetInt32(6), reader.GetString(7), reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetInt16(11),
                reader.GetByte(12), reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetByte(14), reader.GetString(15),
                reader.GetDecimal(16), reader.GetDecimal(17),
                reader.IsDBNull(18) ? null : reader.GetInt32(18),reader.GetBoolean(19)));
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
            SELECT l.ID,l.Anno,l.Codice,l.Cliente,COALESCE(c.Nome,''),l.Destino_ID,l.DataRedazione,
                   l.DataInterventoPianificata,l.OraInterventoPianificata,l.DataUltimoIntervento,
                   l.OperatoreAssegnato,l.StatoLavoro_ID,l.EsitoLavoro_ID,
                   COALESCE(l.DescrizioneSintetica,''),COALESCE(l.IstruzioniOperative,''),
                   l.ImportoManodoperaPreventivato,l.ImportoMaterialiPreventivato,l.ImportoPreventivoNetto,
                   l.DataInterventoEffettiva,l.OraInterventoEffettiva,l.OperatoreEsecutore,
                   l.OreUomoConsuntive,COALESCE(l.AttivitaEseguita,''),
                   l.ImportoManodoperaConsuntivo,l.ImportoMaterialiConsuntivo,
                   l.ImportoRichiesto,l.ImportoIncassato,l.Fattura_ID,COALESCE(l.NoteConsuntive,''),COALESCE(l.ScaricatoLavorazione,0)
            FROM Lavori l LEFT JOIN Clienti c ON c.Codice=l.Cliente WHERE l.ID=@id
            """;
        await using var cn=new MySqlConnection(ConnectionString); await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn); cmd.Parameters.AddWithValue("@id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) return null;
        return new WorkEditModel {
            Id=r.GetInt32(0),Year=r.GetInt16(1),Code=r.GetInt32(2),CustomerId=r.GetInt32(3),Customer=r.GetString(4),
            SiteId=r.IsDBNull(5)?null:r.GetInt32(5),DraftedOn=D(r,6),PlannedOn=D(r,7),PlannedAt=T(r,8),LastServiceOn=D(r,9),AssignedOperator=r.GetInt16(10),
            StatusId=r.GetByte(11),OutcomeId=r.IsDBNull(12)?null:r.GetByte(12),Summary=r.GetString(13),Instructions=r.GetString(14),
            PlannedLabour=r.GetDecimal(15),PlannedMaterials=r.GetDecimal(16),PlannedNet=r.GetDecimal(17),CompletedOn=D(r,18),CompletedAt=T(r,19),
            ExecutingOperator=r.IsDBNull(20)?null:r.GetInt16(20),ManHours=r.IsDBNull(21)?null:r.GetDecimal(21),WorkPerformed=r.GetString(22),
            ActualLabour=r.GetDecimal(23),ActualMaterials=r.GetDecimal(24),RequestedAmount=r.GetDecimal(25),CollectedAmount=r.GetDecimal(26),
            InvoiceId=r.IsDBNull(27)?null:r.GetInt32(27),Notes=r.GetString(28),DispatchedToWork=r.GetBoolean(29)
        };
    }

    public async Task<IReadOnlyList<WorkSiteLookupItem>> WorkSitesAsync(int customerId,CancellationToken ct)
    {
        const string sql="""
          SELECT ID,Descrizione FROM (
            SELECT NULL AS ID,0 AS Ordine,
              CONCAT_WS(' · ','Sede principale',NULLIF(CONCAT_WS(' ',NULLIF(TRIM(Via),''),NULLIF(TRIM(Civico),''),NULLIF(TRIM(Citta),'')) ,'')) AS Descrizione
            FROM Clienti WHERE Codice=@customer
            UNION ALL
            SELECT ID,1,COALESCE(NULLIF(CONCAT_WS(' · ',NULLIF(TRIM(Nome),''),NULLIF(CONCAT_WS(' ',NULLIF(TRIM(Via),''),NULLIF(TRIM(Civico),''),NULLIF(TRIM(Citta),'')),'')),''),CONCAT('Sede ',Codice))
            FROM Destini WHERE CliFor='C' AND Ditta=@customer AND COALESCE(Attivo,0)<>0
          ) x ORDER BY Ordine,Descrizione;
          """;
        var result=new List<WorkSiteLookupItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@customer",customerId);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.IsDBNull(0)?null:r.GetInt32(0),r.GetString(1)));return result;
    }

    public async Task<IReadOnlyList<OperatorLookupItem>> OperatorsAsync(CancellationToken ct)
    {
        var x=new List<OperatorLookupItem>();
        await using var cn=new MySqlConnection(ConnectionString); await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand("SELECT Codice,COALESCE(NULLIF(TRIM(Username),''),CONCAT('Operatore ',Codice)) AS Descrizione FROM Utenti WHERE COALESCE(Attivo,0)<>0 AND COALESCE(Qualifica,0) IN (1,4) ORDER BY Descrizione",cn);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) x.Add(new(r.GetInt16(0),r.GetString(1)));
        return x;
    }

    public async Task<IReadOnlyList<MobileWorkItem>> MobileWorksAsync(string username,CancellationToken ct)
    {
        const string sql="""
          SELECT l.ID,CONCAT(l.Codice,'/',l.Anno),l.DataInterventoPianificata,l.OraInterventoPianificata,
                 COALESCE(c.Nome,''),
                 COALESCE(
                   NULLIF(CONCAT_WS(' · ',NULLIF(TRIM(d.Nome),''),NULLIF(CONCAT_WS(' ',NULLIF(TRIM(d.Via),''),NULLIF(TRIM(d.Civico),''),NULLIF(TRIM(d.Citta),'')),'')),''),
                   NULLIF(CONCAT_WS(' ',NULLIF(TRIM(c.Via),''),NULLIF(TRIM(c.Civico),''),NULLIF(TRIM(c.Citta),'')),''),
                   'Indirizzo non disponibile'),
                 COALESCE(l.DescrizioneSintetica,''),COALESCE(s.Descrizione,'')
          FROM Lavori l
          INNER JOIN Utenti u ON u.Codice=l.OperatoreAssegnato
          LEFT JOIN Clienti c ON c.Codice=l.Cliente
          LEFT JOIN Destini d ON d.ID=l.Destino_ID
          LEFT JOIN StatiLavoro s ON s.ID=l.StatoLavoro_ID
          WHERE u.Username=@username AND COALESCE(l.ScaricatoLavorazione,0)<>0 AND l.StatoLavoro_ID IN (1,2)
          ORDER BY l.DataInterventoPianificata,l.OraInterventoPianificata,l.ID
          """;
        var result=new List<MobileWorkItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@username",username);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),r.GetString(1),r.IsDBNull(2)?null:r.GetDateTime(2),r.IsDBNull(3)?null:r.GetTimeSpan(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7)));return result;
    }

    public async Task<MobileWorkDetailItem?> MobileWorkDetailAsync(int id,string username,CancellationToken ct)
    {
        const string sql="""
          SELECT l.ID,CONCAT(l.Codice,'/',l.Anno),l.DataRedazione,l.DataInterventoPianificata,l.OraInterventoPianificata,
                  l.DataUltimoIntervento,COALESCE(c.Nome,''),
                 COALESCE(NULLIF(CONCAT_WS(' · ',NULLIF(TRIM(d.Nome),''),NULLIF(CONCAT_WS(' ',NULLIF(TRIM(d.Via),''),NULLIF(TRIM(d.Civico),''),NULLIF(TRIM(d.Citta),'')),'')),''),
                          CONCAT_WS(' · ','Sede principale',NULLIF(CONCAT_WS(' ',NULLIF(TRIM(c.Via),''),NULLIF(TRIM(c.Civico),''),NULLIF(TRIM(c.Citta),'')),''))),
                  COALESCE(c.Listino,0),COALESCE(NULLIF(TRIM(u.Username),''),CONCAT('Operatore ',u.Codice)),
                 COALESCE(s.Descrizione,''),COALESCE(e.Descrizione,''),COALESCE(l.DescrizioneSintetica,''),COALESCE(l.IstruzioniOperative,''),
                 COALESCE(l.ImportoManodoperaPreventivato,0),COALESCE(l.ImportoMaterialiPreventivato,0),COALESCE(l.ImportoPreventivoNetto,0)
          FROM Lavori l
          INNER JOIN Utenti u ON u.Codice=l.OperatoreAssegnato
          LEFT JOIN Clienti c ON c.Codice=l.Cliente
          LEFT JOIN Destini d ON d.ID=l.Destino_ID
          LEFT JOIN StatiLavoro s ON s.ID=l.StatoLavoro_ID
          LEFT JOIN EsitiLavoro e ON e.ID=l.EsitoLavoro_ID
          WHERE l.ID=@id AND u.Username=@username AND COALESCE(l.ScaricatoLavorazione,0)<>0 AND l.StatoLavoro_ID IN (1,2)
          """;
        int workId;byte priceList;string number,customer,site,assignedOperator,status,outcome,summary,instructions;
        DateTime? draftedOn,plannedOn,lastServiceOn;TimeSpan? plannedAt;decimal plannedLabour,plannedMaterials,plannedNet;
        await using(var cn=new MySqlConnection(ConnectionString))
        {
            await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@username",username);
            await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
            workId=r.GetInt32(0);number=r.GetString(1);draftedOn=D(r,2);plannedOn=D(r,3);plannedAt=T(r,4);lastServiceOn=D(r,5);
            customer=r.GetString(6);site=r.GetString(7);priceList=r.GetByte(8);assignedOperator=r.GetString(9);status=r.GetString(10);outcome=r.GetString(11);summary=r.GetString(12);instructions=r.GetString(13);
            plannedLabour=r.GetDecimal(14);plannedMaterials=r.GetDecimal(15);plannedNet=r.GetDecimal(16);
        }
        var rows=await PlannedDetailsAsync(workId,ct);
        static MobileWorkDetailRow Map(WorkDetailItem row)=>new(row.Reference,row.Description,row.Quantity,row.UnitPrice,row.Amount);
        return new(workId,number,draftedOn,plannedOn,plannedAt,lastServiceOn,customer,site,priceList,assignedOperator,status,outcome,summary,instructions,
            plannedLabour,plannedMaterials,plannedNet,rows.Where(x=>x.Type=="P").Select(Map).ToArray(),rows.Where(x=>x.Type=="A").Select(Map).ToArray());
    }

    public async Task AssignOperatorForDispatchAsync(int workId,short operatorId,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        const string validate="SELECT COUNT(*) FROM Utenti WHERE Codice=@operator AND COALESCE(Attivo,0)<>0 AND COALESCE(Qualifica,0) IN (1,4) AND COALESCE(Bloccato,0)=0";
        await using(var check=new MySqlCommand(validate,cn,tx)){check.Parameters.AddWithValue("@operator",operatorId);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("L'operatore selezionato non è abilitato alla lavorazione.");}
        const string update="""
          UPDATE Lavori SET OperatoreAssegnato=@operator,
              StatoLavoro_ID=CASE WHEN StatoLavoro_ID=1 THEN 2 ELSE StatoLavoro_ID END
          WHERE ID=@work AND StatoLavoro_ID IN (1,2) AND COALESCE(ScaricatoLavorazione,0)=0;
          """;
        await using(var cmd=new MySqlCommand(update,cn,tx)){cmd.Parameters.AddWithValue("@operator",operatorId);cmd.Parameters.AddWithValue("@work",workId);if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La scheda non può più essere assegnata per lo scarico.");}
        const string history="INSERT INTO LavoriStorico(Lavoro_ID,TipoEvento,DataEvento,StatoNuovo_ID,Note,DatiNuovi) VALUES(@work,'ASSEGNAZIONE_SCARICO',NOW(),2,'Operatore attribuito da Agenda lavori',JSON_OBJECT('OperatoreAssegnato',@operator))";
        await using(var cmd=new MySqlCommand(history,cn,tx)){cmd.Parameters.AddWithValue("@work",workId);cmd.Parameters.AddWithValue("@operator",operatorId);await cmd.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);
    }

    public async Task<int> DispatchToWorkAsync(IReadOnlyCollection<int> workIds,CancellationToken ct)
    {
        var ids=workIds.Where(x=>x>0).Distinct().ToArray();if(ids.Length==0)throw new InvalidOperationException("Selezionare almeno una scheda.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        var names=ids.Select((_,i)=>$"@id{i}").ToArray();var list=string.Join(',',names);
        await using(var check=new MySqlCommand($"SELECT COUNT(*) FROM Lavori WHERE ID IN ({list}) AND (OperatoreAssegnato=0 OR StatoLavoro_ID NOT IN (1,2))",cn,tx))
        {for(var i=0;i<ids.Length;i++)check.Parameters.AddWithValue(names[i],ids[i]);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=0)throw new InvalidOperationException("Una o più schede non hanno un operatore valido o non sono scaricabili.");}
        await using(var update=new MySqlCommand($"UPDATE Lavori SET ScaricatoLavorazione=1,DataScaricoLavorazione=NOW(),StatoLavoro_ID=2 WHERE ID IN ({list}) AND COALESCE(ScaricatoLavorazione,0)=0",cn,tx))
        {for(var i=0;i<ids.Length;i++)update.Parameters.AddWithValue(names[i],ids[i]);await update.ExecuteNonQueryAsync(ct);}
        await using(var history=new MySqlCommand($"INSERT INTO LavoriStorico(Lavoro_ID,TipoEvento,DataEvento,StatoNuovo_ID,Note) SELECT ID,'SCARICO_LAVORAZIONE',NOW(),2,'Scheda resa disponibile alla lavorazione mobile' FROM Lavori WHERE ID IN ({list})",cn,tx))
        {for(var i=0;i<ids.Length;i++)history.Parameters.AddWithValue(names[i],ids[i]);await history.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);return ids.Length;
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
            SELECT 'A',a.Codice,COALESCE(a.Descrizione,''),COALESCE(c.Descrizione,''),COALESCE(NULLIF(a.Umv,''),NULLIF(a.Uml,''),NULLIF(a.Uma,''),''),COALESCE(a.PrezzoStd,0),
                   MAX(CASE WHEN al.Listino=1 THEN NULLIF(al.Prezzo,0) END),MAX(CASE WHEN al.Listino=2 THEN NULLIF(al.Prezzo,0) END),
                   MAX(CASE WHEN al.Listino=3 THEN NULLIF(al.Prezzo,0) END),MAX(CASE WHEN al.Listino=4 THEN NULLIF(al.Prezzo,0) END),
                   MAX(CASE WHEN al.Listino=5 THEN NULLIF(al.Prezzo,0) END),MAX(CASE WHEN al.Listino=6 THEN NULLIF(al.Prezzo,0) END),
                   COALESCE(GROUP_CONCAT(DISTINCT b.Barcode ORDER BY b.Barcode SEPARATOR '|'),'')
            FROM Articoli a LEFT JOIN Categorie c ON c.Codice=a.Categoria
            LEFT JOIN ArtListini al ON al.Articolo=a.Codice AND al.Listino BETWEEN 1 AND 6
            LEFT JOIN Barcodes b ON b.Articolo=a.Codice
            GROUP BY a.Codice,a.Descrizione,c.Descrizione,a.Umv,a.Uml,a.Uma,a.PrezzoStd
            UNION ALL
            SELECT 'P',CAST(p.Codice AS CHAR),p.Descrizione,'Prestazione','',COALESCE(p.Prezzo,0),NULL,NULL,NULL,NULL,NULL,NULL,''
            FROM Prestazioni p ORDER BY 1,3
            """;
        var x=new List<WorkReferenceLookup>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))x.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetDecimal(5),
            r.IsDBNull(6)?null:r.GetDecimal(6),r.IsDBNull(7)?null:r.GetDecimal(7),r.IsDBNull(8)?null:r.GetDecimal(8),
            r.IsDBNull(9)?null:r.GetDecimal(9),r.IsDBNull(10)?null:r.GetDecimal(10),r.IsDBNull(11)?null:r.GetDecimal(11),r.GetString(12)));
        return x;
    }

    public async Task<object> SubmitMobileReportAsync(int workId,string username,MobileReportRequest report,CancellationToken ct)
    {
        if(!Guid.TryParse(report.SubmissionId,out _))throw new InvalidOperationException("Identificativo di invio non valido.");
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        await using(var allowed=new MySqlCommand("SELECT l.StatoLavoro_ID FROM Lavori l INNER JOIN Utenti u ON u.Codice=l.OperatoreAssegnato WHERE l.ID=@id AND u.Username=@username AND COALESCE(l.ScaricatoLavorazione,0)<>0",cn,tx)){allowed.Parameters.AddWithValue("@id",workId);allowed.Parameters.AddWithValue("@username",username);var value=await allowed.ExecuteScalarAsync(ct);if(value is null)throw new InvalidOperationException("Scheda non disponibile per l'operatore.");if(Convert.ToByte(value)>=5)throw new InvalidOperationException("Il consuntivo è già stato confermato e non può ricevere altri invii.");}
        await using(var existing=new MySqlCommand("SELECT Stato FROM MobileConsuntivi WHERE SubmissionId=@submission LIMIT 1",cn,tx)){existing.Parameters.AddWithValue("@submission",report.SubmissionId);var state=await existing.ExecuteScalarAsync(ct);if(state is not null){await tx.CommitAsync(ct);return new{received=true,status=Convert.ToString(state),duplicate=true,submissionId=report.SubmissionId};}}
        var payload=System.Text.Json.JsonSerializer.Serialize(report);
        await using(var save=new MySqlCommand("INSERT INTO MobileConsuntivi(SubmissionId,Lavoro_ID,Username,Payload,Stato) VALUES(@submission,@work,@username,@payload,'RICEVUTO')",cn,tx)){save.Parameters.AddWithValue("@submission",report.SubmissionId);save.Parameters.AddWithValue("@work",workId);save.Parameters.AddWithValue("@username",username);save.Parameters.AddWithValue("@payload",payload);await save.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);return new{received=true,status="RICEVUTO",duplicate=false,submissionId=report.SubmissionId};
    }

    public async Task<IReadOnlyList<MobileReportInboxItem>> MobileReportsInboxAsync(CancellationToken ct)
    {
        const string sql="""SELECT m.ID,m.SubmissionId,m.Lavoro_ID,CONCAT(l.Codice,'/',l.Anno),COALESCE(c.Nome,''),m.Username,m.RicevutoIl,m.Stato,COALESCE(m.Errore,'') FROM MobileConsuntivi m INNER JOIN Lavori l ON l.ID=m.Lavoro_ID LEFT JOIN Clienti c ON c.Codice=l.Cliente ORDER BY CASE m.Stato WHEN 'RICEVUTO' THEN 0 WHEN 'ERRORE' THEN 1 ELSE 2 END,m.RicevutoIl DESC""";
        var result=new List<MobileReportInboxItem>();await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt64(0),Convert.ToString(r.GetValue(1),System.Globalization.CultureInfo.InvariantCulture)??"",r.GetInt32(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetDateTime(6),r.GetString(7),r.GetString(8)));return result;
    }

    public async Task<MobileReportPreview?> PendingMobileReportAsync(int workId,CancellationToken ct)
    {
        const string sql="""SELECT m.ID,m.SubmissionId,m.Lavoro_ID,CONCAT(l.Codice,'/',l.Anno),COALESCE(c.Nome,''),m.Username,m.RicevutoIl,m.Stato,COALESCE(m.Errore,''),CAST(m.Payload AS CHAR) FROM MobileConsuntivi m INNER JOIN Lavori l ON l.ID=m.Lavoro_ID LEFT JOIN Clienti c ON c.Codice=l.Cliente WHERE m.Lavoro_ID=@work AND m.Stato IN ('RICEVUTO','ERRORE') ORDER BY m.RicevutoIl DESC,m.ID DESC LIMIT 1""";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@work",workId);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        var inbox=new MobileReportInboxItem(r.GetInt64(0),Convert.ToString(r.GetValue(1),System.Globalization.CultureInfo.InvariantCulture)??"",r.GetInt32(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetDateTime(6),r.GetString(7),r.GetString(8));
        var report=System.Text.Json.JsonSerializer.Deserialize<MobileReportRequest>(r.GetString(9),new System.Text.Json.JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidOperationException("Dati del consuntivo non validi.");
        return new(inbox,report);
    }

    public async Task AcquireMobileReportAsync(long inboxId,CancellationToken ct)
    {
        string payload,username;int workId;
        await using(var cn=new MySqlConnection(ConnectionString)){await cn.OpenAsync(ct);await using var cmd=new MySqlCommand("SELECT Lavoro_ID,Username,CAST(Payload AS CHAR) FROM MobileConsuntivi WHERE ID=@id AND Stato IN ('RICEVUTO','ERRORE')",cn);cmd.Parameters.AddWithValue("@id",inboxId);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new InvalidOperationException("Consuntivo non disponibile per l'acquisizione.");workId=r.GetInt32(0);username=r.GetString(1);payload=r.GetString(2);}
        var report=System.Text.Json.JsonSerializer.Deserialize<MobileReportRequest>(payload,new System.Text.Json.JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidOperationException("Dati del consuntivo non validi.");
        try{await ApplyMobileReportDirectAsync(workId,username,report,ct);await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var done=new MySqlCommand("UPDATE MobileConsuntivi SET Stato='ACQUISITO',AcquisitoIl=NOW(),Errore=NULL WHERE ID=@id",cn);done.Parameters.AddWithValue("@id",inboxId);await done.ExecuteNonQueryAsync(ct);}
        catch(Exception ex){await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var error=new MySqlCommand("UPDATE MobileConsuntivi SET Stato='ERRORE',Errore=@error WHERE ID=@id",cn);error.Parameters.AddWithValue("@id",inboxId);error.Parameters.AddWithValue("@error",ex.Message);await error.ExecuteNonQueryAsync(ct);throw;}
    }

    public async Task UpdateConfirmedAdministrativeDataAsync(int workId,short? executingOperator,decimal requestedAmount,decimal collectedAmount,CancellationToken ct)
    {
        const string sql="UPDATE Lavori SET OperatoreEsecutore=@operator,ImportoRichiesto=@requested,ImportoIncassato=@collected WHERE ID=@id";
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var cmd=new MySqlCommand(sql,cn);
        cmd.Parameters.AddWithValue("@id",workId);cmd.Parameters.AddWithValue("@operator",(object?)executingOperator??DBNull.Value);cmd.Parameters.AddWithValue("@requested",requestedAmount);cmd.Parameters.AddWithValue("@collected",collectedAmount);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Scheda lavoro non trovata.");
    }

    private async Task<object> ApplyMobileReportDirectAsync(int workId,string username,MobileReportRequest report,CancellationToken ct)
    {
        if(!Guid.TryParse(report.SubmissionId,out _))throw new InvalidOperationException("Identificativo di invio non valido.");
        if(!DateTime.TryParseExact(report.CompletedOn,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var completedOn))throw new InvalidOperationException("Data lavoro non valida.");
        if(!TimeSpan.TryParse(report.CompletedAt,System.Globalization.CultureInfo.InvariantCulture,out var completedAt))throw new InvalidOperationException("Ora lavoro non valida.");
        var hoursText=(report.ManHours??"").Trim().Replace(',','.');decimal? manHours=null;
        if(hoursText.Length>0){if(!decimal.TryParse(hoursText,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out var parsedHours)||parsedHours<0)throw new InvalidOperationException("Ore uomo non valide.");manHours=parsedHours;}
        if(string.IsNullOrWhiteSpace(report.Outcome))throw new InvalidOperationException("Selezionare l'esito.");
        var rows=(report.Rows??[]).Where(x=>x.Quantity>0).ToArray();
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        await using(var duplicate=new MySqlCommand("SELECT COUNT(*) FROM LavoriStorico WHERE Lavoro_ID=@id AND JSON_UNQUOTE(JSON_EXTRACT(DatiNuovi,'$.submissionId'))=@submission",cn,tx))
        {duplicate.Parameters.AddWithValue("@id",workId);duplicate.Parameters.AddWithValue("@submission",report.SubmissionId);if(Convert.ToInt32(await duplicate.ExecuteScalarAsync(ct))>0){await tx.CommitAsync(ct);return new{received=true,duplicate=true,submissionId=report.SubmissionId};}}
        int year,code;short operatorId;byte status;
        await using(var work=new MySqlCommand("SELECT l.Anno,l.Codice,l.OperatoreAssegnato,l.StatoLavoro_ID FROM Lavori l INNER JOIN Utenti u ON u.Codice=l.OperatoreAssegnato WHERE l.ID=@id AND u.Username=@username AND COALESCE(l.ScaricatoLavorazione,0)<>0 FOR UPDATE",cn,tx))
        {work.Parameters.AddWithValue("@id",workId);work.Parameters.AddWithValue("@username",username);await using var r=await work.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new InvalidOperationException("Scheda non disponibile per l'operatore.");year=r.GetInt16(0);code=r.GetInt32(1);operatorId=r.GetInt16(2);status=r.GetByte(3);}
        if(status>=5)throw new InvalidOperationException("Il consuntivo risulta già chiuso.");
        byte outcomeId;await using(var outcome=new MySqlCommand("SELECT ID FROM EsitiLavoro WHERE Descrizione=@outcome LIMIT 1",cn,tx)){outcome.Parameters.AddWithValue("@outcome",report.Outcome.Trim());var value=await outcome.ExecuteScalarAsync(ct);if(value is null)throw new InvalidOperationException("Esito non riconosciuto.");outcomeId=Convert.ToByte(value);}
        await using(var clear=new MySqlCommand("DELETE FROM LavoriChiusiRg WHERE ID=@id",cn,tx)){clear.Parameters.AddWithValue("@id",workId);await clear.ExecuteNonQueryAsync(ct);}
        short rowNumber=0;
        foreach(var row in rows)
        {
            var type=(row.Type??"").Trim().ToUpperInvariant();var reference=(row.Reference??"").Trim();if(type is not ("A" or "P")||reference.Length==0||row.Price<0)throw new InvalidOperationException("Riga consuntiva non valida.");
            var validation=type=="A"?"SELECT COUNT(*) FROM Articoli WHERE Codice=@reference":"SELECT COUNT(*) FROM Prestazioni WHERE CAST(Codice AS CHAR)=@reference OR LPAD(CAST(Codice AS CHAR),3,'0')=@reference";
            await using(var check=new MySqlCommand(validation,cn,tx)){check.Parameters.AddWithValue("@reference",reference);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))==0)throw new InvalidOperationException($"Riferimento {reference} non trovato.");}
            await using var insert=new MySqlCommand("INSERT INTO LavoriChiusiRg(ID,Anno,Codice,Riga,Articolo,TipoRiga,Quantita,Prezzo) VALUES(@id,@year,@code,@row,@reference,@type,@quantity,@price)",cn,tx);
            insert.Parameters.AddWithValue("@id",workId);insert.Parameters.AddWithValue("@year",year);insert.Parameters.AddWithValue("@code",code);insert.Parameters.AddWithValue("@row",++rowNumber);insert.Parameters.AddWithValue("@reference",reference);insert.Parameters.AddWithValue("@type",type);insert.Parameters.AddWithValue("@quantity",row.Quantity);insert.Parameters.AddWithValue("@price",row.Price);await insert.ExecuteNonQueryAsync(ct);
        }
        const string update="""UPDATE Lavori SET DataInterventoEffettiva=@date,OraInterventoEffettiva=@time,OperatoreEsecutore=@operator,OreUomoConsuntive=@hours,EsitoLavoro_ID=@outcome,AttivitaEseguita=@performed,ImportoManodoperaConsuntivo=COALESCE((SELECT SUM(Quantita*Prezzo) FROM LavoriChiusiRg WHERE ID=@id AND TipoRiga='P'),0),ImportoMaterialiConsuntivo=COALESCE((SELECT SUM(Quantita*Prezzo) FROM LavoriChiusiRg WHERE ID=@id AND TipoRiga='A'),0),ImportoRichiesto=COALESCE((SELECT SUM(Quantita*Prezzo) FROM LavoriChiusiRg WHERE ID=@id),0),ImportoIncassato=@collected,NoteConsuntive=@notes,StatoLavoro_ID=5 WHERE ID=@id""";
        await using(var save=new MySqlCommand(update,cn,tx)){save.Parameters.AddWithValue("@id",workId);save.Parameters.AddWithValue("@date",completedOn.Date);save.Parameters.AddWithValue("@time",completedAt);save.Parameters.AddWithValue("@operator",operatorId);save.Parameters.AddWithValue("@hours",(object?)manHours??DBNull.Value);save.Parameters.AddWithValue("@outcome",outcomeId);save.Parameters.AddWithValue("@performed",(report.WorkPerformed??"").Trim());save.Parameters.AddWithValue("@collected",report.CollectedAmount);save.Parameters.AddWithValue("@notes",(report.Notes??"").Trim());await save.ExecuteNonQueryAsync(ct);}
        await using(var history=new MySqlCommand("INSERT INTO LavoriStorico(Lavoro_ID,TipoEvento,DataEvento,StatoPrecedente_ID,StatoNuovo_ID,EsitoNuovo_ID,DataInterventoEffettiva,Note,DatiNuovi) VALUES(@id,'CONSUNTIVO_MOBILE',NOW(),@oldStatus,5,@outcome,@date,'Consuntivo confermato e inviato dal mobile',JSON_OBJECT('submissionId',@submission,'username',@username))",cn,tx)){history.Parameters.AddWithValue("@id",workId);history.Parameters.AddWithValue("@oldStatus",status);history.Parameters.AddWithValue("@outcome",outcomeId);history.Parameters.AddWithValue("@date",completedOn.Date);history.Parameters.AddWithValue("@submission",report.SubmissionId);history.Parameters.AddWithValue("@username",username);await history.ExecuteNonQueryAsync(ct);}
        await tx.CommitAsync(ct);return new{received=true,duplicate=false,submissionId=report.SubmissionId};
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
            OraInterventoPianificata=@PlannedAt,DataUltimoIntervento=@LastServiceOn,Destino_ID=@SiteId,
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
        if(m.SiteId.HasValue){await using var site=new MySqlCommand("SELECT COUNT(*) FROM Destini WHERE ID=@site AND Ditta=@customer AND CliFor='C' AND COALESCE(Attivo,0)<>0",cn,tx);site.Parameters.AddWithValue("@site",m.SiteId.Value);site.Parameters.AddWithValue("@customer",m.CustomerId);if(Convert.ToInt32(await site.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Sede lavoro non valida.");}
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
