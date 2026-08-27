ALTER TABLE distretti
  MODIFY COLUMN Codice SMALLINT NOT NULL,
  MODIFY COLUMN Descrizione VARCHAR(100) NOT NULL,
  ADD CONSTRAINT PK_distretti PRIMARY KEY (Codice),
  ADD INDEX IX_distretti_Descrizione_Codice (Descrizione,Codice);
