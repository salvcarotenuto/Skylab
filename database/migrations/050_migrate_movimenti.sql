ALTER TABLE movimenti
  ADD COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
  ADD CONSTRAINT PK_movimenti PRIMARY KEY (ID);

UPDATE movimenti m
JOIN
(
  SELECT ID, MaxRiga + DuplicatePosition - 1 AS NewRiga
  FROM
  (
    SELECT ID, Anno, Settore, Codice, Riga,
           COUNT(*) OVER
             (PARTITION BY Anno, Settore, Codice, Riga) AS DuplicateCount,
           ROW_NUMBER() OVER
             (PARTITION BY Anno, Settore, Codice, Riga ORDER BY ID) AS DuplicatePosition,
           MAX(Riga) OVER
             (PARTITION BY Anno, Settore, Codice) AS MaxRiga
    FROM movimenti
  ) ranked
  WHERE DuplicateCount > 1
    AND DuplicatePosition > 1
) renumbered ON renumbered.ID = m.ID
SET m.Riga = renumbered.NewRiga;

ALTER TABLE movimenti
  MODIFY COLUMN Anno SMALLINT NOT NULL,
  MODIFY COLUMN Settore TINYINT NOT NULL,
  MODIFY COLUMN Codice INT NOT NULL,
  MODIFY COLUMN Riga SMALLINT NOT NULL,
  MODIFY COLUMN Articolo VARCHAR(30) NULL,
  MODIFY COLUMN Quantita DECIMAL(12,3) NULL DEFAULT 0,
  MODIFY COLUMN Prezzo DECIMAL(12,3) NULL DEFAULT 0,
  MODIFY COLUMN Importo DECIMAL(12,2) NULL DEFAULT 0,
  DROP COLUMN Ordine,
  ADD CONSTRAINT UX_movimenti_Mnemonica UNIQUE (Anno, Settore, Codice, Riga),
  ADD INDEX IX_movimenti_Articolo_DataMov (Articolo, DataMov),
  ADD INDEX IX_movimenti_DataMov (DataMov),
  ADD INDEX IX_movimenti_ULocale_DataMov (ULocale, DataMov);
