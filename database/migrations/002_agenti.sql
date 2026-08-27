-- SkyLabdb: revisione della tabella agenti.
-- I riferimenti orfani presenti in destini non vengono modificati da questa migrazione.
ALTER TABLE agenti
  MODIFY COLUMN Codice SMALLINT NOT NULL,
  MODIFY COLUMN Nome VARCHAR(100) NOT NULL,
  MODIFY COLUMN Provvigione DECIMAL(5,2) NOT NULL DEFAULT 0,
  ADD CONSTRAINT PK_agenti PRIMARY KEY (Codice),
  ADD INDEX IX_agenti_Nome (Nome);
