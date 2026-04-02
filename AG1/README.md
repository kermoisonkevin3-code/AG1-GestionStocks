# GroupyVendeur – Application de gestion stocks (C# ASP.NET Core 8)

> **BTS SIO SLAM – Session 2026 · KERMOISON Kevin · INGETIS Paris**
> Projet lourd (client C#) · Alternance Lexramax Inc – Nanterre

---

## 📋 Description

GroupyVendeur est une application web de back-office destinée aux vendeurs de la plateforme Groupy.
Développée en **C# ASP.NET Core 8 MVC**, elle permet de gérer les produits, surveiller les stocks,
traiter les commandes passées sur GroupyClient (PHP) et analyser les statistiques de vente.

La base de données **ecommerce_db** est **partagée** avec GroupyClient (PHP), garantissant
une synchronisation temps réel sans API intermédiaire.

---

## 🛠 Stack technologique

| Couche | Technologie |
|--------|------------|
| Langage | C# 12 (.NET 8) |
| Framework | ASP.NET Core 8 MVC |
| Accès BDD | MySql.Data 8.3 (MySqlCommand paramétré) |
| Authentification | Microsoft.AspNetCore.Authentication.Cookies |
| Sécurité mots de passe | BCrypt.Net-Next 4.0.3 |
| Vues | Razor (.cshtml) + CSS personnalisé + Vanilla JS |
| IDE | Visual Studio 2022 |
| Tests | Postman, débogueur VS2022 |
| Versioning | Git / GitHub |

---

## 🚀 Installation

### Prérequis
- .NET 8 SDK
- Visual Studio 2022 (workload .NET Desktop + Web)
- MySQL 8 accessible (WAMP démarré)
- Base `ecommerce_db` importée depuis `ecommerce_db.sql`

### Étapes

```bash
# 1. Cloner le dépôt
git clone https://github.com/kermoisonkevin/GroupyVendeur.git
cd GroupyVendeur

# 2. Restaurer les packages NuGet
dotnet restore

# 3. Vérifier la connexion BDD dans Data/DatabaseContext.cs
# Server=localhost;Database=ecommerce_db;Uid=root;Pwd=;

# 4. Compiler et lancer
dotnet run
# → http://localhost:5000
```

---

## 🔐 Comptes de démonstration

| Rôle | Email | Mot de passe |
|------|-------|-------------|
| Admin | admin@ag1.com | Admin1234! |
| Vendeur | sophie.martin@ag1.com | Admin1234! |

---

## 📁 Structure du projet

```
GroupyVendeur/
├── Program.cs                    # Point d'entrée + DI
├── appsettings.json              # Configuration
├── GroupyVendeur.csproj          # NuGet packages
│
├── Data/
│   └── DatabaseContext.cs        # Connexion MySQL (MySqlCommand)
│
├── Models/
│   └── Models.cs                 # Entités + ViewModels
│
├── Services/
│   ├── AuthService.cs            # Login/logout BCrypt
│   ├── StockService.cs           # CRUD produits + ajustement stock
│   └── CommandeService.cs        # Gestion commandes E1
│
├── Controllers/
│   ├── AuthController.cs         # Login/logout
│   ├── HomeController.cs         # Dashboard KPIs
│   ├── ProductsController.cs     # Produits + ajustement stock
│   └── OrdersController.cs       # Commandes + clients
│
├── Views/
│   ├── Shared/_Layout.cshtml     # Layout commun (sidebar + topbar)
│   ├── Auth/Login.cshtml         # Connexion
│   ├── Home/Index.cshtml         # Dashboard
│   ├── Products/                 # Index, Details, Create, Edit
│   └── Orders/                   # Index, Details, Clients
│
└── wwwroot/
    ├── css/ag1.css               # Styles (thème sombre industriel)
    └── js/ag1.js                 # JS (confirmations, toasts)
```

---

## 🛡 Sécurité (Critère 7)

- **BCrypt.Net** – `HashPassword()` / `Verify()` (cost=12, format $2b$)
- **Cookie Authentication** – Claims (Id, Name, Email, Role)
- **[ValidateAntiForgeryToken]** – CSRF sur tous les POST
- **MySqlCommand paramétré** – Anti-injection SQL (`@param`)
- **[Authorize(Roles=)]** – Contrôle d'accès par rôle

---

## 📊 Fonctionnalités principales

- **Dashboard** – 6 KPIs temps réel (stock faible, ruptures, commandes, CA 30j)
- **Produits** – CRUD complet avec ajustement stock (4 types + motif)
- **Commandes** – Traitement commandes E1 + flux de statut visuel
- **Clients** – Liste acheteurs ayant participé aux préventes
- **Statistiques** – Évolution stock et commandes

---

## 🧪 Tests (Critère 9)

Voir [`TESTS.md`](TESTS.md) pour le tableau de recette.

---

## 📖 Documentation

- [`CDC_AG1_GestionStocks.docx`](docs/) – Cahier des charges
- [`Manuel_AG1_GestionStocks.docx`](docs/) – Manuel vendeur
- [`CHANGELOG.md`](CHANGELOG.md) – Historique des versions

---

## 👤 Auteur

**Kevin Kermoison** – Étudiant BTS SIO SLAM  
INGETIS Paris · Alternance Lexramax Inc · Session 2026
