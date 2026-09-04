ALTER TABLE Fornitori ADD COLUMN ULocale SMALLINT NULL AFTER Contatto;
CREATE INDEX IX_Fornitori_ULocale ON Fornitori(ULocale);
