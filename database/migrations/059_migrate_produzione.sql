-- Consolida Produzione e ProduzioneMp e aggiunge la quantità prodotta.

CREATE TABLE produzione_legacy_backup_059 LIKE Produzione;
INSERT INTO produzione_legacy_backup_059 SELECT * FROM Produzione;

CREATE TABLE produzionemp_legacy_backup_059 LIKE ProduzioneMp;
INSERT INTO produzionemp_legacy_backup_059 SELECT * FROM ProduzioneMp;

ALTER TABLE Produzione
    ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
    MODIFY COLUMN Anno SMALLINT NOT NULL,
    MODIFY COLUMN Codice INT NOT NULL,
    ADD COLUMN Quantita DECIMAL(12,3) NOT NULL DEFAULT 1.000 AFTER Cliente,
    MODIFY COLUMN OreLav DECIMAL(10,2) NULL DEFAULT 0.00,
    MODIFY COLUMN CostoOra DECIMAL(10,2) NULL DEFAULT 0.00,
    MODIFY COLUMN CostoLav DECIMAL(12,2) NULL DEFAULT 0.00,
    MODIFY COLUMN CostoMpr DECIMAL(12,2) NULL DEFAULT 0.00,
    MODIFY COLUMN CostiGen DECIMAL(12,2) NULL DEFAULT 0.00,
    MODIFY COLUMN CostoUni DECIMAL(12,3) NULL DEFAULT 0.000,
    MODIFY COLUMN CostoTotale DECIMAL(12,2) NULL DEFAULT 0.00,
    ADD PRIMARY KEY (ID),
    ADD UNIQUE KEY UX_Produzione_Anno_Codice (Anno, Codice),
    ADD KEY IX_Produzione_Prodotto (Prodotto),
    ADD KEY IX_Produzione_Cliente (Cliente);

ALTER TABLE ProduzioneMp
    ADD COLUMN ID INT NULL FIRST;

UPDATE ProduzioneMp r
JOIN Produzione p
  ON p.Anno = r.Anno
 AND p.Codice = r.Codice
SET r.ID = p.ID;

ALTER TABLE ProduzioneMp
    MODIFY COLUMN ID INT NOT NULL,
    MODIFY COLUMN Anno SMALLINT NOT NULL,
    MODIFY COLUMN Codice INT NOT NULL,
    MODIFY COLUMN Riga SMALLINT NOT NULL,
    MODIFY COLUMN Quantita DECIMAL(12,3) NULL DEFAULT 0.000,
    MODIFY COLUMN Prezzo DECIMAL(12,3) NULL DEFAULT 0.000,
    MODIFY COLUMN Importo DECIMAL(12,2) NULL DEFAULT 0.00,
    ADD PRIMARY KEY (ID, Riga),
    ADD KEY IX_ProduzioneMp_Anno_Codice (Anno, Codice),
    ADD KEY IX_ProduzioneMp_Articolo (Articolo),
    ADD CONSTRAINT FK_ProduzioneMp_Produzione
        FOREIGN KEY (ID) REFERENCES Produzione (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT;
