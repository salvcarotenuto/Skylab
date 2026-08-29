-- Introduce un progressivo stabile per le fotografie di ciascun lavoro.
ALTER TABLE Lavorifoto
  ADD COLUMN Numero SMALLINT NULL AFTER ID;

UPDATE Lavorifoto f
INNER JOIN (
  SELECT ID, FileName,
         ROW_NUMBER() OVER (PARTITION BY ID ORDER BY FileName) AS Numero
  FROM Lavorifoto
) n ON n.ID=f.ID AND n.FileName=f.FileName
SET f.Numero=n.Numero;

ALTER TABLE Lavorifoto
  DROP PRIMARY KEY,
  MODIFY COLUMN Numero SMALLINT NOT NULL,
  ADD PRIMARY KEY (ID, Numero),
  ADD UNIQUE KEY UQ_Lavorifoto_ID_FileName (ID, FileName);
