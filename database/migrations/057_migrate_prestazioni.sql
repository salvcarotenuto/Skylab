-- Consolida chiave e campi obbligatori della tabella Prestazioni.

CREATE TABLE prestazioni_legacy_backup_057 LIKE Prestazioni;
INSERT INTO prestazioni_legacy_backup_057 SELECT * FROM Prestazioni;

ALTER TABLE Prestazioni
    MODIFY COLUMN Codice SMALLINT NOT NULL,
    MODIFY COLUMN Descrizione VARCHAR(100) NOT NULL,
    MODIFY COLUMN Prezzo DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    ADD PRIMARY KEY (Codice);
