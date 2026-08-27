-- Normalizza la classificazione IVA e collega MovIva alla registrazione MovCont.

CREATE TABLE moviva_before_types_backup_061 LIKE MovIva;
INSERT INTO moviva_before_types_backup_061 SELECT * FROM MovIva;

CREATE TABLE movivarg_before_types_backup_061 LIKE MovIvaRg;
INSERT INTO movivarg_before_types_backup_061 SELECT * FROM MovIvaRg;

CREATE TABLE movcont_before_origin_backup_061 LIKE MovCont;
INSERT INTO movcont_before_origin_backup_061 SELECT * FROM MovCont;

CREATE TABLE movcontdc_before_sector_backup_061 LIKE MovContDc;
INSERT INTO movcontdc_before_sector_backup_061 SELECT * FROM MovContDc;

CREATE TABLE TipiMovIva (
    Codice       VARCHAR(2) NOT NULL,
    Descrizione  VARCHAR(50) NOT NULL,
    RegistroIva  VARCHAR(1) NOT NULL,
    Segno         SMALLINT NOT NULL DEFAULT 1,
    PRIMARY KEY (Codice),
    KEY IX_TipiMovIva_RegistroIva (RegistroIva)
) ENGINE=InnoDB;

INSERT INTO TipiMovIva (Codice, Descrizione, RegistroIva, Segno) VALUES
    ('FA', 'Fattura acquisto',              'A',  1),
    ('FV', 'Fattura vendita',               'V',  1),
    ('CA', 'Nota credito acquisto',         'A', -1),
    ('CV', 'Nota credito vendita',          'V', -1),
    ('DA', 'Nota debito acquisto',          'A',  1),
    ('DV', 'Nota debito vendita',           'V',  1),
    ('RF', 'Ricevuta fiscale',              'C',  1),
    ('SC', 'Scontrino',                     'C',  1),
    ('CG', 'Corrispettivi giornalieri',     'C',  1);

-- Elimina il record anomalo approvato; non possiede righe IVA.
DELETE r
FROM MovIvaRg r
JOIN MovIva v ON v.ID = r.ID
WHERE v.Anno = 0 AND v.Settore = 0 AND v.Codice = 0;

DELETE FROM MovIva
WHERE Anno = 0 AND Settore = 0 AND Codice = 0;

ALTER TABLE MovIva
    ADD COLUMN TipoMovIva VARCHAR(2) NULL AFTER Codice,
    ADD COLUMN Sezionale VARCHAR(1) NOT NULL DEFAULT '' AFTER TipoMovIva,
    ADD COLUMN MovCont_Id INT NULL AFTER Sezionale;

UPDATE MovIva
SET TipoMovIva = CASE
    WHEN Settore = 40 AND Causale IN (41,44) THEN 'FV'
    WHEN Settore = 40 AND Causale = 42 THEN 'DV'
    WHEN Settore = 40 AND Causale = 43 THEN 'CV'
    WHEN Settore = 50 AND Causale = 51 THEN 'FA'
    WHEN Settore = 50 AND Causale = 52 THEN 'DA'
    WHEN Settore = 50 AND Causale = 53 THEN 'CA'
    ELSE NULL
END;

UPDATE MovIva v
JOIN MovCont c
  ON c.Anno = v.Anno
 AND c.Settore = v.Settore
 AND c.Codice = v.Codice
SET v.MovCont_Id = c.ID;

ALTER TABLE MovIva
    DROP INDEX UX_moviva_Mnemonica,
    DROP INDEX IX_moviva_Causale,
    DROP COLUMN Settore,
    DROP COLUMN Causale,
    MODIFY COLUMN TipoMovIva VARCHAR(2) NOT NULL,
    MODIFY COLUMN MovCont_Id INT NOT NULL,
    ADD UNIQUE KEY UX_MovIva_Mnemonica (Anno, TipoMovIva, Sezionale, Codice),
    ADD UNIQUE KEY UX_MovIva_MovCont (MovCont_Id),
    ADD KEY IX_MovIva_Tipo_Data (TipoMovIva, DataDoc),
    ADD CONSTRAINT FK_MovIva_TipiMovIva
        FOREIGN KEY (TipoMovIva) REFERENCES TipiMovIva (Codice)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    ADD CONSTRAINT FK_MovIva_MovCont
        FOREIGN KEY (MovCont_Id) REFERENCES MovCont (ID)
        ON UPDATE CASCADE ON DELETE RESTRICT;

ALTER TABLE MovCont
    DROP INDEX UX_movcont_Anno_Settore_Codice,
    RENAME COLUMN Settore TO OrigineMovimento,
    ADD UNIQUE KEY UX_MovCont_Anno_Origine_Codice
        (Anno, OrigineMovimento, Codice);

ALTER TABLE MovContDc
    DROP COLUMN Settore;
