// AG1/Services/CommandeService.cs
using AG1.Data;
using AG1.Models;
using MySql.Data.MySqlClient;

namespace AG1.Services;

public interface ICommandeService
{
    Task<List<Commande>> GetAllCommandesAsync(string? statut = null, string? search = null);
    Task<Commande?> GetCommandeByIdAsync(int id);
    Task<bool> UpdateStatutAsync(int id, string statut, int vendeurId);
    Task<List<Client>> GetClientsAsync(string? search = null);
}

public class CommandeService : ICommandeService
{
    private readonly DatabaseContext _db;
    public CommandeService(DatabaseContext db) => _db = db;

    public async Task<List<Commande>> GetAllCommandesAsync(string? statut = null, string? search = null)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var where = new List<string> { "1=1" };
        if (!string.IsNullOrWhiteSpace(statut)) where.Add("c.statut = @statut");
        if (!string.IsNullOrWhiteSpace(search)) where.Add("(cl.nom LIKE @search OR cl.email LIKE @search)");

        var sql = $@"
            SELECT c.*, CONCAT(cl.prenom, ' ', cl.nom) as client_nom,
                   CONCAT(v.prenom, ' ', v.nom) as vendeur_nom
            FROM commandes c
            JOIN clients cl ON c.client_id = cl.id
            LEFT JOIN vendeurs v ON c.vendeur_id = v.id
            WHERE {string.Join(" AND ", where)}
            ORDER BY c.created_at DESC";

