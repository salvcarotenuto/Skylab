-- Uniforma Pagamenti alla struttura MicroFish.

CREATE TABLE pagamenti_legacy_backup_056 LIKE Pagamenti;
INSERT INTO pagamenti_legacy_backup_056 SELECT * FROM Pagamenti;

ALTER TABLE Pagamenti
    MODIFY COLUMN Codice SMALLINT NOT NULL,
    ADD COLUMN TipoPagamento VARCHAR(1) NULL DEFAULT '' AFTER Sigla,
    ADD COLUMN TipoTitolo TINYINT NULL DEFAULT NULL AFTER TipoPagamento,
    RENAME COLUMN `Offset` TO TimeOffset,
    MODIFY COLUMN Spese DECIMAL(10,2) NULL DEFAULT 0.00;

UPDATE Pagamenti
SET TipoPagamento = CASE
        WHEN Codice IN (1,2,3) THEN 'C'
        WHEN Codice IN (4,5) THEN 'R'
        WHEN Codice IN (6,7,10) THEN 'A'
        WHEN Codice IN (8,11,12) THEN 'B'
        WHEN Codice = 9 THEN 'T'
        ELSE ''
    END,
    TipoTitolo = CASE
        WHEN Codice IN (4,5) THEN 6
        WHEN Codice IN (6,7,10) THEN 1
        ELSE NULL
    END;

ALTER TABLE Pagamenti
    DROP COLUMN Tipo,
    ADD PRIMARY KEY (Codice);
