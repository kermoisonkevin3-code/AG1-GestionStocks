// AG1/Services/AuthService.cs
using AG1.Data;
using AG1.Models;
using MySql.Data.MySqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AG1.Services;

public interface IAuthService
{
    Task<Vendeur?> ValidateLoginAsync(string email, string motDePasse);
    Task SignInAsync(HttpContext context, Vendeur vendeur, bool rememberMe);
    Task SignOutAsync(HttpContext context);
    Task<Vendeur?> GetVendeurByIdAsync(int id);
    Task<List<Vendeur>> GetAllVendeursAsync();
    Task<bool> CreateVendeurAsync(Vendeur vendeur, string motDePasse);
    Task<bool> UpdateVendeurAsync(Vendeur vendeur);
    Task<bool> ToggleVendeurAsync(int id);
}

public class AuthService : IAuthService
{
    private readonly DatabaseContext _db;
    public AuthService(DatabaseContext db) => _db = db;

    public async Task<Vendeur?> ValidateLoginAsync(string email, string motDePasse)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT * FROM vendeurs WHERE email = @email AND actif = 1 LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var vendeur = MapVendeur(reader);
        if (!BCrypt.Net.BCrypt.Verify(motDePasse, vendeur.MotDePasse)) return null;
        // CRITÈRE 18 – Log de connexion
        try {
            using var logConn = await _db.GetOpenConnectionAsync();
            var logCmd = new MySqlCommand(
                "INSERT IGNORE INTO logs_connexion (client_id, role, action, ip_address) VALUES (@id, @role, 'connexion_ag1', '0.0.0.0')",
                logConn);
            logCmd.Parameters.AddWithValue("@id",   vendeur.Id);
            logCmd.Parameters.AddWithValue("@role", vendeur.Role);
            await logCmd.ExecuteNonQueryAsync();
        } catch { /* silencieux si table absente */ }

        return vendeur;
    }

    public async Task SignInAsync(HttpContext context, Vendeur vendeur, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, vendeur.Id.ToString()),
            new(ClaimTypes.Name, vendeur.NomComplet),
            new(ClaimTypes.Email, vendeur.Email),
            new(ClaimTypes.Role, vendeur.Role),
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(rememberMe ? 24 : 8)
        };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    public async Task SignOutAsync(HttpContext context)
        => await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public async Task<Vendeur?> GetVendeurByIdAsync(int id)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand("SELECT * FROM vendeurs WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapVendeur(reader) : null;
    }

    public async Task<List<Vendeur>> GetAllVendeursAsync()
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand("SELECT * FROM vendeurs ORDER BY nom", conn);
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        var list = new List<Vendeur>();
        while (await reader.ReadAsync()) list.Add(MapVendeur(reader));
        return list;
    }

    public async Task<bool> CreateVendeurAsync(Vendeur vendeur, string motDePasse)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var hash = BCrypt.Net.BCrypt.HashPassword(motDePasse, 12);
        var cmd = new MySqlCommand(@"
            INSERT INTO vendeurs (nom, prenom, email, mot_de_passe, role)
            VALUES (@nom, @prenom, @email, @mdp, @role)", conn);
        cmd.Parameters.AddWithValue("@nom",    vendeur.Nom);
        cmd.Parameters.AddWithValue("@prenom", vendeur.Prenom ?? "");
        cmd.Parameters.AddWithValue("@email",  vendeur.Email);
        cmd.Parameters.AddWithValue("@mdp",    hash);
        cmd.Parameters.AddWithValue("@role",   vendeur.Role);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateVendeurAsync(Vendeur vendeur)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand(@"
            UPDATE vendeurs SET nom=@nom, prenom=@prenom, email=@email, role=@role WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@nom",    vendeur.Nom);
        cmd.Parameters.AddWithValue("@prenom", vendeur.Prenom ?? "");
        cmd.Parameters.AddWithValue("@email",  vendeur.Email);
        cmd.Parameters.AddWithValue("@role",   vendeur.Role);
        cmd.Parameters.AddWithValue("@id",     vendeur.Id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ToggleVendeurAsync(int id)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand("UPDATE vendeurs SET actif = NOT actif WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Vendeur MapVendeur(MySqlDataReader r) => new()
    {
        Id         = r.GetInt32("id"),
        Nom        = r.GetString("nom"),
        Prenom     = r.IsDBNull(r.GetOrdinal("prenom")) ? null : r.GetString("prenom"),
        Email      = r.GetString("email"),
        MotDePasse = r.GetString("mot_de_passe"),
        Role       = r.GetString("role"),
        Actif      = r.GetBoolean("actif"),
        CreatedAt  = r.GetDateTime("created_at"),
    };
}
