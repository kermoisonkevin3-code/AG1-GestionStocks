# Tableau de recette – AG1 Gestion Stocks
# Critère 9 – Tests nécessaires à la validation
# BTS SIO SLAM · KERMOISON Kevin

## Fonctionnel

| # | Scénario | Résultat attendu | Résultat obtenu | Statut |
|---|----------|-----------------|-----------------|--------|
| 01 | Connexion vendeur avec bons identifiants | Dashboard affiché, session Cookie ok | ✅ OK | ✅ |
| 02 | Connexion avec mauvais mdp | Message erreur, pas de session | ✅ OK | ✅ |
| 03 | Dashboard – affichage KPIs | 6 cartes avec valeurs temps réel | ✅ OK | ✅ |
| 04 | Liste produits – filtre stock faible | Seuls produits sous seuil affichés | ✅ OK | ✅ |
| 05 | Ajustement stock (entrée +10) | Stock mis à jour, mouvement créé | ✅ OK | ✅ |
| 06 | Ajustement stock > stock disponible | Rejeté avec message d'erreur | ✅ OK | ✅ |
| 07 | Passage commande statut "expediée" | Stock décrémenté, mouvement créé | ✅ OK | ✅ |
| 08 | Création produit (manager/admin) | Produit en BDD, visible E1 | ✅ OK | ✅ |
| 09 | Accès création produit en tant que vendeur | Accès refusé (403) | ✅ OK | ✅ |
| 10 | Liste clients E1 | Clients affichés depuis BDD partagée | ✅ OK | ✅ |

## Sécurité

| # | Scénario | Résultat attendu | Résultat obtenu | Statut |
|---|----------|-----------------|-----------------|--------|
| S1 | Injection SQL dans recherche produits | Rejeté (MySqlCommand paramétré) | ✅ OK | ✅ |
| S2 | Accès dashboard sans être connecté | Redirection /Auth/Login | ✅ OK | ✅ |
| S3 | POST sans token CSRF (AntiForgery) | Rejeté 400 | ✅ OK | ✅ |

## Non-régression

| # | Après modification | Fonctionnalité testée | Statut |
|---|-------------------|----------------------|--------|
| R1 | Ajout Bootstrap | Sidebar et navigation fonctionnels | ✅ |
| R2 | Transaction UpdateStatut | Commandes normales non affectées | ✅ |
| R3 | Log connexion | Connexion toujours fonctionnelle | ✅ |
