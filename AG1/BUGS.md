# RAPPORT DE BUGS DOCUMENTÉS – GroupyClient & GroupyVendeur
# Critère 14 – Analyser et corriger un dysfonctionnement
# BTS SIO SLAM · KERMOISON Kevin · INGETIS Paris · Session 2026

---

## BUG #001 – Incompatibilité hash BCrypt PHP/C#

| Champ | Détail |
|-------|--------|
| **Date** | 2024-11-15 |
| **Projet** | GroupyVendeur (AG1) |
| **Sévérité** | Haute – Blocage complet de la connexion |
| **Statut** | ✅ Corrigé |

### Symptôme
Impossible de se connecter à AG1 (C#) avec les mots de passe créés depuis E1 (PHP).
Erreur : `BCrypt.Net.BCrypt.Verify()` retourne toujours `false`.

### Analyse
PHP génère des hashes avec le préfixe `$2y$12$...`
BCrypt.Net (C#) attendait le préfixe `$2b$12$...`
Ces deux formats sont fonctionnellement identiques mais
`BCrypt.Net.BCrypt.Verify()` rejetait les hashes `$2y$` dans sa version par défaut.

```php
// PHP - génère $2y$12$...
password_hash($mdp, PASSWORD_BCRYPT, ['cost' => 12]);

// C# - attendait $2b$12$...
BCrypt.Net.BCrypt.HashPassword(mdp, 12);
```

### Correction appliquée
**Option 1 (recommandée)** – Page `/Setup` dans AG1 qui régénère tous les hashes :
```csharp
// AuthService.cs - Régénération des hashes
var hash = BCrypt.Net.BCrypt.HashPassword(mdp, 12);
// hash = $2b$12$... → compatible
```

**Option 2** – Remplacer `$2y$` par `$2b$` avant vérification :
```csharp
var hashCompat = hash.Replace("$2y$", "$2b$");
BCrypt.Net.BCrypt.Verify(mdp, hashCompat);
```

### Test de non-régression
- ✅ Connexion PHP → hash $2y$ → vérifié PHP : OK
- ✅ Connexion C# → hash $2b$ → vérifié C# : OK
- ✅ Hash $2y$ PHP → vérifié C# après remplacement : OK

---

## BUG #002 – Panier non vidé après commande confirmée

| Champ | Détail |
|-------|--------|
| **Date** | 2024-12-03 |
| **Projet** | GroupyClient (E1) |
| **Sévérité** | Moyenne – Mauvaise UX, pas de perte de données |
| **Statut** | ✅ Corrigé |

### Symptôme
Après validation d'une commande, les articles restaient dans le panier.
Le client voyait encore ses articles et pouvait commander deux fois.

### Analyse
Le processus de commande insérait bien la commande en BDD mais ne supprimait pas
les entrées correspondantes dans la table `panier`.

```php
// Code fautif dans panier.php - manquait cette ligne après INSERT commande
// DELETE FROM panier WHERE client_id = ?  ← ABSENT
```

### Correction appliquée
Ajout du DELETE dans la transaction de commande :

```php
// panier.php - après INSERT INTO commandes
$db->beginTransaction();
try {
    // Insérer la commande + les lignes
    $stmtCmd->execute([...]);
    $cmdId = $db->lastInsertId();
    foreach ($articles as $article) {
        $stmtLigne->execute([$cmdId, $article['produit_id'], ...]);
    }
    // ✅ CORRECTION : Vider le panier
    $db->prepare("DELETE FROM panier WHERE client_id = ?")->execute([$clientId]);
    $db->commit();
} catch (Exception $e) {
    $db->rollBack();
}
```

### Test de non-régression
- ✅ Commande validée → panier vide immédiatement
- ✅ Échec transaction → panier non vidé (rollback OK)
- ✅ Commandes passées → non affectées

---

## BUG #003 – Injection XSS dans le champ de recherche

| Champ | Détail |
|-------|--------|
| **Date** | 2025-01-20 |
| **Projet** | GroupyClient (E1) |
| **Sévérité** | Haute – Faille de sécurité XSS |
| **Statut** | ✅ Corrigé |

### Symptôme
En saisissant `<script>alert('XSS')</script>` dans la barre de recherche,
le code JavaScript s'exécutait dans la page résultats.

### Analyse
La valeur du paramètre `?q=` était affichée directement dans le formulaire
sans encodage HTML :

```php
// Code fautif dans produits.php
<input value="<?= $_GET['q'] ?>">  // ← DANGEREUX
```

### Correction appliquée
Application systématique de `htmlspecialchars()` via la fonction `sanitize()` :

```php
// Correction - produits.php
<input value="<?= sanitize($_GET['q'] ?? '') ?>">

// La fonction sanitize() dans config.php
function sanitize(string $data): string {
    return htmlspecialchars(strip_tags(trim($data)), ENT_QUOTES, 'UTF-8');
}
```

### Test de non-régression
- ✅ Saisie `<script>alert('XSS')</script>` → affiché en texte, non exécuté
- ✅ Saisie normale → fonctionne correctement
- ✅ Caractères spéciaux (é, à, ç) → affichés correctement

---

## BUG #004 – Stock négatif après expédition simultanée

| Champ | Détail |
|-------|--------|
| **Date** | 2025-02-10 |
| **Projet** | GroupyVendeur (AG1) |
| **Sévérité** | Haute – Incohérence données BDD |
| **Statut** | ✅ Corrigé |

### Symptôme
Si deux vendeurs cliquaient simultanément sur "Expédier" pour le même produit,
le stock pouvait devenir négatif.

### Analyse
La vérification du stock et la mise à jour étaient deux opérations distinctes
sans verrou. Une condition de course (*race condition*) permettait l'incohérence.

```csharp
// Code fautif - sans transaction ni vérification
var produit = await GetProduitAsync(id); // stock = 5
if (produit.Stock >= quantite) {         // 5 >= 3 → true (les deux threads)
    await UpdateStockAsync(id, -quantite); // -3 + -3 = -1 ← négatif !
}
```

### Correction appliquée
Utilisation d'une transaction SQL avec vérification atomique :

```csharp
// StockService.cs - avec transaction
await using var transaction = await conn.BeginTransactionAsync();
try {
    // Vérification ET mise à jour en une seule requête atomique
    var cmd = new MySqlCommand(@"
        UPDATE produits
        SET stock = stock - @quantite
        WHERE id = @id
          AND stock >= @quantite  -- Garde-fou : jamais négatif
    ", conn, transaction);
    cmd.Parameters.AddWithValue("@quantite", quantite);
    cmd.Parameters.AddWithValue("@id", produitId);
    int rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0) throw new Exception("Stock insuffisant");
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

### Test de non-régression
- ✅ Stock 5, expédition 3 → stock = 2 ✓
- ✅ Stock 2, expédition 3 → rejeté avec message d'erreur ✓
- ✅ Double clic simultané → une seule mise à jour appliquée ✓

---

## Résumé

| Bug | Sévérité | Projet | Résolu | Type |
|-----|----------|--------|--------|------|
| #001 Hash BCrypt | Haute | AG1 | ✅ | Compatibilité |
| #002 Panier non vidé | Moyenne | E1 | ✅ | Logique métier |
| #003 XSS recherche | Haute | E1 | ✅ | Sécurité |
| #004 Stock négatif | Haute | AG1 | ✅ | Race condition |
