-- Migrazione già applicata manualmente al database operativo.
ALTER TABLE Barcodes
  RENAME COLUMN Codice TO Articolo,
  RENAME INDEX IX_barcodes_Codice_Tipo TO IX_Barcodes_Articolo_Tipo;
