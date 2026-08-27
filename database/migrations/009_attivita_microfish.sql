-- SkyLabdb: allineamento esatto della tabella attivita al modello MicroFish.
ALTER TABLE attivita
  DROP INDEX IDXattivita_TheDate_TheTime,
  DROP INDEX IDXattivita_Utente_TheDate,
  DROP INDEX IDXattivita_Tabella_Codice,
  MODIFY COLUMN ID INT NOT NULL AUTO_INCREMENT FIRST,
  MODIFY COLUMN TheDate DATE NULL AFTER ID,
  MODIFY COLUMN TheTime TIME NULL AFTER TheDate,
  MODIFY COLUMN Azienda SMALLINT NULL DEFAULT 0 AFTER TheTime,
  MODIFY COLUMN Utente SMALLINT NULL DEFAULT 0 AFTER Azienda,
  MODIFY COLUMN Tabella VARCHAR(50) NULL DEFAULT '' AFTER Utente,
  MODIFY COLUMN Azione TINYINT NULL DEFAULT 0 AFTER Tabella,
  MODIFY COLUMN Anno SMALLINT NULL DEFAULT 0 AFTER Azione,
  MODIFY COLUMN Numero VARCHAR(30) NULL AFTER Anno,
  MODIFY COLUMN Codice VARCHAR(30) NULL DEFAULT '' AFTER Numero,
  ADD INDEX IX_Attivita_DataOra (TheDate DESC, TheTime DESC),
  ADD INDEX IX_Attivita_Utente (Utente, TheDate DESC),
  ADD INDEX IX_Attivita_TabellaRecord (Tabella, Codice);
