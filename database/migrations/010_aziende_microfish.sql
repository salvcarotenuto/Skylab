-- SkyLabdb: allineamento esatto della tabella aziende al modello MicroFish.
ALTER TABLE aziende
  DROP INDEX IDXaziende_Nome,
  DROP COLUMN ScadenzaLicenza,
  DROP COLUMN UsaUsbKey,
  DROP COLUMN UsbKeyCode,
  DROP COLUMN DataAggiornamentoDb,
  MODIFY COLUMN Codice INT NOT NULL FIRST,
  MODIFY COLUMN Nome VARCHAR(120) NOT NULL AFTER Codice,
  MODIFY COLUMN Password VARCHAR(255) NOT NULL AFTER Nome,
  MODIFY COLUMN Attiva TINYINT(1) NOT NULL DEFAULT 1 AFTER Password,
  MODIFY COLUMN Bloccata TINYINT(1) NOT NULL DEFAULT 0 AFTER Attiva,
  MODIFY COLUMN NomeDatabase VARCHAR(120) NULL AFTER Bloccata,
  MODIFY COLUMN VersioneDbAttuale VARCHAR(30) NULL AFTER NomeDatabase,
  MODIFY COLUMN VersioneDbRichiesta VARCHAR(30) NULL AFTER VersioneDbAttuale,
  ADD CONSTRAINT UX_Aziende_Nome UNIQUE (Nome);
