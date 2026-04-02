// AG1/Services/StockService.cs
using AG1.Data;
using AG1.Models;
using MySql.Data.MySqlClient;

namespace AG1.Services;

public interface IStockService
{
    Task<List<Produit>> GetAllProduitsAsync(string? search = null, int? categorieId = null, string? statut = null);
    Task<Produit?> GetProduitByIdAsync(int id);
    Task<bool> CreateProduitAsync(Produit produit);
    Task<bool> UpdateProduitAsync(Produit produit);
    Task<bool> ToggleProduitAsync(int id);
    Task<bool> AjusterStockAsync(int produitId, int vendeurId, string type, int quantite, string? motif, int? commandeId = null);
    Task<List<MouvementStock>> GetMouvementsAsync(int? produitId = null, int limit = 50);
    Task<List<Categorie>> GetCategoriesAsync();
    Task<DashboardViewModel> GetDashboardDataAsync();
}

public class StockService : IStockService
{
    private readonly DatabaseContext _db;
    public StockService(DatabaseContext db) => _db = db;

    public async Task<List<Produit>> GetAllProduitsAsync(string? search = null, int? categorieId = null, string? statut = null)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var where = new List<string> { "1=1" };
        if (!string.IsNullOrWhiteSpace(search)) where.Add("(p.nom LIKE @search OR p.reference LIKE @search)");
        if (categorieId.HasValue) where.Add("p.categorie_id = @catId");
        if (statut == "faible")  where.Add("p.stock > 0 AND p.stock <= p.stock_min");
        if (statut == "rupture") where.Add("p.stock = 0");
        if (statut == "ok")      where.Add("p.stock > p.stock_min");

        var sql = $@"SELECT p.*, c.nom as categorie_nom
                     FROM produits p
                     LEFT JOIN categories c ON p.categorie_id = c.id
                     WHERE {string.Join(" AND ", where)}
                     ORDER BY p.nom";
        var cmd = new MySqlCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");
        if (categorieId.HasValue) cmd.Parameters.AddWithValue("@catId", categorieId.Value);

