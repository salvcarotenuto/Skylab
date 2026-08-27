UPDATE naturagiu
SET Descrizione = TRIM(COALESCE(Descrizione, ''));

ALTER TABLE naturagiu
  MODIFY COLUMN Descrizione VARCHAR(250) NOT NULL DEFAULT '',
  ADD INDEX IX_naturagiu_Descrizione_Codice (Descrizione, Codice);
