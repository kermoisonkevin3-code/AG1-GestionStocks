# CHANGELOG – GroupyClient & GroupyVendeur

> Toutes les modifications notables sont documentées dans ce fichier.
> Format : [Sémantique de version](https://semver.org) · Date ISO 8601

---

## [2.1.0] – 2026-03-26 – Système de parrainage

### Ajouté (GroupyClient)
- `parrainages` : nouvelle table BDD (parrain_id, filleul_id, code, remise)
- Champ `code_parrainage` dans `register.php` (optionnel)
- Fonctions PHP : `getOrCreateCodeParrainage()`, `lierFilleulParrain()`,
  `appliquerRemiseParrain()`, `getParrainageInfo()`
- Section "Mon espace parrainage" dans `mes-preventes.php`
- Déclenchement automatique remise 10€ à la 1ère commande du filleul
- Protection anti-auto-parrainage (`parrain_id ≠ filleul_id`)
- Protection double remise (colonne `remise_accordee`)

### Modifié
- `register.php` : ajout champ code parrainage optionnel
- `clients` : 2 nouvelles colonnes (`credit_remise`, `code_parrainage`)
- `includes/config.php` : 5 nouvelles fonctions parrainage

---

## [2.0.0] – 2026-03-15 – Bootstrap 5 + API REST

### Ajouté (GroupyClient)
- Intégration **Bootstrap 5.3** : navbar responsive, cards, badges, progress,
  pagination, toasts, accordion, breadcrumb (Critère 4)
- **API REST JSON** : endpoint `api/index.php` avec 5 routes
  (GET produits, produit, categories, stats · POST prevente) (Critère 6)
- `includes/bootstrap_header.php` : en-tête Bootstrap réutilisable
- `includes/bootstrap_footer.php` : pied de page Bootstrap réutilisable
- `produits_bootstrap.php` : version catalogue avec composants Bootstrap
- Pagination côté serveur avec Bootstrap (9 produits par page)
- Bootstrap Icons sur toutes les pages migrées

### Modifié
- `css/style.css` : variables CSS harmonisées avec Bootstrap
- `js/main.js` : `showToast()` migré vers Bootstrap Toast API

---

## [1.2.0] – 2026-02-10 – Amélioration sécurité

### Ajouté
- Trigger SQL `after_connexion_admin` : log automatique des connexions admin (Critère 18)
- Vue SQL `v_stats_vendeurs` : statistiques agrégées par vendeur (Critère 18)
- Vue SQL `v_produits_actifs` : produits avec progression (Critère 18)
- Script `backup/backup.sh` : mysqldump automatisé (Critère 20)

### Modifié
- `includes/config.php` : ajout `PDO::ATTR_EMULATE_PREPARES = false`
- Sessions : `session.cookie_httponly = true` + `session.cookie_samesite = Strict`

### Corrigé
- **Bug #001** : Hash BCrypt $2y$ (PHP) incompatible avec $2b$ (C#)
  → Page `/Setup` dans AG1 pour régénérer les hashes
- **Bug #002** : Panier non vidé après commande confirmée
  → `DELETE FROM panier WHERE client_id = ?` dans `cart_action.php`

---

## [1.1.0] – 2026-01-15 – AJAX panier + autocomplete

### Ajouté
- `php/cart_action.php` : API AJAX panier (ajout/suppression/quantité)
- `php/search_suggest.php` : autocomplete recherche via AJAX
- Payload base64 pour sécuriser les échanges AJAX
- Animations CSS au scroll (IntersectionObserver)
- Design doré/élégant avec variables CSS

### Modifié
- `panier.php` : migration vers gestion AJAX (sans rechargement de page)

---

## [1.0.0] – 2024-10-01 – Version initiale

### Ajouté (GroupyClient – E1)
- Architecture MVC légère PHP natif
- Système d'authentification BCrypt (clients + admin)
- Catalogue produits avec filtres, tri, recherche
- Fiche produit avec avis et étoiles
- Panier statique (version initiale)
- Passage de commande avec adresse de livraison
- Espace client (historique, profil)
- Panel admin avec KPIs
- Base de données `ecommerce_db` (9 tables)

### Ajouté (GroupyVendeur – AG1)
- Architecture ASP.NET Core 8 MVC
- Authentification Cookie + Claims (rôles vendeur/manager/admin)
- Dashboard KPIs (stock faible, ruptures, commandes, CA)
- CRUD produits avec ajustement stock (4 types)
- Traitement commandes E1 avec flux de statut
- Liste clients acheteurs
- Base de données partagée avec GroupyClient

---

## Convention de versioning

| Version | Signification |
|---------|---------------|
| MAJEUR | Changement incompatible (refonte BDD, nouvelle architecture) |
| MINEUR | Nouvelle fonctionnalité rétrocompatible |
| PATCH  | Correction de bug rétrocompatible |
