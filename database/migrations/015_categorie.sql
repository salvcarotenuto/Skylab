-- SkyLabdb: chiave e indice della tabella categorie.
ALTER TABLE categorie
  MODIFY COLUMN Codice SMALLINT NOT NULL,
  MODIFY COLUMN Descrizione VARCHAR(100) NOT NULL,
  ADD CONSTRAINT PK_categorie PRIMARY KEY (Codice),
  ADD INDEX IX_categorie_Descrizione (Descrizione);