        var list = new List<Produit>();
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(MapProduit(reader));
        return list;
    }

    public async Task<Produit?> GetProduitByIdAsync(int id)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand(@"
            SELECT p.*, c.nom as categorie_nom FROM produits p
            LEFT JOIN categories c ON p.categorie_id = c.id
            WHERE p.id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapProduit(reader) : null;
    }

    public async Task<bool> CreateProduitAsync(Produit p)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand(@"
            INSERT INTO produits (categorie_id, nom, description, prix, prix_achat, stock, stock_min, reference, image_url, actif)
            VALUES (@catId, @nom, @desc, @prix, @prixA, @stock, @stockMin, @ref, @img, @actif)", conn);
        cmd.Parameters.AddWithValue("@catId", p.CategorieId.HasValue ? (object)p.CategorieId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@nom", p.Nom);
        cmd.Parameters.AddWithValue("@desc", p.Description ?? "");
        cmd.Parameters.AddWithValue("@prix", p.Prix);
        cmd.Parameters.AddWithValue("@prixA", p.PrixAchat);
        cmd.Parameters.AddWithValue("@stock", p.Stock);
        cmd.Parameters.AddWithValue("@stockMin", p.StockMin);
        cmd.Parameters.AddWithValue("@ref", p.Reference ?? "");
        cmd.Parameters.AddWithValue("@img", p.ImageUrl ?? "");
        cmd.Parameters.AddWithValue("@actif", p.Actif);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateProduitAsync(Produit p)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand(@"
            UPDATE produits SET categorie_id=@catId, nom=@nom, description=@desc, prix=@prix,
            prix_achat=@prixA, stock_min=@stockMin, reference=@ref, image_url=@img, actif=@actif
            WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@catId", p.CategorieId.HasValue ? (object)p.CategorieId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@nom", p.Nom);
        cmd.Parameters.AddWithValue("@desc", p.Description ?? "");
        cmd.Parameters.AddWithValue("@prix", p.Prix);
        cmd.Parameters.AddWithValue("@prixA", p.PrixAchat);
        cmd.Parameters.AddWithValue("@stockMin", p.StockMin);
        cmd.Parameters.AddWithValue("@ref", p.Reference ?? "");
        cmd.Parameters.AddWithValue("@img", p.ImageUrl ?? "");
        cmd.Parameters.AddWithValue("@actif", p.Actif);
        cmd.Parameters.AddWithValue("@id", p.Id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ToggleProduitAsync(int id)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand("UPDATE produits SET actif = NOT actif WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> AjusterStockAsync(int produitId, int vendeurId, string type, int quantite, string? motif, int? commandeId = null)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var tx = (MySqlTransaction)await conn.BeginTransactionAsync();
        try
        {
            var delta = type == "entree" || type == "retour" ? quantite : -quantite;
            var upd = new MySqlCommand(
                "UPDATE produits SET stock = GREATEST(0, stock + @delta) WHERE id = @id", conn, tx);
            upd.Parameters.AddWithValue("@delta", delta);
            upd.Parameters.AddWithValue("@id", produitId);
            await upd.ExecuteNonQueryAsync();

            var ins = new MySqlCommand(@"
                INSERT INTO mouvements_stock (produit_id, vendeur_id, type, quantite, motif, commande_id)
                VALUES (@pId, @vId, @type, @qty, @motif, @cmdId)", conn, tx);
            ins.Parameters.AddWithValue("@pId", produitId);
            ins.Parameters.AddWithValue("@vId", vendeurId);
            ins.Parameters.AddWithValue("@type", type);
            ins.Parameters.AddWithValue("@qty", quantite);
            ins.Parameters.AddWithValue("@motif", motif ?? "");
            ins.Parameters.AddWithValue("@cmdId", commandeId.HasValue ? (object)commandeId.Value : DBNull.Value);
            await ins.ExecuteNonQueryAsync();

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            return false;
        }
    }

    public async Task<List<MouvementStock>> GetMouvementsAsync(int? produitId = null, int limit = 50)
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var where = produitId.HasValue ? "WHERE ms.produit_id = @pId" : "";
        var cmd = new MySqlCommand($@"
            SELECT ms.*, p.nom as produit_nom, CONCAT(v.prenom, ' ', v.nom) as vendeur_nom
            FROM mouvements_stock ms
            JOIN produits p ON ms.produit_id = p.id
            JOIN vendeurs v ON ms.vendeur_id = v.id
            {where}
            ORDER BY ms.created_at DESC LIMIT @limit", conn);
        if (produitId.HasValue) cmd.Parameters.AddWithValue("@pId", produitId.Value);
        cmd.Parameters.AddWithValue("@limit", limit);

        var list = new List<MouvementStock>();
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new MouvementStock
            {
                Id         = reader.GetInt32("id"),
                ProduitId  = reader.GetInt32("produit_id"),
                VendeurId  = reader.GetInt32("vendeur_id"),
                Type       = reader.GetString("type"),
                Quantite   = reader.GetInt32("quantite"),
                Motif      = reader.IsDBNull(reader.GetOrdinal("motif")) ? null : reader.GetString("motif"),
                CreatedAt  = reader.GetDateTime("created_at"),
                ProduitNom = reader.GetString("produit_nom"),
                VendeurNom = reader.GetString("vendeur_nom"),
            });
        }
        return list;
    }

    public async Task<List<Categorie>> GetCategoriesAsync()
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var cmd = new MySqlCommand("SELECT * FROM categories ORDER BY nom", conn);
        var list = new List<Categorie>();
        using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new Categorie { Id = reader.GetInt32("id"), Nom = reader.GetString("nom") });
        return list;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync()
    {
        using var conn = await _db.GetOpenConnectionAsync();
        var vm = new DashboardViewModel();

        // Stats produits
        var cmd1 = new MySqlCommand(@"
            SELECT COUNT(*) as total,
              SUM(CASE WHEN stock = 0 THEN 1 ELSE 0 END) as rupture,
              SUM(CASE WHEN stock > 0 AND stock <= stock_min THEN 1 ELSE 0 END) as faible
            FROM produits WHERE actif = 1", conn);
        using (var r = (MySqlDataReader)await cmd1.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                vm.TotalProduits       = r.GetInt32("total");
                vm.ProduitsRupture     = r.GetInt32("rupture");
                vm.ProduitsStockFaible = r.GetInt32("faible");
            }
        }

        // Stats commandes
        var cmd2 = new MySqlCommand(@"
            SELECT
              SUM(CASE WHEN statut='en_attente' THEN 1 ELSE 0 END) as attente,
              SUM(CASE WHEN DATE(created_at) = CURDATE() THEN 1 ELSE 0 END) as aujourdhui,
              SUM(CASE WHEN created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY) THEN total ELSE 0 END) as ca30j
            FROM commandes", conn);
        using (var r = (MySqlDataReader)await cmd2.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                vm.CommandesEnAttente  = r.IsDBNull(r.GetOrdinal("attente"))    ? 0 : r.GetInt32("attente");
                vm.CommandesAujourdhui = r.IsDBNull(r.GetOrdinal("aujourdhui")) ? 0 : r.GetInt32("aujourdhui");
                vm.ChiffreAffaires30j  = r.IsDBNull(r.GetOrdinal("ca30j"))      ? 0 : r.GetDecimal("ca30j");
            }
        }

        // Produits en alerte
        var cmd3 = new MySqlCommand(@"
            SELECT p.*, c.nom as categorie_nom FROM produits p
            LEFT JOIN categories c ON p.categorie_id = c.id
            WHERE p.actif = 1 AND p.stock <= p.stock_min ORDER BY p.stock ASC LIMIT 5", conn);
        using (var r = (MySqlDataReader)await cmd3.ExecuteReaderAsync())
        {
            while (await r.ReadAsync()) vm.ProduitsAlerte.Add(MapProduit(r));
        }

        // Dernières commandes
        var cmd4 = new MySqlCommand(@"
            SELECT c.*, CONCAT(cl.prenom, ' ', cl.nom) as client_nom
            FROM commandes c
            JOIN clients cl ON c.client_id = cl.id
            ORDER BY c.created_at DESC LIMIT 5", conn);
        using (var r = (MySqlDataReader)await cmd4.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                vm.DernieresCommandes.Add(new Commande
                {
                    Id        = r.GetInt32("id"),
                    ClientId  = r.GetInt32("client_id"),
                    Statut    = r.GetString("statut"),
                    Total     = r.GetDecimal("total"),
                    CreatedAt = r.GetDateTime("created_at"),
                    ClientNom = r.GetString("client_nom"),
                });
            }
        }

        return vm;
    }

    private static Produit MapProduit(MySqlDataReader r)
    {
        int catNomOrdinal = -1;
        try { catNomOrdinal = r.GetOrdinal("categorie_nom"); } catch { }

        return new Produit
        {
            Id           = r.GetInt32("id"),
            CategorieId  = r.IsDBNull(r.GetOrdinal("categorie_id")) ? null : r.GetInt32("categorie_id"),
            Nom          = r.GetString("nom"),
            Description  = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString("description"),
            Prix         = r.GetDecimal("prix"),
            PrixAchat    = r.GetDecimal("prix_achat"),
            Stock        = r.GetInt32("stock"),
            StockMin     = r.GetInt32("stock_min"),
            Reference    = r.IsDBNull(r.GetOrdinal("reference")) ? null : r.GetString("reference"),
            ImageUrl     = r.IsDBNull(r.GetOrdinal("image_url")) ? null : r.GetString("image_url"),
            Actif        = r.GetBoolean("actif"),
            CreatedAt    = r.GetDateTime("created_at"),
            CategorieNom = catNomOrdinal >= 0 && !r.IsDBNull(catNomOrdinal) ? r.GetString(catNomOrdinal) : null,
        };
    }
}
