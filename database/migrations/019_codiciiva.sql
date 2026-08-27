-- SkyLabdb: consolidamento tabella codici IVA.
UPDATE codiciiva
SET FeNatura=NULL
WHERE TRIM(COALESCE(FeNatura,''))='';

ALTER TABLE codiciiva
  MODIFY COLUMN Codice VARCHAR(12) NOT NULL,
  MODIFY COLUMN Descrizione VARCHAR(250) NOT NULL,
  MODIFY COLUMN Aliquota DECIMAL(5,2) NOT NULL DEFAULT 0,
  MODIFY COLUMN Detrazione DECIMAL(5,2) NOT NULL DEFAULT 0,
  MODIFY COLUMN FeNatura VARCHAR(10) NULL,
  ADD INDEX IX_codiciiva_Descrizione (Descrizione),
  ADD CONSTRAINT FK_codiciiva_fecodiciiva FOREIGN KEY (FeNatura)
    REFERENCES fecodiciiva (Codice)
    ON UPDATE CASCADE ON DELETE RESTRICT;
