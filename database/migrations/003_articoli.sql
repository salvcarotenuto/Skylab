-- SkyLabdb: revisione della tabella madre articoli.
-- I 42 duplicati verificati sono copie identiche; viene conservata una riga per Codice.
-- Le foreign key e la bonifica dei riferimenti orfani sono rinviate.

ALTER TABLE articoli
  ADD COLUMN _MigrationRowId BIGINT NOT NULL AUTO_INCREMENT UNIQUE FIRST;

DELETE FROM articoli
WHERE _MigrationRowId IN (
  SELECT duplicate_id FROM (
    SELECT _MigrationRowId duplicate_id,
           ROW_NUMBER() OVER (PARTITION BY Codice ORDER BY _MigrationRowId) rn
    FROM articoli
  ) duplicates
  WHERE rn > 1
);

UPDATE articoli SET
  Categoria=COALESCE(Categoria,0), Gruppo=COALESCE(Gruppo,0),
  Specie=COALESCE(Specie,0), Marca=COALESCE(Marca,0),
  Livello=COALESCE(Livello,0), Peso=COALESCE(Peso,0),
  Pezzi=COALESCE(Pezzi,0), Durata=COALESCE(Durata,0),
  Consumo=COALESCE(Consumo,0), Fornitore1=COALESCE(Fornitore1,0),
  Fornitore2=COALESCE(Fornitore2,0), Fornitore3=COALESCE(Fornitore3,0),
  ScortaMin=COALESCE(ScortaMin,0), ScortaMax=COALESCE(ScortaMax,0),
  Giacin=COALESCE(Giacin,0), CostoStd=COALESCE(CostoStd,0),
  PrezzoStd=COALESCE(PrezzoStd,0), Provvigione=COALESCE(Provvigione,0);

ALTER TABLE articoli
  DROP COLUMN _MigrationRowId,
  MODIFY COLUMN Codice VARCHAR(30) NOT NULL,
  MODIFY COLUMN Descrizione VARCHAR(255) NOT NULL,
  MODIFY COLUMN Categoria SMALLINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Gruppo SMALLINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Specie SMALLINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Marca SMALLINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Livello TINYINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Uma VARCHAR(4) NULL,
  MODIFY COLUMN Uml VARCHAR(4) NULL,
  MODIFY COLUMN Umv VARCHAR(4) NULL,
  MODIFY COLUMN Peso DECIMAL(10,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN Pezzi SMALLINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Durata SMALLINT NOT NULL DEFAULT 0,
  MODIFY COLUMN Consumo DECIMAL(10,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN Fornitore1 INT NOT NULL DEFAULT 0,
  MODIFY COLUMN Fornitore2 INT NOT NULL DEFAULT 0,
  MODIFY COLUMN Fornitore3 INT NOT NULL DEFAULT 0,
  MODIFY COLUMN ScortaMin DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN ScortaMax DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN Giacin DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN CostoStd DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN PrezzoStd DECIMAL(12,3) NOT NULL DEFAULT 0,
  MODIFY COLUMN Codiva VARCHAR(12) NULL,
  MODIFY COLUMN Provvigione DECIMAL(5,2) NOT NULL DEFAULT 0,
  ADD CONSTRAINT PK_articoli PRIMARY KEY (Codice),
  ADD INDEX IX_articoli_Descrizione_Codice (Descrizione, Codice),
  ADD INDEX IX_articoli_Categoria_Gruppo_Descrizione (Categoria, Gruppo, Descrizione),
  ADD INDEX IX_articoli_Marca (Marca),
  ADD INDEX IX_articoli_Fornitore1 (Fornitore1);
