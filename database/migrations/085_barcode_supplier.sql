-- Provenienza facoltativa dei barcode comunicati dal fornitore.
ALTER TABLE Barcodes
  ADD COLUMN Fornitore INT NULL AFTER Tipo,
  ADD INDEX IX_Barcodes_Fornitore (Fornitore),
  ADD CONSTRAINT FK_Barcodes_Fornitori FOREIGN KEY (Fornitore)
    REFERENCES Fornitori(Codice) ON DELETE SET NULL ON UPDATE CASCADE;
