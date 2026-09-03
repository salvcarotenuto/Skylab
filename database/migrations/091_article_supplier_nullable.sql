-- Il fornitore principale dell'articolo è facoltativo.
ALTER TABLE Articoli
  MODIFY COLUMN Fornitore INT NULL DEFAULT NULL;

UPDATE Articoli SET Fornitore = NULL WHERE Fornitore = 0;
