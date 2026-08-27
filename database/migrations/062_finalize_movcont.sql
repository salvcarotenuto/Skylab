-- Pulizia definitiva di MovCont, MovContRg e MovContDc.

CREATE TABLE movcont_before_finalize_backup_062 LIKE MovCont;
INSERT INTO movcont_before_finalize_backup_062 SELECT * FROM MovCont;

CREATE TABLE movcontrg_before_finalize_backup_062 LIKE MovContRg;
INSERT INTO movcontrg_before_finalize_backup_062 SELECT * FROM MovContRg;

CREATE TABLE movcontdc_before_finalize_backup_062 LIKE MovContDc;
INSERT INTO movcontdc_before_finalize_backup_062 SELECT * FROM MovContDc;

-- Movimento anomalo privo di righe, documento e movimento IVA.
DELETE FROM MovCont
WHERE Anno = 0
  AND OrigineMovimento = 0
  AND Codice = 0
  AND ID NOT IN (SELECT MovCont_Id FROM MovIva)
  AND ID NOT IN (SELECT Mov_Id FROM MovContDc)
  AND ID NOT IN (SELECT ID FROM MovContRg);

ALTER TABLE MovCont
    DROP INDEX IX_movcont_Documento,
    DROP INDEX UX_MovCont_Anno_Origine_Codice,
    DROP COLUMN Documento,
    CHANGE COLUMN OrigineMovimento Origine TINYINT NOT NULL,
    MODIFY COLUMN Anno SMALLINT NOT NULL,
    MODIFY COLUMN Codice INT NOT NULL,
    MODIFY COLUMN Causale SMALLINT NOT NULL,
    MODIFY COLUMN DataMov DATE NOT NULL,
    MODIFY COLUMN Descrizione VARCHAR(50) NOT NULL,
    MODIFY COLUMN Importo DECIMAL(12,2) NOT NULL,
    ADD UNIQUE KEY UX_MovCont_Anno_Origine_Codice (Anno, Origine, Codice),
    ADD KEY IX_MovCont_DataMov (DataMov),
    ADD KEY IX_MovCont_Causale (Causale);

ALTER TABLE MovContRg
    DROP INDEX IX_movcontrg_stage_Mnemonica,
    DROP COLUMN Anno,
    DROP COLUMN Settore,
    DROP COLUMN Codice;

ALTER TABLE MovContDc
    MODIFY COLUMN TipoDoc VARCHAR(2) NOT NULL;

UPDATE MovContDc SET TipoDoc = 'FV' WHERE TipoDoc = 'V';
UPDATE MovContDc SET TipoDoc = 'SD' WHERE TipoDoc = 'S';
