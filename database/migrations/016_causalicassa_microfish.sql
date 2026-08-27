-- SkyLabdb: allineamento della tabella causalicassa al modello MicroFish.
ALTER TABLE causalicassa
  MODIFY COLUMN Codice SMALLINT NOT NULL FIRST,
  MODIFY COLUMN Descrizione VARCHAR(100) NOT NULL AFTER Codice,
  MODIFY COLUMN Tipo VARCHAR(1) NOT NULL AFTER Descrizione,
  MODIFY COLUMN Ditta VARCHAR(1) NOT NULL AFTER Tipo,
  MODIFY COLUMN Locked TINYINT(1) NOT NULL DEFAULT 0 AFTER Ditta;
