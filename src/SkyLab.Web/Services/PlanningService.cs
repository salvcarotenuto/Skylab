using MySqlConnector;
using SkyLab.Web.Models;

namespace SkyLab.Web.Services;

public sealed class PlanningService(IConfiguration configuration)
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

    public async Task<IReadOnlyList<PlanningDistrict>> DistrictsAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT Codice, Descrizione FROM Distretti ORDER BY Descrizione";
        var result = new List<PlanningDistrict>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetInt16(0), reader.GetString(1)));
        return result;
    }

    public async Task<IReadOnlyList<PlanningCategory>> CategoriesAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT Codice, Descrizione FROM Categorie ORDER BY Descrizione";
        var result = new List<PlanningCategory>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetInt16(0), reader.GetString(1)));
        return result;
    }

    public async Task<IReadOnlyList<InstalledMachine>> InstalledAsync(
        DateTime installedTo,
        string? search,
        short category,
        string order,
        CancellationToken cancellationToken)
    {
        var orderBy = order switch
        {
            "cliente" => "c.Nome, a.Descrizione",
            "installazione" => "m.DataRif DESC, c.Nome",
            "valore" => "m.Valore DESC, c.Nome",
            "scadenza" => "m.ProxData, c.Nome",
            _ => "a.Descrizione, c.Nome"
        };
        var sql = $"""
            SELECT m.ID, m.Cliente, COALESCE(c.Nome,''), COALESCE(c.Citta,''),
                   COALESCE(cat.Descrizione,''), COALESCE(m.Articolo,''), COALESCE(a.Descrizione,''),
                   m.Valore, m.DataRif, m.ProxData
            FROM MacchineCli m
            JOIN Clienti c ON c.Codice=m.Cliente
            LEFT JOIN Articoli a ON a.Codice=m.Articolo
            LEFT JOIN Categorie cat ON cat.Codice=m.Categoria
            WHERE (m.DataRif IS NULL OR m.DataRif <= @installedTo)
              AND (@search='' OR c.Nome LIKE CONCAT('%',@search,'%') OR a.Descrizione LIKE CONCAT('%',@search,'%') OR m.Articolo LIKE CONCAT('%',@search,'%'))
              AND (@category=0 OR m.Categoria=@category)
            ORDER BY {orderBy}
            """;

        var result = new List<InstalledMachine>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@installedTo", installedTo.Date);
        command.Parameters.AddWithValue("@search", search?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@category", category);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDecimal(7), reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.IsDBNull(9) ? null : reader.GetDateTime(9)));
        }
        return result;
    }

    public async Task<IReadOnlyList<PlanningDueItem>> DueAsync(
        DateTime from,
        DateTime to,
        string? search,
        byte customerType,
        short district,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT m.ID, m.Cliente, COALESCE(c.Nome,''), COALESCE(c.Citta,''),
                   COALESCE(c.Distretto,0), COALESCE(d.Descrizione,''), COALESCE(c.Tipologia,0),
                   COALESCE(m.Articolo,''), COALESCE(a.Descrizione,''), COALESCE(cat.Descrizione,''),
                   m.Valore, m.Durata, m.DataRif, m.ProxData,
                     i.ID,i.Lavoro_ID,COALESCE(i.Stato,''),i.DataIntervento,i.OraIntervento,
                    COALESCE(i.Descrizione,''),COALESCE(i.Note,'')
            FROM MacchineCli m
            JOIN Clienti c ON c.Codice=m.Cliente
            LEFT JOIN Distretti d ON d.Codice=c.Distretto
            LEFT JOIN Articoli a ON a.Codice=m.Articolo
            LEFT JOIN Categorie cat ON cat.Codice=m.Categoria
            LEFT JOIN ImpegniLavoro i ON i.ID=(
              SELECT MAX(ix.ID) FROM ImpegniLavoro ix
              WHERE ix.MacchinaCli_ID=m.ID AND ix.DataScadenzaOrigine=m.ProxData AND ix.Stato<>'X')
            WHERE m.ProxData BETWEEN @from AND @to
              AND (@search='' OR c.Nome LIKE CONCAT('%',@search,'%'))
              AND (@customerType=0 OR c.Tipologia=@customerType)
              AND (@district=0 OR c.Distretto=@district)
            ORDER BY c.Nome, m.ProxData, a.Descrizione
            """;

        var result = new List<PlanningDueItem>();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@from", from.Date);
        command.Parameters.AddWithValue("@to", to.Date);
        command.Parameters.AddWithValue("@search", search?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@customerType", customerType);
        command.Parameters.AddWithValue("@district", district);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt16(4), reader.GetString(5), reader.GetByte(6), reader.GetString(7),
                reader.GetString(8), reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                reader.IsDBNull(11) ? null : reader.GetInt16(11), reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                 reader.GetDateTime(13),reader.IsDBNull(14)?null:reader.GetInt32(14),reader.IsDBNull(15)?null:reader.GetInt32(15),reader.GetString(16),
                 reader.IsDBNull(17)?null:reader.GetDateTime(17),reader.IsDBNull(18)?null:reader.GetTimeSpan(18),
                 reader.GetString(19),reader.GetString(20)));
        }
        return result;
    }

    public async Task<int> AcquireCommitmentAsync(int machineId,int customerId,DateTime dueDate,DateTime? agreedOn,TimeSpan? agreedAt,string? description,string? notes,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        const string checkSql="SELECT COUNT(*) FROM MacchineCli WHERE ID=@machine AND Cliente=@customer AND ProxData=@due";
        await using(var check=new MySqlCommand(checkSql,cn,tx)){check.Parameters.AddWithValue("@machine",machineId);check.Parameters.AddWithValue("@customer",customerId);check.Parameters.AddWithValue("@due",dueDate.Date);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Scadenza non più disponibile.");}
        const string existingSql="SELECT ID FROM ImpegniLavoro WHERE MacchinaCli_ID=@machine AND DataScadenzaOrigine=@due AND Stato<>'X' ORDER BY ID DESC LIMIT 1";
        await using(var existing=new MySqlCommand(existingSql,cn,tx))
        {
            existing.Parameters.AddWithValue("@machine",machineId);
            existing.Parameters.AddWithValue("@due",dueDate.Date);
            var id=await existing.ExecuteScalarAsync(ct);
            if(id is not null)
            {
                const string updateSql="""
                  UPDATE ImpegniLavoro
                  SET DataIntervento=@agreedOn,OraIntervento=@agreedAt,
                      Stato=CASE WHEN @agreedOn IS NULL THEN 'A' ELSE 'P' END,
                      Descrizione=NULLIF(@description,''),Note=NULLIF(@notes,'')
                  WHERE ID=@id;
                  """;
                await using var update=new MySqlCommand(updateSql,cn,tx);
                update.Parameters.AddWithValue("@id",Convert.ToInt32(id));
                update.Parameters.AddWithValue("@agreedOn",(object?)agreedOn?.Date??DBNull.Value);
                update.Parameters.AddWithValue("@agreedAt",(object?)agreedAt??DBNull.Value);
                update.Parameters.AddWithValue("@description",description?.Trim()??"");
                update.Parameters.AddWithValue("@notes",notes?.Trim()??"");
                await update.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
                return Convert.ToInt32(id);
            }
        }
        const string sql="""
          INSERT INTO ImpegniLavoro(Cliente_ID,MacchinaCli_ID,Origine,DataScadenzaOrigine,DataAcquisizione,DataIntervento,OraIntervento,Stato,Descrizione,Note)
          VALUES(@customer,@machine,'P',@due,NOW(),@agreedOn,@agreedAt,CASE WHEN @agreedOn IS NULL THEN 'A' ELSE 'P' END,NULLIF(@description,''),NULLIF(@notes,''));
          SELECT LAST_INSERT_ID();
          """;
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@customer",customerId);cmd.Parameters.AddWithValue("@machine",machineId);cmd.Parameters.AddWithValue("@due",dueDate.Date);cmd.Parameters.AddWithValue("@agreedOn",(object?)agreedOn?.Date??DBNull.Value);cmd.Parameters.AddWithValue("@agreedAt",(object?)agreedAt??DBNull.Value);cmd.Parameters.AddWithValue("@description",description?.Trim()??"");cmd.Parameters.AddWithValue("@notes",notes?.Trim()??"");
        var result=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));await tx.CommitAsync(ct);return result;
    }

    public async Task<int> CreateWorkFromCommitmentAsync(int commitmentId,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        const string sourceSql="""
          SELECT i.Cliente_ID,i.MacchinaCli_ID,i.DataIntervento,i.OraIntervento,
                 COALESCE(i.Descrizione,''),COALESCE(i.Note,''),i.Lavoro_ID,
                 m.DestinoID,m.DataRif,COALESCE(m.Articolo,''),COALESCE(a.Descrizione,''),COALESCE(cat.Descrizione,'')
          FROM ImpegniLavoro i
          LEFT JOIN MacchineCli m ON m.ID=i.MacchinaCli_ID
          LEFT JOIN Articoli a ON a.Codice=m.Articolo
          LEFT JOIN Categorie cat ON cat.Codice=m.Categoria
          WHERE i.ID=@id AND i.Stato<>'X' FOR UPDATE
          """;
        int customer;int? machine;DateTime? plannedOn;TimeSpan? plannedAt;string description;string notes;int? existingWork;int? site;DateTime? lastService;string article;string articleDescription;string category;
        await using(var source=new MySqlCommand(sourceSql,cn,tx))
        {
            source.Parameters.AddWithValue("@id",commitmentId);await using var r=await source.ExecuteReaderAsync(ct);
            if(!await r.ReadAsync(ct))throw new InvalidOperationException("Prenotazione non trovata.");
            customer=r.GetInt32(0);machine=r.IsDBNull(1)?null:r.GetInt32(1);plannedOn=r.IsDBNull(2)?null:r.GetDateTime(2);plannedAt=r.IsDBNull(3)?null:r.GetTimeSpan(3);
            description=r.GetString(4);notes=r.GetString(5);existingWork=r.IsDBNull(6)?null:r.GetInt32(6);site=r.IsDBNull(7)?null:r.GetInt32(7);lastService=r.IsDBNull(8)?null:r.GetDateTime(8);
            article=r.GetString(9);articleDescription=r.GetString(10);category=r.GetString(11);
        }
        if(existingWork.HasValue){await tx.CommitAsync(ct);return existingWork.Value;}
        if(!plannedOn.HasValue)throw new InvalidOperationException("Definire la data della prenotazione prima di preparare la scheda.");

        var year=(short)DateTime.Today.Year;var code=1;
        await using(var next=new MySqlCommand("SELECT Codice FROM Lavori WHERE Anno=@year ORDER BY Codice DESC LIMIT 1 FOR UPDATE",cn,tx))
        {
            next.Parameters.AddWithValue("@year",year);var current=await next.ExecuteScalarAsync(ct);if(current is not null and not DBNull)code=Convert.ToInt32(current)+1;
        }
        var summary=string.IsNullOrWhiteSpace(description)?articleDescription:description;
        var reference=string.Join(" · ",new[]{article,articleDescription,category}.Where(x=>!string.IsNullOrWhiteSpace(x)));
        var instructions=string.Join(Environment.NewLine,new[]{reference,string.IsNullOrWhiteSpace(notes)?null:notes}.Where(x=>!string.IsNullOrWhiteSpace(x)));
        const string insertSql="""
          INSERT INTO Lavori
            (Anno,Codice,DataRedazione,DataInterventoPianificata,OraInterventoPianificata,DataUltimoIntervento,
             Cliente,Destino_ID,OperatoreAssegnato,StatoLavoro_ID,DescrizioneSintetica,IstruzioniOperative,
             ImportoManodoperaPreventivato,ImportoMaterialiPreventivato,ImportoPreventivoNetto,
             ImportoManodoperaConsuntivo,ImportoMaterialiConsuntivo,ImportoRichiesto,ImportoIncassato)
          VALUES
            (@year,@code,CURDATE(),@plannedOn,@plannedAt,@lastService,@customer,@site,0,1,NULLIF(@summary,''),NULLIF(@instructions,''),0,0,0,0,0,0,0);
          SELECT LAST_INSERT_ID();
          """;
        int workId;
        await using(var insert=new MySqlCommand(insertSql,cn,tx))
        {
            insert.Parameters.AddWithValue("@year",year);insert.Parameters.AddWithValue("@code",code);insert.Parameters.AddWithValue("@plannedOn",plannedOn.Value.Date);
            insert.Parameters.AddWithValue("@plannedAt",(object?)plannedAt??DBNull.Value);insert.Parameters.AddWithValue("@lastService",(object?)lastService??DBNull.Value);
            insert.Parameters.AddWithValue("@customer",customer);insert.Parameters.AddWithValue("@site",(object?)site??DBNull.Value);insert.Parameters.AddWithValue("@summary",summary.Trim());insert.Parameters.AddWithValue("@instructions",instructions.Trim());
            workId=Convert.ToInt32(await insert.ExecuteScalarAsync(ct));
        }
        await using(var history=new MySqlCommand("INSERT INTO LavoriStorico(Lavoro_ID,TipoEvento,DataEvento,StatoNuovo_ID,DataPianificataNuova,Note) VALUES(@work,'REDAZIONE_SCHEDA',NOW(),1,@planned,'Scheda preparata dalla prenotazione')",cn,tx))
        {history.Parameters.AddWithValue("@work",workId);history.Parameters.AddWithValue("@planned",plannedOn.Value.Date);await history.ExecuteNonQueryAsync(ct);}
        await using(var link=new MySqlCommand("UPDATE ImpegniLavoro SET Lavoro_ID=@work,Stato='T' WHERE ID=@id AND Lavoro_ID IS NULL",cn,tx))
        {link.Parameters.AddWithValue("@work",workId);link.Parameters.AddWithValue("@id",commitmentId);if(await link.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("La prenotazione è già stata trasformata.");}
        await tx.CommitAsync(ct);return workId;
    }

    public async Task<PlanningDayAvailability> DayAvailabilityAsync(DateTime date,CancellationToken ct)
    {
        date=date.Date;
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        const string summarySql="""
          SELECT
            (SELECT COUNT(*) FROM Utenti u WHERE COALESCE(u.Attivo,0)<>0 AND COALESCE(u.Bloccato,0)=0),
            (SELECT COUNT(DISTINCT NULLIF(l.OperatoreAssegnato,0)) FROM Lavori l WHERE l.DataInterventoPianificata=@date),
            (SELECT COUNT(*) FROM ImpegniLavoro i WHERE i.DataIntervento=@date AND i.Stato<>'X'),
            (SELECT COUNT(*) FROM Lavori l WHERE l.DataInterventoPianificata=@date);
          """;
        int active,assigned,reservations,works;
        await using(var cmd=new MySqlCommand(summarySql,cn))
        {
            cmd.Parameters.AddWithValue("@date",date);
            await using var r=await cmd.ExecuteReaderAsync(ct);await r.ReadAsync(ct);
            active=r.GetInt32(0);assigned=r.GetInt32(1);reservations=r.GetInt32(2);works=r.GetInt32(3);
        }
        const string detailsSql="""
          SELECT Ora,Cliente,Descrizione,Operatore FROM (
            SELECT i.OraIntervento AS Ora,COALESCE(c.Nome,'' ) AS Cliente,
                   COALESCE(i.Descrizione,'Prenotazione intervento') AS Descrizione,'' AS Operatore,0 AS Ordine
            FROM ImpegniLavoro i JOIN Clienti c ON c.Codice=i.Cliente_ID
            WHERE i.DataIntervento=@date AND i.Stato<>'X'
            UNION ALL
            SELECT l.OraInterventoPianificata,COALESCE(c.Nome,''),COALESCE(l.DescrizioneSintetica,'Scheda lavoro'),
                   COALESCE(NULLIF(CONCAT_WS(' ',u.Nome,u.Cognome),''),u.Username,''),1
            FROM Lavori l JOIN Clienti c ON c.Codice=l.Cliente
            LEFT JOIN Utenti u ON u.Codice=NULLIF(l.OperatoreAssegnato,0)
            WHERE l.DataInterventoPianificata=@date
          ) x ORDER BY COALESCE(Ora,'23:59:59'),Ordine,Cliente LIMIT 20;
          """;
        var details=new List<PlanningDayCommitment>();
        await using(var cmd=new MySqlCommand(detailsSql,cn))
        {
            cmd.Parameters.AddWithValue("@date",date);
            await using var r=await cmd.ExecuteReaderAsync(ct);
            while(await r.ReadAsync(ct))details.Add(new(
                r.IsDBNull(0)?"—":r.GetTimeSpan(0).ToString(@"hh\:mm"),r.GetString(1),r.GetString(2),r.GetString(3)));
        }
        var calendar=CalendarStatus(date);
        return new(date,calendar.Working,calendar.Status,active,assigned,reservations,works,details);
    }

    public async Task<IReadOnlyList<PlanningAgendaItem>> AgendaAsync(DateTime from,DateTime to,CancellationToken ct)
    {
        const string sql="""
          SELECT Data,Ora,Cliente,Sede,Descrizione,Operatore,Tipo FROM (
            SELECT i.DataIntervento AS Data,i.OraIntervento AS Ora,COALESCE(c.Nome,'') AS Cliente,
                   COALESCE(NULLIF(CONCAT_WS(' · ',NULLIF(TRIM(d.Nome),''),NULLIF(CONCAT_WS(' ',NULLIF(TRIM(d.Via),''),NULLIF(TRIM(d.Civico),''),NULLIF(TRIM(d.Citta),'')),'')),''),
                            NULLIF(CONCAT_WS(' ',NULLIF(TRIM(c.Via),''),NULLIF(TRIM(c.Civico),''),NULLIF(TRIM(c.Citta),'')),''),'Sede principale') AS Sede,
                   COALESCE(i.Descrizione,'Prenotazione intervento') AS Descrizione,
                   '' AS Operatore,'Prenotazione' AS Tipo
            FROM ImpegniLavoro i JOIN Clienti c ON c.Codice=i.Cliente_ID
            LEFT JOIN MacchineCli m ON m.ID=i.MacchinaCli_ID
            LEFT JOIN Destini d ON d.ID=m.DestinoID
            WHERE i.DataIntervento BETWEEN @from AND @to AND i.Stato<>'X'
            UNION ALL
            SELECT l.DataInterventoPianificata,l.OraInterventoPianificata,COALESCE(c.Nome,''),
                   COALESCE(NULLIF(CONCAT_WS(' ',NULLIF(TRIM(c.Via),''),NULLIF(TRIM(c.Civico),''),NULLIF(TRIM(c.Citta),'')),''),'Sede principale'),
                   COALESCE(l.DescrizioneSintetica,'Scheda lavoro'),
                   COALESCE(NULLIF(CONCAT_WS(' ',u.Nome,u.Cognome),''),u.Username,''),'Scheda lavoro'
            FROM Lavori l JOIN Clienti c ON c.Codice=l.Cliente
            LEFT JOIN Utenti u ON u.Codice=NULLIF(l.OperatoreAssegnato,0)
            WHERE l.DataInterventoPianificata BETWEEN @from AND @to
          ) x ORDER BY Data,COALESCE(Ora,'23:59:59'),Operatore,Cliente;
          """;
        var result=new List<PlanningAgendaItem>();
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);
        await using var cmd=new MySqlCommand(sql,cn);cmd.Parameters.AddWithValue("@from",from.Date);cmd.Parameters.AddWithValue("@to",to.Date);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetDateTime(0),r.IsDBNull(1)?"—":r.GetTimeSpan(1).ToString(@"hh\:mm"),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6)));
        return result;
    }

    private static (bool Working,string Status) CalendarStatus(DateTime date)
    {
        if(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)return(false,date.DayOfWeek==DayOfWeek.Saturday?"Sabato":"Domenica");
        var fixedHoliday=(date.Month,date.Day) switch{(1,1)=>"Capodanno",(1,6)=>"Epifania",(4,25)=>"Liberazione",(5,1)=>"Festa del lavoro",(6,2)=>"Festa della Repubblica",(8,15)=>"Ferragosto",(11,1)=>"Ognissanti",(12,8)=>"Immacolata",(12,25)=>"Natale",(12,26)=>"Santo Stefano",_=>null};
        if(fixedHoliday is not null)return(false,fixedHoliday);
        if(date==EasterSunday(date.Year).AddDays(1))return(false,"Lunedì dell'Angelo");
        return(true,"Giorno lavorativo");
    }

    private static DateTime EasterSunday(int year)
    {
        var a=year%19;var b=year/100;var c=year%100;var d=b/4;var e=b%4;var f=(b+8)/25;var g=(b-f+1)/3;
        var h=(19*a+b-d-g+15)%30;var i=c/4;var k=c%4;var l=(32+2*e+2*i-h-k)%7;var m=(a+11*h+22*l)/451;
        var month=(h+l-7*m+114)/31;var day=(h+l-7*m+114)%31+1;return new DateTime(year,month,day);
    }
}
