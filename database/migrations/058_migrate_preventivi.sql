-- Consolida Preventivi e PreventiviRg secondo il modello documentale SkyLab.

CREATE TABLE preventivi_legacy_backup_058 LIKE Preventivi;
INSERT INTO preventivi_legacy_backup_058 SELECT * FROM Preventivi;

CREATE TABLE preventivirg_legacy_backup_058 LIKE PreventiviRg;
INSERT INTO preventivirg_legacy_backup_058 SELECT * FROM PreventiviRg;

-- Gli zeri legacy non rappresentano documenti collegati.
UPDATE Preventivi SET IdDdt = NULL WHERE IdDdt = 0;
UPDATE Preventivi SET IdFattura = NULL WHERE IdFattura = 0;

ALTER TABLE Preventivi
    ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
    MODIFY COLUMN Anno SMALLINT NOT NULL,
    MODIFY COLUMN Codice INT NOT NULL,
    MODIFY COLUMN SpeseTra DECIMAL(10,2) NULL DEFAULT 0.00,
    MODIFY COLUMN SpIncasso DECIMAL(10,2) NULL DEFAULT 0.00,
    MODIFY COLUMN Provvigioni DECIMAL(5,2) NULL DEFAULT 0.00,
    MODIFY COLUMN Merce DECIMAL(12,2) NULL DEFAULT 0.00,
    MODIFY COLUMN Lavoro DECIMAL(12,2) NULL DEFAULT 0.00,
    MODIFY COLUMN Sconto DECIMAL(5,2) NULL DEFAULT 0.00,
    MODIFY COLUMN Totale DECIMAL(12,2) NULL DEFAULT 0.00,
    MODIFY COLUMN IdDdt INT NULL DEFAULT NULL,
    MODIFY COLUMN IdFattura INT NULL DEFAULT NULL,
    ADD PRIMARY KEY (ID),
    ADD UNIQUE KEY UX_Preventivi_Anno_Codice (Anno, Codice),
    ADD KEY IX_Preventivi_DataDoc (DataDoc),
    ADD KEY IX_Preventivi_Cliente (Cliente),
    ADD KEY IX_Preventivi_IdDdt (IdDdt),
    ADD KEY IX_Preventivi_IdFattura (IdFattura),
    ADD CONSTRAINT FK_Preventivi_Ddt
        FOREIGN KEY (IdDdt) REFERENCES Ddt (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Preventivi_Fatture
        FOREIGN KEY (IdFattura) REFERENCES Fatture (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT;

ALTER TABLE PreventiviRg
    ADD COLUMN ID INT NULL FIRST;

UPDATE PreventiviRg r
JOIN Preventivi p
  ON p.Anno = r.Anno
 AND p.Codice = r.Codice
SET r.ID = p.ID;

ALTER TABLE PreventiviRg
    MODIFY COLUMN ID INT NOT NULL,
    MODIFY COLUMN Anno SMALLINT NOT NULL,
    MODIFY COLUMN Codice INT NOT NULL,
    MODIFY COLUMN Riga SMALLINT NOT NULL,
    MODIFY COLUMN Quantita DECIMAL(12,3) NULL DEFAULT 0.000,
    MODIFY COLUMN Peso DECIMAL(12,3) NULL DEFAULT 0.000,
    MODIFY COLUMN Prezzo DECIMAL(12,3) NULL DEFAULT 0.000,
    MODIFY COLUMN Sconto DECIMAL(5,2) NULL DEFAULT 0.00,
    MODIFY COLUMN Importo DECIMAL(12,2) NULL DEFAULT 0.00,
    ADD PRIMARY KEY (ID, Riga),
    ADD KEY IX_PreventiviRg_Anno_Codice (Anno, Codice),
    ADD KEY IX_PreventiviRg_Articolo (Articolo),
    ADD CONSTRAINT FK_PreventiviRg_Preventivi
        FOREIGN KEY (ID) REFERENCES Preventivi (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT;
