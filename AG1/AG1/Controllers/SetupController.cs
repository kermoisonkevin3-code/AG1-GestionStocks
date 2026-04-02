// AG1/Controllers/SetupController.cs
// Page temporaire pour créer/réinitialiser le compte admin
// SUPPRIMER ce fichier après la première connexion !

using AG1.Data;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace AG1.Controllers;

public class SetupController : Controller
{
    private readonly DatabaseContext _db;
    public SetupController(DatabaseContext db) => _db = db;

    // Accès : http://localhost:5000/Setup
    public async Task<IActionResult> Index()
    {
        // Génère un hash BCrypt.Net pour "Admin1234!"
        var hash = BCrypt.Net.BCrypt.HashPassword("Admin1234!", 12);

        using var conn = await _db.GetOpenConnectionAsync();

        // Met à jour tous les vendeurs avec le bon hash
        var cmd = new MySqlCommand(
            "UPDATE vendeurs SET mot_de_passe = @hash", conn);
        cmd.Parameters.AddWithValue("@hash", hash);
        var rows = await cmd.ExecuteNonQueryAsync();

        return Content($@"
            <h2>✅ Mots de passe réinitialisés ({rows} vendeur(s) mis à jour)</h2>
            <p>Hash généré : <code>{hash}</code></p>
            <hr>
            <h3>Comptes disponibles :</h3>
            <ul>
                <li>Email : <strong>admin@ag1.com</strong> — Mot de passe : <strong>Admin1234!</strong></li>
                <li>Email : <strong>sophie.martin@ag1.com</strong> — Mot de passe : <strong>Admin1234!</strong></li>
            </ul>
            <br>
            <a href='/Auth/Login' style='background:#00d084;color:#000;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:bold'>
                → Aller à la page de connexion
            </a>
            <br><br>
            <p style='color:red'><strong>⚠ Supprime le fichier SetupController.cs après ta première connexion !</strong></p>
        ", "text/html");
    }
}
