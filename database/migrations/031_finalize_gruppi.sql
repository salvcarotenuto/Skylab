ALTER TABLE gruppi
  MODIFY COLUMN Codice SMALLINT NOT NULL,
  MODIFY COLUMN Descrizione VARCHAR(100) NOT NULL,
  ADD CONSTRAINT PK_gruppi PRIMARY KEY (Codice),
  ADD INDEX IX_gruppi_Descrizione_Codice (Descrizione,Codice);
