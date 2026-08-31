-- Codice IVA viene mantenuto e gestito dalla scheda articolo.
-- Data registrazione e provvigione non sono utilizzati e vengono rimossi.

ALTER TABLE Articoli
  DROP COLUMN DataReg,
  DROP COLUMN Provvigione;
