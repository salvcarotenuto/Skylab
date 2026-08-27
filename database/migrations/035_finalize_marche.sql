ALTER TABLE marche
  MODIFY COLUMN Codice SMALLINT NOT NULL,
  MODIFY COLUMN Descrizione VARCHAR(100) NOT NULL,
  ADD CONSTRAINT PK_marche PRIMARY KEY (Codice),
  ADD INDEX IX_marche_Descrizione_Codice (Descrizione,Codice);
