CREATE TABLE moviva_legacy_backup_052 LIKE moviva;
INSERT INTO moviva_legacy_backup_052 SELECT * FROM moviva;

ALTER TABLE moviva
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
  ADD CONSTRAINT PK_moviva PRIMARY KEY (ID),
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Settore TINYINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  ADD CONSTRAINT UX_moviva_Mnemonica UNIQUE (Anno, Settore, Codice);

CREATE TABLE movivarg
(
  ID INT NOT NULL,
  ULocale SMALLINT NOT NULL DEFAULT 0,
  AliqIva DECIMAL(5,2) NOT NULL,
  Imponibile DECIMAL(12,2) NULL DEFAULT 0,
  Iva DECIMAL(12,2) NULL DEFAULT 0,
  CONSTRAINT PK_movivarg PRIMARY KEY (ID, ULocale, AliqIva),
  CONSTRAINT FK_movivarg_moviva
    FOREIGN KEY (ID) REFERENCES moviva(ID)
    ON UPDATE RESTRICT ON DELETE CASCADE
) ENGINE=InnoDB;

INSERT INTO movivarg (ID, ULocale, AliqIva, Imponibile, Iva)
SELECT x.ID, 0, x.Aliquota, SUM(x.Imponibile), SUM(x.Iva)
FROM
(
  SELECT m.ID, c.Aliquota,
         CAST(COALESCE(m.Impo1, 0) AS DECIMAL(12,2)) Imponibile,
         CAST(COALESCE(m.Iva1, 0) AS DECIMAL(12,2)) Iva
  FROM moviva m JOIN codiciiva c ON c.Codice = NULLIF(TRIM(m.Codiva1), '')
  WHERE COALESCE(TRIM(m.Codiva1), '') <> '' OR COALESCE(m.Impo1, 0) <> 0 OR COALESCE(m.Iva1, 0) <> 0
  UNION ALL
  SELECT m.ID, c.Aliquota,
         CAST(COALESCE(m.Impo2, 0) AS DECIMAL(12,2)),
         CAST(COALESCE(m.Iva2, 0) AS DECIMAL(12,2))
  FROM moviva m JOIN codiciiva c ON c.Codice = NULLIF(TRIM(m.Codiva2), '')
  WHERE COALESCE(TRIM(m.Codiva2), '') <> '' OR COALESCE(m.Impo2, 0) <> 0 OR COALESCE(m.Iva2, 0) <> 0
  UNION ALL
  SELECT m.ID, c.Aliquota,
         CAST(COALESCE(m.Impo3, 0) AS DECIMAL(12,2)),
         CAST(COALESCE(m.Iva3, 0) AS DECIMAL(12,2))
  FROM moviva m JOIN codiciiva c ON c.Codice = NULLIF(TRIM(m.Codiva3), '')
  WHERE COALESCE(TRIM(m.Codiva3), '') <> '' OR COALESCE(m.Impo3, 0) <> 0 OR COALESCE(m.Iva3, 0) <> 0
) x
GROUP BY x.ID, x.Aliquota;

ALTER TABLE moviva
  MODIFY COLUMN Causale TINYINT NULL DEFAULT 0,
  CHANGE COLUMN Numero NumDoc VARCHAR(20) NULL DEFAULT '',
  ADD COLUMN CtPartita SMALLINT NULL DEFAULT 0 AFTER Ditta,
  ADD COLUMN ULocale SMALLINT NULL DEFAULT 0 AFTER CtPartita,
  MODIFY COLUMN Imponibile DECIMAL(12,2) NULL DEFAULT 0,
  MODIFY COLUMN Iva DECIMAL(12,2) NULL DEFAULT 0,
  MODIFY COLUMN Totale DECIMAL(12,2) NULL DEFAULT 0,
  ADD COLUMN FeName VARCHAR(50) NULL DEFAULT '' AFTER Totale,
  ADD COLUMN Notes VARCHAR(254) NULL DEFAULT '' AFTER FeName,
  DROP COLUMN Sezione,
  DROP COLUMN DataReg,
  DROP COLUMN Protocollo,
  DROP COLUMN Regime,
  DROP COLUMN Segno,
  DROP COLUMN CliFor,
  DROP COLUMN Codiva1,
  DROP COLUMN Codiva2,
  DROP COLUMN Codiva3,
  DROP COLUMN Impo1,
  DROP COLUMN Impo2,
  DROP COLUMN Impo3,
  DROP COLUMN Iva1,
  DROP COLUMN Iva2,
  DROP COLUMN Iva3,
  ADD INDEX IX_moviva_DataDoc (DataDoc),
  ADD INDEX IX_moviva_Ditta_DataDoc (Ditta, DataDoc),
  ADD INDEX IX_moviva_Causale (Causale),
  ADD INDEX IX_moviva_CtPartita (CtPartita);
