// AG1/Data/DatabaseContext.cs
// Contexte de connexion MySQL partagée (même BDD que E1)

using MySql.Data.MySqlClient;

namespace AG1.Data;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<MySqlConnection> GetOpenConnectionAsync()
    {
        var conn = CreateConnection();
        await conn.OpenAsync();
        return conn;
    }
}
