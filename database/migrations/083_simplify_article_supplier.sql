-- Articoli: un solo fornitore principale e relativo codice articolo.
ALTER TABLE Articoli
  RENAME COLUMN Fornitore1 TO Fornitore,
  RENAME COLUMN CodiceF1 TO CodiceFornitore,
  DROP COLUMN Fornitore2,
  DROP COLUMN Fornitore3,
  DROP COLUMN CodiceF2,
  DROP COLUMN CodiceF3,
  RENAME INDEX IX_articoli_Fornitore1 TO IX_articoli_Fornitore;