        var cmd = new MySqlCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(statut)) cmd.Parameters.AddWithValue("@statut", statut);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");

        var list = new List<Commande>();
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Commande
            {
                Id                  = reader.GetInt32("id"),
                ClientId            = reader.GetInt32("client_id"),
                Statut              = reader.GetString("statut"),
                Total               = reader.GetDecimal("total"),
                AdresseLivraison    = reader.IsDBNull(reader.GetOrdinal("adresse_livraison"))     ? null : reader.GetString("adresse_livraison"),
                VilleLivraison      = reader.IsDBNull(reader.GetOrdinal("ville_livraison"))       ? null : reader.GetString("ville_livraison"),
                CodePostalLivraison = reader.IsDBNull(reader.GetOrdinal("code_postal_livraison")) ? null : reader.GetString("code_postal_livraison"),
                Notes               = reader.IsDBNull(reader.GetOrdinal("notes"))                 ? null : reader.GetString("notes"),
                VendeurId           = reader.IsDBNull(reader.GetOrdinal("vendeur_id"))            ? null : reader.GetInt32("vendeur_id"),
                CreatedAt           = reader.GetDateTime("created_at"),
                UpdatedAt           = reader.GetDateTime("updated_at"),
                ClientNom           = reader.GetString("client_nom"),
                VendeurNom          = reader.IsDBNull(reader.GetOrdinal("vendeur_nom"))           ? null : reader.GetString("vendeur_nom"),
            });
        }
        return list;
    }

    public async Task<Commande?> GetCommandeByIdAsync(int id)
    {
        using var conn = await _db.GetOpenConnectionAsync();

        var cmd = new MySqlCommand(@"
            SELECT c.*, CONCAT(cl.prenom, ' ', cl.nom) as client_nom,
                   cl.email as client_email, cl.telephone as client_tel
            FROM commandes c
            JOIN clients cl ON c.client_id = cl.id
            WHERE c.id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        Commande? commande = null;
        using (var r = (MySqlDataReader)await cmd.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                commande = new Commande
                {
                    Id                  = r.GetInt32("id"),
                    ClientId            = r.GetInt32("client_id"),
                    Statut              = r.GetString("statut"),
                    Total               = r.GetDecimal("total"),
                    AdresseLivraison    = r.IsDBNull(r.GetOrdinal("adresse_livraison"))     ? null : r.GetString("adresse_livraison"),
                    VilleLivraison      = r.IsDBNull(r.GetOrdinal("ville_livraison"))       ? null : r.GetString("ville_livraison"),
                    Notes               = r.IsDBNull(r.GetOrdinal("notes"))                 ? null : r.GetString("notes"),
                    VendeurId           = r.IsDBNull(r.GetOrdinal("vendeur_id"))            ? null : r.GetInt32("vendeur_id"),
                    CreatedAt           = r.GetDateTime("created_at"),
                    UpdatedAt           = r.GetDateTime("updated_at"),
                    ClientNom           = r.GetString("client_nom"),
                };
            }
        }
        if (commande == null) return null;

        var ligneCmd = new MySqlCommand(@"
            SELECT cl.*, p.nom as produit_nom
            FROM commande_lignes cl
            JOIN produits p ON cl.produit_id = p.id
            WHERE cl.commande_id = @id", conn);
        ligneCmd.Parameters.AddWithValue("@id", id);

        using var lr = (MySqlDataReader)await ligneCmd.ExecuteReaderAsync();
        while (await lr.ReadAsync())
        {
            commande.Lignes.Add(new CommandeLigne
            {
                Id         = lr.GetInt32("id"),
                CommandeId = lr.GetInt32("commande_id"),
                ProduitId  = lr.GetInt32("produit_id"),
                Quantite   = lr.GetInt32("quantite"),
                PrixUnit   = lr.GetDecimal("prix_unit"),
                ProduitNom = lr.GetString("produit_nom"),
            });
        }
        return commande;
    }

    public async Task<bool> UpdateStatutAsync(int id, string statut, int vendeurId)
    {
        // CRITÈRE 14 – Bug #004 corrigé : transaction atomique + log statut
        using var conn = await _db.GetOpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();
        try
        {
            // 1. Récupérer l'ancien statut pour le log
            var getCmd = new MySqlCommand("SELECT statut FROM commandes WHERE id = @id", conn, (MySqlTransaction)transaction);
            getCmd.Parameters.AddWithValue("@id", id);
            var ancienStatut = (string?)await getCmd.ExecuteScalarAsync() ?? "";

            // 2. Mettre à jour le statut
            var cmd = new MySqlCommand(@"
                UPDATE commandes SET statut = @statut, vendeur_id = @vendeurId WHERE id = @id",
                conn, (MySqlTransaction)transaction);
            cmd.Parameters.AddWithValue("@statut",    statut);
            cmd.Parameters.AddWithValue("@vendeurId", vendeurId);
            cmd.Parameters.AddWithValue("@id",        id);
            var rows = await cmd.ExecuteNonQueryAsync();

            // 3. Si passage en expediée → décrémenter stock (CRITÈRE 18 trigger)
            if (statut == "expediee" && ancienStatut != "expediee")
            {
                var stockCmd = new MySqlCommand(@"
                    UPDATE produits p
                    INNER JOIN commande_lignes cl ON cl.produit_id = p.id
                    SET p.stock = GREATEST(0, p.stock - cl.quantite)
                    WHERE cl.commande_id = @id AND p.stock >= cl.quantite",
                    conn, (MySqlTransaction)transaction);
                stockCmd.Parameters.AddWithValue("@id", id);
                await stockCmd.ExecuteNonQueryAsync();

                // Log mouvement stock (CRITÈRE 18)
                var logCmd = new MySqlCommand(@"
                    INSERT INTO mouvements_stock (produit_id, vendeur_id, type, quantite, motif, commande_id)
                    SELECT cl.produit_id, @vendeurId, 'sortie', cl.quantite,
                           CONCAT('Expédition commande #', @cmdId), @cmdId
                    FROM commande_lignes cl WHERE cl.commande_id = @cmdId",
                    conn, (MySqlTransaction)transaction);
                logCmd.Parameters.AddWithValue("@vendeurId", vendeurId);
                logCmd.Parameters.AddWithValue("@cmdId", id);
                await logCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return rows > 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<List<Client>> GetClientsAsync(string? search = null)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var where = string.IsNullOrWhiteSpace(search) ? "" : "WHERE nom LIKE @search OR email LIKE @search";
        var cmd = new MySqlCommand($"SELECT * FROM clients {where} ORDER BY nom LIMIT 100", conn);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");

        var list = new List<Client>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new Client
            {
                Id        = r.GetInt32("id"),
                Nom       = r.GetString("nom"),
                Prenom    = r.IsDBNull(r.GetOrdinal("prenom"))    ? null : r.GetString("prenom"),
                Email     = r.GetString("email"),
                Telephone = r.IsDBNull(r.GetOrdinal("telephone")) ? null : r.GetString("telephone"),
                Ville     = r.IsDBNull(r.GetOrdinal("ville"))     ? null : r.GetString("ville"),
                Actif     = r.GetBoolean("actif"),
                CreatedAt = r.GetDateTime("created_at"),
            });
        }
        return list;
    }
}
