-- SkyLabdb: revisione della tabella accessi.
-- Applicare solo dopo aver verificato che la tabella sia vuota o aver migrato TheDate/TheTime.
ALTER TABLE accessi
  DROP INDEX IDXaccessi_Utente_Azienda_TheDate_TheTime,
  MODIFY COLUMN Utente SMALLINT NOT NULL,
  MODIFY COLUMN Azienda INT NOT NULL,
  DROP COLUMN TheDate,
  DROP COLUMN TheTime,
  ADD COLUMN DataOra DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) AFTER Azienda,
  ADD CONSTRAINT FK_accessi_utenti
    FOREIGN KEY (Utente) REFERENCES utenti (Codice)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  ADD CONSTRAINT FK_accessi_aziende
    FOREIGN KEY (Azienda) REFERENCES aziende (Codice)
    ON UPDATE RESTRICT ON DELETE RESTRICT,
  ADD INDEX IX_accessi_Utente_Azienda_DataOra (Utente, Azienda, DataOra),
  ADD INDEX IX_accessi_DataOra (DataOra);
