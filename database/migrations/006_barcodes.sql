-- SkyLabdb: bonifica e revisione della tabella barcodes.
-- Vengono eliminate tutte le assegnazioni di barcode duplicati e i riferimenti
-- ad articoli inesistenti, perché non consentono di identificare univocamente l'articolo.

DELETE b
FROM barcodes b
LEFT JOIN articoli a ON a.Codice=b.Codice
WHERE a.Codice IS NULL
   OR b.Barcode IN (
      SELECT Barcode FROM (
        SELECT Barcode FROM barcodes GROUP BY Barcode HAVING COUNT(*)>1
      ) duplicated
   );

UPDATE barcodes SET Tipo=COALESCE(Tipo,0);

ALTER TABLE barcodes
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
  MODIFY COLUMN Codice VARCHAR(30) NOT NULL,
  MODIFY COLUMN Barcode VARCHAR(30) NOT NULL,
  MODIFY COLUMN Tipo TINYINT NOT NULL DEFAULT 0,
  ADD CONSTRAINT PK_barcodes PRIMARY KEY (ID),
  ADD CONSTRAINT UX_barcodes_Barcode UNIQUE (Barcode),
  ADD INDEX IX_barcodes_Codice_Tipo (Codice, Tipo),
  ADD CONSTRAINT FK_barcodes_articoli
    FOREIGN KEY (Codice) REFERENCES articoli (Codice)
    ON UPDATE CASCADE ON DELETE CASCADE;
