-- ============================================================
-- BASE DE DONNÉES PARTAGÉE : E1 (e-commerce) & AG1 (gestion stocks)
-- Nom de la BDD : ecommerce_db
-- ============================================================

CREATE DATABASE IF NOT EXISTS ecommerce_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE ecommerce_db;

-- ============================================================
-- TABLE : categories
-- ============================================================
CREATE TABLE categories (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    nom         VARCHAR(100) NOT NULL,
    description TEXT,
    image_url   VARCHAR(255),
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : produits
-- ============================================================
CREATE TABLE produits (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    categorie_id    INT,
    nom             VARCHAR(200) NOT NULL,
    description     TEXT,
    prix            DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    prix_achat      DECIMAL(10,2) DEFAULT 0.00,
    stock           INT NOT NULL DEFAULT 0,
    stock_min       INT NOT NULL DEFAULT 5,
    reference       VARCHAR(100) UNIQUE,
    image_url       VARCHAR(255),
    actif           TINYINT(1) DEFAULT 1,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (categorie_id) REFERENCES categories(id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : clients (utilisateurs E1)
-- ============================================================
CREATE TABLE clients (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nom             VARCHAR(100) NOT NULL,
    prenom          VARCHAR(100),
    email           VARCHAR(200) NOT NULL UNIQUE,
    mot_de_passe    VARCHAR(255) NOT NULL,
    telephone       VARCHAR(20),
    adresse         TEXT,
    ville           VARCHAR(100),
    code_postal     VARCHAR(10),
    pays            VARCHAR(100) DEFAULT 'France',
    role            ENUM('client','admin') DEFAULT 'client',
    actif           TINYINT(1) DEFAULT 1,
    token_reset     VARCHAR(255) NULL,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : vendeurs (utilisateurs AG1 – C#)
-- ============================================================
CREATE TABLE vendeurs (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nom             VARCHAR(100) NOT NULL,
    prenom          VARCHAR(100),
    email           VARCHAR(200) NOT NULL UNIQUE,
    mot_de_passe    VARCHAR(255) NOT NULL,
    role            ENUM('vendeur','manager','admin') DEFAULT 'vendeur',
    actif           TINYINT(1) DEFAULT 1,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : commandes
-- ============================================================
CREATE TABLE commandes (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    client_id       INT NOT NULL,
    statut          ENUM('en_attente','confirmee','expediee','livree','annulee') DEFAULT 'en_attente',
    total           DECIMAL(10,2) DEFAULT 0.00,
    adresse_livraison TEXT,
    ville_livraison VARCHAR(100),
    code_postal_livraison VARCHAR(10),
    pays_livraison  VARCHAR(100) DEFAULT 'France',
    notes           TEXT,
    vendeur_id      INT NULL,  -- vendeur qui a traité la commande (AG1)
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (client_id) REFERENCES clients(id),
    FOREIGN KEY (vendeur_id) REFERENCES vendeurs(id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : commande_lignes
-- ============================================================
CREATE TABLE commande_lignes (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    commande_id INT NOT NULL,
    produit_id  INT NOT NULL,
    quantite    INT NOT NULL DEFAULT 1,
    prix_unit   DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (commande_id) REFERENCES commandes(id) ON DELETE CASCADE,
    FOREIGN KEY (produit_id) REFERENCES produits(id)
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : panier (sessions panier E1)
-- ============================================================
CREATE TABLE panier (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    client_id   INT NULL,
    session_id  VARCHAR(128),
    produit_id  INT NOT NULL,
    quantite    INT NOT NULL DEFAULT 1,
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE,
    FOREIGN KEY (produit_id) REFERENCES produits(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : mouvements_stock (AG1 – traçabilité)
-- ============================================================
CREATE TABLE mouvements_stock (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    produit_id  INT NOT NULL,
    vendeur_id  INT NOT NULL,
    type        ENUM('entree','sortie','ajustement','retour') NOT NULL,
    quantite    INT NOT NULL,
    motif       VARCHAR(255),
    commande_id INT NULL,
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (produit_id) REFERENCES produits(id),
    FOREIGN KEY (vendeur_id) REFERENCES vendeurs(id),
    FOREIGN KEY (commande_id) REFERENCES commandes(id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ============================================================
-- TABLE : avis_produits
-- ============================================================
CREATE TABLE avis_produits (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    produit_id  INT NOT NULL,
    client_id   INT NOT NULL,
    note        TINYINT NOT NULL CHECK (note BETWEEN 1 AND 5),
    commentaire TEXT,
    approuve    TINYINT(1) DEFAULT 0,
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (produit_id) REFERENCES produits(id) ON DELETE CASCADE,
    FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================
-- DONNÉES DE DÉMO
-- ============================================================

INSERT INTO categories (nom, description) VALUES
('Électronique', 'Smartphones, tablettes, accessoires'),
('Mode', 'Vêtements, chaussures, accessoires de mode'),
('Maison & Jardin', 'Décoration, outillage, jardinage'),
('Sport & Loisirs', 'Équipements sportifs et de loisirs');

INSERT INTO produits (categorie_id, nom, description, prix, prix_achat, stock, stock_min, reference) VALUES
(1, 'Smartphone Pro X', 'Smartphone haut de gamme 6.7 pouces', 899.99, 500.00, 50, 10, 'SPX-001'),
(1, 'Casque Bluetooth Z', 'Casque sans fil avec réduction de bruit', 149.99, 70.00, 30, 5, 'CBZ-002'),
(2, 'Veste Cuir Vintage', 'Veste en cuir véritable coupe slim', 299.99, 120.00, 20, 5, 'VCV-003'),
(3, 'Lampe Design Arc', 'Lampe de salon design scandinave', 89.99, 35.00, 40, 8, 'LDA-004'),
(4, 'Vélo Électrique Urban', 'Vélo électrique urbain 250W', 1299.99, 700.00, 15, 3, 'VEU-005');

-- Admin E1 (password: Admin1234!)
INSERT INTO clients (nom, prenom, email, mot_de_passe, role) VALUES
('Admin', 'E1', 'admin@e1.com', '$2y$12$LZCmF5Q.Ke5sXpFlNqW7..N2D0CcOH4SOAlc0.eVrQfR3RKK0lYLm', 'admin');

-- Demo client (password: Client1234!)
INSERT INTO clients (nom, prenom, email, mot_de_passe) VALUES
('Dupont', 'Jean', 'jean.dupont@demo.com', '$2y$12$YnUQNnN6R5lKTd4CamDULOjWi6h4oE0R0CnNxMbhLxdETCH.aCPYm');

-- Admin AG1 (password: Admin1234!)
INSERT INTO vendeurs (nom, prenom, email, mot_de_passe, role) VALUES
('Admin', 'AG1', 'admin@ag1.com', '$2y$12$LZCmF5Q.Ke5sXpFlNqW7..N2D0CcOH4SOAlc0.eVrQfR3RKK0lYLm', 'admin'),
('Martin', 'Sophie', 'sophie.martin@ag1.com', '$2y$12$LZCmF5Q.Ke5sXpFlNqW7..N2D0CcOH4SOAlc0.eVrQfR3RKK0lYLm', 'vendeur');
