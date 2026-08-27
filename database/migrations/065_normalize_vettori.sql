-- Normalizza Vettori e consolida i riferimenti documentali.

CREATE TABLE vettori_before_normalize_backup_065 LIKE Vettori;
INSERT INTO vettori_before_normalize_backup_065 SELECT * FROM Vettori;

CREATE TABLE ddt_before_vettori_fk_backup_065 LIKE Ddt;
INSERT INTO ddt_before_vettori_fk_backup_065 SELECT * FROM Ddt;

CREATE TABLE fatture_before_vettori_fk_backup_065 LIKE Fatture;
INSERT INTO fatture_before_vettori_fk_backup_065 SELECT * FROM Fatture;

CREATE TABLE preventivi_before_vettori_fk_backup_065 LIKE Preventivi;
INSERT INTO preventivi_before_vettori_fk_backup_065 SELECT * FROM Preventivi;

-- Record di prova non utilizzato da alcun documento.
DELETE FROM Vettori WHERE Codice = 1;

UPDATE Vettori SET Pagamento = NULL WHERE Pagamento = 0;
UPDATE Vettori SET Banca = NULL WHERE Banca = 0;

ALTER TABLE Vettori
    MODIFY COLUMN Codice SMALLINT NOT NULL,
    MODIFY COLUMN Nome VARCHAR(255) NOT NULL,
    CHANGE COLUMN CodFi CodiceFiscale VARCHAR(20) NULL DEFAULT '',
    CHANGE COLUMN PIva PartitaIva VARCHAR(12) NULL DEFAULT '',
    CHANGE COLUMN CodIban Iban VARCHAR(34) NULL DEFAULT '',
    CHANGE COLUMN Notes Note VARCHAR(250) NULL DEFAULT '',
    MODIFY COLUMN Pagamento SMALLINT NULL DEFAULT NULL,
    MODIFY COLUMN Banca INT NULL DEFAULT NULL,
    ADD PRIMARY KEY (Codice),
    ADD KEY IX_Vettori_Nome (Nome),
    ADD KEY IX_Vettori_PartitaIva (PartitaIva),
    ADD KEY IX_Vettori_Pagamento (Pagamento),
    ADD KEY IX_Vettori_Banca (Banca),
    ADD CONSTRAINT FK_Vettori_Pagamenti
        FOREIGN KEY (Pagamento) REFERENCES Pagamenti (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Vettori_Banche
        FOREIGN KEY (Banca) REFERENCES Banche (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT;

UPDATE Ddt SET Vettore = NULL WHERE Vettore = 0;
UPDATE Fatture SET Vettore = NULL WHERE Vettore = 0;
UPDATE Preventivi SET Vettore = NULL WHERE Vettore = 0;

ALTER TABLE Ddt
    MODIFY COLUMN Vettore SMALLINT NULL DEFAULT NULL,
    ADD KEY IX_Ddt_Vettore (Vettore),
    ADD CONSTRAINT FK_Ddt_Vettori
        FOREIGN KEY (Vettore) REFERENCES Vettori (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT;

ALTER TABLE Fatture
    MODIFY COLUMN Vettore SMALLINT NULL DEFAULT NULL,
    ADD KEY IX_Fatture_Vettore (Vettore),
    ADD CONSTRAINT FK_Fatture_Vettori
        FOREIGN KEY (Vettore) REFERENCES Vettori (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT;

ALTER TABLE Preventivi
    MODIFY COLUMN Vettore SMALLINT NULL DEFAULT NULL,
    ADD KEY IX_Preventivi_Vettore (Vettore),
    ADD CONSTRAINT FK_Preventivi_Vettori
        FOREIGN KEY (Vettore) REFERENCES Vettori (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT;
