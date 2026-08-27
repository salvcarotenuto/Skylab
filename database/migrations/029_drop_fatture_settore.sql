ALTER TABLE fatturerg
  DROP INDEX IX_fatturerg_Anno_Settore_Codice_Riga,
  DROP COLUMN Settore,
  ADD INDEX IX_fatturerg_Anno_Codice_Riga (Anno,Codice,Riga);

ALTER TABLE fatture
  DROP INDEX UQ_fatture_Anno_Settore_Codice,
  DROP COLUMN Settore,
  ADD CONSTRAINT UQ_fatture_Anno_Codice UNIQUE (Anno,Codice);
