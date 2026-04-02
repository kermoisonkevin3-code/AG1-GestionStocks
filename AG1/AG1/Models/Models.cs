// AG1/Models/Models.cs
// Modèles de données correspondant à la BDD partagée

namespace AG1.Models;

// ---- Produit ----
public class Produit
{
    public int Id { get; set; }
    public int? CategorieId { get; set; }
    public string Nom { get; set; } = "";
    public string? Description { get; set; }
    public decimal Prix { get; set; }
    public decimal PrixAchat { get; set; }
    public int Stock { get; set; }
    public int StockMin { get; set; }
    public string? Reference { get; set; }
    public string? ImageUrl { get; set; }
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public string? CategorieNom { get; set; }
    // Statut stock calculé
    public string StatutStock => Stock <= 0 ? "rupture" : Stock <= StockMin ? "faible" : "ok";
}

// ---- Catégorie ----
public class Categorie
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Description { get; set; }
}

// ---- Vendeur ----
public class Vendeur
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Prenom { get; set; }
    public string Email { get; set; } = "";
    public string MotDePasse { get; set; } = "";
    public string Role { get; set; } = "vendeur";
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string NomComplet => $"{Prenom} {Nom}".Trim();
}

// ---- Commande ----
public class Commande
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Statut { get; set; } = "en_attente";
    public decimal Total { get; set; }
    public string? AdresseLivraison { get; set; }
    public string? VilleLivraison { get; set; }
    public string? CodePostalLivraison { get; set; }
    public string? PaysLivraison { get; set; }
    public string? Notes { get; set; }
    public int? VendeurId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public string? ClientNom { get; set; }
    public string? VendeurNom { get; set; }
    public List<CommandeLigne> Lignes { get; set; } = new();
}

// ---- Ligne de commande ----
public class CommandeLigne
{
    public int Id { get; set; }
    public int CommandeId { get; set; }
    public int ProduitId { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnit { get; set; }
    public string? ProduitNom { get; set; }
    public decimal SousTotal => PrixUnit * Quantite;
}

// ---- Mouvement de stock ----
public class MouvementStock
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public int VendeurId { get; set; }
    public string Type { get; set; } = "entree"; // entree, sortie, ajustement, retour
    public int Quantite { get; set; }
    public string? Motif { get; set; }
    public int? CommandeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProduitNom { get; set; }
    public string? VendeurNom { get; set; }
}

// ---- Client ----
public class Client
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Prenom { get; set; }
    public string Email { get; set; } = "";
    public string? Telephone { get; set; }
    public string? Ville { get; set; }
    public bool Actif { get; set; }
    public DateTime CreatedAt { get; set; }
    public string NomComplet => $"{Prenom} {Nom}".Trim();
}

// ---- ViewModels ----
public class DashboardViewModel
{
    public int TotalProduits { get; set; }
    public int ProduitsStockFaible { get; set; }
    public int ProduitsRupture { get; set; }
    public int CommandesEnAttente { get; set; }
    public int CommandesAujourdhui { get; set; }
    public decimal ChiffreAffaires30j { get; set; }
    public List<Produit> ProduitsAlerte { get; set; } = new();
    public List<Commande> DernieresCommandes { get; set; } = new();
}

public class LoginViewModel
{
    public string Email { get; set; } = "";
    public string MotDePasse { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class StockActionViewModel
{
    public int ProduitId { get; set; }
    public string Type { get; set; } = "entree";
    public int Quantite { get; set; }
    public string? Motif { get; set; }
}

public class ProduitFormViewModel
{
    public Produit Produit { get; set; } = new();
    public List<Categorie> Categories { get; set; } = new();
}
