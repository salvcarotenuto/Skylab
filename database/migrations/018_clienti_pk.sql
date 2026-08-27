-- SkyLabdb: archiviazione delle righe fornitore presenti in clienti.
CREATE TABLE clienti_legacy_f_backup LIKE clienti;

INSERT INTO clienti_legacy_f_backup
SELECT * FROM clienti
WHERE UPPER(TRIM(COALESCE(CliFor,'')))='F';

-- Dopo la verifica dell'archivio, clienti contiene esclusivamente clienti.
DELETE FROM clienti
WHERE UPPER(TRIM(COALESCE(CliFor,'')))='F';

ALTER TABLE clienti
  DROP COLUMN CliFor,
  MODIFY COLUMN Codice INT NOT NULL FIRST,
  ADD CONSTRAINT PK_clienti PRIMARY KEY (Codice);
