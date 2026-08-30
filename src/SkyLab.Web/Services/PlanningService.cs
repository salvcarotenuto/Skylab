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
                   i.ID,COALESCE(i.Stato,''),i.DataIntervento,i.OraIntervento
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
                reader.GetDateTime(13),reader.IsDBNull(14)?null:reader.GetInt32(14),reader.GetString(15),
                reader.IsDBNull(16)?null:reader.GetDateTime(16),reader.IsDBNull(17)?null:reader.GetTimeSpan(17)));
        }
        return result;
    }

    public async Task<int> AcquireCommitmentAsync(int machineId,int customerId,DateTime dueDate,DateTime? agreedOn,TimeSpan? agreedAt,string? description,string? notes,CancellationToken ct)
    {
        await using var cn=new MySqlConnection(ConnectionString);await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        const string checkSql="SELECT COUNT(*) FROM MacchineCli WHERE ID=@machine AND Cliente=@customer AND ProxData=@due";
        await using(var check=new MySqlCommand(checkSql,cn,tx)){check.Parameters.AddWithValue("@machine",machineId);check.Parameters.AddWithValue("@customer",customerId);check.Parameters.AddWithValue("@due",dueDate.Date);if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Scadenza non più disponibile.");}
        const string existingSql="SELECT ID FROM ImpegniLavoro WHERE MacchinaCli_ID=@machine AND DataScadenzaOrigine=@due AND Stato<>'X' ORDER BY ID DESC LIMIT 1";
        await using(var existing=new MySqlCommand(existingSql,cn,tx)){existing.Parameters.AddWithValue("@machine",machineId);existing.Parameters.AddWithValue("@due",dueDate.Date);var id=await existing.ExecuteScalarAsync(ct);if(id is not null)return Convert.ToInt32(id);}
        const string sql="""
          INSERT INTO ImpegniLavoro(Cliente_ID,MacchinaCli_ID,Origine,DataScadenzaOrigine,DataAcquisizione,DataIntervento,OraIntervento,Stato,Descrizione,Note)
          VALUES(@customer,@machine,'P',@due,NOW(),@agreedOn,@agreedAt,CASE WHEN @agreedOn IS NULL THEN 'A' ELSE 'P' END,NULLIF(@description,''),NULLIF(@notes,''));
          SELECT LAST_INSERT_ID();
          """;
        await using var cmd=new MySqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@customer",customerId);cmd.Parameters.AddWithValue("@machine",machineId);cmd.Parameters.AddWithValue("@due",dueDate.Date);cmd.Parameters.AddWithValue("@agreedOn",(object?)agreedOn?.Date??DBNull.Value);cmd.Parameters.AddWithValue("@agreedAt",(object?)agreedAt??DBNull.Value);cmd.Parameters.AddWithValue("@description",description?.Trim()??"");cmd.Parameters.AddWithValue("@notes",notes?.Trim()??"");
        var result=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));await tx.CommitAsync(ct);return result;
    }
}
